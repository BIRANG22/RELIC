using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class SkillInventoryPanelUI : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private Transform inventoryContent;
    [SerializeField] private SkillInventoryIconUI inventorySlotPrefab;
    [SerializeField] private bool useVerticalInventoryLayout = true;
    [SerializeField] private float inventorySlotSpacing = 8f;
    [SerializeField] private Vector2 fallbackSlotSize = new(64f, 64f);

    [Header("Tooltip")]
    [SerializeField] private EquippedSkillPanelUI tooltipPanelOwner;
    [SerializeField] private InventoryRuntimeContextProvider runtimeContextProvider;

    [Header("Battle Room Lock")]
    [SerializeField] private bool lockEditInBattleRoom = true;
    [SerializeField] private string battleRoomLockMessage = "\uC804\uD22C \uC911\uC5D0\uB294 \uC2A4\uD0AC\uC744 \uBCC0\uACBD\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.";

    private string selectedCharacterId;
    private int selectedEquippedSkillIndex = -1;
    private SkillInventoryIconUI selectedInventorySkillIcon;

    private void Awake()
    {
        ResolveContentIfNeeded();
        ResolveTooltipPanelOwner();
        EnsureInventoryVerticalLayout();
    }

    private void OnEnable()
    {
        ResolveContentIfNeeded();
        ResolveTooltipPanelOwner();
        EnsureInventoryVerticalLayout();
        ResetSelectionState();
        Refresh();
    }

    private void OnDisable()
    {
        ResetSelectionState();
        HideSkillTooltip();
    }

    public void SelectEquipSlot(string characterId, int equippedSkillIndex)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        InventoryPanelSelectionResetter.ResetAllSelectionsExcept(this);

        if (CheckSkillEditLocked())
            return;

        selectedCharacterId = characterId;
        selectedEquippedSkillIndex = equippedSkillIndex;

        if (selectedInventorySkillIcon != null)
            EquipSelectedInventorySkillToSlot(characterId, equippedSkillIndex);
    }

    public void SelectInventorySkillIcon(SkillInventoryIconUI selectedIcon)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        InventoryPanelSelectionResetter.ResetAllSelectionsExcept(this);

        selectedInventorySkillIcon = selectedIcon;
        UpdateInventorySelectionVisuals();
        UpdateEmptyEquipSlotHighlights();
    }

    public void SelectSkill(string skillId)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (string.IsNullOrWhiteSpace(skillId))
            return;

        if (string.IsNullOrWhiteSpace(selectedCharacterId) ||
            !SkillInventoryEquipService.IsFreeSkillSlotIndex(selectedEquippedSkillIndex))
        {
            return;
        }

        EquipSkill(selectedCharacterId, selectedEquippedSkillIndex, skillId);
    }

    public bool EquipSelectedInventorySkillToSlot(string characterId, int equippedSkillIndex)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return false;

        if (selectedInventorySkillIcon == null)
            return false;

        string skillId = selectedInventorySkillIcon.SkillId;

        if (string.IsNullOrWhiteSpace(skillId))
            return false;

        selectedCharacterId = characterId;
        selectedEquippedSkillIndex = equippedSkillIndex;

        return EquipSkill(characterId, equippedSkillIndex, skillId);
    }

    public bool UnequipSkill(string characterId, int equippedSkillIndex)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return false;

        if (CheckSkillEditLocked())
            return false;

        if (DataManager.Instance == null)
            return false;

        IInventoryRuntimeContext context = ResolveRuntimeContext();
        if (context == null)
            return false;

        SkillInventoryEquipService service = CreateEquipService(context.SkillInventoryIds);

        if (!service.UnequipSkillFromSlot(characterId, equippedSkillIndex))
            return false;

        ResetSelectionState();
        RefreshAll();
        EquippedSkillPanelUI.RefreshAll();
        return true;
    }

    public void Refresh()
    {
        RefreshInventory();
    }

    public void ResetSelectionState()
    {
        selectedCharacterId = null;
        selectedEquippedSkillIndex = -1;
        selectedInventorySkillIcon = null;
        UpdateInventorySelectionVisuals();
        UpdateEmptyEquipSlotHighlights();
    }

    private void UpdateEmptyEquipSlotHighlights()
    {
        bool shouldHighlight = selectedInventorySkillIcon != null;

        EquippedSkillSlotUI[] slots = Object.FindObjectsByType<EquippedSkillSlotUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].SetEquipAvailableHighlight(shouldHighlight);
        }
    }

    public void ShowSkillTooltip(string skillId, RectTransform hoveredSlotRect)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        ResolveTooltipPanelOwner();

        if (tooltipPanelOwner == null ||
            string.IsNullOrWhiteSpace(skillId) ||
            DataManager.Instance == null ||
            DataManager.Instance.SkillDatabase == null)
        {
            return;
        }

        if (!DataManager.Instance.SkillDatabase.TryGet(skillId, out SkillMasterData skillData))
            return;

        tooltipPanelOwner.ShowSkillTooltip(skillData, hoveredSlotRect);
    }

    public void HideSkillTooltip()
    {
        if (tooltipPanelOwner == null)
            return;

        tooltipPanelOwner.HideSkillTooltip();
    }

    public static void RefreshAll()
    {
        SkillInventoryPanelUI[] panels = Object.FindObjectsByType<SkillInventoryPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (panels == null || panels.Length == 0)
        {
            SkillInventoryPanelUI panel = EnsureScenePanel();

            if (panel != null)
                panel.Refresh();

            return;
        }

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
                panels[i].Refresh();
        }
    }

    private bool EquipSkill(string characterId, int equippedSkillIndex, string skillId)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return false;

        if (CheckSkillEditLocked())
            return false;

        if (DataManager.Instance == null)
            return false;

        IInventoryRuntimeContext context = ResolveRuntimeContext();
        if (context == null)
            return false;

        SkillInventoryEquipService service = CreateEquipService(context.SkillInventoryIds);

        if (!service.EquipInventorySkillToSlot(characterId, equippedSkillIndex, skillId))
            return false;

        ResetSelectionState();
        RefreshAll();
        EquippedSkillPanelUI.RefreshAll();
        return true;
    }

    private SkillInventoryEquipService CreateEquipService(IList<string> skillInventoryIds)
    {
        return new SkillInventoryEquipService(
            DataManager.Instance.CharacterRuntimeStore,
            skillInventoryIds,
            ResolveSkill);
    }

    private SkillMasterData ResolveSkill(string skillId)
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.SkillDatabase == null ||
            string.IsNullOrWhiteSpace(skillId))
        {
            return null;
        }

        DataManager.Instance.SkillDatabase.TryGet(skillId.Trim(), out SkillMasterData skill);
        return skill;
    }

    public bool IsSkillEditLocked()
    {
        if (!lockEditInBattleRoom)
            return false;

        BattleRoomLoader battleRoomLoader =
            Object.FindFirstObjectByType<BattleRoomLoader>(FindObjectsInactive.Include);

        return battleRoomLoader != null && battleRoomLoader.gameObject.activeInHierarchy;
    }

    public bool CheckSkillEditLocked()
    {
        if (!IsSkillEditLocked())
            return false;

        BattleWarningUI.ShowMessage(battleRoomLockMessage);
        ResetSelectionState();
        return true;
    }

    private void RefreshInventory()
    {
        ResolveContentIfNeeded();

        if (inventoryContent == null)
            return;

        EnsureInventoryVerticalLayout();
        selectedInventorySkillIcon = null;
        UpdateEmptyEquipSlotHighlights();
        ClearInventoryIcons();

        if (DataManager.Instance == null)
            return;

        IInventoryRuntimeContext context = ResolveRuntimeContext();
        if (context == null)
            return;

        for (int i = 0; i < context.SkillInventoryIds.Count; i++)
        {
            string skillId = context.SkillInventoryIds[i];

            if (string.IsNullOrWhiteSpace(skillId))
                continue;

            SkillInventoryIconUI icon = CreateIcon(inventoryContent);
            icon.Setup(skillId.Trim(), this);
            EnsureInventoryIconLayoutElement(icon);
        }

        RebuildInventoryLayout();
        ScheduleRebuildInventoryLayout();
    }

    private SkillInventoryIconUI CreateIcon(Transform parent)
    {
        if (inventorySlotPrefab != null)
            return Instantiate(inventorySlotPrefab, parent);

        GameObject iconObject = new("SkillInventoryIcon");
        iconObject.transform.SetParent(parent, false);

        RectTransform rect = iconObject.AddComponent<RectTransform>();
        rect.sizeDelta = fallbackSlotSize;

        Image image = iconObject.AddComponent<Image>();
        image.preserveAspect = true;

        Button button = iconObject.AddComponent<Button>();
        button.targetGraphic = image;

        return iconObject.AddComponent<SkillInventoryIconUI>();
    }

    private void ClearInventoryIcons()
    {
        if (inventoryContent == null)
            return;

        for (int i = inventoryContent.childCount - 1; i >= 0; i--)
        {
            Transform child = inventoryContent.GetChild(i);

            if (child == null)
                continue;

            if (child.GetComponent<SkillInventoryIconUI>() == null)
                continue;

            child.gameObject.SetActive(false);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    private void EnsureInventoryVerticalLayout()
    {
        if (!useVerticalInventoryLayout || inventoryContent == null)
            return;

        GameObject contentObject = inventoryContent.gameObject;

        GridLayoutGroup grid = contentObject.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            ConfigureInventoryGridLayout(grid);
            return;
        }

        HorizontalLayoutGroup horizontal = contentObject.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null)
        {
            horizontal.enabled = true;
            return;
        }

        VerticalLayoutGroup vertical = contentObject.GetComponent<VerticalLayoutGroup>();
        if (vertical == null)
            vertical = contentObject.AddComponent<VerticalLayoutGroup>();

        vertical.enabled = true;
        vertical.childAlignment = TextAnchor.UpperCenter;
        vertical.spacing = inventorySlotSpacing;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = false;
        vertical.childForceExpandHeight = false;
        vertical.childScaleWidth = false;
        vertical.childScaleHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = contentObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void ConfigureInventoryGridLayout(GridLayoutGroup grid)
    {
        if (grid == null)
            return;

        grid.enabled = true;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 1;
        grid.spacing = new Vector2(0f, inventorySlotSpacing);

        if (grid.cellSize.x <= 0f || grid.cellSize.y <= 0f)
            grid.cellSize = fallbackSlotSize;
    }

    private void EnsureInventoryIconLayoutElement(SkillInventoryIconUI icon)
    {
        if (icon == null)
            return;

        RectTransform rect = icon.GetComponent<RectTransform>();
        LayoutElement layoutElement = icon.GetComponent<LayoutElement>();

        if (layoutElement == null)
            layoutElement = icon.gameObject.AddComponent<LayoutElement>();

        Vector2 size = fallbackSlotSize;

        if (rect != null)
        {
            if (rect.sizeDelta.x > 0f)
                size.x = rect.sizeDelta.x;

            if (rect.sizeDelta.y > 0f)
                size.y = rect.sizeDelta.y;
        }

        layoutElement.ignoreLayout = false;
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;
        layoutElement.minWidth = size.x;
        layoutElement.minHeight = size.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }

    private void RebuildInventoryLayout()
    {
        if (inventoryContent is RectTransform rect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private void ScheduleRebuildInventoryLayout()
    {
        if (!isActiveAndEnabled)
            return;

        StopCoroutine(nameof(RebuildInventoryLayoutNextFrame));
        StartCoroutine(nameof(RebuildInventoryLayoutNextFrame));
    }

    private IEnumerator RebuildInventoryLayoutNextFrame()
    {
        yield return null;
        RebuildInventoryLayout();
        Canvas.ForceUpdateCanvases();
        RebuildInventoryLayout();
    }

    private void UpdateInventorySelectionVisuals()
    {
        if (inventoryContent == null)
            return;

        for (int i = inventoryContent.childCount - 1; i >= 0; i--)
        {
            SkillInventoryIconUI icon = inventoryContent.GetChild(i).GetComponent<SkillInventoryIconUI>();
            if (icon != null)
                icon.SetSelected(icon == selectedInventorySkillIcon);
        }
    }

    private void ResolveContentIfNeeded()
    {
        if (inventoryContent != null)
            return;

        Transform content = transform.Find("Content");

        if (content == null)
            content = FindChildByName(transform, "Content");

        if (content == null)
        {
            GameObject contentObject = new("Content");
            contentObject.transform.SetParent(transform, false);
            RectTransform rect = contentObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            content = contentObject.transform;
        }

        inventoryContent = content;
    }

    private Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child != null && child != root && child.name == childName)
                return child;
        }

        return null;
    }

    private void ResolveTooltipPanelOwner()
    {
        if (tooltipPanelOwner != null)
            return;

        tooltipPanelOwner = GetComponentInParent<EquippedSkillPanelUI>();

        if (tooltipPanelOwner == null)
            tooltipPanelOwner = FindFirstObjectByType<EquippedSkillPanelUI>(FindObjectsInactive.Include);
    }

    private static SkillInventoryPanelUI EnsureScenePanel()
    {
        GameObject root = GameObject.Find("SkillInventory");

        if (root == null)
            return null;

        SkillInventoryPanelUI panel = root.GetComponent<SkillInventoryPanelUI>();

        if (panel == null)
            panel = root.AddComponent<SkillInventoryPanelUI>();

        return panel;
    }

    private IInventoryRuntimeContext ResolveRuntimeContext()
    {
        if (runtimeContextProvider == null)
            runtimeContextProvider = GetComponentInParent<InventoryRuntimeContextProvider>(true);

        if (runtimeContextProvider != null)
            return runtimeContextProvider.GetContext();

        if (DataManager.Instance == null)
            return null;

        return InventoryRuntimeContext.ForBattle(DataManager.Instance.BattleRuntimeStore.GetOrCreate());
    }
}
