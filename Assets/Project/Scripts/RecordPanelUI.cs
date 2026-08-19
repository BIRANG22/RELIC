using System;
using System.Collections.Generic;
using System.Linq;
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
        Skill,
        Fragment,
        Relic,
        Item
    }

    [Header("Main Tab Panels")]
    [SerializeField] private GameObject skillContent;
    [SerializeField] private GameObject fragmentContent;
    [SerializeField] private GameObject relicContent;
    [SerializeField] private GameObject itemContent;

    [Header("Main Tab Buttons")]
    [Tooltip("기억(Skill) 메인 탭 버튼을 연결합니다.")]
    [SerializeField] private Button skillTabButton;
    [Tooltip("파편(Fragment) 메인 탭 버튼을 연결합니다.")]
    [SerializeField] private Button fragmentTabButton;
    [Tooltip("유물(Relic) 메인 탭 버튼을 연결합니다.")]
    [SerializeField] private Button relicTabButton;
    [Tooltip("아이템(Item) 메인 탭 버튼을 연결합니다.")]
    [SerializeField] private Button itemTabButton;

    [Header("Grid Contents")]
    [Tooltip("SkillContent 안의 Scroll View/Viewport/Content를 연결합니다.")]
    [SerializeField] private RectTransform skillGridContent;
    [Tooltip("FragmentContent 안의 Scroll View/Viewport/Content를 연결합니다.")]
    [SerializeField] private RectTransform fragmentGridContent;
    [Tooltip("RelicContent 안의 Scroll View/Viewport/Content를 연결합니다.")]
    [SerializeField] private RectTransform relicGridContent;
    [Tooltip("ItemContent 안의 Scroll View/Viewport/Content를 연결합니다.")]
    [SerializeField] private RectTransform itemGridContent;

    [Header("Scroll Rects")]
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

    private readonly List<RecordIconSlotUI> spawnedSlots = new();
    private RecordIconSlotUI selectedSlot;
    private MainTab currentMainTab = MainTab.Skill;
    private ColorBlock skillTabOriginalColors;
    private ColorBlock fragmentTabOriginalColors;
    private ColorBlock relicTabOriginalColors;
    private ColorBlock itemTabOriginalColors;
    private bool mainTabColorsCached;

    private void Awake()
    {
        ApplyGridConstraints();
        CacheMainTabButtonColors();
    }

    private void OnEnable()
    {
        ShowSkillTab();
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

    public void ShowSkillTab()
    {
        BuildSkillList();
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

    public void ShowSkillPassive() => BuildSkillList(Category.Passive);
    public void ShowSkillUnique() => BuildSkillList(Category.Unique);
    public void ShowSkillAbility() => BuildSkillList(Category.Ability);
    public void ShowSkillPublic() => BuildSkillList(Category.Public);
    public void ShowSkillCore() => BuildSkillList(Category.Core);

    // 기존 버튼 연결이 남아 있어도 전체 룬 목록을 표시하도록 유지합니다.
    public void ShowCommonRunes() => BuildRuneList();
    public void ShowExclusiveRunes() => BuildRuneList();

    public void ShowPassiveRelics() => BuildRelicList(false);
    public void ShowActiveRelics() => BuildRelicList(true);

    private void BuildSkillList()
    {
        SetMainTab(MainTab.Skill);
        ClearCurrentSlots();

        DataManager dataManager = GetDataManager();
        if (dataManager == null || dataManager.SkillDatabase == null)
            return;

        IEnumerable<SkillMasterData> skills = dataManager.SkillDatabase.GetAll()
            .Where(skill => skill != null && skill.Level != 2)
            .OrderBy(RecordDisplayNameResolver.SkillName, StringComparer.CurrentCulture);

        CreateSkillSlots(dataManager, skills);
    }

    private void BuildSkillList(Category category)
    {
        SetMainTab(MainTab.Skill);
        ClearCurrentSlots();

        DataManager dataManager = GetDataManager();
        if (dataManager == null || dataManager.SkillDatabase == null)
            return;

        IEnumerable<SkillMasterData> skills = dataManager.SkillDatabase.GetAll()
            .Where(skill => skill != null && skill.Level != 2 && skill.Category == category)
            .OrderBy(RecordDisplayNameResolver.SkillName, StringComparer.CurrentCulture);

        CreateSkillSlots(dataManager, skills);
    }

    private void CreateSkillSlots(DataManager dataManager, IEnumerable<SkillMasterData> skills)
    {
        foreach (SkillMasterData skill in skills)
        {
            Sprite icon = skill.Icon;
            if (icon == null && dataManager.SkillIconDatabase != null)
                dataManager.SkillIconDatabase.TryGetIcon(skill.SkillId, out icon);

            CreateSlot(skillGridContent, icon, RecordDisplayNameResolver.SkillName(skill));
        }

        CompleteListBuild(skillScrollRect);
    }

    private void BuildRuneList()
    {
        SetMainTab(MainTab.Fragment);
        ClearCurrentSlots();

        DataManager dataManager = GetDataManager();
        if (dataManager == null || dataManager.RuneDatabase == null)
            return;

        // Fragment 탭에서는 공용룬과 캐릭터 전용룬을 구분하지 않고
        // GameData의 Rune 시트에 등록된 모든 룬을 표시합니다.
        IEnumerable<RuneData> runes = dataManager.RuneDatabase.GetAll()
            .Where(rune => rune != null)
            .OrderBy(RecordDisplayNameResolver.RuneName, StringComparer.CurrentCulture);

        foreach (RuneData rune in runes)
        {
            Sprite icon = null;
            if (dataManager.RuneIconDatabase != null)
                dataManager.RuneIconDatabase.TryGetIcon(rune.RuneId, out icon);

            CreateSlot(fragmentGridContent, icon, RecordDisplayNameResolver.RuneName(rune));
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
            Sprite icon = null;
            if (dataManager.RelicIconDatabase != null)
                dataManager.RelicIconDatabase.TryGetIcon(relic.FragmentId, out icon);

            CreateSlot(relicGridContent, icon, RecordDisplayNameResolver.RelicName(relic));
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
            .OrderBy(item => item.ItemId, StringComparer.Ordinal);

        foreach (ItemData item in items)
        {
            Sprite icon = null;
            if (dataManager.ItemIconDatabase != null)
                dataManager.ItemIconDatabase.TryGetIcon(item.ItemId, out icon);

            CreateSlot(itemGridContent, icon, RecordDisplayNameResolver.ItemName(item));
        }

        CompleteListBuild(itemScrollRect);
    }

    private void SetMainTab(MainTab tab)
    {
        currentMainTab = tab;

        SetActive(skillContent, tab == MainTab.Skill);
        SetActive(fragmentContent, tab == MainTab.Fragment);
        SetActive(relicContent, tab == MainTab.Relic);
        SetActive(itemContent, tab == MainTab.Item);

        SelectMainTabButton(tab);
    }

    private void SelectMainTabButton(MainTab tab)
    {
        CacheMainTabButtonColors();

        ApplyMainTabButtonColors(skillTabButton, skillTabOriginalColors, tab == MainTab.Skill);
        ApplyMainTabButtonColors(fragmentTabButton, fragmentTabOriginalColors, tab == MainTab.Fragment);
        ApplyMainTabButtonColors(relicTabButton, relicTabOriginalColors, tab == MainTab.Relic);
        ApplyMainTabButtonColors(itemTabButton, itemTabOriginalColors, tab == MainTab.Item);
    }

    private void CacheMainTabButtonColors()
    {
        if (mainTabColorsCached)
            return;

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
            colors.normalColor = originalColors.selectedColor;

        button.colors = colors;
    }

    private Button GetMainTabButton(MainTab tab)
    {
        return tab switch
        {
            MainTab.Skill => skillTabButton,
            MainTab.Fragment => fragmentTabButton,
            MainTab.Relic => relicTabButton,
            MainTab.Item => itemTabButton,
            _ => null
        };
    }

    private void CreateSlot(RectTransform parent, Sprite icon, string displayName)
    {
        if (parent == null || iconSlotPrefab == null)
            return;

        RecordIconSlotUI slot = Instantiate(iconSlotPrefab, parent, false);
        slot.Initialize(icon, displayName, OnSlotClicked);
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

    private void ClearCurrentSlots()
    {
        selectedSlot = null;

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
    }

    private void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
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
