using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;
namespace Relic.Gameplay.Monster
{
    public class SlimeAI : MonsterAIBase
    {
        [SerializeField] private int towardPlayerWeight = 90;
        [SerializeField] private int randomWeight = 10;
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
                ("S_Monster_01", 40),
                ("S_Monster_02", 30),
                ("S_Monster_03", 30)
            );
        }

        public override Vector2Int SelectMoveOffset(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager,
            int moveAmount)
        {
            if (Random.Range(0, towardPlayerWeight + randomWeight) < towardPlayerWeight)
            {
                Vector2Int toward = GetMoveTowardNearestPlayer(monsterUnit, gridManager, moveAmount);

                if (toward != Vector2Int.zero)
                    return toward;
            }

            return GetRandomMoveOffset(moveAmount);
        }
    }
}