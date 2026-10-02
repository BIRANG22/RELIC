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
    private const int RelicSlotCount = 7;
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
        battle.SelectedErosionDifficultyIds = CopyIds(lobby.SelectedErosionDifficultyIds);
        battle.ErosionPartyStartEffectsApplied = false;

        if (characters != null && lobby.CharacterLoadouts != null)
        {
            for (int i = 0; i < lobby.CharacterLoadouts.Count; i++)
            {
                LobbyCharacterLoadoutData loadout = lobby.CharacterLoadouts[i];
                string characterId = loadout?.CharacterId?.Trim();
                if (string.IsNullOrEmpty(characterId) || !characters.TryGet(characterId, out CharacterRuntimeData character))
                    continue;

                character.EquippedRelicIds = CopyArray(loadout.EquippedRelicIds, RelicSlotCount);
                character.EquippedSkillIds = CreateLobbyBattleStartSkillLoadout(character);

                // 저장된 로비 로드아웃에도 이전 탐사에서 얻은 기억이 남아 있을 수 있으므로
                // 다음 탐사 시작 직전에는 로비 기본 장착 기억만 남기도록 함께 정리합니다.
                loadout.EquippedSkillIds = CopyArray(character.EquippedSkillIds, SkillSlotCount);
            }
        }

        ClearTransferredLobbyState(lobby);

        return new LobbyBattleRuntimeTransferResult(true, string.Empty);
    }

    public static void ClearTransferredLobbyState(LobbyRuntimeData lobby)
    {
        if (lobby == null)
            return;

        CultureTankResearchService.Normalize(lobby);

        lobby.OwnedRelicIds ??= new List<string>();
        lobby.SkillInventoryIds ??= new List<string>();
        lobby.PendingCultureTankBattleStartEffects ??= new List<CultureTankBattleStartEffectRuntimeData>();

        lobby.OwnedRelicIds.Clear();
        lobby.SkillInventoryIds.Clear();
        lobby.PendingCultureTankBattleStartEffects.Clear();
        ClearLobbyEquippedRelics(lobby.CharacterLoadouts);
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

    private static string[] CreateLobbyBattleStartSkillLoadout(CharacterRuntimeData character)
    {
        var skills = new string[SkillSlotCount];
        if (character == null)
            return skills;

        // 로비에서 출발할 때 유지되는 기억은 캐릭터의 고유/기본 장착 기억뿐입니다.
        // EquippedSkillIds[2], [3] 및 탐사 중 교체된 [1] 값은 다음 탐사로 넘기지 않습니다.
        skills[0] = character.UniqueSkillId ?? string.Empty;
        skills[1] = character.AbilitySkillId ?? string.Empty;
        skills[2] = string.Empty;
        skills[3] = string.Empty;
        return skills;
    }

    private static string[] CopyArray(string[] source, int length)
    {
        var copy = new string[length];
        if (source != null)
            Array.Copy(source, copy, Math.Min(source.Length, length));
        return copy;
    }

    private static void ClearLobbyEquippedRelics(IReadOnlyList<LobbyCharacterLoadoutData> loadouts)
    {
        if (loadouts == null)
            return;

        for (int i = 0; i < loadouts.Count; i++)
        {
            LobbyCharacterLoadoutData loadout = loadouts[i];
            if (loadout == null)
                continue;

            loadout.EquippedRelicIds = new string[RelicSlotCount];
        }
    }
}
