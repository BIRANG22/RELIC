using UnityEngine;
using UnityEngine.UI;

public readonly struct MapLineLayout
{
    public Vector2 AnchoredPosition { get; }
    public Vector2 Size { get; }
    public float RotationDegrees { get; }

    public MapLineLayout(Vector2 anchoredPosition, Vector2 size, float rotationDegrees)
    {
        AnchoredPosition = anchoredPosition;
        Size = size;
        RotationDegrees = rotationDegrees;
    }
}

public class MapLineView : MonoBehaviour
{
    [Header("Line Visual")]
    [SerializeField] private Image lineImage;
    [SerializeField] private Sprite lineSprite;
    [SerializeField, Min(0f)] private float thickness = 13f;

    [Header("Dotted Line Clip")]
    [Tooltip("긴 점선 이미지를 잘라서 보여줄 Mask 영역입니다. 비어 있으면 자식에서 자동으로 찾습니다.")]
    [SerializeField] private RectTransform clipArea;

    [Tooltip("길이와 스케일을 유지할 긴 점선 이미지입니다. 비어 있으면 Line Image의 RectTransform을 사용합니다.")]
    [SerializeField] private RectTransform dottedImageRect;

    private Vector2 originalDottedSize;
    private Vector3 originalDottedScale;
    private bool dottedVisualCached;

    private bool currentAvailable;
    private bool currentTraversed;

    private void Awake()
    {
        ResolveReferences();
        PrepareRotationSafeMask();
        CacheDottedVisual();
    }

    public void Setup(Vector2 from, Vector2 to)
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null)
            return;

        ResolveReferences();
        PrepareRotationSafeMask();
        CacheDottedVisual();

        // 노드 중심에서 다음 노드 중심까지 정확히 연결합니다.
        MapLineLayout layout = CalculateLayout(from, to, thickness, 0f);

        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = layout.AnchoredPosition;
        rect.sizeDelta = layout.Size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.Euler(0f, 0f, layout.RotationDegrees);

        if (clipArea != null)
        {
            // 마스크는 라인의 로컬 X축을 기준으로 폭만 변경합니다.
            // 부모 라인 오브젝트와 함께 회전하므로 위/아래 대각선 모두 같은 방식으로 잘립니다.
            clipArea.anchorMin = new Vector2(0.5f, 0.5f);
            clipArea.anchorMax = new Vector2(0.5f, 0.5f);
            clipArea.pivot = new Vector2(0.5f, 0.5f);
            clipArea.anchoredPosition = Vector2.zero;
            clipArea.sizeDelta = new Vector2(layout.Size.x, layout.Size.y);
            clipArea.localScale = Vector3.one;
            clipArea.localRotation = Quaternion.identity;
        }

        if (dottedImageRect != null && dottedVisualCached)
        {
            // 긴 원본 점선의 길이/간격이 변하지 않도록 원래 크기와 스케일을 유지합니다.
            dottedImageRect.anchorMin = new Vector2(0f, 0.5f);
            dottedImageRect.anchorMax = new Vector2(0f, 0.5f);
            dottedImageRect.pivot = new Vector2(0f, 0.5f);
            dottedImageRect.anchoredPosition = Vector2.zero;
            dottedImageRect.sizeDelta = originalDottedSize;
            dottedImageRect.localScale = originalDottedScale;
            dottedImageRect.localRotation = Quaternion.identity;
        }

        if (lineImage == null)
            return;

        if (lineSprite != null)
            lineImage.sprite = lineSprite;

        lineImage.preserveAspect = false;
        lineImage.raycastTarget = false;
        ApplyLineColor();
    }

    public void SetAvailabilityVisual(bool available)
    {
        SetProgressVisual(available, false);
    }

    public void SetProgressVisual(bool available, bool traversed)
    {
        currentAvailable = available;
        currentTraversed = traversed;
        ApplyLineColor();
    }

    private void ApplyLineColor()
    {
        if (lineImage == null)
            return;

        Color availableColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        Color unavailableColor = new Color32(0x77, 0x77, 0x77, 0xFF);
        lineImage.color = (currentAvailable || currentTraversed) ? availableColor : unavailableColor;
    }

    private void ResolveReferences()
    {
        if (clipArea == null)
        {
            // 회전에 안전한 일반 Mask를 우선 찾습니다.
            Mask mask = GetComponentInChildren<Mask>(true);
            if (mask != null)
            {
                clipArea = mask.GetComponent<RectTransform>();
            }
            else
            {
                RectMask2D rectMask = GetComponentInChildren<RectMask2D>(true);
                if (rectMask != null)
                    clipArea = rectMask.GetComponent<RectTransform>();
            }
        }

        if (lineImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                RectTransform imageRect = images[i].rectTransform;
                if (clipArea == null || imageRect == clipArea || !imageRect.IsChildOf(clipArea))
                    continue;

                lineImage = images[i];
                break;
            }

            if (lineImage == null && images.Length > 0)
                lineImage = images[0];
        }

        if (dottedImageRect == null && lineImage != null)
            dottedImageRect = lineImage.rectTransform;
    }

    private void PrepareRotationSafeMask()
    {
        if (clipArea == null)
            return;

        // RectMask2D는 회전된 UI에서 축 정렬 클리핑 때문에 대각선 방향에 따라
        // 내용이 사라질 수 있습니다. 스텐실 기반 Mask로 바꿔 회전된 사각형 그대로 자릅니다.
        RectMask2D rectMask = clipArea.GetComponent<RectMask2D>();
        if (rectMask != null)
            rectMask.enabled = false;

        Mask mask = clipArea.GetComponent<Mask>();
        if (mask == null)
            mask = clipArea.gameObject.AddComponent<Mask>();

        Image maskGraphic = clipArea.GetComponent<Image>();
        if (maskGraphic == null)
            maskGraphic = clipArea.gameObject.AddComponent<Image>();

        maskGraphic.raycastTarget = false;
        maskGraphic.color = Color.white;
        mask.showMaskGraphic = false;
    }

    private void CacheDottedVisual()
    {
        if (dottedVisualCached || dottedImageRect == null)
            return;

        originalDottedSize = dottedImageRect.sizeDelta;
        originalDottedScale = dottedImageRect.localScale;
        dottedVisualCached = true;
    }

    public static MapLineLayout CalculateLayout(
        Vector2 from,
        Vector2 to,
        float thickness,
        float endpointInset)
    {
        Vector2 direction = to - from;
        float trimmedDistance = Mathf.Max(0f, direction.magnitude - Mathf.Max(0f, endpointInset) * 2f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        return new MapLineLayout(
            (from + to) * 0.5f,
            new Vector2(trimmedDistance, Mathf.Max(0f, thickness)),
            angle);
    }
}
