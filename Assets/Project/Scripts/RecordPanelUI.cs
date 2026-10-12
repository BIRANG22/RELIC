using System;
using System.Collections.Generic;
using System.Linq;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도감의 6개 탭과 단일 Content에 표시되는 아이콘 목록을 관리합니다.
/// 상세 정보 UI는 별도로 구현할 예정이므로 이 스크립트에서는 다루지 않습니다.
/// </summary>
public sealed class RecordPanelUI : MonoBehaviour
{
    private enum RecordTab { Skill, Fragment, Relic, Compound, Item, Effect }

    [Header("Shared Record Layout")]
    [SerializeField] private RectTransform recordGridContent;
    [SerializeField] private ScrollRect recordScrollRect;
    [SerializeField] private RecordIconSlotUI iconSlotPrefab;

    [Header("Record Tab Buttons")]
    [SerializeField] private Button skillTabButton;
    [SerializeField] private Button fragmentTabButton;
    [SerializeField] private Button relicTabButton;
    [SerializeField] private Button compoundTabButton;
    [SerializeField] private Button itemTabButton;
    [SerializeField] private Button effectTabButton;

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
    private readonly Dictionary<Button, ColorBlock> originalButtonColors = new();
    [Header("Name Tooltip")]
    [SerializeField] private RectTransform nameTooltip;
    [SerializeField] private TMP_Text nameTooltipText;
    [SerializeField] private float tooltipOffset = 8f;

    private RecordIconSlotUI hoveredSlot;
    private RecordIconSlotUI runtimeIconSlotTemplate;
    private RecordTab currentTab = RecordTab.Skill;
    private bool initialized;
    private bool debugRevealAll;

    private void Awake()
    {
        CacheRarityPalette();
        ResolveReferences();
        BindButtons();
        CacheButtonColors();
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
        HideNameTooltip();
        ShowSkillTab();
    }

    private void OnDisable() => HideNameTooltip();

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
    public void ShowItemTab() => ShowTab(RecordTab.Item);
    public void ShowEffectTab() => ShowTab(RecordTab.Effect);

    private void ResolveReferences()
    {
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
        itemTabButton ??= FindTabButton("Item");
        effectTabButton ??= FindTabButton("Effect");
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

        if (recordScrollRect != null && recordGridContent != null)
            recordScrollRect.content = recordGridContent;
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
        BindButton(itemTabButton, ShowItemTab);
        BindButton(effectTabButton, ShowEffectTab);
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
        yield return itemTabButton;
        yield return effectTabButton;
    }

    private void ShowTab(RecordTab tab)
    {
        ResolveReferences();
        currentTab = tab;
        RefreshSelectedButton();
        if (recordGridContent == null)
        {
            Debug.LogWarning("[RecordPanelUI] Scroll View/Viewport/Content를 찾을 수 없습니다.", this);
            return;
        }

        ClearSlots();
        if (iconSlotPrefab == null)
            iconSlotPrefab = CreateRuntimeIconTemplate();
        if (iconSlotPrefab == null) return;

        DataManager data = DataManager.Instance != null
            ? DataManager.Instance
            : FindFirstObjectByType<DataManager>(FindObjectsInactive.Include);
        if (data == null)
        {
            Debug.LogWarning("[RecordPanelUI] DataManager를 찾을 수 없습니다.", this);
            return;
        }

        switch (tab)
        {
            case RecordTab.Skill:
                if (data.SkillDatabase != null)
                    foreach (SkillMasterData skill in data.SkillDatabase.GetAll()
                        .Where(x => x != null && x.Category != Category.Move && x.Level != 2)
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
                    foreach (RuneData rune in data.RuneDatabase.GetAll().Where(x => x != null).OrderBy(x => GetRecordRarityOrder(x.Rarity))
                        .ThenBy(x => x.RuneId, StringComparer.OrdinalIgnoreCase))
                    {
                        Sprite icon = null;
                        if (data.RuneIconDatabase != null) data.RuneIconDatabase.TryGetIcon(rune.RuneId, out icon);
                        AddSlot(icon, RecordDisplayNameResolver.RuneName(rune), debugRevealAll || RecordDiscoveryService.IsRuneDiscovered(data, rune.RuneId), rune.Rarity);
                    }
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
            case RecordTab.Item:
                if (data.ItemDatabase != null)
                    foreach (ItemData item in data.ItemDatabase.GetAll().Where(x => x != null).OrderBy(x => GetRecordRarityOrder(x.Rarity))
                        .ThenBy(x => x.ItemId, StringComparer.OrdinalIgnoreCase))
                    {
                        Sprite icon = null;
                        if (data.ItemIconDatabase != null) data.ItemIconDatabase.TryGetIcon(item.ItemId, out icon);
                        AddSlot(icon, RecordDisplayNameResolver.ItemName(item), debugRevealAll || RecordDiscoveryService.IsItemDiscovered(data, item.ItemId), item.Rarity);
                    }
                break;
            case RecordTab.Effect:
                if (data.EffectDatabase != null)
                    foreach (EffectMasterData effect in data.EffectDatabase.All.Values.Where(x => x != null && x.Mark == 1)
                        .OrderBy(x => x.EffectType).ThenBy(x => x.EffectId, StringComparer.OrdinalIgnoreCase))
                    {
                        Sprite icon = null;
                        if (data.StatusEffectIconDatabase != null)
                            data.StatusEffectIconDatabase.TryGetIcon(effect.EffectId, out icon);
                        AddSlot(icon, GameDataLocalization.EffectName(effect), true);
                    }
                break;
        }

        RefreshRecordNumber(data, tab);
        Canvas.ForceUpdateCanvases();
        if (recordScrollRect != null)
        {
            recordScrollRect.StopMovement();
            recordScrollRect.verticalNormalizedPosition = 1f;
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
                        if (skill == null || skill.Category == Category.Move || skill.Level == 2)
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
            case RecordTab.Item:
                if (data.ItemDatabase != null)
                    foreach (ItemData item in data.ItemDatabase.GetAll())
                    {
                        if (item == null) continue;
                        total++;
                        if (RecordDiscoveryService.IsItemDiscovered(data, item.ItemId)) discovered++;
                    }
                break;
            case RecordTab.Effect:
                if (data.EffectDatabase != null)
                    total = data.EffectDatabase.All.Values.Count(effect => effect != null && effect.Mark == 1);
                // Effect discovery is not tracked separately yet; all marked entries are public.
                discovered = total;
                break;
        }

        skillNumberText.text = $"{discovered}/{total}";
    }

    private void RefreshSelectedButton()
    {
        Button[] buttons = { skillTabButton, fragmentTabButton, relicTabButton, compoundTabButton, itemTabButton, effectTabButton };
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
        HideNameTooltip();
        foreach (RecordIconSlotUI slot in createdSlots)
            if (slot != null) Destroy(slot.gameObject);
        createdSlots.Clear();
    }

    private void AddSlot(Sprite icon, string displayName, bool discovered, string rarity = null)
    {
        RecordIconSlotUI slot = Instantiate(iconSlotPrefab, recordGridContent, false);
        slot.gameObject.SetActive(true);
        slot.Initialize(discovered ? icon : null, discovered ? displayName : "미발견", null, discovered, HandleSlotHover);
        if (!string.IsNullOrWhiteSpace(rarity))
            slot.SetBackColor(GetRarityDisplayColor(rarity));
        createdSlots.Add(slot);
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
