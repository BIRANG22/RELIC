using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class VespaAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_11";
        private const string AttackSkillId = "S_Monster_12";

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

            if (monsterUnit.MainGridIndex < 0)
                return plan;

            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            int targetGridIndex = FindBestTargetAndMoveSteps(
                monsterUnit,
                gridManager,
                monsterCoord,
                out List<Vector2Int> moveSteps);

            if (targetGridIndex < 0)
                return plan;

            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);
            Vector2Int totalMoveOffset = Vector2Int.zero;
            int group = 1;

            for (int i = 0; i < moveSteps.Count; i++)
            {
                Vector2Int moveOffset = moveSteps[i];
                totalMoveOffset += moveOffset;

                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    moveOffset,
                    i == 0
                        ? MonsterAISlotPreference.Front
                        : MonsterAISlotPreference.NextSlot,
                    group,
                    i));
            }

            Vector2Int projectedCoord = monsterCoord + totalMoveOffset;
            int dashTargetGridIndex = FindBestHorizontalDashTarget(
                gridManager,
                projectedCoord);

            // 이동 후 가로 돌진 경로에 장애물이나 다른 유닛이 있다면 공격을 예약하지 않습니다.
            // 장애물 뒤의 대상을 계속 공격 대상으로 잡는 현상을 방지합니다.
            if (dashTargetGridIndex < 0)
                return plan;

            Vector2Int dashTargetCoord = gridManager.IndexToCoord(dashTargetGridIndex);
            BattleDirection attackDirection = GetHorizontalAttackDirection(
                projectedCoord,
                dashTargetCoord,
                monsterUnit.RuntimeData.Direction);

            int projectedGridIndex = gridManager.IsValidCoord(projectedCoord)
                ? gridManager.CoordToIndex(projectedCoord)
                : monsterUnit.MainGridIndex;

            plan.Add(new MonsterAIAction(
                AttackSkillId,
                Vector2Int.zero,
                moveSteps.Count > 0
                    ? MonsterAISlotPreference.NextSlot
                    : MonsterAISlotPreference.Front,
                group,
                moveSteps.Count,
                projectedGridIndex,
                true,
                attackDirection));

            return plan;
        }

        /// <summary>
        /// 베스파는 가로 돌진만 사용하므로, 다른 가로 라인에 있는 캐릭터를 노릴 때는
        /// 먼저 가로로 자리를 벌린 뒤 세로로 이동하여 같은 가로 라인을 맞춥니다.
        /// </summary>
        private int FindBestTargetAndMoveSteps(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int monsterCoord,
            out List<Vector2Int> bestMoveSteps)
        {
            bestMoveSteps = new List<Vector2Int>();
            List<int> targets = FindCharacterTargetGridIndices();

            int bestTargetGridIndex = -1;
            int bestMoveCount = int.MaxValue;
            int bestAttackDistance = int.MaxValue;

            // 정확히 같은 가로 라인을 만들 수 있는 대상을 먼저 찾습니다.
            for (int i = 0; i < targets.Count; i++)
            {
                int targetGridIndex = targets[i];

                if (targetGridIndex < 0)
                    continue;

                Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);
                List<Vector2Int> candidateSteps = BuildExactAlignmentSteps(
                    monsterUnit,
                    gridManager,
                    monsterCoord,
                    targetCoord);

                if (candidateSteps == null)
                    continue;

                Vector2Int projectedCoord = monsterCoord;
                for (int stepIndex = 0; stepIndex < candidateSteps.Count; stepIndex++)
                    projectedCoord += candidateSteps[stepIndex];

                if (!IsHorizontalDashPathClear(gridManager, projectedCoord, targetCoord))
                    continue;

                int attackDistance = Mathf.Abs(targetCoord.x - projectedCoord.x);

                if (candidateSteps.Count < bestMoveCount ||
                    (candidateSteps.Count == bestMoveCount && attackDistance < bestAttackDistance))
                {
                    bestTargetGridIndex = targetGridIndex;
                    bestMoveSteps = candidateSteps;
                    bestMoveCount = candidateSteps.Count;
                    bestAttackDistance = attackDistance;
                }
            }

            if (bestTargetGridIndex >= 0)
                return bestTargetGridIndex;

            // 두 번의 이동으로 라인을 맞출 수 없다면 가장 가까운 캐릭터 계열 대상을 향해 접근합니다.
            int fallbackTargetGridIndex = FindNearestCharacterTargetGridIndex(monsterUnit, gridManager);

            if (fallbackTargetGridIndex < 0)
                return -1;

            Vector2Int fallbackCoord = gridManager.IndexToCoord(fallbackTargetGridIndex);
            bestMoveSteps = BuildFallbackSteps(
                monsterUnit,
                gridManager,
                monsterCoord,
                fallbackCoord);

            return fallbackTargetGridIndex;
        }

        private List<Vector2Int> BuildExactAlignmentSteps(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int monsterCoord,
            Vector2Int targetCoord)
        {
            // 이미 같은 가로 라인이라면 기존 규칙대로 50% 확률로만 가로 이동합니다.
            if (monsterCoord.y == targetCoord.y)
            {
                List<Vector2Int> sameLineSteps = new();

                if (BattleRandom.Value() >= 0.5f)
                    return sameLineSteps;

                Vector2Int horizontalMove = FindBestHorizontalMove(
                    monsterUnit,
                    gridManager,
                    monsterCoord,
                    targetCoord,
                    requireDifferentTargetX: false);

                if (horizontalMove != Vector2Int.zero)
                    sameLineSteps.Add(horizontalMove);

                return sameLineSteps;
            }

            int verticalDifference = targetCoord.y - monsterCoord.y;

            // 한 번의 세로 이동으로 같은 가로 라인을 맞출 수 있어야 합니다.
            if (Mathf.Abs(verticalDifference) > 2)
                return null;

            Vector2Int verticalStep = new(0, verticalDifference);

            // 현재 X 위치에서도 대상과 서로 다른 열을 유지할 수 있다면
            // 불필요한 가로 이동 없이 세로 이동만으로 같은 가로 라인을 맞춥니다.
            if (monsterCoord.x != targetCoord.x &&
                CanMonsterMove(monsterUnit, gridManager, verticalStep))
            {
                return new List<Vector2Int>
                {
                    verticalStep
                };
            }

            // 대상과 같은 열에 있어 세로 이동만 하면 돌진 방향이 만들어지지 않는 경우에만
            // 먼저 가로로 자리를 옮긴 뒤 세로 이동합니다.
            Vector2Int horizontalStep = FindBestHorizontalMove(
                monsterUnit,
                gridManager,
                monsterCoord,
                targetCoord,
                requireDifferentTargetX: true,
                verticalDifference);

            if (horizontalStep == Vector2Int.zero)
                return null;

            Vector2Int totalOffset = horizontalStep + verticalStep;

            // 두 행동을 가로 → 세로 순서로 실행했을 때 전체 경로가 유효한지 확인합니다.
            if (!CanMonsterMove(monsterUnit, gridManager, totalOffset))
                return null;

            return new List<Vector2Int>
            {
                horizontalStep,
                verticalStep
            };
        }

        private List<Vector2Int> BuildFallbackSteps(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int monsterCoord,
            Vector2Int targetCoord)
        {
            List<Vector2Int> result = new();
            int verticalDifference = targetCoord.y - monsterCoord.y;
            int verticalAmount = Mathf.Clamp(verticalDifference, -2, 2);

            // 대상과 다른 열에 있다면 가로 이동 없이 세로로만 접근합니다.
            // 같은 가로 라인이 되었을 때 바로 좌우 돌진 방향을 만들 수 있기 때문입니다.
            if (monsterCoord.x != targetCoord.x)
            {
                if (verticalAmount == 0)
                    return result;

                Vector2Int verticalOnlyStep = new(0, verticalAmount);

                if (CanMonsterMove(monsterUnit, gridManager, verticalOnlyStep))
                    result.Add(verticalOnlyStep);

                return result;
            }

            // 대상과 같은 열에 있을 때만 가로로 자리를 벌려 돌진 방향을 만듭니다.
            Vector2Int horizontalStep = FindBestHorizontalMove(
                monsterUnit,
                gridManager,
                monsterCoord,
                targetCoord,
                requireDifferentTargetX: true);

            if (horizontalStep != Vector2Int.zero)
                result.Add(horizontalStep);

            if (verticalAmount == 0)
                return result;

            Vector2Int verticalStep = new(0, verticalAmount);
            Vector2Int totalOffset = horizontalStep + verticalStep;

            if (CanMonsterMove(monsterUnit, gridManager, totalOffset))
                result.Add(verticalStep);

            return result;
        }

        private Vector2Int FindBestHorizontalMove(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int monsterCoord,
            Vector2Int targetCoord,
            bool requireDifferentTargetX,
            int followingVerticalAmount = 0)
        {
            Vector2Int bestMove = Vector2Int.zero;
            int bestDistance = int.MaxValue;

            int[] directions = { -1, 1 };
            int[] distances = { 1, 2 };

            for (int directionIndex = 0; directionIndex < directions.Length; directionIndex++)
            {
                for (int distanceIndex = 0; distanceIndex < distances.Length; distanceIndex++)
                {
                    Vector2Int horizontalMove = new(
                        directions[directionIndex] * distances[distanceIndex],
                        0);

                    Vector2Int projectedCoord = monsterCoord + horizontalMove;

                    if (requireDifferentTargetX && projectedCoord.x == targetCoord.x)
                        continue;

                    Vector2Int totalOffset = horizontalMove + new Vector2Int(0, followingVerticalAmount);

                    if (!CanMonsterMove(monsterUnit, gridManager, totalOffset))
                        continue;

                    int horizontalDistance = Mathf.Abs(targetCoord.x - projectedCoord.x);

                    if (horizontalDistance < bestDistance)
                    {
                        bestDistance = horizontalDistance;
                        bestMove = horizontalMove;
                    }
                }
            }

            return bestMove;
        }


        private int FindBestHorizontalDashTarget(
            GridManager gridManager,
            Vector2Int originCoord)
        {
            List<int> targets = FindCharacterTargetGridIndices();
            int bestTargetGridIndex = -1;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                int targetGridIndex = targets[i];

                if (targetGridIndex < 0)
                    continue;

                Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

                if (!IsHorizontalDashPathClear(gridManager, originCoord, targetCoord))
                    continue;

                int distance = Mathf.Abs(targetCoord.x - originCoord.x);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTargetGridIndex = targetGridIndex;
                }
            }

            return bestTargetGridIndex;
        }

        private bool IsHorizontalDashPathClear(
            GridManager gridManager,
            Vector2Int originCoord,
            Vector2Int targetCoord)
        {
            if (gridManager == null || originCoord.y != targetCoord.y || originCoord.x == targetCoord.x)
                return false;

            int direction = targetCoord.x > originCoord.x ? 1 : -1;
            BattleGridEffectController gridEffectController =
                Object.FindFirstObjectByType<BattleGridEffectController>(
                    FindObjectsInactive.Include);

            // 대상 칸 직전까지만 검사합니다. 대상 캐릭터가 있는 칸은 돌진 충돌 지점입니다.
            for (int x = originCoord.x + direction; x != targetCoord.x; x += direction)
            {
                Vector2Int checkCoord = new(x, originCoord.y);

                if (!gridManager.IsValidCoord(checkCoord))
                    return false;

                int checkIndex = gridManager.CoordToIndex(checkCoord);

                if (BattleOccupancyService.IsOccupiedByAnyUnit(checkIndex))
                    return false;

                if (gridEffectController != null && gridEffectController.IsBlocked(checkIndex))
                    return false;
            }

            return true;
        }

        private static BattleDirection GetHorizontalAttackDirection(
            Vector2Int originCoord,
            Vector2Int targetCoord,
            BattleDirection fallback)
        {
            if (targetCoord.x > originCoord.x)
                return BattleDirection.Right;

            if (targetCoord.x < originCoord.x)
                return BattleDirection.Left;

            return fallback;
        }
    }
}
