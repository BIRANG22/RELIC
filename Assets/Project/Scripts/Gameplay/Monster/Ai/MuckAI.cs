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

            Vector2Int current = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int target = gridManager.IndexToCoord(targetGridIndex);
            int currentDistance = Mathf.Max(Mathf.Abs(target.x - current.x), Mathf.Abs(target.y - current.y));

            // Do not approach further once adjacent: Muck is not trying to collide.
            if (currentDistance <= 1)
                return Vector2Int.zero;

            Vector2Int best = Vector2Int.zero;
            int bestRisk = int.MaxValue;
            int bestDistance = currentDistance;
            int bestManhattan = int.MaxValue;

            foreach (Vector2Int offset in MoveDirections)
            {
                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                int destination = GetProjectedMainGridIndex(monsterUnit, gridManager, offset);
                if (destination < 0)
                    continue;

                Vector2Int next = current + offset;
                int distance = Mathf.Max(Mathf.Abs(target.x - next.x), Mathf.Abs(target.y - next.y));
                if (distance >= currentDistance)
                    continue;

                int risk = IsRiskyDestination(destination, gridEffects) ? 1 : 0;
                int manhattan = Mathf.Abs(target.x - next.x) + Mathf.Abs(target.y - next.y);
                if (risk > bestRisk || (risk == bestRisk && distance > bestDistance) ||
                    (risk == bestRisk && distance == bestDistance && manhattan >= bestManhattan))
                    continue;

                best = offset;
                bestRisk = risk;
                bestDistance = distance;
                bestManhattan = manhattan;
            }

            return best;
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
