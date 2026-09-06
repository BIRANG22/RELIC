using System;
using System.Text;
using System.Threading.Tasks;
using Relic.Gameplay.Data;
using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

[DisallowMultipleComponent]
public sealed class SteamLobbyBattleStartSynchronizer : MonoBehaviour
{
    private const string BattleStartLobbyDataKey = "relic.battle.start.v1";
    private const string BattleStartBroadcastPrefix = "RELIC_BATTLE_START_V1:";
    private const int MaxLobbyChatMessageBytes = 4096;

    public static SteamLobbyBattleStartSynchronizer Instance { get; private set; }

    [SerializeField] private float sharedStateWaitTimeoutSeconds = 5f;

    public bool IsNetworkBattleStartActive { get; private set; }

    private bool isEnteringBattle;
    private string pendingBattleSessionId;
    private string completedBattleSessionId;

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
        pendingBattleSessionId = null;
        completedBattleSessionId = null;
        isEnteringBattle = false;
        IsNetworkBattleStartActive = true;

        if (!IsLocalHost())
            ApplyBattleStartFromLobbyData();
#endif
    }

    public void HandleLobbyMembershipChanged()
    {
#if STEAMWORKS_NET
        if (!IsNetworkBattleStartActive || !currentLobbyId.IsValid())
            return;

        ulong currentOwnerId = SteamMatchmaking.GetLobbyOwner(currentLobbyId).m_SteamID;
        if (currentOwnerId != originalHostSteamId)
            LeaveLobby();
#endif
    }

    public void HandleLobbyDataChanged()
    {
#if STEAMWORKS_NET
        if (!IsNetworkBattleStartActive || IsLocalHost())
            return;

        ApplyBattleStartFromLobbyData();
#endif
    }

    public void LeaveLobby()
    {
#if STEAMWORKS_NET
        currentLobbyId = default;
        localSteamId = 0UL;
        originalHostSteamId = 0UL;
#endif

        IsNetworkBattleStartActive = false;
        pendingBattleSessionId = null;
        completedBattleSessionId = null;
        isEnteringBattle = false;
    }

    public bool CanLocalPlayerStartBattle()
    {
        if (!IsNetworkBattleStartActive)
            return true;

#if STEAMWORKS_NET
        return IsLocalHost();
#else
        return false;
#endif
    }

    public bool TryBroadcastBattleStart(
        LobbySharedStateSnapshot sharedSnapshot,
        MapRuntimeData mapRuntime,
        out LobbyBattleStartCommand command)
    {
        command = null;

        if (!IsNetworkBattleStartActive)
            return false;

#if STEAMWORKS_NET
        if (!IsLocalHost() ||
            sharedSnapshot == null ||
            sharedSnapshot.Revision <= 0 ||
            mapRuntime == null ||
            string.IsNullOrWhiteSpace(mapRuntime.SelectedChapterId) ||
            string.IsNullOrWhiteSpace(mapRuntime.CurrentStage))
        {
            return false;
        }

        command = new LobbyBattleStartCommand(
            Guid.NewGuid().ToString("N"),
            localSteamId,
            sharedSnapshot.Revision,
            Guid.NewGuid().ToString("N"),
            Guid.NewGuid().GetHashCode(),
            mapRuntime.SelectedChapterId.Trim(),
            mapRuntime.CurrentStage.Trim());

        string commandJson = LobbyBattleStartSerialization.SerializeCommand(command);
        bool dataDelivered = SteamMatchmaking.SetLobbyData(
            currentLobbyId,
            BattleStartLobbyDataKey,
            commandJson);
        bool chatDelivered = TrySendLobbyChatPayload(
            BattleStartBroadcastPrefix + commandJson);

        if (!dataDelivered && !chatDelivered)
        {
            Debug.LogWarning(
                "[SteamLobbyBattleStartSynchronizer] Failed to broadcast battle start.",
                this);
            return false;
        }

        completedBattleSessionId = command.BattleSessionId;
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

    private void OnLobbyChatMessage(LobbyChatMsg_t callback)
    {
        if (!IsNetworkBattleStartActive ||
            !currentLobbyId.IsValid() ||
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

        if (byteCount <= 0 ||
            senderId.m_SteamID != originalHostSteamId ||
            IsLocalHost())
        {
            return;
        }

        string payload = Encoding.UTF8.GetString(buffer, 0, byteCount);
        if (!payload.StartsWith(BattleStartBroadcastPrefix, StringComparison.Ordinal))
            return;

        string commandJson = payload.Substring(BattleStartBroadcastPrefix.Length);
        if (!LobbyBattleStartSerialization.TryDeserializeCommand(
                commandJson,
                out LobbyBattleStartCommand command))
        {
            return;
        }

        TryBeginClientBattleStart(command);
    }

    private void ApplyBattleStartFromLobbyData()
    {
        string payload = SteamMatchmaking.GetLobbyData(
            currentLobbyId,
            BattleStartLobbyDataKey);

        if (!LobbyBattleStartSerialization.TryDeserializeCommand(
                payload,
                out LobbyBattleStartCommand command))
        {
            return;
        }

        TryBeginClientBattleStart(command);
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

    private void TryBeginClientBattleStart(LobbyBattleStartCommand command)
    {
        if (command == null ||
            command.HostSteamId != originalHostSteamId ||
            string.IsNullOrWhiteSpace(command.BattleSessionId) ||
            string.Equals(command.BattleSessionId, pendingBattleSessionId, StringComparison.Ordinal) ||
            string.Equals(command.BattleSessionId, completedBattleSessionId, StringComparison.Ordinal) ||
            isEnteringBattle)
        {
            return;
        }

        BeginClientBattleStartAsync(command);
    }

    private async void BeginClientBattleStartAsync(LobbyBattleStartCommand command)
    {
        isEnteringBattle = true;
        pendingBattleSessionId = command.BattleSessionId;

        bool sharedStateReady =
            await WaitForRequiredSharedStateAsync(command.RequiredSharedStateRevision);
        if (!sharedStateReady)
        {
            Debug.LogWarning(
                "[SteamLobbyBattleStartSynchronizer] Timed out while waiting for shared lobby state before battle start.",
                this);
            pendingBattleSessionId = null;
            isEnteringBattle = false;
            return;
        }

        LobbyBattleEntryResult result =
            await LobbyBattleEntryService.EnterBattleAsync(command);

        if (!result.Succeeded)
        {
            Debug.LogWarning(
                "[SteamLobbyBattleStartSynchronizer] Failed to enter battle from host command. " +
                result.Error,
                this);
            pendingBattleSessionId = null;
            isEnteringBattle = false;
            return;
        }

        completedBattleSessionId = command.BattleSessionId;
        pendingBattleSessionId = null;
        isEnteringBattle = false;
    }

    private async Task<bool> WaitForRequiredSharedStateAsync(long requiredRevision)
    {
        if (requiredRevision <= 0)
            return true;

        SteamLobbySharedStateSynchronizer sharedStateSynchronizer =
            SteamLobbySharedStateSynchronizer.Instance;
        if (sharedStateSynchronizer == null)
            return false;

        float deadline = Time.realtimeSinceStartup +
                         Mathf.Max(0.1f, sharedStateWaitTimeoutSeconds);

        while (Time.realtimeSinceStartup <= deadline)
        {
            if (sharedStateSynchronizer.AppliedRevision >= requiredRevision)
                return true;

            sharedStateSynchronizer.HandleLobbyDataChanged();
            await Task.Yield();
        }

        return sharedStateSynchronizer.AppliedRevision >= requiredRevision;
    }
#endif
}
