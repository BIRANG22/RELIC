using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class EliseAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_20";
        private const string MeleeAttackSkillId = "S_Monster_21";
        private const string PositionPressureSkillId = "S_Monster_22";
        private const string SpawnEggSkillId = "S_Monster_23";
        private const string SpawnWebSkillId = "S_Monster_24";
        private const string BarrierSkillId = "S_Monster_25";

        private const string BarrierEffectId = "E_Barrier";
        private const string SpiderEggGridEffectId = "GR_spider_egg";

        private const int PreferredMinDistance = 3;
        private const int PreferredMaxDistance = 4;
        private const int MaxSpiderEggCount = 2;

        // Range_19: ÁÖº¯ 8Ä­ + »óÇÏÁÂ¿ì 2Ä­±îÁö ÀÌµ¿ °¡´É
        private static readonly Vector2Int[] MoveOffsets =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down,

            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(1, 1),

            Vector2Int.left * 2,
            Vector2Int.right * 2,
            Vector2Int.up * 2,
            Vector2Int.down * 2
        };

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return MeleeAttackSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            if (monsterUnit == null || monsterUnit.RuntimeData == null || gridManager == null)
                return plan;

            MonsterRuntimeData runtime = monsterUnit.RuntimeData;
            BattleCharacter[] players = FindPlayers();
            int priority = 0;

            if (!HasStatus(runtime, BarrierEffectId))
            {
                plan.Add(new MonsterAIAction(
                    BarrierSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Front,
                    -1,
                    priority++));
            }

            bool hasPlayerInMeleeRange = HasPlayerInMeleeRange(monsterUnit, players, gridManager);
            Vector2Int moveOffset = ResolveRangeKeepingMove(monsterUnit, players, gridManager);

            if (hasPlayerInMeleeRange)
            {
                plan.Add(new MonsterAIAction(
                    MeleeAttackSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Front,
                    -1,
                    priority++));
            }

            if (moveOffset != Vector2Int.zero)
            {
                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    moveOffset,
                    hasPlayerInMeleeRange ? MonsterAISlotPreference.NextSlot : MonsterAISlotPreference.Front,
                    -1,
                    priority++));
            }

            AddSpiderSpawnAction(plan, monsterUnit, players, gridManager, runtime.TurnCount + 1, priority++);
            AddPositionPressureActions(plan, players, priority);

            return plan;
        }

        private void AddSpiderSpawnAction(
            MonsterAIPlan plan,
            MonsterUnit monsterUnit,
            BattleCharacter[] players,
            GridManager gridManager,
            int turn,
            int priority)
        {
            int eggGridIndex = CountGridEffects(SpiderEggGridEffectId) < MaxSpiderEggCount
                ? FindBestSpawnGridNearPlayers(monsterUnit, players, gridManager)
                : -1;
            int webGridIndex = FindBestSpawnGridNearPlayers(monsterUnit, players, gridManager, eggGridIndex);

            bool preferEgg = eggGridIndex >= 0 && (turn % 2 == 1 || webGridIndex < 0);

            if (preferEgg)
            {
                plan.Add(new MonsterAIAction(
                    SpawnEggSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Back,
                    -1,
                    priority,
                    eggGridIndex));
                return;
            }

            if (webGridIndex >= 0)
            {
                plan.Add(new MonsterAIAction(
                    SpawnWebSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Back,
                    -1,
                    priority,
                    webGridIndex));
            }
        }

        private void AddPositionPressureActions(
            MonsterAIPlan plan,
            BattleCharacter[] players,
            int basePriority)
        {
            if (players == null)
                return;

            List<int> targetGridIndices = new();

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                if (!targetGridIndices.Contains(player.CurrentGridIndex))
                    targetGridIndices.Add(player.CurrentGridIndex);
            }

            if (targetGridIndices.Count <= 0)
                return;

            plan.Add(new MonsterAIAction(
                PositionPressureSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Last,
                -1,
                basePriority,
                targetGridIndices[0],
                explicitRangeGridIndices: targetGridIndices));
        }

        private Vector2Int ResolveRangeKeepingMove(
            MonsterUnit monsterUnit,
            BattleCharacter[] players,
            GridManager gridManager)
        {
            if (monsterUnit.MainGridIndex < 0)
                return Vector2Int.zero;

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            int currentDistance = GetNearestPlayerDistance(currentCoord, players, gridManager);

            if (currentDistance >= PreferredMinDistance && currentDistance <= PreferredMaxDistance)
                return Vector2Int.zero;

            Vector2Int bestOffset = Vector2Int.zero;
            int bestScore = int.MaxValue;

            for (int i = 0; i < MoveOffsets.Length; i++)
            {
                Vector2Int offset = MoveOffsets[i];

                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                Vector2Int movedCoord = currentCoord + offset;
                int distance = GetNearestPlayerDistance(movedCoord, players, gridManager);
                int rangePenalty = GetPreferredRangePenalty(distance);
                int directionalPenalty = currentDistance < PreferredMinDistance
                    ? Mathf.Max(0, PreferredMinDistance - distance)
                    : Mathf.Max(0, distance - PreferredMaxDistance);
                int score = rangePenalty * 100 + directionalPenalty * 10 + Mathf.Abs(distance - PreferredMinDistance);

                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestOffset = offset;
            }

            return bestOffset;
        }

        private static int GetPreferredRangePenalty(int distance)
        {
            if (distance < PreferredMinDistance)
                return PreferredMinDistance - distance;

            if (distance > PreferredMaxDistance)
                return distance - PreferredMaxDistance;

            return 0;
        }

        private bool HasPlayerInMeleeRange(
            MonsterUnit monsterUnit,
            BattleCharacter[] players,
            GridManager gridManager)
        {
            if (monsterUnit.MainGridIndex < 0 || players == null)
                return false;

            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);

                if (Mathf.Max(
                        Mathf.Abs(playerCoord.x - monsterCoord.x),
                        Mathf.Abs(playerCoord.y - monsterCoord.y)) <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        private int FindBestSpawnGridNearPlayers(
            MonsterUnit monsterUnit,
            BattleCharacter[] players,
            GridManager gridManager,
            int excludedGridIndex = -1)
        {
            if (players == null || gridManager == null)
                return -1;

            int bestGridIndex = -1;
            int bestScore = int.MaxValue;
            int cellCount = gridManager.Width * gridManager.Height;
            Vector2Int monsterCoord = monsterUnit.MainGridIndex >= 0
                ? gridManager.IndexToCoord(monsterUnit.MainGridIndex)
                : Vector2Int.zero;

            for (int gridIndex = 0; gridIndex < cellCount; gridIndex++)
            {
                if (gridIndex == excludedGridIndex)
                    continue;

                if (!IsSpawnGridAvailable(gridIndex, gridManager))
                    continue;

                Vector2Int coord = gridManager.IndexToCoord(gridIndex);
                int nearestPlayerDistance = GetNearestPlayerDistance(coord, players, gridManager);

                if (nearestPlayerDistance == int.MaxValue)
                    continue;

                int distanceFromMonster =
                    Mathf.Abs(coord.x - monsterCoord.x) +
                    Mathf.Abs(coord.y - monsterCoord.y);
                int score = Mathf.Abs(nearestPlayerDistance - 1) * 100 +
                            nearestPlayerDistance * 10 +
                            distanceFromMonster;

                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestGridIndex = gridIndex;
            }

            return bestGridIndex;
        }

        private bool IsSpawnGridAvailable(int gridIndex, GridManager gridManager)
        {
            if (gridIndex < 0 || gridManager.GetCellByIndex(gridIndex) == null)
                return false;

            if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex))
                return false;

            BattleGridEffectController controller =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            if (controller != null && controller.HasEffect(gridIndex))
                return false;

            return true;
        }

        private int GetNearestPlayerDistance(
            Vector2Int originCoord,
            BattleCharacter[] players,
            GridManager gridManager)
        {
            if (players == null)
                return int.MaxValue;

            int nearestDistance = int.MaxValue;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                int distance =
                    Mathf.Abs(playerCoord.x - originCoord.x) +
                    Mathf.Abs(playerCoord.y - originCoord.y);

                if (distance < nearestDistance)
                    nearestDistance = distance;
            }

            return nearestDistance;
        }

        private static bool HasStatus(MonsterRuntimeData runtime, string effectId)
        {
            if (runtime == null || runtime.StatusEffects == null || string.IsNullOrWhiteSpace(effectId))
                return false;

            for (int i = 0; i < runtime.StatusEffects.Count; i++)
            {
                StatusEffectRuntimeData status = runtime.StatusEffects[i];

                if (status == null || status.EffectId != effectId)
                    continue;

                if (status.Stack > 0)
                    return true;
            }

            return false;
        }

        private static int CountGridEffects(string gridEffectId)
        {
            if (string.IsNullOrWhiteSpace(gridEffectId))
                return 0;

            BattleGridEffectController controller =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            if (controller == null)
                return 0;

            int count = 0;
            IReadOnlyList<BattleGridEffectPlacement> placements = controller.State.GetPlacements();

            for (int i = 0; i < placements.Count; i++)
            {
                if (placements[i].GridEffectId == gridEffectId)
                    count++;
            }

            return count;
        }
    }

    public static class EliseSlotLockService
    {
        public const string EliseMonsterId = "Mon_12";

        public static int RollLockedSlotIndex(
            IReadOnlyList<MonsterUnit> monsterUnits,
            int slotCount)
        {
            if (slotCount <= 0)
                return -1;

            if (!HasAliveElise(monsterUnits))
                return -1;

            return BattleRandom.Range(0, slotCount);
        }

        private static bool HasAliveElise(IReadOnlyList<MonsterUnit> monsterUnits)
        {
            if (monsterUnits == null)
                return false;

            for (int i = 0; i < monsterUnits.Count; i++)
            {
                MonsterUnit monsterUnit = monsterUnits[i];
                MonsterRuntimeData runtime = monsterUnit != null ? monsterUnit.RuntimeData : null;

                if (runtime == null || runtime.IsDead)
                    continue;

                if (runtime.MonsterId == EliseMonsterId)
                    return true;
            }

            return false;
        }
    }
}
