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

    private const string SkullBoneGridEffectId = "GR_Skull_bone";

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

    /// <summary>
    /// 몬스터 행동 예약 전에 사령술 특성으로 준비된 병사를 부활시킵니다.
    /// 부활 몬스터는 룸 등록까지 마친 후 일반 몬스터와 함께 행동을 예약합니다.
    /// </summary>
    public static int RespawnAtTurnStart(System.Collections.Generic.List<MonsterUnit> monsters, GridManager gridManager)
    {
        if (monsters == null || gridManager == null)
            return 0;

        int revived = 0;
        // 부활 중 몬스터 목록에 개체가 추가되므로 최초 목록만 순회합니다.
        MonsterUnit[] snapshot = monsters.ToArray();
        foreach (MonsterUnit mort in snapshot)
        {
            if (mort == null || mort.RuntimeData == null || mort.RuntimeData.IsDead ||
                mort.RuntimeData.MonsterId != "Mon_11" || !HasNecromancyTrait(mort.RuntimeData))
                continue;

            // 준비된 유골이 여러 개라면 모두 처리합니다. 실패한 항목은 다음 턴에 재시도합니다.
            int attempts = recordsByMortRuntimeId.TryGetValue(mort.RuntimeData.RuntimeId, out var records)
                ? records.Count : 0;
            for (int i = 0; i < attempts; i++)
            {
                if (!HasReadySoldier(mort.RuntimeData.RuntimeId, mort.RuntimeData.TurnCount))
                    break;
                if (!TryRespawnReadySoldier(mort, gridManager))
                    break;
                revived++;
            }
        }
        return revived;
    }

    private static bool HasNecromancyTrait(Relic.Gameplay.Data.MonsterRuntimeData runtime)
    {
        if (runtime?.StatusEffects == null)
            return false;
        foreach (var status in runtime.StatusEffects)
            if (status != null && status.EffectId == "E_Necromancy")
                return true;
        return false;
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

        // 유골은 부활 위치를 막는 장애물이므로 소환 직전에 해당 유골만 제거합니다.
        // 소환이 실패하면 제거한 유골을 원래 위치에 복구합니다.
        BattleGridEffectController boneController =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);
        List<int> removedBoneCells = new();
        if (boneController != null)
        {
            for (int i = 0; i < spawnCells.Count; i++)
            {
                int cell = spawnCells[i];
                if (boneController.State.TryGetEffectId(cell, out string effectId) &&
                    effectId == SkullBoneGridEffectId && boneController.TryRemoveEffect(cell))
                    removedBoneCells.Add(cell);
            }
        }

        SpawnedMonsterResult result = spawner.SpawnRuntimeMonster(record.MonsterId, spawnCells);

        if (result == null || result.RuntimeData == null)
        {
            if (boneController != null)
                for (int i = 0; i < removedBoneCells.Count; i++)
                    boneController.TryPlaceEffect(removedBoneCells[i], SkullBoneGridEffectId);
            return false;
        }

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

                if (!IsSpawnableGrid(gridIndex, gridManager, true))
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

    private static bool IsSpawnableGrid(int gridIndex, GridManager gridManager, bool allowSkullBone = false)
    {
        if (gridIndex < 0 || gridManager == null || gridManager.GetCellByIndex(gridIndex) == null)
            return false;

        if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex))
            return false;

        BattleGridEffectController gridEffectController =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (gridEffectController == null || !gridEffectController.IsBlocked(gridIndex))
            return true;

        // 사령술은 유골이 있는 원래 칸에서 부활할 수 있습니다.
        return allowSkullBone &&
               gridEffectController.State.TryGetEffectId(gridIndex, out string effectId) &&
               effectId == SkullBoneGridEffectId;
    }
}
