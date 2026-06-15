using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class RancorAI : MonsterAIBase
    {
        private const string AttackSkillId = "S_Monster_06";
        private const string BuffSkillId = "S_Monster_10";
        private const string MoveSkillId = "S_Monster_02";

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return BuffSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            plan.Add(new MonsterAIAction(
                BuffSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Back,
                -1,
                10
            ));

            bool danger = HasPlayerAround8(monsterUnit, gridManager);;

            if (!danger)
                return plan;

            int group = 1;

            plan.Add(new MonsterAIAction(
                AttackSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Front,
                group,
                0
            ));

            MonsterSkillData moveSkill =
                DataManager.Instance.MonsterSkillDatabase.Get(MoveSkillId);

            Vector2Int moveOffset = Vector2Int.zero;

            if (moveSkill != null)
            {
                var moveOffsets =
                    MonsterMoveRangeService.GetMoveOffsets(moveSkill.RangeId);

                moveOffset =
                    GetBestMoveAwayFromNearestPlayer(
                        monsterUnit,
                        gridManager,
                        moveOffsets
                    );
            }

            Debug.Log($"[RancolAI] MoveOffset:{moveOffset}");

            if (moveOffset != Vector2Int.zero)
            {
                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    moveOffset,
                    MonsterAISlotPreference.SameSlot,
                    group,
                    1
                ));
            }

            return plan;
        }
    }
}