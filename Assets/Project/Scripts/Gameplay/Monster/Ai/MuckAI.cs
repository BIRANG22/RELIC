using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class MuckAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_01";
        private const string AttackSkillId = "S_Monster_02";

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

            string attackRangeId = monsterUnit.RuntimeData.AttackRangeId;
            bool usesRangedAttackOrigin =
                !string.IsNullOrWhiteSpace(attackRangeId) &&
                attackRangeId.Trim() != "0";
            MonsterSkillData attackSkill = usesRangedAttackOrigin
                ? DataManager.Instance?.MonsterSkillDatabase.Get(AttackSkillId)
                : null;

            // 머크는 행동 범위 안에 캐릭터가 있어도 항상 가장 가까운 캐릭터 쪽으로
            // 십자 또는 대각선 방향 1칸 이동을 먼저 시도합니다.
            Vector2Int moveOffset = GetBestOneTileMoveTowardNearestPlayer(monsterUnit, gridManager);
            bool canMove = moveOffset != Vector2Int.zero &&
                           CanMonsterMove(monsterUnit, gridManager, moveOffset);

            int group = 1;

            if (canMove)
            {
                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    moveOffset,
                    MonsterAISlotPreference.Front,
                    group,
                    0
                ));
            }

            int projectedMainGridIndex = canMove
                ? GetProjectedMainGridIndex(monsterUnit, gridManager, moveOffset)
                : monsterUnit.MainGridIndex;

            int rangeOriginGridIndex = -1;

            if (usesRangedAttackOrigin)
            {
                rangeOriginGridIndex = FindRangedAttackOrigin(
                    monsterUnit,
                    projectedMainGridIndex,
                    attackSkill,
                    attackRangeId,
                    gridManager);

                // 이동을 마친 위치에서 행동 범위 안에 캐릭터가 없으면 공격하지 않습니다.
                if (rangeOriginGridIndex < 0)
                    return plan;
            }

            plan.Add(new MonsterAIAction(
                AttackSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.SameSlot,
                group,
                1,
                rangeOriginGridIndex
            ));

            return plan;
        }

        private Vector2Int GetBestOneTileMoveTowardNearestPlayer(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            BattleCharacter target = FindNearestPlayer(monsterUnit, gridManager);

            if (target == null ||
                target.CurrentGridIndex < 0 ||
                monsterUnit.MainGridIndex < 0)
            {
                return Vector2Int.zero;
            }

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex);

            Vector2Int bestOffset = Vector2Int.zero;
            int bestChebyshevDistance = int.MaxValue;
            int bestManhattanDistance = int.MaxValue;

            for (int i = 0; i < MoveDirections.Length; i++)
            {
                Vector2Int offset = MoveDirections[i];

                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                Vector2Int projectedCoord = currentCoord + offset;
                int deltaX = Mathf.Abs(targetCoord.x - projectedCoord.x);
                int deltaY = Mathf.Abs(targetCoord.y - projectedCoord.y);
                int chebyshevDistance = Mathf.Max(deltaX, deltaY);
                int manhattanDistance = deltaX + deltaY;

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
    }
}
