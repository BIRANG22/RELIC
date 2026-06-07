using Relic.Gameplay.Data;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerHUDSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Basic")]
    [SerializeField] private Image portraitImage;

    [Header("Command Selection Highlight")]
    [SerializeField] private GameObject commandSelectedHighlightObject;
    [SerializeField] private Image commandSelectedHighlightImage;
    [SerializeField] private bool autoFindCommandSelectedHighlight = true;
    [SerializeField] private float selectedScale = 1.06f;

    [Header("Click Selection")]
    [SerializeField] private bool enableHudClickSelect = true;
    [SerializeField] private bool ensureClickableRaycastGraphic = true;
    [SerializeField] private bool autoAddButtonClickHandler = true;

    private Button clickButton;
    private bool internalButtonClickInvoking;

    [Header("HP")]
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text hpValueText;

    [Header("Stamina")]
    [SerializeField] private Image staminaFill;
    [SerializeField] private TMP_Text staminaValueText;

    [Header("Unique Resource")]
    [SerializeField] private GameObject[] resourceSlots;
    [SerializeField] private Image[] resourceFillImages;

    [Header("Shield")]
    [SerializeField] private Image shieldFill;
    [SerializeField] private TMP_Text shieldValueText;

    [Header("Status Effects")]
    [SerializeField] private Transform statusIconRoot;
    [SerializeField] private StatusEffectIcon statusIconPrefab;
    [SerializeField] private float statusEffectIconSpacing = 4f;

    private CharacterRuntimeData boundRuntime;
    private CharacterMasterData boundMaster;

    private readonly List<StatusEffectIcon> spawnedStatusIcons = new();
    private Vector3 defaultLocalScale;

    public event Action<CharacterRuntimeData, RectTransform> OnClicked;

    private void Awake()
    {
        defaultLocalScale = transform.localScale;

        SetupHudClickSelection();
        ResolveCommandSelectedHighlightReferences();
        ApplyStatusEffectParentLayout();
        ApplyCommandSelectedVisual(false);
    }

    private void OnEnable()
    {
        SetupHudClickSelection();

        if (boundRuntime != null)
            Refresh();
    }

    public void Bind(CharacterRuntimeData runtimeData)
    {
        boundRuntime = runtimeData;
        boundMaster = null;

        ResolveCommandSelectedHighlightReferences();
        ApplyStatusEffectParentLayout();

        if (boundRuntime == null)
        {
            Clear();
            return;
        }

        if (DataManager.Instance != null)
            DataManager.Instance.CharacterDatabase.TryGet(boundRuntime.CharacterId, out boundMaster);

        Refresh();
    }

    public void Refresh()
    {
        if (boundRuntime == null)
        {
            Clear();
            return;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = GetCharacterIcon(boundRuntime.CharacterId);
            portraitImage.enabled = portraitImage.sprite != null;
        }

        int maxHp = boundMaster != null ? boundMaster.MaxHealth : Mathf.Max(1, boundRuntime.CurrentHealth);
        int maxStamina = boundMaster != null ? boundMaster.MaxStamina : Mathf.Max(1, boundRuntime.CurrentStamina);
        int maxResource = boundMaster != null ? boundMaster.MaxResource : Mathf.Max(1, boundRuntime.CurrentResource);

        RefreshBar(hpFill, hpValueText, boundRuntime.CurrentHealth, maxHp);
        RefreshBar(staminaFill, staminaValueText, boundRuntime.PreviewStamina, maxStamina);
        RefreshUniqueResource(boundRuntime.CurrentResource, maxResource);
        RefreshShield(boundRuntime.CurrentShield, maxHp);
        RefreshStatusEffects(boundRuntime.StatusEffects);
    }

    private void RefreshBar(Image fill, TMP_Text valueText, int current, int max)
    {
        current = Mathf.Clamp(current, 0, max);

        if (fill != null)
            fill.fillAmount = max > 0 ? (float)current / max : 0f;

        if (valueText != null)
            valueText.text = current.ToString();
    }

    private void RefreshUniqueResource(int currentResource, int maxResource)
    {
        currentResource = Mathf.Clamp(currentResource, 0, maxResource);

        int slotCount = resourceSlots != null ? resourceSlots.Length : 0;

        for (int i = 0; i < slotCount; i++)
        {
            bool useSlot = i < maxResource;

            if (resourceSlots[i] != null)
                resourceSlots[i].SetActive(useSlot);

            if (resourceFillImages == null || i >= resourceFillImages.Length)
                continue;

            Image fillImage = resourceFillImages[i];

            if (fillImage == null)
                continue;

            bool filled = useSlot && i < currentResource;
            fillImage.gameObject.SetActive(filled);
        }
    }

    private void RefreshShield(int shield, int maxHp)
    {
        shield = Mathf.Max(0, shield);

        if (shieldFill != null)
        {
            shieldFill.gameObject.SetActive(shield > 0);
            shieldFill.fillAmount = maxHp > 0 ? (float)shield / maxHp : 0f;
        }

        if (shieldValueText != null)
        {
            shieldValueText.gameObject.SetActive(shield > 0);
            shieldValueText.text = shield.ToString();
        }
    }

    private void RefreshStatusEffects(List<StatusEffectRuntimeData> statusEffects)
    {
        ClearStatusEffectIcons();
        ApplyStatusEffectParentLayout();

        if (statusIconRoot == null || statusIconPrefab == null)
            return;

        if (statusEffects == null)
            return;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            if (statusEffects[i] == null)
                continue;

            StatusEffectIcon icon = Instantiate(statusIconPrefab, statusIconRoot);
            icon.Set(statusEffects[i]);
            spawnedStatusIcons.Add(icon);
        }
    }

    private void ClearStatusEffectIcons()
    {
        for (int i = spawnedStatusIcons.Count - 1; i >= 0; i--)
        {
            if (spawnedStatusIcons[i] == null)
                continue;

            Destroy(spawnedStatusIcons[i].gameObject);
        }

        spawnedStatusIcons.Clear();

        if (statusIconRoot == null)
            return;

        for (int i = statusIconRoot.childCount - 1; i >= 0; i--)
            Destroy(statusIconRoot.GetChild(i).gameObject);
    }

    private Sprite GetCharacterIcon(string characterId)
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase.TryGetIcon(characterId, out Sprite icon))
            return icon;

        return null;
    }

    public void SetCommandSelected(bool selected)
    {
        ApplyCommandSelectedVisual(selected);
    }

    private void ApplyCommandSelectedVisual(bool selected)
    {
        ResolveCommandSelectedHighlightReferences();

        if (commandSelectedHighlightObject != null)
            commandSelectedHighlightObject.SetActive(selected);

        if (commandSelectedHighlightImage != null)
        {
            commandSelectedHighlightImage.enabled = selected;
            commandSelectedHighlightImage.gameObject.SetActive(selected);
        }

        transform.localScale = selected
            ? defaultLocalScale * Mathf.Max(1f, selectedScale)
            : defaultLocalScale;
    }

    private void ResolveCommandSelectedHighlightReferences()
    {
        if (!autoFindCommandSelectedHighlight)
            return;

        if (commandSelectedHighlightObject == null)
            commandSelectedHighlightObject = FindChildGameObjectByName("CommandSelectedHighlight");

        if (commandSelectedHighlightObject == null)
            commandSelectedHighlightObject = FindChildGameObjectByName("SelectedHighlight");

        if (commandSelectedHighlightObject == null)
            commandSelectedHighlightObject = FindChildGameObjectByName("SelectHighlight");

        if (commandSelectedHighlightObject == null)
            commandSelectedHighlightObject = FindChildGameObjectByName("ActiveHighlight");

        if (commandSelectedHighlightImage == null && commandSelectedHighlightObject != null)
            commandSelectedHighlightImage = commandSelectedHighlightObject.GetComponent<Image>();
    }

    private void ApplyStatusEffectParentLayout()
    {
        if (statusIconRoot == null)
            return;

        HorizontalLayoutGroup layout = statusIconRoot.GetComponent<HorizontalLayoutGroup>();

        if (layout != null)
            layout.spacing = statusEffectIconSpacing;
    }

    private GameObject FindChildGameObjectByName(string targetName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == null || child == transform)
                continue;

            if (child.gameObject.name == targetName)
                return child.gameObject;
        }

        return null;
    }

    private void Clear()
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }

        RefreshBar(hpFill, hpValueText, 0, 1);
        RefreshBar(staminaFill, staminaValueText, 0, 1);
        RefreshUniqueResource(0, 0);
        RefreshShield(0, 1);
        ClearStatusEffectIcons();
        SetCommandSelected(false);
    }

    private void SetupHudClickSelection()
    {
        if (!enableHudClickSelect)
            return;

        if (ensureClickableRaycastGraphic)
            EnsureClickableGraphic();

        if (autoAddButtonClickHandler)
            EnsureButtonClickHandler();
    }

    private void EnsureClickableGraphic()
    {
        Graphic graphic = GetComponent<Graphic>();

        if (graphic == null)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            return;
        }

        graphic.raycastTarget = true;
    }

    private void EnsureButtonClickHandler()
    {
        if (clickButton == null)
            clickButton = GetComponent<Button>();

        if (clickButton == null)
            clickButton = gameObject.AddComponent<Button>();

        clickButton.transition = Selectable.Transition.None;
        clickButton.onClick.RemoveListener(HandleInternalButtonClick);
        clickButton.onClick.AddListener(HandleInternalButtonClick);
    }

    private void HandleInternalButtonClick()
    {
        if (!enableHudClickSelect)
            return;

        internalButtonClickInvoking = true;
        InvokeHudClicked();
        internalButtonClickInvoking = false;
    }

    private void InvokeHudClicked()
    {
        if (boundRuntime == null)
            return;

        RectTransform rect = GetComponent<RectTransform>();
        OnClicked?.Invoke(boundRuntime, rect);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!enableHudClickSelect)
            return;

        if (internalButtonClickInvoking)
            return;

        InvokeHudClicked();
    }
}