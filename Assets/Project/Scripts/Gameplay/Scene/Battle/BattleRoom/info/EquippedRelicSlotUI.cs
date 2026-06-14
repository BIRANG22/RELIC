using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class EquippedRelicSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Info")]
    [SerializeField] private int partySlotIndex;
    [SerializeField] private int relicSlotIndex;

    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Graphic clickTargetGraphic;

    [Header("Hover Border")]
    [SerializeField] private Image borderImage;
    [SerializeField] private bool useHoverBorderColor = true;
    [SerializeField] private Color normalBorderColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color hoverBorderColor = new Color(1f, 0.86f, 0.35f, 1f);
    [SerializeField] private Color selectedBorderColor = new Color(1f, 0.58f, 0.12f, 1f);

    [Header("Selected Effect")]
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private bool useSelectedScale = true;
    [SerializeField] private float selectedScale = 1.06f;
    [SerializeField] private float scaleLerpSpeed = 14f;
    [SerializeField] private bool boostSortingOnHoverOrSelected = true;
    [SerializeField] private int sortingOrderBoost = 2000;

    [Header("Sound")]
    [SerializeField] private bool playClickSfx = true;
    [SerializeField] private SfxType clickSfxType = SfxType.NormalButtonClick;

    private RelicEquipPanelUI owner;
    private string currentRelicId;
    private RectTransform rectTransform;
    private Vector3 baseScale = Vector3.one;
    private bool hasCapturedBaseScale;
    private bool isPointerOver;
    private bool isSelected;
    private Canvas sortingCanvas;
    private bool hadSortingCanvas;
    private bool originalOverrideSorting;
    private int originalSortingOrder;

    public int PartySlotIndex => partySlotIndex;
    public int RelicSlotIndex => relicSlotIndex;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (scaleTarget == null)
            scaleTarget = rectTransform;

        CaptureBaseScaleOnce();

        if (iconImage == null)
            iconImage = FindIconImageInChildren();

        if (clickTargetGraphic == null)
            clickTargetGraphic = GetComponent<Graphic>();

        if (clickTargetGraphic == null && iconImage != null)
            clickTargetGraphic = iconImage;

        if (clickTargetGraphic != null)
            clickTargetGraphic.raycastTarget = true;

        CaptureSortingCanvas();
        ApplyBorderState();
        ApplyScale(true);
        ApplySortingState();
    }

    private void Update()
    {
        ApplyScale(false);
    }

    private void OnDisable()
    {
        isPointerOver = false;
        isSelected = false;
        ApplyBorderState();
        ResetScale();
        ApplySortingState();
        owner?.HideRelicTooltip();
    }

    public void Init(RelicEquipPanelUI owner)
    {
        this.owner = owner;
    }

    public void Refresh()
    {
        string characterId = GetCharacterId();

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Clear();
            return;
        }

        if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                characterId,
                out CharacterRuntimeData character))
        {
            Clear();
            return;
        }

        RelicEquipService.EnsureRelicSlots(character);

        string relicId = character.EquippedRelicIds[relicSlotIndex];
        SetIcon(relicId);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected && !string.IsNullOrWhiteSpace(GetCharacterId());
        ApplyBorderState();
        ApplyScale(false);
        ApplySortingState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        string characterId = GetCharacterId();

        if (string.IsNullOrWhiteSpace(characterId))
            return;

        string equippedRelicId = GetEquippedRelicId(characterId);

        if (!string.IsNullOrWhiteSpace(equippedRelicId))
        {
            owner?.UnequipRelic(characterId, relicSlotIndex);

            Debug.Log(
                $"[EquippedRelicSlotUI] ¿Øπ∞ «ÿ¡¶ / Character:{characterId} / Slot:{relicSlotIndex + 1} / Relic:{equippedRelicId}"
            );

            return;
        }

        owner?.SelectEquipSlot(characterId, relicSlotIndex);

        Debug.Log(
            $"[EquippedRelicSlotUI] ∫Û ΩΩ∑‘ º±≈√ / Character:{characterId} / Slot:{relicSlotIndex + 1}"
        );
    }

    private string GetEquippedRelicId(string characterId)
    {
        if (DataManager.Instance == null)
            return null;

        if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                characterId,
                out CharacterRuntimeData character))
            return null;

        RelicEquipService.EnsureRelicSlots(character);

        if (relicSlotIndex < 0 || relicSlotIndex >= character.EquippedRelicIds.Length)
            return null;

        return character.EquippedRelicIds[relicSlotIndex];
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        ApplyBorderState();
        ApplySortingState();

        if (owner == null || string.IsNullOrWhiteSpace(currentRelicId))
            return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        owner.ShowRelicTooltip(currentRelicId, rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        ApplyBorderState();
        ApplySortingState();
        owner?.HideRelicTooltip();
    }


    private Image FindIconImageInChildren()
    {
        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null)
                continue;

            string objectName = image.gameObject.name.ToLowerInvariant();
            if (objectName.Contains("icon"))
                return image;
        }

        return null;
    }

    private string GetCharacterId()
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.PartyRuntimeStore == null)
            return null;

        return DataManager.Instance.PartyRuntimeStore.GetCharacterId(partySlotIndex);
    }

    private void SetIcon(string relicId)
    {
        currentRelicId = relicId;

        if (clickTargetGraphic == null)
            clickTargetGraphic = GetComponent<Graphic>();

        if (clickTargetGraphic != null)
            clickTargetGraphic.raycastTarget = true;

        if (iconImage == null)
            return;

        iconImage.raycastTarget = false;

        if (!string.IsNullOrWhiteSpace(relicId) &&
            DataManager.Instance.RelicIconDatabase != null &&
            DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out Sprite icon))
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
        else
        {
            Clear();
        }

        ApplyBorderState();
        ApplyScale(false);
        ApplySortingState();
    }

    public void Clear()
    {
        currentRelicId = null;
        isPointerOver = false;
        isSelected = false;

        if (clickTargetGraphic == null)
            clickTargetGraphic = GetComponent<Graphic>();

        if (clickTargetGraphic != null)
            clickTargetGraphic.raycastTarget = true;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            iconImage.raycastTarget = false;
        }

        if (clickTargetGraphic != null)
        {
            clickTargetGraphic.enabled = true;
            clickTargetGraphic.raycastTarget = true;
        }

        ApplyBorderState();
        ApplyScale(true);
        ApplySortingState();
    }

    private void ApplyBorderState()
    {
        if (!useHoverBorderColor || borderImage == null)
            return;

        bool hasCharacter = !string.IsNullOrWhiteSpace(GetCharacterId());

        if (isSelected && hasCharacter)
            borderImage.color = selectedBorderColor;
        else if (isPointerOver && hasCharacter)
            borderImage.color = hoverBorderColor;
        else
            borderImage.color = normalBorderColor;
    }

    private void ApplyScale(bool instant)
    {
        if (scaleTarget == null)
            return;

        CaptureBaseScaleOnce();

        float scaleMultiplier = useSelectedScale && isSelected && !string.IsNullOrWhiteSpace(GetCharacterId()) ? selectedScale : 1f;
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

        bool shouldBoost = (isPointerOver || isSelected) && !string.IsNullOrWhiteSpace(GetCharacterId());

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

        AudioManager.Instance.PlaySfx(clickSfxType);
    }
}
