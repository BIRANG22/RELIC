using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

[DisallowMultipleComponent]
public sealed class SteamBattleStateSynchronizer : MonoBehaviour
{
    private const string SnapshotLobbyDataKey = "relic.battle.state.snapshot.v1";
    private const string SnapshotBroadcastPrefix = "RELIC_BATTLE_STATE_SNAPSHOT_V1:";
    private const string CommandPrefix = "RELIC_BATTLE_STATE_CMD_V1:";
    private const string CommandResultPrefix = "RELIC_BATTLE_STATE_RESULT_V1:";
    private const int MaxLobbyChatMessageBytes = 4096;

    public static SteamBattleStateSynchronizer Instance { get; private set; }

    public bool IsNetworkBattleActive { get; private set; }
    public long AppliedRevision { get; private set; }
    public BattleNetworkSnapshot CurrentSnapshot { get; private set; }

    private BattleTurnExecutor turnExecutor;
    private BattleTimelineController timelineController;
    private BattleTurnReadyPanelUI readyPanel;

    private readonly Dictionary<ulong, int> viewedSlotsByMember = new();
    private readonly Dictionary<ulong, bool> readyByMember = new();

    private long hostRevision;
    private string lastPublishedPayload;
    private float hostPublishTimer;
    private float clientPollTimer;
    private bool applyingNetworkTimeline;

#if STEAMWORKS_NET
    private CSteamID currentLobbyId;
    private ulong localSteamId;
    private ulong originalHostSteamId;
    private Callback<LobbyChatMsg_t> lobbyChatMessage;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        BattleTurnExecutor.BattleExecutionStarted -= OnBattleExecutionStarted;
        BattleTurnExecutor.PlayerTurnReturned -= OnPlayerTurnReturned;

#if STEAMWORKS_NET
        lobbyChatMessage?.Dispose();
#endif

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
#if STEAMWORKS_NET
        if (!IsNetworkBattleActive)
            return;

        if (IsLocalHost())
        {
            hostPublishTimer += Time.unscaledDeltaTime;
            if (hostPublishTimer >= 0.3f)
            {
                hostPublishTimer = 0f;
                PublishSnapshotIfChanged();
            }
        }
        else
        {
            clientPollTimer += Time.unscaledDeltaTime;
            if (clientPollTimer >= 0.25f)
            {
                clientPollTimer = 0f;
                ApplySnapshotFromLobbyData();
            }
        }
#endif
    }

    public static SteamBattleStateSynchronizer EnsureForBattleScene(
        BattleTurnExecutor executor,
        BattleTimelineController timeline)
    {
        if (!SteamLobbySessionState.TryGetLobby(out _, out _, out _))
            return null;

        SteamBattleStateSynchronizer synchronizer = Instance;

        if (synchronizer == null)
        {
            GameObject obj = new GameObject("[SteamBattleStateSynchronizer]");
            synchronizer = obj.AddComponent<SteamBattleStateSynchronizer>();
        }

        synchronizer.Bind(executor, timeline);
        return synchronizer;
    }

    public static bool CanLocalPlayerControlCharacter(string characterId)
    {
        if (Instance == null || !Instance.IsNetworkBattleActive)
            return true;

#if STEAMWORKS_NET
        return SteamLobbySessionState.IsMemberAllowedToControlCharacter(
            Instance.localSteamId,
            characterId);
#else
        return false;
#endif
    }

    public static bool CanLocalPlayerMutateSharedBattleState()
    {
        return Instance == null ||
               !Instance.IsNetworkBattleActive ||
               SteamLobbySessionState.IsLocalHost;
    }

    public static bool TryBlockSharedBattleStateEdit(string message = null)
    {
        if (CanLocalPlayerMutateSharedBattleState())
            return false;

        BattleWarningUI.ShowMessage(
            string.IsNullOrWhiteSpace(message)
                ? "멀티 배틀에서는 호스트만 공유 상태를 변경할 수 있습니다."
                : message);
        return true;
    }

    public static bool TryHandleExecuteTurnRequest(BattleTurnExecutor executor)
    {
        if (Instance == null ||
            !Instance.IsNetworkBattleActive ||
            executor == null ||
            Instance.turnExecutor != executor)
        {
            return false;
        }

        Instance.ToggleLocalReady();
        return true;
    }

    public static bool TryHandleTimelineSlotClicked(
        BattleTimelineController controller,
        int slotIndex)
    {
        if (Instance == null ||
            !Instance.IsNetworkBattleActive ||
            controller == null ||
            Instance.timelineController != controller)
        {
            return false;
        }

        Instance.RequestTimelineSlotSelection(slotIndex);
        return true;
    }

    public static bool TryHandlePlayerCommandReservation(
        BattleTimelineController controller,
        int slotIndex,
        PlayerReservedCommand command,
        out bool accepted)
    {
        accepted = false;

        if (Instance == null ||
            !Instance.IsNetworkBattleActive ||
            Instance.applyingNetworkTimeline ||
            controller == null ||
            Instance.timelineController != controller)
        {
            return false;
        }

        accepted = Instance.RequestPlayerCommandReservation(slotIndex, command);
        return true;
    }

    public static bool TryHandleRemoveCommand(
        BattleTimelineController controller,
        int slotIndex,
        int orderIndex,
        out bool accepted)
    {
        accepted = false;

        if (Instance == null ||
            !Instance.IsNetworkBattleActive ||
            Instance.applyingNetworkTimeline ||
            controller == null ||
            Instance.timelineController != controller)
        {
            return false;
        }

        accepted = Instance.RequestRemoveCommand(slotIndex, orderIndex);
        return true;
    }

    public bool CanLocalPlayerEditCharacter(string characterId)
    {
        return CanLocalPlayerControlCharacter(characterId);
    }

    public bool RequestEquipRelic(string characterId, int relicSlotIndex, string relicId)
    {
        BattleNetworkCommand command = CreateCommand(BattleNetworkCommandType.EquipRelic);
        command.characterId = characterId ?? string.Empty;
        command.slotIndex = relicSlotIndex;
        command.itemId = relicId ?? string.Empty;
        return SendOrApplyCommand(command, false);
    }

    public bool RequestUnequipRelic(string characterId, int relicSlotIndex)
    {
        BattleNetworkCommand command = CreateCommand(BattleNetworkCommandType.UnequipRelic);
        command.characterId = characterId ?? string.Empty;
        command.slotIndex = relicSlotIndex;
        return SendOrApplyCommand(command, false);
    }

    public bool RequestEquipSkill(string characterId, int equippedSkillIndex, string skillId)
    {
        BattleNetworkCommand command = CreateCommand(BattleNetworkCommandType.EquipSkill);
        command.characterId = characterId ?? string.Empty;
        command.slotIndex = equippedSkillIndex;
        command.itemId = skillId ?? string.Empty;
        return SendOrApplyCommand(command, false);
    }

    public bool RequestUnequipSkill(string characterId, int equippedSkillIndex)
    {
        BattleNetworkCommand command = CreateCommand(BattleNetworkCommandType.UnequipSkill);
        command.characterId = characterId ?? string.Empty;
        command.slotIndex = equippedSkillIndex;
        return SendOrApplyCommand(command, false);
    }

    private void Bind(BattleTurnExecutor executor, BattleTimelineController timeline)
    {
        turnExecutor = executor != null
            ? executor
            : UnityEngine.Object.FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
        timelineController = timeline != null
            ? timeline
            : UnityEngine.Object.FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
        readyPanel = BattleTurnReadyPanelUI.Ensure(turnExecutor);

#if STEAMWORKS_NET
        if (!SteamLobbySessionState.TryGetLobby(
                out ulong lobbyId,
                out ulong sessionLocalSteamId,
                out ulong hostSteamId))
        {
            return;
        }

        currentLobbyId = new CSteamID(lobbyId);
        localSteamId = sessionLocalSteamId;
        originalHostSteamId = hostSteamId;
        IsNetworkBattleActive = true;
        EnsureSteamCallbacks();
        EnsureMemberStateKeys();

        BattleTurnExecutor.BattleExecutionStarted -= OnBattleExecutionStarted;
        BattleTurnExecutor.BattleExecutionStarted += OnBattleExecutionStarted;
        BattleTurnExecutor.PlayerTurnReturned -= OnPlayerTurnReturned;
        BattleTurnExecutor.PlayerTurnReturned += OnPlayerTurnReturned;

        if (IsLocalHost())
            PublishSnapshotIfChanged(true);
        else
            ApplySnapshotFromLobbyData();
#endif
    }

    private void ToggleLocalReady()
    {
#if STEAMWORKS_NET
        bool current = readyByMember.TryGetValue(localSteamId, out bool ready) && ready;
        BattleNetworkCommand command = CreateCommand(BattleNetworkCommandType.SetReady);
        command.ready = !current;

        if (SendOrApplyCommand(command, true) && !IsLocalHost())
        {
            readyByMember[localSteamId] = command.ready;
            RefreshReadyPanel(CurrentSnapshot);
        }
#endif
    }

    private void RequestTimelineSlotSelection(int slotIndex)
    {
#if STEAMWORKS_NET
        if (!IsValidTimelineSlot(slotIndex))
            return;

        BattleNetworkCommand command = CreateCommand(BattleNetworkCommandType.SelectTimelineSlot);
        command.slotIndex = slotIndex;

        if (SendOrApplyCommand(command, true) && !IsLocalHost())
        {
            viewedSlotsByMember[localSteamId] = slotIndex;
            timelineController.SelectTimelineSlotFromNetwork(slotIndex, true);
            ApplyViewedSlotsToTimeline(CurrentSnapshot);
        }
#endif
    }

    private bool RequestPlayerCommandReservation(int slotIndex, PlayerReservedCommand command)
    {
#if STEAMWORKS_NET
        if (command == null || !CanLocalPlayerControlCharacter(command.CharacterId))
            return false;

        BattleNetworkCommand networkCommand =
            CreatePlayerCommand(BattleNetworkCommandType.ReservePlayerCommand, slotIndex, command);

        if (!SendOrApplyCommand(networkCommand, true))
            return false;

        if (!IsLocalHost())
            timelineController.ConfirmPlayerCommandFromNetwork(slotIndex, command);

        return true;
#else
        return false;
#endif
    }

    private bool RequestRemoveCommand(int slotIndex, int orderIndex)
    {
#if STEAMWORKS_NET
        if (!TryGetPlayerCommand(slotIndex, orderIndex, out PlayerReservedCommand command))
            return false;

        if (!CanLocalPlayerControlCharacter(command.CharacterId))
            return false;

        BattleNetworkCommand networkCommand = CreateCommand(BattleNetworkCommandType.RemovePlayerCommand);
        networkCommand.slotIndex = slotIndex;
        networkCommand.commandIndex = orderIndex;
        networkCommand.characterId = command.CharacterId;

        if (!SendOrApplyCommand(networkCommand, true))
            return false;

        if (!IsLocalHost())
            timelineController.RemoveCommandFromNetwork(slotIndex, orderIndex);

        return true;
#else
        return false;
#endif
    }

#if STEAMWORKS_NET
    private bool IsLocalHost()
    {
        return localSteamId != 0UL && localSteamId == originalHostSteamId;
    }

    private void EnsureSteamCallbacks()
    {
        if (lobbyChatMessage == null)
            lobbyChatMessage = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMessage);
    }

    private BattleNetworkCommand CreateCommand(BattleNetworkCommandType type)
    {
        return new BattleNetworkCommand
        {
            requestId = Guid.NewGuid().ToString("N"),
            requesterSteamId = BattleNetworkSerialization.ToText(localSteamId),
            commandType = (int)type,
            knownRevision = AppliedRevision
        };
    }

    private BattleNetworkCommand CreatePlayerCommand(
        BattleNetworkCommandType type,
        int slotIndex,
        PlayerReservedCommand command)
    {
        BattleNetworkCommand dto = CreateCommand(type);
        dto.slotIndex = slotIndex;
        dto.characterId = command.CharacterId;
        dto.skillId = command.SkillId;
        dto.direction = (int)command.Direction;
        dto.selectedGridIndex = command.SelectedGridIndex;
        dto.moveOffsetX = command.MoveOffset.x;
        dto.moveOffsetY = command.MoveOffset.y;
        dto.plannedMoveDistance = command.PlannedMoveDistance;
        dto.moveDistancePerCost = command.MoveDistancePerCost;
        dto.rangeGridIndices = ToArray(command.RangeGridIndices);
        dto.targetGridIndices = ToArray(command.TargetGridIndices);
        return dto;
    }

    private bool SendOrApplyCommand(BattleNetworkCommand command, bool publishOnAccept)
    {
        if (command == null)
            return false;

        if (IsLocalHost())
        {
            BattleNetworkCommandResult result = TryApplyHostCommand(command, publishOnAccept);
            return result.Accepted;
        }

        string payload = CommandPrefix + BattleNetworkSerialization.SerializeCommand(command);
        bool sent = TrySendLobbyChatPayload(payload);

        if (!sent)
            BattleWarningUI.ShowMessage("네트워크 명령 전송에 실패했습니다.");

        return sent;
    }

    private void OnLobbyChatMessage(LobbyChatMsg_t callback)
    {
        if (!IsNetworkBattleActive ||
            callback.m_ulSteamIDLobby != currentLobbyId.m_SteamID)
        {
            return;
        }

        byte[] buffer = new byte[MaxLobbyChatMessageBytes];
        int byteCount = SteamMatchmaking.GetLobbyChatEntry(
            currentLobbyId,
            checked((int)callback.m_iChatID),
            out CSteamID senderId,
            buffer,
            buffer.Length,
            out _);

        if (byteCount <= 0)
            return;

        string payload = Encoding.UTF8.GetString(buffer, 0, byteCount);

        if (payload.StartsWith(CommandPrefix, StringComparison.Ordinal))
        {
            if (IsLocalHost())
                HandleHostCommandPayload(payload, senderId);

            return;
        }

        if (payload.StartsWith(CommandResultPrefix, StringComparison.Ordinal) &&
            senderId.m_SteamID == originalHostSteamId)
        {
            HandleClientCommandResultPayload(payload);
            return;
        }

        if (payload.StartsWith(SnapshotBroadcastPrefix, StringComparison.Ordinal) &&
            senderId.m_SteamID == originalHostSteamId)
        {
            HandleClientSnapshotBroadcastPayload(payload);
        }
    }

    private void HandleHostCommandPayload(string payload, CSteamID senderId)
    {
        string commandJson = payload.Substring(CommandPrefix.Length);

        if (!BattleNetworkSerialization.TryDeserializeCommand(
                commandJson,
                out BattleNetworkCommand command) ||
            !BattleNetworkSerialization.TryParseSteamId(
                command.requesterSteamId,
                out ulong requesterSteamId) ||
            requesterSteamId != senderId.m_SteamID)
        {
            return;
        }

        BattleNetworkCommandResult result = TryApplyHostCommand(command, true);
        BattleNetworkCommandResponse response = new()
        {
            requestId = command.requestId,
            requesterSteamId = command.requesterSteamId,
            accepted = result.Accepted,
            rejectReason = (int)result.RejectReason,
            resultRevision = result.Snapshot != null ? result.Snapshot.revision : AppliedRevision,
            snapshot = result.Snapshot ?? CurrentSnapshot
        };

        string responsePayload =
            CommandResultPrefix + BattleNetworkSerialization.SerializeCommandResponse(response);
        TrySendLobbyChatPayload(responsePayload);
    }

    private BattleNetworkCommandResult TryApplyHostCommand(
        BattleNetworkCommand command,
        bool publishOnAccept)
    {
        if (command == null ||
            !BattleNetworkSerialization.TryParseSteamId(command.requesterSteamId, out ulong requester))
        {
            return BattleNetworkCommandResult.Reject(BattleNetworkRejectReason.UnknownMember);
        }

        BattleNetworkCommandType commandType = (BattleNetworkCommandType)command.commandType;
        BattleNetworkRejectReason rejectReason = commandType switch
        {
            BattleNetworkCommandType.SelectTimelineSlot => ApplyHostSlotSelection(requester, command),
            BattleNetworkCommandType.ClearTimelineSlotSelection => ApplyHostClearSlotSelection(requester),
            BattleNetworkCommandType.ReservePlayerCommand => ApplyHostPlayerReservation(requester, command),
            BattleNetworkCommandType.RemovePlayerCommand => ApplyHostRemovePlayerCommand(requester, command),
            BattleNetworkCommandType.SetReady => ApplyHostReady(requester, command),
            BattleNetworkCommandType.EquipRelic => ApplyHostEquipmentCommand(requester, command),
            BattleNetworkCommandType.UnequipRelic => ApplyHostEquipmentCommand(requester, command),
            BattleNetworkCommandType.EquipSkill => ApplyHostEquipmentCommand(requester, command),
            BattleNetworkCommandType.UnequipSkill => ApplyHostEquipmentCommand(requester, command),
            _ => BattleNetworkRejectReason.InvalidCommand
        };

        if (rejectReason != BattleNetworkRejectReason.None)
            return BattleNetworkCommandResult.Reject(rejectReason);

        BattleNetworkSnapshot snapshot = publishOnAccept
            ? PublishSnapshotIfChanged(true)
            : CurrentSnapshot;

        if (commandType == BattleNetworkCommandType.SetReady)
            TryStartHostExecutionIfAllReady();

        return BattleNetworkCommandResult.Accept(snapshot);
    }

    private BattleNetworkRejectReason ApplyHostSlotSelection(
        ulong requester,
        BattleNetworkCommand command)
    {
        if (!IsValidTimelineSlot(command.slotIndex))
            return BattleNetworkRejectReason.InvalidSlot;

        if (IsSlotViewedByOtherMember(command.slotIndex, requester))
            return BattleNetworkRejectReason.SlotViewedByOtherMember;

        viewedSlotsByMember[requester] = command.slotIndex;

        if (requester == localSteamId)
            timelineController.SelectTimelineSlotFromNetwork(command.slotIndex, true);

        return BattleNetworkRejectReason.None;
    }

    private BattleNetworkRejectReason ApplyHostClearSlotSelection(ulong requester)
    {
        viewedSlotsByMember.Remove(requester);
        return BattleNetworkRejectReason.None;
    }

    private BattleNetworkRejectReason ApplyHostPlayerReservation(
        ulong requester,
        BattleNetworkCommand command)
    {
        if (!IsValidTimelineSlot(command.slotIndex))
            return BattleNetworkRejectReason.InvalidSlot;

        if (!SteamLobbySessionState.IsMemberAllowedToControlCharacter(requester, command.characterId))
            return BattleNetworkRejectReason.NotCharacterOwner;

        if (IsSlotViewedByOtherMember(command.slotIndex, requester))
            return BattleNetworkRejectReason.SlotViewedByOtherMember;

        PlayerReservedCommand playerCommand = RebuildPlayerCommand(command);
        if (playerCommand == null)
            return BattleNetworkRejectReason.InvalidCommand;

        bool accepted =
            timelineController != null &&
            timelineController.ConfirmPlayerCommandFromNetwork(command.slotIndex, playerCommand);

        return accepted
            ? BattleNetworkRejectReason.None
            : BattleNetworkRejectReason.RejectedByService;
    }

    private BattleNetworkRejectReason ApplyHostRemovePlayerCommand(
        ulong requester,
        BattleNetworkCommand command)
    {
        if (!TryGetPlayerCommand(
                command.slotIndex,
                command.commandIndex,
                out PlayerReservedCommand reservedCommand))
        {
            return BattleNetworkRejectReason.InvalidCommand;
        }

        if (!SteamLobbySessionState.IsMemberAllowedToControlCharacter(
                requester,
                reservedCommand.CharacterId))
        {
            return BattleNetworkRejectReason.NotCharacterOwner;
        }

        bool removed =
            timelineController != null &&
            timelineController.RemoveCommandFromNetwork(command.slotIndex, command.commandIndex);

        return removed
            ? BattleNetworkRejectReason.None
            : BattleNetworkRejectReason.RejectedByService;
    }

    private BattleNetworkRejectReason ApplyHostReady(
        ulong requester,
        BattleNetworkCommand command)
    {
        readyByMember[requester] = command.ready;
        return BattleNetworkRejectReason.None;
    }

    private BattleNetworkRejectReason ApplyHostEquipmentCommand(
        ulong requester,
        BattleNetworkCommand command)
    {
        if (!SteamLobbySessionState.IsMemberAllowedToControlCharacter(requester, command.characterId))
            return BattleNetworkRejectReason.NotCharacterOwner;

        if (!ApplyEquipmentCommand(command))
            return BattleNetworkRejectReason.RejectedByService;

        RefreshRuntimeViews();
        return BattleNetworkRejectReason.None;
    }

    private bool ApplyEquipmentCommand(BattleNetworkCommand command)
    {
        if (DataManager.Instance == null)
            return false;

        BattleRuntimeData battleRuntime = DataManager.Instance.BattleRuntimeStore?.GetOrCreate();
        if (battleRuntime == null)
            return false;

        BattleNetworkCommandType type = (BattleNetworkCommandType)command.commandType;

        return type switch
        {
            BattleNetworkCommandType.EquipRelic => new RelicEquipService(
                DataManager.Instance.CharacterRuntimeStore,
                battleRuntime.OwnedRelicIds,
                DataManager.Instance.RelicDatabase).EquipRelic(
                    command.characterId,
                    command.slotIndex,
                    command.itemId),
            BattleNetworkCommandType.UnequipRelic => new RelicEquipService(
                DataManager.Instance.CharacterRuntimeStore,
                battleRuntime.OwnedRelicIds,
                DataManager.Instance.RelicDatabase).UnequipRelic(
                    command.characterId,
                    command.slotIndex),
            BattleNetworkCommandType.EquipSkill => new SkillInventoryEquipService(
                DataManager.Instance.CharacterRuntimeStore,
                battleRuntime.SkillInventoryIds,
                ResolveSkill).EquipInventorySkillToSlot(
                    command.characterId,
                    command.slotIndex,
                    command.itemId),
            BattleNetworkCommandType.UnequipSkill => new SkillInventoryEquipService(
                DataManager.Instance.CharacterRuntimeStore,
                battleRuntime.SkillInventoryIds,
                ResolveSkill).UnequipSkillFromSlot(
                    command.characterId,
                    command.slotIndex),
            _ => false
        };
    }

    private SkillMasterData ResolveSkill(string skillId)
    {
        return string.IsNullOrWhiteSpace(skillId) ||
               DataManager.Instance == null ||
               DataManager.Instance.SkillDatabase == null
            ? null
            : DataManager.Instance.SkillDatabase.Get(skillId);
    }

    private void HandleClientCommandResultPayload(string payload)
    {
        if (IsLocalHost())
            return;

        string responseJson = payload.Substring(CommandResultPrefix.Length);

        if (!BattleNetworkSerialization.TryDeserializeCommandResponse(
                responseJson,
                out BattleNetworkCommandResponse response))
        {
            return;
        }

        if (!BattleNetworkSerialization.TryParseSteamId(response.requesterSteamId, out ulong requester) ||
            requester != localSteamId)
        {
            return;
        }

        if (response.snapshot != null)
            ApplySnapshot(response.snapshot, true);
    }

    private void HandleClientSnapshotBroadcastPayload(string payload)
    {
        if (IsLocalHost())
            return;

        string snapshotJson = payload.Substring(SnapshotBroadcastPrefix.Length);

        if (BattleNetworkSerialization.TryDeserializeSnapshot(
                snapshotJson,
                out BattleNetworkSnapshot snapshot))
        {
            ApplySnapshot(snapshot, true);
        }
    }

    private BattleNetworkSnapshot PublishSnapshotIfChanged(bool force = false)
    {
        if (!IsLocalHost())
            return CurrentSnapshot;

        EnsureMemberStateKeys();

        BattleNetworkSnapshot candidate = CreateSnapshot(Math.Max(hostRevision, AppliedRevision) + 1);
        string payload = BattleNetworkSerialization.SerializeSnapshot(candidate);

        if (!force && payload == lastPublishedPayload)
            return CurrentSnapshot;

        hostRevision = candidate.revision;
        lastPublishedPayload = payload;

        if (!SteamMatchmaking.SetLobbyData(currentLobbyId, SnapshotLobbyDataKey, payload))
        {
            Debug.LogWarning("[SteamBattleStateSynchronizer] Failed to publish battle snapshot.", this);
            return CurrentSnapshot;
        }

        ApplySnapshot(candidate, true);
        BroadcastSnapshot(candidate, payload);
        return candidate;
    }

    private void BroadcastSnapshot(BattleNetworkSnapshot snapshot, string serializedPayload)
    {
        if (snapshot == null || !IsLocalHost())
            return;

        string payload = SnapshotBroadcastPrefix +
                         (serializedPayload ?? BattleNetworkSerialization.SerializeSnapshot(snapshot));
        TrySendLobbyChatPayload(payload);
    }

    private void ApplySnapshotFromLobbyData()
    {
        string payload = SteamMatchmaking.GetLobbyData(currentLobbyId, SnapshotLobbyDataKey);

        if (BattleNetworkSerialization.TryDeserializeSnapshot(
                payload,
                out BattleNetworkSnapshot snapshot))
        {
            ApplySnapshot(snapshot, false);
        }
    }

    private void ApplySnapshot(BattleNetworkSnapshot snapshot, bool allowEqualRevision)
    {
        if (snapshot == null ||
            !BattleNetworkSerialization.TryParseSteamId(snapshot.hostSteamId, out ulong hostId) ||
            hostId != originalHostSteamId)
        {
            return;
        }

        bool isNewer = snapshot.revision > AppliedRevision;
        bool isSameAllowed = allowEqualRevision && snapshot.revision == AppliedRevision;

        if (!isNewer && !isSameAllowed)
            return;

        CurrentSnapshot = snapshot;
        AppliedRevision = snapshot.revision;
        ImportReadyStates(snapshot);
        ImportViewedSlots(snapshot);

        if (!IsLocalHost())
            ApplyRuntimeSnapshot(snapshot);

        ApplyTimelineSnapshot(snapshot);
        RefreshReadyPanel(snapshot);

        if (turnExecutor != null)
            turnExecutor.SetNetworkExecutionLocked(snapshot.isExecuting && !IsLocalHost());
    }

    private bool TrySendLobbyChatPayload(string payload)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(payload ?? string.Empty);

        return bytes.Length <= MaxLobbyChatMessageBytes &&
               SteamMatchmaking.SendLobbyChatMsg(currentLobbyId, bytes, bytes.Length);
    }
#endif

    private void TryStartHostExecutionIfAllReady()
    {
#if STEAMWORKS_NET
        if (!IsLocalHost() || turnExecutor == null || !AreAllActiveMembersReady())
            return;

        StartCoroutine(StartHostExecutionNextFrame());
#endif
    }

    private IEnumerator StartHostExecutionNextFrame()
    {
        yield return null;

        if (turnExecutor != null)
            turnExecutor.ExecuteTurnFromNetworkHost();
    }

    private void OnBattleExecutionStarted()
    {
#if STEAMWORKS_NET
        if (IsNetworkBattleActive && IsLocalHost())
            PublishSnapshotIfChanged(true);
#endif
    }

    private void OnPlayerTurnReturned()
    {
#if STEAMWORKS_NET
        if (!IsNetworkBattleActive || !IsLocalHost())
            return;

        List<ulong> members = GetActiveMemberIds();
        for (int i = 0; i < members.Count; i++)
            readyByMember[members[i]] = false;

        PublishSnapshotIfChanged(true);
#endif
    }

    private bool IsValidTimelineSlot(int slotIndex)
    {
        return timelineController != null &&
               slotIndex >= 0 &&
               slotIndex < timelineController.SlotCount;
    }

    private bool TryGetPlayerCommand(
        int slotIndex,
        int commandIndex,
        out PlayerReservedCommand command)
    {
        command = null;

        if (timelineController == null)
            return false;

        IReadOnlyList<PlayerReservedCommand> commands =
            timelineController.GetPlayerCommands(slotIndex);

        if (commands == null || commandIndex < 0 || commandIndex >= commands.Count)
            return false;

        command = commands[commandIndex];
        return command != null;
    }

    private bool IsSlotViewedByOtherMember(int slotIndex, ulong requester)
    {
        foreach (KeyValuePair<ulong, int> pair in viewedSlotsByMember)
        {
            if (pair.Key != requester && pair.Value == slotIndex)
                return true;
        }

        return false;
    }

    private PlayerReservedCommand RebuildPlayerCommand(BattleNetworkCommand command)
    {
        if (DataManager.Instance == null ||
            string.IsNullOrWhiteSpace(command.characterId) ||
            string.IsNullOrWhiteSpace(command.skillId) ||
            !DataManager.Instance.CharacterRuntimeStore.TryGet(
                command.characterId,
                out CharacterRuntimeData runtime))
        {
            return null;
        }

        SkillMasterData skill = DataManager.Instance.SkillDatabase?.Get(command.skillId);
        if (skill == null)
            return null;

        PlayerReservedCommand rebuilt = new(runtime, skill);
        ApplyPlayerCommandFields(rebuilt, command);
        return rebuilt;
    }

    private PlayerReservedCommand RebuildPlayerCommand(BattleNetworkPlayerCommandSnapshot snapshot)
    {
        if (snapshot == null ||
            DataManager.Instance == null ||
            string.IsNullOrWhiteSpace(snapshot.characterId) ||
            string.IsNullOrWhiteSpace(snapshot.skillId) ||
            !DataManager.Instance.CharacterRuntimeStore.TryGet(
                snapshot.characterId,
                out CharacterRuntimeData runtime))
        {
            return null;
        }

        SkillMasterData skill = DataManager.Instance.SkillDatabase?.Get(snapshot.skillId);
        if (skill == null)
            return null;

        PlayerReservedCommand command = new(runtime, skill);
        command.SetMoveDirection((BattleDirection)snapshot.direction);

        if (snapshot.plannedMoveDistance > 0)
        {
            command.SetMoveReservationCost(
                snapshot.plannedMoveDistance,
                Mathf.Max(1, snapshot.moveDistancePerCost));
        }

        ApplyPlayerSelectionFields(
            command,
            (BattleDirection)snapshot.direction,
            snapshot.selectedGridIndex,
            snapshot.rangeGridIndices,
            snapshot.targetGridIndices,
            snapshot.moveOffsetX,
            snapshot.moveOffsetY);
        return command;
    }

    private void ApplyPlayerCommandFields(PlayerReservedCommand rebuilt, BattleNetworkCommand command)
    {
        BattleDirection direction = (BattleDirection)command.direction;
        rebuilt.SetMoveDirection(direction);

        if (command.plannedMoveDistance > 0)
        {
            rebuilt.SetMoveReservationCost(
                command.plannedMoveDistance,
                Mathf.Max(1, command.moveDistancePerCost));
        }

        ApplyPlayerSelectionFields(
            rebuilt,
            direction,
            command.selectedGridIndex,
            command.rangeGridIndices,
            command.targetGridIndices,
            command.moveOffsetX,
            command.moveOffsetY);
    }

    private static void ApplyPlayerSelectionFields(
        PlayerReservedCommand command,
        BattleDirection direction,
        int selectedGridIndex,
        int[] rangeGridIndices,
        int[] targetGridIndices,
        int moveOffsetX,
        int moveOffsetY)
    {
        List<int> range = ToList(rangeGridIndices);
        List<int> targets = ToList(targetGridIndices);
        Vector2Int moveOffset = new(moveOffsetX, moveOffsetY);

        if (selectedGridIndex >= 0 && moveOffset != Vector2Int.zero)
        {
            command.SetSelectionResult(direction, selectedGridIndex, range, moveOffset);
            return;
        }

        if (selectedGridIndex >= 0)
        {
            command.SetSelectionAreaResult(direction, selectedGridIndex, range);
            return;
        }

        if (range.Count > 0 || targets.Count > 0)
            command.SetDirectionResult(direction, range, targets);
    }

    private MonsterReservedCommand RebuildMonsterCommand(BattleNetworkMonsterCommandSnapshot snapshot)
    {
        if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.runtimeId))
            return null;

        MonsterUnit monster = FindMonster(snapshot.runtimeId);
        if (monster == null || monster.RuntimeData == null)
            return null;

        MonsterSkillData skill = DataManager.Instance?.MonsterSkillDatabase?.Get(snapshot.skillId);
        if (skill == null)
            return null;

        MonsterReservedCommand command = new(monster.RuntimeData, skill);
        command.SetMoveOffset(new Vector2Int(snapshot.moveOffsetX, snapshot.moveOffsetY));
        command.SetActionIndex(snapshot.actionIndex);
        command.SetRangeOriginGridIndex(snapshot.rangeOriginGridIndex);
        command.SetPortalMove(snapshot.isPortalMove);

        if (snapshot.reservedDamage > 0)
            command.SetReservedDamage(snapshot.reservedDamage);

        if (snapshot.hasForcedDirection)
            command.SetForcedDirection((BattleDirection)snapshot.forcedDirection);
        else
            command.ClearForcedDirection();

        if ((snapshot.rangeGridIndices != null && snapshot.rangeGridIndices.Length > 0) ||
            (snapshot.targetGridIndices != null && snapshot.targetGridIndices.Length > 0))
        {
            command.SetExplicitRangeResult(
                ToList(snapshot.rangeGridIndices),
                ToList(snapshot.targetGridIndices));
        }

        return command;
    }

    private BattleNetworkSnapshot CreateSnapshot(long revision)
    {
        return new BattleNetworkSnapshot
        {
            hostSteamId = BattleNetworkSerialization.ToText(SteamLobbySessionState.HostSteamId),
            revision = revision,
            isExecuting = turnExecutor != null && turnExecutor.IsExecuting,
            map = Clone(DataManager.Instance?.MapRuntimeStore?.Get()),
            battle = Clone(DataManager.Instance?.BattleRuntimeStore?.GetOrCreate()),
            partySlots = CreatePartySlotSnapshots(),
            characters = CreateCharacterSnapshots(),
            monsters = CreateMonsterSnapshots(),
            timelineSlots = CreateTimelineSnapshots(),
            viewedSlots = CreateViewedSlotSnapshots(),
            readyStates = CreateReadyStateSnapshots()
        };
    }

    private BattleNetworkPartySlotSnapshot[] CreatePartySlotSnapshots()
    {
        PartyRuntimeStore partyStore = DataManager.Instance?.PartyRuntimeStore;
        if (partyStore == null)
            return Array.Empty<BattleNetworkPartySlotSnapshot>();

        BattleNetworkPartySlotSnapshot[] result =
            new BattleNetworkPartySlotSnapshot[partyStore.MaxPartyCountValue];

        for (int i = 0; i < result.Length; i++)
        {
            string characterId = partyStore.GetCharacterId(i);
            ulong ownerSteamId = SteamLobbySessionState.GetCharacterOwnerSteamId(characterId);
            if (ownerSteamId == 0UL)
                ownerSteamId = SteamLobbySessionState.HostSteamId;

            result[i] = new BattleNetworkPartySlotSnapshot
            {
                slotIndex = i,
                ownerSteamId = BattleNetworkSerialization.ToText(ownerSteamId),
                characterId = characterId ?? string.Empty,
                spawnGridIndex = partyStore.GetSpawnGridIndex(i),
                currentGridIndex = partyStore.GetCurrentGridIndex(i)
            };
        }

        return result;
    }

    private CharacterRuntimeData[] CreateCharacterSnapshots()
    {
        PartyRuntimeStore partyStore = DataManager.Instance?.PartyRuntimeStore;
        CharacterRuntimeStore characterStore = DataManager.Instance?.CharacterRuntimeStore;

        if (partyStore == null || characterStore == null)
            return Array.Empty<CharacterRuntimeData>();

        List<CharacterRuntimeData> result = new();

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (!string.IsNullOrWhiteSpace(characterId) &&
                characterStore.TryGet(characterId, out CharacterRuntimeData character))
            {
                result.Add(Clone(character));
            }
        }

        return result.ToArray();
    }

    private BattleNetworkMonsterSnapshot[] CreateMonsterSnapshots()
    {
        MonsterUnit[] monsters = UnityEngine.Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        List<BattleNetworkMonsterSnapshot> result = new();

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null)
                continue;

            result.Add(new BattleNetworkMonsterSnapshot
            {
                runtime = Clone(monster.RuntimeData),
                occupiedGridIndices = ToArray(monster.OccupiedGridIndices)
            });
        }

        return result.ToArray();
    }

    private BattleNetworkTimelineSlotSnapshot[] CreateTimelineSnapshots()
    {
        if (timelineController == null)
            return Array.Empty<BattleNetworkTimelineSlotSnapshot>();

        BattleNetworkTimelineSlotSnapshot[] result =
            new BattleNetworkTimelineSlotSnapshot[timelineController.SlotCount];

        for (int slotIndex = 0; slotIndex < result.Length; slotIndex++)
        {
            result[slotIndex] = new BattleNetworkTimelineSlotSnapshot
            {
                slotIndex = slotIndex,
                playerCommands = CreatePlayerCommandSnapshots(slotIndex),
                monsterCommands = CreateMonsterCommandSnapshots(slotIndex)
            };
        }

        return result;
    }

    private BattleNetworkPlayerCommandSnapshot[] CreatePlayerCommandSnapshots(int slotIndex)
    {
        IReadOnlyList<PlayerReservedCommand> commands =
            timelineController.GetPlayerCommands(slotIndex);

        if (commands == null)
            return Array.Empty<BattleNetworkPlayerCommandSnapshot>();

        List<BattleNetworkPlayerCommandSnapshot> result = new();

        for (int i = 0; i < commands.Count; i++)
        {
            PlayerReservedCommand command = commands[i];
            if (command == null)
                continue;

            result.Add(new BattleNetworkPlayerCommandSnapshot
            {
                characterId = command.CharacterId,
                skillId = command.SkillId,
                direction = (int)command.Direction,
                selectedGridIndex = command.SelectedGridIndex,
                moveOffsetX = command.MoveOffset.x,
                moveOffsetY = command.MoveOffset.y,
                plannedMoveDistance = command.PlannedMoveDistance,
                moveDistancePerCost = command.MoveDistancePerCost,
                rangeGridIndices = ToArray(command.RangeGridIndices),
                targetGridIndices = ToArray(command.TargetGridIndices)
            });
        }

        return result.ToArray();
    }

    private BattleNetworkMonsterCommandSnapshot[] CreateMonsterCommandSnapshots(int slotIndex)
    {
        IReadOnlyList<MonsterReservedCommand> commands =
            timelineController.GetMonsterCommands(slotIndex);

        if (commands == null)
            return Array.Empty<BattleNetworkMonsterCommandSnapshot>();

        List<BattleNetworkMonsterCommandSnapshot> result = new();

        for (int i = 0; i < commands.Count; i++)
        {
            MonsterReservedCommand command = commands[i];
            if (command == null)
                continue;

            result.Add(new BattleNetworkMonsterCommandSnapshot
            {
                runtimeId = command.RuntimeId,
                skillId = command.SkillId,
                moveOffsetX = command.MoveOffset.x,
                moveOffsetY = command.MoveOffset.y,
                reservedDamage = command.ReservedDamage,
                actionIndex = command.ActionIndex,
                rangeOriginGridIndex = command.RangeOriginGridIndex,
                hasForcedDirection = command.HasForcedDirection,
                forcedDirection = (int)command.ForcedDirection,
                isPortalMove = command.IsPortalMove,
                rangeGridIndices = ToArray(command.RangeGridIndices),
                targetGridIndices = ToArray(command.TargetGridIndices)
            });
        }

        return result.ToArray();
    }

    private BattleNetworkMemberTimelineSelection[] CreateViewedSlotSnapshots()
    {
        List<BattleNetworkMemberTimelineSelection> result = new();

        foreach (KeyValuePair<ulong, int> pair in viewedSlotsByMember)
        {
            result.Add(new BattleNetworkMemberTimelineSelection
            {
                memberSteamId = BattleNetworkSerialization.ToText(pair.Key),
                slotIndex = pair.Value
            });
        }

        return result.ToArray();
    }

    private BattleNetworkMemberReadyState[] CreateReadyStateSnapshots()
    {
        EnsureMemberStateKeys();
        List<BattleNetworkMemberReadyState> result = new();

        foreach (KeyValuePair<ulong, bool> pair in readyByMember)
        {
            result.Add(new BattleNetworkMemberReadyState
            {
                memberSteamId = BattleNetworkSerialization.ToText(pair.Key),
                ready = pair.Value
            });
        }

        return result.ToArray();
    }

    private void ApplyRuntimeSnapshot(BattleNetworkSnapshot snapshot)
    {
        if (snapshot == null || DataManager.Instance == null)
            return;

        if (snapshot.map != null)
        {
            DataManager.Instance.MapRuntimeStore.Set(Clone(snapshot.map));
            ApplyBattleSceneMapRuntime(snapshot.map);
        }

        if (snapshot.battle != null)
            DataManager.Instance.BattleRuntimeStore.Set(Clone(snapshot.battle));

        ApplyPartySlots(snapshot.partySlots);
        ApplyCharacters(snapshot.characters);
        ApplyMonsters(snapshot.monsters);
        RefreshRuntimeViews();
    }

    private void ApplyPartySlots(BattleNetworkPartySlotSnapshot[] slots)
    {
        PartyRuntimeStore partyStore = DataManager.Instance?.PartyRuntimeStore;
        if (partyStore == null || slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            BattleNetworkPartySlotSnapshot slot = slots[i];
            if (slot == null || slot.slotIndex < 0 || slot.slotIndex >= partyStore.MaxPartyCountValue)
                continue;

            if (!string.IsNullOrWhiteSpace(slot.characterId))
            {
                partyStore.SetCharacter(slot.slotIndex, slot.characterId);

                if (slot.spawnGridIndex >= 0)
                    partyStore.SetSpawnGridIndex(slot.slotIndex, slot.spawnGridIndex);

                if (slot.currentGridIndex >= 0)
                    partyStore.SetCurrentGridIndex(slot.slotIndex, slot.currentGridIndex);
            }
        }
    }

    private void ApplyCharacters(CharacterRuntimeData[] characters)
    {
        CharacterRuntimeStore characterStore = DataManager.Instance?.CharacterRuntimeStore;
        if (characterStore == null || characters == null)
            return;

        for (int i = 0; i < characters.Length; i++)
        {
            CharacterRuntimeData source = characters[i];

            if (source == null || string.IsNullOrWhiteSpace(source.CharacterId))
                continue;

            if (characterStore.TryGet(source.CharacterId, out CharacterRuntimeData target) &&
                target != null)
            {
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), target);
            }
            else
            {
                characterStore.AddOrUpdate(Clone(source));
            }
        }
    }

    private void ApplyMonsters(BattleNetworkMonsterSnapshot[] monsters)
    {
        if (monsters == null)
            return;

        for (int i = 0; i < monsters.Length; i++)
        {
            BattleNetworkMonsterSnapshot snapshot = monsters[i];
            if (snapshot?.runtime == null || string.IsNullOrWhiteSpace(snapshot.runtime.RuntimeId))
                continue;

            MonsterUnit monster = FindMonster(snapshot.runtime.RuntimeId);
            if (monster == null)
                continue;

            if (monster.RuntimeData != null)
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(snapshot.runtime), monster.RuntimeData);
            else
                monster.Initialize(Clone(snapshot.runtime));

            monster.SetOccupiedCells(ToList(snapshot.occupiedGridIndices));
            monster.RefreshRuntimeDisplayName();
            monster.RefreshHUD();
        }
    }

    private void ApplyTimelineSnapshot(BattleNetworkSnapshot snapshot)
    {
        if (timelineController == null || snapshot == null)
            return;

        applyingNetworkTimeline = true;

        try
        {
            if (!IsLocalHost())
            {
                timelineController.ClearAllReservations();

                if (snapshot.timelineSlots != null)
                {
                    for (int i = 0; i < snapshot.timelineSlots.Length; i++)
                    {
                        BattleNetworkTimelineSlotSnapshot slot = snapshot.timelineSlots[i];
                        if (slot == null)
                            continue;

                        ApplyPlayerCommands(slot);
                        ApplyMonsterCommands(slot);
                    }
                }
            }

            ApplyViewedSlotsToTimeline(snapshot);
        }
        finally
        {
            applyingNetworkTimeline = false;
        }
    }

    private void ApplyPlayerCommands(BattleNetworkTimelineSlotSnapshot slot)
    {
        if (slot.playerCommands == null)
            return;

        for (int i = 0; i < slot.playerCommands.Length; i++)
        {
            PlayerReservedCommand command = RebuildPlayerCommand(slot.playerCommands[i]);
            if (command != null)
                timelineController.ConfirmPlayerCommandFromNetwork(slot.slotIndex, command);
        }
    }

    private void ApplyMonsterCommands(BattleNetworkTimelineSlotSnapshot slot)
    {
        if (slot.monsterCommands == null)
            return;

        for (int i = 0; i < slot.monsterCommands.Length; i++)
        {
            MonsterReservedCommand command = RebuildMonsterCommand(slot.monsterCommands[i]);
            if (command != null)
                timelineController.AddMonsterCommand(slot.slotIndex, command);
        }
    }

    private void ApplyViewedSlotsToTimeline(BattleNetworkSnapshot snapshot)
    {
        if (timelineController == null)
            return;

        List<int> remoteViewedSlots = new();

        foreach (KeyValuePair<ulong, int> pair in viewedSlotsByMember)
        {
#if STEAMWORKS_NET
            if (pair.Key == localSteamId)
                continue;
#endif

            if (pair.Value >= 0)
                remoteViewedSlots.Add(pair.Value);
        }

        timelineController.SetNetworkViewedSlots(remoteViewedSlots);
    }

    private void ImportReadyStates(BattleNetworkSnapshot snapshot)
    {
        readyByMember.Clear();

        if (snapshot?.readyStates == null)
            return;

        for (int i = 0; i < snapshot.readyStates.Length; i++)
        {
            BattleNetworkMemberReadyState state = snapshot.readyStates[i];

            if (state != null &&
                BattleNetworkSerialization.TryParseSteamId(state.memberSteamId, out ulong member))
            {
                readyByMember[member] = state.ready;
            }
        }
    }

    private void ImportViewedSlots(BattleNetworkSnapshot snapshot)
    {
        viewedSlotsByMember.Clear();

        if (snapshot?.viewedSlots == null)
            return;

        for (int i = 0; i < snapshot.viewedSlots.Length; i++)
        {
            BattleNetworkMemberTimelineSelection selection = snapshot.viewedSlots[i];

            if (selection != null &&
                BattleNetworkSerialization.TryParseSteamId(selection.memberSteamId, out ulong member) &&
                selection.slotIndex >= 0)
            {
                viewedSlotsByMember[member] = selection.slotIndex;
            }
        }
    }

    private void RefreshReadyPanel(BattleNetworkSnapshot snapshot)
    {
#if STEAMWORKS_NET
        if (readyPanel == null)
            readyPanel = BattleTurnReadyPanelUI.Ensure(turnExecutor);

        if (readyPanel != null)
            readyPanel.Refresh(snapshot ?? CurrentSnapshot, localSteamId);
#endif
    }

    private void RefreshRuntimeViews()
    {
        BattleBagPanelUI.RefreshAll();
        BattleGoldHudUI.RefreshAll();
        SkillInventoryPanelUI.RefreshAll();
        EquippedSkillPanelUI.RefreshAll();
        RelicEquipPanelUI.RefreshAll();

        PlayerHUDSlot[] playerHUDs = UnityEngine.Object.FindObjectsByType<PlayerHUDSlot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < playerHUDs.Length; i++)
        {
            if (playerHUDs[i] != null)
                playerHUDs[i].Refresh();
        }

        MonsterUnit[] monsters = UnityEngine.Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null)
                monsters[i].RefreshHUD();
        }
    }

    private static void ApplyBattleSceneMapRuntime(MapRuntimeData runtime)
    {
        if (runtime == null)
            return;

        BattleSceneController sceneController =
            UnityEngine.Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);

        if (sceneController != null)
            sceneController.ApplyNetworkMapRuntime(runtime);
    }

    private bool AreAllActiveMembersReady()
    {
        List<ulong> members = GetActiveMemberIds();

        if (members.Count <= 0)
            return false;

        for (int i = 0; i < members.Count; i++)
        {
            if (!readyByMember.TryGetValue(members[i], out bool ready) || !ready)
                return false;
        }

        return true;
    }

    private void EnsureMemberStateKeys()
    {
        List<ulong> activeMembers = GetActiveMemberIds();
        HashSet<ulong> activeSet = new(activeMembers);

        for (int i = 0; i < activeMembers.Count; i++)
        {
            if (!readyByMember.ContainsKey(activeMembers[i]))
                readyByMember[activeMembers[i]] = false;
        }

        RemoveInactiveKeys(readyByMember, activeSet);
        RemoveInactiveKeys(viewedSlotsByMember, activeSet);
    }

    private static void RemoveInactiveKeys<T>(
        Dictionary<ulong, T> dictionary,
        HashSet<ulong> activeMembers)
    {
        List<ulong> remove = new();

        foreach (KeyValuePair<ulong, T> pair in dictionary)
        {
            if (!activeMembers.Contains(pair.Key))
                remove.Add(pair.Key);
        }

        for (int i = 0; i < remove.Count; i++)
            dictionary.Remove(remove[i]);
    }

    private List<ulong> GetActiveMemberIds()
    {
        List<ulong> result = new();
        LobbyPartySnapshot party = SteamLobbySessionState.PartySnapshot;

        if (party?.Slots != null)
        {
            for (int i = 0; i < party.Slots.Count; i++)
            {
                LobbyPartySlotState slot = party.Slots[i];

                if (slot == null ||
                    slot.OwnerSteamId == 0UL ||
                    string.IsNullOrWhiteSpace(slot.CharacterId) ||
                    result.Contains(slot.OwnerSteamId))
                {
                    continue;
                }

                result.Add(slot.OwnerSteamId);
            }
        }

        if (result.Count <= 0 && SteamLobbySessionState.HostSteamId != 0UL)
            result.Add(SteamLobbySessionState.HostSteamId);

        return result;
    }

    private MonsterUnit FindMonster(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
            return null;

        MonsterUnit[] monsters = UnityEngine.Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i]?.RuntimeData != null &&
                monsters[i].RuntimeData.RuntimeId == runtimeId)
            {
                return monsters[i];
            }
        }

        return null;
    }

    private static T Clone<T>(T source)
    {
        if (source == null)
            return default;

        return JsonUtility.FromJson<T>(JsonUtility.ToJson(source));
    }

    private static int[] ToArray(IReadOnlyList<int> source)
    {
        if (source == null)
            return Array.Empty<int>();

        int[] result = new int[source.Count];

        for (int i = 0; i < source.Count; i++)
            result[i] = source[i];

        return result;
    }

    private static List<int> ToList(int[] source)
    {
        List<int> result = new();

        if (source == null)
            return result;

        for (int i = 0; i < source.Length; i++)
            result.Add(source[i]);

        return result;
    }

    private readonly struct BattleNetworkCommandResult
    {
        public bool Accepted { get; }
        public BattleNetworkRejectReason RejectReason { get; }
        public BattleNetworkSnapshot Snapshot { get; }

        private BattleNetworkCommandResult(
            bool accepted,
            BattleNetworkRejectReason rejectReason,
            BattleNetworkSnapshot snapshot)
        {
            Accepted = accepted;
            RejectReason = rejectReason;
            Snapshot = snapshot;
        }

        public static BattleNetworkCommandResult Accept(BattleNetworkSnapshot snapshot)
        {
            return new BattleNetworkCommandResult(
                true,
                BattleNetworkRejectReason.None,
                snapshot);
        }

        public static BattleNetworkCommandResult Reject(BattleNetworkRejectReason reason)
        {
            return new BattleNetworkCommandResult(false, reason, null);
        }
    }
}
