using System;
using System.Collections.Generic;
using System.Text;
using Relic.Gameplay.Data;
using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

[DisallowMultipleComponent]
public sealed class SteamLobbySharedStateSynchronizer : MonoBehaviour
{
    private const string SharedSnapshotLobbyDataKey = "relic.lobby.shared.snapshot.v1";
    private const string SharedSnapshotBroadcastPrefix = "RELIC_LOBBY_SHARED_SNAPSHOT_V1:";
    private const string SharedCommandPrefix = "RELIC_LOBBY_SHARED_CMD_V1:";
    private const string SharedCommandResultPrefix = "RELIC_LOBBY_SHARED_RESULT_V1:";
    private const int MaxLobbyChatMessageBytes = 4096;

    public static SteamLobbySharedStateSynchronizer Instance { get; private set; }

    public bool IsNetworkSharedStateActive { get; private set; }
    public long AppliedRevision { get; private set; }
    public LobbySharedStateSnapshot CurrentSnapshot { get; private set; }

    public event Action SharedStateApplied;

    private SteamLobbyPartySynchronizer subscribedPartySynchronizer;

#if STEAMWORKS_NET
    private CSteamID currentLobbyId;
    private ulong localSteamId;
    private ulong originalHostSteamId;
    private long hostRevision;
    private Callback<LobbyChatMsg_t> lobbyChatMessage;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        UnsubscribePartyStateApplied();

#if STEAMWORKS_NET
        lobbyChatMessage?.Dispose();
#endif

        if (Instance == this)
            Instance = null;
    }

    public void EnterLobby(ulong lobbyId, ulong localId, ulong ownerSteamId)
    {
#if STEAMWORKS_NET
        if (lobbyId == 0UL || localId == 0UL || ownerSteamId == 0UL)
            return;

        EnsureSteamCallbacks();
        currentLobbyId = new CSteamID(lobbyId);
        localSteamId = localId;
        originalHostSteamId = ownerSteamId;
        hostRevision = 0;
        AppliedRevision = 0;
        CurrentSnapshot = null;
        IsNetworkSharedStateActive = true;
        SubscribePartyStateApplied();

        if (IsLocalHost())
            PublishSnapshot();
        else
            ApplySnapshotFromLobbyData();
#endif
    }

    public void HandleLobbyMembershipChanged()
    {
#if STEAMWORKS_NET
        if (!IsNetworkSharedStateActive || !currentLobbyId.IsValid())
            return;

        ulong currentOwnerId = SteamMatchmaking.GetLobbyOwner(currentLobbyId).m_SteamID;

        if (currentOwnerId != originalHostSteamId)
        {
            LeaveLobby();
            return;
        }

        if (IsLocalHost())
            PublishSnapshot();
        else
            ApplySnapshotFromLobbyData();
#endif
    }

    public void HandleLobbyDataChanged()
    {
#if STEAMWORKS_NET
        if (!IsNetworkSharedStateActive || IsLocalHost())
            return;

        ApplySnapshotFromLobbyData();
#endif
    }

    public void LeaveLobby()
    {
#if STEAMWORKS_NET
        currentLobbyId = default;
        localSteamId = 0UL;
        originalHostSteamId = 0UL;
        hostRevision = 0;
#endif

        IsNetworkSharedStateActive = false;
        AppliedRevision = 0;
        CurrentSnapshot = null;
        UnsubscribePartyStateApplied();
    }

    public bool CanLocalPlayerMutateHostOnlyState()
    {
        if (!IsNetworkSharedStateActive)
            return true;

#if STEAMWORKS_NET
        return IsLocalHost();
#else
        return false;
#endif
    }

    public bool CanLocalPlayerEditCharacter(string characterId)
    {
        if (!IsNetworkSharedStateActive)
            return true;

#if STEAMWORKS_NET
        return IsMemberAllowedToEditCharacter(localSteamId, characterId);
#else
        return false;
#endif
    }

    public void PublishHostSnapshotAfterLocalMutation()
    {
#if STEAMWORKS_NET
        if (IsNetworkSharedStateActive && IsLocalHost())
            PublishSnapshot();
#endif
    }

    public LobbySharedStateSnapshot PublishHostSnapshotNow()
    {
#if STEAMWORKS_NET
        if (IsNetworkSharedStateActive && IsLocalHost())
        {
            long previousRevision = AppliedRevision;
            LobbySharedStateSnapshot snapshot = PublishSnapshot();
            return snapshot != null && snapshot.Revision > previousRevision
                ? snapshot
                : null;
        }
#endif

        return CurrentSnapshot;
    }

    public bool RequestEquipRelic(
        string characterId,
        int relicSlotIndex,
        string relicId)
    {
        return RequestEquipmentCommand(
            LobbySharedStateCommandType.EquipRelic,
            characterId,
            relicSlotIndex,
            relicId);
    }

    public bool RequestUnequipRelic(string characterId, int relicSlotIndex)
    {
        return RequestEquipmentCommand(
            LobbySharedStateCommandType.UnequipRelic,
            characterId,
            relicSlotIndex,
            string.Empty);
    }

    public bool RequestEquipSkill(
        string characterId,
        int equippedSkillIndex,
        string skillId)
    {
        return RequestEquipmentCommand(
            LobbySharedStateCommandType.EquipSkill,
            characterId,
            equippedSkillIndex,
            skillId);
    }

    public bool RequestUnequipSkill(string characterId, int equippedSkillIndex)
    {
        return RequestEquipmentCommand(
            LobbySharedStateCommandType.UnequipSkill,
            characterId,
            equippedSkillIndex,
            string.Empty);
    }

    private void SubscribePartyStateApplied()
    {
        SteamLobbyPartySynchronizer partySynchronizer =
            SteamLobbyPartySynchronizer.Instance;

        if (subscribedPartySynchronizer == partySynchronizer)
            return;

        UnsubscribePartyStateApplied();

        subscribedPartySynchronizer = partySynchronizer;
        if (subscribedPartySynchronizer != null)
            subscribedPartySynchronizer.PartyStateApplied += OnPartyStateApplied;
    }

    private void UnsubscribePartyStateApplied()
    {
        if (subscribedPartySynchronizer != null)
            subscribedPartySynchronizer.PartyStateApplied -= OnPartyStateApplied;

        subscribedPartySynchronizer = null;
    }

    private void OnPartyStateApplied()
    {
        if (!IsNetworkSharedStateActive)
            return;

        if (CanLocalPlayerMutateHostOnlyState())
            PublishHostSnapshotAfterLocalMutation();
        else
            RefreshSharedStateViews();
    }

    private bool RequestEquipmentCommand(
        LobbySharedStateCommandType commandType,
        string characterId,
        int slotIndex,
        string itemId)
    {
        if (!IsNetworkSharedStateActive ||
            string.IsNullOrWhiteSpace(characterId) ||
            !CanLocalPlayerEditCharacter(characterId))
        {
            return false;
        }

#if STEAMWORKS_NET
        LobbySharedStateCommand command = CreateLocalCommand(
            commandType,
            characterId,
            slotIndex,
            itemId);

        if (IsLocalHost())
            return TryApplyHostCommand(command).Accepted;

        string payload =
            SharedCommandPrefix +
            LobbySharedStateSerialization.SerializeCommand(command);
        return TrySendLobbyChatPayload(payload);
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

    private LobbySharedStateCommand CreateLocalCommand(
        LobbySharedStateCommandType commandType,
        string characterId,
        int slotIndex,
        string itemId)
    {
        return new LobbySharedStateCommand(
            Guid.NewGuid().ToString("N"),
            localSteamId,
            commandType,
            characterId,
            slotIndex,
            itemId,
            AppliedRevision);
    }

    private void OnLobbyChatMessage(LobbyChatMsg_t callback)
    {
        if (!IsNetworkSharedStateActive ||
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

        if (payload.StartsWith(SharedCommandPrefix, StringComparison.Ordinal))
        {
            if (IsLocalHost())
                HandleHostCommandPayload(payload, senderId);

            return;
        }

        if (payload.StartsWith(SharedCommandResultPrefix, StringComparison.Ordinal) &&
            senderId.m_SteamID == originalHostSteamId)
        {
            HandleClientCommandResultPayload(payload);
            return;
        }

        if (payload.StartsWith(SharedSnapshotBroadcastPrefix, StringComparison.Ordinal) &&
            senderId.m_SteamID == originalHostSteamId)
        {
            HandleClientSnapshotBroadcastPayload(payload);
        }
    }

    private void HandleHostCommandPayload(string payload, CSteamID senderId)
    {
        string commandJson = payload.Substring(SharedCommandPrefix.Length);

        if (!LobbySharedStateSerialization.TryDeserializeCommand(
                commandJson,
                out LobbySharedStateCommand command) ||
            command.RequesterSteamId != senderId.m_SteamID)
        {
            return;
        }

        LobbySharedStateCommandResult result = TryApplyHostCommand(command);
        LobbySharedStateCommandResponse response = new(
            command.RequestId,
            command.RequesterSteamId,
            result.Accepted,
            result.RejectReason,
            result.Snapshot != null ? result.Snapshot.Revision : AppliedRevision,
            result.Snapshot ?? CurrentSnapshot);
        string responsePayload =
            SharedCommandResultPrefix +
            LobbySharedStateSerialization.SerializeCommandResponse(response);
        TrySendLobbyChatPayload(responsePayload);
    }

    private LobbySharedStateCommandResult TryApplyHostCommand(
        LobbySharedStateCommand command)
    {
        if (command == null || command.RequesterSteamId == 0UL)
        {
            return LobbySharedStateCommandResult.Reject(
                LobbySharedStateCommandRejectReason.UnknownMember);
        }

        if (!IsMemberAllowedToEditCharacter(
                command.RequesterSteamId,
                command.CharacterId))
        {
            return LobbySharedStateCommandResult.Reject(
                LobbySharedStateCommandRejectReason.NotCharacterOwner);
        }

        if (!ApplyEquipmentCommand(command))
        {
            return LobbySharedStateCommandResult.Reject(
                LobbySharedStateCommandRejectReason.RejectedByService);
        }

        LobbySharedStateSnapshot snapshot = PublishSnapshot();
        return LobbySharedStateCommandResult.Accept(snapshot);
    }

    private void HandleClientCommandResultPayload(string payload)
    {
        if (IsLocalHost())
            return;

        string responseJson = payload.Substring(SharedCommandResultPrefix.Length);

        if (!LobbySharedStateSerialization.TryDeserializeCommandResponse(
                responseJson,
                out LobbySharedStateCommandResponse response))
        {
            return;
        }

        if (response.RequesterSteamId != localSteamId)
            return;

        if (response.Snapshot != null)
            ApplySnapshot(response.Snapshot, true);
    }

    private void HandleClientSnapshotBroadcastPayload(string payload)
    {
        if (IsLocalHost())
            return;

        string snapshotJson = payload.Substring(SharedSnapshotBroadcastPrefix.Length);

        if (!LobbySharedStateSerialization.TryDeserializeSnapshot(
                snapshotJson,
                out LobbySharedStateSnapshot snapshot))
        {
            return;
        }

        ApplySnapshot(snapshot, true);
    }

    private LobbySharedStateSnapshot PublishSnapshot()
    {
        if (!IsLocalHost())
            return CurrentSnapshot;

        hostRevision = Math.Max(hostRevision, AppliedRevision) + 1;
        LobbySharedStateSnapshot snapshot = CreateSnapshot(hostRevision);
        string payload = LobbySharedStateSerialization.SerializeSnapshot(snapshot);

        if (!SteamMatchmaking.SetLobbyData(
                currentLobbyId,
                SharedSnapshotLobbyDataKey,
                payload))
        {
            Debug.LogWarning(
                "[SteamLobbySharedStateSynchronizer] Failed to publish lobby shared snapshot.",
                this);
            return CurrentSnapshot;
        }

        ApplySnapshot(snapshot, true);
        BroadcastSnapshot(snapshot);
        return snapshot;
    }

    private void BroadcastSnapshot(LobbySharedStateSnapshot snapshot)
    {
        if (snapshot == null || !IsLocalHost())
            return;

        string payload =
            SharedSnapshotBroadcastPrefix +
            LobbySharedStateSerialization.SerializeSnapshot(snapshot);
        TrySendLobbyChatPayload(payload);
    }

    private bool TrySendLobbyChatPayload(string payload)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(payload ?? string.Empty);

        return bytes.Length <= MaxLobbyChatMessageBytes &&
               SteamMatchmaking.SendLobbyChatMsg(
                   currentLobbyId,
                   bytes,
                   bytes.Length);
    }

    private void ApplySnapshotFromLobbyData()
    {
        string payload = SteamMatchmaking.GetLobbyData(
            currentLobbyId,
            SharedSnapshotLobbyDataKey);

        if (!LobbySharedStateSerialization.TryDeserializeSnapshot(
                payload,
                out LobbySharedStateSnapshot snapshot))
        {
            return;
        }

        ApplySnapshot(snapshot, false);
    }

    private void ApplySnapshot(
        LobbySharedStateSnapshot snapshot,
        bool allowEqualRevision)
    {
        if (snapshot == null ||
            snapshot.HostSteamId != originalHostSteamId)
        {
            return;
        }

        bool isNewer = snapshot.Revision > AppliedRevision;
        bool isSameAllowed =
            allowEqualRevision && snapshot.Revision == AppliedRevision;

        if (!isNewer && !isSameAllowed)
            return;

        ApplySnapshotToRuntime(snapshot);
        AppliedRevision = snapshot.Revision;
        hostRevision = Math.Max(hostRevision, snapshot.Revision);
        CurrentSnapshot = snapshot;
        RefreshSharedStateViews();
        SharedStateApplied?.Invoke();
    }
#endif

    private static LobbySharedStateSnapshot CreateSnapshot(long revision)
    {
        LobbyRuntimeData lobby = DataManager.Instance != null
            ? DataManager.Instance.LobbyRuntimeStore?.GetOrCreate()
            : null;

        LobbyRuntimeData copy = LobbySharedStateRuntimeCopy.CopyLobbyRuntime(lobby);
        CaptureCharacterLoadouts(copy);

#if STEAMWORKS_NET
        ulong hostId = Instance != null ? Instance.originalHostSteamId : 0UL;
#else
        ulong hostId = 0UL;
#endif

        return LobbySharedStateSnapshot.FromRuntime(
            hostId,
            revision,
            TrialSelectionState.SelectedMask,
            copy);
    }

    private static void CaptureCharacterLoadouts(LobbyRuntimeData lobby)
    {
        if (lobby == null || DataManager.Instance == null)
            return;

        lobby.CharacterLoadouts ??= new List<LobbyCharacterLoadoutData>();
        lobby.CharacterLoadouts.Clear();

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        CharacterRuntimeStore characterStore = DataManager.Instance.CharacterRuntimeStore;
        if (partyStore == null || characterStore == null)
            return;

        LobbySharedStateCharacterRuntimeUtility.EnsurePartyCharacterRuntimes(
            partyStore,
            characterStore,
            DataManager.Instance.CharacterDatabase,
            DataManager.Instance.RelicDatabase);

        int maxCount = partyStore.MaxPartyCountValue;
        for (int i = 0; i < maxCount; i++)
        {
            string characterId = partyStore.GetCharacterId(i);
            if (string.IsNullOrWhiteSpace(characterId) ||
                !characterStore.TryGet(characterId, out CharacterRuntimeData character) ||
                character == null)
            {
                continue;
            }

            lobby.CharacterLoadouts.Add(new LobbyCharacterLoadoutData
            {
                CharacterId = character.CharacterId != null
                    ? character.CharacterId.Trim()
                    : characterId.Trim(),
                EquippedRelicIds = CopyArray(character.EquippedRelicIds, 7),
                EquippedSkillIds = CopyArray(character.EquippedSkillIds, 4)
            });
        }
    }

    private static void ApplySnapshotToRuntime(LobbySharedStateSnapshot snapshot)
    {
        if (snapshot == null || DataManager.Instance == null)
            return;

        LobbyRuntimeData lobbyCopy =
            LobbySharedStateRuntimeCopy.CopyLobbyRuntime(snapshot.Lobby);
        DataManager.Instance.LobbyRuntimeStore?.Set(lobbyCopy);
        TrialSelectionState.SetMask(snapshot.TrialSelectionMask);
        LobbySharedStateCharacterRuntimeUtility.ApplyLobbyLoadouts(
            lobbyCopy,
            DataManager.Instance.PartyRuntimeStore,
            DataManager.Instance.CharacterRuntimeStore,
            DataManager.Instance.CharacterDatabase,
            DataManager.Instance.RelicDatabase);
    }

    private static bool ApplyEquipmentCommand(LobbySharedStateCommand command)
    {
        if (command == null || DataManager.Instance == null)
            return false;

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore?.GetOrCreate();
        if (lobby == null)
            return false;

        return command.CommandType switch
        {
            LobbySharedStateCommandType.EquipRelic => new RelicEquipService(
                DataManager.Instance.CharacterRuntimeStore,
                lobby.OwnedRelicIds,
                DataManager.Instance.RelicDatabase).EquipRelic(
                    command.CharacterId,
                    command.SlotIndex,
                    command.ItemId),

            LobbySharedStateCommandType.UnequipRelic => new RelicEquipService(
                DataManager.Instance.CharacterRuntimeStore,
                lobby.OwnedRelicIds,
                DataManager.Instance.RelicDatabase).UnequipRelic(
                    command.CharacterId,
                    command.SlotIndex),

            LobbySharedStateCommandType.EquipSkill => new SkillInventoryEquipService(
                DataManager.Instance.CharacterRuntimeStore,
                lobby.SkillInventoryIds,
                ResolveSkill).EquipInventorySkillToSlot(
                    command.CharacterId,
                    command.SlotIndex,
                    command.ItemId),

            LobbySharedStateCommandType.UnequipSkill => new SkillInventoryEquipService(
                DataManager.Instance.CharacterRuntimeStore,
                lobby.SkillInventoryIds,
                ResolveSkill).UnequipSkillFromSlot(
                    command.CharacterId,
                    command.SlotIndex),

            _ => false
        };
    }

    private static bool IsMemberAllowedToEditCharacter(
        ulong memberSteamId,
        string characterId)
    {
        if (memberSteamId == 0UL || string.IsNullOrWhiteSpace(characterId))
            return false;

        SteamLobbyPartySynchronizer partySynchronizer =
            SteamLobbyPartySynchronizer.Instance;
        LobbyPartySnapshot partySnapshot = partySynchronizer != null
            ? partySynchronizer.CurrentSnapshot
            : null;

        if (partySnapshot == null)
            return false;

        string targetCharacterId = characterId.Trim();
        for (int i = 0; i < partySnapshot.Slots.Count; i++)
        {
            LobbyPartySlotState slot = partySnapshot.Slots[i];

            if (slot == null ||
                !string.Equals(
                    slot.CharacterId,
                    targetCharacterId,
                    StringComparison.Ordinal))
            {
                continue;
            }

            return slot.OwnerSteamId == memberSteamId;
        }

        return false;
    }

    private static SkillMasterData ResolveSkill(string skillId)
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.SkillDatabase == null ||
            string.IsNullOrWhiteSpace(skillId))
        {
            return null;
        }

        DataManager.Instance.SkillDatabase.TryGet(
            skillId.Trim(),
            out SkillMasterData skill);
        return skill;
    }

    private static string[] CopyArray(string[] source, int length)
    {
        string[] result = new string[length];

        if (source == null)
            return result;

        Array.Copy(source, result, Math.Min(source.Length, length));
        return result;
    }

    private static void RefreshSharedStateViews()
    {
        LobbyBlueDustiumHudUI.RefreshAll();
        BattleBagPanelUI.RefreshAll();
        RelicEquipPanelUI.RefreshAll();
        SkillInventoryPanelUI.RefreshAll();
        EquippedSkillPanelUI.RefreshAll();

        LobbyRelicShopPresenter[] shops = FindObjectsByType<LobbyRelicShopPresenter>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < shops.Length; i++)
            shops[i]?.RefreshOffers();

        LobbyCultureTankController[] tanks =
            FindObjectsByType<LobbyCultureTankController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        for (int i = 0; i < tanks.Length; i++)
            tanks[i]?.RefreshNow();
    }
}
