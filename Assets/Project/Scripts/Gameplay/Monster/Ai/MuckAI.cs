using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class MuckAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_01";
        private const string AttackSkillId = "S_Monster_02";
        private const string ResidueGridEffectId = "GR_Residue";

        private static readonly Vector2Int[] MoveDirections =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(-1, 1), new Vector2Int(1, 1),
            new Vector2Int(-1, -1), new Vector2Int(1, -1)
        };

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return AttackSkillId;
        }

        public override MonsterAIPlan CreatePlan(MonsterUnit monsterUnit, BattleContext context, GridManager gridManager)
        {
            MonsterAIPlan plan = new();
            if (monsterUnit == null || monsterUnit.RuntimeData == null || gridManager == null)
                return plan;

            int targetGridIndex = FindNearestCharacterTargetGridIndex(monsterUnit, gridManager);
            if (targetGridIndex < 0)
                return plan;

            BattleGridEffectController gridEffects =
                UnityEngine.Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);
            Vector2Int moveOffset = GetApproachMove(monsterUnit, gridManager, targetGridIndex, gridEffects);
            const int group = 1;

            // Face the reserved target even when no movement is needed.
            // For movement, keep the movement direction as the facing priority.
            // When the target is directly above or below, retain the current facing.
            Vector2Int current = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int target = gridManager.IndexToCoord(targetGridIndex);
            int horizontal = moveOffset.x != 0 ? moveOffset.x : target.x - current.x;
            BattleDirection reservedDirection = monsterUnit.RuntimeData.Direction;
            bool hasReservedDirection = horizontal != 0;
            if (hasReservedDirection)
            {
                reservedDirection = horizontal > 0 ? BattleDirection.Right : BattleDirection.Left;
                SetFacingForReservation(monsterUnit, reservedDirection);
            }

            if (moveOffset != Vector2Int.zero)
            {
                plan.Add(new MonsterAIAction(
                    MoveSkillId, moveOffset, MonsterAISlotPreference.Front, group, 0));
            }

            // The projectile targets the nearest character's tile, regardless of attack range.
            // Keep the attack reserved even when no approach move is possible.
            plan.Add(new MonsterAIAction(
                AttackSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.SameSlot,
                group,
                1,
                hasForcedDirection: hasReservedDirection,
                forcedDirection: reservedDirection,
                explicitRangeGridIndices: new List<int> { targetGridIndex }));

            return plan;
        }

        private Vector2Int GetApproachMove(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            int targetGridIndex,
            BattleGridEffectController gridEffects)
        {
            if (monsterUnit.MainGridIndex < 0)
                return Vector2Int.zero;

            Vector2Int origin = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            BattleCharacter[] players = FindPlayers();
            List<Vector2Int> targets = new();
            foreach (BattleCharacter player in players)
            {
                if (IsAlivePlayer(player) && player.CurrentGridIndex >= 0)
                    targets.Add(gridManager.IndexToCoord(player.CurrentGridIndex));
            }

            if (targets.Count == 0)
                return Vector2Int.zero;

            // 이미 공격 가능한 거리라면 움직이지 않습니다.
            foreach (Vector2Int target in targets)
            {
                if (Mathf.Max(Mathf.Abs(target.x - origin.x), Mathf.Abs(target.y - origin.y)) <= 1)
                    return Vector2Int.zero;
            }

            // 점유된 칸을 피하는 최단 경로를 탐색합니다. 즉시 가까워지지 않는
            // 첫 걸음도 우회 경로의 일부라면 유효하게 취급합니다.
            Queue<Vector2Int> queue = new();
            Dictionary<Vector2Int, int> distances = new();
            Dictionary<Vector2Int, Vector2Int> firstSteps = new();
            queue.Enqueue(Vector2Int.zero);
            distances[Vector2Int.zero] = 0;
            firstSteps[Vector2Int.zero] = Vector2Int.zero;

            int bestPathLength = int.MaxValue;
            int bestRisk = int.MaxValue;
            List<Vector2Int> bestSteps = new();
            int bestReachableDistance = int.MaxValue;
            int bestReachablePathLength = int.MaxValue;
            int bestReachableRisk = int.MaxValue;
            List<Vector2Int> fallbackSteps = new();

            while (queue.Count > 0)
            {
                Vector2Int offset = queue.Dequeue();
                int length = distances[offset];
                Vector2Int position = origin + offset;
                int nearest = int.MaxValue;
                foreach (Vector2Int target in targets)
                {
                    int distance = Mathf.Max(Mathf.Abs(target.x - position.x),
                                             Mathf.Abs(target.y - position.y));
                    nearest = Mathf.Min(nearest, distance);
                }

                if (offset != Vector2Int.zero)
                {
                    Vector2Int step = firstSteps[offset];
                    int firstIndex = gridManager.CoordToIndex(origin + step);
                    int risk = IsRiskyDestination(firstIndex, gridEffects) ? 1 : 0;
                    if (nearest <= 1)
                    {
                        if (length < bestPathLength ||
                            (length == bestPathLength && risk < bestRisk))
                        {
                            bestPathLength = length;
                            bestRisk = risk;
                            bestSteps.Clear();
                        }
                        if (length == bestPathLength && risk == bestRisk &&
                            !bestSteps.Contains(step))
                            bestSteps.Add(step);
                    }
                    else if (nearest < bestReachableDistance ||
                             (nearest == bestReachableDistance && length < bestReachablePathLength) ||
                             (nearest == bestReachableDistance && length == bestReachablePathLength && risk < bestReachableRisk))
                    {
                        bestReachableDistance = nearest;
                        bestReachablePathLength = length;
                        bestReachableRisk = risk;
                        fallbackSteps.Clear();
                        fallbackSteps.Add(step);
                    }
                    else if (nearest == bestReachableDistance &&
                             length == bestReachablePathLength && risk == bestReachableRisk &&
                             !fallbackSteps.Contains(step))
                    {
                        fallbackSteps.Add(step);
                    }
                }

                if (length >= bestPathLength)
                    continue;

                foreach (Vector2Int direction in MoveDirections)
                {
                    Vector2Int next = offset + direction;
                    if (distances.ContainsKey(next) ||
                        (offset == Vector2Int.zero && !CanMonsterMove(monsterUnit, gridManager, direction)) ||
                        !CanOccupyAtOffset(monsterUnit, gridManager, next, gridEffects))
                        continue;

                    // 매 이동은 한 칸이므로, 지나가는 첫 칸도 이동 가능해야 합니다.
                    Vector2Int step = offset == Vector2Int.zero ? direction : firstSteps[offset];
                    distances[next] = length + 1;
                    firstSteps[next] = step;
                    queue.Enqueue(next);
                }
            }

            List<Vector2Int> choices = bestSteps.Count > 0 ? bestSteps : fallbackSteps;
            return choices.Count == 0 ? Vector2Int.zero :
                choices[BattleRandom.Range(0, choices.Count)];
        }

        private static bool CanOccupyAtOffset(
            MonsterUnit monsterUnit, GridManager gridManager,
            Vector2Int offset, BattleGridEffectController gridEffects)
        {
            foreach (int occupiedIndex in monsterUnit.OccupiedGridIndices)
            {
                Vector2Int coord = gridManager.IndexToCoord(occupiedIndex) + offset;
                if (!gridManager.IsValidCoord(coord))
                    return false;

                int index = gridManager.CoordToIndex(coord);
                if (BattleOccupancyService.IsOccupiedByAnyUnit(index, null, monsterUnit) ||
                    (gridEffects != null && gridEffects.IsBlocked(index)))
                    return false;
            }
            return true;
        }

        private static void SetFacingForReservation(MonsterUnit monsterUnit, BattleDirection direction)
        {
            if (monsterUnit == null)
                return;

            BattleUnitFacing facing = monsterUnit.GetComponentInChildren<BattleUnitFacing>(true);
            if (facing != null)
                facing.FaceRight(direction == BattleDirection.Right);

            if (monsterUnit.RuntimeData != null)
                monsterUnit.RuntimeData.Direction = direction;
        }

        private static bool IsRiskyDestination(int gridIndex, BattleGridEffectController controller)
        {
            if (gridIndex < 0 || controller == null ||
                !controller.State.TryGetEffectId(gridIndex, out string effectId) ||
                string.IsNullOrWhiteSpace(effectId))
                return false;

            return !string.Equals(effectId, ResidueGridEffectId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
