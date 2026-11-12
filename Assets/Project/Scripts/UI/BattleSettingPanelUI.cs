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
    private static int lastClosedByEscapeFrame = -1;

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
    private float skillTooltipResourceOriginalFontSize;
    private bool skillTooltipResourceOriginalAutoSizing;
    private bool skillTooltipResourceStyleCaptured;
    private Image skillTooltipResourceIcon;
    private Image skillTooltipRangeIcon;
    private Image skillTooltipLine;
    private readonly Image[] skillTooltipEntries = new Image[3];
    private GameObject equipmentTooltipPanel;
    private RectTransform equipmentTooltipRect;
    private TMP_Text equipmentTooltipName;
    private TMP_Text equipmentTooltipDetail;
    private Image equipmentTooltipLine;

    [Header("Setting Tooltip Rarity Line Colors")]
    [SerializeField] private Color tooltipCommonLineColor = new Color32(0xA9, 0xB1, 0xBE, 0xFF);
    [SerializeField] private Color tooltipRareLineColor = new Color32(0x52, 0x86, 0xC7, 0xFF);
    [SerializeField] private Color tooltipEpicLineColor = new Color32(0xA0, 0x72, 0xC2, 0xFF);
    [SerializeField] private Color tooltipUniqueLineColor = new Color32(0xCD, 0xA4, 0x52, 0xFF);
    [SerializeField] private Color tooltipExclusiveLineColor = Color.white;
    private UnitStatusEffectTooltipUI effectTooltip;
    private readonly List<StatusEffectRuntimeData> effectPreview = new List<StatusEffectRuntimeData>(1);
    private string activeEffectId;
    private RectTransform activeEffectAnchor;
    [Header("Setting Linked Effect Tooltips")]
    [SerializeField] private Vector2 linkedEffectTooltipOffset = new Vector2(0f, 8f);
    [SerializeField, Min(0f)] private float linkedEffectTooltipSpacing = 8f;
    [Header("Setting Tooltip Fade")]
    [SerializeField, Min(0f)] private float tooltipFadeInDuration = 0.15f;
    [SerializeField, Min(0f)] private float tooltipFadeOutDuration = 0.12f;
    private CanvasGroup skillTooltipFadeGroup;
    private CanvasGroup equipmentTooltipFadeGroup;
    private CanvasGroup statTooltipFadeGroup;
    private bool skillTooltipVisible;
    private bool equipmentTooltipVisible;
    private bool statTooltipVisible;
    private int effectTooltipPreviousSortingOrder = -1;
    private Canvas effectTooltipSortingCanvas;

    private static CanvasGroup EnsureTooltipCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        CanvasGroup group = go.GetComponent<CanvasGroup>();
        if (group == null) group = go.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }

    private void SetTooltipFadeTarget(GameObject go, ref CanvasGroup group, ref bool target, bool visible)
    {
        if (go == null) return;
        group ??= EnsureTooltipCanvasGroup(go);
        if (visible && !go.activeSelf)
        {
            go.SetActive(true);
            if (group != null) group.alpha = 0f;
        }
        target = visible;
        if (visible && group != null) group.alpha = Mathf.Clamp01(group.alpha);
    }

    private static void AdvanceTooltipFade(GameObject go, CanvasGroup group, bool visible, float dt, float fadeIn, float fadeOut)
    {
        if (go == null || group == null || !go.activeSelf) return;
        float duration = visible ? fadeIn : fadeOut;
        float goal = visible ? 1f : 0f;
        group.alpha = duration <= 0f ? goal : Mathf.MoveTowards(group.alpha, goal, dt / duration);
        if (!visible && group.alpha <= 0f) go.SetActive(false);
    }

    private void UpdateTooltipFades()
    {
        float dt = Time.unscaledDeltaTime;
        AdvanceTooltipFade(skillTooltipPanel, skillTooltipFadeGroup, skillTooltipVisible, dt, tooltipFadeInDuration, tooltipFadeOutDuration);
        AdvanceTooltipFade(equipmentTooltipPanel, equipmentTooltipFadeGroup, equipmentTooltipVisible, dt, tooltipFadeInDuration, tooltipFadeOutDuration);
        AdvanceTooltipFade(statNameTooltipPanel, statTooltipFadeGroup, statTooltipVisible, dt, tooltipFadeInDuration, tooltipFadeOutDuration);
    }
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
            UpdateTooltipFades();
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
        // The whole setting window is closing, so discard any remaining fade state.
        if (skillTooltipPanel != null) skillTooltipPanel.SetActive(false);
        if (equipmentTooltipPanel != null) equipmentTooltipPanel.SetActive(false);
        if (statNameTooltipPanel != null) statNameTooltipPanel.SetActive(false);
        if (panelRoot != null)
            panelRoot.SetActive(false);
        ReleasePauseIfNeeded();
    }

    public static bool TryCloseOpenPanel()
    {
        BattleSettingPanelUI[] controllers = FindObjectsByType<BattleSettingPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            BattleSettingPanelUI controller = controllers[i];
            if (controller == null || controller.panelRoot == null ||
                !controller.panelRoot.activeInHierarchy)
            {
                continue;
            }

            controller.Close();
            lastClosedByEscapeFrame = Time.frameCount;
            return true;
        }

        return false;
    }

    public static bool IsAnyPanelOpen()
    {
        BattleSettingPanelUI[] controllers = FindObjectsByType<BattleSettingPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            BattleSettingPanelUI controller = controllers[i];
            if (controller != null && controller.panelRoot != null &&
                controller.panelRoot.activeInHierarchy)
            {
                return true;
            }
        }

        return false;
    }

    public static bool WasClosedByEscapeThisFrame =>
        lastClosedByEscapeFrame == Time.frameCount;

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
            if (skillTooltipResourceValue != null && !skillTooltipResourceStyleCaptured)
            {
                skillTooltipResourceOriginalFontSize = skillTooltipResourceValue.fontSize;
                skillTooltipResourceOriginalAutoSizing = skillTooltipResourceValue.enableAutoSizing;
                skillTooltipResourceStyleCaptured = true;
            }
            skillTooltipResourceIcon = GetImage(FindPath(skill, "Resources_Icon"));
            skillTooltipRangeIcon = GetImage(FindPath(skill, "Range"));
            skillTooltipLine = GetImage(FindPath(skill, "Background/Line"));
            for (int i = 0; i < skillTooltipEntries.Length; i++)
                skillTooltipEntries[i] = GetImage(FindPath(skill, $"Entry_Slot {i + 1}/Back"));
            skillTooltipFadeGroup = EnsureTooltipCanvasGroup(skillTooltipPanel);
            skillTooltipVisible = false;
            if (skillTooltipFadeGroup != null) skillTooltipFadeGroup.alpha = 0f;
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
            equipmentTooltipFadeGroup = EnsureTooltipCanvasGroup(equipmentTooltipPanel);
            equipmentTooltipVisible = false;
            if (equipmentTooltipFadeGroup != null) equipmentTooltipFadeGroup.alpha = 0f;
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
        statTooltipFadeGroup = EnsureTooltipCanvasGroup(statNameTooltipPanel);
        statTooltipVisible = false;
        if (statTooltipFadeGroup != null) statTooltipFadeGroup.alpha = 0f;
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
        // When the pointer moves from an icon onto its description, keep that
        // popup open so its underlined links can actually be hovered.
        if (hovered == null && activeTooltipBinding != null &&
            IsPointerInsideActiveTooltip(activeTooltipBinding))
            hovered = activeTooltipBinding;

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

    private bool IsPointerInsideActiveTooltip(TooltipBinding binding)
    {
        if (binding == null) return false;
        RectTransform popup = binding.Kind == TooltipKind.Skill ? skillTooltipRect : equipmentTooltipRect;
        if (popup == null || !popup.gameObject.activeInHierarchy) return false;
        Canvas canvas = popup.GetComponentInParent<Canvas>();
        if (RectTransformUtility.RectangleContainsScreenPoint(popup, Input.mousePosition, ResolveEventCamera(canvas)))
            return true;
        // Some prefab backgrounds extend outside the root rect.
        Rect bounds = GetRenderedTooltipScreenBounds(popup, ResolveEventCamera(canvas));
        return bounds.Contains(Input.mousePosition);
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
        SetTooltipFadeTarget(statNameTooltipPanel, ref statTooltipFadeGroup, ref statTooltipVisible, true);
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
        SetTooltipFadeTarget(skillTooltipPanel, ref skillTooltipFadeGroup, ref skillTooltipVisible, true);
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
            int resolvedResourceCost = Mathf.Max(0, preview != null ? preview.PayAmount : skill.ResourceCostValue);
            detail = (detail ?? string.Empty).Replace("{ResourceCostValue}", resolvedResourceCost.ToString());
            detail = Regex.Replace(detail, "(<link=\"effecticon:[^\"]+\">)(.*?)(</link>)", "$1<u>$2</u>$3", RegexOptions.Singleline);
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
            ApplyTooltipRarityLineColor(skillTooltipLine, skill.Rarity.ToString());
        }
        if (skillTooltipResourceValue != null)
        {
            BattlePlayerSkillPreview preview = BattlePlayerSkillPreviewCalculator.CreatePreview(runtime, skill, -1, null);
            if (passive)
            {
                skillTooltipResourceValue.enableAutoSizing = false;
                skillTooltipResourceValue.fontSize = 20f;
                skillTooltipResourceValue.text = GameLocalization.Get("battle.skill.passive_duration", "지속");
            }
            else
            {
                if (skillTooltipResourceStyleCaptured)
                {
                    skillTooltipResourceValue.fontSize = skillTooltipResourceOriginalFontSize;
                    skillTooltipResourceValue.enableAutoSizing = skillTooltipResourceOriginalAutoSizing;
                }
                skillTooltipResourceValue.text = Mathf.Max(0, preview != null ? preview.PayAmount : skill.ResourceCostValue).ToString();
            }
        }
    }

    private void ShowEquipmentTooltip(string name, string detail, string rarity, bool nameOnly)
    {
        if (equipmentTooltipPanel == null)
            return;
        SetTooltipFadeTarget(equipmentTooltipPanel, ref equipmentTooltipFadeGroup, ref equipmentTooltipVisible, true);
        if (equipmentTooltipName != null) equipmentTooltipName.text = name ?? string.Empty;
        if (equipmentTooltipDetail != null)
        {
            equipmentTooltipDetail.gameObject.SetActive(!nameOnly);
            if (!nameOnly)
            {
                RelicDescriptionPresenter.Set(equipmentTooltipDetail, detail ?? string.Empty);
                RelicEffectTooltipText autoHover = equipmentTooltipDetail.GetComponent<RelicEffectTooltipText>();
                if (autoHover != null) autoHover.enabled = false;
                equipmentTooltipDetail.raycastTarget = true;
            }
        }
        if (equipmentTooltipLine != null)
        {
            equipmentTooltipLine.gameObject.SetActive(!nameOnly);
            if (!nameOnly)
            {
                ApplyTooltipRarityLineColor(equipmentTooltipLine, rarity);
            }
        }
    }

    // Only Background/Line is tinted. Background/Line2 keeps its prefab color.
    private void ApplyTooltipRarityLineColor(Image line, string rarity)
    {
        if (line == null) return;
        Color color = GetTooltipRarityLineColor(rarity);
        // Do not inherit the previous tooltip's alpha or a transparent prefab tint.
        line.color = color;
    }

    private Color GetTooltipRarityLineColor(string rarity)
    {
        switch ((rarity ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "rare": return tooltipRareLineColor;
            case "epic": return tooltipEpicLineColor;
            case "unique": case "legendary": return tooltipUniqueLineColor;
            case "exclusive": return tooltipExclusiveLineColor;
            default: return tooltipCommonLineColor;
        }
    }

    [Header("Setting Tooltip Position")]
    [SerializeField, Min(0f)] private float tooltipTopGap = 8f;
    [SerializeField, Min(0f)] private float tooltipScreenPadding = 8f;

    // Position each popup below its hovered icon. Use the actual root Canvas viewport,
    // not the immediate parent rect, because the parent can be smaller than the screen.
    private void PositionTooltip(RectTransform source, RectTransform tooltip)
    {
        if (source == null || tooltip == null || !(tooltip.parent is RectTransform parent))
            return;

        Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
        Canvas tooltipCanvas = tooltip.GetComponentInParent<Canvas>();
        Camera sourceCamera = ResolveEventCamera(sourceCanvas);
        Camera tooltipCamera = ResolveEventCamera(tooltipCanvas);
        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);
        Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, topCenter);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, tooltipCamera, out Vector2 local))
            return;

        // Resolve size after the localized text has been assigned.
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip);
        Vector2 size = GetTooltipSizeInParent(tooltip, parent);
        Vector2 target = new Vector2(
            local.x + (tooltip.pivot.x - 0.5f) * size.x,
            local.y + Mathf.Max(0f, tooltipTopGap) + (1f - tooltip.pivot.y) * size.y);
        tooltip.localPosition = new Vector3(target.x, target.y, tooltip.localPosition.z);
        ClampTooltipToScreen(tooltip, parent, tooltipCamera);
    }

    // Clamp the visible contents, not only the TooltipPanel root. Some panels have
    // backgrounds and localized text extending outside the root RectTransform.
    // Recompute after layout and on each hover frame so resolution changes are safe.
    private void ClampTooltipToScreen(RectTransform tooltip, RectTransform parent, Camera camera)
    {
        if (tooltip == null || parent == null) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip);
        Canvas renderCanvas = tooltip.GetComponentInParent<Canvas>();
        Canvas rootCanvas = ResolveEventCanvas(renderCanvas);
        Camera renderCamera = ResolveEventCamera(renderCanvas);
        Rect viewport = rootCanvas != null ? rootCanvas.pixelRect :
            new Rect(0f, 0f, Screen.width, Screen.height);
        float margin = Mathf.Max(0f, tooltipScreenPadding);
        Rect available = Rect.MinMaxRect(viewport.xMin + margin, viewport.yMin + margin,
            viewport.xMax - margin, viewport.yMax - margin);
        if (available.width <= 0f || available.height <= 0f) return;

        // Restore the authored scale when this tooltip was fitted on a smaller screen.
        if (!tooltipOriginalScales.TryGetValue(tooltip, out Vector3 originalScale))
        {
            originalScale = tooltip.localScale;
            tooltipOriginalScales.Add(tooltip, originalScale);
        }
        tooltip.localScale = originalScale;
        Canvas.ForceUpdateCanvases();

        Rect bounds = GetRenderedTooltipScreenBounds(tooltip, renderCamera);
        if (bounds.width <= 0f || bounds.height <= 0f) return;

        // If the complete tooltip is larger than the available viewport, fitting it
        // is the only way to avoid clipping all of its content.
        float fit = Mathf.Min(1f, available.width / bounds.width, available.height / bounds.height);
        if (fit < 1f)
        {
            tooltip.localScale = originalScale * Mathf.Max(0.01f, fit);
            Canvas.ForceUpdateCanvases();
            bounds = GetRenderedTooltipScreenBounds(tooltip, renderCamera);
        }

        float dx = bounds.xMin < available.xMin ? available.xMin - bounds.xMin :
            bounds.xMax > available.xMax ? available.xMax - bounds.xMax : 0f;
        float dy = bounds.yMin < available.yMin ? available.yMin - bounds.yMin :
            bounds.yMax > available.yMax ? available.yMax - bounds.yMax : 0f;
        if (Mathf.Abs(dx) < 0.01f && Mathf.Abs(dy) < 0.01f) return;

        Vector2 center = bounds.center;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, center, camera, out Vector2 a) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
                center + new Vector2(dx, dy), camera, out Vector2 b))
        {
            tooltip.localPosition += (Vector3)(b - a);
        }
    }

    private readonly Dictionary<RectTransform, Vector3> tooltipOriginalScales =
        new Dictionary<RectTransform, Vector3>();

    private static Rect GetRenderedTooltipScreenBounds(RectTransform tooltip, Camera camera)
    {
        Vector3[] corners = new Vector3[4];
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        bool found = false;

        // Only visible graphics contribute; disabled panels and invisible hit targets
        // should not make the tooltip appear much larger than its visible content.
        Graphic[] graphics = tooltip.GetComponentsInChildren<Graphic>(false);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == null || !graphic.isActiveAndEnabled || graphic.canvasRenderer.GetAlpha() <= 0f)
                continue;
            RectTransform rect = graphic.rectTransform;
            if (rect == null) continue;
            rect.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                minX = Mathf.Min(minX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxX = Mathf.Max(maxX, point.x);
                maxY = Mathf.Max(maxY, point.y);
                found = true;
            }
        }

        // A background-only tooltip may not have enabled Graphic descendants.
        if (!found)
        {
            tooltip.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                minX = Mathf.Min(minX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxX = Mathf.Max(maxX, point.x);
                maxY = Mathf.Max(maxY, point.y);
            }
        }
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private static Vector2 GetTooltipSizeInParent(RectTransform tooltip, RectTransform parent)
    {
        Vector3 horizontal = parent.InverseTransformVector(tooltip.TransformVector(Vector3.right * tooltip.rect.width));
        Vector3 vertical = parent.InverseTransformVector(tooltip.TransformVector(Vector3.up * tooltip.rect.height));
        return new Vector2(Mathf.Abs(horizontal.x), Mathf.Abs(vertical.y));
    }

    private static bool IsStatTooltip(TooltipKind kind) =>
        kind == TooltipKind.Mana || kind == TooltipKind.Recovery || kind == TooltipKind.Karma;

    private void PositionStatNameTooltip(RectTransform source)
    {
        PositionTooltip(source, statNameTooltipRect);
    }

    // Display every linked effect together with its skill / equipment tooltip.
    // The pointer does not need to enter the underlined text.
    private void UpdateEffectLinkHover()
    {
        if (activeTooltipBinding == null || IsStatTooltip(activeTooltipBinding.Kind))
        {
            HideEffectTooltip();
            return;
        }

        bool skill = activeTooltipBinding.Kind == TooltipKind.Skill;
        TMP_Text detail = skill ? skillTooltipDetail : equipmentTooltipDetail;
        RectTransform anchor = skill ? skillTooltipRect : equipmentTooltipRect;
        if (detail == null || !detail.gameObject.activeInHierarchy ||
            anchor == null || !anchor.gameObject.activeInHierarchy)
        {
            HideEffectTooltip();
            return;
        }

        List<string> ids = SkillDescriptionFormatter.ExtractLinkedEffectIds(detail.text);
        DataManager manager = DataManager.Instance;
        effectPreview.Clear();
        if (manager?.EffectDatabase != null)
        {
            foreach (string id in ids)
            {
                if (manager.EffectDatabase.TryGet(id, out EffectMasterData effect) && effect != null)
                    effectPreview.Add(new StatusEffectRuntimeData(id, 1));
            }
        }
        if (effectPreview.Count == 0)
        {
            HideEffectTooltip();
            return;
        }

        string signature = string.Join("|", ids);
        effectTooltip ??= UnitStatusEffectTooltipUI.GetOrCreate();
        if (effectTooltip == null) return;

        if (activeEffectId != signature || activeEffectAnchor != anchor)
        {
            List<StatusEffectRuntimeData> currentEffects = new List<StatusEffectRuntimeData>(effectPreview);
            HideEffectTooltip();
            effectPreview.AddRange(currentEffects);
            activeEffectId = signature;
            activeEffectAnchor = anchor;
            effectTooltip.ShowSkillEffectList(this, effectPreview, anchor, null,
                linkedEffectTooltipOffset, linkedEffectTooltipSpacing,
                tooltipFadeInDuration, tooltipFadeOutDuration);
        }
        else
        {
            effectTooltip.UpdateSkillEffectListPosition(this, anchor, null,
                linkedEffectTooltipOffset, linkedEffectTooltipSpacing);
        }

        // The setting panel is a high-sorting-order modal, so the linked
        // effect tooltips must draw above it without changing the prefab.
        Canvas effectCanvas = effectTooltip.GetComponentInChildren<Canvas>(true);
        if (effectCanvas != null)
        {
            if (effectTooltipSortingCanvas != effectCanvas)
            {
                effectTooltipSortingCanvas = effectCanvas;
                effectTooltipPreviousSortingOrder = effectCanvas.sortingOrder;
            }
            effectCanvas.overrideSorting = true;
            Canvas settingCanvas = panelRoot != null ? panelRoot.GetComponent<Canvas>() : null;
            effectCanvas.sortingOrder = Mathf.Max(effectCanvas.sortingOrder,
                settingCanvas != null ? settingCanvas.sortingOrder + 1 : MenuMaximumSortingOrder + 2);
        }
    }

    private void HideEffectTooltip()
    {
        if (activeEffectId != null)
            effectTooltip?.HideSkillEffectList(this);
        if (effectTooltipSortingCanvas != null && effectTooltipPreviousSortingOrder >= 0)
        {
            effectTooltipSortingCanvas.sortingOrder = effectTooltipPreviousSortingOrder;
            effectTooltipSortingCanvas = null;
            effectTooltipPreviousSortingOrder = -1;
        }
        activeEffectId = null;
        activeEffectAnchor = null;
        effectPreview.Clear();
    }

    private void HideAllTooltips(bool clearBinding = true)
    {
        HideEffectTooltip();
        SetTooltipFadeTarget(skillTooltipPanel, ref skillTooltipFadeGroup, ref skillTooltipVisible, false);
        SetTooltipFadeTarget(equipmentTooltipPanel, ref equipmentTooltipFadeGroup, ref equipmentTooltipVisible, false);
        SetTooltipFadeTarget(statNameTooltipPanel, ref statTooltipFadeGroup, ref statTooltipVisible, false);
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

