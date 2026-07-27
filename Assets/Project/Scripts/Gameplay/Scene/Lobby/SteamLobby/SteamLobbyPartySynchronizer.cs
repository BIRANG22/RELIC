using System;
using System.Collections.Generic;
using System.Text;
using Relic.Gameplay.Data;
using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

[DisallowMultipleComponent]
public sealed class SteamLobbyPartySynchronizer : MonoBehaviour
{
    private const string SnapshotLobbyDataKey = "relic.party.snapshot.v1";
    private const string CommandPrefix = "RELIC_PARTY_CMD_V1:";
    private const string CommandResultPrefix = "RELIC_PARTY_RESULT_V1:";
    private const int MaxLobbyChatMessageBytes = 4096;
    private const int FirstDefaultSpawnGridIndex = 6;

    public static SteamLobbyPartySynchronizer Instance { get; private set; }

    public bool IsNetworkPartyActive { get; private set; }
    public long AppliedRevision { get; private set; }
    public LobbyPartySnapshot CurrentSnapshot { get; private set; }

    public event Action PartyStateApplied;

#if STEAMWORKS_NET
    private CSteamID currentLobbyId;
    private ulong localSteamId;
    private ulong originalHostSteamId;
    private LobbyPartyAuthorityState authorityState;
    private LobbyPartySnapshot authoritativeSnapshot;
    private PendingLocalCommand pendingLocalCommand;
    private QueuedLocalIntent queuedLocalIntent;
    private Callback<LobbyChatMsg_t> lobbyChatMessage;

    private sealed class PendingLocalCommand
    {
        public LobbyPartyCharacterChangeCommand Command { get; }
        public long AcceptedRevision { get; set; }

        public PendingLocalCommand(LobbyPartyCharacterChangeCommand command)
        {
            Command = command;
        }
    }

    private sealed class QueuedLocalIntent
    {
        public int SlotIndex { get; }
        public string CharacterId { get; }

        public QueuedLocalIntent(int slotIndex, string characterId)
        {
            SlotIndex = slotIndex;
            CharacterId = characterId ?? string.Empty;
        }
    }
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
        authorityState = null;
        authoritativeSnapshot = null;
        pendingLocalCommand = null;
        queuedLocalIntent = null;
        AppliedRevision = 0;
        CurrentSnapshot = null;
        IsNetworkPartyActive = true;

        if (IsLocalHost())
        {
            authorityState = LobbyPartyAuthorityState.CreateHost(
                localSteamId,
                ReadLocalCharacters());
            ReconcileHostMembership();
            PublishSnapshot();
        }
        else
        {
            SteamMatchmaking.RequestLobbyData(currentLobbyId);
            ApplySnapshotFromLobbyData();
        }
#endif
    }

    public void HandleLobbyMembershipChanged()
    {
#if STEAMWORKS_NET
        if (!IsNetworkPartyActive || !currentLobbyId.IsValid())
            return;

        ulong currentOwnerId = SteamMatchmaking.GetLobbyOwner(currentLobbyId).m_SteamID;

        if (currentOwnerId != originalHostSteamId)
        {
            LeaveLobby(true);
            return;
        }

        if (IsLocalHost())
        {
            if (ReconcileHostMembership())
                PublishSnapshot();
        }
        else
        {
            SteamMatchmaking.RequestLobbyData(currentLobbyId);
            ApplySnapshotFromLobbyData();
        }
#endif
    }

    public void HandleLobbyDataChanged()
    {
#if STEAMWORKS_NET
        if (!IsNetworkPartyActive || IsLocalHost())
            return;

        ApplySnapshotFromLobbyData();
#endif
    }

    public bool CanLocalPlayerEditSlot(int slotIndex)
    {
        if (!IsNetworkPartyActive)
            return true;

        if (CurrentSnapshot == null ||
            slotIndex < 0 ||
            slotIndex >= CurrentSnapshot.Slots.Count)
        {
            return false;
        }

#if STEAMWORKS_NET
        return CurrentSnapshot.Slots[slotIndex].OwnerSteamId == localSteamId;
#else
        return false;
#endif
    }

    public int FindDisplayedCharacterSlot(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId) || CurrentSnapshot == null)
            return -1;

        for (int i = 0; i < CurrentSnapshot.Slots.Count; i++)
        {
            if (CurrentSnapshot.Slots[i].CharacterId == characterId)
                return i;
        }

        return -1;
    }

    public bool IsCharacterUsedByOtherSlot(string characterId, int slotIndex)
    {
        if (string.IsNullOrWhiteSpace(characterId) || CurrentSnapshot == null)
            return false;

        for (int i = 0; i < CurrentSnapshot.Slots.Count; i++)
        {
            if (i != slotIndex &&
                CurrentSnapshot.Slots[i].CharacterId == characterId)
            {
                return true;
            }
        }

        return false;
    }

    public bool CanLocalPlayerSelectCharacter(string characterId)
    {
        if (!IsNetworkPartyActive)
            return true;

#if STEAMWORKS_NET
        if (CurrentSnapshot == null || string.IsNullOrWhiteSpace(characterId))
            return false;

        for (int i = 0; i < CurrentSnapshot.Slots.Count; i++)
        {
            LobbyPartySlotState slot = CurrentSnapshot.Slots[i];

            if (slot.CharacterId == characterId)
                return slot.OwnerSteamId == localSteamId;
        }

        return FindOwnedEmptySlot() >= 0;
#else
        return false;
#endif
    }

    public bool RequestAutomaticCharacterToggle(string characterId)
    {
#if STEAMWORKS_NET
        if (!IsNetworkPartyActive ||
            CurrentSnapshot == null ||
            string.IsNullOrWhiteSpace(characterId))
        {
            return false;
        }

        for (int i = 0; i < CurrentSnapshot.Slots.Count; i++)
        {
            LobbyPartySlotState slot = CurrentSnapshot.Slots[i];

            if (slot.CharacterId != characterId)
                continue;

            if (slot.OwnerSteamId != localSteamId)
                return false;

            return RequestCharacterChange(i, string.Empty);
        }

        int emptySlotIndex = FindOwnedEmptySlot();
        return emptySlotIndex >= 0 &&
               RequestCharacterChange(emptySlotIndex, characterId);
#else
        return false;
#endif
    }

    public bool RequestCharacterChange(int slotIndex, string characterId)
    {
#if STEAMWORKS_NET
        if (!IsNetworkPartyActive ||
            CurrentSnapshot == null ||
            !CanLocalPlayerEditSlot(slotIndex) ||
            IsCharacterUsedByOtherSlot(characterId, slotIndex))
        {
            return false;
        }

        if (IsLocalHost())
        {
            LobbyPartyCharacterChangeCommand hostCommand =
                CreateLocalCommand(slotIndex, characterId);
            return TryApplyHostCommand(hostCommand).Accepted;
        }

        if (pendingLocalCommand != null)
        {
            queuedLocalIntent = new QueuedLocalIntent(slotIndex, characterId);
            ApplyOptimisticCharacter(slotIndex, characterId);
            return true;
        }

        return SendClientCommand(slotIndex, characterId);
#else
        return false;
#endif
    }

    public void LeaveLobby(bool preserveCharacters)
    {
#if STEAMWORKS_NET
        CSteamID lobbyToLeave = currentLobbyId;

        if (lobbyToLeave.IsValid())
            SteamMatchmaking.LeaveLobby(lobbyToLeave);

        currentLobbyId = default;
        localSteamId = 0UL;
        originalHostSteamId = 0UL;
        authorityState = null;
        authoritativeSnapshot = null;
        pendingLocalCommand = null;
        queuedLocalIntent = null;
#endif

        IsNetworkPartyActive = false;
        AppliedRevision = 0;
        CurrentSnapshot = null;
        RefreshPartyViews();
        PartyStateApplied?.Invoke();
    }

#if STEAMWORKS_NET
    private bool IsLocalHost()
    {
        return localSteamId != 0UL && localSteamId == originalHostSteamId;
    }

    private LobbyPartyCharacterChangeCommand CreateLocalCommand(
        int slotIndex,
        string characterId)
    {
        return new LobbyPartyCharacterChangeCommand(
            Guid.NewGuid().ToString("N"),
            localSteamId,
            slotIndex,
            characterId,
            AppliedRevision);
    }

    private bool SendClientCommand(int slotIndex, string characterId)
    {
        LobbyPartyCharacterChangeCommand command =
            CreateLocalCommand(slotIndex, characterId);
        string payload =
            CommandPrefix + LobbyPartySerialization.SerializeCommand(command);

        if (!TrySendLobbyChatPayload(payload))
            return false;

        pendingLocalCommand = new PendingLocalCommand(command);
        ApplyOptimisticCharacter(slotIndex, characterId);
        return true;
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

    private void ApplyOptimisticCharacter(int slotIndex, string characterId)
    {
        if (CurrentSnapshot == null ||
            slotIndex < 0 ||
            slotIndex >= CurrentSnapshot.Slots.Count)
        {
            return;
        }

        LobbyPartySlotState[] slots =
            new LobbyPartySlotState[CurrentSnapshot.Slots.Count];

        for (int i = 0; i < slots.Length; i++)
        {
            LobbyPartySlotState source = CurrentSnapshot.Slots[i];
            slots[i] = new LobbyPartySlotState(
                source.SlotIndex,
                source.OwnerSteamId,
                i == slotIndex ? characterId : source.CharacterId);
        }

        CurrentSnapshot = new LobbyPartySnapshot(
            CurrentSnapshot.HostSteamId,
            CurrentSnapshot.Revision,
            CurrentSnapshot.OrderedClientSteamIds,
            slots);
        RefreshOptimisticViews();
    }

    private int FindOwnedEmptySlot()
    {
        if (CurrentSnapshot == null)
            return -1;

        for (int i = 0; i < CurrentSnapshot.Slots.Count; i++)
        {
            LobbyPartySlotState slot = CurrentSnapshot.Slots[i];

            if (slot.OwnerSteamId == localSteamId &&
                string.IsNullOrWhiteSpace(slot.CharacterId))
            {
                return i;
            }
        }

        return -1;
    }

    private void EnsureSteamCallbacks()
    {
        if (lobbyChatMessage == null)
            lobbyChatMessage = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMessage);
    }

    private bool ReconcileHostMembership()
    {
        if (authorityState == null)
            return false;

        List<ulong> currentClients = ReadCurrentClientIds();
        bool changed = false;

        for (int i = authorityState.OrderedClientSteamIds.Count - 1; i >= 0; i--)
        {
            ulong existingClient = authorityState.OrderedClientSteamIds[i];

            if (currentClients.Contains(existingClient))
                continue;

            changed |= authorityState.RemoveClient(existingClient).Changed;
        }

        for (int i = 0; i < currentClients.Count; i++)
        {
            ulong clientId = currentClients[i];

            if (authorityState.ContainsMember(clientId))
                continue;

            changed |= authorityState.AddClient(clientId).Changed;
        }

        return changed;
    }

    private List<ulong> ReadCurrentClientIds()
    {
        List<ulong> result = new List<ulong>(2);
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyId);

        for (int i = 0; i < memberCount; i++)
        {
            ulong memberId = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyId, i).m_SteamID;

            if (memberId != originalHostSteamId && memberId != 0UL)
                result.Add(memberId);
        }

        return result;
    }

    private List<ulong> ReadCurrentMemberIds()
    {
        List<ulong> result = new List<ulong>(3);
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyId);

        for (int i = 0; i < memberCount; i++)
        {
            ulong memberId = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyId, i).m_SteamID;

            if (memberId != 0UL)
                result.Add(memberId);
        }

        return result;
    }

    private string[] ReadLocalCharacters()
    {
        string[] result = new string[LobbyPartyAuthorityState.SlotCount];
        PartyRuntimeStore partyStore = DataManager.Instance != null
            ? DataManager.Instance.PartyRuntimeStore
            : null;

        for (int i = 0; i < result.Length; i++)
            result[i] = partyStore?.GetCharacterId(i) ?? string.Empty;

        return result;
    }

    private void OnLobbyChatMessage(LobbyChatMsg_t callback)
    {
        if (!IsNetworkPartyActive ||
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
        }
    }

    private void HandleHostCommandPayload(string payload, CSteamID senderId)
    {
        string commandJson = payload.Substring(CommandPrefix.Length);

        if (!LobbyPartySerialization.TryDeserializeCommand(commandJson, out var command) ||
            command.RequesterSteamId != senderId.m_SteamID)
        {
            return;
        }

        LobbyPartyCommandResult result = TryApplyHostCommand(command);
        LobbyPartyCommandResponse response = new LobbyPartyCommandResponse(
            command.RequestId,
            command.RequesterSteamId,
            result.Accepted,
            result.RejectReason,
            authorityState != null ? authorityState.Revision : 0);
        string responsePayload =
            CommandResultPrefix +
            LobbyPartySerialization.SerializeCommandResponse(response);
        TrySendLobbyChatPayload(responsePayload);
    }

    private LobbyPartyCommandResult TryApplyHostCommand(
        LobbyPartyCharacterChangeCommand command)
    {
        if (authorityState == null)
        {
            return LobbyPartyCommandResult.Reject(
                LobbyPartyCommandRejectReason.UnknownMember);
        }

        LobbyPartyCommandResult result = authorityState.TryChangeCharacter(
            command,
            IsValidCharacterId);

        if (!result.Accepted)
        {
            Debug.LogWarning(
                "[SteamLobbyPartySynchronizer] Party command rejected: " +
                result.RejectReason,
                this);
            return result;
        }

        PublishSnapshot();
        return result;
    }

    private void HandleClientCommandResultPayload(string payload)
    {
        if (IsLocalHost() || pendingLocalCommand == null)
            return;

        string responseJson = payload.Substring(CommandResultPrefix.Length);

        if (!LobbyPartySerialization.TryDeserializeCommandResponse(
                responseJson,
                out var response) ||
            response.RequesterSteamId != localSteamId ||
            response.RequestId != pendingLocalCommand.Command.RequestId)
        {
            return;
        }

        if (!response.Accepted)
        {
            Debug.LogWarning(
                "[SteamLobbyPartySynchronizer] Party command rejected: " +
                response.RejectReason,
                this);
            pendingLocalCommand = null;
            queuedLocalIntent = null;
            CurrentSnapshot = authoritativeSnapshot;
            RefreshOptimisticViews();
            return;
        }

        pendingLocalCommand.AcceptedRevision = response.ResultRevision;
        TryCompletePendingCommand();
    }

    private bool IsValidCharacterId(string characterId)
    {
        return DataManager.Instance != null &&
               DataManager.Instance.CharacterDatabase != null &&
               DataManager.Instance.CharacterDatabase.TryGet(characterId, out _);
    }

    private void PublishSnapshot()
    {
        if (authorityState == null || !IsLocalHost())
            return;

        LobbyPartySnapshot snapshot = authorityState.CreateSnapshot();
        string payload = LobbyPartySerialization.SerializeSnapshot(snapshot);

        if (!SteamMatchmaking.SetLobbyData(currentLobbyId, SnapshotLobbyDataKey, payload))
        {
            Debug.LogWarning(
                "[SteamLobbyPartySynchronizer] Failed to publish party snapshot.",
                this);
            return;
        }

        ApplySnapshot(snapshot, true);
    }

    private void ApplySnapshotFromLobbyData()
    {
        string payload = SteamMatchmaking.GetLobbyData(
            currentLobbyId,
            SnapshotLobbyDataKey);

        if (!LobbyPartySerialization.TryDeserializeSnapshot(payload, out var snapshot))
            return;

        ApplySnapshot(snapshot, false);
    }

    private void ApplySnapshot(LobbyPartySnapshot snapshot, bool allowEqualRevision)
    {
        if (snapshot == null)
            return;

        bool isNewer = snapshot.Revision > AppliedRevision;
        bool isSameAuthoritativeState =
            allowEqualRevision && snapshot.Revision == AppliedRevision;

        if (!isNewer && !isSameAuthoritativeState)
            return;

        List<ulong> members = ReadCurrentMemberIds();

        if (!LobbyPartySerialization.ValidateSnapshot(
                snapshot,
                originalHostSteamId,
                members))
        {
            return;
        }

        ApplySnapshotToPartyRuntime(snapshot);
        authoritativeSnapshot = snapshot;
        AppliedRevision = snapshot.Revision;

        if (IsLocalHost() || pendingLocalCommand == null)
            CurrentSnapshot = snapshot;

        RefreshPartyViews();
        PartyStateApplied?.Invoke();
        TryCompletePendingCommand();
    }

    private void TryCompletePendingCommand()
    {
        if (IsLocalHost() ||
            pendingLocalCommand == null ||
            authoritativeSnapshot == null ||
            pendingLocalCommand.AcceptedRevision <= 0 ||
            authoritativeSnapshot.Revision < pendingLocalCommand.AcceptedRevision)
        {
            return;
        }

        pendingLocalCommand = null;
        CurrentSnapshot = authoritativeSnapshot;

        QueuedLocalIntent queuedIntent = queuedLocalIntent;
        queuedLocalIntent = null;

        if (queuedIntent == null)
        {
            RefreshOptimisticViews();
            return;
        }

        string authoritativeCharacter =
            authoritativeSnapshot.Slots[queuedIntent.SlotIndex].CharacterId;

        if (authoritativeCharacter == queuedIntent.CharacterId)
        {
            RefreshOptimisticViews();
            return;
        }

        if (!RequestCharacterChange(
                queuedIntent.SlotIndex,
                queuedIntent.CharacterId))
        {
            CurrentSnapshot = authoritativeSnapshot;
            RefreshOptimisticViews();
        }
    }

    private static void ApplySnapshotToPartyRuntime(LobbyPartySnapshot snapshot)
    {
        PartyRuntimeStore partyStore = DataManager.Instance != null
            ? DataManager.Instance.PartyRuntimeStore
            : null;

        if (partyStore == null)
            return;

        partyStore.Clear();

        for (int i = 0; i < snapshot.Slots.Count; i++)
        {
            string characterId = snapshot.Slots[i].CharacterId;

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            partyStore.SetCharacter(i, characterId);
            partyStore.SetSpawnGridIndex(i, FirstDefaultSpawnGridIndex + i);
        }
    }
#endif

    private static void RefreshOptimisticViews()
    {
        CharBtn[] charButtons = FindObjectsByType<CharBtn>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < charButtons.Length; i++)
            charButtons[i]?.RefreshSelectedPartyMarker();
    }

    private static void RefreshPartyViews()
    {
        PartySlot[] partySlots = FindObjectsByType<PartySlot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < partySlots.Length; i++)
            partySlots[i]?.RefreshFromRuntime();

        LobbyPartyStatusIconPresenter[] presenters =
            FindObjectsByType<LobbyPartyStatusIconPresenter>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int i = 0; i < presenters.Length; i++)
            presenters[i]?.Refresh();

        CharPick[] characterPickers = FindObjectsByType<CharPick>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < characterPickers.Length; i++)
            characterPickers[i]?.RefreshFromPartyRuntime();

        CharBtn[] charButtons = FindObjectsByType<CharBtn>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < charButtons.Length; i++)
            charButtons[i]?.RefreshSelectedPartyMarker();

        SpawnGridPanel[] spawnGridPanels = FindObjectsByType<SpawnGridPanel>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < spawnGridPanels.Length; i++)
        {
            spawnGridPanels[i]?.AutoPlacePartyIfNeeded();
            spawnGridPanels[i]?.Refresh();
        }
    }
}
