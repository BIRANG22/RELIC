using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public static class MonsterSkillRangeService
{
    public static List<int> BuildRangeGridIndices(
        MonsterUnit monster,
        MonsterSkillData skillData,
        GridManager gridManager,
        bool facingRight)
    {
        List<int> result = new();

        if (monster == null || skillData == null || gridManager == null)
            return result;

        if (skillData.RangeId == "Range_All")
            return GetAllTargetGridIndices(skillData.Target);

        if (skillData.RangeId == "Range_X-axis")
            return GetXAxisRange(monster, gridManager, facingRight);

        SkillRangeData rangeData = DataManager.Instance.RangeDatabase.Get(skillData.RangeId);

        if (rangeData == null || rangeData.Positions == null)
            return result;

        int originIndex = monster.MainGridIndex;

        if (originIndex < 0)
            return result;

        Vector2Int origin = gridManager.IndexToCoord(originIndex);

        for (int i = 0; i < rangeData.Positions.Count; i++)
        {
            Vector2Int offset = rangeData.Positions[i];

            if (!facingRight)
                offset = new Vector2Int(-offset.x, offset.y);

            Vector2Int coord = origin + offset;

            if (!gridManager.IsValidCoord(coord))
                continue;

            result.Add(gridManager.CoordToIndex(coord));
        }

        return result;
    }

    public static List<int> FilterTargetGridIndices(
        MonsterSkillData skillData,
        List<int> rangeGridIndices)
    {
        if (skillData != null && skillData.RangeId == "Range_All")
            return new List<int>(rangeGridIndices);

        List<int> result = new();

        if (skillData == null || rangeGridIndices == null)
            return result;

        for (int i = 0; i < rangeGridIndices.Count; i++)
        {
            int gridIndex = rangeGridIndices[i];

            if (HasTargetOnGrid(skillData.Target, gridIndex))
                result.Add(gridIndex);
        }

        return result;
    }

    private static bool HasTargetOnGrid(TargetType target, int gridIndex)
    {
        if (target == TargetType.PlayerParty)
        {
            BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i] != null &&
                    characters[i].CurrentGridIndex == gridIndex)
                    return true;
            }

            return false;
        }

        if (target == TargetType.EnemyParty)
        {
            MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < monsters.Length; i++)
            {
                if (monsters[i] != null &&
                    monsters[i].ContainsGridIndex(gridIndex))
                    return true;
            }

            return false;
        }

        if (target == TargetType.Self)
            return true;

        return false;
    }

    private static List<int> GetAllTargetGridIndices(TargetType target)
    {
        List<int> result = new();

        if (target == TargetType.PlayerParty)
        {
            BattleCharacter[] players = Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null || players[i].RuntimeData == null)
                    continue;

                if (players[i].RuntimeData.CurrentHealth <= 0)
                    continue;

                if (players[i].CurrentGridIndex >= 0)
                    result.Add(players[i].CurrentGridIndex);
            }
        }
        else if (target == TargetType.EnemyParty)
        {
            MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < monsters.Length; i++)
            {
                MonsterUnit monster = monsters[i];

                if (monster == null || monster.RuntimeData == null)
                    continue;

                if (monster.RuntimeData.IsDead)
                    continue;

                for (int j = 0; j < monster.OccupiedGridIndices.Count; j++)
                    result.Add(monster.OccupiedGridIndices[j]);
            }
        }

        return result;
    }

    private static List<int> GetXAxisRange(
        MonsterUnit monster,
        GridManager gridManager,
        bool facingRight)
    {
        List<int> result = new();

        int originIndex = monster.MainGridIndex;

        if (originIndex < 0)
            return result;

        Vector2Int origin = gridManager.IndexToCoord(originIndex);
        int dir = facingRight ? 1 : -1;

        for (int x = origin.x + dir; ; x += dir)
        {
            Vector2Int coord = new Vector2Int(x, origin.y);

            if (!gridManager.IsValidCoord(coord))
                break;

            result.Add(gridManager.CoordToIndex(coord));
        }

        return result;
    }
}