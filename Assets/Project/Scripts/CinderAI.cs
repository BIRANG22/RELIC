using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 신더 AI
    /// - 행동 범위 안에 캐릭터가 있으면 자폭 공격을 사용합니다.
    /// - 행동 범위 안에 캐릭터가 없으면 가장 가까운 캐릭터를 향해 1칸 이동합니다.
    /// - 이동 후 새 위치에서 행동 범위를 다시 확인하고, 캐릭터가 들어왔다면 자폭합니다.
    /// </summary>
    public class CinderAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_01";
        private const string AttackSkillId = "S_Monster_14";

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

            string actionRangeId = monsterUnit.RuntimeData.AttackRangeId;

            // 현재 위치에서 행동 범위 안에 캐릭터가 있다면 이동하지 않고 바로 자폭합니다.
            if (HasPlayerInActionRange(
                    monsterUnit.MainGridIndex,
                    actionRangeId,
                    gridManager))
            {
                AddAttack(plan, monsterUnit.MainGridIndex, MonsterAISlotPreference.Front, 0);
                return plan;
            }

            // 현재 위치에서 공격할 수 없다면 가장 가까운 캐릭터를 향해 1칸 이동합니다.
            Vector2Int moveOffset = GetBestOneTileMoveTowardNearestPlayer(monsterUnit, gridManager);
            bool canMove = moveOffset != Vector2Int.zero &&
                           CanMonsterMove(monsterUnit, gridManager, moveOffset);

            if (!canMove)
                return plan;

            const int group = 1;

            plan.Add(new MonsterAIAction(
                MoveSkillId,
                moveOffset,
                MonsterAISlotPreference.Front,
                group,
                0
            ));

            // 이동을 마친 위치에서 행동 범위를 다시 판정합니다.
            int projectedGridIndex = GetProjectedMainGridIndex(
                monsterUnit,
                gridManager,
                moveOffset);

            if (HasPlayerInActionRange(
                    projectedGridIndex,
                    actionRangeId,
                    gridManager))
            {
                AddAttack(
                    plan,
                    projectedGridIndex,
                    MonsterAISlotPreference.SameSlot,
                    1,
                    group);
            }

            return plan;
        }

        private void AddAttack(
            MonsterAIPlan plan,
            int rangeOriginGridIndex,
            MonsterAISlotPreference slotPreference,
            int priority,
            int group = -1)
        {
            if (plan == null)
                return;

            plan.Add(new MonsterAIAction(
                AttackSkillId,
                Vector2Int.zero,
                slotPreference,
                group,
                priority,
                rangeOriginGridIndex
            ));
        }

        private bool HasPlayerInActionRange(
            int originGridIndex,
            string actionRangeId,
            GridManager gridManager)
        {
            if (originGridIndex < 0 ||
                gridManager == null ||
                string.IsNullOrWhiteSpace(actionRangeId) ||
                actionRangeId.Trim() == "0")
            {
                return false;
            }

            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;

            if (rangeDatabase == null)
                return false;

            List<int> actionRangeGridIndices = BattleRangeCalculator.GetSelectionRangeIndices(
                originGridIndex,
                actionRangeId,
                rangeDatabase,
                gridManager);

            if (actionRangeGridIndices == null || actionRangeGridIndices.Count <= 0)
                return false;

            BattleCharacter[] players = FindPlayers();

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                if (actionRangeGridIndices.Contains(player.CurrentGridIndex))
                    return true;
            }

            return false;
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
