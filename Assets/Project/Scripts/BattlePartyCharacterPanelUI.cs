using Relic.Gameplay.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// BattleCharacterPanel의 Char01~03 상시 캐릭터 정보와 Char_Select 선택을 관리합니다.
/// 선택된 캐릭터의 액티브 스킬 UI 갱신은 기존 BattleCharacterPanelUI.Bind가 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class BattlePartyCharacterPanelUI : MonoBehaviour
{
    private const int PartySlotCount = 3;
    private static readonly Color32 KarmaOnColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    private static readonly Color32 KarmaOffColor = new Color32(0x77, 0x77, 0x77, 0xFF);

    private sealed class SlotView
    {
        public Transform Root;
        public Image CharacterIcon;
        public Image PassiveIcon;
        public Image HpFill;
        public Image CostFill;
        public readonly Image[] Karma = new Image[5];
        public Transform StatusContent;
        public readonly Image[] Runes = new Image[6];
        public readonly Image[] Artifacts = new Image[6];
        public CharacterRuntimeData Runtime;
        public int LastHash = int.MinValue;
        public readonly List<StatusEffectIcon> SpawnedStatusIcons = new();
    }

    [SerializeField] private StatusEffectIcon statusEffectIconPrefab;

    private readonly SlotView[] slots = new SlotView[PartySlotCount];
    private readonly CharacterRuntimeData[] partyRuntimes = new CharacterRuntimeData[PartySlotCount];
    private readonly BattlePartyCharacterSelectTarget[] selectTargets = new BattlePartyCharacterSelectTarget[PartySlotCount];
    private BattleCharacterPanelUI characterPanel;
    private BattleRoomLoader roomLoader;
    private BattleTimelineController timelineController;

    public CharacterRuntimeData GetRuntime(int index)
    {
        return index >= 0 && index < partyRuntimes.Length ? partyRuntimes[index] : null;
    }

    private void Awake()
    {
        characterPanel = GetComponent<BattleCharacterPanelUI>();
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RefreshPartyFromRuntimeStore();
        BattleTimelineController.CharacterSelectionChanged -= HandleCharacterSelectionChanged;
        BattleTimelineController.CharacterSelectionChanged += HandleCharacterSelectionChanged;
    }

    private void OnDisable()
    {
        BattleTimelineController.CharacterSelectionChanged -= HandleCharacterSelectionChanged;
    }

    private void LateUpdate()
    {
        RefreshPartyFromRuntimeStore();

        for (int i = 0; i < slots.Length; i++)
        {
            SlotView slot = slots[i];
            if (slot == null || slot.Runtime == null)
                continue;

            int hash = CalculateRuntimeHash(slot.Runtime);
            if (hash != slot.LastHash)
                RefreshSlot(i, true);
        }
    }

    public void SetParty(IReadOnlyList<CharacterRuntimeData> runtimes)
    {
        ResolveReferences();

        for (int i = 0; i < PartySlotCount; i++)
        {
            CharacterRuntimeData runtime = runtimes != null && i < runtimes.Count ? runtimes[i] : null;
            partyRuntimes[i] = runtime;
            if (slots[i] != null)
                slots[i].Runtime = runtime;
            RefreshSlot(i, true);
        }
    }

    public void RefreshPartyFromRuntimeStore()
    {
        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        if (partyStore == null || DataManager.Instance.CharacterRuntimeStore == null)
            return;

        bool changed = false;
        for (int i = 0; i < PartySlotCount; i++)
        {
            CharacterRuntimeData runtime = null;
            string characterId = partyStore.GetCharacterId(i);
            if (!string.IsNullOrWhiteSpace(characterId))
                DataManager.Instance.CharacterRuntimeStore.TryGet(characterId, out runtime);

            if (!ReferenceEquals(partyRuntimes[i], runtime))
            {
                partyRuntimes[i] = runtime;
                if (slots[i] != null)
                    slots[i].Runtime = runtime;
                changed = true;
            }
        }

        if (changed)
        {
            for (int i = 0; i < PartySlotCount; i++)
                RefreshSlot(i, true);
        }
    }

    public bool SelectIndex(int index)
    {
        if (index < 0 || index >= partyRuntimes.Length)
            return false;

        CharacterRuntimeData runtime = partyRuntimes[index];
        if (runtime == null || runtime.IsDead)
            return false;

        if (!SteamBattleStateSynchronizer.CanLocalPlayerControlCharacter(runtime.CharacterId))
        {
            BattleWarningUI.ShowMessage(GameLocalization.Get("battle.other_player_character", "다른 플레이어의 캐릭터입니다."));
            return false;
        }

        ResolveControllers();

        if (roomLoader != null)
        {
            roomLoader.OnPlayerCharacterClicked(runtime);
            return true;
        }

        if (characterPanel != null)
            characterPanel.Bind(runtime);

        if (timelineController != null)
            timelineController.SelectCharacter(runtime);

        return true;
    }

    private void HandleCharacterSelectionChanged(CharacterRuntimeData runtime)
    {
        if (runtime != null && characterPanel != null && characterPanel.BoundRuntime != runtime)
            characterPanel.Bind(runtime);
    }

    private void ResolveReferences()
    {
        if (characterPanel == null)
            characterPanel = GetComponent<BattleCharacterPanelUI>();

        if (statusEffectIconPrefab == null && characterPanel != null)
            statusEffectIconPrefab = characterPanel.StatusEffectIconPrefab;

        for (int i = 0; i < PartySlotCount; i++)
        {
            if (slots[i] == null)
                slots[i] = BuildSlotView(FindDirectChild(transform, $"Char0{i + 1}"));

            Transform selectRoot = FindPath(transform, $"Char_Select/Char0{i + 1}");
            if (selectRoot != null)
            {
                BattlePartyCharacterSelectTarget target = selectRoot.GetComponent<BattlePartyCharacterSelectTarget>();
                if (target == null)
                    target = selectRoot.gameObject.AddComponent<BattlePartyCharacterSelectTarget>();
                target.Configure(this, i);
                selectTargets[i] = target;
            }
        }

        ResolveControllers();
    }

    private void ResolveControllers()
    {
        if (roomLoader == null)
            roomLoader = FindFirstObjectByType<BattleRoomLoader>(FindObjectsInactive.Include);
        if (timelineController == null)
            timelineController = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
    }

    private SlotView BuildSlotView(Transform root)
    {
        if (root == null)
            return null;

        SlotView view = new SlotView { Root = root };
        view.CharacterIcon = GetImage(FindPath(root, "Icon/Mask/Image"));
        view.PassiveIcon = GetImage(FindPath(root, "Passive/Icon"));
        view.HpFill = GetImage(FindPath(root, "Resources/Hp/Fill"));
        view.CostFill = GetImage(FindPath(root, "Resources/Cost/Fill"));
        view.StatusContent = FindPath(root, "Resources/StatusEffect/Content");

        for (int i = 0; i < view.Karma.Length; i++)
            view.Karma[i] = GetImage(FindPath(root, $"Resources/Karma/Karma0{i + 1}"));
        for (int i = 0; i < view.Runes.Length; i++)
            view.Runes[i] = GetImage(FindPath(root, $"Rune/Rune0{i + 1}/Icon"));
        for (int i = 0; i < view.Artifacts.Length; i++)
            view.Artifacts[i] = GetImage(FindPath(root, $"Artifact/Artifact0{i + 1}/Icon"));

        return view;
    }

    private void RefreshSlot(int index, bool forceStatusRebuild)
    {
        if (index < 0 || index >= slots.Length)
            return;

        SlotView slot = slots[index];
        if (slot == null)
            return;

        CharacterRuntimeData runtime = partyRuntimes[index];
        slot.Runtime = runtime;
        slot.Root.gameObject.SetActive(runtime != null);

        if (runtime == null)
        {
            ClearStatusIcons(slot);
            slot.LastHash = 0;
            return;
        }

        CharacterMasterData master = null;
        if (DataManager.Instance?.CharacterDatabase != null)
            DataManager.Instance.CharacterDatabase.TryGet(runtime.CharacterId, out master);

        Sprite characterIcon = master != null ? master.Icon : null;
        if (characterIcon == null && DataManager.Instance?.CharacterIconDatabase != null)
            DataManager.Instance.CharacterIconDatabase.TryGetIcon(runtime.CharacterId, out characterIcon);
        ApplySprite(slot.CharacterIcon, characterIcon);

        Sprite passiveIcon = null;
        if (!string.IsNullOrWhiteSpace(runtime.PassiveSkillId) && DataManager.Instance?.SkillIconDatabase != null)
            DataManager.Instance.SkillIconDatabase.TryGetIcon(runtime.PassiveSkillId, out passiveIcon);
        ApplySprite(slot.PassiveIcon, passiveIcon);

        int maxHp = Mathf.Max(0, runtime.MaxHP + runtime.RunMaxHPBonus);
        int maxCost = Mathf.Max(0, runtime.MaxCost + runtime.RunMaxCostBonus);
        if (slot.HpFill != null)
            slot.HpFill.fillAmount = maxHp > 0 ? Mathf.Clamp01((float)runtime.PreviewHP / maxHp) : 0f;
        if (slot.CostFill != null)
            slot.CostFill.fillAmount = maxCost > 0 ? Mathf.Clamp01((float)runtime.PreviewCost / maxCost) : 0f;

        int karma = Mathf.Clamp(runtime.PreviewResource, 0, 5);
        for (int i = 0; i < slot.Karma.Length; i++)
        {
            if (slot.Karma[i] != null)
                slot.Karma[i].color = i < karma ? KarmaOnColor : KarmaOffColor;
        }

        for (int i = 0; i < slot.Runes.Length; i++)
        {
            string id = runtime.EquippedRuneIds != null && i < runtime.EquippedRuneIds.Length
                ? runtime.EquippedRuneIds[i]
                : null;
            Sprite icon = null;
            if (!string.IsNullOrWhiteSpace(id) && DataManager.Instance?.RuneIconDatabase != null)
                DataManager.Instance.RuneIconDatabase.TryGetIcon(id, out icon);
            ApplySprite(slot.Runes[i], icon);
        }

        // EquippedRelicIds[0]은 액티브 유물 슬롯이고, 이 영역은 장착 아티팩트 6칸이므로 1~6을 표시합니다.
        for (int i = 0; i < slot.Artifacts.Length; i++)
        {
            int relicIndex = i + 1;
            string id = runtime.EquippedRelicIds != null && relicIndex < runtime.EquippedRelicIds.Length
                ? runtime.EquippedRelicIds[relicIndex]
                : null;
            Sprite icon = null;
            if (!string.IsNullOrWhiteSpace(id) && DataManager.Instance?.RelicIconDatabase != null)
                DataManager.Instance.RelicIconDatabase.TryGetIcon(id, out icon);
            ApplySprite(slot.Artifacts[i], icon);
        }

        if (forceStatusRebuild)
            RebuildStatusIcons(slot, runtime.StatusEffects);
        else
            RebuildStatusIcons(slot, runtime.StatusEffects);

        slot.LastHash = CalculateRuntimeHash(runtime);
    }

    private void RebuildStatusIcons(SlotView slot, List<StatusEffectRuntimeData> source)
    {
        if (slot.StatusContent == null || statusEffectIconPrefab == null)
            return;

        Dictionary<string, StatusEffectRuntimeData> merged = new(StringComparer.Ordinal);
        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                StatusEffectRuntimeData effect = source[i];
                if (effect == null || string.IsNullOrWhiteSpace(effect.EffectId))
                    continue;

                if (merged.TryGetValue(effect.EffectId, out StatusEffectRuntimeData existing))
                {
                    existing.Stack += effect.Stack;
                    existing.TurnCount = Mathf.Max(existing.TurnCount, effect.TurnCount);
                }
                else
                {
                    merged.Add(effect.EffectId, new StatusEffectRuntimeData
                    {
                        EffectId = effect.EffectId,
                        Stack = effect.Stack,
                        TurnCount = effect.TurnCount,
                        IsPassive = effect.IsPassive,
                        SourceSkillId = effect.SourceSkillId
                    });
                }
            }
        }

        ClearStatusIcons(slot);
        foreach (StatusEffectRuntimeData effect in merged.Values)
        {
            StatusEffectIcon icon = Instantiate(statusEffectIconPrefab, slot.StatusContent);
            icon.gameObject.name = $"StatusEffect_{effect.EffectId}";
            icon.Set(effect);
            slot.SpawnedStatusIcons.Add(icon);
        }
    }

    private static int CalculateRuntimeHash(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return 0;

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + runtime.PreviewHP;
            hash = hash * 31 + runtime.PreviewCost;
            hash = hash * 31 + runtime.PreviewResource;
            hash = hash * 31 + runtime.MaxHP + runtime.RunMaxHPBonus;
            hash = hash * 31 + runtime.MaxCost + runtime.RunMaxCostBonus;
            hash = hash * 31 + (runtime.PassiveSkillId ?? string.Empty).GetHashCode();

            if (runtime.EquippedRuneIds != null)
                for (int i = 0; i < runtime.EquippedRuneIds.Length; i++)
                    hash = hash * 31 + (runtime.EquippedRuneIds[i] ?? string.Empty).GetHashCode();
            if (runtime.EquippedRelicIds != null)
                for (int i = 0; i < runtime.EquippedRelicIds.Length; i++)
                    hash = hash * 31 + (runtime.EquippedRelicIds[i] ?? string.Empty).GetHashCode();
            if (runtime.StatusEffects != null)
                for (int i = 0; i < runtime.StatusEffects.Count; i++)
                {
                    StatusEffectRuntimeData e = runtime.StatusEffects[i];
                    if (e == null) continue;
                    hash = hash * 31 + (e.EffectId ?? string.Empty).GetHashCode();
                    hash = hash * 31 + e.Stack;
                    hash = hash * 31 + e.TurnCount;
                }
            return hash;
        }
    }

    private static void ClearStatusIcons(SlotView slot)
    {
        for (int i = slot.SpawnedStatusIcons.Count - 1; i >= 0; i--)
        {
            if (slot.SpawnedStatusIcons[i] != null)
                Destroy(slot.SpawnedStatusIcons[i].gameObject);
        }
        slot.SpawnedStatusIcons.Clear();
    }

    private static void ApplySprite(Image image, Sprite sprite)
    {
        if (image == null)
            return;
        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
    }

    private static Image GetImage(Transform target) => target != null ? target.GetComponent<Image>() : null;

    private static Transform FindDirectChild(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name == name)
                return child;
        }
        return null;
    }

    private static Transform FindPath(Transform root, string path)
    {
        if (root == null || string.IsNullOrWhiteSpace(path))
            return null;
        Transform current = root;
        string[] parts = path.Split('/');
        for (int p = 0; p < parts.Length; p++)
        {
            current = FindDirectChild(current, parts[p]);
            if (current == null)
                return null;
        }
        return current;
    }
}

public sealed class BattlePartyCharacterSelectTarget : MonoBehaviour, IPointerClickHandler
{
    private BattlePartyCharacterPanelUI owner;
    private int index;

    public void Configure(BattlePartyCharacterPanelUI panel, int partyIndex)
    {
        owner = panel;
        index = partyIndex;

        Graphic graphic = GetComponent<Graphic>();
        if (graphic != null)
            graphic.raycastTarget = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        owner?.SelectIndex(index);
    }
}
