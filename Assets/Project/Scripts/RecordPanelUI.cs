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
        Compound,
        Item
    }

    private const string UnknownDisplayName = "???";
    private const string UnknownDescription = "아직 기록되지 않았습니다";

    [Header("Main Tab Panels")]
    [SerializeField] private GameObject uniqueContent;
    [SerializeField] private GameObject skillContent;
    [SerializeField] private GameObject fragmentContent;
    [SerializeField] private GameObject relicContent;
    [SerializeField] private GameObject compoundContent;
    [SerializeField] private GameObject itemContent;

    [Header("Main Tab Buttons")]
    [SerializeField] private Button uniqueTabButton;
    [SerializeField] private Button skillTabButton;
    [SerializeField] private Button fragmentTabButton;
    [SerializeField] private Button relicTabButton;
    [SerializeField] private Button compoundTabButton;
    [SerializeField] private Button itemTabButton;

    [Header("Record Numbers")]
    [SerializeField] private TMP_Text uniqueNumberText;
    [SerializeField] private TMP_Text skillNumberText;
    [SerializeField] private TMP_Text fragmentNumberText;
    [SerializeField] private TMP_Text relicNumberText;
    [SerializeField] private TMP_Text compoundNumberText;
    [SerializeField] private TMP_Text itemNumberText;

    [Header("Grid Contents")]
    [SerializeField] private RectTransform uniqueRootContent;
    [SerializeField] private RectTransform skillGridContent;
    [SerializeField] private RectTransform fragmentGridContent;
    [SerializeField] private RectTransform relicGridContent;
    [SerializeField] private RectTransform compoundGridContent;
    [SerializeField] private RectTransform itemGridContent;

    [Header("Scroll Rects")]
    [SerializeField] private ScrollRect uniqueScrollRect;
    [SerializeField] private ScrollRect skillScrollRect;
    [SerializeField] private ScrollRect fragmentScrollRect;
    [SerializeField] private ScrollRect relicScrollRect;
    [SerializeField] private ScrollRect compoundScrollRect;
    [SerializeField] private ScrollRect itemScrollRect;

    [Header("Item Slot")]
    [SerializeField] private RecordIconSlotUI iconSlotPrefab;
    [SerializeField, Min(1)] private int iconsPerRow = 5;

    [Header("Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private string emptyNameText = string.Empty;
    [SerializeField] private string emptyDescriptionText = string.Empty;

    [Header("Rarity Colors")]
    [SerializeField] private Color commonRarityColor = Color.white;
    [SerializeField] private Color rareRarityColor = Color.white;
    [SerializeField] private Color epicRarityColor = Color.white;
    [SerializeField] private Color uniqueRarityColor = Color.white;
    [SerializeField] private Color exclusiveRarityColor = new Color(1f, 0.82f, 0.2f, 1f);

    [Header("Effect Value Color")]
    [Tooltip("유물 효과 설명에서 {ValueRate1}, {ValueRate2}, {CountRate1} 등으로 치환되는 수치의 강조 색상입니다.")]
    [SerializeField] private Color valueHighlightColor = Color.yellow;

    [Header("Fragment Info")]
    [SerializeField] private GameObject fragmentInfoPanel;
    [SerializeField] private TMP_Text fragmentUnlockText;

    [Header("Memory Info")]
    [SerializeField] private GameObject memoryInfoPanel;
    [SerializeField] private Image memoryRangeImage;
    [SerializeField] private GameObject memoryRangeQuestion;
    [SerializeField] private TMP_Text memoryMethodText;
    [SerializeField] private TMP_Text memoryConsumptionText;
    [SerializeField] private TMP_Text memoryPointText;

    [Header("Compound Info")]
    [SerializeField] private GameObject compoundInfoPanel;
    [SerializeField] private TMP_Text compoundTypeText;
    [SerializeField] private TMP_Text compoundUsesText;
    [SerializeField] private GameObject compoundItem01Question;
    [SerializeField] private Image compoundItem01Icon;
    [SerializeField] private GameObject compoundItem02Question;
    [SerializeField] private Image compoundItem02Icon;
    [SerializeField] private GameObject compoundItem03Question;
    [SerializeField] private Image compoundItem03Icon;

    [Header("Initial View")]
    [SerializeField] private bool selectFirstItemAutomatically = true;

    [Header("Record Scroll Padding")]
    [Tooltip("모든 도감 스크롤에서 마지막 슬롯이 Viewport 마스크에 잘리지 않도록 Content 하단에 추가하는 공통 여백입니다.")]
    [SerializeField, Min(0f)] private float contentBottomPadding = 40f;
    [Tooltip("모든 도감 스크롤에서 첫 슬롯이 Viewport 상단에 너무 붙거나 잘리지 않도록 Content 상단에 추가하는 공통 여백입니다.")]
    [SerializeField, Min(0f)] private float contentTopPadding = 20f;

    private readonly List<RecordIconSlotUI> spawnedSlots = new();
    private readonly Dictionary<RecordIconSlotUI, string> slotDescriptions = new();
    private readonly Dictionary<RecordIconSlotUI, string> slotRarityLabels = new();
    private readonly Dictionary<RecordIconSlotUI, string> slotRarities = new();
    private readonly Dictionary<RecordIconSlotUI, RuneData> slotRunes = new();
    private readonly Dictionary<RecordIconSlotUI, SkillMasterData> slotSkills = new();
    private readonly HashSet<RecordIconSlotUI> revealedSkillSlots = new();
    private readonly Dictionary<RecordIconSlotUI, CompoundData> slotCompounds = new();
    private readonly List<RecordIconSlotUI> activeFixedSlots = new();
    private RecordIconSlotUI selectedSlot;
    private MainTab currentMainTab = MainTab.Unique;

    private ColorBlock uniqueTabOriginalColors;
    private ColorBlock skillTabOriginalColors;
    private ColorBlock fragmentTabOriginalColors;
    private ColorBlock relicTabOriginalColors;
    private ColorBlock compoundTabOriginalColors;
    private ColorBlock itemTabOriginalColors;
    private bool mainTabColorsCached;
    private Color nameOriginalColor;
    private Color rarityOriginalColor;
    private bool infoColorsCached;
    private bool debugRevealAll;
    private Coroutine uniqueLayoutRefreshCoroutine;

    private void Awake()
    {
        EnsureReferences();
        ApplyGridConstraints();
        CacheMainTabButtonColors();
        CacheInfoTextColors();
        BindCompoundTabButton();
    }

    private void OnEnable()
    {
        RecordDiscoveryService.BackfillFromCurrentState(GetDataManager());
        EnsureReferences();
        RefreshRecordCounts();
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
            case MainTab.Compound:
                ShowCompoundTab();
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

    public void ShowCompoundTab()
    {
        BuildCompoundList();
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

    public void ShowPassiveRelics() => BuildRelicList();
    // 기존 프리팹/버튼 이벤트 호환용. 액티브 유물은 이제 연성제 탭으로 이동했습니다.
    public void ShowActiveRelics() => ShowCompoundTab();

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

        string description = revealSkill && skill != null
            ? FormatHighlightedSkillDescription(skill)
            : UnknownDescription;
        string rarity = skill != null ? SkillRarityUtility.GetCanonicalName(skill.Rarity) : string.Empty;
        slotDescriptions[slot] = description;
        slotRarityLabels[slot] = skill != null
            ? FormatRarityLabel(rarity, MainTab.Unique)
            : string.Empty;
        slotRarities[slot] = rarity;
        if (skill != null)
            slotSkills[slot] = skill;
        if (revealSkill && skill != null)
            revealedSkillSlots.Add(slot);
        ApplySlotRarityColor(slot, rarity);
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
            string description = revealSkill ? FormatHighlightedSkillDescription(skill) : UnknownDescription;
            string rarity = SkillRarityUtility.GetCanonicalName(skill.Rarity);
            RecordIconSlotUI slot = CreateSlot(
                skillGridContent,
                icon,
                displayName,
                revealSkill,
                description,
                FormatRarityLabel(rarity, MainTab.Skill),
                rarity);

            if (slot != null)
            {
                slotSkills[slot] = skill;
                if (revealSkill)
                    revealedSkillSlots.Add(slot);
            }
        }

        CompleteListBuild(skillScrollRect);
    }


    private static int GetLegacySkillRarityOrder(SkillMasterData skill)
    {
        if (skill == null)
            return int.MaxValue;

        return skill.Rarity switch
        {
            SkillRarity.Exclusive => 0,
            SkillRarity.Common => 1,
            SkillRarity.Rare => 2,
            SkillRarity.Epic => 3,
            SkillRarity.Unique => 4,
            SkillRarity.Move => 5,
            _ => 6
        };
    }

    private static int GetRuneRecordGroupOrder(RuneData rune)
    {
        if (rune == null)
            return int.MaxValue;

        return string.Equals(rune.Rarity, "Exclusive", StringComparison.OrdinalIgnoreCase)
            ? 0
            : 1;
    }

    private static int GetRuneCharacterOrder(RuneData rune)
    {
        if (rune == null || !string.Equals(rune.Rarity, "Exclusive", StringComparison.OrdinalIgnoreCase))
            return int.MaxValue;

        return GetTrailingIdNumber(rune.TargetCharacterId);
    }

    private static int GetRuneRarityOrder(RuneData rune)
    {
        if (rune == null || string.Equals(rune.Rarity, "Exclusive", StringComparison.OrdinalIgnoreCase))
            return 0;

        if (string.Equals(rune.Rarity, "Common", StringComparison.OrdinalIgnoreCase))
            return 0;
        if (string.Equals(rune.Rarity, "Rare", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (string.Equals(rune.Rarity, "Unique", StringComparison.OrdinalIgnoreCase))
            return 2;

        return 3;
    }

    private void BuildRuneList()
    {
        SetMainTab(MainTab.Fragment);
        ClearCurrentSlots();

        DataManager dataManager = GetDataManager();
        if (dataManager == null || dataManager.RuneDatabase == null)
            return;

        IEnumerable<RuneData> runes = dataManager.RuneDatabase.GetAll()
            .Where(rune => rune != null)
            .OrderBy(GetRuneRecordGroupOrder)
            .ThenBy(GetRuneCharacterOrder)
            .ThenBy(rune => string.Equals(rune.Rarity, "Exclusive", StringComparison.OrdinalIgnoreCase)
                ? Mathf.Max(0, rune.UnlockLevel)
                : GetRuneRarityOrder(rune))
            .ThenBy(rune => GetTrailingIdNumber(rune.RuneId))
            .ThenBy(rune => rune.RuneId, StringComparer.OrdinalIgnoreCase);

        foreach (RuneData rune in runes)
        {
            bool discovered = debugRevealAll || RecordDiscoveryService.IsRuneDiscovered(dataManager, rune.RuneId);
            Sprite icon = null;

            if (discovered && dataManager.RuneIconDatabase != null)
                dataManager.RuneIconDatabase.TryGetIcon(rune.RuneId, out icon);

            string displayName = discovered ? RecordDisplayNameResolver.RuneName(rune) : UnknownDisplayName;
            string description = discovered ? FormatRuneEffectDescription(rune) : UnknownDescription;

            RecordIconSlotUI slot = CreateSlot(
                fragmentGridContent,
                icon,
                displayName,
                discovered,
                description,
                FormatRarityLabel(rune.Rarity, MainTab.Fragment),
                rune.Rarity);

            if (slot != null)
                slotRunes[slot] = rune;
        }

        CompleteListBuild(fragmentScrollRect);
    }

    private string FormatRuneEffectDescription(RuneData rune)
    {
        if (rune == null)
            return string.Empty;

        string description = rune.EffectDesc;
        return FormatHighlightedEffectDescription(description, rune.ValueRate, rune.CountRate);
    }

    private void BuildRelicList()
    {
        SetMainTab(MainTab.Relic);
        ClearCurrentSlots();

        DataManager dataManager = GetDataManager();
        if (dataManager == null || dataManager.RelicDatabase == null)
            return;

        IEnumerable<RelicData> relics = dataManager.RelicDatabase.GetAll()
            .Where(relic => relic != null)
            .OrderBy(relic => GetRecordRarityOrder(relic.Rarity))
            .ThenBy(relic => GetTrailingIdNumber(relic.FragmentId))
            .ThenBy(relic => relic.FragmentId, StringComparer.OrdinalIgnoreCase);

        foreach (RelicData relic in relics)
        {
            bool discovered = debugRevealAll || RecordDiscoveryService.IsRelicDiscovered(dataManager, relic.FragmentId);
            Sprite icon = null;
            if (discovered && dataManager.RelicIconDatabase != null)
                dataManager.RelicIconDatabase.TryGetIcon(relic.FragmentId, out icon);

            string displayName = discovered ? RecordDisplayNameResolver.RelicName(relic) : UnknownDisplayName;
            string description = discovered ? FormatRelicEffectDescription(relic) : UnknownDescription;
            CreateSlot(relicGridContent, icon, displayName, discovered, description, FormatRarityLabel(relic.Rarity, MainTab.Relic), relic.Rarity);
        }

        CompleteListBuild(relicScrollRect);
    }


    private string FormatHighlightedSkillDescription(SkillMasterData skill)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.Details))
            return string.Empty;

        return FormatHighlightedEffectDescription(skill.Details, skill.ValueRate, skill.CountRate);
    }

    private string FormatRelicEffectDescription(RelicData relic)
    {
        if (relic == null || string.IsNullOrWhiteSpace(relic.EffectDesc))
            return string.Empty;

        return FormatHighlightedEffectDescription(relic.EffectDesc, relic.ValueRate, relic.CountRate);
    }

    private string FormatHighlightedEffectDescription(string description, string valueRate, string countRate)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        string result = description;
        string colorHex = ColorUtility.ToHtmlStringRGB(valueHighlightColor);

        result = ReplaceIndexedHighlightedValues(result, "ValueRate", valueRate, colorHex);
        result = ReplaceIndexedHighlightedValues(result, "CountRate", countRate, colorHex);

        // 기존 데이터의 {ValueRate}, {CountRate} 표기도 호환을 위해 유지합니다.
        result = ReplaceHighlightedValue(result, "{ValueRate}", valueRate, colorHex);
        result = ReplaceHighlightedValue(result, "{CountRate}", countRate, colorHex);

        return result;
    }

    private static string ReplaceIndexedHighlightedValues(string source, string tokenName, string values, string colorHex)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrWhiteSpace(tokenName))
            return source;

        string[] splitValues = string.IsNullOrWhiteSpace(values)
            ? Array.Empty<string>()
            : values.Split(';');

        for (int i = 0; i < splitValues.Length; i++)
        {
            string token = $"{{{tokenName}{i + 1}}}";
            if (!source.Contains(token))
                continue;

            string displayValue = GetDisplayRateValue(splitValues[i]);
            source = source.Replace(token, $"<color=#{colorHex}>{displayValue}</color>");
        }

        return source;
    }

    private static string ReplaceHighlightedValue(string source, string token, string value, string colorHex)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(token) || !source.Contains(token))
            return source;

        string displayValue = GetDisplayRateValue(value);
        return source.Replace(token, $"<color=#{colorHex}>{displayValue}</color>");
    }

    private static string GetDisplayRateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "?";

        string displayValue = value.Trim();

        // 실제 계산 데이터의 음수 부호는 유지하되, 설명에서는 '감소' 문구와 중복되지 않도록 부호를 숨깁니다.
        if (displayValue.Length > 1 &&
            displayValue[0] == '-' &&
            float.TryParse(
                displayValue.Substring(1),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out _))
        {
            return displayValue.Substring(1);
        }

        return displayValue;
    }

    private static int GetRecordRarityOrder(string rarity)
    {
        if (string.Equals(rarity, "Common", StringComparison.OrdinalIgnoreCase))
            return 0;
        if (string.Equals(rarity, "Rare", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (string.Equals(rarity, "Epic", StringComparison.OrdinalIgnoreCase))
            return 2;
        if (string.Equals(rarity, "Unique", StringComparison.OrdinalIgnoreCase))
            return 3;

        return 4;
    }

    private static int GetTrailingIdNumber(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return int.MaxValue;

        int end = relicId.Length - 1;
        while (end >= 0 && char.IsDigit(relicId[end]))
            end--;

        int start = end + 1;
        if (start >= relicId.Length)
            return int.MaxValue;

        return int.TryParse(relicId.Substring(start), out int number)
            ? number
            : int.MaxValue;
    }

    private void BuildCompoundList()
    {
        SetMainTab(MainTab.Compound);
        ClearCurrentSlots();

        DataManager dataManager = GetDataManager();
        if (dataManager == null || dataManager.CompoundDatabase == null)
            return;

        IEnumerable<CompoundData> compounds = dataManager.CompoundDatabase.GetAll()
            .Where(compound => compound != null)
            .OrderBy(compound => GetRecordRarityOrder(compound.Rarity))
            .ThenBy(compound => GetTrailingIdNumber(compound.CompoundId))
            .ThenBy(compound => compound.CompoundId, StringComparer.OrdinalIgnoreCase);

        foreach (CompoundData compound in compounds)
        {
            bool discovered = debugRevealAll || RecordDiscoveryService.IsCompoundDiscovered(dataManager, compound.CompoundId);
            Sprite icon = null;
            if (discovered)
                TryGetCompoundIcon(dataManager, compound.CompoundId, out icon);

            string displayName = discovered
                ? (string.IsNullOrWhiteSpace(compound.Name) ? compound.CompoundId : compound.Name)
                : UnknownDisplayName;
            string description = discovered ? compound.EffectDesc : UnknownDescription;
            RecordIconSlotUI slot = CreateSlot(compoundGridContent, icon, displayName, discovered, description, FormatRarityLabel(compound.Rarity, MainTab.Compound), compound.Rarity);
            if (slot != null)
                slotCompounds[slot] = compound;
        }

        CompleteListBuild(compoundScrollRect);
    }

    private void BuildItemList()
    {
        ClearCurrentSlots();

        DataManager dataManager = GetDataManager();
        if (dataManager == null || dataManager.ItemDatabase == null)
            return;

        IEnumerable<ItemData> items = dataManager.ItemDatabase.GetAll()
            .Where(item => item != null)
            .OrderBy(item => GetRecordRarityOrder(item.Rarity))
            .ThenBy(item => GetTrailingIdNumber(item.ItemId))
            .ThenBy(item => item.ItemId, StringComparer.OrdinalIgnoreCase);

        foreach (ItemData item in items)
        {
            bool discovered = debugRevealAll || RecordDiscoveryService.IsItemDiscovered(dataManager, item.ItemId);
            Sprite icon = null;

            if (discovered && dataManager.ItemIconDatabase != null)
                dataManager.ItemIconDatabase.TryGetIcon(item.ItemId, out icon);

            string displayName = discovered ? RecordDisplayNameResolver.ItemName(item) : UnknownDisplayName;
            string description = discovered
                ? GameDataLocalization.ItemDescription(item)
                : UnknownDescription;

            CreateSlot(itemGridContent, icon, displayName, discovered, description, FormatRarityLabel(item.Rarity, MainTab.Item), item.Rarity);
        }

        CompleteListBuild(itemScrollRect);
    }

    private void RefreshRecordCounts()
    {
        DataManager dataManager = GetDataManager();
        if (dataManager == null)
        {
            SetRecordCountText(uniqueNumberText, 0, 0);
            SetRecordCountText(skillNumberText, 0, 0);
            SetRecordCountText(fragmentNumberText, 0, 0);
            SetRecordCountText(relicNumberText, 0, 0);
            SetRecordCountText(compoundNumberText, 0, 0);
            SetRecordCountText(itemNumberText, 0, 0);
            return;
        }

        HashSet<string> uniqueSkillIds = GetUniqueRecordSkillIds(dataManager);
        int uniqueTotal = uniqueSkillIds.Count;
        int uniqueDiscovered = uniqueSkillIds.Count(skillId => RecordDiscoveryService.IsSkillDiscovered(dataManager, skillId));
        SetRecordCountText(uniqueNumberText, uniqueDiscovered, uniqueTotal);

        if (dataManager.SkillDatabase != null)
        {
            List<SkillMasterData> skills = dataManager.SkillDatabase.GetAll()
                .Where(skill => skill != null && skill.Level != 2)
                .Where(skill => skill.Category != Category.Move)
                .Where(skill => !uniqueSkillIds.Contains(skill.SkillId))
                .ToList();

            int discovered = skills.Count(skill => RecordDiscoveryService.IsSkillDiscovered(dataManager, skill.SkillId));
            SetRecordCountText(skillNumberText, discovered, skills.Count);
        }
        else
        {
            SetRecordCountText(skillNumberText, 0, 0);
        }

        if (dataManager.RuneDatabase != null)
        {
            List<RuneData> runes = dataManager.RuneDatabase.GetAll().Where(rune => rune != null).ToList();
            int discovered = runes.Count(rune => RecordDiscoveryService.IsRuneDiscovered(dataManager, rune.RuneId));
            SetRecordCountText(fragmentNumberText, discovered, runes.Count);
        }
        else
        {
            SetRecordCountText(fragmentNumberText, 0, 0);
        }

        if (dataManager.RelicDatabase != null)
        {
            List<RelicData> relics = dataManager.RelicDatabase.GetAll().Where(relic => relic != null).ToList();
            int discovered = relics.Count(relic => RecordDiscoveryService.IsRelicDiscovered(dataManager, relic.FragmentId));
            SetRecordCountText(relicNumberText, discovered, relics.Count);
        }
        else
        {
            SetRecordCountText(relicNumberText, 0, 0);
        }

        if (dataManager.CompoundDatabase != null)
        {
            List<CompoundData> compounds = dataManager.CompoundDatabase.GetAll().Where(compound => compound != null).ToList();
            int discovered = compounds.Count(compound => RecordDiscoveryService.IsCompoundDiscovered(dataManager, compound.CompoundId));
            SetRecordCountText(compoundNumberText, discovered, compounds.Count);
        }
        else
        {
            SetRecordCountText(compoundNumberText, 0, 0);
        }

        if (dataManager.ItemDatabase != null)
        {
            List<ItemData> items = dataManager.ItemDatabase.GetAll().Where(item => item != null).ToList();
            int discovered = items.Count(item => RecordDiscoveryService.IsItemDiscovered(dataManager, item.ItemId));
            SetRecordCountText(itemNumberText, discovered, items.Count);
        }
        else
        {
            SetRecordCountText(itemNumberText, 0, 0);
        }
    }

    private static HashSet<string> GetUniqueRecordSkillIds(DataManager dataManager)
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

                string canonicalSkillId = GetCanonicalSkillId(dataManager.SkillDatabase, skillId);
                if (!string.IsNullOrWhiteSpace(canonicalSkillId))
                    result.Add(canonicalSkillId.Trim());
            }
        }

        return result;
    }

    private static void SetRecordCountText(TMP_Text target, int discoveredCount, int totalCount)
    {
        if (target == null)
            return;

        int safeTotal = Mathf.Max(0, totalCount);
        int safeDiscovered = Mathf.Clamp(discoveredCount, 0, safeTotal);
        target.text = $"{safeDiscovered}/{safeTotal}";
    }

    private void SetMainTab(MainTab tab)
    {
        currentMainTab = tab;

        SetActive(uniqueContent, tab == MainTab.Unique);
        SetActive(skillContent, tab == MainTab.Skill);
        SetActive(fragmentContent, tab == MainTab.Fragment);
        SetActive(relicContent, tab == MainTab.Relic);
        SetActive(compoundContent, tab == MainTab.Compound);
        SetActive(itemContent, tab == MainTab.Item);
        SetActive(fragmentInfoPanel, tab == MainTab.Fragment);
        SetActive(memoryInfoPanel, tab == MainTab.Unique || tab == MainTab.Skill);
        SetActive(compoundInfoPanel, tab == MainTab.Compound);
        if (rarityText != null)
            rarityText.gameObject.SetActive(tab == MainTab.Unique || tab == MainTab.Skill || tab == MainTab.Fragment || tab == MainTab.Relic || tab == MainTab.Compound || tab == MainTab.Item);
        RefreshRecordCounts();

        if (tab != MainTab.Fragment)
            ClearFragmentInfo();

        if (tab != MainTab.Unique && tab != MainTab.Skill)
            ClearMemoryInfo();

        if (tab != MainTab.Compound)
            ClearCompoundInfo();

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
        ApplyMainTabButtonColors(compoundTabButton, compoundTabOriginalColors, tab == MainTab.Compound);
        ApplyMainTabButtonColors(itemTabButton, itemTabOriginalColors, tab == MainTab.Item);

        ApplyMainTabAnimationState(uniqueTabButton, tab == MainTab.Unique);
        ApplyMainTabAnimationState(skillTabButton, tab == MainTab.Skill);
        ApplyMainTabAnimationState(fragmentTabButton, tab == MainTab.Fragment);
        ApplyMainTabAnimationState(relicTabButton, tab == MainTab.Relic);
        ApplyMainTabAnimationState(compoundTabButton, tab == MainTab.Compound);
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

        if (compoundTabButton != null)
            compoundTabOriginalColors = compoundTabButton.colors;

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


    private static bool TryGetCompoundIcon(DataManager dataManager, string compoundId, out Sprite icon)
    {
        icon = null;

        if (dataManager == null || dataManager.RelicIconDatabase == null || string.IsNullOrWhiteSpace(compoundId))
            return false;

        string id = compoundId.Trim();
        if (dataManager.RelicIconDatabase.TryGetIcon(id, out icon) && icon != null)
            return true;

        const string compoundPrefix = "Compound_";
        if (!id.StartsWith(compoundPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string suffix = id.Substring(compoundPrefix.Length);
        string legacyRelicId = $"Relic_A_{suffix}";
        return dataManager.RelicIconDatabase.TryGetIcon(legacyRelicId, out icon) && icon != null;
    }

    private RecordIconSlotUI CreateSlot(
        RectTransform parent,
        Sprite icon,
        string displayName,
        bool showIcon,
        string description = "",
        string rarityLabel = "",
        string rarity = "")
    {
        if (parent == null || iconSlotPrefab == null)
            return null;

        RecordIconSlotUI slot = Instantiate(iconSlotPrefab, parent, false);
        slot.Initialize(icon, displayName, OnSlotClicked, showIcon);
        spawnedSlots.Add(slot);
        slotDescriptions[slot] = description ?? string.Empty;
        slotRarityLabels[slot] = rarityLabel ?? string.Empty;
        slotRarities[slot] = rarity ?? string.Empty;
        ApplySlotRarityColor(slot, rarity);
        return slot;
    }

    private void ApplySlotRarityColor(RecordIconSlotUI slot, string rarity)
    {
        if (slot == null || string.IsNullOrWhiteSpace(rarity))
            return;

        Transform backTransform = slot.transform.Find("Back") ?? FindDeepChild(slot.transform, "Back");
        if (backTransform == null)
            return;

        Image backImage = backTransform.GetComponent<Image>();
        if (backImage != null)
        {
            Color rarityColor = GetRarityColor(rarity);
            Color currentColor = backImage.color;
            rarityColor.a = currentColor.a;
            backImage.color = rarityColor;
        }
    }

    private void OnSlotClicked(RecordIconSlotUI clickedSlot, string displayName)
    {
        SelectSlot(clickedSlot);
        SetInfo(displayName, GetSlotDescription(clickedSlot));
        RefreshRarityInfo(clickedSlot);
        RefreshFragmentInfo(clickedSlot);
        RefreshMemoryInfo(clickedSlot);
        RefreshCompoundInfo(clickedSlot);
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
            SetInfo(firstSlot.DisplayName, GetSlotDescription(firstSlot));
            RefreshRarityInfo(firstSlot);
            RefreshFragmentInfo(firstSlot);
            RefreshMemoryInfo(firstSlot);
            RefreshCompoundInfo(firstSlot);
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
        slotDescriptions.Clear();
        slotRarityLabels.Clear();
        slotRarities.Clear();
        slotRunes.Clear();
        slotSkills.Clear();
        revealedSkillSlots.Clear();
        slotCompounds.Clear();
        SetInfo(emptyNameText, emptyDescriptionText);
        ClearRarityInfo();
        ClearFragmentInfo();
        ClearMemoryInfo();
        ClearCompoundInfo();
    }

    private string GetSlotDescription(RecordIconSlotUI slot)
    {
        if (slot != null && slotDescriptions.TryGetValue(slot, out string description))
            return description ?? string.Empty;

        return string.Empty;
    }

    private void SetInfo(string name, string description)
    {
        SetName(name);
        SetDescription(description);
    }

    private void SetName(string value)
    {
        if (nameText != null)
            nameText.text = value ?? string.Empty;
    }

    private void SetDescription(string value)
    {
        if (descriptionText != null)
            descriptionText.text = value ?? string.Empty;
    }

    private void RefreshRarityInfo(RecordIconSlotUI slot)
    {
        bool showRarity = currentMainTab == MainTab.Unique
            || currentMainTab == MainTab.Skill
            || currentMainTab == MainTab.Fragment
            || currentMainTab == MainTab.Relic
            || currentMainTab == MainTab.Compound
            || currentMainTab == MainTab.Item;

        CacheInfoTextColors();

        if (rarityText != null)
        {
            rarityText.gameObject.SetActive(showRarity);
            rarityText.text = showRarity && slot != null && slotRarityLabels.TryGetValue(slot, out string label)
                ? label ?? string.Empty
                : string.Empty;
        }

        if (!showRarity || slot == null || !slotRarities.TryGetValue(slot, out string rarity))
        {
            RestoreInfoTextColors();
            return;
        }

        Color rarityColor = GetRarityColor(rarity);
        if (rarityText != null)
            rarityText.color = rarityColor;
    }

    private void ClearRarityInfo()
    {
        bool showRarity = currentMainTab == MainTab.Unique
            || currentMainTab == MainTab.Skill
            || currentMainTab == MainTab.Fragment
            || currentMainTab == MainTab.Relic
            || currentMainTab == MainTab.Compound
            || currentMainTab == MainTab.Item;

        if (rarityText != null)
        {
            rarityText.gameObject.SetActive(showRarity);
            rarityText.text = string.Empty;
        }

        RestoreInfoTextColors();
    }

    private void CacheInfoTextColors()
    {
        if (infoColorsCached)
            return;

        if (nameText == null || rarityText == null)
            return;

        nameOriginalColor = nameText.color;
        rarityOriginalColor = rarityText.color;
        infoColorsCached = true;
    }

    private void RestoreInfoTextColors()
    {
        if (!infoColorsCached)
            return;

        if (nameText != null)
            nameText.color = nameOriginalColor;
        if (rarityText != null)
            rarityText.color = rarityOriginalColor;
    }

    /// <summary>
    /// 도감에서 실제 사용하는 레어리티 색상을 외부 UI에서도 동일하게 사용할 수 있도록 반환합니다.
    /// </summary>
    public Color GetRarityDisplayColor(string rarity)
    {
        return GetRarityColor(rarity);
    }

    private Color GetRarityColor(string rarity)
    {
        if (string.Equals(rarity, "Exclusive", StringComparison.OrdinalIgnoreCase))
            return exclusiveRarityColor;
        if (string.Equals(rarity, "Rare", StringComparison.OrdinalIgnoreCase))
            return rareRarityColor;
        if (string.Equals(rarity, "Epic", StringComparison.OrdinalIgnoreCase))
            return epicRarityColor;
        if (string.Equals(rarity, "Unique", StringComparison.OrdinalIgnoreCase))
            return uniqueRarityColor;

        return commonRarityColor;
    }

    private static string FormatRarityLabel(string rarity, MainTab tab)
    {
        string normalized = string.IsNullOrWhiteSpace(rarity) ? string.Empty : rarity.Trim();

        if (tab == MainTab.Unique || tab == MainTab.Skill)
        {
            if (string.Equals(normalized, "Exclusive", StringComparison.OrdinalIgnoreCase)) return "고유 기억";
            if (string.Equals(normalized, "Common", StringComparison.OrdinalIgnoreCase)) return "일반 기억";
            if (string.Equals(normalized, "Rare", StringComparison.OrdinalIgnoreCase)) return "레어 기억";
            if (string.Equals(normalized, "Epic", StringComparison.OrdinalIgnoreCase)) return "에픽 기억";
            if (string.Equals(normalized, "Unique", StringComparison.OrdinalIgnoreCase)) return "유니크 기억";
        }
        else if (tab == MainTab.Fragment)
        {
            if (string.Equals(normalized, "Exclusive", StringComparison.OrdinalIgnoreCase)) return "고유 파편";
            if (string.Equals(normalized, "Common", StringComparison.OrdinalIgnoreCase)) return "각인 파편";
            if (string.Equals(normalized, "Rare", StringComparison.OrdinalIgnoreCase)) return "일반 파편";
            if (string.Equals(normalized, "Unique", StringComparison.OrdinalIgnoreCase)) return "축복 파편";
        }
        else if (tab == MainTab.Item)
        {
            if (string.Equals(normalized, "Common", StringComparison.OrdinalIgnoreCase)) return "일반 재료";
            if (string.Equals(normalized, "Rare", StringComparison.OrdinalIgnoreCase)) return "레어 재료";
            if (string.Equals(normalized, "Epic", StringComparison.OrdinalIgnoreCase)) return "에픽 재료";
        }
        else if (tab == MainTab.Compound)
        {
            if (string.Equals(normalized, "Common", StringComparison.OrdinalIgnoreCase)) return "일반 연성제";
            if (string.Equals(normalized, "Rare", StringComparison.OrdinalIgnoreCase)) return "레어 연성제";
            if (string.Equals(normalized, "Epic", StringComparison.OrdinalIgnoreCase)) return "에픽 연성제";
        }
        else if (tab == MainTab.Relic)
        {
            if (string.Equals(normalized, "Common", StringComparison.OrdinalIgnoreCase)) return "일반 유물";
            if (string.Equals(normalized, "Rare", StringComparison.OrdinalIgnoreCase)) return "레어 유물";
            if (string.Equals(normalized, "Epic", StringComparison.OrdinalIgnoreCase)) return "에픽 유물";
            if (string.Equals(normalized, "Unique", StringComparison.OrdinalIgnoreCase)) return "유니크 유물";
        }

        return normalized;
    }



    private void RefreshFragmentInfo(RecordIconSlotUI slot)
    {
        if (currentMainTab != MainTab.Fragment || fragmentInfoPanel == null)
            return;

        if (slot == null || !slotRunes.TryGetValue(slot, out RuneData rune) || rune == null)
        {
            ClearFragmentInfo();
            return;
        }

        if (fragmentUnlockText == null)
            return;

        if (string.Equals(rune.Rarity, "Exclusive", StringComparison.OrdinalIgnoreCase))
        {
            fragmentUnlockText.text = $"획득 조건 : 캐릭터 Lv.{Mathf.Max(0, rune.UnlockLevel)} 도달";
            return;
        }

        fragmentUnlockText.text = $"획득 조건 : 블루 더스티움 {Mathf.Max(0, rune.BlueDustiumCost)}";
    }

    private void ClearFragmentInfo()
    {
        if (fragmentUnlockText != null)
            fragmentUnlockText.text = string.Empty;
    }

    private void RefreshMemoryInfo(RecordIconSlotUI slot)
    {
        if ((currentMainTab != MainTab.Unique && currentMainTab != MainTab.Skill) || memoryInfoPanel == null)
            return;

        if (slot == null || !slotSkills.TryGetValue(slot, out SkillMasterData skill) || skill == null)
        {
            ClearMemoryInfo();
            return;
        }

        if (!revealedSkillSlots.Contains(slot))
        {
            SetUnknownMemoryInfo();
            return;
        }

        SetMemoryRangeQuestion(false);
        SetMemoryRangeImage(ResolveSkillRangeIcon(skill.RangeId));

        if (memoryMethodText != null)
            memoryMethodText.text = $"방식 : {GetMemoryMethodTypeDisplayName(skill.RangeType)}";

        if (memoryConsumptionText != null)
            memoryConsumptionText.text = $"소모 : {GetMemoryConsumptionDisplay(skill)}";

        List<SkillEffectEntry> entries = skill.EffectEntries;
        if ((entries == null || entries.Count == 0) && DataManager.Instance != null)
            entries = SkillEffectParser.Parse(skill, DataManager.Instance.EffectDatabase);

        if (memoryPointText != null)
            memoryPointText.text = $"효과 : {GetMemoryPointDisplay(entries)}";
    }

    private void ClearMemoryInfo()
    {
        SetMemoryRangeQuestion(false);
        SetMemoryRangeImage(null);

        if (memoryMethodText != null)
            memoryMethodText.text = string.Empty;

        if (memoryConsumptionText != null)
            memoryConsumptionText.text = string.Empty;

        if (memoryPointText != null)
            memoryPointText.text = string.Empty;
    }

    private void SetUnknownMemoryInfo()
    {
        SetMemoryRangeImage(null);
        SetMemoryRangeQuestion(true);

        if (memoryMethodText != null)
            memoryMethodText.text = "방식 : ???";

        if (memoryConsumptionText != null)
            memoryConsumptionText.text = "소모 : ???";

        if (memoryPointText != null)
            memoryPointText.text = "효과 : ???";
    }

    private void SetMemoryRangeQuestion(bool active)
    {
        if (memoryRangeQuestion != null)
            memoryRangeQuestion.SetActive(active);
    }

    private void SetMemoryRangeImage(Sprite sprite)
    {
        if (memoryRangeImage == null)
            return;

        bool hasSprite = sprite != null;
        memoryRangeImage.sprite = sprite;
        memoryRangeImage.preserveAspect = true;
        memoryRangeImage.enabled = hasSprite;
        memoryRangeImage.gameObject.SetActive(hasSprite);
    }

    private static Sprite ResolveSkillRangeIcon(string rangeId)
    {
        if (string.IsNullOrWhiteSpace(rangeId) ||
            DataManager.Instance == null ||
            DataManager.Instance.SkillRangeIconDatabase == null)
        {
            return null;
        }

        return DataManager.Instance.SkillRangeIconDatabase.TryGetIcon(rangeId, out Sprite icon)
            ? icon
            : null;
    }

    private static string GetMemoryMethodTypeDisplayName(RangeType rangeType)
    {
        switch (rangeType)
        {
            case RangeType.Direction:
                return "시전자 위치";
            case RangeType.Selection:
                return "그리드 선택";
            case RangeType.Passive:
                return "카르마 최대 시 지속";
            default:
                return string.Empty;
        }
    }

    private static string GetMemoryConsumptionDisplay(SkillMasterData skill)
    {
        if (skill == null)
            return string.Empty;

        if (skill.ResourceCostValue <= 0)
            return "소모 없음";

        string resourceName;
        switch (skill.ReferenceResource)
        {
            case ReferenceResource.HP:
                resourceName = "생명력";
                break;
            case ReferenceResource.UniqueResource:
                resourceName = "카르마";
                break;
            case ReferenceResource.Cost:
                resourceName = "마나";
                break;
            case ReferenceResource.MovePoint:
                resourceName = "이동";
                break;
            default:
                resourceName = string.Empty;
                break;
        }

        if (string.IsNullOrEmpty(resourceName))
            return Mathf.Max(0, skill.ResourceCostValue).ToString();

        return $"{resourceName} {Mathf.Max(0, skill.ResourceCostValue)}";
    }

    private static string GetMemoryPointDisplay(List<SkillEffectEntry> entries)
    {
        if (entries == null || entries.Count == 0)
            return "없음";

        List<string> parts = new List<string>(2);
        int count = Mathf.Min(2, entries.Count);
        for (int i = 0; i < count; i++)
        {
            SkillEffectEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.EffectId))
                continue;

            string effectName = GetMemoryEffectDisplayName(entry);
            int effectValue = entry.ValueAmount != 0 ? entry.ValueAmount : entry.CountAmount;
            string valueText = Mathf.Abs(effectValue).ToString();
            parts.Add(string.IsNullOrWhiteSpace(effectName) ? valueText : $"{effectName} {valueText}");
        }

        return parts.Count > 0 ? string.Join(" / ", parts) : "없음";
    }

    private static string GetMemoryEffectDisplayName(SkillEffectEntry entry)
    {
        if (entry == null)
            return string.Empty;

        string effectName = entry.EffectData != null && !string.IsNullOrWhiteSpace(entry.EffectData.Name)
            ? GameDataLocalization.EffectName(entry.EffectData)
            : entry.EffectId;

        string normalized = effectName.Replace(" ", string.Empty).ToLowerInvariant();
        if (normalized.Contains("타격") || normalized.Contains("strike"))
            return "피해";

        return effectName;
    }

    private void RefreshCompoundInfo(RecordIconSlotUI slot)
    {
        if (currentMainTab != MainTab.Compound || compoundInfoPanel == null)
            return;

        if (slot == null || !slotCompounds.TryGetValue(slot, out CompoundData compound) || compound == null)
        {
            ClearCompoundInfo();
            return;
        }

        DataManager dataManager = GetDataManager();
        bool discovered = debugRevealAll || RecordDiscoveryService.IsCompoundDiscovered(dataManager, compound.CompoundId);

        if (compoundTypeText != null)
            compoundTypeText.text = discovered
                ? $"사용 대상 : {GetCompoundTargetTypeLabel(compound.TargetType)}"
                : "사용 대상 : ???";

        if (compoundUsesText != null)
            compoundUsesText.text = discovered
                ? $"사용 가능 횟수 : {compound.Durability}"
                : "사용 가능 횟수 : ???";

        UpdateCompoundMaterialSlot(compound.MaterialId1, compoundItem01Question, compoundItem01Icon, dataManager);
        UpdateCompoundMaterialSlot(compound.MaterialId2, compoundItem02Question, compoundItem02Icon, dataManager);
        UpdateCompoundMaterialSlot(compound.MaterialId3, compoundItem03Question, compoundItem03Icon, dataManager);
    }

    private static string GetCompoundTargetTypeLabel(string targetType)
    {
        if (string.Equals(targetType, "Self", StringComparison.OrdinalIgnoreCase))
            return "자신";

        if (string.Equals(targetType, "Grid", StringComparison.OrdinalIgnoreCase))
            return "그리드";

        return string.IsNullOrWhiteSpace(targetType) ? "?" : targetType.Trim();
    }

    private void UpdateCompoundMaterialSlot(string materialId, GameObject question, Image iconImage, DataManager dataManager)
    {
        bool discovered = debugRevealAll || RecordDiscoveryService.IsItemDiscovered(dataManager, materialId);

        if (question != null)
            question.SetActive(!discovered);

        if (iconImage == null)
            return;

        iconImage.gameObject.SetActive(discovered);
        iconImage.sprite = null;

        if (!discovered || dataManager?.ItemIconDatabase == null || string.IsNullOrWhiteSpace(materialId))
            return;

        if (dataManager.ItemIconDatabase.TryGetIcon(materialId, out Sprite icon))
            iconImage.sprite = icon;
    }

    private void ClearCompoundInfo()
    {
        if (compoundTypeText != null)
            compoundTypeText.text = "?";

        if (compoundUsesText != null)
            compoundUsesText.text = "?";

        ClearCompoundMaterialSlot(compoundItem01Question, compoundItem01Icon);
        ClearCompoundMaterialSlot(compoundItem02Question, compoundItem02Icon);
        ClearCompoundMaterialSlot(compoundItem03Question, compoundItem03Icon);
    }

    private static void ClearCompoundMaterialSlot(GameObject question, Image iconImage)
    {
        if (question != null)
            question.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.gameObject.SetActive(false);
        }
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
        ApplyLayoutTopPadding(uniqueRootContent);
        ApplyGridConstraint(skillGridContent);
        ApplyGridConstraint(fragmentGridContent);
        ApplyGridConstraint(relicGridContent);
        ApplyGridConstraint(compoundGridContent);
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

        padding.top = Mathf.Max(0, Mathf.RoundToInt(contentTopPadding));
        padding.bottom = Mathf.Max(0, Mathf.RoundToInt(contentBottomPadding));
        grid.padding = padding;
    }

    private void ApplyLayoutTopPadding(RectTransform content)
    {
        if (content == null)
            return;

        LayoutGroup layout = content.GetComponent<LayoutGroup>();
        if (layout == null)
            return;

        RectOffset padding = layout.padding;
        if (padding == null)
            padding = new RectOffset();

        padding.top = Mathf.Max(0, Mathf.RoundToInt(contentTopPadding));
        layout.padding = padding;
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

        if (compoundContent == null)
            compoundContent = FindGameObjectByPath("Content/CompoundContent") ?? FindGameObjectByName("CompoundContent");

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

        if (compoundTabButton == null)
            compoundTabButton = FindButtonByPath("Buttons/Compound") ?? FindButtonByName("Compound");

        if (itemTabButton == null)
            itemTabButton = FindButtonByPath("Buttons/Item") ?? FindButtonByName("Item");

        if (uniqueNumberText == null)
            uniqueNumberText = FindTextByPathOrName("Number/Unique", "Unique");
        if (skillNumberText == null)
            skillNumberText = FindTextByPathOrName("Number/Skill", "Skill");
        if (fragmentNumberText == null)
            fragmentNumberText = FindTextByPathOrName("Number/Fragment", "Fragment");
        if (relicNumberText == null)
            relicNumberText = FindTextByPathOrName("Number/Relic", "Relic");
        if (compoundNumberText == null)
            compoundNumberText = FindTextByPathOrName("Number/Compound", "Compound");
        if (itemNumberText == null)
            itemNumberText = FindTextByPathOrName("Number/Item", "Item");

        if (uniqueRootContent == null && uniqueContent != null)
            uniqueRootContent = FindNestedRectTransform(uniqueContent.transform, "Scroll View/Viewport/Content");

        if (skillGridContent == null && skillContent != null)
            skillGridContent = FindNestedRectTransform(skillContent.transform, "Scroll View/Viewport/Content");

        if (fragmentGridContent == null && fragmentContent != null)
            fragmentGridContent = FindNestedRectTransform(fragmentContent.transform, "Scroll View/Viewport/Content");

        if (relicGridContent == null && relicContent != null)
            relicGridContent = FindNestedRectTransform(relicContent.transform, "Scroll View/Viewport/Content");

        if (compoundGridContent == null && compoundContent != null)
            compoundGridContent = FindNestedRectTransform(compoundContent.transform, "Scroll View/Viewport/Content");

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

        if (compoundScrollRect == null && compoundContent != null)
            compoundScrollRect = compoundContent.GetComponentInChildren<ScrollRect>(true);

        if (itemScrollRect == null && itemContent != null)
            itemScrollRect = itemContent.GetComponentInChildren<ScrollRect>(true);

        if (nameText == null)
            nameText = FindTextByPathOrName("Info/Name", "Name");

        if (rarityText == null)
            rarityText = FindTextByPathOrName("Info/Rarity", "Rarity");

        if (fragmentInfoPanel == null)
            fragmentInfoPanel = FindGameObjectByPath("Info/Fragment") ?? FindGameObjectByName("Fragment");

        if (fragmentUnlockText == null)
            fragmentUnlockText = FindTextByPathOrName("Info/Fragment/Unlock", "Unlock");

        if (memoryInfoPanel == null)
            memoryInfoPanel = FindGameObjectByPath("Info/Memory") ?? FindGameObjectByName("Memory");

        if (memoryRangeImage == null)
            memoryRangeImage = FindImageByPath("Info/Memory/Range");

        if (memoryRangeQuestion == null)
            memoryRangeQuestion = FindGameObjectByPath("Info/Memory/Range_Qus") ?? FindGameObjectByName("Range_Qus");

        if (memoryMethodText == null)
            memoryMethodText = FindTextByPathOrName("Info/Memory/method", "method");

        if (memoryConsumptionText == null)
            memoryConsumptionText = FindTextByPathOrName("Info/Memory/consumption", "consumption");

        if (memoryPointText == null)
            memoryPointText = FindTextByPathOrName("Info/Memory/Point", "Point");

        if (compoundInfoPanel == null)
            compoundInfoPanel = FindGameObjectByPath("Info/Compound") ?? FindGameObjectByName("Compound");

        if (compoundTypeText == null)
            compoundTypeText = FindTextByPathOrName("Info/Compound/Type", "Type");

        if (compoundUsesText == null)
            compoundUsesText = FindTextByPathOrName("Info/Compound/Uses", "Uses");

        if (compoundItem01Question == null)
            compoundItem01Question = FindGameObjectByPath("Info/Compound/Mixture/Item01/question");
        if (compoundItem01Icon == null)
            compoundItem01Icon = FindImageByPath("Info/Compound/Mixture/Item01/Icon");

        if (compoundItem02Question == null)
            compoundItem02Question = FindGameObjectByPath("Info/Compound/Mixture/Item02/question");
        if (compoundItem02Icon == null)
            compoundItem02Icon = FindImageByPath("Info/Compound/Mixture/Item02/Icon");

        if (compoundItem03Question == null)
            compoundItem03Question = FindGameObjectByPath("Info/Compound/Mixture/Item03/question");
        if (compoundItem03Icon == null)
            compoundItem03Icon = FindImageByPath("Info/Compound/Mixture/Item03/Icon");

        TMP_Text effectText = FindTextByPathOrName("Info/Effect", "Effect");
        if (effectText != null)
        {
            descriptionText = effectText;
        }
        else if (descriptionText == null)
        {
            descriptionText = FindTextByPathOrName("Info/Desc", "Desc")
                ?? FindTextByPathOrName("Info/Description", "Description");
        }
    }

    private void BindCompoundTabButton()
    {
        if (compoundTabButton == null)
            return;

        // 새 Compound 버튼의 Inspector OnClick 연결이 비어 있어도 도감에서 바로 사용할 수 있게 합니다.
        compoundTabButton.onClick.RemoveListener(ShowCompoundTab);
        compoundTabButton.onClick.AddListener(ShowCompoundTab);
    }

    private TMP_Text FindTextByPathOrName(string path, string objectName)
    {
        Transform byPath = transform.Find(path);
        if (byPath != null)
        {
            TMP_Text pathText = byPath.GetComponent<TMP_Text>();
            if (pathText != null)
                return pathText;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (!string.Equals(child.name, objectName, StringComparison.OrdinalIgnoreCase))
                continue;

            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text != null)
                return text;
        }

        return null;
    }

    private GameObject FindGameObjectByPath(string path)
    {
        Transform found = transform.Find(path);
        return found != null ? found.gameObject : null;
    }


    private Image FindImageByPath(string path)
    {
        Transform found = transform.Find(path);
        return found != null ? found.GetComponent<Image>() : null;
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

        float paddedContentHeight = contentHeight + contentTopPadding + contentBottomPadding;

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
