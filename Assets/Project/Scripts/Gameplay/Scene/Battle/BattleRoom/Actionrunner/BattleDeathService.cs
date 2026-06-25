using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleDeathService
{
    private const float DefaultMonsterDeathDestroyDelay = 0.6f;

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

        if (CoroutineHost.Instance != null && Application.isPlaying)
        {
            CoroutineHost.Instance.StartCoroutine(HandleMonsterDeadRoutine(monster));
            return;
        }

        if (!TryBeginMonsterDeath(monster))
            return;

        RemoveDeadMonster(monster);
    }

    public IEnumerator HandleMonsterDeadRoutine(MonsterUnit monster)
    {
        if (!TryBeginMonsterDeath(monster))
            yield break;

        yield return new WaitForSeconds(GetMonsterDeathDestroyDelay(monster));

        RemoveDeadMonster(monster);
    }

    private bool TryBeginMonsterDeath(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return false;

        if (!monster.RuntimeData.IsDead)
            return false;

        if (monster.RuntimeData.IsDeathHandled)
            return false;

        monster.RuntimeData.IsDeathHandled = true;

        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();

        if (animator != null)
            animator.PlayDead();

        if (monster.RuntimeData.MonsterId == "Mon_01")
            SpawnBlobsFromMuck(monster);

        CollectMonsterReward(monster);

        if (roomLoader != null)
            roomLoader.UnregisterRuntimeMonster(monster);

        return true;
    }

    private void RemoveDeadMonster(MonsterUnit monster)
    {
        if (monster == null)
            return;

        monster.DestroyHUD();

        if (Application.isPlaying)
            Object.Destroy(monster.gameObject);
        else
            Object.DestroyImmediate(monster.gameObject);
    }

    private float GetMonsterDeathDestroyDelay(MonsterUnit monster)
    {
        if (monster == null)
            return DefaultMonsterDeathDestroyDelay;

        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();

        if (animator == null)
            return DefaultMonsterDeathDestroyDelay;

        return Mathf.Max(0f, animator.DeadAnimationDuration);
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
