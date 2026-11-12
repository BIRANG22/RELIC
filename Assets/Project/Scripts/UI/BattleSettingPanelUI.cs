using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleSettingPanelUI : MonoBehaviour
{
    private const int PartySlotCount = 3;
    private const int MenuMaximumSortingOrder = 30003;
    private static readonly Color32 KarmaOnColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    private static readonly Color32 KarmaOffColor = new Color32(0x77, 0x77, 0x77, 0xFF);

    public enum TooltipKind
    {
        Skill,
        Rune,
        Relic,
        Compound,
        Mana,
        Recovery,
        Karma
    }

    public readonly struct StatTooltipText
    {
        public readonly string LocalizationKey;
        public readonly string Fallback;

        public StatTooltipText(string localizationKey, string fallback)
        {
            LocalizationKey = localizationKey;
            Fallback = fallback;
        }
    }

    public sealed class ResourceHoverState
    {
        public RectTransform Target { get; private set; }
        public TooltipKind Kind { get; private set; }

        public void Enter(RectTransform target, TooltipKind kind)
        {
            Target = target;
            Kind = kind;
        }

        public void Exit(RectTransform target)
        {
            if (Target != target)
                return;
            Target = null;
        }

        public void Clear()
        {
            Target = null;
        }
    }

    private sealed class TooltipBinding
    {
        public RectTransform Target;
        public TooltipKind Kind;
        public string DataId;
        public string CharacterId;
    }

    public sealed class CharacterSnapshot
    {
        public string CharacterId;
        public int CurrentHP;
        public int MaxHP;
        public int CurrentCost;
        public int MaxCost;
        public int CostRecovery;
        public int CurrentResource;
        public string PassiveSkillId;
        public string UniqueSkillId;
        public readonly string[] MemorySkillIds = new string[3];
        public string ActiveRelicId;
        public readonly string[] ArtifactIds = new string[6];
        public readonly string[] RuneIds = new string[6];
    }

    private sealed class CharacterView
    {
        public Transform Root;
        public Image Portrait;
        public Image Job;
        public TMP_Text Name;
        public Image HpFill;
        public TMP_Text HpValue;
        public TMP_Text ManaValue;
        public TMP_Text RegenValue;
        public readonly Image[] Karma = new Image[5];
        public RectTransform ManaHoverArea;
        public RectTransform RegenHoverArea;
        public Image Passive;
        public Image Ultimate;
        public readonly Image[] Memories = new Image[3];
        public Image ActiveRelic;
        public readonly Image[] Artifacts = new Image[6];
        public readonly Image[] Runes = new Image[6];
        public int LastHash = int.MinValue;
    }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform characterContent;
    [SerializeField] private BattleCharacterPanelUI battleCharacterPanel;

    private readonly CharacterView[] views = new CharacterView[PartySlotCount];
    private readonly List<TooltipBinding> tooltipBindings = new List<TooltipBinding>(60);
    private readonly ResourceHoverState resourceHoverState = new ResourceHoverState();
    private TooltipBinding activeTooltipBinding;
    private RectTransform displayedResourceTarget;
    private GameObject skillTooltipPanel;
    private RectTransform skillTooltipRect;
    private TMP_Text skillTooltipName;
    private TMP_Text skillTooltipDetail;
    private TMP_Text skillTooltipType;
    private TMP_Text skillTooltipResourceValue;
    private Image skillTooltipResourceIcon;
    private Image skillTooltipRangeIcon;
    private Image skillTooltipLine;
    private readonly Image[] skillTooltipEntries = new Image[3];
    private GameObject equipmentTooltipPanel;
    private RectTransform equipmentTooltipRect;
    private TMP_Text equipmentTooltipName;
    private TMP_Text equipmentTooltipDetail;
    private Image equipmentTooltipLine;
    private UnitStatusEffectTooltipUI effectTooltip;
    private readonly List<StatusEffectRuntimeData> effectPreview = new List<StatusEffectRuntimeData>(1);
    private string activeEffectId;
    private GameObject statNameTooltipPanel;
    private RectTransform statNameTooltipRect;
    private TMP_Text statNameTooltipText;
    private bool isPauseApplied;
    private float timeScaleBeforePause = 1f;

    private void Awake()
    {
        ResolveViews();
        if (openButton != null)
            openButton.onClick.AddListener(Toggle);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(Toggle);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
        ReleasePauseIfNeeded();
    }

    private void LateUpdate()
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            BringPanelToFront();
            MaintainPause();
            Refresh(false);
            UpdateTooltipHover();
        }
    }

    public void Open()
    {
        if (panelRoot == null)
            return;

        panelRoot.SetActive(true);
        BringPanelToFront();
        ApplyPauseIfNeeded();
        ResolveViews();
        ResolveTooltipViews();
        Refresh(true);
    }

    public void Toggle()
    {
        if (panelRoot == null)
            return;

        if (GetNextPanelActiveState(panelRoot.activeSelf))
            Open();
        else
            Close();
    }

    public static bool GetNextPanelActiveState(bool currentlyActive) => !currentlyActive;

    public void Close()
    {
        resourceHoverState.Clear();
        displayedResourceTarget = null;
        HideAllTooltips();
        if (panelRoot != null)
            panelRoot.SetActive(false);
        ReleasePauseIfNeeded();
    }

    public static bool CanShowTooltip(TooltipKind kind, string dataId)
    {
        return kind == TooltipKind.Mana || kind == TooltipKind.Recovery || kind == TooltipKind.Karma ||
               !string.IsNullOrWhiteSpace(dataId);
    }

    public static StatTooltipText GetStatTooltipText(TooltipKind kind)
    {
        switch (kind)
        {
            case TooltipKind.Mana: return new StatTooltipText("common.mana", "마나");
            case TooltipKind.Recovery: return new StatTooltipText("common.recovery", "마나재생량");
            case TooltipKind.Karma: return new StatTooltipText("resource.karma", "카르마");
            default: return new StatTooltipText(string.Empty, string.Empty);
        }
    }

    public static TooltipKind GetResourceTooltipKind(string sectionName)
    {
        if (string.Equals(sectionName, "Mana", StringComparison.Ordinal)) return TooltipKind.Mana;
        if (string.Equals(sectionName, "Regen", StringComparison.Ordinal)) return TooltipKind.Recovery;
        if (string.Equals(sectionName, "Karma", StringComparison.Ordinal)) return TooltipKind.Karma;
        throw new ArgumentOutOfRangeException(nameof(sectionName), sectionName, "지원하지 않는 자원 영역입니다.");
    }

    public static int GetModalSortingOrder(int menuMaximumSortingOrder) => menuMaximumSortingOrder + 1;

    public static float ResolveResumeTimeScale(float capturedTimeScale, bool otherPauseModalOpen) =>
        otherPauseModalOpen ? 0f : Mathf.Max(0f, capturedTimeScale);

    public static string GetSkillTypeLocalizationKey(SkillMasterData skill)
    {
        if (skill == null)
            return string.Empty;
        if (skill.Category == Category.Move || skill.Rarity == SkillRarity.Move)
            return "common.move";
        if (skill.Category == Category.Passive)
            return "common.passive";
        if (skill.IsFastSkill == 1)
        {
            if (skill.SkillType == SkillType.Buff) return "common.fast_buff";
            if (skill.SkillType == SkillType.Debuff) return "common.fast_debuff";
            if (skill.SkillType == SkillType.Attack) return "common.fast_attack";
        }
        if (skill.SkillType == SkillType.Buff) return "common.buff";
        if (skill.SkillType == SkillType.Debuff) return "common.debuff";
        if (skill.SkillType == SkillType.Attack) return "common.attack";
        return string.Empty;
    }

    public static CharacterSnapshot BuildSnapshot(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return null;

        CharacterSnapshot result = new CharacterSnapshot
        {
            CharacterId = runtime.CharacterId,
            CurrentHP = Mathf.Max(0, runtime.CurrentHP),
            MaxHP = BattleCharacterPanelUI.ResolvePartyMaxHP(runtime),
            CurrentCost = Mathf.Max(0, runtime.CurrentCost),
            MaxCost = BattleCharacterPanelUI.ResolvePartyMaxCost(runtime),
            CostRecovery = runtime.TotalCostRecovery,
            CurrentResource = Mathf.Clamp(runtime.CurrentResource, 0, 5),
            PassiveSkillId = runtime.PassiveSkillId,
            UniqueSkillId = runtime.UniqueSkillId,
            ActiveRelicId = GetAt(runtime.EquippedRelicIds, 0)
        };

        for (int i = 0; i < result.MemorySkillIds.Length; i++)
            result.MemorySkillIds[i] = GetAt(runtime.EquippedSkillIds, i + 1);
        if (string.IsNullOrWhiteSpace(result.MemorySkillIds[0]))
            result.MemorySkillIds[0] = runtime.AbilitySkillId;

        for (int i = 0; i < result.ArtifactIds.Length; i++)
            result.ArtifactIds[i] = GetAt(runtime.EquippedRelicIds, i + 1);
        for (int i = 0; i < result.RuneIds.Length; i++)
            result.RuneIds[i] = GetAt(runtime.EquippedRuneIds, i);

        return result;
    }

    private void ResolveViews()
    {
        if (characterContent == null)
            return;

        for (int i = 0; i < views.Length; i++)
        {
            Transform root = FindDirectChild(characterContent, $"Char{i + 1}");
            if (root == null)
                continue;

            CharacterView view = views[i] ?? new CharacterView();
            view.Root = root;
            view.Portrait = GetImage(FindPath(root, "Icon/Mask/Image")) ?? GetIconInSection(root, "Icon");
            view.Job = GetImage(FindPath(root, "Job/Image"));
            view.Name = FindPath(root, "Name")?.GetComponentInChildren<TMP_Text>(true);
            Transform hp = FindPath(root, "Resources/HP") ?? FindPath(root, "Resources/Hp");
            view.HpFill = GetImage(FindPath(hp, "Fill"));
            view.HpValue = FindNamedText(hp, "Value", "Text", "HP_Value", "Resources_Value");
            view.ManaValue = FindNamedText(FindPath(root, "Resources/Mana"), "Value");
            view.RegenValue = FindNamedText(FindPath(root, "Resources/Regen"), "Value");
            view.ManaHoverArea = FindPath(root, "Resources/Mana/Icon") as RectTransform;
            view.RegenHoverArea = FindPath(root, "Resources/Regen/Icon") as RectTransform;
            for (int karma = 0; karma < view.Karma.Length; karma++)
                view.Karma[karma] = GetImage(FindPath(root, $"Resources/Karma/Icon{karma + 1:00}"));
            BindResourceHover(view.ManaHoverArea, TooltipKind.Mana);
            BindResourceHover(view.RegenHoverArea, TooltipKind.Recovery);
            for (int karma = 0; karma < view.Karma.Length; karma++)
                BindResourceHover(view.Karma[karma] != null ? view.Karma[karma].rectTransform : null, TooltipKind.Karma);
            view.Passive = GetIconInSection(root, "Passive");
            view.Ultimate = GetImage(FindPath(root, "Skill/Content/Ultimate/Icon"));
            view.ActiveRelic = GetIconInSection(root, "Compound");

            for (int slot = 0; slot < view.Memories.Length; slot++)
                view.Memories[slot] = GetIndexedIcon(root, "Skill", "Skill", slot + 1);
            for (int slot = 0; slot < view.Artifacts.Length; slot++)
                view.Artifacts[slot] = GetIndexedIcon(root, "Artifact", "Artifact", slot + 1);
            for (int slot = 0; slot < view.Runes.Length; slot++)
                view.Runes[slot] = GetIndexedIcon(root, "Rune", "Rune", slot + 1);

            views[i] = view;
        }

        ResolveTooltipViews();
    }

    private void Refresh(bool force)
    {
        DataManager manager = DataManager.Instance;
        if (manager?.PartyRuntimeStore == null || manager.CharacterRuntimeStore == null)
            return;

        for (int i = 0; i < views.Length; i++)
        {
            CharacterView view = views[i];
            if (view == null)
                continue;

            CharacterRuntimeData runtime = null;
            string characterId = manager.PartyRuntimeStore.GetCharacterId(i);
            if (!string.IsNullOrWhiteSpace(characterId))
                manager.CharacterRuntimeStore.TryGet(characterId, out runtime);

            CharacterSnapshot snapshot = BuildSnapshot(runtime);
            int hash = CalculateHash(snapshot);
            if (!force && hash == view.LastHash)
                continue;

            Apply(view, snapshot, manager);
            RebuildTooltipBindings(view, snapshot);
            view.LastHash = hash;
        }
    }

    private void ResolveTooltipViews()
    {
        if (panelRoot == null)
            return;

        Transform root = panelRoot.transform;
        Transform skill = FindDirectChild(root, "TooltipPanel");
        if (skill != null)
        {
            skillTooltipPanel = skill.gameObject;
            skillTooltipRect = skill as RectTransform;
            skillTooltipName = FindPath(skill, "Name")?.GetComponent<TMP_Text>();
            skillTooltipDetail = FindPath(skill, "Detail")?.GetComponent<TMP_Text>();
            skillTooltipType = FindPath(skill, "Type/Type_text")?.GetComponent<TMP_Text>();
            skillTooltipResourceValue = FindPath(skill, "Resources_Value")?.GetComponent<TMP_Text>();
            skillTooltipResourceIcon = GetImage(FindPath(skill, "Resources_Icon"));
            skillTooltipRangeIcon = GetImage(FindPath(skill, "Range"));
            skillTooltipLine = GetImage(FindPath(skill, "Background/Line"));
            for (int i = 0; i < skillTooltipEntries.Length; i++)
                skillTooltipEntries[i] = GetImage(FindPath(skill, $"Entry_Slot {i + 1}/Back"));
            skillTooltipPanel.SetActive(false);
        }

        Transform equipment = FindDirectChild(root, "TooltipUI");
        if (equipment != null)
        {
            equipmentTooltipPanel = equipment.gameObject;
            equipmentTooltipRect = equipment as RectTransform;
            equipmentTooltipName = FindPath(equipment, "Name")?.GetComponent<TMP_Text>();
            equipmentTooltipDetail = FindPath(equipment, "Detail")?.GetComponent<TMP_Text>();
            equipmentTooltipLine = GetImage(FindPath(equipment, "Background/Line"));
            equipmentTooltipPanel.SetActive(false);
        }

        EnsureStatNameTooltip();
    }

    private void EnsureStatNameTooltip()
    {
        if (statNameTooltipPanel != null || panelRoot == null)
            return;
        GameObject tooltipObject = new GameObject("StatNameTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        tooltipObject.transform.SetParent(panelRoot.transform, false);
        statNameTooltipPanel = tooltipObject;
        statNameTooltipRect = tooltipObject.GetComponent<RectTransform>();
        statNameTooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        statNameTooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        statNameTooltipRect.pivot = new Vector2(0.5f, 0.5f);
        statNameTooltipRect.sizeDelta = new Vector2(150f, 15f);
        statNameTooltipText = tooltipObject.GetComponent<TextMeshProUGUI>();
        statNameTooltipText.raycastTarget = false;
        statNameTooltipText.fontSize = 18f;
        statNameTooltipText.alignment = TextAlignmentOptions.Center;
        statNameTooltipText.color = Color.white;
        if (equipmentTooltipName != null)
        {
            statNameTooltipText.font = equipmentTooltipName.font;
            statNameTooltipText.fontSharedMaterial = equipmentTooltipName.fontSharedMaterial;
        }
        statNameTooltipPanel.SetActive(false);
    }

    private void RebuildTooltipBindings(CharacterView view, CharacterSnapshot snapshot)
    {
        tooltipBindings.RemoveAll(binding => binding.Target == null ||
            (view.Root != null && binding.Target.IsChildOf(view.Root)));
        if (view == null || snapshot == null)
            return;

        AddBinding(view.Passive, TooltipKind.Skill, snapshot.PassiveSkillId, snapshot.CharacterId);
        AddBinding(view.Ultimate, TooltipKind.Skill, snapshot.UniqueSkillId, snapshot.CharacterId);
        for (int i = 0; i < view.Memories.Length; i++) AddBinding(view.Memories[i], TooltipKind.Skill, snapshot.MemorySkillIds[i], snapshot.CharacterId);
        AddBinding(view.ActiveRelic, TooltipKind.Compound, snapshot.ActiveRelicId);
        for (int i = 0; i < view.Artifacts.Length; i++) AddBinding(view.Artifacts[i], TooltipKind.Relic, snapshot.ArtifactIds[i]);
        for (int i = 0; i < view.Runes.Length; i++) AddBinding(view.Runes[i], TooltipKind.Rune, snapshot.RuneIds[i]);

    }

    private void AddBinding(Image image, TooltipKind kind, string dataId) =>
        AddBinding(image != null ? image.rectTransform : null, kind, dataId, null);

    private void AddBinding(Image image, TooltipKind kind, string dataId, string characterId) =>
        AddBinding(image != null ? image.rectTransform : null, kind, dataId, characterId);

    private void AddBinding(RectTransform target, TooltipKind kind, string dataId, string characterId = null)
    {
        if (target == null || !CanShowTooltip(kind, dataId))
            return;
        tooltipBindings.Add(new TooltipBinding { Target = target, Kind = kind, DataId = dataId, CharacterId = characterId });
    }

    private void BindResourceHover(RectTransform target, TooltipKind kind)
    {
        if (target == null)
            return;
        BattleSettingResourceHoverTarget hoverTarget = target.GetComponent<BattleSettingResourceHoverTarget>();
        if (hoverTarget == null)
            hoverTarget = target.gameObject.AddComponent<BattleSettingResourceHoverTarget>();
        hoverTarget.Initialize(this, target, kind);
        Graphic graphic = target.GetComponent<Graphic>();
        if (graphic != null)
            graphic.raycastTarget = true;
    }

    private void UpdateTooltipHover()
    {
        RectTransform resourceTarget = resourceHoverState.Target;
        if (resourceTarget != displayedResourceTarget)
        {
            displayedResourceTarget = resourceTarget;
            HideAllTooltips();
            if (resourceTarget != null)
                ShowStatNameTooltip(resourceHoverState.Kind);
        }

        if (resourceTarget != null)
        {
            UpdateStatNameTooltipText(resourceHoverState.Kind);
            PositionStatNameTooltip(resourceTarget);
            return;
        }

        TooltipBinding hovered = FindHoveredBinding(tooltipBindings);

        if (hovered != activeTooltipBinding)
        {
            activeTooltipBinding = hovered;
            HideAllTooltips(false);
            if (hovered != null)
                ShowTooltip(hovered);
        }

        if (activeTooltipBinding != null)
        {
            if (IsStatTooltip(activeTooltipBinding.Kind))
                PositionStatNameTooltip(activeTooltipBinding.Target);
            else
                PositionTooltip(activeTooltipBinding.Target,
                    activeTooltipBinding.Kind == TooltipKind.Skill ? skillTooltipRect : equipmentTooltipRect);
            UpdateEffectLinkHover();
        }
    }

    internal void ResourcePointerEnter(RectTransform target, TooltipKind kind)
    {
        if (target != null && IsStatTooltip(kind))
            resourceHoverState.Enter(target, kind);
    }

    internal void ResourcePointerExit(RectTransform target)
    {
        resourceHoverState.Exit(target);
    }

    private static TooltipBinding FindHoveredBinding(List<TooltipBinding> bindings)
    {
        for (int i = bindings.Count - 1; i >= 0; i--)
        {
            TooltipBinding candidate = bindings[i];
            if (candidate.Target == null || !candidate.Target.gameObject.activeInHierarchy)
                continue;
            Canvas canvas = candidate.Target.GetComponentInParent<Canvas>();
            Camera camera = ResolveEventCamera(canvas);
            if (RectTransformUtility.RectangleContainsScreenPoint(candidate.Target, Input.mousePosition, camera))
                return candidate;
        }
        return null;
    }

    private void ShowTooltip(TooltipBinding binding)
    {
        DataManager manager = DataManager.Instance;
        if (manager == null)
            return;

        if (binding.Kind == TooltipKind.Skill)
        {
            if (manager.SkillDatabase == null || !manager.SkillDatabase.TryGet(binding.DataId, out SkillMasterData skill) || skill == null)
                return;
            CharacterRuntimeData runtime = null;
            if (!string.IsNullOrWhiteSpace(binding.CharacterId))
                manager.CharacterRuntimeStore?.TryGet(binding.CharacterId, out runtime);
            ShowSkillTooltip(skill, runtime);
            return;
        }

        if (binding.Kind == TooltipKind.Rune)
        {
            if (manager.RuneDatabase != null && manager.RuneDatabase.TryGet(binding.DataId, out RuneData rune) && rune != null)
                ShowEquipmentTooltip(GameDataLocalization.RuneName(rune), SkillDescriptionFormatter.Format(GameDataLocalization.RuneDescription(rune), rune.ValueRate, rune.CountRate), rune.Rarity, false);
            return;
        }

        if (binding.Kind == TooltipKind.Relic)
        {
            if (manager.RelicDatabase != null && manager.RelicDatabase.TryGet(binding.DataId, out RelicData relic) && relic != null)
                ShowEquipmentTooltip(GameDataLocalization.RelicName(relic), GameDataLocalization.RelicEffectDescription(relic), relic.Rarity, false);
            return;
        }

        if (binding.Kind == TooltipKind.Compound)
        {
            if (manager.CompoundDatabase != null && manager.CompoundDatabase.TryGet(binding.DataId, out CompoundData compound) && compound != null)
                ShowEquipmentTooltip(GameDataLocalization.CompoundName(compound), SkillDescriptionFormatter.Format(GameDataLocalization.CompoundDescription(compound), compound.ValueRate, compound.CountRate), compound.Rarity, false);
            return;
        }

        ShowStatNameTooltip(binding.Kind);
    }

    private void ShowStatNameTooltip(TooltipKind kind)
    {
        EnsureStatNameTooltip();
        UpdateStatNameTooltipText(kind);
        if (statNameTooltipPanel != null)
            statNameTooltipPanel.SetActive(true);
    }

    private void UpdateStatNameTooltipText(TooltipKind kind)
    {
        StatTooltipText text = GetStatTooltipText(kind);
        if (statNameTooltipText == null)
            return;
        string localized = GameLocalization.Get(text.LocalizationKey, fallback: text.Fallback);
        if (statNameTooltipText.text != localized)
            statNameTooltipText.text = localized;
    }

    private void ShowSkillTooltip(SkillMasterData skill, CharacterRuntimeData runtime)
    {
        if (skillTooltipPanel == null)
            return;
        skillTooltipPanel.SetActive(true);
        if (skillTooltipName != null) skillTooltipName.text = GameDataLocalization.SkillName(skill);
        if (skillTooltipType != null)
        {
            string key = GetSkillTypeLocalizationKey(skill);
            skillTooltipType.text = string.IsNullOrEmpty(key) ? string.Empty : GameLocalization.Get(key);
        }
        if (skillTooltipDetail != null)
        {
            BattlePlayerSkillPreview preview = BattlePlayerSkillPreviewCalculator.CreatePreview(runtime, skill, -1, null);
            string detail = BattlePlayerSkillPreviewCalculator.FormatDescription(
                skill, GameDataLocalization.SkillDetailsTemplate(skill), preview,
                SkillDetailNumericLinkHandler.DetailedMode, string.Empty);
            detail = Regex.Replace(detail ?? string.Empty, "(<link=\"effecticon:[^\"]+\">)(.*?)(</link>)", "$1<u>$2</u>$3", RegexOptions.Singleline);
            SkillEffectInlineIconUtility.SetTooltipTextWithoutIcons(skillTooltipDetail, detail);
        }
        for (int i = 0; i < skillTooltipEntries.Length; i++)
            if (skillTooltipEntries[i] != null)
                skillTooltipEntries[i].color = i < Mathf.Clamp(skill.Entry, 0, 3) ? new Color32(0xEE, 0xEE, 0xEE, 0xFF) : new Color32(0x11, 0x11, 0x11, 0xFF);
        bool passive = skill.Category == Category.Passive;
        if (skillTooltipResourceIcon != null) skillTooltipResourceIcon.gameObject.SetActive(!passive);
        if (skillTooltipRangeIcon != null)
        {
            Sprite rangeIcon = null;
            DataManager.Instance?.SkillRangeIconDatabase?.TryGetIcon(skill.RangeId, out rangeIcon);
            ApplySprite(skillTooltipRangeIcon, rangeIcon);
            skillTooltipRangeIcon.gameObject.SetActive(rangeIcon != null);
        }
        if (!passive && skillTooltipResourceIcon != null)
        {
            if (battleCharacterPanel == null)
                battleCharacterPanel = FindFirstObjectByType<BattleCharacterPanelUI>(FindObjectsInactive.Include);
            Sprite resourceIcon = battleCharacterPanel != null ? battleCharacterPanel.GetResourceIcon(skill.ReferenceResource) : null;
            ApplySprite(skillTooltipResourceIcon, resourceIcon);
            Color tint = skill.ReferenceResource == ReferenceResource.HP
                ? new Color32(0xD0, 0x36, 0x36, 0xFF)
                : skill.ReferenceResource == ReferenceResource.Cost
                    ? new Color32(0x33, 0x6F, 0xC5, 0xFF)
                    : Color.white;
            skillTooltipResourceIcon.color = new Color(tint.r, tint.g, tint.b, skillTooltipResourceIcon.color.a);
        }
        if (skillTooltipLine != null)
        {
            Color color;
            string rarity = skill.Rarity.ToString();
            if (!RecordPanelUI.TryGetCachedRarityDisplayColor(rarity, out color))
                color = new Color32(0xA9, 0xB1, 0xBE, 0xFF);
            skillTooltipLine.color = new Color(color.r, color.g, color.b, skillTooltipLine.color.a);
        }
        if (skillTooltipResourceValue != null)
        {
            BattlePlayerSkillPreview preview = BattlePlayerSkillPreviewCalculator.CreatePreview(runtime, skill, -1, null);
            skillTooltipResourceValue.text = passive
                ? GameLocalization.Get("battle.skill.passive_duration", "지속")
                : Mathf.Max(0, preview != null ? preview.PayAmount : skill.ResourceCostValue).ToString();
        }
    }

    private void ShowEquipmentTooltip(string name, string detail, string rarity, bool nameOnly)
    {
        if (equipmentTooltipPanel == null)
            return;
        equipmentTooltipPanel.SetActive(true);
        if (equipmentTooltipName != null) equipmentTooltipName.text = name ?? string.Empty;
        if (equipmentTooltipDetail != null)
        {
            equipmentTooltipDetail.gameObject.SetActive(!nameOnly);
            if (!nameOnly) RelicDescriptionPresenter.Set(equipmentTooltipDetail, detail ?? string.Empty);
        }
        if (equipmentTooltipLine != null)
        {
            equipmentTooltipLine.gameObject.SetActive(!nameOnly);
            if (!nameOnly && RecordPanelUI.TryGetCachedRarityDisplayColor(rarity, out Color color))
                equipmentTooltipLine.color = new Color(color.r, color.g, color.b, equipmentTooltipLine.color.a);
        }
    }

    private void PositionTooltip(RectTransform source, RectTransform tooltip)
    {
        if (source == null || tooltip == null || tooltip.parent is not RectTransform parent)
            return;
        Canvas canvas = tooltip.GetComponentInParent<Canvas>();
        Camera camera = ResolveEventCamera(canvas);
        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, camera, out Vector2 local))
            return;
        Vector2 size = tooltip.rect.size;
        Rect bounds = parent.rect;
        local.x = Mathf.Clamp(local.x + 12f, bounds.xMin + size.x * tooltip.pivot.x, bounds.xMax - size.x * (1f - tooltip.pivot.x));
        local.y = Mathf.Clamp(local.y, bounds.yMin + size.y * tooltip.pivot.y, bounds.yMax - size.y * (1f - tooltip.pivot.y));
        tooltip.localPosition = new Vector3(local.x, local.y, tooltip.localPosition.z);
    }

    private static bool IsStatTooltip(TooltipKind kind) =>
        kind == TooltipKind.Mana || kind == TooltipKind.Recovery || kind == TooltipKind.Karma;

    private void PositionStatNameTooltip(RectTransform source)
    {
        if (source == null || statNameTooltipRect == null || statNameTooltipRect.parent is not RectTransform parent)
            return;
        Vector3 center = parent.InverseTransformPoint(source.TransformPoint(source.rect.center));
        statNameTooltipRect.localPosition = new Vector3(center.x, center.y + 30f, statNameTooltipRect.localPosition.z);
    }

    private void UpdateEffectLinkHover()
    {
        if (activeTooltipBinding == null || activeTooltipBinding.Kind != TooltipKind.Skill || skillTooltipDetail == null || !skillTooltipDetail.gameObject.activeInHierarchy)
        {
            HideEffectTooltip();
            return;
        }
        Canvas canvas = skillTooltipDetail.canvas;
        Camera camera = ResolveEventCamera(canvas);
        int index = TMP_TextUtilities.FindIntersectingLink(skillTooltipDetail, Input.mousePosition, camera);
        if (index < 0)
        {
            HideEffectTooltip();
            return;
        }
        skillTooltipDetail.ForceMeshUpdate();
        TMP_TextInfo info = skillTooltipDetail.textInfo;
        if (index >= info.linkCount || !MonsterSkillEffectLinkTooltip.TryParseEffectId(info.linkInfo[index].GetLinkID(), out string effectId))
        {
            HideEffectTooltip();
            return;
        }
        if (activeEffectId == effectId)
        {
            AlignEffectTooltipTopLeftToPointer();
            return;
        }
        DataManager manager = DataManager.Instance;
        if (manager?.EffectDatabase == null || !manager.EffectDatabase.TryGet(effectId, out EffectMasterData effect) || effect == null)
        {
            HideEffectTooltip();
            return;
        }
        effectTooltip ??= UnitStatusEffectTooltipUI.GetOrCreate();
        if (effectTooltip == null)
            return;
        HideEffectTooltip();
        activeEffectId = effectId;
        effectPreview.Add(new StatusEffectRuntimeData(effectId, 1));
        effectTooltip.ShowPinned(this, effectPreview, Input.mousePosition);
        AlignEffectTooltipTopLeftToPointer();
    }

    private void AlignEffectTooltipTopLeftToPointer()
    {
        if (effectTooltip == null || activeEffectId == null)
            return;
        UnitStatusEffectTooltipItemUI[] items = effectTooltip.GetComponentsInChildren<UnitStatusEffectTooltipItemUI>(true);
        for (int i = 0; i < items.Length; i++)
        {
            UnitStatusEffectTooltipItemUI item = items[i];
            if (item == null || !item.gameObject.activeInHierarchy || item.gameObject.name != "UnitStatusEffectTooltipItemUI")
                continue;
            RectTransform rect = item.transform as RectTransform;
            RectTransform parent = rect != null ? rect.parent as RectTransform : null;
            if (rect == null || parent == null)
                return;
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = ResolveEventCamera(canvas);
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, Input.mousePosition, camera, out Vector3 world))
                MonsterSkillEffectLinkTooltip.AlignTopLeftToWorldPoint(rect, world);
            return;
        }
    }

    private void HideEffectTooltip()
    {
        if (activeEffectId != null)
            effectTooltip?.Hide(this);
        activeEffectId = null;
        effectPreview.Clear();
    }

    private void HideAllTooltips(bool clearBinding = true)
    {
        HideEffectTooltip();
        if (skillTooltipPanel != null) skillTooltipPanel.SetActive(false);
        if (equipmentTooltipPanel != null) equipmentTooltipPanel.SetActive(false);
        if (statNameTooltipPanel != null) statNameTooltipPanel.SetActive(false);
        if (equipmentTooltipDetail != null) equipmentTooltipDetail.gameObject.SetActive(true);
        if (equipmentTooltipLine != null) equipmentTooltipLine.gameObject.SetActive(true);
        if (clearBinding) activeTooltipBinding = null;
    }

    private void BringPanelToFront()
    {
        if (panelRoot == null)
            return;
        Transform panelTransform = panelRoot.transform;
        if (panelTransform.parent != null && panelTransform.GetSiblingIndex() != panelTransform.parent.childCount - 1)
            panelTransform.SetAsLastSibling();
        Canvas canvas = panelRoot.GetComponent<Canvas>();
        if (canvas == null)
            canvas = panelRoot.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = GetModalSortingOrder(MenuMaximumSortingOrder);
        if (panelRoot.GetComponent<GraphicRaycaster>() == null)
            panelRoot.AddComponent<GraphicRaycaster>();
    }

    private void ApplyPauseIfNeeded()
    {
        if (!isPauseApplied)
        {
            timeScaleBeforePause = Time.timeScale;
            isPauseApplied = true;
        }
        Time.timeScale = 0f;
    }

    private void MaintainPause()
    {
        if (isPauseApplied)
            Time.timeScale = 0f;
    }

    private void ReleasePauseIfNeeded()
    {
        if (!isPauseApplied)
            return;
        Time.timeScale = ResolveResumeTimeScale(timeScaleBeforePause, IsOtherPauseModalOpen());
        isPauseApplied = false;
    }

    private static bool IsOtherPauseModalOpen()
    {
        BattleMenuEscapeInputController[] controllers = FindObjectsByType<BattleMenuEscapeInputController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
            if (controllers[i] != null && controllers[i].IsMenuPanelVisible)
                return true;
        return false;
    }

    private static void Apply(CharacterView view, CharacterSnapshot snapshot, DataManager manager)
    {
        view.Root.gameObject.SetActive(snapshot != null);
        if (snapshot == null)
            return;

        CharacterMasterData master = null;
        manager.CharacterDatabase?.TryGet(snapshot.CharacterId, out master);
        Sprite portrait = master != null ? master.Icon : null;
        if (portrait == null)
            manager.CharacterIconDatabase?.TryGetIcon(snapshot.CharacterId, out portrait);
        ApplySprite(view.Portrait, portrait);
        ApplySprite(view.Job, ResolveJobIcon(manager.CharacterIconDatabase, snapshot.CharacterId));

        if (view.Name != null)
            view.Name.text = master != null ? GameDataLocalization.CharacterName(master) : snapshot.CharacterId;
        if (view.HpFill != null)
            view.HpFill.fillAmount = snapshot.MaxHP > 0 ? Mathf.Clamp01((float)snapshot.CurrentHP / snapshot.MaxHP) : 0f;
        if (view.HpValue != null)
            view.HpValue.text = $"{snapshot.CurrentHP} / {snapshot.MaxHP}";
        if (view.ManaValue != null)
            view.ManaValue.text = FormatMana(snapshot.CurrentCost, snapshot.MaxCost);
        if (view.RegenValue != null)
            view.RegenValue.text = snapshot.CostRecovery.ToString();
        for (int i = 0; i < view.Karma.Length; i++)
            if (view.Karma[i] != null)
                view.Karma[i].color = i < snapshot.CurrentResource ? KarmaOnColor : KarmaOffColor;

        ApplySkill(view.Passive, snapshot.PassiveSkillId, manager);
        ApplySkill(view.Ultimate, snapshot.UniqueSkillId, manager);
        for (int i = 0; i < view.Memories.Length; i++)
            ApplySkill(view.Memories[i], snapshot.MemorySkillIds[i], manager);
        ApplyRelic(view.ActiveRelic, snapshot.ActiveRelicId, manager);
        for (int i = 0; i < view.Artifacts.Length; i++)
            ApplyRelic(view.Artifacts[i], snapshot.ArtifactIds[i], manager);
        for (int i = 0; i < view.Runes.Length; i++)
        {
            Sprite icon = null;
            if (!string.IsNullOrWhiteSpace(snapshot.RuneIds[i]))
                manager.RuneIconDatabase?.TryGetIcon(snapshot.RuneIds[i], out icon);
            ApplySprite(view.Runes[i], icon);
        }
    }

    private static void ApplySkill(Image target, string id, DataManager manager)
    {
        Sprite icon = null;
        if (!string.IsNullOrWhiteSpace(id))
            manager.SkillIconDatabase?.TryGetIcon(id, out icon);
        ApplySprite(target, icon);
    }

    private static void ApplyRelic(Image target, string id, DataManager manager)
    {
        Sprite icon = null;
        if (!string.IsNullOrWhiteSpace(id))
        {
            manager.RelicIconDatabase?.TryGetIcon(id, out icon);
            if (icon == null && id.StartsWith("Compound_", StringComparison.Ordinal))
                manager.RelicIconDatabase?.TryGetIcon("Relic_A_" + id.Substring("Compound_".Length), out icon);
        }
        ApplySprite(target, icon);
    }

    public static Sprite ResolveJobIcon(CharacterIconDatabase database, string characterId)
    {
        if (database == null || string.IsNullOrWhiteSpace(characterId))
            return null;

        return database.TryGetMark(characterId, out Sprite icon) ? icon : null;
    }

    private static int CalculateHash(CharacterSnapshot snapshot)
    {
        if (snapshot == null)
            return 0;
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (snapshot.CharacterId ?? string.Empty).GetHashCode();
            hash = hash * 31 + snapshot.CurrentHP;
            hash = hash * 31 + snapshot.MaxHP;
            hash = hash * 31 + snapshot.CurrentCost;
            hash = hash * 31 + snapshot.MaxCost;
            hash = hash * 31 + snapshot.CostRecovery;
            hash = hash * 31 + snapshot.CurrentResource;
            hash = hash * 31 + (snapshot.PassiveSkillId ?? string.Empty).GetHashCode();
            hash = hash * 31 + (snapshot.UniqueSkillId ?? string.Empty).GetHashCode();
            hash = hash * 31 + (snapshot.ActiveRelicId ?? string.Empty).GetHashCode();
            for (int i = 0; i < snapshot.MemorySkillIds.Length; i++) hash = hash * 31 + (snapshot.MemorySkillIds[i] ?? string.Empty).GetHashCode();
            for (int i = 0; i < snapshot.ArtifactIds.Length; i++) hash = hash * 31 + (snapshot.ArtifactIds[i] ?? string.Empty).GetHashCode();
            for (int i = 0; i < snapshot.RuneIds.Length; i++) hash = hash * 31 + (snapshot.RuneIds[i] ?? string.Empty).GetHashCode();
            return hash;
        }
    }

    private static Image GetIndexedIcon(Transform root, string sectionName, string slotPrefix, int index)
    {
        Transform section = FindDirectChild(root, sectionName);
        Transform content = FindDirectChild(section, "Content") ?? section;
        Transform slot = FindDirectChild(content, $"{slotPrefix}{index:00}") ?? FindDirectChild(content, $"{slotPrefix}{index}");
        return GetImage(FindDirectChild(slot, "Icon")) ?? slot?.GetComponentInChildren<Image>(true);
    }

    private static Image GetIconInSection(Transform root, string sectionName)
    {
        Transform section = FindDirectChild(root, sectionName);
        return GetImage(FindDirectChild(section, "Icon")) ?? section?.GetComponentInChildren<Image>(true);
    }

    private static TMP_Text FindNamedText(Transform root, params string[] names)
    {
        if (root == null)
            return null;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            for (int i = 0; i < names.Length; i++)
                if (string.Equals(text.name, names[i], StringComparison.OrdinalIgnoreCase))
                    return text;
        return root.GetComponentInChildren<TMP_Text>(true);
    }

    private static void ApplySprite(Image image, Sprite sprite)
    {
        if (image == null)
            return;
        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
    }

    private static string GetAt(string[] values, int index) => values != null && index >= 0 && index < values.Length ? values[index] : null;
    public static string FormatMana(int current, int maximum) => $"{current}/{maximum}";

    public static Canvas ResolveEventCanvas(Canvas canvas)
    {
        if (canvas == null)
            return null;
        return canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
    }

    private static Camera ResolveEventCamera(Canvas canvas)
    {
        Canvas eventCanvas = ResolveEventCanvas(canvas);
        return eventCanvas != null && eventCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? eventCanvas.worldCamera
            : null;
    }

    private static Image GetImage(Transform target) => target != null ? target.GetComponent<Image>() : null;
    private static Transform FindPath(Transform root, string path) => root != null && !string.IsNullOrEmpty(path) ? root.Find(path) : null;

    private static Transform FindDirectChild(Transform root, string childName)
    {
        if (root == null)
            return null;
        for (int i = 0; i < root.childCount; i++)
            if (string.Equals(root.GetChild(i).name, childName, StringComparison.Ordinal))
                return root.GetChild(i);
        return null;
    }
}

[DisallowMultipleComponent]
public sealed class BattleSettingResourceHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private BattleSettingPanelUI owner;
    private RectTransform target;
    private BattleSettingPanelUI.TooltipKind kind;

    public void Initialize(
        BattleSettingPanelUI tooltipOwner,
        RectTransform targetRect,
        BattleSettingPanelUI.TooltipKind tooltipKind)
    {
        owner = tooltipOwner;
        target = targetRect;
        kind = tooltipKind;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        owner?.ResourcePointerEnter(target, kind);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.ResourcePointerExit(target);
    }

    private void OnDisable()
    {
        owner?.ResourcePointerExit(target);
    }
}

