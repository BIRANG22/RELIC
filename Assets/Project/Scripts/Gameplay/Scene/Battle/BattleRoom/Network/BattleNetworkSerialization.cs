using System;
using System.Globalization;
using UnityEngine;

public static class BattleNetworkSerialization
{
    public const int ProtocolVersion = 1;

    public static string SerializeSnapshot(BattleNetworkSnapshot snapshot)
    {
        return snapshot == null ? string.Empty : JsonUtility.ToJson(snapshot);
    }

    public static bool TryDeserializeSnapshot(string payload, out BattleNetworkSnapshot snapshot)
    {
        snapshot = null;

        if (string.IsNullOrWhiteSpace(payload))
            return false;

        try
        {
            BattleNetworkSnapshot dto = JsonUtility.FromJson<BattleNetworkSnapshot>(payload);

            if (dto == null ||
                dto.version != ProtocolVersion ||
                dto.revision <= 0 ||
                !TryParseSteamId(dto.hostSteamId, out _))
            {
                return false;
            }

            snapshot = dto;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static string SerializeCommand(BattleNetworkCommand command)
    {
        return command == null ? string.Empty : JsonUtility.ToJson(command);
    }

    public static bool TryDeserializeCommand(string payload, out BattleNetworkCommand command)
    {
        command = null;

        if (string.IsNullOrWhiteSpace(payload))
            return false;

        try
        {
            BattleNetworkCommand dto = JsonUtility.FromJson<BattleNetworkCommand>(payload);

            if (dto == null ||
                dto.version != ProtocolVersion ||
                string.IsNullOrWhiteSpace(dto.requestId) ||
                !TryParseSteamId(dto.requesterSteamId, out _) ||
                !Enum.IsDefined(typeof(BattleNetworkCommandType), dto.commandType))
            {
                return false;
            }

            command = dto;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static string SerializeCommandResponse(BattleNetworkCommandResponse response)
    {
        return response == null ? string.Empty : JsonUtility.ToJson(response);
    }

    public static string SerializeExecution(BattleNetworkExecutionSnapshot snapshot)
    {
        return snapshot == null ? string.Empty : JsonUtility.ToJson(snapshot);
    }

    public static bool TryDeserializeExecution(
        string payload,
        out BattleNetworkExecutionSnapshot snapshot)
    {
        snapshot = null;

        if (string.IsNullOrWhiteSpace(payload))
            return false;

        try
        {
            BattleNetworkExecutionSnapshot dto =
                JsonUtility.FromJson<BattleNetworkExecutionSnapshot>(payload);

            if (dto == null ||
                dto.version != ProtocolVersion ||
                dto.revision <= 0 ||
                !TryParseSteamId(dto.hostSteamId, out _))
            {
                return false;
            }

            snapshot = dto;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool TryDeserializeCommandResponse(
        string payload,
        out BattleNetworkCommandResponse response)
    {
        response = null;

        if (string.IsNullOrWhiteSpace(payload))
            return false;

        try
        {
            BattleNetworkCommandResponse dto =
                JsonUtility.FromJson<BattleNetworkCommandResponse>(payload);

            if (dto == null ||
                dto.version != ProtocolVersion ||
                string.IsNullOrWhiteSpace(dto.requestId) ||
                !TryParseSteamId(dto.requesterSteamId, out _) ||
                !Enum.IsDefined(typeof(BattleNetworkRejectReason), dto.rejectReason))
            {
                return false;
            }

            response = dto;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static string ToText(ulong steamId)
    {
        return steamId.ToString(CultureInfo.InvariantCulture);
    }

    public static bool TryParseSteamId(string value, out ulong steamId)
    {
        return ulong.TryParse(
            value,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out steamId) &&
            steamId != 0UL;
    }
}
