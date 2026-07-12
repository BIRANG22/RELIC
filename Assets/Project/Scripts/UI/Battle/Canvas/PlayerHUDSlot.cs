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

    [Header("Keyboard Number")]
    [SerializeField] private GameObject numberObject;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private bool autoFindNumberObject = true;
    [SerializeField] private string numberObjectName = "Number";

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

    [Header("Cost")]
    [SerializeField] private TMP_Text costValueText;
    [SerializeField] private bool showMaxCost = true;

    [Header("Unique Resource")]
    [SerializeField] private Transform resourceSlotRoot;
    [SerializeField] private GameObject[] resourceSlots;
    [SerializeField] private Image[] resourceFillImages;
    [SerializeField] private bool autoFindResourceSlots = true;
    [SerializeField] private string resourceSlotRootName = "ResourceSlotGroup";
    [SerializeField] private string resourceSlotNamePrefix = "ResourceSlot_";
    [SerializeField] private string resourceFillImageName = "FillImage";
    [SerializeField] private int maxResourceSlotCount = 5;

    [Header("Armor")]
    [SerializeField] private Image shieldFill;
    [SerializeField] private TMP_Text shieldValueText;

    [Header("Status Effects")]
    [SerializeField] private Transform statusIconRoot;
    [SerializeField] private StatusEffectIcon statusIconPrefab;
    [SerializeField] private float statusEffectIconSpacing = 4f;

    private CharacterRuntimeData boundRuntime;
    private CharacterMasterData boundMaster;
    private int keyboardNumber;

    private readonly List<StatusEffectIcon> spawnedStatusIcons = new();
    private Vector3 defaultLocalScale;

    public CharacterRuntimeData BoundRuntime => boundRuntime;

    public event Action<CharacterRuntimeData, RectTransform> OnClicked;

    private void Awake()
    {
        defaultLocalScale = transform.localScale;

        SetupHudClickSelection();
        ResolveKeyboardNumberReferences();
        ResolveCommandSelectedHighlightReferences();
        ResolveResourceSlotReferences();
        ApplyStatusEffectParentLayout();
        ApplyKeyboardNumberVisual();
        ApplyCommandSelectedVisual(false);
    }

    private void OnEnable()
    {
        SetupHudClickSelection();
        ResolveKeyboardNumberReferences();
        ResolveResourceSlotReferences();
        ApplyKeyboardNumberVisual();

        if (boundRuntime != null)
            Refresh();
    }

    public void Bind(CharacterRuntimeData runtimeData)
    {
        boundRuntime = runtimeData;
        boundMaster = null;

        ResolveCommandSelectedHighlightReferences();
        ResolveResourceSlotReferences();
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

    public void SetKeyboardNumber(int number)
    {
        keyboardNumber = Mathf.Max(0, number);
        ApplyKeyboardNumberVisual();
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

        int maxHP = boundRuntime.MaxHP > 0
            ? boundRuntime.MaxHP
            : boundMaster != null ? boundMaster.MaxHP : Mathf.Max(1, boundRuntime.CurrentHP);
        int maxCost = boundRuntime.MaxCost > 0
            ? Mathf.Max(boundRuntime.MaxCost, boundRuntime.PreviewCost)
            : boundMaster != null ? boundMaster.MaxCost : Mathf.Max(1, boundRuntime.CurrentCost);
        int maxResource = boundMaster != null ? boundMaster.MaxResource : Mathf.Max(1, boundRuntime.CurrentResource);

        RefreshBar(hpFill, hpValueText, boundRuntime.PreviewHP, maxHP);
        RefreshCost(boundRuntime.PreviewCost, maxCost);
        RefreshUniqueResource(boundRuntime.PreviewResource, maxResource);
        RefreshArmor(boundRuntime.CurrentShield);
        RefreshStatusEffects(boundRuntime.StatusEffects);
        ApplyKeyboardNumberVisual();
    }

    private void RefreshBar(Image fill, TMP_Text valueText, int current, int max)
    {
        current = Mathf.Clamp(current, 0, max);

        if (fill != null)
            fill.fillAmount = max > 0 ? (float)current / max : 0f;

        if (valueText != null)
            valueText.text = current.ToString();
    }

    private void RefreshCost(int currentCost, int maxCost)
    {
        maxCost = Mathf.Max(0, maxCost);
        currentCost = Mathf.Clamp(currentCost, 0, maxCost);

        if (costValueText == null)
            return;

        costValueText.text = showMaxCost
            ? $"{currentCost} / {maxCost}"
            : currentCost.ToString();
    }

    private void RefreshUniqueResource(int currentResource, int maxResource)
    {
        ResolveResourceSlotReferences();

        maxResource = Mathf.Clamp(maxResource, 0, Mathf.Max(0, maxResourceSlotCount));
        currentResource = Mathf.Clamp(currentResource, 0, maxResource);

        int slotCount = resourceSlots != null ? resourceSlots.Length : 0;

        for (int i = 0; i < slotCount; i++)
        {
            bool useSlot = i < maxResource;
            bool filled = useSlot && i < currentResource;

            // ResourceSlot itself is the empty frame/background.
            // Keep the slot visible while this character can own that resource slot.
            // Only the FillImage is hidden when the resource is empty.
            if (resourceSlots[i] != null)
                resourceSlots[i].SetActive(useSlot);

            if (resourceFillImages == null || i >= resourceFillImages.Length)
                continue;

            Image fillImage = resourceFillImages[i];

            if (fillImage == null)
                continue;

            fillImage.enabled = filled;
            fillImage.gameObject.SetActive(filled);
        }
    }

    private void RefreshArmor(int armor)
    {
        armor = Mathf.Max(0, armor);

        if (shieldFill != null)
            shieldFill.gameObject.SetActive(armor > 0);

        if (shieldValueText != null)
        {
            shieldValueText.gameObject.SetActive(armor > 0);
            shieldValueText.text = "+" + armor.ToString();
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

    public void SetBaseScale(Vector3 baseScale)
    {
        defaultLocalScale = baseScale;
        transform.localScale = baseScale;
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

    private void ApplyKeyboardNumberVisual()
    {
        ResolveKeyboardNumberReferences();

        bool visible = keyboardNumber > 0;

        if (numberObject != null)
            numberObject.SetActive(visible);

        if (numberText != null)
        {
            numberText.gameObject.SetActive(visible);
            numberText.text = visible ? keyboardNumber.ToString() : string.Empty;
        }
    }

    private void ResolveKeyboardNumberReferences()
    {
        if (!autoFindNumberObject)
            return;

        if (numberObject == null)
            numberObject = FindChildGameObjectByName(numberObjectName);

        if (numberText == null && numberObject != null)
            numberText = numberObject.GetComponentInChildren<TMP_Text>(true);
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

    private void ResolveResourceSlotReferences()
    {
        if (!autoFindResourceSlots)
            return;

        if (resourceSlotRoot == null)
        {
            GameObject rootObject = FindChildGameObjectByName(resourceSlotRootName);

            if (rootObject != null)
                resourceSlotRoot = rootObject.transform;
        }

        if (resourceSlotRoot == null)
            return;

        int slotCount = Mathf.Max(0, maxResourceSlotCount);

        if (resourceSlots == null || resourceSlots.Length != slotCount)
            resourceSlots = new GameObject[slotCount];

        if (resourceFillImages == null || resourceFillImages.Length != slotCount)
            resourceFillImages = new Image[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            if (resourceSlots[i] != null && resourceFillImages[i] != null)
                continue;

            string slotName = resourceSlotNamePrefix + i;
            Transform slotTransform = FindDirectOrNestedChild(resourceSlotRoot, slotName);

            if (slotTransform == null)
                continue;

            resourceSlots[i] = slotTransform.gameObject;

            if (resourceFillImages[i] == null)
            {
                Transform fillTransform = FindDirectOrNestedChild(slotTransform, resourceFillImageName);

                if (fillTransform != null)
                    resourceFillImages[i] = fillTransform.GetComponent<Image>();
            }
        }
    }


    private Transform FindDirectOrNestedChild(Transform root, string targetName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child == null)
                continue;

            if (child.name == targetName)
                return child;

            Transform nested = FindDirectOrNestedChild(child, targetName);

            if (nested != null)
                return nested;
        }

        return null;
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
        RefreshCost(0, 0);
        RefreshUniqueResource(0, 0);
        RefreshArmor(0);
        ClearStatusEffectIcons();
        SetKeyboardNumber(0);
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
        if (IsMenuPanelOpen())
            return;

        if (!enableHudClickSelect)
            return;

        internalButtonClickInvoking = true;
        InvokeHudClicked();
        internalButtonClickInvoking = false;
    }

    private void InvokeHudClicked()
    {
        if (IsMenuPanelOpen())
            return;

        if (boundRuntime == null)
            return;

        RectTransform rect = GetComponent<RectTransform>();
        OnClicked?.Invoke(boundRuntime, rect);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsMenuPanelOpen())
            return;

        if (!enableHudClickSelect)
            return;

        if (internalButtonClickInvoking)
            return;

        InvokeHudClicked();
    }

    private static bool IsMenuPanelOpen()
    {
        GameObject menuPanel = GameObject.Find("MenuPanel");
        return menuPanel != null && menuPanel.activeInHierarchy;
    }
}

