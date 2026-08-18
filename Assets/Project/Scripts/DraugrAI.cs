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

            // 현재 위치에서 바로 공격할 수 있다면 이동하지 않고 가로베기를 사용합니다.
            if (TryBuildAttack(
                    monsterUnit.MainGridIndex,
                    gridManager,
                    out BattleDirection currentDirection,
                    out List<int> currentRange))
            {
                plan.Add(new MonsterAIAction(
                    AttackSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Front,
                    1,
                    0,
                    monsterUnit.MainGridIndex,
                    true,
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
            int targetGridIndex = FindNearestCharacterTargetGridIndex(monsterUnit, gridManager);

            if (targetGridIndex < 0 || monsterUnit.MainGridIndex < 0)
                return Vector2Int.zero;

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

            Vector2Int bestOffset = Vector2Int.zero;
            int bestChebyshevDistance = int.MaxValue;
            int bestManhattanDistance = int.MaxValue;

            for (int i = 0; i < MoveDirections.Length; i++)
            {
                Vector2Int offset = MoveDirections[i];

                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                Vector2Int movedCoord = currentCoord + offset;
                int dx = Mathf.Abs(targetCoord.x - movedCoord.x);
                int dy = Mathf.Abs(targetCoord.y - movedCoord.y);
                int chebyshevDistance = Mathf.Max(dx, dy);
                int manhattanDistance = dx + dy;

                if (chebyshevDistance > bestChebyshevDistance)
                    continue;

                if (chebyshevDistance == bestChebyshevDistance &&
                    manhattanDistance >= bestManhattanDistance)
                {
                    continue;
                }

                bestChebyshevDistance = chebyshevDistance;
                bestManhattanDistance = manhattanDistance;
                bestOffset = offset;
            }

            return bestOffset;
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
