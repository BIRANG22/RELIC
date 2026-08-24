using System;
using System.Collections;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class MapNodeView : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    [Header("Click Check Animation")]
    [SerializeField] private bool playCheckAnimationBeforeClick = true;
    [SerializeField] private Image checkAnimationImage;
    [SerializeField] private bool autoCreateCheckAnimationImage = true;
    [SerializeField] private Sprite[] checkAnimationSprites = Array.Empty<Sprite>();
    [SerializeField] private float checkFrameInterval = 0.05f;
    private static readonly Vector2 CheckImageSize = new Vector2(80f, 80f);
    [SerializeField] private bool hideIconDuringCheckAnimation;
    [SerializeField] private bool keepLastCheckSpriteUntilRoomOpens = true;
    [SerializeField] private int keepFrameIndex = -1;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private Color checkImageColor = new Color32(0xB2, 0x3E, 0x45, 0xFF);

    [Header("Check Animation SFX")]
    [SerializeField] private bool playCheckAnimationSfx = true;
    [SerializeField] private SfxType checkAnimationSfxType = SfxType.BattleMapNodeCheckAnimation;
    [SerializeField, Range(0f, 1f)] private float checkAnimationSfxVolume = 1f;

    [Header("Persistent Check")]
    [SerializeField] private bool showCheckSpriteForVisitedNode = true;
    [SerializeField] private bool showCheckSpriteForClearedNode = true;

    [Header("Selectable Hover Breath")]
    [SerializeField] private bool useSelectableHoverBreath = true;
    [SerializeField] private float hoverBreathScaleMultiplier = 1.08f;
    [SerializeField] private float hoverBreathSpeed = 4f;

    private GeneratedMapNodeData nodeData;
    private Action<GeneratedMapNodeData> onClicked;
    private Coroutine clickRoutine;
    private bool isClickProcessing;
    private bool currentCanClick;
    private bool currentAvailable;
    private bool currentVisited;
    private bool isCategoryHighlighted;
    private RectTransform rectTransform;
    private Vector3 baseScale = Vector3.one;
    private Color baseIconColor = Color.white;
    private bool hasCapturedBaseIconColor;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        baseScale = transform.localScale;

        if (button == null)
            button = GetComponent<Button>();

        CaptureBaseIconColor();
        EnsureCheckAnimationImage();
    }

    private void OnDisable()
    {
        ResetHoverScale();
    }

    private void Update()
    {
        if (CanPlayHoverBreath())
            UpdateSelectableHoverBreath();
        else
            ResetHoverScale();
    }

    public void Setup(
        GeneratedMapNodeData data,
        MapNodeIconDatabase iconDatabase,
        Action<GeneratedMapNodeData> clickCallback,
        bool canClick)
    {
        nodeData = data;
        onClicked = clickCallback;
        currentCanClick = canClick;
        currentAvailable = false;
        currentVisited = false;
        isCategoryHighlighted = false;
        isClickProcessing = false;
        CaptureBaseScale();
        ResetHoverScale();

        if (clickRoutine != null)
        {
            StopCoroutine(clickRoutine);
            clickRoutine = null;
        }

        EnsureCheckAnimationImage();
        HideCheckImage();

        if (iconImage != null &&
            iconDatabase != null &&
            data != null &&
            iconDatabase.TryGetIcon(data.Type, out Sprite icon))
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }

        ApplyPersistentCheckSpriteFromRuntime();
        SetClickable(canClick);
    }

    public void SetClickable(bool canClick)
    {
        currentCanClick = canClick;

        if (button != null)
            button.interactable = canClick && !isClickProcessing;

        if (!CanPlayHoverBreath())
            ResetHoverScale();
    }

    public void SetAvailabilityVisual(bool available)
    {
        SetProgressVisual(available, false);
    }

    public void SetProgressVisual(bool available, bool visited)
    {
        currentAvailable = available;
        currentVisited = visited;
        ApplyNodeColor();
    }

    public void SetCategoryHighlighted(bool highlighted)
    {
        // 이동 가능 여부 색상이 항상 우선되도록 카테고리 강조는 색상을 변경하지 않습니다.
        isCategoryHighlighted = highlighted;
        ApplyNodeColor();
    }

    private void ApplyNodeColor()
    {
        if (iconImage == null)
            return;

        Color availableColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        Color unavailableColor = new Color32(0x77, 0x77, 0x77, 0xFF);
        iconImage.color = (currentAvailable || currentVisited) ? availableColor : unavailableColor;
    }
    private void CaptureBaseIconColor()
    {
        if (iconImage == null || hasCapturedBaseIconColor)
            return;

        baseIconColor = iconImage.color;
        baseIconColor.a = 1f;
        hasCapturedBaseIconColor = true;
    }

    public void OnClick()
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (nodeData == null)
            return;

        if (isClickProcessing)
            return;

        if (button != null && !button.interactable)
            return;

        if (playCheckAnimationBeforeClick && HasCheckAnimationSprites())
        {
            clickRoutine = StartCoroutine(PlayCheckAnimationThenClick());
            return;
        }

        InvokeClick();
    }

    private IEnumerator PlayCheckAnimationThenClick()
    {
        isClickProcessing = true;
        ResetHoverScale();

        if (button != null)
            button.interactable = false;

        if (hideIconDuringCheckAnimation && iconImage != null)
            iconImage.enabled = false;

        EnsureCheckAnimationImage();
        PlayCheckAnimationSfx();

        if (checkAnimationImage != null)
        {
            checkAnimationImage.color = checkImageColor;
            checkAnimationImage.enabled = true;
            checkAnimationImage.gameObject.SetActive(true);
            checkAnimationImage.transform.SetAsLastSibling();
        }

        for (int i = 0; i < checkAnimationSprites.Length; i++)
        {
            if (checkAnimationSprites[i] != null && checkAnimationImage != null)
            {
                checkAnimationImage.sprite = checkAnimationSprites[i];
                checkAnimationImage.color = checkImageColor;
                checkAnimationImage.enabled = true;
            }

            if (checkFrameInterval > 0f)
            {
                if (useUnscaledTime)
                    yield return new WaitForSecondsRealtime(checkFrameInterval);
                else
                    yield return new WaitForSeconds(checkFrameInterval);
            }
            else
            {
                yield return null;
            }
        }

        if (keepLastCheckSpriteUntilRoomOpens)
            ShowKeepFrame();
        else
            HideCheckImage();

        if (hideIconDuringCheckAnimation && iconImage != null)
            iconImage.enabled = true;

        InvokeClick();
    }

    private void PlayCheckAnimationSfx()
    {
        if (!playCheckAnimationSfx)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(checkAnimationSfxType, checkAnimationSfxVolume);
    }

    private void CaptureBaseScale()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        baseScale = transform.localScale;
    }

    private void UpdateSelectableHoverBreath()
    {
        if (!CanPlayHoverBreath())
            return;

        float speed = Mathf.Max(0.01f, hoverBreathSpeed);
        float multiplier = Mathf.Max(1f, hoverBreathScaleMultiplier);
        float t = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
        float scale = Mathf.Lerp(1f, multiplier, t);

        transform.localScale = baseScale * scale;
    }

    private bool CanPlayHoverBreath()
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return false;

        if (!useSelectableHoverBreath)
            return false;

        if (!currentCanClick || isClickProcessing)
            return false;

        if (button != null && !button.interactable)
            return false;

        return true;
    }

    private void ResetHoverScale()
    {
        transform.localScale = baseScale;
    }

    private void InvokeClick()
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        onClicked?.Invoke(nodeData);
    }

    private void ApplyPersistentCheckSpriteFromRuntime()
    {
        if (nodeData == null)
            return;

        if (!HasCheckAnimationSprites())
            return;

        MapRuntimeData runtime = null;

        if (DataManager.Instance != null && DataManager.Instance.MapRuntimeStore != null)
            runtime = DataManager.Instance.MapRuntimeStore.Get();

        if (runtime == null)
            return;

        if (!ShouldShowPersistentCheck(runtime))
            return;

        ShowKeepFrame();
    }

    private bool ShouldShowPersistentCheck(MapRuntimeData runtime)
    {
        string nodeKey = GetNodeKey();

        if (string.IsNullOrWhiteSpace(nodeKey))
            return false;

        if (showCheckSpriteForVisitedNode && runtime.VisitedMapIds != null && runtime.VisitedMapIds.Contains(nodeKey))
            return true;

        if (showCheckSpriteForClearedNode && runtime.ClearedMapIds != null && runtime.ClearedMapIds.Contains(nodeKey))
            return true;

        return false;
    }

    private string GetNodeKey()
    {
        if (nodeData == null)
            return string.Empty;

        return nodeData.NodeIndex.ToString();
    }

    private bool HasCheckAnimationSprites()
    {
        if (checkAnimationSprites == null || checkAnimationSprites.Length <= 0)
            return false;

        for (int i = 0; i < checkAnimationSprites.Length; i++)
        {
            if (checkAnimationSprites[i] != null)
                return true;
        }

        return false;
    }

    private void ShowKeepFrame()
    {
        if (!HasCheckAnimationSprites())
            return;

        EnsureCheckAnimationImage();

        if (checkAnimationImage == null)
            return;

        int index = keepFrameIndex;

        if (index < 0 || index >= checkAnimationSprites.Length)
            index = checkAnimationSprites.Length - 1;

        while (index >= 0 && checkAnimationSprites[index] == null)
            index--;

        if (index < 0)
            return;

        checkAnimationImage.sprite = checkAnimationSprites[index];
        checkAnimationImage.color = checkImageColor;
        checkAnimationImage.enabled = true;
        checkAnimationImage.gameObject.SetActive(true);
        checkAnimationImage.transform.SetAsLastSibling();
    }

    private void HideCheckImage()
    {
        if (checkAnimationImage == null)
            return;

        checkAnimationImage.sprite = null;
        checkAnimationImage.enabled = false;
        checkAnimationImage.gameObject.SetActive(false);
    }

    private void EnsureCheckAnimationImage()
    {
        if (checkAnimationImage != null)
        {
            ConfigureCheckImageRect(checkAnimationImage.rectTransform);
            checkAnimationImage.raycastTarget = false;
            checkAnimationImage.color = checkImageColor;
            return;
        }

        if (!autoCreateCheckAnimationImage)
            return;

        RectTransform parentRect = transform as RectTransform;

        if (parentRect == null)
            return;

        GameObject imageObject = new GameObject("CheckAnimationImage", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(transform, false);
        imageObject.transform.SetAsLastSibling();

        checkAnimationImage = imageObject.GetComponent<Image>();
        checkAnimationImage.raycastTarget = false;
        checkAnimationImage.color = checkImageColor;

        ConfigureCheckImageRect(checkAnimationImage.rectTransform);
        HideCheckImage();
    }

    private void ConfigureCheckImageRect(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = CheckImageSize;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
