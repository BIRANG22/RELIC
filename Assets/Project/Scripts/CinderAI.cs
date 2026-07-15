using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 신더 AI
    /// - 폭발 준비 상태가 아니라면 행동 범위 안에 캐릭터가 있어도 계속 가까운 캐릭터를 향해 1칸 이동합니다.
    /// - E_Explode 수치가 0이 되면 다음 턴 타임라인에 자폭 공격을 등록합니다.
    /// </summary>
    public class CinderAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_01";
        private const string ExplodeSkillId = "S_Monster_14";

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
            return monster != null && monster.IsExplodeReady
                ? ExplodeSkillId
                : MoveSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            if (monsterUnit == null || monsterUnit.RuntimeData == null || gridManager == null)
                return plan;

            // 폭발 수치가 0이 된 다음 턴에는 이동하지 않고 자폭 공격을 등록합니다.
            if (monsterUnit.RuntimeData.IsExplodeReady)
            {
                plan.Add(new MonsterAIAction(
                    ExplodeSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Front,
                    1,
                    0,
                    monsterUnit.MainGridIndex
                ));

                return plan;
            }

            // 행동 범위 안에 캐릭터가 있더라도 계속 가장 가까운 캐릭터 쪽으로 이동합니다.
            Vector2Int moveOffset = GetBestOneTileMoveTowardNearestPlayer(monsterUnit, gridManager);
            bool canMove = moveOffset != Vector2Int.zero &&
                           CanMonsterMove(monsterUnit, gridManager, moveOffset);

            if (!canMove)
                return plan;

            plan.Add(new MonsterAIAction(
                MoveSkillId,
                moveOffset,
                MonsterAISlotPreference.Front,
                1,
                0
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
