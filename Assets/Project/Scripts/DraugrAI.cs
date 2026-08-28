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
    /// - 반격: BattleActionRunner에서 공격 스킬 피격 후 S_Monster_28을 즉시 실행
    /// </summary>
    public class DraugrAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_26";
        private const string AttackSkillId = "S_Monster_27";

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

            if (monsterUnit == null || monsterUnit.RuntimeData == null || gridManager == null)
                return plan;

            // 현재 위치에서 공격 가능하더라도 캐릭터와 같은 가로 라인이 아니라면
            // 반격 적중 가능성을 높이기 위해 계속 이동합니다.
            if (TryBuildAttack(
                    monsterUnit.MainGridIndex,
                    gridManager,
                    out BattleDirection currentDirection,
                    out List<int> currentRange) &&
                HasAttackableTargetOnSameHorizontalLine(monsterUnit.MainGridIndex, gridManager))
            {
                // 공격만 예약하는 경우에는 예약 시점의 공격 대상 방향으로 Facing을 맞춰 둡니다.
                // 실행 전 피격 등으로 Facing이 바뀌면 BattleActionRunner가 현재 Facing을 사용하므로
                // 최종 공격 방향도 자연스럽게 변경됩니다.
                SetFacingForReservedAttack(monsterUnit, currentDirection);

                plan.Add(new MonsterAIAction(
                    AttackSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Front,
                    1,
                    0,
                    monsterUnit.MainGridIndex,
                    false,
                    currentDirection,
                    false,
                    0,
                    currentRange));

                return plan;
            }

            // 공격할 수 없다면 가장 가까운 캐릭터를 향해 주변 8방향 중 1칸 이동합니다.
            Vector2Int moveOffset = GetBestOneTileMoveTowardNearestTarget(monsterUnit, gridManager);

            if (moveOffset == Vector2Int.zero)
                return plan;

            const int group = 1;

            plan.Add(new MonsterAIAction(
                MoveSkillId,
                moveOffset,
                MonsterAISlotPreference.Front,
                group,
                0));

            int projectedGridIndex = GetProjectedMainGridIndex(monsterUnit, gridManager, moveOffset);

            // 이동을 마친 새 위치에서 다시 실제 공격 가능 여부를 판정합니다.
            if (TryBuildAttack(
                    projectedGridIndex,
                    gridManager,
                    out BattleDirection movedDirection,
                    out List<int> movedRange))
            {
                plan.Add(new MonsterAIAction(
                    AttackSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.SameSlot,
                    group,
                    1,
                    projectedGridIndex,
                    true,
                    movedDirection,
                    false,
                    0,
                    movedRange));
            }

            return plan;
        }

        private Vector2Int GetBestOneTileMoveTowardNearestTarget(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            if (monsterUnit.MainGridIndex < 0)
                return Vector2Int.zero;

            List<int> targetGridIndices = FindCharacterTargetGridIndices();

            if (targetGridIndices == null || targetGridIndices.Count == 0)
                return Vector2Int.zero;

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int bestOffset = Vector2Int.zero;
            int bestSameLineCount = -1;
            int bestAttackableCount = -1;
            int bestVerticalDistance = int.MaxValue;
            int bestChebyshevDistance = int.MaxValue;
            int bestManhattanDistance = int.MaxValue;

            for (int i = 0; i < MoveDirections.Length; i++)
            {
                Vector2Int offset = MoveDirections[i];

                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                Vector2Int movedCoord = currentCoord + offset;
                int movedGridIndex = gridManager.CoordToIndex(movedCoord);
                int sameLineCount = CountTargetsOnSameHorizontalLine(movedCoord, targetGridIndices, gridManager);
                int attackableCount = CountAttackableTargets(movedGridIndex, targetGridIndices, gridManager);

                int verticalDistance = int.MaxValue;
                int chebyshevDistance = int.MaxValue;
                int manhattanDistance = int.MaxValue;

                for (int targetIndex = 0; targetIndex < targetGridIndices.Count; targetIndex++)
                {
                    Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndices[targetIndex]);
                    int dx = Mathf.Abs(targetCoord.x - movedCoord.x);
                    int dy = Mathf.Abs(targetCoord.y - movedCoord.y);
                    int candidateChebyshev = Mathf.Max(dx, dy);
                    int candidateManhattan = dx + dy;

                    if (dy < verticalDistance ||
                        (dy == verticalDistance && candidateChebyshev < chebyshevDistance) ||
                        (dy == verticalDistance && candidateChebyshev == chebyshevDistance && candidateManhattan < manhattanDistance))
                    {
                        verticalDistance = dy;
                        chebyshevDistance = candidateChebyshev;
                        manhattanDistance = candidateManhattan;
                    }
                }

                bool better =
                    sameLineCount > bestSameLineCount ||
                    (sameLineCount == bestSameLineCount && verticalDistance < bestVerticalDistance) ||
                    (sameLineCount == bestSameLineCount && verticalDistance == bestVerticalDistance && attackableCount > bestAttackableCount) ||
                    (sameLineCount == bestSameLineCount && verticalDistance == bestVerticalDistance && attackableCount == bestAttackableCount && chebyshevDistance < bestChebyshevDistance) ||
                    (sameLineCount == bestSameLineCount && verticalDistance == bestVerticalDistance && attackableCount == bestAttackableCount && chebyshevDistance == bestChebyshevDistance && manhattanDistance < bestManhattanDistance);

                if (!better)
                    continue;

                bestSameLineCount = sameLineCount;
                bestAttackableCount = attackableCount;
                bestVerticalDistance = verticalDistance;
                bestChebyshevDistance = chebyshevDistance;
                bestManhattanDistance = manhattanDistance;
                bestOffset = offset;
            }

            return bestOffset;
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
