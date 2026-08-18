using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 바로우 AI 골격입니다.
    /// 패턴이 확정되기 전까지는 행동을 예약하지 않습니다.
    /// </summary>
    public class BarrowAI : MonsterAIBase
    {
        public override string SelectSkill(
            MonsterRuntimeData monster,
            BattleContext context
        )
        {
            return null;
        }
    }
}
