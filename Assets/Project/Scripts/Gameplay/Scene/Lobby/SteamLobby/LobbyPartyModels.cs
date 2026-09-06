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

    public LobbyPartySnapshot WithSlotCharacter(int slotIndex, string characterId)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
            return this;

        LobbyPartySlotState[] slots = new LobbyPartySlotState[Slots.Count];

        for (int i = 0; i < slots.Length; i++)
        {
            LobbyPartySlotState source = Slots[i];
            slots[i] = new LobbyPartySlotState(
                source.SlotIndex,
                source.OwnerSteamId,
                i == slotIndex ? characterId : source.CharacterId);
        }

        return new LobbyPartySnapshot(
            HostSteamId,
            Revision,
            OrderedClientSteamIds,
            slots,
            ViewedCharacters);
    }

    public LobbyPartySnapshot WithViewedCharacter(
        ulong memberSteamId,
        string characterId)
    {
        if (memberSteamId == 0UL)
            return this;

        bool isClearRequest = string.IsNullOrWhiteSpace(characterId);
        bool replaced = false;
        List<LobbyPartyMemberViewState> viewedCharacters = new();

        for (int i = 0; i < ViewedCharacters.Count; i++)
        {
            LobbyPartyMemberViewState view = ViewedCharacters[i];

            if (view.MemberSteamId != memberSteamId)
            {
                viewedCharacters.Add(view);
                continue;
            }

            replaced = true;

            if (!isClearRequest)
                viewedCharacters.Add(
                    new LobbyPartyMemberViewState(memberSteamId, characterId));
        }

        if (!replaced && !isClearRequest)
            viewedCharacters.Add(
                new LobbyPartyMemberViewState(memberSteamId, characterId));

        return new LobbyPartySnapshot(
            HostSteamId,
            Revision,
            OrderedClientSteamIds,
            Slots,
            viewedCharacters);
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

        return TrackSentRequest(PendingCommandState.ForCharacterChange(command));
    }

    public bool TrackSentCommand(LobbyPartyViewedCharacterCommand command)
    {
        if (command == null)
            return false;

        return TrackSentRequest(PendingCommandState.ForViewedCharacter(command));
    }

    private bool TrackSentRequest(PendingCommandState command)
    {
        if (command == null ||
            string.IsNullOrWhiteSpace(command.RequestId) ||
            command.RequesterSteamId == 0UL)
        {
            return false;
        }

        pendingCommands.Add(command);
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

    public LobbyPartySnapshot ApplyPendingOptimism(LobbyPartySnapshot snapshot)
    {
        LobbyPartySnapshot result = snapshot;

        if (result == null)
            return null;

        for (int i = 0; i < pendingCommands.Count; i++)
            result = pendingCommands[i].ApplyTo(result);

        return result;
    }

    private sealed class PendingCommandState
    {
        public string RequestId { get; }
        public ulong RequesterSteamId { get; }
        public int SlotIndex { get; }
        public string CharacterId { get; }
        public bool IsViewedCharacterCommand { get; }
        public long AcceptedRevision { get; set; }

        private PendingCommandState(
            string requestId,
            ulong requesterSteamId,
            int slotIndex,
            string characterId,
            bool isViewedCharacterCommand)
        {
            RequestId = requestId ?? string.Empty;
            RequesterSteamId = requesterSteamId;
            SlotIndex = slotIndex;
            CharacterId = characterId ?? string.Empty;
            IsViewedCharacterCommand = isViewedCharacterCommand;
        }

        public static PendingCommandState ForCharacterChange(
            LobbyPartyCharacterChangeCommand command)
        {
            return new PendingCommandState(
                command.RequestId,
                command.RequesterSteamId,
                command.SlotIndex,
                command.RequestedCharacterId,
                false);
        }

        public static PendingCommandState ForViewedCharacter(
            LobbyPartyViewedCharacterCommand command)
        {
            return new PendingCommandState(
                command.RequestId,
                command.RequesterSteamId,
                -1,
                command.ViewedCharacterId,
                true);
        }

        public LobbyPartySnapshot ApplyTo(LobbyPartySnapshot snapshot)
        {
            if (snapshot == null)
                return null;

            return IsViewedCharacterCommand
                ? snapshot.WithViewedCharacter(RequesterSteamId, CharacterId)
                : snapshot.WithSlotCharacter(SlotIndex, CharacterId);
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
