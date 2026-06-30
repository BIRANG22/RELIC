using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class MuckAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_01";
        private const string AttackSkillId = "S_Monster_04";

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

            if (monsterUnit == null || monsterUnit.RuntimeData == null)
                return plan;

            string attackRangeId = monsterUnit.RuntimeData.AttackRangeId;
            bool usesRangedAttackOrigin =
                !string.IsNullOrWhiteSpace(attackRangeId) &&
                attackRangeId.Trim() != "0";
            MonsterSkillData attackSkill = usesRangedAttackOrigin
                ? DataManager.Instance?.MonsterSkillDatabase.Get(AttackSkillId)
                : null;
            int currentRangeOriginGridIndex = -1;

            if (usesRangedAttackOrigin)
            {
                currentRangeOriginGridIndex = FindRangedAttackOrigin(
                    monsterUnit,
                    monsterUnit.MainGridIndex,
                    attackSkill,
                    attackRangeId,
                    gridManager);
            }

            bool canAttackFromCurrentPosition =
                !usesRangedAttackOrigin || currentRangeOriginGridIndex >= 0;
            Vector2Int moveOffset = GetMoveTowardNearestPlayer(monsterUnit, gridManager, 1);
            bool canMove = moveOffset != Vector2Int.zero &&
                           CanMonsterMove(monsterUnit, gridManager, moveOffset);

            bool skipMove = !canMove ||
                            (canAttackFromCurrentPosition && Random.value < 0.5f);
            Vector2Int effectiveMoveOffset = skipMove ? Vector2Int.zero : moveOffset;

            int group = 1;

            if (!skipMove)
            {
                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    moveOffset,
                    MonsterAISlotPreference.Front,
                    group,
                    0
                ));
            }

            int rangeOriginGridIndex = currentRangeOriginGridIndex;

            if (usesRangedAttackOrigin && !skipMove)
            {
                int projectedMainGridIndex = GetProjectedMainGridIndex(
                    monsterUnit,
                    gridManager,
                    effectiveMoveOffset);

                rangeOriginGridIndex = FindRangedAttackOrigin(
                    monsterUnit,
                    projectedMainGridIndex,
                    attackSkill,
                    attackRangeId,
                    gridManager);
            }

            if (usesRangedAttackOrigin && rangeOriginGridIndex < 0)
                return plan;

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
    }
}
