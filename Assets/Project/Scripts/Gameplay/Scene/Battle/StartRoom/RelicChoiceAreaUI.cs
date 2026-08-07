using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class RelicChoiceAreaUI : MonoBehaviour
{
    [Header("Slots In Scene")]
    [SerializeField] private RelicChoiceSlotUI[] choiceSlots;

    [Header("Choice Setting")]
    [SerializeField, Min(1)] private int choiceCount = 3;
    [SerializeField] private Button acquireButton;

    [Header("Complete")]
    [SerializeField] private BattleMapController battleMapController;
    [SerializeField] private StartRoomController startRoomController;

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

        if (SteamBattleStateSynchronizer.TryApplyKnownStartRelicChoices(this))
            return;

        if (!SteamBattleStateSynchronizer.CanLocalPlayerMutateSharedBattleState())
        {
            ClearSlots();
            return;
        }

        SetupChoices();
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

        if (!GrantRelic(relicId))
        {
            isSelectionCompleted = false;
            SetupChoices();
            return;
        }

        PlayAcquireSfx();
        RefreshRelicEquipPanel();
        CompleteChoiceEvent(relicId);
    }

    private bool GrantRelic(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId) || HasRelicAnywhere(relicId))
            return false;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();
        runtime.OwnedRelicIds ??= new List<string>();

        runtime.OwnedRelicIds.Add(relicId.Trim());
        NormalizeOwnedRelics(runtime);
        DataManager.Instance.BattleRuntimeStore.Set(runtime);
        return true;
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
