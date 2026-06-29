using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class BlobAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_01";
        private const string AttackSkillId = "S_Monster_05";

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

            MonsterSkillData attackSkill =
                DataManager.Instance?.MonsterSkillDatabase.Get(AttackSkillId);

            if (CanAttackFromGrid(
                    monsterUnit,
                    attackSkill,
                    monsterUnit.MainGridIndex,
                    gridManager,
                    out BattleDirection attackDirection))
            {
                AddAttack(plan, group: 1, priority: 0, monsterUnit.MainGridIndex, attackDirection);
                return plan;
            }

            Vector2Int moveOffset = GetChaseMoveOffset(monsterUnit, attackSkill, gridManager);
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

            int projectedGridIndex = GetProjectedMainGridIndex(
                monsterUnit,
                gridManager,
                canMove ? moveOffset : Vector2Int.zero);

            if (CanAttackFromGrid(
                    monsterUnit,
                    attackSkill,
                    projectedGridIndex,
                    gridManager,
                    out BattleDirection projectedAttackDirection))
            {
                AddAttack(plan, group, 1, projectedGridIndex, projectedAttackDirection);
            }

            return plan;
        }

        private void AddAttack(
            MonsterAIPlan plan,
            int group,
            int priority,
            int rangeOriginGridIndex,
            BattleDirection direction)
        {
            if (plan == null)
                return;

            plan.Add(new MonsterAIAction(
                AttackSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.SameSlot,
                group,
                priority,
                rangeOriginGridIndex,
                true,
                direction
            ));
        }

        private bool CanAttackFromGrid(
            MonsterUnit monsterUnit,
            MonsterSkillData attackSkill,
            int originGridIndex,
            GridManager gridManager,
            out BattleDirection attackDirection)
        {
            attackDirection = BattleDirection.Right;

            if (monsterUnit == null || attackSkill == null || gridManager == null || originGridIndex < 0)
                return false;

            return TryFindPlayerInSideAttackRange(
                originGridIndex,
                gridManager,
                out attackDirection);
        }

        private Vector2Int GetChaseMoveOffset(
            MonsterUnit monsterUnit,
            MonsterSkillData attackSkill,
            GridManager gridManager)
        {
            BattleCharacter target = FindNearestPlayer(monsterUnit, gridManager);

            if (target == null || target.CurrentGridIndex < 0)
                return Vector2Int.zero;

            List<Vector2Int> moveOffsets = GetBlobMoveOffsets();

            if (moveOffsets.Count <= 0)
                return Vector2Int.zero;

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex);

            Vector2Int bestOffset = Vector2Int.zero;
            int bestScore = int.MaxValue;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < moveOffsets.Count; i++)
            {
                Vector2Int offset = moveOffsets[i];

                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                Vector2Int projectedCoord = currentCoord + offset;
                int projectedGridIndex = gridManager.CoordToIndex(projectedCoord);
                int score = GetFrontTwoAttackPositionScore(projectedCoord, targetCoord);

                if (CanAttackFromGrid(
                        monsterUnit,
                        attackSkill,
                        projectedGridIndex,
                        gridManager,
                        out _))
                {
                    score = 0;
                }

                int distance =
                    Mathf.Abs(targetCoord.x - projectedCoord.x) +
                    Mathf.Abs(targetCoord.y - projectedCoord.y);

                if (score > bestScore)
                    continue;

                if (score == bestScore && distance >= bestDistance)
                    continue;

                bestScore = score;
                bestDistance = distance;
                bestOffset = offset;
            }

            return bestOffset;
        }

        private bool TryFindPlayerInSideAttackRange(
            int originGridIndex,
            GridManager gridManager,
            out BattleDirection attackDirection)
        {
            attackDirection = BattleDirection.Right;

            if (gridManager == null || originGridIndex < 0)
                return false;

            BattleCharacter[] players = FindPlayers();
            Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
            int bestDistance = int.MaxValue;
            bool found = false;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                int dx = playerCoord.x - originCoord.x;

                if (playerCoord.y != originCoord.y)
                    continue;

                int horizontalDistance = Mathf.Abs(dx);

                if (horizontalDistance <= 0 || horizontalDistance > 2)
                    continue;

                if (horizontalDistance >= bestDistance)
                    continue;

                bestDistance = horizontalDistance;
                attackDirection = dx > 0
                    ? BattleDirection.Right
                    : BattleDirection.Left;
                found = true;
            }

            return found;
        }

        private List<Vector2Int> GetBlobMoveOffsets()
        {
            MonsterSkillData moveSkill =
                DataManager.Instance?.MonsterSkillDatabase.Get(MoveSkillId);

            List<Vector2Int> moveOffsets = moveSkill != null
                ? MonsterMoveRangeService.GetMoveOffsets(moveSkill.RangeId)
                : new List<Vector2Int>();

            if (moveOffsets.Count > 0)
                return moveOffsets;

            return new List<Vector2Int>
            {
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.down
            };
        }

        private static int GetFrontTwoAttackPositionScore(
            Vector2Int originCoord,
            Vector2Int targetCoord)
        {
            int verticalDistance = Mathf.Abs(targetCoord.y - originCoord.y);
            int horizontalDistance = Mathf.Abs(targetCoord.x - originCoord.x);
            int horizontalMiss;

            if (horizontalDistance <= 0)
                horizontalMiss = 1;
            else if (horizontalDistance <= 2)
                horizontalMiss = 0;
            else
                horizontalMiss = horizontalDistance - 2;

            return verticalDistance * 10 + horizontalMiss;
        }
    }
}
