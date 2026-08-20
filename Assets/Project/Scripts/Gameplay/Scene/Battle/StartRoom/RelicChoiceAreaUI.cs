using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;

public class RelicChoiceAreaUI : MonoBehaviour
{
    [Header("Slots In Scene")]
    [SerializeField] private RelicChoiceSlotUI[] choiceSlots;

    [Header("Choice Setting")]
    [SerializeField, Min(1)] private int choiceCount = 3;
    [SerializeField] private Button acquireButton;

    [Header("Skill Reward")]
    [SerializeField] private BattleRewardPanelUI rewardPanel;
    [SerializeField, Min(1)] private int skillRewardCount = StartRoomSkillRewardSelectionUtility.RewardCountPerChoice;

    [Header("Complete")]
    [SerializeField] private BattleMapController battleMapController;
    [SerializeField] private StartRoomController startRoomController;

    [Header("Canvas Handoff")]
    [Tooltip("Equip_panel이 열리는 동안 숨길 시작방 선택 Canvas입니다. 비워두면 부모의 RelicChoiceCanvas/Canvas를 자동으로 찾습니다.")]
    [SerializeField] private GameObject relicChoiceCanvas;

    [Header("SFX")]
    [SerializeField] private bool playAcquireSfx = true;
    [SerializeField] private SfxType acquireSfxType = SfxType.RelicChoiceAcquire;

    private bool isOpen;
    private bool isSelectionCompleted;
    private string selectedRelicId;
    private RelicChoiceSlotUI selectedSlot;

    private void Awake()
    {
        if (startRoomController == null)
            startRoomController = GetComponentInParent<StartRoomController>(true);

        if (battleMapController == null)
            battleMapController = Object.FindFirstObjectByType<BattleMapController>(FindObjectsInactive.Include);

        EnsureRewardPanelReference();

        if (relicChoiceCanvas == null)
            relicChoiceCanvas = FindRelicChoiceCanvas();

        // Choice Slot을 클릭하면 즉시 유물을 습득하므로 Acquire Button은 사용하지 않습니다.
        if (acquireButton != null)
        {
            acquireButton.onClick.RemoveListener(AcquireSelectedRelic);
            acquireButton.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        ClearSelection();
    }

    private void OnDestroy()
    {
        if (acquireButton != null)
            acquireButton.onClick.RemoveListener(AcquireSelectedRelic);
    }

    public void Open()
    {
        isOpen = true;
        isSelectionCompleted = false;
        ClearSelection();

        gameObject.SetActive(true);

        SetupSkillRewardChoices();
    }

    public void Close()
    {
        isOpen = false;
        ClearSelection();
        ClearSlots();
        gameObject.SetActive(false);
    }

    public void ApplyNetworkChoices(IReadOnlyList<string> relicIds)
    {
        if (isSelectionCompleted)
            return;

        isOpen = true;
        ClearSelection();
        gameObject.SetActive(true);
        SetupChoices(relicIds, false);
    }

    private void SetupChoices()
    {
        SetupChoices(PickRandomRelicIds(), true);
    }

    private void SetupSkillRewardChoices()
    {
        SetupSkillRewardChoices(StartRoomSkillRewardSelectionUtility.DefaultChoices);
    }

    private void SetupSkillRewardChoices(IReadOnlyList<StartRoomSkillRewardChoice> choices)
    {
        ClearSelection();
        ClearSlots();

        List<RelicChoiceSlotUI> validSlots = GetValidSlots();
        if (validSlots.Count == 0)
        {
            Debug.LogWarning("[RelicChoiceAreaUI] Choice Slots are empty. Put RelicChoiceSlot_1, RelicChoiceSlot_2, RelicChoiceSlot_3 into Choice Slots in the Inspector.");
            return;
        }

        List<StartRoomSkillRewardChoice> validChoices = NormalizeSkillRewardChoices(choices);
        if (validChoices.Count == 0)
        {
            Debug.LogWarning("[RelicChoiceAreaUI] No selectable start room skill reward choices were found.");
            return;
        }

        int count = Mathf.Min(choiceCount, validChoices.Count, validSlots.Count);
        for (int i = 0; i < validSlots.Count; i++)
        {
            RelicChoiceSlotUI slot = validSlots[i];
            if (i < count)
            {
                slot.gameObject.SetActive(true);
                slot.SetupSkillRewardChoice(validChoices[i], this);
            }
            else
            {
                slot.ClearSlot();
                slot.gameObject.SetActive(false);
            }
        }
    }

    private void SetupChoices(IReadOnlyList<string> relicIds, bool broadcastChoices)
    {
        ClearSelection();
        ClearSlots();

        List<RelicChoiceSlotUI> validSlots = GetValidSlots();
        if (validSlots.Count == 0)
        {
            Debug.LogWarning("[RelicChoiceAreaUI] Choice Slots are empty. Put RelicChoiceSlot_1, RelicChoiceSlot_2, RelicChoiceSlot_3 into Choice Slots in the Inspector.");
            return;
        }

        List<string> normalizedRelicIds = NormalizeChoiceIds(relicIds);
        if (normalizedRelicIds.Count == 0)
        {
            Debug.LogWarning("[RelicChoiceAreaUI] No selectable relic ids were found.");
            return;
        }

        int count = Mathf.Min(choiceCount, normalizedRelicIds.Count, validSlots.Count);
        for (int i = 0; i < validSlots.Count; i++)
        {
            RelicChoiceSlotUI slot = validSlots[i];
            if (i < count)
            {
                slot.gameObject.SetActive(true);
                slot.Setup(normalizedRelicIds[i], this);
            }
            else
            {
                slot.ClearSlot();
                slot.gameObject.SetActive(false);
            }
        }

        if (broadcastChoices)
            SteamBattleStateSynchronizer.TryBroadcastStartRelicChoices(normalizedRelicIds.GetRange(0, count));

    }

    private List<string> NormalizeChoiceIds(IReadOnlyList<string> relicIds)
    {
        List<string> normalized = new();
        HashSet<string> uniqueIds = new();

        if (relicIds == null)
            return normalized;

        for (int i = 0; i < relicIds.Count; i++)
        {
            string relicId = relicIds[i];
            if (string.IsNullOrWhiteSpace(relicId))
                continue;

            relicId = relicId.Trim();
            if (uniqueIds.Add(relicId))
                normalized.Add(relicId);
        }

        return normalized;
    }

    private List<StartRoomSkillRewardChoice> NormalizeSkillRewardChoices(
        IReadOnlyList<StartRoomSkillRewardChoice> choices)
    {
        List<StartRoomSkillRewardChoice> normalized = new();
        HashSet<SkillType> uniqueTypes = new();

        if (choices == null)
            return normalized;

        for (int i = 0; i < choices.Count; i++)
        {
            StartRoomSkillRewardChoice choice = choices[i];
            if (!choice.IsValid)
                continue;

            if (uniqueTypes.Add(choice.SkillType))
                normalized.Add(choice);
        }

        return normalized;
    }

    private List<RelicChoiceSlotUI> GetValidSlots()
    {
        List<RelicChoiceSlotUI> validSlots = new();

        if (choiceSlots == null)
            return validSlots;

        for (int i = 0; i < choiceSlots.Length; i++)
        {
            if (choiceSlots[i] != null && !validSlots.Contains(choiceSlots[i]))
                validSlots.Add(choiceSlots[i]);
        }

        return validSlots;
    }

    private List<string> PickRandomRelicIds()
    {
        if (DataManager.Instance == null || DataManager.Instance.RelicDatabase == null)
        {
            Debug.LogWarning("[RelicChoiceAreaUI] DataManager or RelicDatabase is null.");
            return new List<string>();
        }

        List<string> candidates = StartRoomRelicSelectionUtility.CollectActiveRelicIds(
            DataManager.Instance.RelicDatabase.GetAll());

        RemoveAlreadyOwnedRelics(candidates);
        Shuffle(candidates);

        int count = Mathf.Min(choiceCount, candidates.Count);
        if (count <= 0)
            return new List<string>();

        return candidates.GetRange(0, count);
    }

    private void RemoveAlreadyOwnedRelics(List<string> candidates)
    {
        if (candidates == null || candidates.Count == 0)
            return;

        HashSet<string> unavailableRelicIds = GetUnavailableRelicIds();

        if (unavailableRelicIds.Count == 0)
            return;

        candidates.RemoveAll(id => !string.IsNullOrWhiteSpace(id) && unavailableRelicIds.Contains(id.Trim()));
    }

    public void SelectSlot(RelicChoiceSlotUI slot, string relicId)
    {
        if (!isOpen || isSelectionCompleted || slot == null || string.IsNullOrWhiteSpace(relicId))
            return;

        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        selectedSlot = slot;
        selectedRelicId = relicId.Trim();

        List<RelicChoiceSlotUI> validSlots = GetValidSlots();
        for (int i = 0; i < validSlots.Count; i++)
            validSlots[i].SetSelected(validSlots[i] == selectedSlot);

        // 선택 상태만 만드는 것이 아니라 슬롯 클릭 즉시 유물을 습득합니다.
        SelectRelic(selectedRelicId);
    }

    public void SelectSkillRewardChoice(RelicChoiceSlotUI slot, StartRoomSkillRewardChoice choice)
    {
        if (!isOpen || isSelectionCompleted || slot == null || !choice.IsValid)
            return;

        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        selectedSlot = slot;
        selectedRelicId = choice.ChoiceId;

        List<RelicChoiceSlotUI> validSlots = GetValidSlots();
        for (int i = 0; i < validSlots.Count; i++)
            validSlots[i].SetSelected(validSlots[i] == selectedSlot);

        SelectSkillRewardChoice(choice);
    }

    public void AcquireSelectedRelic()
    {
        if (string.IsNullOrWhiteSpace(selectedRelicId))
            return;

        SelectRelic(selectedRelicId);
    }

    public void SelectRelic(string relicId)
    {
        if (!isOpen || isSelectionCompleted)
            return;

        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        if (string.IsNullOrWhiteSpace(relicId))
            return;

        if (DataManager.Instance == null || DataManager.Instance.BattleRuntimeStore == null || DataManager.Instance.RelicDatabase == null)
        {
            Debug.LogWarning("[RelicChoiceAreaUI] DataManager, BattleRuntimeStore, or RelicDatabase is null.");
            return;
        }

        relicId = relicId.Trim();

        if (!DataManager.Instance.RelicDatabase.TryGet(relicId, out _))
        {
            Debug.LogWarning($"[RelicChoiceAreaUI] Unknown relic id: {relicId}");
            return;
        }

        if (HasRelicAnywhere(relicId))
        {
            Debug.LogWarning($"[RelicChoiceAreaUI] 이미 보유 중인 유물입니다. Relic:{relicId}");
            SetupChoices();
            return;
        }

        isSelectionCompleted = true;
        SteamBattleStateSynchronizer.TryBroadcastStartRelicSelected(relicId);

        PlayAcquireSfx();

        // 시작방 선택 Canvas가 Equip_panel 뒤에 그대로 남지 않도록 먼저 숨깁니다.
        // Close()를 사용하면 선택 슬롯이 초기화되므로 Canvas 활성 상태만 임시로 변경합니다.
        SetRelicChoiceCanvasVisible(false);

        if (BattleRewardEquipPanelUI.TryOpenRelicReward(
                relicId,
                () =>
                {
                    RefreshRelicEquipPanel();
                    RestoreChoiceCanvasAndComplete(relicId);
                }))
        {
            return;
        }

        SetRelicChoiceCanvasVisible(true);
        Debug.LogWarning($"[RelicChoiceAreaUI] Equip_panel을 찾을 수 없어 유물 선택을 완료하지 않았습니다. Relic:{relicId}");
        isSelectionCompleted = false;
        SetupChoices();
    }

    private void SelectSkillRewardChoice(StartRoomSkillRewardChoice choice)
    {
        if (!isOpen || isSelectionCompleted || !choice.IsValid)
            return;

        if (!TryCreateSkillRewards(
                choice.SkillType,
                skillRewardCount,
                out List<BattleRewardData> rewards,
                out string resultMessage))
        {
            Debug.LogWarning($"[RelicChoiceAreaUI] {resultMessage}");
            SetupSkillRewardChoices();
            return;
        }

        EnsureRewardPanelReference();
        if (rewardPanel == null)
        {
            Debug.LogWarning("[RelicChoiceAreaUI] Shared BattleRewardPanelUI not found for start room skill rewards.");
            SetupSkillRewardChoices();
            return;
        }

        isSelectionCompleted = true;
        PlayAcquireSfx();
        SetRelicChoiceCanvasVisible(false);

        rewardPanel.Open(rewards, () => RestoreChoiceCanvasAndComplete(choice.ChoiceId));
    }


    private GameObject FindRelicChoiceCanvas()
    {
        Transform current = transform;
        while (current != null)
        {
            if (string.Equals(current.name, "RelicChoiceCanvas", System.StringComparison.OrdinalIgnoreCase))
                return current.gameObject;

            current = current.parent;
        }

        Canvas parentCanvas = GetComponentInParent<Canvas>(true);
        return parentCanvas != null ? parentCanvas.gameObject : gameObject;
    }

    private void SetRelicChoiceCanvasVisible(bool visible)
    {
        if (relicChoiceCanvas == null)
            relicChoiceCanvas = FindRelicChoiceCanvas();

        if (relicChoiceCanvas != null && relicChoiceCanvas.activeSelf != visible)
            relicChoiceCanvas.SetActive(visible);
    }

    private void RestoreChoiceCanvasAndComplete(string relicId)
    {
        SetRelicChoiceCanvasVisible(true);

        if (isActiveAndEnabled)
            StartCoroutine(CompleteChoiceAfterCanvasRestoreRoutine(relicId));
        else
            CompleteChoiceEvent(relicId);
    }

    private bool TryCreateSkillRewards(
        SkillType skillType,
        int count,
        out List<BattleRewardData> rewards,
        out string resultMessage)
    {
        rewards = new List<BattleRewardData>();
        resultMessage = string.Empty;

        int rewardCount = Mathf.Max(0, count);
        if (rewardCount <= 0)
        {
            resultMessage = "Start room skill reward count is invalid.";
            return false;
        }

        if (DataManager.Instance == null ||
            DataManager.Instance.SkillDatabase == null ||
            DataManager.Instance.BattleRuntimeStore == null)
        {
            resultMessage = "DataManager, SkillDatabase, or BattleRuntimeStore is null.";
            return false;
        }

        IReadOnlyList<SkillMasterData> allSkills = DataManager.Instance.SkillDatabase.GetAll();
        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();
        IReadOnlyDictionary<string, CharacterRuntimeData> characters =
            DataManager.Instance.CharacterRuntimeStore?.GetAll();

        HashSet<string> unavailableSkillIds =
            StartRoomSkillRewardSelectionUtility.CollectUnavailableSkillIds(runtime, characters);
        List<SkillMasterData> candidates =
            StartRoomSkillRewardSelectionUtility.CollectAvailableCoreSkillRewards(
                allSkills,
                skillType,
                unavailableSkillIds);

        if (candidates.Count < rewardCount)
        {
            resultMessage = $"Not enough available {skillType} core skill rewards.";
            return false;
        }

        for (int i = 0; i < rewardCount; i++)
        {
            int selectedIndex = BattleRandom.Range(0, candidates.Count);
            SkillMasterData skill = candidates[selectedIndex];
            candidates.RemoveAt(selectedIndex);

            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                continue;

            string skillId = skill.SkillId.Trim();
            rewards.Add(CreateSkillReward(skill, GetSkillSprite(skillId, skill)));
            StartRoomSkillRewardSelectionUtility.AddSkillAndPair(unavailableSkillIds, skillId);
        }

        if (rewards.Count <= 0)
        {
            resultMessage = "No valid start room skill rewards were created.";
            return false;
        }

        return true;
    }

    private BattleRewardData CreateSkillReward(SkillMasterData skill, Sprite icon)
    {
        string skillId = skill != null && !string.IsNullOrWhiteSpace(skill.SkillId)
            ? skill.SkillId.Trim()
            : string.Empty;

        return new BattleRewardData
        {
            Type = BattleRewardType.Skill,
            RewardId = skillId,
            SourceKey = $"StartRoom|Skill|{skillId}",
            Amount = 1,
            Icon = icon,
            Name = skill != null ? GameDataLocalization.SkillName(skill) : skillId,
            Description = BuildSkillDescription(skill)
        };
    }

    private string BuildSkillDescription(SkillMasterData skill)
    {
        if (skill == null)
            return string.Empty;

        string rarityName = SkillRarityUtility.GetDisplayName(skill.Rarity);
        string description = GameDataLocalization.SkillDetails(skill);

        if (string.IsNullOrWhiteSpace(description))
            description = GameLocalization.Get("battle.available_skill", "획득 가능한 기억입니다.");

        return string.IsNullOrWhiteSpace(rarityName)
            ? description
            : $"[{rarityName}] {description}";
    }

    private Sprite GetSkillSprite(string skillId, SkillMasterData skill)
    {
        if (skill != null && skill.Icon != null)
            return skill.Icon;

        if (string.IsNullOrWhiteSpace(skillId) ||
            DataManager.Instance == null ||
            DataManager.Instance.SkillIconDatabase == null)
        {
            return null;
        }

        return DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId.Trim(), out Sprite icon)
            ? icon
            : null;
    }

    private void EnsureRewardPanelReference()
    {
        if (rewardPanel != null)
            return;

        rewardPanel = GetComponentInChildren<BattleRewardPanelUI>(true);

        if (rewardPanel == null)
            rewardPanel = Object.FindFirstObjectByType<BattleRewardPanelUI>(FindObjectsInactive.Include);
    }

    private IEnumerator CompleteChoiceAfterCanvasRestoreRoutine(string relicId)
    {
        // Canvas가 다시 표시된 한 프레임 뒤 시작방 완료 처리를 진행합니다.
        yield return null;
        CompleteChoiceEvent(relicId);
    }

    private void PlayAcquireSfx()
    {
        if (!playAcquireSfx)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(acquireSfxType);
    }

    private HashSet<string> GetUnavailableRelicIds()
    {
        HashSet<string> ids = new();

        if (DataManager.Instance == null)
            return ids;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore?.GetOrCreate();

        if (runtime?.OwnedRelicIds != null)
        {
            for (int i = 0; i < runtime.OwnedRelicIds.Count; i++)
                AddRelicId(ids, runtime.OwnedRelicIds[i]);
        }

        IReadOnlyDictionary<string, CharacterRuntimeData> characters =
            DataManager.Instance.CharacterRuntimeStore?.GetAll();

        if (characters != null)
        {
            foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters)
            {
                CharacterRuntimeData character = pair.Value;

                if (character?.EquippedRelicIds == null)
                    continue;

                for (int i = 0; i < character.EquippedRelicIds.Length; i++)
                    AddRelicId(ids, character.EquippedRelicIds[i]);
            }
        }

        return ids;
    }

    private bool HasRelicAnywhere(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return false;

        return GetUnavailableRelicIds().Contains(relicId.Trim());
    }

    private void AddRelicId(HashSet<string> ids, string relicId)
    {
        if (ids == null || string.IsNullOrWhiteSpace(relicId))
            return;

        ids.Add(relicId.Trim());
    }

    private void NormalizeOwnedRelics(BattleRuntimeData runtime)
    {
        if (runtime == null)
            return;

        runtime.OwnedRelicIds ??= new List<string>();
        HashSet<string> uniqueIds = new();

        for (int i = runtime.OwnedRelicIds.Count - 1; i >= 0; i--)
        {
            string relicId = runtime.OwnedRelicIds[i];

            if (string.IsNullOrWhiteSpace(relicId))
            {
                runtime.OwnedRelicIds.RemoveAt(i);
                continue;
            }

            relicId = relicId.Trim();

            if (!uniqueIds.Add(relicId))
            {
                runtime.OwnedRelicIds.RemoveAt(i);
                continue;
            }

            runtime.OwnedRelicIds[i] = relicId;
        }
    }

    private void RefreshRelicEquipPanel()
    {
        RelicEquipPanelUI.RefreshAll();
    }

    private void CompleteChoiceEvent(string relicId)
    {
        if (startRoomController != null)
            startRoomController.OnRelicChoiceFinished(relicId);
        else
            Debug.LogWarning("[RelicChoiceAreaUI] StartRoomController is not connected.");
    }

    private void ClearSlots()
    {
        List<RelicChoiceSlotUI> validSlots = GetValidSlots();
        for (int i = 0; i < validSlots.Count; i++)
            validSlots[i].ClearSlot();
    }

    private void ClearSelection()
    {
        selectedRelicId = string.Empty;
        selectedSlot = null;

        List<RelicChoiceSlotUI> validSlots = GetValidSlots();
        for (int i = 0; i < validSlots.Count; i++)
            validSlots[i].SetSelected(false);

    }

    private void Shuffle(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}

public static class StartRoomRelicSelectionUtility
{
    private const string ActiveRelicIdPrefix = "Relic_A_";

    public static List<string> CollectActiveRelicIds(IReadOnlyList<RelicData> relics)
    {
        List<string> result = new();

        if (relics == null)
            return result;

        for (int i = 0; i < relics.Count; i++)
        {
            string id = relics[i]?.FragmentId?.Trim();

            if (!string.IsNullOrWhiteSpace(id) &&
                id.StartsWith(ActiveRelicIdPrefix, System.StringComparison.Ordinal))
            {
                result.Add(id);
            }
        }

        return result;
    }
}

public readonly struct StartRoomSkillRewardChoice
{
    public StartRoomSkillRewardChoice(SkillType skillType, string title, string description)
    {
        SkillType = skillType;
        Title = title;
        Description = description;
    }

    public SkillType SkillType { get; }
    public string Title { get; }
    public string Description { get; }

    public string ChoiceId => SkillType switch
    {
        SkillType.Attack => "StartRoom_Event09_Attack",
        SkillType.Buff => "StartRoom_Event09_Buff",
        SkillType.Debuff => "StartRoom_Event09_Debuff",
        _ => string.Empty
    };

    public bool IsValid => SkillType == SkillType.Attack ||
                           SkillType == SkillType.Buff ||
                           SkillType == SkillType.Debuff;
}

public static class StartRoomSkillRewardSelectionUtility
{
    public const int RewardCountPerChoice = 2;

    private static readonly StartRoomSkillRewardChoice[] DefaultChoiceSet =
    {
        new(SkillType.Attack, "공격 관련 기억", "보유하지 않은 공격 기억 2개를 제시합니다."),
        new(SkillType.Buff, "버프 관련 기억", "보유하지 않은 버프 기억 2개를 제시합니다."),
        new(SkillType.Debuff, "디버프 관련 기억", "보유하지 않은 디버프 기억 2개를 제시합니다.")
    };

    public static IReadOnlyList<StartRoomSkillRewardChoice> DefaultChoices => DefaultChoiceSet;

    public static List<SkillMasterData> CollectAvailableCoreSkillRewards(
        IReadOnlyList<SkillMasterData> allSkills,
        SkillType requiredSkillType,
        ISet<string> unavailableSkillIds)
    {
        List<SkillMasterData> result = new();

        if (allSkills == null)
            return result;

        HashSet<string> normalizedUnavailableIds = NormalizeSkillIds(unavailableSkillIds);

        for (int i = 0; i < allSkills.Count; i++)
        {
            SkillMasterData skill = allSkills[i];
            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                continue;

            string skillId = skill.SkillId.Trim();

            if (skill.Category != Category.Core)
                continue;

            if (skill.SkillType != requiredSkillType)
                continue;

            if (!SkillRarityUtility.IsBaseSkillVariant(skillId))
                continue;

            if (normalizedUnavailableIds.Contains(skillId))
                continue;

            result.Add(skill);
        }

        return result;
    }

    public static HashSet<string> CollectUnavailableSkillIds(
        BattleRuntimeData runtime,
        IReadOnlyDictionary<string, CharacterRuntimeData> characters,
        IReadOnlyList<BattleRewardData> pendingRewards = null)
    {
        HashSet<string> ids = new();

        if (runtime?.SkillInventoryIds != null)
        {
            for (int i = 0; i < runtime.SkillInventoryIds.Count; i++)
                AddSkillAndPair(ids, runtime.SkillInventoryIds[i]);
        }

        if (characters != null)
        {
            foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters)
            {
                CharacterRuntimeData character = pair.Value;
                if (character == null)
                    continue;

                AddSkillAndPair(ids, character.MoveSkillId);
                AddSkillAndPair(ids, character.PassiveSkillId);
                AddSkillAndPair(ids, character.UniqueSkillId);
                AddSkillAndPair(ids, character.AbilitySkillId);

                if (character.EquippedSkillIds == null)
                    continue;

                for (int i = 0; i < character.EquippedSkillIds.Length; i++)
                    AddSkillAndPair(ids, character.EquippedSkillIds[i]);
            }
        }

        if (pendingRewards != null)
        {
            for (int i = 0; i < pendingRewards.Count; i++)
            {
                BattleRewardData reward = pendingRewards[i];
                if (reward != null && reward.Type == BattleRewardType.Skill)
                    AddSkillAndPair(ids, reward.RewardId);
            }
        }

        return ids;
    }

    public static void AddSkillAndPair(HashSet<string> ids, string skillId)
    {
        if (ids == null || string.IsNullOrWhiteSpace(skillId))
            return;

        string normalizedSkillId = skillId.Trim();
        ids.Add(normalizedSkillId);

        if (SkillRarityUtility.TryGetPairedVariantId(normalizedSkillId, out string pairedSkillId))
            ids.Add(pairedSkillId);
    }

    private static HashSet<string> NormalizeSkillIds(ISet<string> skillIds)
    {
        HashSet<string> result = new();

        if (skillIds == null)
            return result;

        foreach (string skillId in skillIds)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                continue;

            result.Add(skillId.Trim());
        }

        return result;
    }
}
