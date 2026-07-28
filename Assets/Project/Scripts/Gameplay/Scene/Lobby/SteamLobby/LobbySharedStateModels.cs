using Relic.Gameplay.Data;
using UnityEngine;

public enum LobbySharedStateCommandType
{
    None,
    EquipRelic,
    UnequipRelic,
    EquipSkill,
    UnequipSkill
}

public enum LobbySharedStateCommandRejectReason
{
    None,
    UnknownMember,
    NotCharacterOwner,
    InvalidCommand,
    RejectedByService
}

public sealed class LobbySharedStateSnapshot
{
    public ulong HostSteamId { get; }
    public long Revision { get; }
    public int TrialSelectionMask { get; }
    public LobbyRuntimeData Lobby { get; }

    public LobbySharedStateSnapshot(
        ulong hostSteamId,
        long revision,
        int trialSelectionMask,
        LobbyRuntimeData lobby)
    {
        HostSteamId = hostSteamId;
        Revision = revision;
        TrialSelectionMask = trialSelectionMask;
        Lobby = LobbySharedStateRuntimeCopy.CopyLobbyRuntime(lobby);
    }

    public static LobbySharedStateSnapshot FromRuntime(
        ulong hostSteamId,
        long revision,
        int trialSelectionMask,
        LobbyRuntimeData lobby)
    {
        return new LobbySharedStateSnapshot(
            hostSteamId,
            revision,
            trialSelectionMask,
            lobby);
    }
}

public sealed class LobbySharedStateCommand
{
    public string RequestId { get; }
    public ulong RequesterSteamId { get; }
    public LobbySharedStateCommandType CommandType { get; }
    public string CharacterId { get; }
    public int SlotIndex { get; }
    public string ItemId { get; }
    public long KnownRevision { get; }

    public LobbySharedStateCommand(
        string requestId,
        ulong requesterSteamId,
        LobbySharedStateCommandType commandType,
        string characterId,
        int slotIndex,
        string itemId,
        long knownRevision)
    {
        RequestId = requestId ?? string.Empty;
        RequesterSteamId = requesterSteamId;
        CommandType = commandType;
        CharacterId = characterId ?? string.Empty;
        SlotIndex = slotIndex;
        ItemId = itemId ?? string.Empty;
        KnownRevision = knownRevision;
    }
}

public sealed class LobbySharedStateCommandResponse
{
    public string RequestId { get; }
    public ulong RequesterSteamId { get; }
    public bool Accepted { get; }
    public LobbySharedStateCommandRejectReason RejectReason { get; }
    public long ResultRevision { get; }
    public LobbySharedStateSnapshot Snapshot { get; }

    public LobbySharedStateCommandResponse(
        string requestId,
        ulong requesterSteamId,
        bool accepted,
        LobbySharedStateCommandRejectReason rejectReason,
        long resultRevision,
        LobbySharedStateSnapshot snapshot = null)
    {
        RequestId = requestId ?? string.Empty;
        RequesterSteamId = requesterSteamId;
        Accepted = accepted;
        RejectReason = rejectReason;
        ResultRevision = resultRevision;
        Snapshot = snapshot;
    }
}

public readonly struct LobbySharedStateCommandResult
{
    public bool Accepted { get; }
    public LobbySharedStateCommandRejectReason RejectReason { get; }
    public LobbySharedStateSnapshot Snapshot { get; }

    private LobbySharedStateCommandResult(
        bool accepted,
        LobbySharedStateCommandRejectReason rejectReason,
        LobbySharedStateSnapshot snapshot)
    {
        Accepted = accepted;
        RejectReason = rejectReason;
        Snapshot = snapshot;
    }

    public static LobbySharedStateCommandResult Accept(
        LobbySharedStateSnapshot snapshot)
    {
        return new LobbySharedStateCommandResult(
            true,
            LobbySharedStateCommandRejectReason.None,
            snapshot);
    }

    public static LobbySharedStateCommandResult Reject(
        LobbySharedStateCommandRejectReason reason)
    {
        return new LobbySharedStateCommandResult(false, reason, null);
    }
}

public static class LobbySharedStateRuntimeCopy
{
    public static LobbyRuntimeData CopyLobbyRuntime(LobbyRuntimeData source)
    {
        LobbyRuntimeData copy;

        if (source == null)
        {
            copy = new LobbyRuntimeData();
        }
        else
        {
            string json = JsonUtility.ToJson(source);
            copy = JsonUtility.FromJson<LobbyRuntimeData>(json);
        }

        LobbyRuntimeStore store = new();
        store.Set(copy);
        return store.GetOrCreate();
    }
}
