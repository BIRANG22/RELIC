using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도감 패널의 탭, 분류, 아이콘 목록, 선택 정보 표시를 관리합니다.
/// 실제 데이터와 아이콘은 DataManager에 등록된 GameData를 사용합니다.
/// </summary>
public class RecordPanelUI : MonoBehaviour
{
    private enum MainTab
    {
        Unique,
        Skill,
        Fragment,
        Relic,
        Item
    }

    private const string UnknownDisplayName = "???";

    [Header("Main Tab Panels")]
    [SerializeField] private GameObject uniqueContent;
    [SerializeField] private GameObject skillContent;
    [SerializeField] private GameObject fragmentContent;
    [SerializeField] private GameObject relicContent;
    [SerializeField] private GameObject itemContent;

    [Header("Main Tab Buttons")]
    [SerializeField] private Button uniqueTabButton;
    [SerializeField] private Button skillTabButton;
    [SerializeField] private Button fragmentTabButton;
    [SerializeField] private Button relicTabButton;
    [SerializeField] private Button itemTabButton;

    [Header("Grid Contents")]
    [SerializeField] private RectTransform uniqueRootContent;
    [SerializeField] private RectTransform skillGridContent;
    [SerializeField] private RectTransform fragmentGridContent;
    [SerializeField] private RectTransform relicGridContent;
    [SerializeField] private RectTransform itemGridContent;

    [Header("Scroll Rects")]
    [SerializeField] private ScrollRect uniqueScrollRect;
    [SerializeField] private ScrollRect skillScrollRect;
    [SerializeField] private ScrollRect fragmentScrollRect;
    [SerializeField] private ScrollRect relicScrollRect;
    [SerializeField] private ScrollRect itemScrollRect;

    [Header("Item Slot")]
    [SerializeField] private RecordIconSlotUI iconSlotPrefab;
    [SerializeField, Min(1)] private int iconsPerRow = 5;

    [Header("Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private string emptyNameText = string.Empty;

    [Header("Initial View")]
    [SerializeField] private bool selectFirstItemAutomatically = true;

    [Header("Record Scroll Padding")]
    [Tooltip("모든 도감 스크롤에서 마지막 슬롯이 Viewport 마스크에 잘리지 않도록 Content 하단에 추가하는 공통 여백입니다.")]
    [SerializeField, Min(0f)] private float contentBottomPadding = 40f;

    private readonly List<RecordIconSlotUI> spawnedSlots = new();
    private readonly List<RecordIconSlotUI> activeFixedSlots = new();
    private RecordIconSlotUI selectedSlot;
    private MainTab currentMainTab = MainTab.Unique;

    private ColorBlock uniqueTabOriginalColors;
    private ColorBlock skillTabOriginalColors;
    private ColorBlock fragmentTabOriginalColors;
    private ColorBlock relicTabOriginalColors;
    private ColorBlock itemTabOriginalColors;
    private bool mainTabColorsCached;
    private bool debugRevealAll;
    private Coroutine uniqueLayoutRefreshCoroutine;

    private void Awake()
    {
        EnsureReferences();
        ApplyGridConstraints();
        CacheMainTabButtonColors();
    }

    private void OnEnable()
    {
        RecordDiscoveryService.BackfillFromCurrentState(GetDataManager());
        EnsureReferences();
        ShowUniqueTab();
    }

    /// <summary>
    /// 디버그 도감 표시 모드를 설정합니다.
    /// true이면 저장된 획득 이력을 변경하지 않고 모든 항목을 UI에서만 공개합니다.
    /// </summary>
    public void SetDebugRevealAll(bool revealAll)
    {
        debugRevealAll = revealAll;

        if (!gameObject.activeInHierarchy)
            return;

        switch (currentMainTab)
        {
            case MainTab.Unique:
                ShowUniqueTab();
                break;
            case MainTab.Skill:
                ShowSkillTab();
                break;
            case MainTab.Fragment:
                ShowFragmentTab();
                break;
            case MainTab.Relic:
                ShowRelicTab();
                break;
            case MainTab.Item:
                ShowItemTab();
                break;
        }
    }

    public void Close()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideRecord();
            return;
        }

        gameObject.SetActive(false);
    }

    public void ShowUniqueTab()
    {
        SetMainTab(MainTab.Unique);
        BuildUniqueSkillSections();
    }

    public void ShowSkillTab()
    {
        BuildLegacySkillList();
    }

    public void ShowFragmentTab()
    {
        BuildRuneList();
    }

    public void ShowRelicTab()
    {
        ShowPassiveRelics();
    }

    public void ShowItemTab()
    {
        SetMainTab(MainTab.Item);
        BuildItemList();
    }

    // 기존 Skill 탭의 하위 버튼 연결이 남아 있어도 모두 전승 기억 목록으로 동작하도록 유지합니다.
    public void ShowSkillPassive() => BuildLegacySkillList(Category.Passive);
    public void ShowSkillUnique() => BuildLegacySkillList(Category.Unique);
    public void ShowSkillAbility() => BuildLegacySkillList(Category.Ability);
    public void ShowSkillPublic() => BuildLegacySkillList(Category.Public);
    public void ShowSkillCore() => BuildLegacySkillList(Category.Core);

    // 기존 버튼 연결 호환.
    public void ShowCommonRunes() => BuildRuneList();
    public void ShowExclusiveRunes() => BuildRuneList();

    public void ShowPassiveRelics() => BuildRelicList(false);
    public void ShowActiveRelics() => BuildRelicList(true);

    private void BuildUniqueSkillSections()
    {
        ClearCurrentSlots();

        DataManager dataManager = GetDataManager();
        if (dataManager == null)
            return;

        RectTransform root = GetUniqueRootContent();
        if (root == null)
        {
            SetName(emptyNameText);
            return;
        }

        foreach (Transform child in root)
        {
            if (child == null)
                continue;

            CharacterMasterData character = FindCharacterForSection(dataManager, child.name);
            if (character == null)
                continue;

            BuildUniqueCharacterSection(dataManager, child, character);
        }

        CompleteListBuild(null);
        ScheduleUniqueLayoutRefresh(root);
    }

    private void BuildUniqueCharacterSection(DataManager dataManager, Transform sectionRoot, CharacterMasterData character)
    {
        bool characterUnlocked = IsCharacterUnlocked(dataManager, character);
        string[] skillIds = GetCharacterUniqueSkillIds(character);

        for (int i = 0; i < 6; i++)
        {
            RectTransform slotContainer = FindFixedSlotContainer(sectionRoot, i + 1);
            if (slotContainer == null)
                continue;

            string canonicalSkillId = GetCanonicalSkillId(dataManager.SkillDatabase, skillIds[i]);

            bool revealSkill = debugRevealAll ||
                               (characterUnlocked &&
                                !string.IsNullOrWhiteSpace(canonicalSkillId) &&
                                RecordDiscoveryService.IsSkillDiscovered(dataManager, canonicalSkillId));

            CreateFixedSkillSlot(dataManager, slotContainer, skillIds[i], revealSkill);
        }
    }

    private void CreateFixedSkillSlot(DataManager dataManager, RectTransform slotContainer, string skillId, bool revealSkill)
    {
        if (slotContainer == null || iconSlotPrefab == null)
            return;

        SkillMasterData skill = null;
        Sprite icon = null;
        string displayName = UnknownDisplayName;
        bool showIcon = false;

        if (!string.IsNullOrWhiteSpace(skillId) && dataManager?.SkillDatabase != null)
        {
            if (dataManager.SkillDatabase.TryGet(skillId, out skill) &&
                revealSkill &&
                skill != null)
            {
                displayName = RecordDisplayNameResolver.SkillName(skill);
                showIcon = true;

                icon = skill.Icon;
                if (icon == null && dataManager.SkillIconDatabase != null)
                    dataManager.SkillIconDatabase.TryGetIcon(skill.SkillId, out icon);
            }
        }

        RecordIconSlotUI slot = Instantiate(iconSlotPrefab, slotContainer, false);
        slot.Initialize(icon, displayName, OnSlotClicked, showIcon);
        spawnedSlots.Add(slot);
    }

    private static string GetCanonicalSkillId(SkillDatabase skillDatabase, string skillId)
    {
        if (skillDatabase == null || string.IsNullOrWhiteSpace(skillId))
            return skillId;

        return skillDatabase.TryGet(skillId, out SkillMasterData skill) && skill != null
            ? skill.SkillId
            : skillId.Trim();
    }

    private void BuildLegacySkillList()
    {
        BuildLegacySkillListInternal(null);
    }

    private void BuildLegacySkillList(Category category)
    {
        BuildLegacySkillListInternal(category);
    }

    private void BuildLegacySkillListInternal(Category? category)
    {
        SetMainTab(MainTab.Skill);
        ClearCurrentSlots();

        DataManager dataManager = GetDataManager();
        if (dataManager == null || dataManager.SkillDatabase == null)
            return;

        HashSet<string> uniqueCharacterSkillIds = GetAllCharacterMemoryIds(dataManager);

        IEnumerable<SkillMasterData> skills = dataManager.SkillDatabase.GetAll()
            .Where(skill => skill != null && skill.Level != 2)
            .Where(skill => skill.Category != Category.Move)
            .Where(skill => !uniqueCharacterSkillIds.Contains(skill.SkillId));

        if (category.HasValue)
            skills = skills.Where(skill => skill.Category == category.Value);

        skills = skills
            .OrderBy(GetLegacySkillRarityOrder)
            .ThenBy(skill => skill.SkillId, StringComparer.OrdinalIgnoreCase);

        foreach (SkillMasterData skill in skills)
        {
            bool revealSkill = debugRevealAll || RecordDiscoveryService.IsSkillDiscovered(dataManager, skill.SkillId);
            Sprite icon = null;

            if (revealSkill)
            {
                icon = skill.Icon;
                if (icon == null && dataManager.SkillIconDatabase != null)
                    dataManager.SkillIconDatabase.TryGetIcon(skill.SkillId, out icon);
            }

            string displayName = revealSkill ? RecordDisplayNameResolver.SkillName(skill) : UnknownDisplayName;
            CreateSlot(skillGridContent, icon, displayName, revealSkill);
        }

        CompleteListBuild(skillScrollRect);
    }


    private static int GetLegacySkillRarityOrder(SkillMasterData skill)
    {
        if (skill == null)
            return int.MaxValue;

        return skill.Rarity switch
        {
            SkillRarity.CoreCommon => 0,
            SkillRarity.CoreRare => 1,
            SkillRarity.CoreEpic => 2,
            SkillRarity.Shared => 3,
            SkillRarity.Passive => 4,
            SkillRarity.Unique => 5,
            SkillRarity.CharacterExclusive => 6,
            SkillRarity.Move => 7,
            _ => 8
        };
    }

    private static Dictionary<string, int> BuildCharacterRuneOrderMap(DataManager dataManager)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (dataManager?.CharacterDatabase == null)
            return result;

        string[] characterOrder = { "Hilt", "Kaya", "Haze", "Ines", "Reina" };
        int order = 0;

        for (int characterIndex = 0; characterIndex < characterOrder.Length; characterIndex++)
        {
            CharacterMasterData character = FindCharacterForSection(dataManager, characterOrder[characterIndex]);
            if (character == null)
                continue;

            string[] runeIds = character.GetRuneIds();
            if (runeIds == null)
                continue;

            for (int runeIndex = 0; runeIndex < runeIds.Length; runeIndex++)
            {
                string runeId = runeIds[runeIndex];
                if (string.IsNullOrWhiteSpace(runeId) || result.ContainsKey(runeId))
                    continue;

                result.Add(runeId.Trim(), order++);
            }
        }

        return result;
    }

    private static int GetRuneGroupOrder(RuneData rune, IReadOnlyDictionary<string, int> characterRuneOrder)
    {
        if (rune == null)
            return int.MaxValue;

        if (!string.IsNullOrWhiteSpace(rune.RuneId) &&
            characterRuneOrder != null &&
            characterRuneOrder.ContainsKey(rune.RuneId.Trim()))
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(rune.TargetCharacterId))
            return 1;

        return 2;
    }

    private static int GetCharacterRuneOrder(RuneData rune, IReadOnlyDictionary<string, int> characterRuneOrder)
    {
        if (rune == null || string.IsNullOrWhiteSpace(rune.RuneId) || characterRuneOrder == null)
            return int.MaxValue;

        return characterRuneOrder.TryGetValue(rune.RuneId.Trim(), out int order)
            ? order
            : int.MaxValue;
    }

    private void BuildRuneList()
    {
        SetMainTab(MainTab.Fragment);
        ClearCurrentSlots();

        DataManager dataManager = GetDataManager();
        if (dataManager == null || dataManager.RuneDatabase == null)
            return;

        Dictionary<string, int> characterRuneOrder = BuildCharacterRuneOrderMap(dataManager);

        IEnumerable<RuneData> runes = dataManager.RuneDatabase.GetAll()
            .Where(rune => rune != null)
            .OrderBy(rune => GetRuneGroupOrder(rune, characterRuneOrder))
            .ThenBy(rune => GetCharacterRuneOrder(rune, characterRuneOrder))
            .ThenBy(rune => rune.RuneId, StringComparer.OrdinalIgnoreCase);

        foreach (RuneData rune in runes)
        {
            bool discovered = debugRevealAll || RecordDiscoveryService.IsRuneDiscovered(dataManager, rune.RuneId);
            Sprite icon = null;

            if (discovered && dataManager.RuneIconDatabase != null)
                dataManager.RuneIconDatabase.TryGetIcon(rune.RuneId, out icon);

            string displayName = discovered ? RecordDisplayNameResolver.RuneName(rune) : UnknownDisplayName;
            CreateSlot(fragmentGridContent, icon, displayName, discovered);
        }

        CompleteListBuild(fragmentScrollRect);
    }

    private void BuildRelicList(bool activeOnly)
    {
        SetMainTab(MainTab.Relic);
        ClearCurrentSlots();

        DataManager dataManager = GetDataManager();
        if (dataManager == null || dataManager.RelicDatabase == null)
            return;

        IEnumerable<RelicData> relics = dataManager.RelicDatabase.GetAll()
            .Where(relic => relic != null && IsActiveRelic(relic) == activeOnly)
            .OrderBy(RecordDisplayNameResolver.RelicName, StringComparer.CurrentCulture);

        foreach (RelicData relic in relics)
        {
            bool discovered = debugRevealAll || RecordDiscoveryService.IsRelicDiscovered(dataManager, relic.FragmentId);
            Sprite icon = null;

            if (discovered && dataManager.RelicIconDatabase != null)
                dataManager.RelicIconDatabase.TryGetIcon(relic.FragmentId, out icon);

            string displayName = discovered ? RecordDisplayNameResolver.RelicName(relic) : UnknownDisplayName;
            CreateSlot(relicGridContent, icon, displayName, discovered);
        }

        CompleteListBuild(relicScrollRect);
    }

    private void BuildItemList()
    {
        ClearCurrentSlots();

        DataManager dataManager = GetDataManager();
        if (dataManager == null || dataManager.ItemDatabase == null)
            return;

        IEnumerable<ItemData> items = dataManager.ItemDatabase.GetAll()
            .Where(item => item != null)
            .OrderBy(item => item.ItemId, StringComparer.OrdinalIgnoreCase);

        foreach (ItemData item in items)
        {
            bool discovered = debugRevealAll || RecordDiscoveryService.IsItemDiscovered(dataManager, item.ItemId);
            Sprite icon = null;

            if (discovered && dataManager.ItemIconDatabase != null)
                dataManager.ItemIconDatabase.TryGetIcon(item.ItemId, out icon);

            string displayName = discovered ? RecordDisplayNameResolver.ItemName(item) : UnknownDisplayName;
            CreateSlot(itemGridContent, icon, displayName, discovered);
        }

        CompleteListBuild(itemScrollRect);
    }

    private void SetMainTab(MainTab tab)
    {
        currentMainTab = tab;

        SetActive(uniqueContent, tab == MainTab.Unique);
        SetActive(skillContent, tab == MainTab.Skill);
        SetActive(fragmentContent, tab == MainTab.Fragment);
        SetActive(relicContent, tab == MainTab.Relic);
        SetActive(itemContent, tab == MainTab.Item);

        SelectMainTabButton(tab);
        StartCoroutine(RefreshMainTabSelectionNextFrame(tab));
    }

    private IEnumerator RefreshMainTabSelectionNextFrame(MainTab tab)
    {
        yield return null;

        if (currentMainTab == tab)
            SelectMainTabButton(tab);
    }

    private void SelectMainTabButton(MainTab tab)
    {
        CacheMainTabButtonColors();

        ApplyMainTabButtonColors(uniqueTabButton, uniqueTabOriginalColors, tab == MainTab.Unique);
        ApplyMainTabButtonColors(skillTabButton, skillTabOriginalColors, tab == MainTab.Skill);
        ApplyMainTabButtonColors(fragmentTabButton, fragmentTabOriginalColors, tab == MainTab.Fragment);
        ApplyMainTabButtonColors(relicTabButton, relicTabOriginalColors, tab == MainTab.Relic);
        ApplyMainTabButtonColors(itemTabButton, itemTabOriginalColors, tab == MainTab.Item);

        ApplyMainTabAnimationState(uniqueTabButton, tab == MainTab.Unique);
        ApplyMainTabAnimationState(skillTabButton, tab == MainTab.Skill);
        ApplyMainTabAnimationState(fragmentTabButton, tab == MainTab.Fragment);
        ApplyMainTabAnimationState(relicTabButton, tab == MainTab.Relic);
        ApplyMainTabAnimationState(itemTabButton, tab == MainTab.Item);
    }

    private static void ApplyMainTabAnimationState(Button button, bool selected)
    {
        if (button == null)
            return;

        ButtonAnimationCoroutine animation = button.GetComponent<ButtonAnimationCoroutine>();
        if (animation == null)
            animation = button.GetComponentInChildren<ButtonAnimationCoroutine>(true);

        if (animation != null)
            animation.ForceSetClickedState(selected, false);
    }

    private void CacheMainTabButtonColors()
    {
        if (mainTabColorsCached)
            return;

        if (uniqueTabButton != null)
            uniqueTabOriginalColors = uniqueTabButton.colors;

        if (skillTabButton != null)
            skillTabOriginalColors = skillTabButton.colors;

        if (fragmentTabButton != null)
            fragmentTabOriginalColors = fragmentTabButton.colors;

        if (relicTabButton != null)
            relicTabOriginalColors = relicTabButton.colors;

        if (itemTabButton != null)
            itemTabOriginalColors = itemTabButton.colors;

        mainTabColorsCached = true;
    }

    private static void ApplyMainTabButtonColors(Button button, ColorBlock originalColors, bool selected)
    {
        if (button == null)
            return;

        ColorBlock colors = originalColors;

        if (selected)
        {
            Color selectedColor = originalColors.selectedColor;
            colors.normalColor = selectedColor;
            colors.highlightedColor = selectedColor;
            colors.selectedColor = selectedColor;
        }
        else
        {
            Color normalColor = originalColors.normalColor;
            colors.normalColor = normalColor;
            colors.highlightedColor = originalColors.highlightedColor;

            // EventSystem이 이전 버튼을 Selected 상태로 잠시 유지하더라도
            // 비선택 탭은 선택 색으로 보이지 않도록 Selected 색도 기본색으로 맞춥니다.
            colors.selectedColor = normalColor;
        }

        button.colors = colors;

        if (button.targetGraphic != null)
        {
            Color targetColor = selected
                ? originalColors.selectedColor
                : originalColors.normalColor;

            button.targetGraphic.CrossFadeColor(
                targetColor * colors.colorMultiplier,
                0f,
                true,
                true);
        }
    }

    private void CreateSlot(RectTransform parent, Sprite icon, string displayName, bool showIcon)
    {
        if (parent == null || iconSlotPrefab == null)
            return;

        RecordIconSlotUI slot = Instantiate(iconSlotPrefab, parent, false);
        slot.Initialize(icon, displayName, OnSlotClicked, showIcon);
        spawnedSlots.Add(slot);
    }

    private void OnSlotClicked(RecordIconSlotUI clickedSlot, string displayName)
    {
        SelectSlot(clickedSlot);
        SetName(displayName);
    }

    private void SelectSlot(RecordIconSlotUI slot)
    {
        if (selectedSlot != null && selectedSlot != slot)
            selectedSlot.SetSelected(false);

        selectedSlot = slot;

        if (selectedSlot != null)
            selectedSlot.SetSelected(true);
    }

    private void CompleteListBuild(ScrollRect scrollRect)
    {
        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;

        if (selectFirstItemAutomatically && spawnedSlots.Count > 0)
        {
            RecordIconSlotUI firstSlot = spawnedSlots[0];
            SelectSlot(firstSlot);
            SetName(firstSlot.DisplayName);
            return;
        }

        SetName(emptyNameText);
    }

    private void CompleteFixedListBuild(ScrollRect scrollRect, List<RecordIconSlotUI> slots)
    {
        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;

        if (selectFirstItemAutomatically)
        {
            RecordIconSlotUI firstSlot = slots.FirstOrDefault(slot => slot != null);
            if (firstSlot != null)
            {
                SelectSlot(firstSlot);
                SetName(firstSlot.DisplayName);
                return;
            }
        }

        SetName(emptyNameText);
    }

    private void ClearCurrentSlots()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
        }

        foreach (RecordIconSlotUI slot in activeFixedSlots)
        {
            if (slot != null)
                slot.SetSelected(false);
        }

        activeFixedSlots.Clear();

        foreach (RecordIconSlotUI slot in spawnedSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        spawnedSlots.Clear();
        SetName(emptyNameText);
    }

    private void SetName(string value)
    {
        if (nameText != null)
            nameText.text = value ?? string.Empty;
    }

    private bool IsActiveRelic(RelicData relic)
    {
        if (relic == null || string.IsNullOrWhiteSpace(relic.Type))
            return false;

        return relic.Type.IndexOf("active", StringComparison.OrdinalIgnoreCase) >= 0 ||
               relic.Type.IndexOf("액티브", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private DataManager GetDataManager()
    {
        if (DataManager.Instance != null)
            return DataManager.Instance;

        DataManager dataManager = FindFirstObjectByType<DataManager>(FindObjectsInactive.Include);
        if (dataManager == null)
            Debug.LogWarning("[RecordPanelUI] DataManager를 찾지 못해 도감 목록을 만들 수 없습니다.");

        return dataManager;
    }

    private void ApplyGridConstraints()
    {
        ApplyGridConstraint(skillGridContent);
        ApplyGridConstraint(fragmentGridContent);
        ApplyGridConstraint(relicGridContent);
        ApplyGridConstraint(itemGridContent);
    }

    private void ApplyGridConstraint(RectTransform content)
    {
        if (content == null)
            return;

        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        if (grid == null)
            return;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, iconsPerRow);

        RectOffset padding = grid.padding;
        if (padding == null)
            padding = new RectOffset();

        padding.bottom = Mathf.Max(0, Mathf.RoundToInt(contentBottomPadding));
        grid.padding = padding;
    }

    private void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }

    private void EnsureReferences()
    {
        if (uniqueContent == null)
            uniqueContent = FindGameObjectByPath("Content/UniqueContent") ?? FindGameObjectByName("UniqueContent");

        if (skillContent == null)
            skillContent = FindGameObjectByPath("Content/SkillContent") ?? FindGameObjectByName("SkillContent");

        if (fragmentContent == null)
            fragmentContent = FindGameObjectByPath("Content/FragmentContent") ?? FindGameObjectByName("FragmentContent");

        if (relicContent == null)
            relicContent = FindGameObjectByPath("Content/RelicContent") ?? FindGameObjectByName("RelicContent");

        if (itemContent == null)
            itemContent = FindGameObjectByPath("Content/ItemContent") ?? FindGameObjectByName("ItemContent");

        if (uniqueTabButton == null)
            uniqueTabButton = FindButtonByPath("Buttons/Unique") ?? FindButtonByName("Unique");

        if (skillTabButton == null)
            skillTabButton = FindButtonByPath("Buttons/Skill") ?? FindButtonByName("Skill");

        if (fragmentTabButton == null)
            fragmentTabButton = FindButtonByPath("Buttons/Fragment") ?? FindButtonByName("Fragment");

        if (relicTabButton == null)
            relicTabButton = FindButtonByPath("Buttons/Relic") ?? FindButtonByName("Relic");

        if (itemTabButton == null)
            itemTabButton = FindButtonByPath("Buttons/Item") ?? FindButtonByName("Item");

        if (uniqueRootContent == null && uniqueContent != null)
            uniqueRootContent = FindNestedRectTransform(uniqueContent.transform, "Scroll View/Viewport/Content");

        if (skillGridContent == null && skillContent != null)
            skillGridContent = FindNestedRectTransform(skillContent.transform, "Scroll View/Viewport/Content");

        if (fragmentGridContent == null && fragmentContent != null)
            fragmentGridContent = FindNestedRectTransform(fragmentContent.transform, "Scroll View/Viewport/Content");

        if (relicGridContent == null && relicContent != null)
            relicGridContent = FindNestedRectTransform(relicContent.transform, "Scroll View/Viewport/Content");

        if (itemGridContent == null && itemContent != null)
            itemGridContent = FindNestedRectTransform(itemContent.transform, "Scroll View/Viewport/Content");

        if (uniqueScrollRect == null && uniqueContent != null)
            uniqueScrollRect = uniqueContent.GetComponentInChildren<ScrollRect>(true);

        if (skillScrollRect == null && skillContent != null)
            skillScrollRect = skillContent.GetComponentInChildren<ScrollRect>(true);

        if (fragmentScrollRect == null && fragmentContent != null)
            fragmentScrollRect = fragmentContent.GetComponentInChildren<ScrollRect>(true);

        if (relicScrollRect == null && relicContent != null)
            relicScrollRect = relicContent.GetComponentInChildren<ScrollRect>(true);

        if (itemScrollRect == null && itemContent != null)
            itemScrollRect = itemContent.GetComponentInChildren<ScrollRect>(true);
    }

    private GameObject FindGameObjectByPath(string path)
    {
        Transform found = transform.Find(path);
        return found != null ? found.gameObject : null;
    }

    private GameObject FindGameObjectByName(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (string.Equals(child.name, objectName, StringComparison.OrdinalIgnoreCase))
                return child.gameObject;
        }

        return null;
    }

    private Button FindButtonByPath(string path)
    {
        Transform found = transform.Find(path);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private Button FindButtonByName(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (!string.Equals(child.name, objectName, StringComparison.OrdinalIgnoreCase))
                continue;

            Button button = child.GetComponent<Button>();
            if (button != null)
                return button;
        }

        return null;
    }

    private static RectTransform FindNestedRectTransform(Transform root, string relativePath)
    {
        if (root == null)
            return null;

        Transform found = root.Find(relativePath);
        return found as RectTransform;
    }

    private RectTransform GetUniqueRootContent()
    {
        if (uniqueRootContent == null && uniqueContent != null)
            uniqueRootContent = FindNestedRectTransform(uniqueContent.transform, "Scroll View/Viewport/Content");

        return uniqueRootContent;
    }

    private static RectTransform FindFixedSlotContainer(Transform sectionRoot, int slotIndex)
    {
        if (sectionRoot == null)
            return null;

        string slotName = $"Slot{slotIndex:00}";
        Transform slotTransform = sectionRoot.Find(slotName);
        if (slotTransform == null)
            slotTransform = FindDeepChild(sectionRoot, slotName);

        return slotTransform as RectTransform;
    }

    private void ScheduleUniqueLayoutRefresh(RectTransform root)
    {
        if (uniqueLayoutRefreshCoroutine != null)
        {
            StopCoroutine(uniqueLayoutRefreshCoroutine);
            uniqueLayoutRefreshCoroutine = null;
        }

        if (!isActiveAndEnabled || root == null)
            return;

        uniqueLayoutRefreshCoroutine = StartCoroutine(RefreshUniqueLayoutNextFrame(root));
    }

    private IEnumerator RefreshUniqueLayoutNextFrame(RectTransform root)
    {
        // 첫 진입 프레임에는 고유기억 슬롯 프리팹과 레이아웃의 크기가 아직 반영되지 않을 수 있습니다.
        // 한 프레임 기다린 뒤 자식과 Content 레이아웃을 강제로 갱신하여 즉시 스크롤 가능하게 만듭니다.
        yield return null;

        if (root == null || uniqueContent == null || !uniqueContent.activeInHierarchy)
        {
            uniqueLayoutRefreshCoroutine = null;
            yield break;
        }

        Canvas.ForceUpdateCanvases();

        foreach (Transform child in root)
        {
            if (child is RectTransform childRect && child.gameObject.activeInHierarchy)
                LayoutRebuilder.ForceRebuildLayoutImmediate(childRect);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        Canvas.ForceUpdateCanvases();

        RefreshUniqueScrollContentHeight(root);

        if (uniqueScrollRect != null)
        {
            uniqueScrollRect.StopMovement();
            uniqueScrollRect.verticalNormalizedPosition = 1f;
        }

        uniqueLayoutRefreshCoroutine = null;
    }

    private void RefreshUniqueScrollContentHeight(RectTransform root)
    {
        if (root == null)
            return;

        if (uniqueScrollRect != null)
        {
            uniqueScrollRect.content = root;
            uniqueScrollRect.vertical = true;
            uniqueScrollRect.horizontal = false;
        }

        Canvas.ForceUpdateCanvases();

        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        bool hasChildRect = false;
        Vector3[] corners = new Vector3[4];

        foreach (Transform child in root)
        {
            if (child is not RectTransform childRect || !child.gameObject.activeSelf)
                continue;

            childRect.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 localCorner = root.InverseTransformPoint(corners[i]);
                minY = Mathf.Min(minY, localCorner.y);
                maxY = Mathf.Max(maxY, localCorner.y);
            }

            hasChildRect = true;
        }

        if (!hasChildRect)
            return;

        float contentHeight = Mathf.Max(0f, maxY - minY);
        float viewportHeight = uniqueScrollRect != null && uniqueScrollRect.viewport != null
            ? uniqueScrollRect.viewport.rect.height
            : 0f;

        float paddedContentHeight = contentHeight + contentBottomPadding;

        root.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            Mathf.Max(paddedContentHeight, viewportHeight));

        Canvas.ForceUpdateCanvases();
    }

    private static Transform FindDeepChild(Transform parent, string targetName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (string.Equals(child.name, targetName, StringComparison.OrdinalIgnoreCase))
                return child;

            Transform nested = FindDeepChild(child, targetName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static void SetSectionLabel(Transform sectionRoot, string label)
    {
        if (sectionRoot == null)
            return;

        TMP_Text directText = null;
        foreach (Transform child in sectionRoot)
        {
            if (child == null)
                continue;

            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text != null)
            {
                directText = text;
                break;
            }
        }

        if (directText != null)
            directText.text = label ?? string.Empty;
    }

    private static string[] GetCharacterUniqueSkillIds(CharacterMasterData character)
    {
        if (character == null)
            return new string[6];

        return new[]
        {
            character.PassiveSkill1,
            character.PassiveSkill2,
            character.UniqueSkill1,
            character.UniqueSkill2,
            character.CharacterSkill1,
            character.CharacterSkill2
        };
    }

    private static HashSet<string> GetAllCharacterMemoryIds(DataManager dataManager)
    {
        HashSet<string> result = new(StringComparer.OrdinalIgnoreCase);
        if (dataManager?.CharacterDatabase == null)
            return result;

        foreach (CharacterMasterData character in dataManager.CharacterDatabase.GetAll().Values)
        {
            if (character == null)
                continue;

            foreach (string skillId in GetCharacterUniqueSkillIds(character))
            {
                if (string.IsNullOrWhiteSpace(skillId))
                    continue;

                result.Add(skillId);

                if (dataManager.SkillDatabase != null &&
                    dataManager.SkillDatabase.TryGet(skillId, out SkillMasterData resolvedSkill) &&
                    resolvedSkill != null &&
                    !string.IsNullOrWhiteSpace(resolvedSkill.SkillId))
                {
                    result.Add(resolvedSkill.SkillId);
                }
            }
        }

        return result;
    }

    private static CharacterMasterData FindCharacterForSection(DataManager dataManager, string sectionName)
    {
        if (dataManager?.CharacterDatabase == null || string.IsNullOrWhiteSpace(sectionName))
            return null;

        string normalizedSectionName = NormalizeKey(sectionName);
        foreach (CharacterMasterData character in dataManager.CharacterDatabase.GetAll().Values)
        {
            if (character == null)
                continue;

            if (MatchesCharacterSectionName(character, normalizedSectionName))
                return character;
        }

        return null;
    }

    private static bool MatchesCharacterSectionName(CharacterMasterData character, string normalizedSectionName)
    {
        if (character == null)
            return false;

        if (NormalizeKey(character.CharacterId) == normalizedSectionName)
            return true;

        if (NormalizeKey(character.Name) == normalizedSectionName)
            return true;

        string localizedName = GameDataLocalization.CharacterName(character);
        if (NormalizeKey(localizedName) == normalizedSectionName)
            return true;

        foreach (string alias in GetCharacterAliases())
        {
            string[] pair = alias.Split('|');
            if (pair.Length != 2)
                continue;

            if (NormalizeKey(pair[0]) != normalizedSectionName)
                continue;

            if (NormalizeKey(character.Name) == NormalizeKey(pair[1]) ||
                NormalizeKey(localizedName) == NormalizeKey(pair[1]) ||
                NormalizeKey(character.CharacterId).Contains(NormalizeKey(pair[0])))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> GetCharacterAliases()
    {
        yield return "Hilt|힐트";
        yield return "Kaya|카야";
        yield return "Haze|헤이즈";
        yield return "Ines|이네스";
        yield return "Reina|레이나";
    }

    private static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        char[] filtered = value
            .Where(c => !char.IsWhiteSpace(c) && c != '_' && c != '-')
            .ToArray();

        return new string(filtered).Trim().ToLowerInvariant();
    }

    private static bool IsCharacterUnlocked(DataManager dataManager, CharacterMasterData character)
    {
        if (character == null)
            return false;

        bool unlockedByDefault = character.IsDefaultProvided;
        if (dataManager?.CharacterRuntimeStore == null)
            return unlockedByDefault;

        if (dataManager.CharacterRuntimeStore.TryGet(character.CharacterId, out CharacterRuntimeData runtime) && runtime != null)
            return unlockedByDefault || runtime.IsUnlocked;

        return unlockedByDefault;
    }

    private static bool IsSkillRevealed(DataManager dataManager, string skillId, bool revealByDefault)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return false;

        if (TryGetDiscoveredIdSet(dataManager, "DiscoveredSkillIds", out HashSet<string> discovered))
            return discovered.Contains(skillId);

        return revealByDefault;
    }

    private static bool TryGetDiscoveredIdSet(DataManager dataManager, string fieldName, out HashSet<string> discoveredIds)
    {
        discoveredIds = null;

        object container = dataManager?.PlayerRuntimeStore?.Data;
        if (container == null || string.IsNullOrWhiteSpace(fieldName))
            return false;

        FieldInfo field = container.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (field == null)
            return false;

        object value = field.GetValue(container);
        if (value == null)
            return false;

        discoveredIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (value is IEnumerable enumerable)
        {
            foreach (object entry in enumerable)
            {
                string text = entry as string;
                if (!string.IsNullOrWhiteSpace(text))
                    discoveredIds.Add(text);
            }
        }

        return discoveredIds.Count > 0;
    }
}

public static class RecordDisplayNameResolver
{
    public static string SkillName(SkillMasterData data)
    {
        return data == null
            ? string.Empty
            : ResolveDisplayName(data, data.SkillId, data.Name, GameDataLocalization.SkillName);
    }

    public static string RuneName(RuneData data)
    {
        return data == null
            ? string.Empty
            : ResolveDisplayName(data, data.RuneId, data.Name, GameDataLocalization.RuneName);
    }

    public static string RelicName(RelicData data)
    {
        return data == null
            ? string.Empty
            : ResolveDisplayName(data, data.FragmentId, data.Name, GameDataLocalization.RelicName);
    }

    public static string ItemName(ItemData data)
    {
        return data == null
            ? string.Empty
            : ResolveDisplayName(data, data.ItemId, data.Name, GameDataLocalization.ItemName);
    }

    public static string ResolveDisplayName<T>(
        T data,
        string fallbackId,
        string sourceName,
        Func<T, string> localizer)
    {
        if (data == null)
            return string.Empty;

        string localizedName = null;
        if (localizer != null)
        {
            try
            {
                localizedName = localizer(data);
            }
            catch (Exception)
            {
                localizedName = null;
            }
        }

        if (!string.IsNullOrWhiteSpace(localizedName))
            return localizedName;

        if (!string.IsNullOrWhiteSpace(sourceName))
            return sourceName;

        return fallbackId?.Trim() ?? string.Empty;
    }
}
