using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public abstract class MonsterAIBase
    {
        public abstract string SelectSkill(
            MonsterRuntimeData monster,
            BattleContext context
        );

        protected bool IsFirstTurn(MonsterRuntimeData monster)
        {
            return monster.TurnCount <= 0;
        }

        protected bool IsHpBelow(
            MonsterRuntimeData monster,
            float percent
        )
        {
            return monster.GetHpPercent() <= percent;
        }

        protected string PickWeighted(
            params (string skillId, int weight)[] candidates
        )
        {
            int totalWeight = 0;

            foreach (var candidate in candidates)
            {
                totalWeight += candidate.weight;
            }

            int random = Random.Range(0, totalWeight);

            int current = 0;

            foreach (var candidate in candidates)
            {
                current += candidate.weight;

                if (random < current)
                    return candidate.skillId;
            }

            return candidates[0].skillId;
        }
    }
}