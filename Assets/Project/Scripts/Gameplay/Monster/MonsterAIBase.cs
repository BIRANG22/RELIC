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

        public virtual Vector2Int SelectMoveOffset(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager,
            int moveAmount)
        {
            return GetRandomMoveOffset(moveAmount);
        }

        protected Vector2Int GetRandomMoveOffset(int moveAmount)
        {
            Vector2Int[] directions =
            {
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.down
            };

            return directions[Random.Range(0, directions.Length)] * moveAmount;
        }

        protected Vector2Int GetMoveTowardNearestPlayer(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            int moveAmount)
        {
            if (monsterUnit == null || gridManager == null)
                return Vector2Int.zero;

            BattleCharacter[] players =
                Object.FindObjectsByType<BattleCharacter>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            if (players == null || players.Length <= 0)
                return Vector2Int.zero;

            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);

            BattleCharacter nearest = null;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null || players[i].CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(players[i].CurrentGridIndex);
                int distance = Mathf.Abs(playerCoord.x - monsterCoord.x) +
                               Mathf.Abs(playerCoord.y - monsterCoord.y);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = players[i];
                }
            }

            if (nearest == null)
                return Vector2Int.zero;

            Vector2Int targetCoord = gridManager.IndexToCoord(nearest.CurrentGridIndex);
            Vector2Int diff = targetCoord - monsterCoord;

            if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
                return new Vector2Int(diff.x > 0 ? moveAmount : -moveAmount, 0);

            return new Vector2Int(0, diff.y > 0 ? moveAmount : -moveAmount);
        }
    }
}