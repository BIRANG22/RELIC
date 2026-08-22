using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// reference 아래에 하나만 존재하는 공용 Nameinfo입니다.
/// 호버한 아이콘의 실제 하단 중앙 바로 아래에 붙어서 이름을 표시합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CompoundReferenceNameTooltip : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;

    [Tooltip("아이콘 하단에서 추가로 떨어뜨릴 거리입니다. Y가 음수면 아래쪽으로 내려갑니다.")]
    [SerializeField] private Vector2 offset = new Vector2(0f, -4f);

    private RectTransform rectTransform;
    private RectTransform currentTarget;

    private void Awake()
    {
        ResolveReferences();
        Hide();
    }

    private void LateUpdate()
    {
        if (!gameObject.activeSelf)
            return;

        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            Hide();
            return;
        }

        UpdatePosition();
    }

    public void Show(RectTransform targetIcon, string displayName)
    {
        if (targetIcon == null || string.IsNullOrWhiteSpace(displayName))
        {
            Hide();
            return;
        }

        ResolveReferences();
        if (rectTransform == null || rectTransform.parent is not RectTransform)
            return;

        currentTarget = targetIcon;

        if (nameText != null)
            nameText.text = displayName;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        UpdatePosition();
    }

    public void Hide()
    {
        currentTarget = null;

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void UpdatePosition()
    {
        if (currentTarget == null || rectTransform == null || rectTransform.parent is not RectTransform parentRect)
            return;

        Vector3[] corners = new Vector3[4];
        currentTarget.GetWorldCorners(corners);
        Vector3 bottomCenterWorld = (corners[0] + corners[3]) * 0.5f;

        Camera eventCamera = GetEventCamera();
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, bottomCenterWorld);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, eventCamera, out Vector2 iconBottomLocal))
            return;

        // Nameinfo의 '윗변 중앙'이 아이콘의 '아랫변 중앙'에 오도록 맞춥니다.
        // Pivot이 중앙/아래/위 어디에 있어도 실제 표시 박스가 아이콘 바로 아래에 위치합니다.
        Rect tooltipRect = rectTransform.rect;
        Vector2 pivot = rectTransform.pivot;
        Vector2 topCenterFromPivot = new Vector2(
            (0.5f - pivot.x) * tooltipRect.width,
            (1f - pivot.y) * tooltipRect.height);

        Vector2 desiredTopCenter = iconBottomLocal + offset;
        Vector2 desiredPivotPosition = desiredTopCenter - topCenterFromPivot;

        Vector3 localPosition = rectTransform.localPosition;
        localPosition.x = desiredPivotPosition.x;
        localPosition.y = desiredPivotPosition.y;
        rectTransform.localPosition = localPosition;
    }

    private Camera GetEventCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private void ResolveReferences()
    {
        rectTransform ??= transform as RectTransform;
        nameText ??= GetComponentInChildren<TMP_Text>(true);
    }
}

/// <summary>
/// 실제로 공개된 Icon에만 런타임으로 붙는 호버 컴포넌트입니다.
/// qus에는 붙지 않으므로 미발견 이름은 노출되지 않습니다.
/// </summary>
public sealed class CompoundReferenceIconHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CompoundReferenceNameTooltip tooltip;
    private RectTransform targetIcon;
    private string displayName;

    public void Initialize(CompoundReferenceNameTooltip targetTooltip, RectTransform icon, string name)
    {
        tooltip = targetTooltip;
        targetIcon = icon;
        displayName = name ?? string.Empty;
    }

    public void Clear()
    {
        tooltip = null;
        targetIcon = null;
        displayName = string.Empty;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip != null && targetIcon != null && !string.IsNullOrWhiteSpace(displayName))
            tooltip.Show(targetIcon, displayName);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null)
            tooltip.Hide();
    }

    private void OnDisable()
    {
        if (tooltip != null)
            tooltip.Hide();
    }
}
