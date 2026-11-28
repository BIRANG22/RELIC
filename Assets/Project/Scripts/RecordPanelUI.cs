using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도감 탭과 캐릭터별 기억 목록을 관리합니다.
/// 아이콘 선택 시 Info 패널을 열고 닫는 기능을 제공합니다.
/// </summary>
public sealed class RecordPanelUI : MonoBehaviour
{
    private enum RecordTab { Skill, Fragment, Relic, Compound, Monster }

    [Header("Shared Record Layout")]
    [SerializeField] private RectTransform recordGridContent;
    [SerializeField] private ScrollRect recordScrollRect;
    [SerializeField] private RecordIconSlotUI iconSlotPrefab;

    [Header("Memory Layout")]
    [SerializeField] private RectTransform memoryRoot;
    [SerializeField] private RectTransform memoryScrollContent;
    [SerializeField] private RectTransform memoryCharacterButtons;
    [SerializeField] private Button memoryCharacterButtonPrefab;
    [SerializeField] private RectTransform memoryPassiveSlots;
    [SerializeField] private RectTransform memoryUltimateSlots;
    [SerializeField] private RectTransform memoryAbilitySlots;
    [SerializeField] private RectTransform memoryCoreSlots;

    [Header("Fragment Layout")]
    [SerializeField] private RectTransform fragmentRoot;
    [SerializeField] private RectTransform fragmentScrollContent;
    [SerializeField] private RectTransform fragmentCategoryButtons;
    [SerializeField] private RectTransform fragmentExclusiveSlots;
    [SerializeField] private RectTransform fragmentSharedSlots;
    [SerializeField, Min(0f)] private float fragmentScrollBottomPadding = 40f;

    [Header("Monster Layout")]
    [SerializeField] private RectTransform monsterRoot;
    [SerializeField] private RectTransform monsterButtonViewport;
    [SerializeField] private Image monsterPreviewImage;
    [SerializeField] private Button monsterPhaseOneButton;
    [SerializeField] private Button monsterPhaseTwoButton;

    [Header("Record Tab Buttons")]
    [SerializeField] private Button skillTabButton;
    [SerializeField] private Button fragmentTabButton;
    [SerializeField] private Button relicTabButton;
    [SerializeField] private Button compoundTabButton;
    [SerializeField] private Button monsterTabButton;

    [Header("Record Number")]
    [SerializeField] private TMP_Text skillNumberText;

    [Header("Rarity Colors (Shared With Other UI)")]
    [SerializeField] private Color commonRarityColor = Color.white;
    [SerializeField] private Color rareRarityColor = Color.white;
    [SerializeField] private Color epicRarityColor = Color.white;
    [SerializeField] private Color uniqueRarityColor = Color.white;
    [SerializeField] private Color exclusiveRarityColor = new Color(1f, 0.82f, 0.2f, 1f);

    private static bool hasCachedRarityPalette;
    private static Color cachedCommonRarityColor;
    private static Color cachedRareRarityColor;
    private static Color cachedEpicRarityColor;
    private static Color cachedUniqueRarityColor;
    private static Color cachedExclusiveRarityColor;

    private readonly List<RecordIconSlotUI> createdSlots = new();
    private readonly HashSet<RecordIconSlotUI> clickableInfoSlots = new();
    private readonly Dictionary<Button, ColorBlock> originalButtonColors = new();
    private readonly Dictionary<RecordIconSlotUI, string> monsterIdsBySlot = new();
    [Header("Name Tooltip")]
    [SerializeField] private RectTransform nameTooltip;
    [SerializeField] private TMP_Text nameTooltipText;
    [SerializeField] private float tooltipOffset = 8f;

    [Header("Record Info Popup")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private Button infoCancelButton;

    public bool IsInfoOpen => infoPanel != null && infoPanel.activeInHierarchy;

    private RecordIconSlotUI selectedInfoSlot;

    private RecordIconSlotUI hoveredSlot;
    private RecordIconSlotUI runtimeIconSlotTemplate;
    private RecordTab currentTab = RecordTab.Skill;
    private bool initialized;
    private bool debugRevealAll;
    private readonly List<Button> createdMemoryCharacterButtons = new();
    private string selectedMemoryCharacterId;
    private readonly List<Button> createdFragmentCategoryButtons = new();
    private readonly List<Button> createdMonsterButtons = new();
    private RectTransform defaultRecordViewport;
    private RectTransform monsterListContent;
    private Coroutine monsterPreviewAnimation;
    private Vector2? monsterPreviewLayoutPosition;
    private ColorBlock? monsterPhaseOneColors;
    private ColorBlock? monsterPhaseTwoColors;
    private UnityEngine.Events.UnityAction showMonsterPhaseOne;
    private UnityEngine.Events.UnityAction showMonsterPhaseTwo;

    private void Awake()
    {
        CacheRarityPalette();
        ResolveReferences();
        defaultRecordViewport = recordScrollRect != null ? recordScrollRect.viewport : null;
        BindButtons();
        CacheButtonColors();
        BindInfoCancelButton();
        initialized = true;
    }

    private void OnEnable()
    {
        CacheRarityPalette();
        ResolveReferences();
        if (!initialized)
        {
            BindButtons();
            CacheButtonColors();
            initialized = true;
        }
        BindInfoCancelButton();
        HideInfo();
        HideNameTooltip();
        ShowSkillTab();
    }

    private void OnDisable()
    {
        HideInfo();
        HideNameTooltip();
        ClearMonsterPreview();
    }

    public void Close()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.HideRecord();
        else
            gameObject.SetActive(false);
    }

    public void SetDebugRevealAll(bool revealAll)
    {
        debugRevealAll = revealAll;
        if (isActiveAndEnabled)
            ShowTab(currentTab);
    }

    public void ShowSkillTab() => ShowTab(RecordTab.Skill);
    public void ShowFragmentTab() => ShowTab(RecordTab.Fragment);
    public void ShowRelicTab() => ShowTab(RecordTab.Relic);
    public void ShowCompoundTab() => ShowTab(RecordTab.Compound);
    public void ShowMonsterTab() => ShowTab(RecordTab.Monster);

    public void OpenInfo()
    {
        OpenInfoForSlot(null);
    }

    private void OpenInfoForSlot(RecordIconSlotUI slot)
    {
        ResolveReferences();
        if (infoPanel == null) return;
        BindInfoCancelButton();
        HideNameTooltip();

        if (selectedInfoSlot != null && selectedInfoSlot != slot)
            selectedInfoSlot.SetSelected(false);
        selectedInfoSlot = slot;
        if (selectedInfoSlot != null)
            selectedInfoSlot.SetSelected(true);

        // Show immediately. Reset any CanvasGroup left by older fade versions.
        CanvasGroup group = infoPanel.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        infoPanel.SetActive(true);
        infoPanel.transform.SetAsLastSibling();
    }

    public void HideInfo()
    {
        if (selectedInfoSlot != null)
            selectedInfoSlot.SetSelected(false);
        selectedInfoSlot = null;
        if (infoPanel == null) return;
        infoPanel.SetActive(false);
    }

    public bool TryCloseInfo()
    {
        if (!IsInfoOpen) return false;
        HideInfo();
        return true;
    }

    private void BindInfoCancelButton()
    {
        ResolveReferences();
        if (infoCancelButton == null) return;
        infoCancelButton.onClick.RemoveListener(HideInfo);
        infoCancelButton.onClick.AddListener(HideInfo);
    }

    private void ResolveReferences()
    {
        if (infoPanel == null)
        {
            Transform info = transform.Find("Info");
            if (info != null) infoPanel = info.gameObject;
        }
        if (infoCancelButton == null && infoPanel != null)
        {
            Transform cancel = infoPanel.transform.Find("Cancel");
            if (cancel != null) infoCancelButton = cancel.GetComponent<Button>();
        }
        if (nameTooltip == null)
            nameTooltip = transform.Find("NameTooltip") as RectTransform;
        if (nameTooltipText == null && nameTooltip != null)
        {
            Transform label = nameTooltip.Find("Name");
            if (label != null) nameTooltipText = label.GetComponent<TMP_Text>();
        }
        // NameTooltip/Name is a dynamic label. A prefab's static localization key
        // would overwrite the hovered item's name when the tooltip is enabled.
        if (nameTooltipText != null)
        {
            LocalizedTMPText staticLocalization = nameTooltipText.GetComponent<LocalizedTMPText>();
            if (staticLocalization != null && staticLocalization.enabled)
                staticLocalization.enabled = false;
        }
        if (recordGridContent == null)
            recordGridContent = transform.Find("Scroll View/Viewport/Content") as RectTransform;
        if (recordScrollRect == null)
        {
            Transform scroll = transform.Find("Scroll View");
            if (scroll != null) recordScrollRect = scroll.GetComponent<ScrollRect>();
        }

        skillTabButton ??= FindTabButton("Skill");
        fragmentTabButton ??= FindTabButton("Fragment");
        relicTabButton ??= FindTabButton("Relic");
        compoundTabButton ??= FindTabButton("Compound");
        monsterTabButton ??= FindTabButton("Monster");
        monsterTabButton ??= FindTabButton("Item");
        Button obsoleteEffectButton = FindTabButton("Effect");
        if (obsoleteEffectButton != null) obsoleteEffectButton.gameObject.SetActive(false);
        if (skillNumberText == null)
        {
            Transform number = transform.Find("SkillNumber");
            if (number != null) skillNumberText = number.GetComponent<TMP_Text>();
        }
        if (iconSlotPrefab == null && recordGridContent != null)
        {
            Transform template = recordGridContent.Find("RecordIconSlot");
            if (template != null) iconSlotPrefab = template.GetComponent<RecordIconSlotUI>();
        }
        if (iconSlotPrefab != null && recordGridContent != null &&
            iconSlotPrefab.transform.IsChildOf(recordGridContent))
            iconSlotPrefab.gameObject.SetActive(false);

        if (recordScrollRect != null)
        {
            if (recordScrollRect.verticalScrollbarVisibility != ScrollRect.ScrollbarVisibility.AutoHide)
                recordScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
        if (monsterRoot == null && defaultRecordViewport != null)
            monsterRoot = defaultRecordViewport.Find("Monster") as RectTransform;
        if (monsterRoot != null)
        {
            monsterButtonViewport ??= monsterRoot.Find("Charbutton") as RectTransform;
            monsterPreviewImage ??= monsterRoot.Find("Content/Preview")?.GetComponent<Image>();
            monsterPhaseOneButton ??= monsterRoot.Find("Content/Charbutton01")?.GetComponent<Button>();
            monsterPhaseTwoButton ??= monsterRoot.Find("Content/Charbutton02")?.GetComponent<Button>();
            if (monsterPhaseOneButton != null)
                monsterPhaseOneColors ??= monsterPhaseOneButton.colors;
            if (monsterPhaseTwoButton != null)
                monsterPhaseTwoColors ??= monsterPhaseTwoButton.colors;
        }
    }

    private Button FindTabButton(string tabName)
    {
        Transform tab = transform.Find("Buttons/" + tabName);
        return tab != null ? tab.GetComponent<Button>() : null;
    }

    private void BindButtons()
    {
        BindButton(skillTabButton, ShowSkillTab);
        BindButton(fragmentTabButton, ShowFragmentTab);
        BindButton(relicTabButton, ShowRelicTab);
        BindButton(compoundTabButton, ShowCompoundTab);
        BindButton(monsterTabButton, ShowMonsterTab);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void CacheButtonColors()
    {
        foreach (Button button in GetTabButtons())
            if (button != null && !originalButtonColors.ContainsKey(button))
                originalButtonColors.Add(button, button.colors);
    }

    private IEnumerable<Button> GetTabButtons()
    {
        yield return skillTabButton;
        yield return fragmentTabButton;
        yield return relicTabButton;
        yield return compoundTabButton;
        yield return monsterTabButton;
    }

    private void ShowTab(RecordTab tab)
    {
        ResolveReferences();
        HideInfo();
        currentTab = tab;
        RefreshSelectedButton();
        bool showMemory = tab == RecordTab.Skill && memoryRoot != null;
        bool showFragment = tab == RecordTab.Fragment && fragmentRoot != null;
        bool showMonster = tab == RecordTab.Monster && monsterRoot != null;
        if (showMonster)
            EnsureMonsterListContent();
        SetRecordContent(showMemory, showFragment, showMonster);
        ClearSlots();
        ClearGeneratedButtons(createdMemoryCharacterButtons);
        ClearGeneratedButtons(createdFragmentCategoryButtons);
        ClearGeneratedButtons(createdMonsterButtons);
        ClearMonsterPreview();
        ClearMonsterPhaseButtons();
        if (!showMemory && !showFragment && !showMonster && recordGridContent == null)
        {
            Debug.LogWarning("[RecordPanelUI] Scroll View/Viewport/Content를 찾을 수 없습니다.", this);
            return;
        }

        DataManager data = DataManager.Instance != null
            ? DataManager.Instance
            : FindFirstObjectByType<DataManager>(FindObjectsInactive.Include);
        if (data == null)
        {
            Debug.LogWarning("[RecordPanelUI] DataManager를 찾을 수 없습니다.", this);
            return;
        }

        if (showMonster)
        {
            BuildMonsterButtons(data);
            RefreshRecordNumber(data, tab);
            return;
        }

        if (iconSlotPrefab == null && recordGridContent != null)
            iconSlotPrefab = CreateRuntimeIconTemplate();
        if (iconSlotPrefab == null) return;

        if (showMemory)
        {
            BuildMemoryCharacterButtons(data);
            RefreshRecordNumber(data, tab);
            return;
        }

        if (showFragment)
        {
            BuildFragmentCategoryButtons(data);
            RefreshRecordNumber(data, tab);
            return;
        }

        switch (tab)
        {
            case RecordTab.Skill:
                if (data.SkillDatabase != null)
                    foreach (SkillMasterData skill in data.SkillDatabase.GetAll()
                        .Where(x => x != null && x.Category != Category.Move)
                        .OrderBy(x => GetRecordRarityOrder(SkillRarityUtility.GetCanonicalName(x.Rarity)))
                        .ThenBy(x => x.SkillId, StringComparer.OrdinalIgnoreCase))
                    {
                        Sprite icon = skill.Icon;
                        if (icon == null && data.SkillIconDatabase != null)
                            data.SkillIconDatabase.TryGetIcon(skill.SkillId, out icon);
                        AddSlot(icon, RecordDisplayNameResolver.SkillName(skill), debugRevealAll || RecordDiscoveryService.IsSkillDiscovered(data, skill.SkillId), SkillRarityUtility.GetCanonicalName(skill.Rarity));
                    }
                break;
            case RecordTab.Fragment:
                if (data.RuneDatabase != null)
                    foreach (RuneData rune in GetSortedRecordRunes(data.RuneDatabase))
                        AddRuneSlot(data, rune, recordGridContent);
                break;
            case RecordTab.Relic:
                if (data.RelicDatabase != null)
                    foreach (RelicData relic in data.RelicDatabase.GetAll().Where(x => x != null).OrderBy(x => GetRecordRarityOrder(x.Rarity))
                        .ThenBy(x => x.FragmentId, StringComparer.OrdinalIgnoreCase))
                    {
                        Sprite icon = null;
                        if (data.RelicIconDatabase != null) data.RelicIconDatabase.TryGetIcon(relic.FragmentId, out icon);
                        AddSlot(icon, RecordDisplayNameResolver.RelicName(relic), debugRevealAll || RecordDiscoveryService.IsRelicDiscovered(data, relic.FragmentId), relic.Rarity);
                    }
                break;
            case RecordTab.Compound:
                if (data.CompoundDatabase != null)
                    foreach (CompoundData compound in data.CompoundDatabase.GetAll().Where(x => x != null).OrderBy(x => GetRecordRarityOrder(x.Rarity))
                        .ThenBy(x => x.CompoundId, StringComparer.OrdinalIgnoreCase))
                    {
                        Sprite icon = null;
                        if (data.RelicIconDatabase != null)
                        {
                            data.RelicIconDatabase.TryGetIcon(compound.CompoundId, out icon);
                            if (icon == null && compound.CompoundId.StartsWith("Compound_", StringComparison.OrdinalIgnoreCase))
                                data.RelicIconDatabase.TryGetIcon("Relic_A_" + compound.CompoundId.Substring("Compound_".Length), out icon);
                        }
                        AddSlot(icon, GameDataLocalization.CompoundName(compound), debugRevealAll || RecordDiscoveryService.IsCompoundDiscovered(data, compound.CompoundId), compound.Rarity);
                    }
                break;
            case RecordTab.Monster:
                if (data.MonsterDatabase != null)
                    foreach (MonsterMasterData monster in data.MonsterDatabase.GetAll().Values.Where(x => x != null)
                        .OrderBy(x => x.Name).ThenBy(x => x.MonsterId, StringComparer.OrdinalIgnoreCase))
                    {
                        Sprite icon = null;
                        data.MonsterIconDatabase?.TryGetIcon(monster.MonsterId, out icon);
                        bool discovered = debugRevealAll || RecordDiscoveryService.IsMonsterDiscovered(data, monster.MonsterId);
                        RecordIconSlotUI slot = AddSlot(icon, string.IsNullOrWhiteSpace(monster.Name) ? monster.MonsterId : monster.Name, discovered);
                        if (discovered && slot != null) monsterIdsBySlot[slot] = monster.MonsterId;
                    }
                break;
        }

        RefreshRecordNumber(data, tab);
        Canvas.ForceUpdateCanvases();
        ResetRecordScrollPosition();
    }

    private void ResetRecordScrollPosition()
    {
        if (recordScrollRect != null && recordScrollRect.content != null)
        {
            recordScrollRect.StopMovement();
            recordScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void SetRecordContent(bool showMemory, bool showFragment, bool showMonster)
    {
        if (memoryRoot != null)
            memoryRoot.gameObject.SetActive(showMemory);
        if (fragmentRoot != null)
            fragmentRoot.gameObject.SetActive(showFragment);
        if (monsterRoot != null)
            monsterRoot.gameObject.SetActive(showMonster);
        if (recordGridContent != null)
            recordGridContent.gameObject.SetActive(!showMemory && !showFragment && !showMonster);
        if (recordScrollRect == null)
            return;

        recordScrollRect.StopMovement();
        RectTransform scrollContent = recordGridContent;
        if (showMemory)
            scrollContent = memoryScrollContent;
        else if (showFragment)
            scrollContent = fragmentScrollContent;
        else if (showMonster)
            scrollContent = monsterListContent;

        recordScrollRect.viewport = showMonster ? monsterButtonViewport : defaultRecordViewport;
        recordScrollRect.content = scrollContent;
        if (scrollContent == null && recordScrollRect.verticalScrollbar != null)
            recordScrollRect.verticalScrollbar.gameObject.SetActive(false);
    }

    private static void ClearGeneratedButtons(List<Button> buttons)
    {
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            button.gameObject.SetActive(false);
            Destroy(button.gameObject);
        }
        buttons.Clear();
    }

    private void BuildMemoryCharacterButtons(DataManager data)
    {
        if (memoryCharacterButtons == null || memoryCharacterButtonPrefab == null ||
            memoryPassiveSlots == null || memoryUltimateSlots == null ||
            memoryAbilitySlots == null || memoryCoreSlots == null)
        {
            Debug.LogWarning("[RecordPanelUI] Memory Layout 인스펙터 연결을 확인해 주세요.", this);
            return;
        }

        if (data.CharacterDatabase == null)
            return;

        CharacterMasterData initialCharacter = null;
        Button initialButton = null;
        foreach (CharacterMasterData character in GetNamedRecordCharacters(data.CharacterDatabase))
        {
            Button button = Instantiate(memoryCharacterButtonPrefab, memoryCharacterButtons, false);
            if (initialCharacter == null ||
                string.Equals(character.CharacterId, selectedMemoryCharacterId, StringComparison.OrdinalIgnoreCase))
            {
                initialCharacter = character;
                initialButton = button;
            }

            TMP_Text nameLabel = button.transform.Find("Name")?.GetComponent<TMP_Text>();
            if (nameLabel != null)
                nameLabel.text = GameDataLocalization.CharacterName(character);

            button.onClick.AddListener(() =>
            {
                if (currentTab != RecordTab.Skill)
                    return;

                ClearSlots();
                ShowCharacterMemory(data, character, button);
            });
            button.gameObject.SetActive(true);
            createdMemoryCharacterButtons.Add(button);
        }

        if (initialCharacter != null)
            ShowCharacterMemory(data, initialCharacter, initialButton);
        else
            RefreshListScrollBounds(memoryScrollContent, 0f);
    }

    private static void RefreshGeneratedButtonColors(IEnumerable<Button> buttons, Button selectedButton, ColorBlock prefabColors)
    {
        foreach (Button button in buttons)
            ApplyRecordButtonColors(button, button == selectedButton, prefabColors);
    }

    private static void ApplyRecordButtonColors(Button button, bool selected, ColorBlock prefabColors)
    {
        if (button == null)
            return;

        ColorBlock colors = prefabColors;
        if (selected)
        {
            colors.normalColor = prefabColors.selectedColor;
            colors.highlightedColor = prefabColors.selectedColor;
        }
        else
        {
            colors.selectedColor = prefabColors.normalColor;
        }

        button.colors = colors;
        if (button.targetGraphic != null)
            button.targetGraphic.CrossFadeColor(
                (button.interactable ? colors.normalColor : colors.disabledColor) * colors.colorMultiplier, 0f, true, true);
    }

    private void ShowCharacterMemory(DataManager data, CharacterMasterData character, Button selectedButton)
    {
        selectedMemoryCharacterId = character.CharacterId;
        RefreshGeneratedButtonColors(createdMemoryCharacterButtons, selectedButton, memoryCharacterButtonPrefab.colors);
        SkillDatabase skills = data.SkillDatabase;
        if (skills == null)
        {
            RefreshListScrollBounds(memoryScrollContent, 0f);
            return;
        }

        AddConfiguredMemorySlots(data, memoryPassiveSlots, character.PassiveSkill1, character.PassiveSkill2);
        AddConfiguredMemorySlots(data, memoryUltimateSlots, character.UniqueSkill1, character.UniqueSkill2, character.UniqueSkill3);
        AddConfiguredMemorySlots(data, memoryAbilitySlots, character.CharacterSkill1, character.CharacterSkill2, character.CharacterSkill3);

        foreach (SkillMasterData skill in skills.GetAll()
            .Where(skill => skill != null && skill.Category == Category.Core &&
                string.Equals(skill.CharacterId?.Trim(), character.CharacterId?.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(skill => GetRecordRarityOrder(SkillRarityUtility.GetCanonicalName(skill.Rarity)))
            .ThenBy(skill => skill.SkillId, StringComparer.OrdinalIgnoreCase))
        {
            AddMemorySlot(data, memoryCoreSlots, skill);
        }

        RefreshListScrollBounds(memoryScrollContent, 0f, memoryPassiveSlots, memoryUltimateSlots,
            memoryAbilitySlots, memoryCoreSlots);
    }

    private void AddConfiguredMemorySlots(DataManager data, RectTransform parent, params string[] skillIds)
    {
        foreach (string skillId in skillIds)
        {
            if (!string.IsNullOrWhiteSpace(skillId) &&
                data.SkillDatabase.TryGet(skillId.Trim(), out SkillMasterData skill) &&
                skill != null)
            {
                AddMemorySlot(data, parent, skill);
            }
        }
    }

    private void AddMemorySlot(DataManager data, RectTransform parent, SkillMasterData skill)
    {
        Sprite icon = skill.Icon;
        if (icon == null)
            data.SkillIconDatabase?.TryGetIcon(skill.SkillId, out icon);

        bool discovered = debugRevealAll || RecordDiscoveryService.IsSkillDiscovered(data, skill.SkillId);
        AddSlot(icon, RecordDisplayNameResolver.SkillName(skill), discovered,
            SkillRarityUtility.GetCanonicalName(skill.Rarity), parent: parent);
    }

    private static IEnumerable<CharacterMasterData> GetNamedRecordCharacters(CharacterDatabase characters)
    {
        if (characters == null)
            return Enumerable.Empty<CharacterMasterData>();

        return characters.GetAll().Values
            .Where(character => character != null && !string.IsNullOrWhiteSpace(character.Name))
            .OrderBy(character => character.CharacterId, StringComparer.OrdinalIgnoreCase);
    }

    private void BuildFragmentCategoryButtons(DataManager data)
    {
        if (fragmentCategoryButtons == null || fragmentExclusiveSlots == null ||
            fragmentSharedSlots == null || memoryCharacterButtonPrefab == null)
        {
            Debug.LogWarning("[RecordPanelUI] Fragment Layout 인스펙터 연결을 확인해 주세요.", this);
            return;
        }

        Button exclusiveButton = CreateFragmentCategoryButton(data, "전용파편", true);
        CreateFragmentCategoryButton(data, "공용파편", false);
        ShowFragmentCategory(data, true, exclusiveButton);
    }

    private Button CreateFragmentCategoryButton(DataManager data, string label, bool exclusive)
    {
        Button button = Instantiate(memoryCharacterButtonPrefab, fragmentCategoryButtons, false);
        TMP_Text nameLabel = button.transform.Find("Name")?.GetComponent<TMP_Text>();
        if (nameLabel != null)
            nameLabel.text = label;

        button.onClick.AddListener(() => ShowFragmentCategory(data, exclusive, button));
        button.gameObject.SetActive(true);
        createdFragmentCategoryButtons.Add(button);
        return button;
    }

    private void ShowFragmentCategory(DataManager data, bool exclusive, Button selectedButton)
    {
        if (currentTab != RecordTab.Fragment)
            return;

        ClearSlots();
        fragmentExclusiveSlots.gameObject.SetActive(exclusive);
        fragmentSharedSlots.gameObject.SetActive(!exclusive);
        RefreshGeneratedButtonColors(createdFragmentCategoryButtons, selectedButton, memoryCharacterButtonPrefab.colors);

        IEnumerable<RuneData> runes = data.RuneDatabase != null
            ? GetSortedRecordRunes(data.RuneDatabase)
            : Enumerable.Empty<RuneData>();
        if (exclusive)
        {
            var runesByCharacter = runes
                .Where(rune => !string.IsNullOrWhiteSpace(rune.TargetCharacterId))
                .ToLookup(rune => rune.TargetCharacterId.Trim(), StringComparer.OrdinalIgnoreCase);

            foreach (CharacterMasterData character in GetNamedRecordCharacters(data.CharacterDatabase))
            {
                foreach (RuneData rune in runesByCharacter[character.CharacterId.Trim()])
                    AddRuneSlot(data, rune, fragmentExclusiveSlots);
            }
        }
        else
        {
            foreach (RuneData rune in runes)
                if (string.Equals(rune.TargetCharacterId?.Trim(), "All", StringComparison.OrdinalIgnoreCase))
                    AddRuneSlot(data, rune, fragmentSharedSlots);
        }

        RefreshListScrollBounds(fragmentScrollContent, fragmentScrollBottomPadding,
            exclusive ? fragmentExclusiveSlots : fragmentSharedSlots);
    }

    private static IEnumerable<RuneData> GetSortedRecordRunes(RuneDatabase runes)
    {
        return runes.GetAll()
            .Where(rune => rune != null)
            .OrderBy(rune => GetRecordRarityOrder(rune.Rarity))
            .ThenBy(rune => rune.RuneId, StringComparer.OrdinalIgnoreCase);
    }

    private void AddRuneSlot(DataManager data, RuneData rune, RectTransform parent)
    {
        Sprite icon = null;
        data.RuneIconDatabase?.TryGetIcon(rune.RuneId, out icon);
        bool discovered = debugRevealAll || RecordDiscoveryService.IsRuneDiscovered(data, rune.RuneId);
        AddSlot(icon, RecordDisplayNameResolver.RuneName(rune), discovered, rune.Rarity, parent: parent);
    }

    private void RefreshListScrollBounds(RectTransform scrollContent, float bottomPadding, params RectTransform[] sections)
    {
        if (scrollContent == null)
            return;

        Canvas.ForceUpdateCanvases();
        float top = scrollContent.rect.yMax;
        float bottom = top;
        foreach (RectTransform section in sections)
        {
            if (section == null || !section.IsChildOf(scrollContent))
                continue;

            LayoutRebuilder.ForceRebuildLayoutImmediate(section);
            Bounds sectionBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scrollContent, section);
            bottom = Mathf.Min(bottom, sectionBounds.min.y);
        }

        // Keep the same top edge and section offsets when the prefab uses a centered pivot.
        Vector3 topPosition = scrollContent.TransformPoint(Vector3.up * top);
        Vector3[] childPositions = new Vector3[scrollContent.childCount];
        for (int i = 0; i < childPositions.Length; i++)
            childPositions[i] = scrollContent.GetChild(i).localPosition - Vector3.up * top;

        Vector2 pivot = scrollContent.pivot;
        pivot.y = 1f;
        scrollContent.pivot = pivot;
        scrollContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, top - bottom + Mathf.Max(0f, bottomPadding));
        scrollContent.position = topPosition;
        for (int i = 0; i < childPositions.Length; i++)
            scrollContent.GetChild(i).localPosition = childPositions[i];

        ResetRecordScrollPosition();
    }

    private void EnsureMonsterListContent()
    {
        if (monsterListContent != null || monsterButtonViewport == null)
            return;

        var list = new GameObject("MonsterButtons", typeof(RectTransform), typeof(VerticalLayoutGroup));
        monsterListContent = (RectTransform)list.transform;
        monsterListContent.SetParent(monsterButtonViewport, false);
        monsterListContent.anchorMin = new Vector2(0f, 1f);
        monsterListContent.anchorMax = new Vector2(1f, 1f);
        monsterListContent.pivot = new Vector2(0.5f, 1f);
        monsterListContent.anchoredPosition = Vector2.zero;
        monsterListContent.sizeDelta = Vector2.zero;

        VerticalLayoutGroup source = monsterButtonViewport.GetComponent<VerticalLayoutGroup>();
        if (source == null)
            return;

        VerticalLayoutGroup layout = list.GetComponent<VerticalLayoutGroup>();
        layout.padding = source.padding;
        layout.childAlignment = source.childAlignment;
        layout.spacing = source.spacing;
        layout.childControlWidth = source.childControlWidth;
        layout.childControlHeight = source.childControlHeight;
        layout.childForceExpandWidth = source.childForceExpandWidth;
        layout.childForceExpandHeight = source.childForceExpandHeight;
        layout.childScaleWidth = source.childScaleWidth;
        layout.childScaleHeight = source.childScaleHeight;
        layout.reverseArrangement = source.reverseArrangement;
        source.enabled = false;
    }

    private static IEnumerable<MonsterMasterData> GetRecordMonsters(MonsterDatabase monsters)
    {
        if (monsters == null)
            return Enumerable.Empty<MonsterMasterData>();

        return monsters.GetAll().Values
            .Where(monster => monster != null && !string.IsNullOrWhiteSpace(monster.MonsterId) &&
                !(monster.MonsterId.EndsWith("_2", StringComparison.Ordinal) &&
                  monsters.TryGet(monster.MonsterId.Substring(0, monster.MonsterId.Length - 2) + "_1", out _)))
            .OrderBy(monster => monster.MonsterId, StringComparer.OrdinalIgnoreCase);
    }

    private bool CanShowMonster(DataManager data, string monsterId)
    {
        return debugRevealAll || RecordDiscoveryService.IsMonsterDiscovered(data, monsterId);
    }

    private void BuildMonsterButtons(DataManager data)
    {
        if (monsterListContent == null || monsterPreviewImage == null || memoryCharacterButtonPrefab == null)
        {
            Debug.LogWarning("[RecordPanelUI] Monster Layout 인스펙터 연결을 확인해 주세요.", this);
            return;
        }

        monsterPreviewLayoutPosition ??= monsterPreviewImage.rectTransform.anchoredPosition;
        MonsterMasterData initialMonster = null;
        Button initialButton = null;
        foreach (MonsterMasterData monster in GetRecordMonsters(data.MonsterDatabase))
        {
            Button button = Instantiate(memoryCharacterButtonPrefab, monsterListContent, false);
            bool discovered = CanShowMonster(data, monster.MonsterId);
            string label = discovered
                ? GameDataLocalization.MonsterName(monster.MonsterId,
                    string.IsNullOrWhiteSpace(monster.Name) ? monster.MonsterId : monster.Name)
                : GameLocalization.Get(LocalizationKeys.Record.Undiscovered);
            SetMonsterButtonLabel(button, label);
            button.interactable = discovered;
            button.onClick.AddListener(() => SelectRecordMonster(data, monster, button));
            button.gameObject.SetActive(true);
            createdMonsterButtons.Add(button);

            if (discovered && initialMonster == null)
            {
                initialMonster = monster;
                initialButton = button;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(monsterListContent);
        monsterListContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            LayoutUtility.GetPreferredHeight(monsterListContent));
        Canvas.ForceUpdateCanvases();
        ResetRecordScrollPosition();

        if (initialMonster != null)
            SelectRecordMonster(data, initialMonster, initialButton);
    }

    private static void SetMonsterButtonLabel(Button button, string label)
    {
        TMP_Text nameLabel = button.transform.Find("Name")?.GetComponent<TMP_Text>();
        if (nameLabel != null)
            nameLabel.text = label;
    }

    private void SelectRecordMonster(DataManager data, MonsterMasterData monster, Button selectedButton)
    {
        if (currentTab != RecordTab.Monster || !CanShowMonster(data, monster.MonsterId))
            return;

        RefreshGeneratedButtonColors(createdMonsterButtons, selectedButton, memoryCharacterButtonPrefab.colors);
        string monsterId = monster.MonsterId;
        string phaseTwoId = monsterId.EndsWith("_1", StringComparison.Ordinal)
            ? monsterId.Substring(0, monsterId.Length - 2) + "_2"
            : null;
        ClearMonsterPhaseButtons();
        if (phaseTwoId != null && data.MonsterDatabase.TryGet(phaseTwoId, out _) &&
            monsterPhaseOneButton != null && monsterPhaseTwoButton != null)
            BindMonsterPhaseButtons(data, monsterId, phaseTwoId);

        ShowMonsterPreview(data, monsterId);
    }

    private void ClearMonsterPhaseButtons()
    {
        if (monsterPhaseOneButton != null)
        {
            if (showMonsterPhaseOne != null)
                monsterPhaseOneButton.onClick.RemoveListener(showMonsterPhaseOne);
            monsterPhaseOneButton.gameObject.SetActive(false);
        }
        if (monsterPhaseTwoButton != null)
        {
            if (showMonsterPhaseTwo != null)
                monsterPhaseTwoButton.onClick.RemoveListener(showMonsterPhaseTwo);
            monsterPhaseTwoButton.gameObject.SetActive(false);
        }
        showMonsterPhaseOne = null;
        showMonsterPhaseTwo = null;
    }

    private void BindMonsterPhaseButtons(DataManager data, string phaseOneId, string phaseTwoId)
    {
        showMonsterPhaseOne = () => SelectMonsterPhase(data, phaseOneId, false);
        showMonsterPhaseTwo = () => SelectMonsterPhase(data, phaseTwoId, true);
        monsterPhaseOneButton.onClick.AddListener(showMonsterPhaseOne);
        monsterPhaseTwoButton.onClick.AddListener(showMonsterPhaseTwo);
        monsterPhaseOneButton.interactable = true;
        monsterPhaseTwoButton.interactable = CanShowMonster(data, phaseTwoId);
        monsterPhaseOneButton.gameObject.SetActive(true);
        monsterPhaseTwoButton.gameObject.SetActive(true);
        RefreshMonsterPhaseButtonColors(false);
    }

    private void SelectMonsterPhase(DataManager data, string monsterId, bool phaseTwo)
    {
        if (currentTab != RecordTab.Monster || !CanShowMonster(data, monsterId))
            return;

        RefreshMonsterPhaseButtonColors(phaseTwo);
        ShowMonsterPreview(data, monsterId);
    }

    private void RefreshMonsterPhaseButtonColors(bool phaseTwo)
    {
        ApplyRecordButtonColors(monsterPhaseOneButton, !phaseTwo, monsterPhaseOneColors.Value);
        ApplyRecordButtonColors(monsterPhaseTwoButton, phaseTwo, monsterPhaseTwoColors.Value);
    }

    private void ShowMonsterPreview(DataManager data, string monsterId)
    {
        ClearMonsterPreview();
        if (data.MonsterPrefabDatabase == null ||
            !data.MonsterPrefabDatabase.TryGetEntry(monsterId, out MonsterPrefabDatabase.Entry entry))
            return;

        // Pair entries can share a battle prefab but have different authored preview frames.
        Sprite[] frames = entry.idlePreviewSprites?.Where(sprite => sprite != null).ToArray() ?? Array.Empty<Sprite>();
        Sprite preview = frames.Length > 0 ? frames[0] : entry.prefab != null
            ? entry.prefab.GetComponentInChildren<SpriteRenderer>(true)?.sprite
            : null;
        RectTransform previewRect = monsterPreviewImage.rectTransform;
        previewRect.sizeDelta = entry.previewSize;
        previewRect.anchoredPosition = monsterPreviewLayoutPosition.GetValueOrDefault() + entry.previewOffset;
        monsterPreviewImage.sprite = preview;
        monsterPreviewImage.preserveAspect = true;
        monsterPreviewImage.raycastTarget = false;
        monsterPreviewImage.enabled = preview != null;
        if (frames.Length > 1)
            monsterPreviewAnimation = StartCoroutine(AnimateMonsterPreview(frames, entry.previewFramesPerSecond));
    }

    private IEnumerator AnimateMonsterPreview(Sprite[] frames, float framesPerSecond)
    {
        var frameWait = new WaitForSecondsRealtime(1f / Mathf.Max(0.01f, framesPerSecond));
        int frame = 0;
        while (true)
        {
            yield return frameWait;
            frame = (frame + 1) % frames.Length;
            monsterPreviewImage.sprite = frames[frame];
        }
    }

    private void ClearMonsterPreview()
    {
        if (monsterPreviewAnimation != null)
        {
            StopCoroutine(monsterPreviewAnimation);
            monsterPreviewAnimation = null;
        }
        if (monsterPreviewImage != null)
        {
            monsterPreviewImage.sprite = null;
            monsterPreviewImage.enabled = false;
        }
    }

    private void RefreshRecordNumber(DataManager data, RecordTab tab)
    {
        if (skillNumberText == null || data == null)
            return;

        int total = 0;
        int discovered = 0;

        switch (tab)
        {
            case RecordTab.Skill:
                if (data.SkillDatabase != null)
                    foreach (SkillMasterData skill in data.SkillDatabase.GetAll())
                    {
                        if (skill == null || skill.Category == Category.Move)
                            continue;
                        total++;
                        if (RecordDiscoveryService.IsSkillDiscovered(data, skill.SkillId)) discovered++;
                    }
                break;
            case RecordTab.Fragment:
                if (data.RuneDatabase != null)
                    foreach (RuneData rune in data.RuneDatabase.GetAll())
                    {
                        if (rune == null) continue;
                        total++;
                        if (RecordDiscoveryService.IsRuneDiscovered(data, rune.RuneId)) discovered++;
                    }
                break;
            case RecordTab.Relic:
                if (data.RelicDatabase != null)
                    foreach (RelicData relic in data.RelicDatabase.GetAll())
                    {
                        if (relic == null) continue;
                        total++;
                        if (RecordDiscoveryService.IsRelicDiscovered(data, relic.FragmentId)) discovered++;
                    }
                break;
            case RecordTab.Compound:
                if (data.CompoundDatabase != null)
                    foreach (CompoundData compound in data.CompoundDatabase.GetAll())
                    {
                        if (compound == null) continue;
                        total++;
                        if (RecordDiscoveryService.IsCompoundDiscovered(data, compound.CompoundId)) discovered++;
                    }
                break;
            case RecordTab.Monster:
                if (data.MonsterDatabase != null)
                    foreach (MonsterMasterData monster in monsterRoot != null
                        ? GetRecordMonsters(data.MonsterDatabase)
                        : data.MonsterDatabase.GetAll().Values)
                    {
                        if (monster == null) continue;
                        total++;
                        if (RecordDiscoveryService.IsMonsterDiscovered(data, monster.MonsterId)) discovered++;
                    }
                break;
        }

        skillNumberText.text = $"{discovered}/{total}";
    }

    private void RefreshSelectedButton()
    {
        Button[] buttons = { skillTabButton, fragmentTabButton, relicTabButton, compoundTabButton, monsterTabButton };
        int selectedIndex = (int)currentTab;
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null) continue;
            if (!originalButtonColors.TryGetValue(button, out ColorBlock original))
            {
                original = button.colors;
                originalButtonColors[button] = original;
            }
            bool selected = i == selectedIndex;
            ColorBlock colors = original;
            if (selected)
            {
                colors.normalColor = original.selectedColor;
                colors.highlightedColor = original.selectedColor;
            }
            else
                colors.selectedColor = original.normalColor;
            button.colors = colors;
            if (button.targetGraphic != null)
                button.targetGraphic.CrossFadeColor((selected ? original.selectedColor : original.normalColor) * colors.colorMultiplier, 0f, true, true);
            ButtonAnimationCoroutine animation = button.GetComponent<ButtonAnimationCoroutine>();
            if (animation == null) animation = button.GetComponentInChildren<ButtonAnimationCoroutine>(true);
            if (animation != null) animation.ForceSetClickedState(selected, false);
        }
    }

    private void HandleSlotClicked(RecordIconSlotUI slot, string itemName)
    {
        // Undiscovered entries have no detail data and must not open Info.
        if (slot == null || !clickableInfoSlots.Contains(slot)) return;
        if (monsterIdsBySlot.TryGetValue(slot, out string monsterId))
            BindMonsterInfo(monsterId);
        OpenInfoForSlot(slot);
    }

    private void BindMonsterInfo(string monsterId)
    {
        DataManager data = DataManager.Instance;
        if (infoPanel == null || data == null || !data.MonsterDatabase.TryGet(monsterId, out MonsterMasterData monster)) return;
        TMP_Text text = infoPanel.GetComponentInChildren<TMP_Text>(true);
        if (text == null) return;
        IEnumerable<MonsterPatternInfoData> patterns = data.MonsterPatternInfoDatabase.GetByMonsterId(monsterId);
        string patternText = string.Join("\n\n", patterns.Select(p => $"[{p.Order}] {p.SkillInfo}\n{p.Description}"));
        text.text = $"{monster.Name}\n\n{patternText}";
    }

    private void HideNameTooltip()
    {
        hoveredSlot = null;
        if (nameTooltip != null) nameTooltip.gameObject.SetActive(false);
    }

    private void HandleSlotHover(RecordIconSlotUI slot, bool entered)
    {
        if (!entered)
        {
            if (hoveredSlot == slot) HideNameTooltip();
            return;
        }
        if (slot == null) return;
        ResolveReferences();
        if (nameTooltip == null) return;
        hoveredSlot = slot;
        // 툴팁의 이미지와 텍스트가 슬롯의 포인터 이벤트를 가로채지 않게 합니다.
        foreach (Graphic graphic in nameTooltip.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
        nameTooltip.gameObject.SetActive(true);
        // Apply after activation as well, so no OnEnable writer can replace it.
        if (nameTooltipText != null) nameTooltipText.text = slot.DisplayName;
        nameTooltip.SetAsLastSibling();
        PositionNameTooltip(slot);
    }

    private void LateUpdate()
    {
        if (hoveredSlot != null && nameTooltip != null && nameTooltip.gameObject.activeSelf)
            PositionNameTooltip(hoveredSlot);
    }

    private void PositionNameTooltip(RecordIconSlotUI slot)
    {
        RectTransform parent = nameTooltip.parent as RectTransform;
        RectTransform slotRect = slot.transform as RectTransform;
        if (parent == null || slotRect == null) return;
        Canvas canvas = nameTooltip.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);
        Vector3 bottomCenter = (corners[0] + corners[3]) * 0.5f;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, bottomCenter);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, camera, out Vector2 localPoint)) return;
        nameTooltip.pivot = new Vector2(0.5f, 1f);
        nameTooltip.position = parent.TransformPoint(localPoint + Vector2.down * tooltipOffset);
    }

    private void ClearSlots()
    {
        HideInfo();
        clickableInfoSlots.Clear();
        monsterIdsBySlot.Clear();
        HideNameTooltip();
        foreach (RecordIconSlotUI slot in createdSlots)
        {
            if (slot == null)
                continue;

            slot.gameObject.SetActive(false);
            Destroy(slot.gameObject);
        }
        createdSlots.Clear();
    }

    private RecordIconSlotUI AddSlot(Sprite icon, string displayName, bool discovered, string rarity = null, Color? backColor = null, Transform parent = null)
    {
        RecordIconSlotUI slot = Instantiate(iconSlotPrefab, parent != null ? parent : recordGridContent, false);
        slot.gameObject.SetActive(true);
        string slotDisplayName = discovered
            ? displayName
            : GameLocalization.Get(LocalizationKeys.Record.Undiscovered);
        slot.Initialize(discovered ? icon : null, slotDisplayName, discovered ? HandleSlotClicked : null, discovered, HandleSlotHover);
        if (discovered) clickableInfoSlots.Add(slot);
        if (backColor.HasValue)
            slot.SetBackColor(backColor.Value);
        else if (!string.IsNullOrWhiteSpace(rarity))
            slot.SetBackColor(GetRarityDisplayColor(rarity));
        createdSlots.Add(slot);
        return slot;
    }

    private RecordIconSlotUI CreateRuntimeIconTemplate()
    {
        GameObject root = new GameObject("RecordIconSlot_Template", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(recordGridContent, false);
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(90f, 90f);
        Image background = root.GetComponent<Image>();
        background.color = new Color(0.22f, 0.25f, 0.3f, 0.5f);
        GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(root.transform, false);
        RectTransform rect = icon.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.1f);
        rect.anchorMax = new Vector2(0.9f, 0.9f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        icon.GetComponent<Image>().raycastTarget = false;
        runtimeIconSlotTemplate = root.AddComponent<RecordIconSlotUI>();
        root.SetActive(false);
        return runtimeIconSlotTemplate;
    }

    private static int GetRecordRarityOrder(string rarity)
    {
        if (string.Equals(rarity, "Common", StringComparison.OrdinalIgnoreCase)) return 0;
        if (string.Equals(rarity, "Rare", StringComparison.OrdinalIgnoreCase)) return 1;
        if (string.Equals(rarity, "Epic", StringComparison.OrdinalIgnoreCase)) return 2;
        if (string.Equals(rarity, "Unique", StringComparison.OrdinalIgnoreCase)) return 3;
        if (string.Equals(rarity, "Exclusive", StringComparison.OrdinalIgnoreCase)) return 4;
        return 5;
    }

    private void CacheRarityPalette()
    {
        cachedCommonRarityColor = commonRarityColor;
        cachedRareRarityColor = rareRarityColor;
        cachedEpicRarityColor = epicRarityColor;
        cachedUniqueRarityColor = uniqueRarityColor;
        cachedExclusiveRarityColor = exclusiveRarityColor;
        hasCachedRarityPalette = true;
    }

    // 여러 전투/로비 UI에서 사용하는 기존 공용 API입니다.
    public static bool TryGetCachedRarityDisplayColor(string rarity, out Color color)
    {
        color = Color.white;
        if (!hasCachedRarityPalette) return false;
        if (string.Equals(rarity, "Exclusive", StringComparison.OrdinalIgnoreCase)) color = cachedExclusiveRarityColor;
        else if (string.Equals(rarity, "Rare", StringComparison.OrdinalIgnoreCase)) color = cachedRareRarityColor;
        else if (string.Equals(rarity, "Epic", StringComparison.OrdinalIgnoreCase)) color = cachedEpicRarityColor;
        else if (string.Equals(rarity, "Unique", StringComparison.OrdinalIgnoreCase)) color = cachedUniqueRarityColor;
        else color = cachedCommonRarityColor;
        return true;
    }

    public Color GetRarityDisplayColor(string rarity)
    {
        TryGetCachedRarityDisplayColor(rarity, out Color color);
        return color;
    }
}

// 다른 UI에서도 참조할 수 있도록 기존 이름 해석 API를 유지합니다.
public static class RecordDisplayNameResolver
{
    public static string SkillName(SkillMasterData data) => data == null ? string.Empty :
        ResolveDisplayName(data, data.SkillId, data.Name, GameDataLocalization.SkillName);
    public static string RuneName(RuneData data) => data == null ? string.Empty :
        ResolveDisplayName(data, data.RuneId, data.Name, GameDataLocalization.RuneName);
    public static string RelicName(RelicData data) => data == null ? string.Empty :
        ResolveDisplayName(data, data.FragmentId, data.Name, GameDataLocalization.RelicName);
    public static string ItemName(ItemData data) => data == null ? string.Empty :
        ResolveDisplayName(data, data.ItemId, data.Name, GameDataLocalization.ItemName);

    public static string ResolveDisplayName<T>(T data, string fallbackId, string sourceName, Func<T, string> localizer)
    {
        if (data == null) return string.Empty;
        string localized = null;
        if (localizer != null)
        {
            try { localized = localizer(data); }
            catch (Exception) { /* 데이터 언어 테이블 준비 전에는 원문을 표시합니다. */ }
        }
        if (!string.IsNullOrWhiteSpace(localized)) return localized;
        if (!string.IsNullOrWhiteSpace(sourceName)) return sourceName;
        return fallbackId?.Trim() ?? string.Empty;
    }
}
