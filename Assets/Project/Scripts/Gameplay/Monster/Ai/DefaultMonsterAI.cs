using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;

namespace Relic.Gameplay.Monster
{
    public class DefaultMonsterAI : MonsterAIBase
    {
        public override string SelectSkill(
            MonsterRuntimeData monster,
            BattleContext context
        )
        {
            if (monster.PossSkillIds.Count <= 0)
                return null;

            return monster.PossSkillIds[0];
        }
    }
}