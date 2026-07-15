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
        HandleMonsterDead(monster, true);
    }

    /// <summary>
    /// 일반 처치 보상을 지급하지 않고 몬스터의 사망 연출과 제거만 처리합니다.
    /// 신더의 자폭처럼 플레이어가 처치한 것으로 취급하지 않는 경우에 사용합니다.
    /// </summary>
    public void HandleMonsterDeadWithoutReward(MonsterUnit monster)
    {
        HandleMonsterDead(monster, false);
    }

    /// <summary>
    /// 머크를 일반 사망 보상 없이 제거하고 블롭 2마리로 분열시킵니다.
    /// </summary>
    public void HandleMuckSplit(MonsterUnit muck)
    {
        if (muck == null || muck.RuntimeData == null)
            return;

        if (muck.RuntimeData.IsDeathHandled)
            return;

        muck.RuntimeData.CurrentHP = 0;
        muck.RuntimeData.IsDeathHandled = true;

        SpawnBlobsFromMuck(muck);

        if (roomLoader != null)
            roomLoader.UnregisterRuntimeMonster(muck);

        RemoveDeadMonster(muck);
    }

    private void HandleMonsterDead(MonsterUnit monster, bool collectReward)
    {
        if (monster == null || monster.RuntimeData == null)
            return;

        if (CoroutineHost.Instance != null && Application.isPlaying)
        {
            CoroutineHost.Instance.StartCoroutine(
                HandleMonsterDeadRoutine(monster, collectReward)
            );
            return;
        }

        if (!TryBeginMonsterDeath(monster, collectReward))
            return;

        RemoveDeadMonster(monster);
    }

    public IEnumerator HandleMonsterDeadRoutine(MonsterUnit monster)
    {
        yield return HandleMonsterDeadRoutine(monster, true);
    }

    private IEnumerator HandleMonsterDeadRoutine(MonsterUnit monster, bool collectReward)
    {
        if (!TryBeginMonsterDeath(monster, collectReward))
            yield break;

        yield return new WaitForSeconds(GetMonsterDeathDestroyDelay(monster));

        RemoveDeadMonster(monster);
    }

    private bool TryBeginMonsterDeath(MonsterUnit monster, bool collectReward)
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

        if (collectReward)
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

        if (monster == null || gridManager == null || count <= 0)
            return result;

        Vector2Int[] priorityOffsets =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
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

            for (int i = 0; i < priorityOffsets.Length; i++)
            {
                TryAddEmptyCell(originCoord + priorityOffsets[i], monster, result);

                if (result.Count >= count)
                    return result;
            }
        }

        // 인접 칸이 모두 막혔으면 머크에서 가까운 빈 그리드부터 찾습니다.
        List<int> fallbackCandidates = new();

        for (int gridIndex = 0; gridIndex < gridManager.Width * gridManager.Height; gridIndex++)
        {
            if (result.Contains(gridIndex) || monster.ContainsGridIndex(gridIndex))
                continue;

            if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex, null, monster))
                continue;

            fallbackCandidates.Add(gridIndex);
        }

        fallbackCandidates.Sort((a, b) =>
        {
            int distanceA = GetNearestMonsterCellDistance(monster, a);
            int distanceB = GetNearestMonsterCellDistance(monster, b);
            int compare = distanceA.CompareTo(distanceB);
            return compare != 0 ? compare : a.CompareTo(b);
        });

        for (int i = 0; i < fallbackCandidates.Count && result.Count < count; i++)
            result.Add(fallbackCandidates[i]);

        return result;
    }

    private void TryAddEmptyCell(
        Vector2Int coord,
        MonsterUnit monster,
        List<int> result)
    {
        if (result == null || !gridManager.IsValidCoord(coord))
            return;

        int gridIndex = gridManager.CoordToIndex(coord);

        if (result.Contains(gridIndex) || monster.ContainsGridIndex(gridIndex))
            return;

        if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex, null, monster))
            return;

        result.Add(gridIndex);
    }

    private int GetNearestMonsterCellDistance(MonsterUnit monster, int gridIndex)
    {
        if (monster == null || gridIndex < 0)
            return int.MaxValue;

        Vector2Int targetCoord = gridManager.IndexToCoord(gridIndex);
        int nearestDistance = int.MaxValue;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            int originIndex = monster.OccupiedGridIndices[i];

            if (originIndex < 0)
                continue;

            Vector2Int originCoord = gridManager.IndexToCoord(originIndex);
            int distance = Mathf.Abs(targetCoord.x - originCoord.x) +
                           Mathf.Abs(targetCoord.y - originCoord.y);
            nearestDistance = Mathf.Min(nearestDistance, distance);
        }

        return nearestDistance;
    }
}
