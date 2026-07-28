using System;
using System.Globalization;
using UnityEngine;

public static class LobbyBattleStartSerialization
{
    private const int ProtocolVersion = 1;

    [Serializable]
    private sealed class CommandDto
    {
        public int version;
        public string requestId;
        public string hostSteamId;
        public long requiredSharedStateRevision;
        public string battleSessionId;
        public int battleSeed;
        public string chapterId;
        public string stageId;
    }

    public static string SerializeCommand(LobbyBattleStartCommand command)
    {
        if (command == null)
            return string.Empty;

        CommandDto dto = new()
        {
            version = ProtocolVersion,
            requestId = command.RequestId,
            hostSteamId = ToText(command.HostSteamId),
            requiredSharedStateRevision = command.RequiredSharedStateRevision,
            battleSessionId = command.BattleSessionId,
            battleSeed = command.BattleSeed,
            chapterId = command.ChapterId,
            stageId = command.StageId
        };

        return JsonUtility.ToJson(dto);
    }

    public static bool TryDeserializeCommand(
        string payload,
        out LobbyBattleStartCommand command)
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
                string.IsNullOrWhiteSpace(dto.battleSessionId) ||
                string.IsNullOrWhiteSpace(dto.chapterId) ||
                string.IsNullOrWhiteSpace(dto.stageId) ||
                dto.requiredSharedStateRevision < 0 ||
                !TryParseSteamId(dto.hostSteamId, out ulong hostSteamId))
            {
                return false;
            }

            command = new LobbyBattleStartCommand(
                dto.requestId,
                hostSteamId,
                dto.requiredSharedStateRevision,
                dto.battleSessionId,
                dto.battleSeed,
                dto.chapterId,
                dto.stageId);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
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
