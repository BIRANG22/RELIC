using System;
using System.Collections.Generic;

public sealed class LobbyPartySlotState
{
    public int SlotIndex { get; }
    public ulong OwnerSteamId { get; }
    public string CharacterId { get; }

    public LobbyPartySlotState(int slotIndex, ulong ownerSteamId, string characterId)
    {
        SlotIndex = slotIndex;
        OwnerSteamId = ownerSteamId;
        CharacterId = characterId ?? string.Empty;
    }
}

public sealed class LobbyPartySnapshot
{
    public ulong HostSteamId { get; }
    public long Revision { get; }
    public IReadOnlyList<ulong> OrderedClientSteamIds { get; }
    public IReadOnlyList<LobbyPartySlotState> Slots { get; }
    public IReadOnlyList<LobbyPartyMemberViewState> ViewedCharacters { get; }

    public LobbyPartySnapshot(
        ulong hostSteamId,
        long revision,
        IReadOnlyList<ulong> orderedClientSteamIds,
        IReadOnlyList<LobbyPartySlotState> slots,
        IReadOnlyList<LobbyPartyMemberViewState> viewedCharacters = null)
    {
        HostSteamId = hostSteamId;
        Revision = revision;
        OrderedClientSteamIds = Copy(orderedClientSteamIds);
        Slots = Copy(slots);
        ViewedCharacters = Copy(viewedCharacters);
    }

    private static T[] Copy<T>(IReadOnlyList<T> source)
    {
        if (source == null)
            return Array.Empty<T>();

        T[] result = new T[source.Count];

        for (int i = 0; i < source.Count; i++)
            result[i] = source[i];

        return result;
    }
}

public sealed class LobbyPartyMemberViewState
{
    public ulong MemberSteamId { get; }
    public string CharacterId { get; }

    public LobbyPartyMemberViewState(ulong memberSteamId, string characterId)
    {
        MemberSteamId = memberSteamId;
        CharacterId = characterId ?? string.Empty;
    }
}

public sealed class LobbyPartyCharacterChangeCommand
{
    public string RequestId { get; }
    public ulong RequesterSteamId { get; }
    public int SlotIndex { get; }
    public string RequestedCharacterId { get; }
    public long KnownRevision { get; }

    public LobbyPartyCharacterChangeCommand(
        string requestId,
        ulong requesterSteamId,
        int slotIndex,
        string requestedCharacterId,
        long knownRevision)
    {
        RequestId = requestId ?? string.Empty;
        RequesterSteamId = requesterSteamId;
        SlotIndex = slotIndex;
        RequestedCharacterId = requestedCharacterId ?? string.Empty;
        KnownRevision = knownRevision;
    }
}

public sealed class LobbyPartyViewedCharacterCommand
{
    public string RequestId { get; }
    public ulong RequesterSteamId { get; }
    public string ViewedCharacterId { get; }
    public long KnownRevision { get; }

    public LobbyPartyViewedCharacterCommand(
        string requestId,
        ulong requesterSteamId,
        string viewedCharacterId,
        long knownRevision)
    {
        RequestId = requestId ?? string.Empty;
        RequesterSteamId = requesterSteamId;
        ViewedCharacterId = viewedCharacterId ?? string.Empty;
        KnownRevision = knownRevision;
    }
}

public sealed class LobbyPartyCommandResponse
{
    public string RequestId { get; }
    public ulong RequesterSteamId { get; }
    public bool Accepted { get; }
    public LobbyPartyCommandRejectReason RejectReason { get; }
    public long ResultRevision { get; }
    public LobbyPartySnapshot Snapshot { get; }

    public LobbyPartyCommandResponse(
        string requestId,
        ulong requesterSteamId,
        bool accepted,
        LobbyPartyCommandRejectReason rejectReason,
        long resultRevision,
        LobbyPartySnapshot snapshot = null)
    {
        RequestId = requestId ?? string.Empty;
        RequesterSteamId = requesterSteamId;
        Accepted = accepted;
        RejectReason = rejectReason;
        ResultRevision = resultRevision;
        Snapshot = snapshot;
    }
}

public sealed class LobbyPartyClientCommandPipeline
{
    private readonly List<PendingCommandState> pendingCommands = new();

    public bool HasPendingCommands => pendingCommands.Count > 0;
    public int PendingCommandCount => pendingCommands.Count;

    public bool TrackSentCommand(LobbyPartyCharacterChangeCommand command)
    {
        if (command == null)
            return false;

        return TrackSentRequest(command.RequestId, command.RequesterSteamId);
    }

    public bool TrackSentCommand(LobbyPartyViewedCharacterCommand command)
    {
        if (command == null)
            return false;

        return TrackSentRequest(command.RequestId, command.RequesterSteamId);
    }

    private bool TrackSentRequest(string requestId, ulong requesterSteamId)
    {
        if (string.IsNullOrWhiteSpace(requestId) || requesterSteamId == 0UL)
            return false;

        pendingCommands.Add(new PendingCommandState(requestId, requesterSteamId));
        return true;
    }

    public bool MarkHostResponse(LobbyPartyCommandResponse response)
    {
        if (response == null)
            return false;

        for (int i = 0; i < pendingCommands.Count; i++)
        {
            PendingCommandState pending = pendingCommands[i];

            if (pending.RequestId != response.RequestId ||
                pending.RequesterSteamId != response.RequesterSteamId)
            {
                continue;
            }

            if (!response.Accepted)
            {
                Clear();
                return true;
            }

            pending.AcceptedRevision = response.ResultRevision;
            return true;
        }

        return false;
    }

    public bool RemoveAcceptedThroughRevision(long authoritativeRevision)
    {
        bool removed = false;

        for (int i = pendingCommands.Count - 1; i >= 0; i--)
        {
            long acceptedRevision = pendingCommands[i].AcceptedRevision;

            if (acceptedRevision <= 0 || acceptedRevision > authoritativeRevision)
                continue;

            pendingCommands.RemoveAt(i);
            removed = true;
        }

        return removed;
    }

    public void Clear()
    {
        pendingCommands.Clear();
    }

    private sealed class PendingCommandState
    {
        public string RequestId { get; }
        public ulong RequesterSteamId { get; }
        public long AcceptedRevision { get; set; }

        public PendingCommandState(string requestId, ulong requesterSteamId)
        {
            RequestId = requestId ?? string.Empty;
            RequesterSteamId = requesterSteamId;
        }
    }
}

public enum LobbyPartyCommandRejectReason
{
    None,
    UnknownMember,
    InvalidSlot,
    NotSlotOwner,
    StaleRevision,
    InvalidCharacter,
    DuplicateCharacter,
    CharacterLockedByOtherMember
}

public readonly struct LobbyPartyCommandResult
{
    public bool Accepted { get; }
    public LobbyPartyCommandRejectReason RejectReason { get; }
    public LobbyPartySnapshot Snapshot { get; }

    private LobbyPartyCommandResult(
        bool accepted,
        LobbyPartyCommandRejectReason rejectReason,
        LobbyPartySnapshot snapshot)
    {
        Accepted = accepted;
        RejectReason = rejectReason;
        Snapshot = snapshot;
    }

    public static LobbyPartyCommandResult Accept(LobbyPartySnapshot snapshot)
    {
        return new LobbyPartyCommandResult(true, LobbyPartyCommandRejectReason.None, snapshot);
    }

    public static LobbyPartyCommandResult Reject(LobbyPartyCommandRejectReason reason)
    {
        return new LobbyPartyCommandResult(false, reason, null);
    }
}

public readonly struct LobbyPartyMembershipResult
{
    public bool Changed { get; }
    public LobbyPartySnapshot Snapshot { get; }

    public LobbyPartyMembershipResult(bool changed, LobbyPartySnapshot snapshot)
    {
        Changed = changed;
        Snapshot = snapshot;
    }
}
