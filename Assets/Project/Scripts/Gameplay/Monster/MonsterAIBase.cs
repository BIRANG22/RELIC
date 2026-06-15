using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public abstract class MonsterAIBase
    {
        public virtual MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            if (monsterUnit == null || monsterUnit.RuntimeData == null)
                return plan;

            string skillId = SelectSkill(monsterUnit.RuntimeData, context);

            if (string.IsNullOrWhiteSpace(skillId))
                return plan;

            plan.Add(new MonsterAIAction(
                skillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Front
            ));

            return plan;
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

        public abstract string SelectSkill(
            MonsterRuntimeData monster,
            BattleContext context
        );

        protected bool IsFirstTurn(MonsterRuntimeData monster)
        {
            return monster.TurnCount <= 0;
        }

        protected bool IsHpBelow(MonsterRuntimeData monster, float percent)
        {
            return monster.GetHpPercent() <= percent;
        }

        protected string PickWeighted(params (string skillId, int weight)[] candidates)
        {
            int totalWeight = 0;

            for (int i = 0; i < candidates.Length; i++)
                totalWeight += candidates[i].weight;

            if (totalWeight <= 0)
                return candidates.Length > 0 ? candidates[0].skillId : null;

            int random = Random.Range(0, totalWeight);
            int current = 0;

            for (int i = 0; i < candidates.Length; i++)
            {
                current += candidates[i].weight;

                if (random < current)
                    return candidates[i].skillId;
            }

            return candidates[0].skillId;
        }

        protected BattleCharacter[] FindPlayers()
        {
            return Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );
        }

        protected Vector2Int GetMoveTowardNearestPlayer(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            int moveAmount)
        {
            BattleCharacter target = FindNearestPlayer(monsterUnit, gridManager);

            if (target == null)
                return Vector2Int.zero;

            return GetMoveTowardTarget(monsterUnit.MainGridIndex, target.CurrentGridIndex, gridManager, moveAmount);
        }

        protected BattleCharacter FindNearestPlayer(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            if (monsterUnit == null || gridManager == null)
                return null;

            BattleCharacter[] players = FindPlayers();
            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);

            BattleCharacter nearest = null;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (player == null || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                int distance =
                    Mathf.Abs(playerCoord.x - monsterCoord.x) +
                    Mathf.Abs(playerCoord.y - monsterCoord.y);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = player;
                }
            }

            return nearest;
        }

        protected BattleCharacter FindFarthestPlayer(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            if (monsterUnit == null || gridManager == null)
                return null;

            BattleCharacter[] players = FindPlayers();
            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);

            BattleCharacter farthest = null;
            int farthestDistance = -1;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (player == null || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                int distance =
                    Mathf.Abs(playerCoord.x - monsterCoord.x) +
                    Mathf.Abs(playerCoord.y - monsterCoord.y);

                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthest = player;
                }
            }

            return farthest;
        }

        protected BattleCharacter FindHighestHpPlayer()
        {
            BattleCharacter[] players = FindPlayers();

            BattleCharacter result = null;
            int highestHp = -1;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (player == null || player.RuntimeData == null)
                    continue;

                if (player.RuntimeData.CurrentHealth > highestHp)
                {
                    highestHp = player.RuntimeData.CurrentHealth;
                    result = player;
                }
            }

            return result;
        }

        protected Vector2Int GetMoveTowardTarget(
            int fromIndex,
            int targetIndex,
            GridManager gridManager,
            int moveAmount)
        {
            Vector2Int from = gridManager.IndexToCoord(fromIndex);
            Vector2Int target = gridManager.IndexToCoord(targetIndex);
            Vector2Int diff = target - from;

            if (diff == Vector2Int.zero)
                return Vector2Int.zero;

            if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
                return new Vector2Int(diff.x > 0 ? moveAmount : -moveAmount, 0);

            return new Vector2Int(0, diff.y > 0 ? moveAmount : -moveAmount);
        }

        protected Vector2Int GetDirectionToTarget(
            int fromIndex,
            int targetIndex,
            GridManager gridManager)
        {
            Vector2Int from = gridManager.IndexToCoord(fromIndex);
            Vector2Int target = gridManager.IndexToCoord(targetIndex);
            Vector2Int diff = target - from;

            if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
                return new Vector2Int(diff.x > 0 ? 1 : -1, 0);

            return new Vector2Int(0, diff.y > 0 ? 1 : -1);
        }

        protected Vector2Int GetMoveAwayFromNearestPlayer(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            int moveAmount)
        {
            BattleCharacter nearest = FindNearestPlayer(monsterUnit, gridManager);

            if (nearest == null)
                return Vector2Int.zero;

            Vector2Int toward = GetMoveTowardTarget(
                monsterUnit.MainGridIndex,
                nearest.CurrentGridIndex,
                gridManager,
                moveAmount
            );

            return -toward;
        }

        protected bool HasPlayerAround8(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            BattleCharacter[] players = FindPlayers();
            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (player == null || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                int dx = Mathf.Abs(playerCoord.x - monsterCoord.x);
                int dy = Mathf.Abs(playerCoord.y - monsterCoord.y);

                if (dx <= 1 && dy <= 1)
                    return true;
            }

            return false;
        }

        protected Vector2Int GetBestMoveTowardNearestPlayer(
    MonsterUnit monsterUnit,
    GridManager gridManager,
    List<Vector2Int> moveOffsets)
        {
            BattleCharacter target = FindNearestPlayer(monsterUnit, gridManager);

            if (target == null)
                return Vector2Int.zero;

            return GetBestMoveTowardTarget(
                monsterUnit,
                target.CurrentGridIndex,
                gridManager,
                moveOffsets
            );
        }

        protected Vector2Int GetBestMoveTowardTarget(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            GridManager gridManager,
            List<Vector2Int> moveOffsets)
        {
            if (monsterUnit == null || gridManager == null || moveOffsets == null)
                return Vector2Int.zero;

            int currentMainIndex = monsterUnit.MainGridIndex;

            if (currentMainIndex < 0)
                return Vector2Int.zero;

            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);
            Vector2Int currentCoord = gridManager.IndexToCoord(currentMainIndex);

            Vector2Int bestOffset = Vector2Int.zero;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < moveOffsets.Count; i++)
            {
                Vector2Int offset = moveOffsets[i];

                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                Vector2Int movedCoord = currentCoord + offset;

                int distance =
                    Mathf.Abs(targetCoord.x - movedCoord.x) +
                    Mathf.Abs(targetCoord.y - movedCoord.y);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestOffset = offset;
                }
            }

            return bestOffset;
        }

        protected Vector2Int GetBestMoveAwayFromNearestPlayer(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            List<Vector2Int> moveOffsets)
        {
            BattleCharacter target = FindNearestPlayer(monsterUnit, gridManager);

            if (target == null)
                return Vector2Int.zero;

            return GetBestMoveAwayFromTarget(
                monsterUnit,
                target.CurrentGridIndex,
                gridManager,
                moveOffsets
            );
        }

        protected Vector2Int GetBestMoveAwayFromTarget(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            GridManager gridManager,
            List<Vector2Int> moveOffsets)
        {
            if (monsterUnit == null || gridManager == null || moveOffsets == null)
                return Vector2Int.zero;

            int currentMainIndex = monsterUnit.MainGridIndex;

            if (currentMainIndex < 0)
                return Vector2Int.zero;

            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);
            Vector2Int currentCoord = gridManager.IndexToCoord(currentMainIndex);

            Vector2Int bestOffset = Vector2Int.zero;
            int bestDistance = -1;

            for (int i = 0; i < moveOffsets.Count; i++)
            {
                Vector2Int offset = moveOffsets[i];

                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                Vector2Int movedCoord = currentCoord + offset;

                int distance =
                    Mathf.Abs(targetCoord.x - movedCoord.x) +
                    Mathf.Abs(targetCoord.y - movedCoord.y);

                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    bestOffset = offset;
                }
            }

            return bestOffset;
        }

        protected bool CanMonsterMove(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int moveOffset)
        {
            if (monsterUnit == null || gridManager == null)
                return false;

            if (moveOffset == Vector2Int.zero)
                return false;

            for (int i = 0; i < monsterUnit.OccupiedGridIndices.Count; i++)
            {
                int occupiedIndex = monsterUnit.OccupiedGridIndices[i];

                Vector2Int currentCoord = gridManager.IndexToCoord(occupiedIndex);
                Vector2Int targetCoord = currentCoord + moveOffset;

                if (!gridManager.IsValidCoord(targetCoord))
                    return false;

                int targetIndex = gridManager.CoordToIndex(targetCoord);

                if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, monsterUnit))
                    return false;
            }

            return true;
        }
    }
}