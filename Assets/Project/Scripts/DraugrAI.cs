using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 드라우그 AI
    /// - 행동 범위: MonsterMasterData의 AttackRange(Range_02)
    /// - 이동: 주변 8방향 중 가장 가까운 캐릭터에게 가까워지는 방향으로 1칸
    /// - 기본 공격: 전방 첫 열 3칸과 둘째 열 중앙 1칸 가로베기
    /// - 반격: BattleActionRunner에서 공격 스킬 피격 후 S_Monster_17을 즉시 실행
    /// </summary>
    public class DraugrAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_15";
        private const string AttackSkillId = "S_Monster_16";

        private static readonly Vector2Int[] MoveDirections =
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

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return AttackSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();
            if (monsterUnit == null || monsterUnit.RuntimeData == null || gridManager == null ||
                monsterUnit.MainGridIndex < 0)
                return plan;

            // A character directly beside this monster can be attacked without moving.
            // Do not stop for targets that merely fall inside the wider attack range.
            // Do this before considering any movement or another preferred target.
            int counterTarget = FindCounterTargetOnCurrentRow(monsterUnit.MainGridIndex, gridManager);
            if (counterTarget >= 0)
            {
                Vector2Int self = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
                Vector2Int other = gridManager.IndexToCoord(counterTarget);
                BattleDirection direction = other.x < self.x
                    ? BattleDirection.Left : BattleDirection.Right;
                SetFacingForReservedAttack(monsterUnit, direction);
                List<int> range = BuildSweepRange(
                    monsterUnit.MainGridIndex, gridManager,
                    direction == BattleDirection.Left ? -1 : 1);
                plan.Add(new MonsterAIAction(
                    AttackSkillId, Vector2Int.zero, MonsterAISlotPreference.Front,
                    1, 0, monsterUnit.MainGridIndex, false, direction, false, 0, range));
                return plan;
            }

            int targetIndex = FindPreferredTarget(monsterUnit.MainGridIndex, gridManager);
            if (targetIndex < 0)
                return plan;

            // Always reserve the sweep, even when the player is currently outside its range.
            // The player may enter the predicted area during the reservation phase.
            Vector2Int moveOffset = GetBestSafeOneTileMoveTowardTarget(
                monsterUnit, gridManager, targetIndex);
            int attackOrigin = monsterUnit.MainGridIndex;
            if (moveOffset != Vector2Int.zero)
            {
                attackOrigin = GetProjectedMainGridIndex(monsterUnit, gridManager, moveOffset);
                plan.Add(new MonsterAIAction(
                    MoveSkillId, moveOffset, MonsterAISlotPreference.Front, 1, 0));
            }

            // Facing is determined from the target, not from a successful hit test.
            Vector2Int originCoord = gridManager.IndexToCoord(attackOrigin);
            Vector2Int targetCoord = gridManager.IndexToCoord(targetIndex);
            BattleDirection attackDirection = targetCoord.x < originCoord.x
                ? BattleDirection.Left : targetCoord.x > originCoord.x
                    ? BattleDirection.Right
                    : GetCurrentFacing(monsterUnit);
            List<int> attackRange = BuildSweepRange(
                attackOrigin, gridManager,
                attackDirection == BattleDirection.Left ? -1 : 1);

            BattleDirection reservationDirection = attackDirection;
            if (moveOffset.x < 0)
                reservationDirection = BattleDirection.Left;
            else if (moveOffset.x > 0)
                reservationDirection = BattleDirection.Right;

            SetFacingForReservedAttack(monsterUnit, reservationDirection);

            plan.Add(new MonsterAIAction(
                AttackSkillId, Vector2Int.zero,
                moveOffset == Vector2Int.zero
                    ? MonsterAISlotPreference.Front
                    : MonsterAISlotPreference.SameSlot,
                1, moveOffset == Vector2Int.zero ? 0 : 1,
                attackOrigin, moveOffset != Vector2Int.zero,
                attackDirection, false, 0, attackRange));
            return plan;
        }

        private int FindCounterTargetOnCurrentRow(int originIndex, GridManager gridManager)
        {
            Vector2Int origin = gridManager.IndexToCoord(originIndex);
            int best = -1;
            int bestDistance = int.MaxValue;
            foreach (int index in FindCharacterTargetGridIndices())
            {
                if (index < 0 || gridManager.GetCellByIndex(index) == null)
                    continue;
                Vector2Int target = gridManager.IndexToCoord(index);
                int distance = Mathf.Abs(target.x - origin.x);
                if (target.y != origin.y || distance != 1)
                    continue;
                if (distance < bestDistance)
                {
                    best = index;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static BattleDirection GetCurrentFacing(MonsterUnit unit)
        {
            return unit != null && unit.RuntimeData != null &&
                   unit.RuntimeData.Direction == BattleDirection.Left
                ? BattleDirection.Left : BattleDirection.Right;
        }

        private int FindPreferredTarget(int originIndex, GridManager gridManager)
        {
            List<int> targets = FindCharacterTargetGridIndices();
            Vector2Int origin = gridManager.IndexToCoord(originIndex);
            int bestIndex = -1;
            int bestDistance = int.MaxValue;
            bool bestAttackable = false;
            int bestSecondaryDistance = int.MaxValue;

            foreach (int target in targets)
            {
                if (target < 0 || gridManager.GetCellByIndex(target) == null)
                    continue;
                Vector2Int coord = gridManager.IndexToCoord(target);
                int dx = Mathf.Abs(coord.x - origin.x);
                int dy = Mathf.Abs(coord.y - origin.y);
                int distance = Mathf.Max(dx, dy);
                bool attackable = TryBuildAttackForTarget(
                    originIndex, target, gridManager, out _, out _);
                int secondary = dx + dy;
                if (distance < bestDistance ||
                    (distance == bestDistance && attackable && !bestAttackable) ||
                    (distance == bestDistance && attackable == bestAttackable &&
                     secondary < bestSecondaryDistance))
                {
                    bestIndex = target;
                    bestDistance = distance;
                    bestAttackable = attackable;
                    bestSecondaryDistance = secondary;
                }
            }
            return bestIndex;
        }

        private Vector2Int GetBestSafeOneTileMoveTowardTarget(
            MonsterUnit monsterUnit, GridManager gridManager, int targetIndex)
        {
            Vector2Int origin = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int target = gridManager.IndexToCoord(targetIndex);
            int currentDistance = Mathf.Max(Mathf.Abs(target.x - origin.x),
                                            Mathf.Abs(target.y - origin.y));
            // Only remain stationary when a character is directly in front (one tile).
            // Being within the broader attack area must not stop the approach.
            foreach (int characterIndex in FindCharacterTargetGridIndices())
            {
                if (characterIndex < 0 || gridManager.GetCellByIndex(characterIndex) == null)
                    continue;
                Vector2Int character = gridManager.IndexToCoord(characterIndex);
                if (character.y == origin.y && Mathf.Abs(character.x - origin.x) == 1)
                    return Vector2Int.zero;
            }
            Vector2Int best = Vector2Int.zero;
            int bestSafety = int.MaxValue;
            int bestDistance = int.MaxValue;
            int bestSecondary = int.MaxValue;
            int bestRowOffset = int.MaxValue;
            BattleGridEffectController effects =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            foreach (Vector2Int offset in MoveDirections)
            {
                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;
                Vector2Int next = origin + offset;
                if (!gridManager.IsValidCoord(next))
                    continue;
                int nextIndex = gridManager.CoordToIndex(next);
                // Never plan a move into a character's tile, even when occupancy
                // objects have not yet been synchronized for this reservation.
                if (nextIndex == targetIndex ||
                    BattleOccupancyService.IsOccupiedByAnyUnit(nextIndex, null, monsterUnit))
                    continue;

                // Check every occupied cell of larger monsters at the projected destination.
                bool destinationBlocked = false;
                foreach (int occupiedIndex in monsterUnit.OccupiedGridIndices)
                {
                    Vector2Int occupiedCoord = gridManager.IndexToCoord(occupiedIndex) + offset;
                    if (!gridManager.IsValidCoord(occupiedCoord) ||
                        BattleOccupancyService.IsOccupiedByAnyUnit(
                            gridManager.CoordToIndex(occupiedCoord), null, monsterUnit))
                    {
                        destinationBlocked = true;
                        break;
                    }
                }
                if (destinationBlocked)
                    continue;

                int safety = effects != null && effects.HasEffect(nextIndex) ? 1 : 0;
                int distance = Mathf.Max(Mathf.Abs(target.x - next.x),
                                         Mathf.Abs(target.y - next.y));
                int secondary = Mathf.Abs(target.x - next.x) + Mathf.Abs(target.y - next.y);
                // Prefer lining up the counter with a character, then closing distance.
                // An aligned counter opportunity is preferred over sweep coverage.
                int rowOffset = Mathf.Abs(target.y - next.y);
                if (rowOffset < bestRowOffset ||
                    (rowOffset == bestRowOffset && distance < bestDistance) ||
                    (rowOffset == bestRowOffset && distance == bestDistance && secondary < bestSecondary) ||
                    (rowOffset == bestRowOffset && distance == bestDistance &&
                     secondary == bestSecondary && safety < bestSafety))
                {
                    bestRowOffset = rowOffset;
                    bestSafety = safety;
                    bestDistance = distance;
                    bestSecondary = secondary;
                    best = offset;
                }
            }
            // Do not move away purely for the sake of moving when already in reach.
            if (best != Vector2Int.zero && bestDistance > currentDistance &&
                bestRowOffset >= Mathf.Abs(target.y - origin.y))
                return Vector2Int.zero;
            return best;
        }

        private bool TryBuildAttackForTarget(
            int originGridIndex, int targetGridIndex, GridManager gridManager,
            out BattleDirection direction, out List<int> rangeGridIndices)
        {
            direction = BattleDirection.Right;
            rangeGridIndices = new List<int>();
            if (originGridIndex < 0 || targetGridIndex < 0 || gridManager == null)
                return false;
            List<int> right = BuildSweepRange(originGridIndex, gridManager, 1);
            List<int> left = BuildSweepRange(originGridIndex, gridManager, -1);
            if (right.Contains(targetGridIndex))
            {
                direction = BattleDirection.Right;
                rangeGridIndices = right;
                return true;
            }
            if (left.Contains(targetGridIndex))
            {
                direction = BattleDirection.Left;
                rangeGridIndices = left;
                return true;
            }
            return false;
        }

        private bool HasAttackableTargetOnSameHorizontalLine(
            int originGridIndex,
            GridManager gridManager)
        {
            if (originGridIndex < 0 || gridManager == null)
                return false;

            Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
            List<int> targets = FindCharacterTargetGridIndices();
            HashSet<int> attackRange = new(BuildSweepRange(originGridIndex, gridManager, 1));
            attackRange.UnionWith(BuildSweepRange(originGridIndex, gridManager, -1));

            for (int i = 0; i < targets.Count; i++)
            {
                int targetGridIndex = targets[i];

                if (!attackRange.Contains(targetGridIndex))
                    continue;

                Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

                if (targetCoord.y == originCoord.y)
                    return true;
            }

            return false;
        }

        private static int CountTargetsOnSameHorizontalLine(
            Vector2Int originCoord,
            List<int> targetGridIndices,
            GridManager gridManager)
        {
            int count = 0;

            for (int i = 0; i < targetGridIndices.Count; i++)
            {
                Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndices[i]);

                if (targetCoord.y == originCoord.y)
                    count++;
            }

            return count;
        }

        private static int CountAttackableTargets(
            int originGridIndex,
            List<int> targetGridIndices,
            GridManager gridManager)
        {
            HashSet<int> targets = new(targetGridIndices);
            List<int> rightRange = BuildSweepRange(originGridIndex, gridManager, 1);
            List<int> leftRange = BuildSweepRange(originGridIndex, gridManager, -1);
            int count = 0;

            for (int i = 0; i < rightRange.Count; i++)
            {
                if (targets.Contains(rightRange[i]))
                    count++;
            }

            for (int i = 0; i < leftRange.Count; i++)
            {
                if (targets.Contains(leftRange[i]))
                    count++;
            }

            return count;
        }

        private bool TryBuildAttack(
            int originGridIndex,
            GridManager gridManager,
            out BattleDirection direction,
            out List<int> rangeGridIndices)
        {
            direction = BattleDirection.Right;
            rangeGridIndices = new List<int>();

            if (originGridIndex < 0 || gridManager == null)
                return false;

            HashSet<int> targetSet = new(FindCharacterTargetGridIndices());

            List<int> rightRange = BuildSweepRange(originGridIndex, gridManager, 1);
            List<int> leftRange = BuildSweepRange(originGridIndex, gridManager, -1);

            bool rightHasTarget = ContainsTarget(rightRange, targetSet);
            bool leftHasTarget = ContainsTarget(leftRange, targetSet);

            if (!rightHasTarget && !leftHasTarget)
                return false;

            if (rightHasTarget && leftHasTarget)
            {
                int rightDistance = FindNearestTargetDistance(originGridIndex, rightRange, targetSet, gridManager);
                int leftDistance = FindNearestTargetDistance(originGridIndex, leftRange, targetSet, gridManager);

                if (leftDistance < rightDistance)
                {
                    direction = BattleDirection.Left;
                    rangeGridIndices = leftRange;
                    return true;
                }
            }

            if (rightHasTarget)
            {
                direction = BattleDirection.Right;
                rangeGridIndices = rightRange;
                return true;
            }

            direction = BattleDirection.Left;
            rangeGridIndices = leftRange;
            return true;
        }


        private static void SetFacingForReservedAttack(
            MonsterUnit monsterUnit,
            BattleDirection direction)
        {
            if (monsterUnit == null)
                return;

            BattleUnitFacing facing = monsterUnit.GetComponent<BattleUnitFacing>();
            bool faceRight = direction == BattleDirection.Right;

            if (facing != null)
                facing.FaceRight(faceRight);

            if (monsterUnit.RuntimeData != null)
                monsterUnit.RuntimeData.Direction = direction;
        }

        private static List<int> BuildSweepRange(
            int originGridIndex,
            GridManager gridManager,
            int horizontalSign)
        {
            List<int> result = new();
            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);

            for (int x = 1; x <= 2; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    // The sweep has three cells in its first column and
                    // only the middle cell in its second column (four total).
                    if (x == 2 && y != 0)
                        continue;
                    Vector2Int coord = origin + new Vector2Int(horizontalSign * x, y);

                    if (!gridManager.IsValidCoord(coord))
                        continue;

                    result.Add(gridManager.CoordToIndex(coord));
                }
            }

            return result;
        }

        private static bool ContainsTarget(List<int> range, HashSet<int> targets)
        {
            for (int i = 0; i < range.Count; i++)
            {
                if (targets.Contains(range[i]))
                    return true;
            }

            return false;
        }

        private static int FindNearestTargetDistance(
            int originGridIndex,
            List<int> range,
            HashSet<int> targets,
            GridManager gridManager)
        {
            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
            int bestDistance = int.MaxValue;

            for (int i = 0; i < range.Count; i++)
            {
                int gridIndex = range[i];

                if (!targets.Contains(gridIndex))
                    continue;

                Vector2Int coord = gridManager.IndexToCoord(gridIndex);
                int distance = Mathf.Abs(coord.x - origin.x) + Mathf.Abs(coord.y - origin.y);
                bestDistance = Mathf.Min(bestDistance, distance);
            }

            return bestDistance;
        }
    }
}
