using System.Collections.Generic;
using Relic.Gameplay.Monster;
using UnityEngine;

/// <summary>
/// 모르트의 사령술을 위해 드라우그/바로우의 사망 정보를 보관합니다.
/// 병사가 사망한 시점의 모르트 TurnCount를 기록하고, 모르트가 한 턴을 더 진행한 뒤 부활 가능 상태로 만듭니다.
/// </summary>
public static class MortNecromancyTracker
{
    private sealed class SoldierDeathRecord
    {
        public string MonsterId;
        public List<int> OccupiedGridIndices;
        public int ReadyMortTurnCount;
    }

    private static readonly Dictionary<string, List<SoldierDeathRecord>> recordsByMortRuntimeId = new();
    private static readonly HashSet<string> revivedSoldierRuntimeIds = new();

    public static void RecordSoldierDeath(MonsterUnit soldier, GridManager gridManager)
    {
        if (!IsNecromancySoldier(soldier) || gridManager == null)
            return;

        MonsterUnit mort = FindNearestLivingMort(soldier, gridManager);

        if (mort == null || mort.RuntimeData == null || string.IsNullOrWhiteSpace(mort.RuntimeData.RuntimeId))
            return;

        string mortRuntimeId = mort.RuntimeData.RuntimeId;

        if (!recordsByMortRuntimeId.TryGetValue(mortRuntimeId, out List<SoldierDeathRecord> records))
        {
            records = new List<SoldierDeathRecord>();
            recordsByMortRuntimeId.Add(mortRuntimeId, records);
        }

        records.Add(new SoldierDeathRecord
        {
            MonsterId = soldier.RuntimeData.MonsterId,
            OccupiedGridIndices = new List<int>(soldier.OccupiedGridIndices),
            ReadyMortTurnCount = mort.RuntimeData.TurnCount + 1
        });
    }

    public static bool HasReadySoldier(string mortRuntimeId, int mortTurnCount)
    {
        if (string.IsNullOrWhiteSpace(mortRuntimeId) ||
            !recordsByMortRuntimeId.TryGetValue(mortRuntimeId, out List<SoldierDeathRecord> records))
        {
            return false;
        }

        for (int i = 0; i < records.Count; i++)
        {
            if (records[i] != null && mortTurnCount >= records[i].ReadyMortTurnCount)
                return true;
        }

        return false;
    }

    public static bool TryRespawnReadySoldier(MonsterUnit mort, GridManager gridManager)
    {
        if (mort == null || mort.RuntimeData == null || mort.RuntimeData.IsDead || gridManager == null)
            return false;

        string mortRuntimeId = mort.RuntimeData.RuntimeId;

        if (string.IsNullOrWhiteSpace(mortRuntimeId) ||
            !recordsByMortRuntimeId.TryGetValue(mortRuntimeId, out List<SoldierDeathRecord> records))
        {
            return false;
        }

        int recordIndex = -1;

        for (int i = 0; i < records.Count; i++)
        {
            SoldierDeathRecord candidate = records[i];

            if (candidate == null || mort.RuntimeData.TurnCount < candidate.ReadyMortTurnCount)
                continue;

            recordIndex = i;
            break;
        }

        if (recordIndex < 0)
            return false;

        SoldierDeathRecord record = records[recordIndex];
        List<int> spawnCells = ResolveSpawnCells(record, mort, gridManager);

        if (spawnCells.Count <= 0)
            return false;

        BattleMonsterSpawner spawner = Object.FindFirstObjectByType<BattleMonsterSpawner>(FindObjectsInactive.Include);

        if (spawner == null)
            return false;

        SpawnedMonsterResult result = spawner.SpawnRuntimeMonster(record.MonsterId, spawnCells);

        if (result == null || result.RuntimeData == null)
            return false;

        // 사령술로 부활한 병사는 최대 체력의 30% 체력으로 돌아옵니다.
        // HUD/룸 등록 전에 체력을 먼저 적용해 처음부터 부활 체력으로 표시되게 합니다.
        result.RuntimeData.CurrentHP = Mathf.Max(
            1,
            Mathf.CeilToInt(result.RuntimeData.MaxHP * 0.3f));

        BattleRoomLoader roomLoader = Object.FindFirstObjectByType<BattleRoomLoader>(FindObjectsInactive.Include);

        if (roomLoader != null)
            roomLoader.RegisterRuntimeMonster(result);

        // 부활 개체는 재처치해도 보상을 지급하지 않도록 런타임 ID를 기록합니다.
        revivedSoldierRuntimeIds.Add(result.RuntimeData.RuntimeId);
        records.RemoveAt(recordIndex);

        if (records.Count == 0)
            recordsByMortRuntimeId.Remove(mortRuntimeId);

        return true;
    }

    public static bool IsRevivedSoldier(string runtimeId)
    {
        return !string.IsNullOrWhiteSpace(runtimeId) && revivedSoldierRuntimeIds.Contains(runtimeId);
    }

    public static void RemoveMort(string mortRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(mortRuntimeId))
            return;

        recordsByMortRuntimeId.Remove(mortRuntimeId);
    }

    private static bool IsNecromancySoldier(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return false;

        string monsterId = monster.RuntimeData.MonsterId;
        return monsterId == "Mon_07" || monsterId == "Mon_08";
    }

    private static MonsterUnit FindNearestLivingMort(MonsterUnit soldier, GridManager gridManager)
    {
        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        if (soldier == null || soldier.MainGridIndex < 0)
            return null;

        Vector2Int soldierCoord = gridManager.IndexToCoord(soldier.MainGridIndex);
        MonsterUnit nearest = null;
        int nearestDistance = int.MaxValue;

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit candidate = monsters[i];

            if (candidate == null || candidate.RuntimeData == null || candidate.RuntimeData.IsDead)
                continue;

            if (candidate.RuntimeData.MonsterId != "Mon_11" || candidate.MainGridIndex < 0)
                continue;

            Vector2Int mortCoord = gridManager.IndexToCoord(candidate.MainGridIndex);
            int distance = Mathf.Abs(mortCoord.x - soldierCoord.x) + Mathf.Abs(mortCoord.y - soldierCoord.y);

            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearest = candidate;
        }

        return nearest;
    }

    private static List<int> ResolveSpawnCells(
        SoldierDeathRecord record,
        MonsterUnit mort,
        GridManager gridManager)
    {
        List<int> result = new();

        if (record == null || gridManager == null)
            return result;

        if (record.OccupiedGridIndices != null && record.OccupiedGridIndices.Count > 0)
        {
            bool allFree = true;

            for (int i = 0; i < record.OccupiedGridIndices.Count; i++)
            {
                int gridIndex = record.OccupiedGridIndices[i];

                if (!IsSpawnableGrid(gridIndex, gridManager))
                {
                    allFree = false;
                    break;
                }
            }

            if (allFree)
            {
                result.AddRange(record.OccupiedGridIndices);
                return result;
            }
        }

        int originGridIndex = record.OccupiedGridIndices != null && record.OccupiedGridIndices.Count > 0
            ? record.OccupiedGridIndices[0]
            : mort.MainGridIndex;

        if (originGridIndex < 0)
            return result;

        Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
        int bestGridIndex = -1;
        int bestDistance = int.MaxValue;

        for (int gridIndex = 0; gridIndex < gridManager.Width * gridManager.Height; gridIndex++)
        {
            if (!IsSpawnableGrid(gridIndex, gridManager))
                continue;

            Vector2Int coord = gridManager.IndexToCoord(gridIndex);
            int distance = Mathf.Abs(coord.x - origin.x) + Mathf.Abs(coord.y - origin.y);

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            bestGridIndex = gridIndex;
        }

        if (bestGridIndex >= 0)
            result.Add(bestGridIndex);

        return result;
    }

    private static bool IsSpawnableGrid(int gridIndex, GridManager gridManager)
    {
        if (gridIndex < 0 || gridManager == null || gridManager.GetCellByIndex(gridIndex) == null)
            return false;

        if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex))
            return false;

        BattleGridEffectController gridEffectController =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        return gridEffectController == null || !gridEffectController.IsBlocked(gridIndex);
    }
}
