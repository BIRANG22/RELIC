using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class RelicIconUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;

    [Header("Hover Breath Effect")]
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private bool useHoverBreathEffect = true;
    [SerializeField] private float hoverMaxScale = 1.1f;
    [SerializeField] private float breathSpeed = 2f;
    [SerializeField] private float scaleLerpSpeed = 14f;

    [Header("Selected Effect")]
    [SerializeField] private bool useSelectedScale = true;
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private bool boostSortingOnHoverOrSelected = true;
    [SerializeField] private int sortingOrderBoost = 2000;

    [Header("Sound")]
    [SerializeField] private bool playClickSfx = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfxId = AudioIds.Sfx.NormalButtonClick;

    private string relicId;
    private RelicEquipPanelUI owner;
    private RectTransform rectTransform;
    private Vector3 baseScale = Vector3.one;
    private bool hasCapturedBaseScale;
    private bool isPointerOver;
    private bool isSelected;
    private Canvas sortingCanvas;
    private bool hadSortingCanvas;
    private bool originalOverrideSorting;
    private int originalSortingOrder;

    public string RelicId => relicId;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (scaleTarget == null)
            scaleTarget = rectTransform;

        CaptureBaseScaleOnce();

        if (iconImage == null)
            iconImage = GetComponent<Image>();

        CaptureSortingCanvas();
        ApplyScale(true);
        ApplySortingState();
    }

    private void Update()
    {
        if (NeedsScaleAnimation())
            ApplyScale(false);
    }

    private void OnDisable()
    {
        isPointerOver = false;
        isSelected = false;
        ResetScale();
        ApplySortingState();
        owner?.HideRelicTooltip();
    }

    public void Setup(string relicId)
    {
        this.relicId = relicId;
        isPointerOver = false;
        isSelected = false;

        RefreshIcon();
        ApplyScale(true);
        ApplySortingState();
    }

    public void Setup(string relicId, RelicEquipPanelUI owner)
    {
        this.relicId = relicId;
        this.owner = owner;
        isPointerOver = false;
        isSelected = false;

        RefreshIcon();
        ApplyScale(true);
        ApplySortingState();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected && !string.IsNullOrWhiteSpace(relicId);
        ApplyScale(false);
        ApplySortingState();
    }

    private void RefreshIcon()
    {
        if (iconImage == null)
            return;

        iconImage.raycastTarget = true;

        if (!string.IsNullOrWhiteSpace(relicId) &&
            DataManager.Instance != null &&
            DataManager.Instance.RelicIconDatabase != null &&
            DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out Sprite icon))
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    public void Clear()
    {
        relicId = null;
        isPointerOver = false;
        isSelected = false;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            iconImage.raycastTarget = true;
        }

        ApplyScale(true);
        ApplySortingState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (string.IsNullOrWhiteSpace(relicId))
            return;

        PlayClickSfx();

        if (owner == null)
        {
            Debug.LogWarning($"[RelicIconUI] owner is missing / Relic:{relicId}");
            return;
        }

        owner.SelectInventoryRelicIcon(this);
        owner.SelectRelic(relicId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (string.IsNullOrWhiteSpace(relicId))
            return;

        isPointerOver = true;
        ApplySortingState();

        if (owner == null)
            return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        owner.ShowRelicTooltip(relicId, rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        ApplySortingState();
        owner?.HideRelicTooltip();
    }

    private void ApplyScale(bool instant)
    {
        if (scaleTarget == null)
            return;

        CaptureBaseScaleOnce();

        float scaleMultiplier = 1f;

        if (useSelectedScale && isSelected && !string.IsNullOrWhiteSpace(relicId))
        {
            scaleMultiplier = selectedScale;
        }
        else if (useHoverBreathEffect && isPointerOver && !string.IsNullOrWhiteSpace(relicId))
        {
            // Repeat between 1.0 and the configured hover max scale.
            float breathT = (Mathf.Sin(Time.unscaledTime * breathSpeed) + 1f) * 0.5f;
            scaleMultiplier = Mathf.Lerp(1f, hoverMaxScale, breathT);
        }

        Vector3 targetScale = baseScale * scaleMultiplier;

        if (instant)
        {
            scaleTarget.localScale = targetScale;
            return;
        }

        float t = 1f - Mathf.Exp(-scaleLerpSpeed * Time.unscaledDeltaTime);
        scaleTarget.localScale = Vector3.Lerp(scaleTarget.localScale, targetScale, t);
    }

    private void ResetScale()
    {
        CaptureBaseScaleOnce();

        if (scaleTarget != null)
            scaleTarget.localScale = baseScale;
    }

    private bool NeedsScaleAnimation()
    {
        if (scaleTarget == null)
            return false;

        if (useHoverBreathEffect && isPointerOver && !string.IsNullOrWhiteSpace(relicId) && !isSelected)
            return true;

        CaptureBaseScaleOnce();
        float multiplier = useSelectedScale && isSelected && !string.IsNullOrWhiteSpace(relicId) ? selectedScale : 1f;
        Vector3 targetScale = baseScale * multiplier;
        return (scaleTarget.localScale - targetScale).sqrMagnitude > 0.000001f;
    }

    private void CaptureBaseScaleOnce()
    {
        if (hasCapturedBaseScale || scaleTarget == null)
            return;

        baseScale = scaleTarget.localScale;
        hasCapturedBaseScale = true;
    }

    private void CaptureSortingCanvas()
    {
        if (!boostSortingOnHoverOrSelected || scaleTarget == null)
            return;

        if (sortingCanvas != null)
            return;

        sortingCanvas = scaleTarget.GetComponent<Canvas>();
        hadSortingCanvas = sortingCanvas != null;

        if (sortingCanvas == null)
            sortingCanvas = scaleTarget.gameObject.AddComponent<Canvas>();

        if (sortingCanvas.GetComponent<GraphicRaycaster>() == null)
            sortingCanvas.gameObject.AddComponent<GraphicRaycaster>();

        originalOverrideSorting = sortingCanvas.overrideSorting;
        originalSortingOrder = sortingCanvas.sortingOrder;
    }

    private void ApplySortingState()
    {
        if (!boostSortingOnHoverOrSelected || sortingCanvas == null)
            return;

        bool shouldBoost = (isPointerOver || isSelected) && !string.IsNullOrWhiteSpace(relicId);

        if (shouldBoost)
        {
            sortingCanvas.overrideSorting = true;
            sortingCanvas.sortingOrder = sortingOrderBoost;
        }
        else
        {
            sortingCanvas.overrideSorting = hadSortingCanvas && originalOverrideSorting;
            sortingCanvas.sortingOrder = hadSortingCanvas ? originalSortingOrder : 0;
        }
    }

    private void PlayClickSfx()
    {
        if (!playClickSfx || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfxId);
    }
}
