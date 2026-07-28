using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class LobbyPartySerialization
{
    private const int ProtocolVersion = 1;

    [Serializable]
    private sealed class CommandDto
    {
        public int version;
        public string requestId;
        public string requesterSteamId;
        public int slotIndex;
        public string requestedCharacterId;
        public long knownRevision;
    }

    [Serializable]
    private sealed class ViewCommandDto
    {
        public int version;
        public string requestId;
        public string requesterSteamId;
        public string viewedCharacterId;
        public long knownRevision;
    }

    [Serializable]
    private sealed class SnapshotDto
    {
        public int version;
        public string hostSteamId;
        public long revision;
        public string[] orderedClientSteamIds;
        public SlotDto[] slots;
        public MemberViewDto[] viewedCharacters;
    }

    [Serializable]
    private sealed class CommandResponseDto
    {
        public int version;
        public string requestId;
        public string requesterSteamId;
        public bool accepted;
        public int rejectReason;
        public long resultRevision;
    }

    [Serializable]
    private sealed class SlotDto
    {
        public int slotIndex;
        public string ownerSteamId;
        public string characterId;
    }

    [Serializable]
    private sealed class MemberViewDto
    {
        public string memberSteamId;
        public string characterId;
    }

    public static string SerializeCommand(LobbyPartyCharacterChangeCommand command)
    {
        if (command == null)
            return string.Empty;

        CommandDto dto = new CommandDto
        {
            version = ProtocolVersion,
            requestId = command.RequestId,
            requesterSteamId = ToText(command.RequesterSteamId),
            slotIndex = command.SlotIndex,
            requestedCharacterId = command.RequestedCharacterId,
            knownRevision = command.KnownRevision
        };

        return JsonUtility.ToJson(dto);
    }

    public static bool TryDeserializeCommand(
        string payload,
        out LobbyPartyCharacterChangeCommand command)
    {
        command = null;

        if (string.IsNullOrWhiteSpace(payload))
            return false;

        try
        {
            CommandDto dto = JsonUtility.FromJson<CommandDto>(payload);

            if (dto == null ||
                dto.version != ProtocolVersion ||
                string.IsNullOrWhiteSpace(dto.requestId) ||
                !TryParseSteamId(dto.requesterSteamId, out ulong requesterSteamId))
            {
                return false;
            }

            command = new LobbyPartyCharacterChangeCommand(
                dto.requestId,
                requesterSteamId,
                dto.slotIndex,
                dto.requestedCharacterId,
                dto.knownRevision);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static string SerializeViewCommand(LobbyPartyViewedCharacterCommand command)
    {
        if (command == null)
            return string.Empty;

        ViewCommandDto dto = new ViewCommandDto
        {
            version = ProtocolVersion,
            requestId = command.RequestId,
            requesterSteamId = ToText(command.RequesterSteamId),
            viewedCharacterId = command.ViewedCharacterId,
            knownRevision = command.KnownRevision
        };

        return JsonUtility.ToJson(dto);
    }

    public static bool TryDeserializeViewCommand(
        string payload,
        out LobbyPartyViewedCharacterCommand command)
    {
        command = null;

        if (string.IsNullOrWhiteSpace(payload))
            return false;

        try
        {
            ViewCommandDto dto = JsonUtility.FromJson<ViewCommandDto>(payload);

            if (dto == null ||
                dto.version != ProtocolVersion ||
                string.IsNullOrWhiteSpace(dto.requestId) ||
                !TryParseSteamId(dto.requesterSteamId, out ulong requesterSteamId))
            {
                return false;
            }

            command = new LobbyPartyViewedCharacterCommand(
                dto.requestId,
                requesterSteamId,
                dto.viewedCharacterId,
                dto.knownRevision);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static string SerializeSnapshot(LobbyPartySnapshot snapshot)
    {
        if (snapshot == null)
            return string.Empty;

        SnapshotDto dto = new SnapshotDto
        {
            version = ProtocolVersion,
            hostSteamId = ToText(snapshot.HostSteamId),
            revision = snapshot.Revision,
            orderedClientSteamIds = new string[snapshot.OrderedClientSteamIds.Count],
            slots = new SlotDto[snapshot.Slots.Count],
            viewedCharacters = new MemberViewDto[snapshot.ViewedCharacters.Count]
        };

        for (int i = 0; i < snapshot.OrderedClientSteamIds.Count; i++)
            dto.orderedClientSteamIds[i] = ToText(snapshot.OrderedClientSteamIds[i]);

        for (int i = 0; i < snapshot.Slots.Count; i++)
        {
            LobbyPartySlotState slot = snapshot.Slots[i];
            dto.slots[i] = new SlotDto
            {
                slotIndex = slot.SlotIndex,
                ownerSteamId = ToText(slot.OwnerSteamId),
                characterId = slot.CharacterId
            };
        }

        for (int i = 0; i < snapshot.ViewedCharacters.Count; i++)
        {
            LobbyPartyMemberViewState view = snapshot.ViewedCharacters[i];
            dto.viewedCharacters[i] = new MemberViewDto
            {
                memberSteamId = ToText(view.MemberSteamId),
                characterId = view.CharacterId
            };
        }

        return JsonUtility.ToJson(dto);
    }

    public static string SerializeCommandResponse(LobbyPartyCommandResponse response)
    {
        if (response == null)
            return string.Empty;

        CommandResponseDto dto = new CommandResponseDto
        {
            version = ProtocolVersion,
            requestId = response.RequestId,
            requesterSteamId = ToText(response.RequesterSteamId),
            accepted = response.Accepted,
            rejectReason = (int)response.RejectReason,
            resultRevision = response.ResultRevision
        };

        return JsonUtility.ToJson(dto);
    }

    public static bool TryDeserializeCommandResponse(
        string payload,
        out LobbyPartyCommandResponse response)
    {
        response = null;

        if (string.IsNullOrWhiteSpace(payload))
            return false;

        try
        {
            CommandResponseDto dto = JsonUtility.FromJson<CommandResponseDto>(payload);

            if (dto == null ||
                dto.version != ProtocolVersion ||
                string.IsNullOrWhiteSpace(dto.requestId) ||
                !TryParseSteamId(dto.requesterSteamId, out ulong requesterSteamId) ||
                !Enum.IsDefined(typeof(LobbyPartyCommandRejectReason), dto.rejectReason))
            {
                return false;
            }

            response = new LobbyPartyCommandResponse(
                dto.requestId,
                requesterSteamId,
                dto.accepted,
                (LobbyPartyCommandRejectReason)dto.rejectReason,
                dto.resultRevision);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool TryDeserializeSnapshot(string payload, out LobbyPartySnapshot snapshot)
    {
        snapshot = null;

        if (string.IsNullOrWhiteSpace(payload))
            return false;

        try
        {
            SnapshotDto dto = JsonUtility.FromJson<SnapshotDto>(payload);

            if (dto == null ||
                dto.version != ProtocolVersion ||
                dto.revision <= 0 ||
                !TryParseSteamId(dto.hostSteamId, out ulong hostSteamId) ||
                dto.orderedClientSteamIds == null ||
                dto.slots == null ||
                dto.slots.Length != LobbyPartyAuthorityState.SlotCount)
            {
                return false;
            }

            ulong[] clients = new ulong[dto.orderedClientSteamIds.Length];

            for (int i = 0; i < clients.Length; i++)
            {
                if (!TryParseSteamId(dto.orderedClientSteamIds[i], out clients[i]))
                    return false;
            }

            LobbyPartySlotState[] slots =
                new LobbyPartySlotState[LobbyPartyAuthorityState.SlotCount];

            for (int i = 0; i < dto.slots.Length; i++)
            {
                SlotDto slotDto = dto.slots[i];

                if (slotDto == null ||
                    !TryParseSteamId(slotDto.ownerSteamId, out ulong ownerSteamId))
                {
                    return false;
                }

                slots[i] = new LobbyPartySlotState(
                    slotDto.slotIndex,
                    ownerSteamId,
                    slotDto.characterId);
            }

            MemberViewDto[] viewDtos = dto.viewedCharacters ?? Array.Empty<MemberViewDto>();
            LobbyPartyMemberViewState[] viewedCharacters =
                new LobbyPartyMemberViewState[viewDtos.Length];

            for (int i = 0; i < viewDtos.Length; i++)
            {
                MemberViewDto viewDto = viewDtos[i];

                if (viewDto == null ||
                    !TryParseSteamId(viewDto.memberSteamId, out ulong memberSteamId))
                {
                    return false;
                }

                viewedCharacters[i] = new LobbyPartyMemberViewState(
                    memberSteamId,
                    viewDto.characterId);
            }

            snapshot = new LobbyPartySnapshot(
                hostSteamId,
                dto.revision,
                clients,
                slots,
                viewedCharacters);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool ValidateSnapshot(
        LobbyPartySnapshot snapshot,
        ulong expectedHostSteamId,
        IReadOnlyCollection<ulong> currentMembers)
    {
        if (snapshot == null ||
            expectedHostSteamId == 0UL ||
            snapshot.HostSteamId != expectedHostSteamId ||
            snapshot.Revision <= 0 ||
            snapshot.Slots == null ||
            snapshot.Slots.Count != LobbyPartyAuthorityState.SlotCount ||
            snapshot.OrderedClientSteamIds == null ||
            snapshot.OrderedClientSteamIds.Count > 2 ||
            currentMembers == null ||
            !ContainsMember(currentMembers, expectedHostSteamId))
        {
            return false;
        }

        HashSet<ulong> clients = new HashSet<ulong>();

        for (int i = 0; i < snapshot.OrderedClientSteamIds.Count; i++)
        {
            ulong clientId = snapshot.OrderedClientSteamIds[i];

            if (clientId == expectedHostSteamId ||
                !ContainsMember(currentMembers, clientId) ||
                !clients.Add(clientId))
            {
                return false;
            }
        }

        HashSet<int> indices = new HashSet<int>();
        HashSet<string> characterIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < snapshot.Slots.Count; i++)
        {
            LobbyPartySlotState slot = snapshot.Slots[i];

            if (slot == null ||
                slot.SlotIndex < 0 ||
                slot.SlotIndex >= LobbyPartyAuthorityState.SlotCount ||
                !indices.Add(slot.SlotIndex) ||
                !ContainsMember(currentMembers, slot.OwnerSteamId) ||
                slot.OwnerSteamId != ExpectedOwner(snapshot, slot.SlotIndex))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(slot.CharacterId) &&
                !characterIds.Add(slot.CharacterId))
            {
                return false;
            }
        }

        if (!ValidateViewedCharacters(snapshot, currentMembers))
            return false;

        return true;
    }

    private static bool ValidateViewedCharacters(
        LobbyPartySnapshot snapshot,
        IReadOnlyCollection<ulong> currentMembers)
    {
        if (snapshot.ViewedCharacters == null)
            return false;

        HashSet<ulong> viewedMembers = new HashSet<ulong>();
        HashSet<string> viewedCharacterIds =
            new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < snapshot.ViewedCharacters.Count; i++)
        {
            LobbyPartyMemberViewState view = snapshot.ViewedCharacters[i];

            if (view == null ||
                view.MemberSteamId == 0UL ||
                string.IsNullOrWhiteSpace(view.CharacterId) ||
                !ContainsMember(currentMembers, view.MemberSteamId) ||
                !viewedMembers.Add(view.MemberSteamId) ||
                !viewedCharacterIds.Add(view.CharacterId) ||
                IsCharacterAssignedToOtherMember(
                    snapshot,
                    view.CharacterId,
                    view.MemberSteamId))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCharacterAssignedToOtherMember(
        LobbyPartySnapshot snapshot,
        string characterId,
        ulong memberSteamId)
    {
        for (int i = 0; i < snapshot.Slots.Count; i++)
        {
            LobbyPartySlotState slot = snapshot.Slots[i];

            if (slot.OwnerSteamId != memberSteamId &&
                slot.CharacterId == characterId)
            {
                return true;
            }
        }

        return false;
    }

    private static ulong ExpectedOwner(LobbyPartySnapshot snapshot, int slotIndex)
    {
        int clientCount = snapshot.OrderedClientSteamIds.Count;

        if (clientCount == 0 || slotIndex == 0)
            return snapshot.HostSteamId;

        if (clientCount == 1)
            return slotIndex == 2
                ? snapshot.OrderedClientSteamIds[0]
                : snapshot.HostSteamId;

        return slotIndex == 1
            ? snapshot.OrderedClientSteamIds[0]
            : snapshot.OrderedClientSteamIds[1];
    }

    private static bool ContainsMember(
        IReadOnlyCollection<ulong> members,
        ulong steamId)
    {
        foreach (ulong memberId in members)
        {
            if (memberId == steamId)
                return true;
        }

        return false;
    }

    private static string ToText(ulong steamId)
    {
        return steamId.ToString(CultureInfo.InvariantCulture);
    }

    private static bool TryParseSteamId(string value, out ulong steamId)
    {
        return ulong.TryParse(
            value,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out steamId) &&
            steamId != 0UL;
    }
}
