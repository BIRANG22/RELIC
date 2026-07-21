using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public readonly struct LobbyBattleRuntimeTransferResult
{
    public LobbyBattleRuntimeTransferResult(bool succeeded, string error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public bool Succeeded { get; }
    public string Error { get; }
}

public sealed class LobbyBattleRuntimeTransferService
{
    private const int RelicSlotCount = 5;
    private const int SkillSlotCount = 4;

    public LobbyBattleRuntimeTransferResult Transfer(
        LobbyRuntimeData lobby,
        BattleRuntimeData battle,
        CharacterRuntimeStore characters)
    {
        if (lobby == null)
            return new LobbyBattleRuntimeTransferResult(false, "Lobby runtime is missing.");

        if (battle == null)
            return new LobbyBattleRuntimeTransferResult(false, "Battle runtime is missing.");

        CultureTankResearchService.Normalize(lobby);

        battle.OwnedRelicIds = CopyIds(lobby.OwnedRelicIds);
        battle.SkillInventoryIds = CopyIds(lobby.SkillInventoryIds);
        battle.StartingSkillInventoryIds = CopyIds(lobby.SkillInventoryIds);
        battle.AcquiredSkillIds = new List<string>();
        battle.CharacterStatistics = new List<BattleRunCharacterStatisticsData>();
        battle.BagItemIds = new List<string>();
        battle.CultureTankBattleStartEffects =
            CultureTankResearchService.CopyPendingBattleStartEffects(lobby);
        lobby.PendingCultureTankBattleStartEffects.Clear();

        if (characters != null && lobby.CharacterLoadouts != null)
        {
            for (int i = 0; i < lobby.CharacterLoadouts.Count; i++)
            {
                LobbyCharacterLoadoutData loadout = lobby.CharacterLoadouts[i];
                string characterId = loadout?.CharacterId?.Trim();
                if (string.IsNullOrEmpty(characterId) || !characters.TryGet(characterId, out CharacterRuntimeData character))
                    continue;

                character.EquippedRelicIds = CopyArray(loadout.EquippedRelicIds, RelicSlotCount);
                character.EquippedSkillIds = CopyArray(loadout.EquippedSkillIds, SkillSlotCount);
            }
        }

        return new LobbyBattleRuntimeTransferResult(true, string.Empty);
    }

    private static List<string> CopyIds(IEnumerable<string> source)
    {
        var copy = new List<string>();
        if (source == null)
            return copy;

        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (string value in source)
        {
            string id = value?.Trim();
            if (!string.IsNullOrEmpty(id) && unique.Add(id))
                copy.Add(id);
        }

        return copy;
    }

    private static string[] CopyArray(string[] source, int length)
    {
        var copy = new string[length];
        if (source != null)
            Array.Copy(source, copy, Math.Min(source.Length, length));
        return copy;
    }
}
