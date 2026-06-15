using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class BlightAI : MonsterAIBase
    {
        private const string AttackSkillId = "S_Monster_06";
        private const string DebuffSkillId = "S_Monster_12";
        private const string MoveSkillId = "S_Monster_02";

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return DebuffSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            plan.Add(new MonsterAIAction(
                DebuffSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Back,
                -1,
                10
            ));

            bool danger = HasPlayerAround8(monsterUnit, gridManager);

            Debug.Log($"[BlightAI] Danger:{danger} / Monster:{monsterUnit.RuntimeData.Name}");

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

            Debug.Log($"[BlightAI] MoveOffset:{moveOffset}");

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