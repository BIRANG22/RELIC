using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleUnitFinder
{
    public BattleCharacter FindBattleCharacter(string characterId)
    {
        BattleCharacter[] characters =
            Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] != null && characters[i].CharacterId == characterId)
                return characters[i];
        }

        return null;
    }

    public MonsterUnit FindMonsterUnit(string runtimeId)
    {
        MonsterUnit[] monsters =
            Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || monsters[i].RuntimeData == null)
                continue;

            if (monsters[i].RuntimeData.RuntimeId == runtimeId)
                return monsters[i];
        }

        return null;
    }
}