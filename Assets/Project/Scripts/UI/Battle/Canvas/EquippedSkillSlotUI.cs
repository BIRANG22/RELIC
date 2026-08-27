using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class EquippedSkillSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Icon")]
    [SerializeField] private Image iconImage;

    [Header("Interaction")]
    [SerializeField] private Button button;
    [SerializeField] private bool hideEmptySlotIcon = true;
    [SerializeField] private bool canClick = true;

    [Header("Hover Border")]
    [SerializeField] private Image borderImage;
    [SerializeField] private bool useHoverBorderColor = true;
    [SerializeField] private Color normalBorderColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color hoverBorderColor = new Color(1f, 0.86f, 0.35f, 1f);
    [SerializeField] private Color selectedBorderColor = new Color(1f, 0.58f, 0.12f, 1f);

    [Header("Equip Available Highlight")]
    [SerializeField] private Color equipAvailableColor = new Color32(78, 103, 223, 255);
    [SerializeField] private Color equipAvailableBreathColor = Color.white;
    private const float EquipAvailableBreathSpeed = 0.5f;

    [Header("Selected Effect")]
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private bool useSelectedScale = true;
    [SerializeField] private float selectedScale = 1.06f;
    [SerializeField] private float scaleLerpSpeed = 14f;
    [SerializeField] private bool boostSortingOnHoverOrSelected = true;
    [SerializeField] private int sortingOrderBoost = 2000;

    [Header("Sound")]
    [SerializeField] private bool playClickSfx = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfxId = AudioIds.Sfx.NormalButtonClick;

    private EquippedSkillPanelUI ownerPanel;
    private SkillInventoryPanelUI skillInventoryPanel;
    private SkillMasterData skillData;
    private CharacterRuntimeData runtimeData;
    private int equippedSkillIndex = -1;
    private RectTransform rectTransform;
    private Vector3 baseScale = Vector3.one;
    private bool hasCapturedBaseScale;
    private bool isPointerOver;
    private bool isSelected;
    private bool isEquipAvailableHighlighted;
    private float equipAvailableHighlightStartTime;
    private Canvas sortingCanvas;
    private bool hadSortingCanvas;
    private bool originalOverrideSorting;
    private int originalSortingOrder;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (scaleTarget == null)
            scaleTarget = rectTransform;

        CaptureBaseScaleOnce();

        if (iconImage == null)
            iconImage = GetComponent<Image>();

        if (button == null)
            button = GetComponent<Button>();

        CaptureSortingCanvas();
        ApplyBorderState();
        ApplyScale(true);
        ApplySortingState();
    }

    private void Update()
    {
        ApplyScale(false);

        if (isEquipAvailableHighlighted)
            ApplyBorderState();
    }

    private void OnDisable()
    {
        isPointerOver = false;
        isSelected = false;
        isEquipAvailableHighlighted = false;
        ApplyBorderState();
        ResetScale();
        ApplySortingState();

        if (ownerPanel != null)
            ownerPanel.HideSkillTooltip();
    }

    public void SetSkill(Sprite icon)
    {
        SetSkill(null, null, icon);
    }

    public void SetSkill(EquippedSkillPanelUI owner, SkillMasterData data, Sprite icon)
    {
        SetSkill(owner, data, icon, true, null);
    }

    public void SetSkill(EquippedSkillPanelUI owner, SkillMasterData data, Sprite icon, bool clickable)
    {
        SetSkill(owner, data, icon, clickable, null);
    }

    public void SetSkill(
        EquippedSkillPanelUI owner,
        SkillMasterData data,
        Sprite icon,
        bool clickable,
        CharacterRuntimeData runtime,
        int equippedSkillIndex = -1,
        SkillInventoryPanelUI inventoryPanel = null)
    {
        ownerPanel = owner;
        skillInventoryPanel = inventoryPanel;
        skillData = data;
        runtimeData = runtime;
        this.equippedSkillIndex = equippedSkillIndex;
        canClick = clickable;
        isPointerOver = false;
        isSelected = false;
        isEquipAvailableHighlighted = false;

        if (scaleTarget == null)
            scaleTarget = GetComponent<RectTransform>();

        CaptureBaseScaleOnce();

        CaptureSortingCanvas();

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null || !hideEmptySlotIcon;
            iconImage.color = skillData != null
                ? SkillRarityUtility.GetSkillIconColor(skillData.SkillId)
                : Color.white;
            iconImage.raycastTarget = true;
        }

        if (button != null)
            button.interactable = canClick && skillData != null;

        ApplyBorderState();
        ApplyScale(true);
        ApplySortingState();
    }

    public void SetEmptySkillSlot(
        EquippedSkillPanelUI owner,
        CharacterRuntimeData runtime,
        int equippedSkillIndex,
        SkillInventoryPanelUI inventoryPanel)
    {
        ownerPanel = owner;
        skillInventoryPanel = inventoryPanel;
        skillData = null;
        runtimeData = runtime;
        this.equippedSkillIndex = equippedSkillIndex;
        canClick = true;
        isPointerOver = false;
        isSelected = false;
        isEquipAvailableHighlighted = false;

        if (scaleTarget == null)
            scaleTarget = GetComponent<RectTransform>();

        CaptureBaseScaleOnce();
        CaptureSortingCanvas();

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = !hideEmptySlotIcon;
            iconImage.color = Color.white;
            iconImage.raycastTarget = true;
        }

        if (button != null)
            button.interactable = true;

        ApplyBorderState();
        ApplyScale(true);
        ApplySortingState();
    }

    public void Clear()
    {
        ownerPanel = null;
        skillInventoryPanel = null;
        skillData = null;
        runtimeData = null;
        equippedSkillIndex = -1;
        canClick = true;
        isPointerOver = false;
        isSelected = false;
        isEquipAvailableHighlighted = false;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = !hideEmptySlotIcon;
            iconImage.color = Color.white;
            iconImage.raycastTarget = true;
        }

        if (button != null)
            button.interactable = false;

        ApplyBorderState();
        ApplyScale(true);
        ApplySortingState();
    }

    public bool IsEmptyEquipSlot =>
        canClick &&
        skillData == null &&
        runtimeData != null &&
        skillInventoryPanel != null &&
        SkillInventoryEquipService.IsFreeSkillSlotIndex(equippedSkillIndex);

    public void SetEquipAvailableHighlight(bool highlighted)
    {
        bool shouldHighlight = highlighted && IsEmptyEquipSlot;

        if (shouldHighlight && !isEquipAvailableHighlighted)
            equipAvailableHighlightStartTime = Time.unscaledTime;

        isEquipAvailableHighlighted = shouldHighlight;
        ApplyBorderState();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected && canClick && (skillData != null || skillInventoryPanel != null);
        ApplyBorderState();
        ApplyScale(false);
        ApplySortingState();
    }

    public void SetClickable(bool clickable)
    {
        canClick = clickable;

        if (!canClick)
            isSelected = false;

        if (button != null)
            button.interactable = canClick && skillData != null;

        ApplyBorderState();
        ApplyScale(false);
        ApplySortingState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (skillData == null)
            return;

        isPointerOver = true;
        ApplyBorderState();
        ApplySortingState();

        if (ownerPanel == null)
            return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        ownerPanel.ShowSkillTooltip(skillData, runtimeData, rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        ApplyBorderState();
        ApplySortingState();

        if (ownerPanel == null)
            return;

        ownerPanel.HideSkillTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (!canClick)
            return;

        PlayClickSfx();

        if (skillInventoryPanel != null &&
            runtimeData != null &&
            SkillInventoryEquipService.IsFreeSkillSlotIndex(equippedSkillIndex))
        {
            if (skillInventoryPanel.CheckSkillEditLocked())
                return;

            if (skillInventoryPanel.EquipSelectedInventorySkillToSlot(
                    runtimeData.CharacterId,
                    equippedSkillIndex))
            {
                return;
            }

            if (skillData != null &&
                skillInventoryPanel.UnequipSkill(runtimeData.CharacterId, equippedSkillIndex))
            {
                return;
            }

            skillInventoryPanel.SelectEquipSlot(runtimeData.CharacterId, equippedSkillIndex);
            ownerPanel?.SelectEquippedSkillSlot(this);
            return;
        }

        if (skillData == null)
            return;

        ownerPanel?.SelectEquippedSkillSlot(this);
    }

    private void ApplyBorderState()
    {
        if (!useHoverBorderColor || borderImage == null)
            return;

        bool canShowHoverOrSelectedEffect = canClick && (skillData != null || skillInventoryPanel != null);

        if (isEquipAvailableHighlighted && IsEmptyEquipSlot)
            borderImage.color = GetEquipAvailableBreathColor();
        else if (isSelected && canShowHoverOrSelectedEffect)
            borderImage.color = selectedBorderColor;
        else if (isPointerOver && canShowHoverOrSelectedEffect)
            borderImage.color = hoverBorderColor;
        else
            borderImage.color = normalBorderColor;
    }


    private Color GetEquipAvailableBreathColor()
    {
        float speed = Mathf.Max(0.01f, EquipAvailableBreathSpeed);
        float elapsed = Time.unscaledTime - equipAvailableHighlightStartTime;
        float t = (Mathf.Sin(elapsed * speed * Mathf.PI * 2f - Mathf.PI * 0.5f) + 1f) * 0.5f;

        // Skill slots alternate between available highlight colors.
        return Color.Lerp(equipAvailableBreathColor, equipAvailableColor, t);
    }

    private void ApplyScale(bool instant)
    {
        if (scaleTarget == null)
            return;

        CaptureBaseScaleOnce();

        float scaleMultiplier = useSelectedScale && isSelected && canClick && (skillData != null || skillInventoryPanel != null) ? selectedScale : 1f;
        Vector3 targetScale = baseScale * scaleMultiplier;

        if (instant)
        {
            scaleTarget.localScale = targetScale;
            return;
        }

        float t = 1f - Mathf.Exp(-scaleLerpSpeed * Time.unscaledDeltaTime);
        scaleTarget.localScale = Vector3.Lerp(scaleTarget.localScale, targetScale, t);
    }

    public void ClearSelectionState()
    {
        isPointerOver = false;
        isSelected = false;
        isEquipAvailableHighlighted = false;
        ApplyBorderState();
        ResetScale();
        ApplySortingState();
    }

    private void ResetScale()
    {
        CaptureBaseScaleOnce();

        if (scaleTarget != null)
            scaleTarget.localScale = baseScale;
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

        bool shouldBoost = (isPointerOver || isSelected) && canClick && (skillData != null || skillInventoryPanel != null);

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
