using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Battle
{
    public class MonsterTurnPlanner : MonoBehaviour
    {
        [SerializeField] private BattleTimelineManager timelineManager;

        public void PlanMonsterActions(
            List<MonsterUnit> monsters,
            BattleContext context)
        {
            if (timelineManager == null)
                return;

            for (int i = 0; i < monsters.Count; i++)
            {
                MonsterUnit monster = monsters[i];

                if (monster == null || monster.RuntimeData == null)
                    continue;

                if (monster.RuntimeData.IsDead)
                    continue;

                string skillId = monster.SelectSkill(context);

                if (string.IsNullOrWhiteSpace(skillId))
                    continue;

                MonsterSkillData skillData = DataManager.Instance.MonsterSkillDatabase.Get(skillId);

                timelineManager.AddMonsterAction(
                    monster,
                    skillId,
                    skillData.TimelineNotation,
                    i
                );
            }
        }
    }
}