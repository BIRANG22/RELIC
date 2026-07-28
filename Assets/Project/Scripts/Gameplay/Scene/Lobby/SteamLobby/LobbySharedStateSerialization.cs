using System;
using System.Globalization;
using Relic.Gameplay.Data;
using UnityEngine;

public static class LobbySharedStateSerialization
{
    private const int ProtocolVersion = 1;

    [Serializable]
    private sealed class SnapshotDto
    {
        public int version;
        public string hostSteamId;
        public long revision;
        public int trialSelectionMask;
        public LobbyRuntimeData lobby;
    }

    [Serializable]
    private sealed class CommandDto
    {
        public int version;
        public string requestId;
        public string requesterSteamId;
        public int commandType;
        public string characterId;
        public int slotIndex;
        public string itemId;
        public long knownRevision;
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
        public SnapshotDto snapshot;
    }

    public static string SerializeSnapshot(LobbySharedStateSnapshot snapshot)
    {
        if (snapshot == null)
            return string.Empty;

        SnapshotDto dto = ToDto(snapshot);
        return JsonUtility.ToJson(dto);
    }

    public static bool TryDeserializeSnapshot(
        string payload,
        out LobbySharedStateSnapshot snapshot)
    {
        snapshot = null;

        if (string.IsNullOrWhiteSpace(payload))
            return false;

        try
        {
            SnapshotDto dto = JsonUtility.FromJson<SnapshotDto>(payload);
            return TryDeserializeSnapshotDto(dto, out snapshot);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static string SerializeCommand(LobbySharedStateCommand command)
    {
        if (command == null)
            return string.Empty;

        CommandDto dto = new()
        {
            version = ProtocolVersion,
            requestId = command.RequestId,
            requesterSteamId = ToText(command.RequesterSteamId),
            commandType = (int)command.CommandType,
            characterId = command.CharacterId,
            slotIndex = command.SlotIndex,
            itemId = command.ItemId,
            knownRevision = command.KnownRevision
        };

        return JsonUtility.ToJson(dto);
    }

    public static bool TryDeserializeCommand(
        string payload,
        out LobbySharedStateCommand command)
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
                !TryParseSteamId(dto.requesterSteamId, out ulong requesterSteamId) ||
                !Enum.IsDefined(typeof(LobbySharedStateCommandType), dto.commandType))
            {
                return false;
            }

            command = new LobbySharedStateCommand(
                dto.requestId,
                requesterSteamId,
                (LobbySharedStateCommandType)dto.commandType,
                dto.characterId,
                dto.slotIndex,
                dto.itemId,
                dto.knownRevision);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static string SerializeCommandResponse(
        LobbySharedStateCommandResponse response)
    {
        if (response == null)
            return string.Empty;

        CommandResponseDto dto = new()
        {
            version = ProtocolVersion,
            requestId = response.RequestId,
            requesterSteamId = ToText(response.RequesterSteamId),
            accepted = response.Accepted,
            rejectReason = (int)response.RejectReason,
            resultRevision = response.ResultRevision,
            snapshot = ToDto(response.Snapshot)
        };

        return JsonUtility.ToJson(dto);
    }

    public static bool TryDeserializeCommandResponse(
        string payload,
        out LobbySharedStateCommandResponse response)
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
                !Enum.IsDefined(typeof(LobbySharedStateCommandRejectReason), dto.rejectReason))
            {
                return false;
            }

            LobbySharedStateSnapshot snapshot = null;
            if (dto.snapshot != null &&
                !TryDeserializeSnapshotDto(dto.snapshot, out snapshot))
            {
                return false;
            }

            response = new LobbySharedStateCommandResponse(
                dto.requestId,
                requesterSteamId,
                dto.accepted,
                (LobbySharedStateCommandRejectReason)dto.rejectReason,
                dto.resultRevision,
                snapshot);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static SnapshotDto ToDto(LobbySharedStateSnapshot snapshot)
    {
        if (snapshot == null)
            return null;

        return new SnapshotDto
        {
            version = ProtocolVersion,
            hostSteamId = ToText(snapshot.HostSteamId),
            revision = snapshot.Revision,
            trialSelectionMask = snapshot.TrialSelectionMask,
            lobby = LobbySharedStateRuntimeCopy.CopyLobbyRuntime(snapshot.Lobby)
        };
    }

    private static bool TryDeserializeSnapshotDto(
        SnapshotDto dto,
        out LobbySharedStateSnapshot snapshot)
    {
        snapshot = null;

        if (dto == null ||
            dto.version != ProtocolVersion ||
            dto.revision <= 0 ||
            dto.lobby == null ||
            !TryParseSteamId(dto.hostSteamId, out ulong hostSteamId))
        {
            return false;
        }

        snapshot = new LobbySharedStateSnapshot(
            hostSteamId,
            dto.revision,
            dto.trialSelectionMask,
            dto.lobby);
        return true;
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
