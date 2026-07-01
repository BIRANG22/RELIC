using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public enum SkillSlotType
{
    Passive,
    Unique,
    Ability,
    Equipped,
    Inventory
}

public struct SkillUpgradeRequest
{
    public string CharacterId;
    public string CurrentSkillId;
    public string UpgradeSkillId;
    public SkillSlotType SlotType;
    public int SlotIndex;
}
public class SkillUpgradePanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private SkillUpgradeIconItem iconPrefab;
    [SerializeField] private Image selectedSkillIconImage;

    [Header("Layout")]
    [SerializeField] private Vector2 fallbackIconSize = new(80f, 80f);
    [SerializeField] private Vector2 iconSpacing = new(15f, 15f);
    [SerializeField] private RectOffset iconPadding = new();
    [SerializeField] private TextAnchor iconAlignment = TextAnchor.UpperLeft;

    private readonly List<SkillUpgradeIconItem> spawnedItems = new();
    private bool hasUpgradedThisRestRoom;
    private bool hasSelectedUpgradeRequest;
    private SkillUpgradeRequest selectedUpgradeRequest;
    private bool hasCachedSelectedIconDefault;
    private Sprite selectedIconDefaultSprite;
    private bool selectedIconDefaultEnabled;

    public bool HasUpgradedThisRestRoom => hasUpgradedThisRestRoom;

    private void Awake()
    {
        ConfigureContentLayout();
    }

    private void OnEnable()
    {
        ConfigureContentLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled)
            ConfigureContentLayout();
    }

    public void ResetRestRoomUpgradeLimit()
    {
        hasUpgradedThisRestRoom = false;
        ClearSelectedUpgradeSelection();
    }

    public void Open()
    {
        if (hasUpgradedThisRestRoom)
        {
            Close();
            return;
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        ClearSelectedUpgradeSelection();
        ConfigureContentLayout();
        Refresh();
    }

    public void Close()
    {
        Clear();
        ClearSelectedUpgradeSelection();

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public bool TuneSelectedSkill()
    {
        if (hasUpgradedThisRestRoom || !hasSelectedUpgradeRequest)
            return false;

        return ApplySkillUpgrade(selectedUpgradeRequest);
    }

    private void Refresh()
    {
        Clear();
        ConfigureContentLayout();

        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        if (partyStore == null)
            return;

        for (int partyIndex = 0; partyIndex < partyStore.MaxPartyCountValue; partyIndex++)
        {
            string characterId = partyStore.GetCharacterId(partyIndex);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                    characterId,
                    out CharacterRuntimeData characterRuntime))
            {
                continue;
            }

            SpawnCharacterSkillItems(characterRuntime);
        }

        SpawnInventorySkillItems();
        RebuildContentLayout();
    }

    private void SpawnInventorySkillItems()
    {
        if (DataManager.Instance == null)
            return;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        if (runtime.SkillInventoryIds == null)
            return;

        for (int i = 0; i < runtime.SkillInventoryIds.Count; i++)
            SpawnInventorySkillItem(runtime.SkillInventoryIds[i], i);
    }

    private void SpawnInventorySkillItem(string skillId, int inventoryIndex)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return;

        if (iconPrefab == null || contentRoot == null)
            return;

        if (!TryGetSkillUpgradeId(skillId, out string upgradeSkillId))
            return;

        SkillUpgradeIconItem item = Instantiate(iconPrefab, contentRoot);
        PrepareSpawnedItemLayout(item);
        item.Initialize(
            null,
            skillId,
            upgradeSkillId,
            SkillSlotType.Inventory,
            inventoryIndex,
            OnSkillItemClicked
        );

        spawnedItems.Add(item);
    }

    private void SpawnCharacterSkillItems(CharacterRuntimeData characterRuntime)
    {
        if (characterRuntime == null)
            return;

        SpawnSkillItem(characterRuntime, characterRuntime.PassiveSkillId, SkillSlotType.Passive, -1);
        SpawnSkillItem(characterRuntime, characterRuntime.UniqueSkillId, SkillSlotType.Unique, -1);
        SpawnSkillItem(characterRuntime, characterRuntime.AbilitySkillId, SkillSlotType.Ability, -1);

        if (characterRuntime.EquippedSkillIds == null)
            return;

        for (int i = 2; i < characterRuntime.EquippedSkillIds.Length; i++)
        {
            SpawnSkillItem(characterRuntime, characterRuntime.EquippedSkillIds[i], SkillSlotType.Equipped, i);
        }
    }

    private void SpawnSkillItem(
        CharacterRuntimeData characterRuntime,
        string skillId,
        SkillSlotType slotType,
        int slotIndex)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return;

        if (iconPrefab == null || contentRoot == null)
            return;

        if (!DataManager.Instance.SkillDatabase.TryGet(skillId.Trim(), out SkillMasterData currentSkill) ||
            !SkillRarityUtility.CanUpgrade(currentSkill))
        {
            return;
        }

        if (!TryGetSkillUpgradeId(skillId, out string upgradeSkillId))
            return;

        SkillUpgradeIconItem item = Instantiate(iconPrefab, contentRoot);
        PrepareSpawnedItemLayout(item);
        item.Initialize(
            characterRuntime.CharacterId,
            skillId,
            upgradeSkillId,
            slotType,
            slotIndex,
            OnSkillItemClicked
        );

        spawnedItems.Add(item);
    }

    private void OnSkillItemClicked(SkillUpgradeRequest request, Sprite selectedIcon)
    {
        if (hasUpgradedThisRestRoom)
            return;

        SelectSkillForUpgrade(request, selectedIcon);
    }

    private void SelectSkillForUpgrade(SkillUpgradeRequest request, Sprite selectedIcon)
    {
        selectedUpgradeRequest = request;
        hasSelectedUpgradeRequest = true;
        SetSelectedSkillIcon(selectedIcon != null ? selectedIcon : ResolveSkillIcon(request.CurrentSkillId));
    }

    private bool ApplySkillUpgrade(SkillUpgradeRequest request)
    {
        if (DataManager.Instance == null)
            return false;

        if (hasUpgradedThisRestRoom)
            return false;

        if (request.SlotType == SkillSlotType.Inventory)
        {
            return UpgradeInventorySkill(request);
        }

        if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                request.CharacterId,
                out CharacterRuntimeData characterRuntime))
        {
            return false;
        }

        switch (request.SlotType)
        {
            case SkillSlotType.Passive:
                characterRuntime.PassiveSkillId = request.UpgradeSkillId;
                break;

            case SkillSlotType.Unique:
                characterRuntime.UniqueSkillId = request.UpgradeSkillId;
                break;

            case SkillSlotType.Ability:
                characterRuntime.AbilitySkillId = request.UpgradeSkillId;
                break;

            case SkillSlotType.Equipped:
                if (characterRuntime.EquippedSkillIds == null)
                    return false;

                if (request.SlotIndex < 0 || request.SlotIndex >= characterRuntime.EquippedSkillIds.Length)
                    return false;

                characterRuntime.EquippedSkillIds[request.SlotIndex] = request.UpgradeSkillId;
                break;

            default:
                return false;
        }

        DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(characterRuntime);
        EquippedSkillPanelUI.RefreshAll();
        SkillInventoryPanelUI.RefreshAll();

        Debug.Log(
            $"[SkillUpgradePanel] Skill upgraded / Character:{request.CharacterId} / " +
            $"{request.CurrentSkillId} -> {request.UpgradeSkillId}"
        );

        CompleteUpgrade();
        return true;
    }

    private bool UpgradeInventorySkill(SkillUpgradeRequest request)
    {
        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        if (runtime.SkillInventoryIds == null)
            return false;

        if (request.SlotIndex < 0 || request.SlotIndex >= runtime.SkillInventoryIds.Count)
            return false;

        runtime.SkillInventoryIds[request.SlotIndex] = request.UpgradeSkillId;
        DataManager.Instance.BattleRuntimeStore.Set(runtime);
        SkillInventoryPanelUI.RefreshAll();

        Debug.Log(
            $"[SkillUpgradePanel] Inventory skill upgraded / " +
            $"{request.CurrentSkillId} -> {request.UpgradeSkillId}"
        );

        CompleteUpgrade();
        return true;
    }

    private void CompleteUpgrade()
    {
        hasUpgradedThisRestRoom = true;
        Close();
    }

    private void SetSelectedSkillIcon(Sprite selectedIcon)
    {
        Image targetImage = ResolveSelectedSkillIconImage();

        if (targetImage == null)
            return;

        CacheSelectedSkillIconDefault(targetImage);
        targetImage.sprite = selectedIcon;
        targetImage.enabled = selectedIcon != null;
        targetImage.preserveAspect = true;
    }

    private void ClearSelectedUpgradeSelection()
    {
        hasSelectedUpgradeRequest = false;
        selectedUpgradeRequest = default;

        Image targetImage = ResolveSelectedSkillIconImage();

        if (targetImage == null)
            return;

        CacheSelectedSkillIconDefault(targetImage);
        targetImage.sprite = selectedIconDefaultSprite;
        targetImage.enabled = selectedIconDefaultEnabled;
    }

    private Image ResolveSelectedSkillIconImage()
    {
        if (selectedSkillIconImage != null)
            return selectedSkillIconImage;

        Transform root = panelRoot != null ? panelRoot.transform : transform;
        Transform wheel = root.Find("Wheel") ?? FindChildByName(root, "Wheel");
        Transform image = wheel != null
            ? wheel.Find("Image") ?? FindChildByName(wheel, "Image")
            : null;

        if (image == null)
            return null;

        selectedSkillIconImage = image.GetComponent<Image>();
        return selectedSkillIconImage;
    }

    private Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                return child;

            Transform nested = FindChildByName(child, childName);

            if (nested != null)
                return nested;
        }

        return null;
    }

    private void CacheSelectedSkillIconDefault(Image targetImage)
    {
        if (hasCachedSelectedIconDefault || targetImage == null)
            return;

        selectedIconDefaultSprite = targetImage.sprite;
        selectedIconDefaultEnabled = targetImage.enabled;
        hasCachedSelectedIconDefault = true;
    }

    private Sprite ResolveSkillIcon(string skillId)
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.SkillDatabase != null &&
            DataManager.Instance.SkillDatabase.TryGet(skillId, out SkillMasterData skillData) &&
            skillData.Icon != null)
        {
            return skillData.Icon;
        }

        if (DataManager.Instance.SkillIconDatabase != null &&
            DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId, out Sprite databaseIcon))
        {
            return databaseIcon;
        }

        return null;
    }

    private void Clear()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();
    }

    private void ConfigureContentLayout()
    {
        if (contentRoot == null)
            return;

        iconPadding ??= new RectOffset();

        Vector2 iconSize = ResolveIconSize();
        GridLayoutGroup grid = contentRoot.GetComponent<GridLayoutGroup>();

        if (grid == null)
            grid = contentRoot.gameObject.AddComponent<GridLayoutGroup>();

        DisableCompetingLayoutGroups(grid);

        grid.enabled = true;
        grid.childAlignment = iconAlignment;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.cellSize = iconSize;
        grid.spacing = iconSpacing;
        grid.padding = iconPadding;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = CalculateColumnCount(iconSize);

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();

        if (fitter == null)
            fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void DisableCompetingLayoutGroups(GridLayoutGroup activeGrid)
    {
        LayoutGroup[] layoutGroups = contentRoot.GetComponents<LayoutGroup>();

        for (int i = 0; i < layoutGroups.Length; i++)
        {
            LayoutGroup layoutGroup = layoutGroups[i];

            if (layoutGroup == null || layoutGroup == activeGrid)
                continue;

            layoutGroup.enabled = false;
        }
    }

    private int CalculateColumnCount(Vector2 iconSize)
    {
        RectTransform rectTransform = contentRoot as RectTransform;
        float width = rectTransform != null ? rectTransform.rect.width : 0f;

        if (width <= 0f && rectTransform != null)
            width = rectTransform.sizeDelta.x;

        if (width <= 0f)
            return 1;

        float availableWidth = Mathf.Max(0f, width - iconPadding.left - iconPadding.right);
        float cellWidth = Mathf.Max(1f, iconSize.x);
        float spacing = Mathf.Max(0f, iconSpacing.x);

        return Mathf.Max(1, Mathf.FloorToInt((availableWidth + spacing) / (cellWidth + spacing)));
    }

    private Vector2 ResolveIconSize()
    {
        Vector2 size = fallbackIconSize;

        if (iconPrefab == null)
            return ClampIconSize(size);

        LayoutElement prefabLayout = iconPrefab.GetComponent<LayoutElement>();

        if (prefabLayout != null &&
            prefabLayout.preferredWidth > 0f &&
            prefabLayout.preferredHeight > 0f)
        {
            return ClampIconSize(new Vector2(prefabLayout.preferredWidth, prefabLayout.preferredHeight));
        }

        RectTransform prefabRect = iconPrefab.GetComponent<RectTransform>();

        if (prefabRect == null)
            return ClampIconSize(size);

        Vector2 rectSize = prefabRect.rect.size;

        if (rectSize.x > 0f && rectSize.y > 0f)
            return ClampIconSize(rectSize);

        if (prefabRect.sizeDelta.x > 0f && prefabRect.sizeDelta.y > 0f)
            return ClampIconSize(prefabRect.sizeDelta);

        return ClampIconSize(size);
    }

    private Vector2 ClampIconSize(Vector2 size)
    {
        return new Vector2(
            Mathf.Max(1f, size.x),
            Mathf.Max(1f, size.y)
        );
    }

    private void PrepareSpawnedItemLayout(SkillUpgradeIconItem item)
    {
        if (item == null)
            return;

        Vector2 iconSize = ResolveIconSize();
        RectTransform rectTransform = item.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = iconSize;
            rectTransform.localScale = Vector3.one;
        }

        LayoutElement layoutElement = item.GetComponent<LayoutElement>();

        if (layoutElement == null)
            layoutElement = item.gameObject.AddComponent<LayoutElement>();

        layoutElement.ignoreLayout = false;
        layoutElement.minWidth = iconSize.x;
        layoutElement.minHeight = iconSize.y;
        layoutElement.preferredWidth = iconSize.x;
        layoutElement.preferredHeight = iconSize.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }

    private void RebuildContentLayout()
    {
        if (contentRoot is RectTransform rectTransform)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private bool TryGetSkillUpgradeId(string skillId, out string upgradeSkillId)
    {
        upgradeSkillId = null;

        if (string.IsNullOrWhiteSpace(skillId))
            return false;

        int lastUnderscoreIndex = skillId.LastIndexOf('_');

        if (lastUnderscoreIndex < 0)
            return false;

        string prefix = skillId.Substring(0, lastUnderscoreIndex + 1);
        string numberText = skillId.Substring(lastUnderscoreIndex + 1);

        if (!int.TryParse(numberText, out int number))
            return false;

        // 이미 강화된 스킬이면 제외
        if (number % 2 == 0)
            return false;

        int upgradeNumber = number + 1;
        string upgradedNumberText = upgradeNumber.ToString(new string('0', numberText.Length));
        upgradeSkillId = prefix + upgradedNumberText;

        if (DataManager.Instance == null)
            return false;

        if (!DataManager.Instance.SkillDatabase.TryGet(skillId.Trim(), out SkillMasterData currentSkill) ||
            !SkillRarityUtility.CanUpgrade(currentSkill))
        {
            return false;
        }

        return DataManager.Instance.SkillDatabase.TryGet(upgradeSkillId, out _);
    }
}
