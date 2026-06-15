using Relic.Gameplay.Monster;
using System.Collections.Generic;
using UnityEngine;

public class BattleDeathService
{
    private readonly GridManager gridManager;
    private readonly BattleMonsterSpawner monsterSpawner;
    private readonly BattleRoomLoader roomLoader;

    public BattleDeathService(
    GridManager gridManager,
    BattleMonsterSpawner monsterSpawner,
    BattleRoomLoader roomLoader)
    {
        this.gridManager = gridManager;
        this.monsterSpawner = monsterSpawner;
        this.roomLoader = roomLoader;
    }

    public void HandleMonsterDead(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return;

        if (monster.RuntimeData.IsDeathHandled)
            return;

        monster.RuntimeData.IsDeathHandled = true;

        if (monster.RuntimeData.MonsterId == "Mon_01")
            SpawnBlobsFromMuck(monster);

        CollectMonsterReward(monster);

        if (roomLoader != null)
            roomLoader.UnregisterRuntimeMonster(monster);

        monster.DestroyHUD();

        Object.Destroy(monster.gameObject);
    }

    private void CollectMonsterReward(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return;

        if (BattleRewardCollector.Instance == null)
            return;

        BattleRewardCollector.Instance.CollectMonsterReward(monster.RuntimeData);
    }

    private void SpawnBlobsFromMuck(MonsterUnit muck)
    {
        if (muck == null || monsterSpawner == null || gridManager == null)
            return;

        List<int> spawnCells = FindEmptyCellsAroundMonster(muck, 2);

        for (int i = 0; i < spawnCells.Count; i++)
        {
            SpawnedMonsterResult result = monsterSpawner.SpawnRuntimeMonster(
                "Mon_02",
                new List<int> { spawnCells[i] }
            );

            if (roomLoader != null)
                roomLoader.RegisterRuntimeMonster(result);
        }
    }

    private List<int> FindEmptyCellsAroundMonster(MonsterUnit monster, int count)
    {
        List<int> result = new();

        if (monster == null || gridManager == null)
            return result;

        Vector2Int[] offsets =
        {
        Vector2Int.left,
        Vector2Int.right,
        Vector2Int.up,
        Vector2Int.down,
        new Vector2Int(-1, 1),
        new Vector2Int(1, 1),
        new Vector2Int(-1, -1),
        new Vector2Int(1, -1)
    };

        for (int cellIndex = 0; cellIndex < monster.OccupiedGridIndices.Count; cellIndex++)
        {
            int originIndex = monster.OccupiedGridIndices[cellIndex];

            if (originIndex < 0)
                continue;

            Vector2Int originCoord = gridManager.IndexToCoord(originIndex);

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2Int coord = originCoord + offsets[i];

                if (!gridManager.IsValidCoord(coord))
                    continue;

                int gridIndex = gridManager.CoordToIndex(coord);

                if (result.Contains(gridIndex))
                    continue;

                if (monster.ContainsGridIndex(gridIndex))
                    continue;

                if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex, null, monster))
                    continue;

                result.Add(gridIndex);

                if (result.Count >= count)
                    return result;
            }
        }

        return result;
    }
}