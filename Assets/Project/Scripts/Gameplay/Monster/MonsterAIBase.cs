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

            return directions[BattleRandom.Range(0, directions.Length)] * moveAmount;
        }

        public abstract string SelectSkill(
            MonsterRuntimeData monster,
            BattleContext context
        );

        protected bool IsFirstTurn(MonsterRuntimeData monster)
        {
            return monster.TurnCount <= 0;
        }

        protected bool IsHPBelow(MonsterRuntimeData monster, float percent)
        {
            return monster.GetHPPercent() <= percent;
        }

        protected string PickWeighted(params (string skillId, int weight)[] candidates)
        {
            int totalWeight = 0;

            for (int i = 0; i < candidates.Length; i++)
                totalWeight += candidates[i].weight;

            if (totalWeight <= 0)
                return candidates.Length > 0 ? candidates[0].skillId : null;

            int random = BattleRandom.Range(0, totalWeight);
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

        protected bool IsAlivePlayer(BattleCharacter player)
        {
            return player != null &&
                   player.RuntimeData != null &&
                   !player.RuntimeData.IsDead;
        }

        protected List<int> FindCharacterTargetGridIndices()
        {
            List<int> result = new();
            BattleCharacter[] players = FindPlayers();

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                if (!result.Contains(player.CurrentGridIndex))
                    result.Add(player.CurrentGridIndex);
            }

            BattleGridEffectController gridEffectController =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            if (gridEffectController != null)
            {
                IReadOnlyList<int> gridEffectTargets =
                    gridEffectController.GetCharacterTargetGridIndices();

                for (int i = 0; i < gridEffectTargets.Count; i++)
                {
                    int gridIndex = gridEffectTargets[i];

                    if (gridIndex >= 0 && !result.Contains(gridIndex))
                        result.Add(gridIndex);
                }
            }

            return result;
        }

        protected int FindNearestCharacterTargetGridIndex(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            if (monsterUnit == null || gridManager == null || monsterUnit.MainGridIndex < 0)
                return -1;

            List<int> targets = FindCharacterTargetGridIndices();
            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            int nearestGridIndex = -1;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                int gridIndex = targets[i];

                if (gridIndex < 0 || gridManager.GetCellByIndex(gridIndex) == null)
                    continue;

                Vector2Int targetCoord = gridManager.IndexToCoord(gridIndex);
                int distance =
                    Mathf.Abs(targetCoord.x - monsterCoord.x) +
                    Mathf.Abs(targetCoord.y - monsterCoord.y);

                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearestGridIndex = gridIndex;
            }

            return nearestGridIndex;
        }

        protected Vector2Int GetMoveTowardNearestPlayer(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            int moveAmount)
        {
            int targetGridIndex = FindNearestCharacterTargetGridIndex(monsterUnit, gridManager);

            if (targetGridIndex < 0)
                return Vector2Int.zero;

            return GetMoveTowardTarget(monsterUnit.MainGridIndex, targetGridIndex, gridManager, moveAmount);
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

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
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

        protected int FindRangedAttackOrigin(
            MonsterUnit monsterUnit,
            int casterGridIndex,
            MonsterSkillData attackSkill,
            string attackRangeId,
            GridManager gridManager)
        {
            if (monsterUnit == null ||
                attackSkill == null ||
                gridManager == null ||
                casterGridIndex < 0 ||
                string.IsNullOrWhiteSpace(attackRangeId) ||
                attackRangeId.Trim() == "0")
            {
                return -1;
            }

            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;

            if (rangeDatabase == null)
                return -1;

            List<int> candidateGridIndices = BattleRangeCalculator.GetSelectionRangeIndices(
                casterGridIndex,
                attackRangeId,
                rangeDatabase,
                gridManager);

            if (candidateGridIndices == null || candidateGridIndices.Count <= 0)
                return -1;

            List<int> validOrigins = new();

            for (int i = 0; i < candidateGridIndices.Count; i++)
            {
                int candidateGridIndex = candidateGridIndices[i];

                if (candidateGridIndex < 0)
                    continue;

                BattleDirection direction = GetDirectionToNearestPlayer(candidateGridIndex, gridManager);

                List<int> attackRange = MonsterSkillRangeService.BuildRangeGridIndices(
                    monsterUnit,
                    attackSkill,
                    gridManager,
                    direction == BattleDirection.Right,
                    candidateGridIndex,
                    rangeDatabase);

                List<int> targetGridIndices = MonsterSkillRangeService.FilterTargetGridIndices(
                    attackSkill,
                    attackRange);

                if (targetGridIndices.Count > 0)
                    validOrigins.Add(candidateGridIndex);
            }

            if (validOrigins.Count <= 0)
                return -1;

            return validOrigins[BattleRandom.Range(0, validOrigins.Count)];
        }

        protected int GetProjectedMainGridIndex(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int moveOffset)
        {
            if (monsterUnit == null || gridManager == null || monsterUnit.MainGridIndex < 0)
                return -1;

            if (moveOffset == Vector2Int.zero)
                return monsterUnit.MainGridIndex;

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int projectedCoord = currentCoord + moveOffset;

            return gridManager.IsValidCoord(projectedCoord)
                ? gridManager.CoordToIndex(projectedCoord)
                : monsterUnit.MainGridIndex;
        }

        protected BattleDirection GetDirectionToNearestPlayer(
            int originGridIndex,
            GridManager gridManager)
        {
            if (gridManager == null || originGridIndex < 0)
                return BattleDirection.Left;

            List<int> targets = FindCharacterTargetGridIndices();
            Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
            int nearestGridIndex = -1;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                int gridIndex = targets[i];

                if (gridIndex < 0)
                    continue;

                Vector2Int targetCandidateCoord = gridManager.IndexToCoord(gridIndex);
                int distance =
                    Mathf.Abs(targetCandidateCoord.x - originCoord.x) +
                    Mathf.Abs(targetCandidateCoord.y - originCoord.y);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestGridIndex = gridIndex;
                }
            }

            if (nearestGridIndex < 0)
                return BattleDirection.Left;

            Vector2Int targetCoord = gridManager.IndexToCoord(nearestGridIndex);

            return targetCoord.x >= originCoord.x
                ? BattleDirection.Right
                : BattleDirection.Left;
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

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
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

        protected BattleCharacter FindHighestHPPlayer()
        {
            BattleCharacter[] players = FindPlayers();

            BattleCharacter result = null;
            int highestHP = -1;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player))
                    continue;

                if (player.RuntimeData.CurrentHP > highestHP)
                {
                    highestHP = player.RuntimeData.CurrentHP;
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
            int targetGridIndex = FindNearestCharacterTargetGridIndex(monsterUnit, gridManager);

            if (targetGridIndex < 0)
                return Vector2Int.zero;

            Vector2Int toward = GetMoveTowardTarget(
                monsterUnit.MainGridIndex,
                targetGridIndex,
                gridManager,
                moveAmount
            );

            return -toward;
        }

        protected bool HasPlayerAround8(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            List<int> targets = FindCharacterTargetGridIndices();
            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);

            for (int i = 0; i < targets.Count; i++)
            {
                int gridIndex = targets[i];

                if (gridIndex < 0)
                    continue;

                Vector2Int targetCoord = gridManager.IndexToCoord(gridIndex);
                int dx = Mathf.Abs(targetCoord.x - monsterCoord.x);
                int dy = Mathf.Abs(targetCoord.y - monsterCoord.y);

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
            int targetGridIndex = FindNearestCharacterTargetGridIndex(monsterUnit, gridManager);

            if (targetGridIndex < 0)
                return Vector2Int.zero;

            return GetBestMoveTowardTarget(
                monsterUnit,
                targetGridIndex,
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
            int targetGridIndex = FindNearestCharacterTargetGridIndex(monsterUnit, gridManager);

            if (targetGridIndex < 0)
                return Vector2Int.zero;

            return GetBestMoveAwayFromTarget(
                monsterUnit,
                targetGridIndex,
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

            if (moveOffset.x != 0 && moveOffset.y != 0)
            {
                return CanMonsterMoveAxisOrder(monsterUnit, gridManager, moveOffset, true) ||
                       CanMonsterMoveAxisOrder(monsterUnit, gridManager, moveOffset, false);
            }

            return CanMonsterMoveAxisOrder(
                monsterUnit,
                gridManager,
                moveOffset,
                moveOffset.x != 0);
        }

        private bool CanMonsterMoveAxisOrder(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int moveOffset,
            bool horizontalFirst)
        {
            List<Vector2Int> currentCoords = new();

            for (int i = 0; i < monsterUnit.OccupiedGridIndices.Count; i++)
                currentCoords.Add(gridManager.IndexToCoord(monsterUnit.OccupiedGridIndices[i]));

            if (horizontalFirst)
            {
                return TryApplyMonsterMoveAxisSteps(
                           currentCoords,
                           moveOffset.x,
                           true,
                           monsterUnit,
                           gridManager) &&
                       TryApplyMonsterMoveAxisSteps(
                           currentCoords,
                           moveOffset.y,
                           false,
                           monsterUnit,
                           gridManager);
            }

            return TryApplyMonsterMoveAxisSteps(
                       currentCoords,
                       moveOffset.y,
                       false,
                       monsterUnit,
                       gridManager) &&
                   TryApplyMonsterMoveAxisSteps(
                       currentCoords,
                       moveOffset.x,
                       true,
                       monsterUnit,
                       gridManager);
        }

        private bool TryApplyMonsterMoveAxisSteps(
            List<Vector2Int> currentCoords,
            int amount,
            bool horizontal,
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            int remaining = amount;

            while (remaining != 0)
            {
                int step = remaining > 0 ? 1 : -1;
                List<Vector2Int> nextCoords = new();

                for (int i = 0; i < currentCoords.Count; i++)
                {
                    Vector2Int nextCoord = currentCoords[i] + (horizontal
                        ? new Vector2Int(step, 0)
                        : new Vector2Int(0, step));

                    if (!gridManager.IsValidCoord(nextCoord))
                        return false;

                    int targetIndex = gridManager.CoordToIndex(nextCoord);

                    if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, monsterUnit))
                        return false;

                    BattleGridEffectController gridEffectController =
                        Object.FindFirstObjectByType<BattleGridEffectController>(
                            FindObjectsInactive.Include);

                    if (gridEffectController != null && gridEffectController.IsBlocked(targetIndex))
                        return false;

                    nextCoords.Add(nextCoord);
                }

                currentCoords.Clear();
                currentCoords.AddRange(nextCoords);
                remaining -= step;
            }

            return true;
        }
    }
}
