using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class DiscordPresenceSnapshotBuilder
{
    public static DiscordPresenceSnapshot Build(
        string sceneName,
        MapRuntimeData map,
        PartyRuntimeStore party,
        CharacterDatabase characters,
        long startUnixSeconds)
    {
        string details = BuildDetails(sceneName, map);
        string state = BuildState(sceneName, party, characters);
        return new DiscordPresenceSnapshot(details, state, startUnixSeconds);
    }

    private static string BuildDetails(string sceneName, MapRuntimeData map)
    {
        if (string.Equals(sceneName, "Title", StringComparison.OrdinalIgnoreCase))
            return "메인 메뉴";

        if (string.Equals(sceneName, "Lobby", StringComparison.OrdinalIgnoreCase))
            return "로비";

        if (map != null && map.IsRunInitialized)
        {
            string chapter = FirstValue(map.SelectedChapterId, "탐험 중");
            string location = FirstValue(map.CurrentStage, map.CurrentMapId);
            return string.IsNullOrWhiteSpace(location) ? chapter : $"{chapter} · {location}";
        }

        return string.IsNullOrWhiteSpace(sceneName) ? "RELIC 플레이 중" : sceneName.Trim();
    }

    private static string BuildState(
        string sceneName,
        PartyRuntimeStore party,
        CharacterDatabase characters)
    {
        if (string.Equals(sceneName, "Title", StringComparison.OrdinalIgnoreCase))
            return "모험 준비 중";

        List<string> names = new();

        if (party != null)
        {
            for (int i = 0; i < party.MaxPartyCountValue; i++)
            {
                string characterId = party.GetCharacterId(i);
                if (string.IsNullOrWhiteSpace(characterId))
                    continue;

                names.Add(ResolveCharacterName(characterId.Trim(), characters));
            }
        }

        return names.Count == 0
            ? "파티 편성 중"
            : $"캐릭터: {string.Join(", ", names)}";
    }

    private static string ResolveCharacterName(string characterId, CharacterDatabase characters)
    {
        if (characters != null &&
            characters.TryGet(characterId, out CharacterMasterData master) &&
            master != null &&
            !string.IsNullOrWhiteSpace(master.Name))
        {
            return master.Name.Trim();
        }

        return characterId;
    }

    private static string FirstValue(params string[] values)
    {
        if (values == null)
            return string.Empty;

        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i].Trim();
        }

        return string.Empty;
    }
}
