using Relic.Gameplay.Monster;
using UnityEngine;

public static class BattleOccupancyService
{
    public static bool IsOccupiedByAnyUnit(
        int gridIndex,
        string selfCharacterId = null,
        MonsterUnit ignoreMonster = null)
    {
        if (IsOccupiedByCharacter(gridIndex, selfCharacterId))
            return true;

        if (IsOccupiedByMonster(gridIndex, ignoreMonster))
            return true;

        return false;
    }

    public static bool IsOccupiedByCharacter(int gridIndex, string selfCharacterId = null)
    {
        BattleCharacter[] characters =
            Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null)
                continue;

            if (!string.IsNullOrWhiteSpace(selfCharacterId) &&
                character.CharacterId == selfCharacterId)
                continue;

            if (character.CurrentGridIndex == gridIndex)
                return true;
        }

        return false;
    }

    public static bool IsOccupiedByMonster(int gridIndex, MonsterUnit ignoreMonster = null)
    {
        MonsterUnit[] monsters =
            Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null)
                continue;

            if (monster.RuntimeData != null && monster.RuntimeData.IsDead)
                continue;

            if (ignoreMonster != null && monster == ignoreMonster)
                continue;

            if (monster.ContainsGridIndex(gridIndex))
                return true;
        }

        return false;
    }
}
