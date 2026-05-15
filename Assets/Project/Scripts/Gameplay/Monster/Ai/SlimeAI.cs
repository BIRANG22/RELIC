using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;

namespace Relic.Gameplay.Monster
{
    public class SlimeAI : MonsterAIBase
    {
        public override string SelectSkill(
            MonsterRuntimeData monster,
            BattleContext context
        )
        {
            if (IsFirstTurn(monster))
            {
                return "S_Monster_01";
            }

            if (IsHpBelow(monster, 0.5f))
            {
                return PickWeighted(
                    ("S_Monster_03", 80),
                    ("S_Monster_02", 20)
                );
            }

            return PickWeighted(
                ("S_Monster_02", 70),
                ("S_Monster_03", 30)
            );
        }
    }
}