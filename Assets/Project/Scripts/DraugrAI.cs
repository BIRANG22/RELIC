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
    /// - 기본 공격: 전방 2열 x 3칸 가로베기
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

            int targetIndex = FindPreferredTarget(monsterUnit.MainGridIndex, gridManager);
            if (targetIndex < 0)
                return plan;

            bool canAttackNow = TryBuildAttackForTarget(
                monsterUnit.MainGridIndex, targetIndex, gridManager,
                out BattleDirection currentDirection, out List<int> currentRange);

            // 공격할 수 있어도 안전한 접근 위치가 있으면 먼저 한 칸 이동합니다.
            Vector2Int moveOffset = GetBestSafeOneTileMoveTowardTarget(
                monsterUnit, gridManager, targetIndex);
            if (moveOffset != Vector2Int.zero)
            {
                const int group = 1;
                int projectedGridIndex = GetProjectedMainGridIndex(monsterUnit, gridManager, moveOffset);
                bool canAttackAfterMove = TryBuildAttackForTarget(
                    projectedGridIndex, targetIndex, gridManager,
                    out BattleDirection movedDirection, out List<int> movedRange);

                // 현재 공격할 수 있는 대상을 이동 때문에 놓치지 않습니다.
                if (canAttackAfterMove || !canAttackNow)
                {
                    plan.Add(new MonsterAIAction(
                        MoveSkillId, moveOffset, MonsterAISlotPreference.Front, group, 0));
                    if (canAttackAfterMove)
                    {
                        plan.Add(new MonsterAIAction(
                            AttackSkillId, Vector2Int.zero, MonsterAISlotPreference.SameSlot,
                            group, 1, projectedGridIndex, true, movedDirection, false, 0, movedRange));
                    }
                    return plan;
                }
            }

            if (canAttackNow)
            {
                SetFacingForReservedAttack(monsterUnit, currentDirection);
                plan.Add(new MonsterAIAction(
                    AttackSkillId, Vector2Int.zero, MonsterAISlotPreference.Front,
                    1, 0, monsterUnit.MainGridIndex, false,
                    currentDirection, false, 0, currentRange));
            }
            return plan;
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
            Vector2Int best = Vector2Int.zero;
            int bestDistance = currentDistance;
            int bestSecondary = int.MaxValue;
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
                // 통과 가능한 그리드 효과도 불필요한 위험이므로 피합니다.
                if (effects != null && effects.HasEffect(nextIndex))
                    continue;
                int distance = Mathf.Max(Mathf.Abs(target.x - next.x),
                                         Mathf.Abs(target.y - next.y));
                int secondary = Mathf.Abs(target.x - next.x) + Mathf.Abs(target.y - next.y);
                if (distance < bestDistance ||
                    (distance == bestDistance && best != Vector2Int.zero && secondary < bestSecondary))
                {
                    bestDistance = distance;
                    bestSecondary = secondary;
                    best = offset;
                }
            }
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
