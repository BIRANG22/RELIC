using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class EliseAI : MonsterAIBase
    {
        private const string AttackHighHPSkillId = "S_Monster_08";
        private const string LastSlotAttackSkillId = "S_Monster_09";
        private const string BuffSkillId = "S_Monster_11";
        private const string DebuffSkillId = "S_Monster_13";
        private const string MoveSkillId = "S_Monster_03";

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return AttackHighHPSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            MonsterRuntimeData runtime = monsterUnit.RuntimeData;
            int turn = runtime.TurnCount + 1;

            if (HasPlayerAround8(monsterUnit, gridManager))
            {
                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    GetMoveAwayFromNearestPlayer(monsterUnit, gridManager, 1),
                    MonsterAISlotPreference.Front,
                    -1,
                    0
                ));
            }

            plan.Add(new MonsterAIAction(
                AttackHighHPSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Center,
                -1,
                1
            ));

            plan.Add(new MonsterAIAction(
                LastSlotAttackSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Last,
                -1,
                2
            ));

            bool useBuff = turn % 3 == 0;
            bool useDebuff = turn % 2 == 0;

            if (useBuff && useDebuff)
                useDebuff = false;

            if (useBuff)
            {
                plan.Add(new MonsterAIAction(
                    BuffSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Front,
                    -1,
                    3
                ));
            }
            else if (useDebuff)
            {
                plan.Add(new MonsterAIAction(
                    DebuffSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Front,
                    -1,
                    3
                ));
            }

            return plan;
        }
    }
}
