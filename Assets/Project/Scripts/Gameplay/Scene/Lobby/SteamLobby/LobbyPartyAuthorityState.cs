using System;
using System.Collections.Generic;

public sealed class LobbyPartyAuthorityState
{
    public const int SlotCount = 3;
    private const int MaxClientCount = SlotCount - 1;

    private readonly List<ulong> orderedClientSteamIds = new(MaxClientCount);
    private readonly LobbyPartySlotState[] slots = new LobbyPartySlotState[SlotCount];
    private readonly Dictionary<ulong, string> viewedCharacterIds = new();

    public ulong HostSteamId { get; }
    public long Revision { get; private set; }
    public IReadOnlyList<ulong> OrderedClientSteamIds => orderedClientSteamIds;

    private LobbyPartyAuthorityState(ulong hostSteamId)
    {
        HostSteamId = hostSteamId;
    }

    public static LobbyPartyAuthorityState CreateHost(
        ulong hostSteamId,
        IReadOnlyList<string> characterIds)
    {
        if (hostSteamId == 0UL)
            throw new ArgumentOutOfRangeException(nameof(hostSteamId));

        LobbyPartyAuthorityState state = new LobbyPartyAuthorityState(hostSteamId);
        HashSet<string> assignedCharacterIds =
            new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < SlotCount; i++)
        {
            string characterId = characterIds != null && i < characterIds.Count
                ? characterIds[i]
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(characterId) &&
                !assignedCharacterIds.Add(characterId))
            {
                characterId = string.Empty;
            }

            state.slots[i] = new LobbyPartySlotState(i, hostSteamId, characterId);
        }

        state.Revision = 1;
        return state;
    }

    public LobbyPartySlotState GetSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            throw new ArgumentOutOfRangeException(nameof(slotIndex));

        return slots[slotIndex];
    }

    public bool ContainsMember(ulong steamId)
    {
        return steamId == HostSteamId || orderedClientSteamIds.Contains(steamId);
    }

    public LobbyPartyMembershipResult AddClient(ulong clientSteamId)
    {
        if (clientSteamId == 0UL ||
            ContainsMember(clientSteamId) ||
            orderedClientSteamIds.Count >= MaxClientCount)
        {
            return UnchangedMembership();
        }

        orderedClientSteamIds.Add(clientSteamId);

        if (orderedClientSteamIds.Count == 1)
        {
            SetSlot(2, clientSteamId, slots[2].CharacterId);
        }
        else
        {
            string secondCharacter = slots[1].CharacterId;
            string thirdCharacter = slots[2].CharacterId;
            SetSlot(1, orderedClientSteamIds[0], thirdCharacter);
            SetSlot(2, orderedClientSteamIds[1], secondCharacter);
        }

        Revision++;
        return ChangedMembership();
    }

    public LobbyPartyMembershipResult RemoveClient(ulong clientSteamId)
    {
        int removedIndex = orderedClientSteamIds.IndexOf(clientSteamId);

        if (removedIndex < 0)
            return UnchangedMembership();

        int previousClientCount = orderedClientSteamIds.Count;
        orderedClientSteamIds.RemoveAt(removedIndex);
        viewedCharacterIds.Remove(clientSteamId);

        if (previousClientCount == 1)
        {
            SetSlot(2, HostSteamId, slots[2].CharacterId);
        }
        else if (removedIndex == 0)
        {
            SetSlot(1, HostSteamId, slots[1].CharacterId);
            SetSlot(2, orderedClientSteamIds[0], slots[2].CharacterId);
        }
        else
        {
            string secondCharacter = slots[1].CharacterId;
            string thirdCharacter = slots[2].CharacterId;
            SetSlot(1, HostSteamId, thirdCharacter);
            SetSlot(2, orderedClientSteamIds[0], secondCharacter);
        }

        Revision++;
        return ChangedMembership();
    }

    public string GetViewedCharacterId(ulong memberSteamId)
    {
        return viewedCharacterIds.TryGetValue(memberSteamId, out string characterId)
            ? characterId
            : string.Empty;
    }

    public LobbyPartyCommandResult TryViewCharacter(
        LobbyPartyViewedCharacterCommand command,
        Func<string, bool> isValidCharacterId)
    {
        if (command == null || !ContainsMember(command.RequesterSteamId))
            return LobbyPartyCommandResult.Reject(LobbyPartyCommandRejectReason.UnknownMember);

        bool isClearRequest = string.IsNullOrWhiteSpace(command.ViewedCharacterId);
        string currentCharacterId = GetViewedCharacterId(command.RequesterSteamId);

        if (isClearRequest)
        {
            if (string.IsNullOrWhiteSpace(currentCharacterId))
                return LobbyPartyCommandResult.Accept(CreateSnapshot());

            viewedCharacterIds.Remove(command.RequesterSteamId);
            Revision++;
            return LobbyPartyCommandResult.Accept(CreateSnapshot());
        }

        if (isValidCharacterId == null ||
            !isValidCharacterId(command.ViewedCharacterId))
        {
            return LobbyPartyCommandResult.Reject(LobbyPartyCommandRejectReason.InvalidCharacter);
        }

        if (IsCharacterLockedByOtherMember(
                command.ViewedCharacterId,
                command.RequesterSteamId))
        {
            return LobbyPartyCommandResult.Reject(
                LobbyPartyCommandRejectReason.CharacterLockedByOtherMember);
        }

        if (currentCharacterId == command.ViewedCharacterId)
            return LobbyPartyCommandResult.Accept(CreateSnapshot());

        viewedCharacterIds[command.RequesterSteamId] = command.ViewedCharacterId;
        Revision++;
        return LobbyPartyCommandResult.Accept(CreateSnapshot());
    }

    public LobbyPartyCommandResult TryChangeCharacter(
        LobbyPartyCharacterChangeCommand command,
        Func<string, bool> isValidCharacterId)
    {
        if (command == null || !ContainsMember(command.RequesterSteamId))
            return LobbyPartyCommandResult.Reject(LobbyPartyCommandRejectReason.UnknownMember);

        if (!IsValidSlot(command.SlotIndex))
            return LobbyPartyCommandResult.Reject(LobbyPartyCommandRejectReason.InvalidSlot);

        LobbyPartySlotState targetSlot = slots[command.SlotIndex];

        if (targetSlot.OwnerSteamId != command.RequesterSteamId)
            return LobbyPartyCommandResult.Reject(LobbyPartyCommandRejectReason.NotSlotOwner);

        bool isClearRequest = string.IsNullOrWhiteSpace(command.RequestedCharacterId);

        if (isClearRequest && string.IsNullOrWhiteSpace(targetSlot.CharacterId))
            return LobbyPartyCommandResult.Accept(CreateSnapshot());

        if (!isClearRequest &&
            (isValidCharacterId == null ||
             !isValidCharacterId(command.RequestedCharacterId)))
        {
            return LobbyPartyCommandResult.Reject(LobbyPartyCommandRejectReason.InvalidCharacter);
        }

        if (!isClearRequest && targetSlot.CharacterId == command.RequestedCharacterId)
            return LobbyPartyCommandResult.Accept(CreateSnapshot());

        if (!isClearRequest &&
            IsViewedByOtherMember(command.RequestedCharacterId, command.RequesterSteamId))
        {
            return LobbyPartyCommandResult.Reject(
                LobbyPartyCommandRejectReason.CharacterLockedByOtherMember);
        }

        for (int i = 0; !isClearRequest && i < SlotCount; i++)
        {
            if (i != command.SlotIndex &&
                slots[i].CharacterId == command.RequestedCharacterId)
            {
                return LobbyPartyCommandResult.Reject(
                    LobbyPartyCommandRejectReason.DuplicateCharacter);
            }
        }

        SetSlot(command.SlotIndex, command.RequesterSteamId, command.RequestedCharacterId);
        Revision++;
        return LobbyPartyCommandResult.Accept(CreateSnapshot());
    }

    public LobbyPartySnapshot CreateSnapshot()
    {
        LobbyPartySlotState[] snapshotSlots = new LobbyPartySlotState[SlotCount];

        for (int i = 0; i < SlotCount; i++)
        {
            LobbyPartySlotState slot = slots[i];
            snapshotSlots[i] = new LobbyPartySlotState(
                slot.SlotIndex,
                slot.OwnerSteamId,
                slot.CharacterId);
        }

        LobbyPartyMemberViewState[] viewedCharacters = CreateViewedCharacterSnapshot();

        return new LobbyPartySnapshot(
            HostSteamId,
            Revision,
            orderedClientSteamIds,
            snapshotSlots,
            viewedCharacters);
    }

    private void SetSlot(int slotIndex, ulong ownerSteamId, string characterId)
    {
        slots[slotIndex] = new LobbyPartySlotState(slotIndex, ownerSteamId, characterId);
    }

    private LobbyPartyMembershipResult ChangedMembership()
    {
        return new LobbyPartyMembershipResult(true, CreateSnapshot());
    }

    private LobbyPartyMembershipResult UnchangedMembership()
    {
        return new LobbyPartyMembershipResult(false, CreateSnapshot());
    }

    private LobbyPartyMemberViewState[] CreateViewedCharacterSnapshot()
    {
        List<LobbyPartyMemberViewState> result = new();
        AddViewedCharacterSnapshot(result, HostSteamId);

        for (int i = 0; i < orderedClientSteamIds.Count; i++)
            AddViewedCharacterSnapshot(result, orderedClientSteamIds[i]);

        return result.ToArray();
    }

    private void AddViewedCharacterSnapshot(
        List<LobbyPartyMemberViewState> result,
        ulong memberSteamId)
    {
        string characterId = GetViewedCharacterId(memberSteamId);

        if (string.IsNullOrWhiteSpace(characterId))
            return;

        result.Add(new LobbyPartyMemberViewState(memberSteamId, characterId));
    }

    private bool IsCharacterLockedByOtherMember(string characterId, ulong requesterSteamId)
    {
        return IsAssignedToOtherMember(characterId, requesterSteamId) ||
               IsViewedByOtherMember(characterId, requesterSteamId);
    }

    private bool IsAssignedToOtherMember(string characterId, ulong requesterSteamId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return false;

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i].OwnerSteamId != requesterSteamId &&
                slots[i].CharacterId == characterId)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsViewedByOtherMember(string characterId, ulong requesterSteamId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return false;

        foreach (KeyValuePair<ulong, string> pair in viewedCharacterIds)
        {
            if (pair.Key != requesterSteamId && pair.Value == characterId)
                return true;
        }

        return false;
    }

    private static bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < SlotCount;
    }
}
