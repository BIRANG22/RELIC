using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class VespaAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_11";
        private const string AttackSkillId = "S_Monster_12";
        private const int MaxMoveActionCount = 2;

        private static readonly Vector2Int[] MoveOffsets =
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, 2),
            new Vector2Int(0, -2),
            new Vector2Int(-2, 0),
            new Vector2Int(2, 0)
        };

        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private sealed class ReachableState
        {
            public Vector2Int Coord;
            public List<Vector2Int> Steps;
        }

        private sealed class AttackCandidate
        {
            public ReachableState State;
            public int TargetGridIndex;
            public int AttackDistance;
        }

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return AttackSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            if (monsterUnit == null || monsterUnit.RuntimeData == null || gridManager == null)
                return plan;

            if (monsterUnit.MainGridIndex < 0)
                return plan;

            Vector2Int startCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);

            // 예약 1회마다 반복해서 씬을 탐색하지 않도록 필요한 정보를 한 번만 수집합니다.
            BattleGridEffectController gridEffectController =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);
            BattleCharacter[] characters = FindPlayers();
            MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            HashSet<int> occupiedIndices = BuildOccupancySnapshot(
                monsterUnit,
                characters,
                monsters);
            HashSet<int> blockedGridEffectIndices = BuildBlockedGridEffectSnapshot(
                gridManager,
                gridEffectController);
            List<int> targetGridIndices = BuildAliveTargetSnapshot(
                characters,
                gridEffectController);

            if (targetGridIndices.Count <= 0)
                return plan;

            List<Vector2Int> shapeOffsets = BuildMonsterShapeOffsets(monsterUnit, gridManager, startCoord);
            List<ReachableState> reachableStates = BuildReachableStates(
                gridManager,
                startCoord,
                shapeOffsets,
                occupiedIndices,
                blockedGridEffectIndices);

            AttackCandidate attackCandidate = FindBestImmediateAttackCandidate(
                gridManager,
                reachableStates,
                targetGridIndices,
                occupiedIndices,
                blockedGridEffectIndices);

            if (attackCandidate != null)
            {
                AddMoveActions(plan, attackCandidate.State.Steps);

                Vector2Int targetCoord = gridManager.IndexToCoord(attackCandidate.TargetGridIndex);
                BattleDirection attackDirection = GetHorizontalAttackDirection(
                    attackCandidate.State.Coord,
                    targetCoord,
                    monsterUnit.RuntimeData.Direction);
                int projectedGridIndex = gridManager.CoordToIndex(attackCandidate.State.Coord);

                plan.Add(new MonsterAIAction(
                    AttackSkillId,
                    Vector2Int.zero,
                    attackCandidate.State.Steps.Count > 0
                        ? MonsterAISlotPreference.NextSlot
                        : MonsterAISlotPreference.Front,
                    1,
                    attackCandidate.State.Steps.Count,
                    projectedGridIndex,
                    true,
                    attackDirection));

                return plan;
            }

            // 이번 턴 공격이 불가능하면 모든 살아 있는 타겟의 좌/우 진입 공간을 목표로 삼습니다.
            // 전투불능 캐릭터는 타겟에서는 빠지지만 occupiedIndices에는 남아 장애물로 유지됩니다.
            List<Vector2Int> futureAttackOrigins = BuildFutureAttackOrigins(
                gridManager,
                targetGridIndices,
                occupiedIndices,
                blockedGridEffectIndices);

            ReachableState fallbackState = FindBestFallbackState(
                gridManager,
                reachableStates,
                futureAttackOrigins,
                targetGridIndices,
                occupiedIndices,
                blockedGridEffectIndices);

            if (fallbackState != null)
                AddMoveActions(plan, fallbackState.Steps);

            return plan;
        }

        private static HashSet<int> BuildOccupancySnapshot(
            MonsterUnit self,
            BattleCharacter[] characters,
            MonsterUnit[] monsters)
        {
            HashSet<int> occupied = new();

            if (characters != null)
            {
                for (int i = 0; i < characters.Length; i++)
                {
                    BattleCharacter character = characters[i];

                    if (character == null || character.CurrentGridIndex < 0)
                        continue;

                    // 전투불능 캐릭터도 그리드에서 사라지지 않으므로 계속 장애물로 취급합니다.
                    occupied.Add(character.CurrentGridIndex);
                }
            }

            if (monsters != null)
            {
                for (int i = 0; i < monsters.Length; i++)
                {
                    MonsterUnit monster = monsters[i];

                    if (monster == null || monster == self)
                        continue;

                    if (monster.RuntimeData != null && monster.RuntimeData.IsDead)
                        continue;

                    for (int gridIndex = 0; gridIndex < monster.OccupiedGridIndices.Count; gridIndex++)
                    {
                        int occupiedGridIndex = monster.OccupiedGridIndices[gridIndex];

                        if (occupiedGridIndex >= 0)
                            occupied.Add(occupiedGridIndex);
                    }

                    if (monster.OccupiedGridIndices.Count == 0 && monster.MainGridIndex >= 0)
                        occupied.Add(monster.MainGridIndex);
                }
            }

            return occupied;
        }

        private static HashSet<int> BuildBlockedGridEffectSnapshot(
            GridManager gridManager,
            BattleGridEffectController gridEffectController)
        {
            HashSet<int> blocked = new();

            if (gridManager == null || gridEffectController == null)
                return blocked;

            for (int x = 0; x < gridManager.Width; x++)
            {
                for (int y = 0; y < gridManager.Height; y++)
                {
                    Vector2Int coord = new(x, y);
                    int gridIndex = gridManager.CoordToIndex(coord);

                    if (gridEffectController.IsBlocked(gridIndex))
                        blocked.Add(gridIndex);
                }
            }

            return blocked;
        }

        private List<int> BuildAliveTargetSnapshot(
            BattleCharacter[] characters,
            BattleGridEffectController gridEffectController)
        {
            List<int> targets = new();

            if (characters != null)
            {
                for (int i = 0; i < characters.Length; i++)
                {
                    BattleCharacter character = characters[i];

                    if (!IsAlivePlayer(character) || character.CurrentGridIndex < 0)
                        continue;

                    if (!targets.Contains(character.CurrentGridIndex))
                        targets.Add(character.CurrentGridIndex);
                }
            }

            if (gridEffectController != null)
            {
                IReadOnlyList<int> gridEffectTargets =
                    gridEffectController.GetCharacterTargetGridIndices();

                for (int i = 0; i < gridEffectTargets.Count; i++)
                {
                    int gridIndex = gridEffectTargets[i];

                    if (gridIndex >= 0 && !targets.Contains(gridIndex))
                        targets.Add(gridIndex);
                }
            }

            return targets;
        }

        private static List<Vector2Int> BuildMonsterShapeOffsets(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int mainCoord)
        {
            List<Vector2Int> offsets = new();

            for (int i = 0; i < monsterUnit.OccupiedGridIndices.Count; i++)
            {
                int gridIndex = monsterUnit.OccupiedGridIndices[i];

                if (gridIndex < 0)
                    continue;

                offsets.Add(gridManager.IndexToCoord(gridIndex) - mainCoord);
            }

            if (offsets.Count == 0)
                offsets.Add(Vector2Int.zero);

            return offsets;
        }

        private static List<ReachableState> BuildReachableStates(
            GridManager gridManager,
            Vector2Int startCoord,
            List<Vector2Int> shapeOffsets,
            HashSet<int> occupiedIndices,
            HashSet<int> blockedGridEffectIndices)
        {
            List<ReachableState> result = new();
            Dictionary<Vector2Int, ReachableState> bestByCoord = new();

            ReachableState startState = new()
            {
                Coord = startCoord,
                Steps = new List<Vector2Int>()
            };

            result.Add(startState);
            bestByCoord[startCoord] = startState;

            List<ReachableState> frontier = new() { startState };

            for (int depth = 0; depth < MaxMoveActionCount; depth++)
            {
                List<ReachableState> nextFrontier = new();

                for (int stateIndex = 0; stateIndex < frontier.Count; stateIndex++)
                {
                    ReachableState state = frontier[stateIndex];

                    for (int moveIndex = 0; moveIndex < MoveOffsets.Length; moveIndex++)
                    {
                        Vector2Int moveOffset = MoveOffsets[moveIndex];

                        if (!CanMoveFromState(
                                gridManager,
                                state.Coord,
                                shapeOffsets,
                                moveOffset,
                                occupiedIndices,
                                blockedGridEffectIndices))
                        {
                            continue;
                        }

                        Vector2Int destination = state.Coord + moveOffset;
                        int newActionCount = state.Steps.Count + 1;

                        if (bestByCoord.TryGetValue(destination, out ReachableState existing) &&
                            existing.Steps.Count <= newActionCount)
                        {
                            continue;
                        }

                        List<Vector2Int> steps = new(state.Steps)
                        {
                            moveOffset
                        };

                        ReachableState nextState = new()
                        {
                            Coord = destination,
                            Steps = steps
                        };

                        bestByCoord[destination] = nextState;
                        result.Add(nextState);
                        nextFrontier.Add(nextState);
                    }
                }

                frontier = nextFrontier;

                if (frontier.Count == 0)
                    break;
            }

            return result;
        }

        private static bool CanMoveFromState(
            GridManager gridManager,
            Vector2Int originCoord,
            List<Vector2Int> shapeOffsets,
            Vector2Int moveOffset,
            HashSet<int> occupiedIndices,
            HashSet<int> blockedGridEffectIndices)
        {
            if (!IsOneOrTwoTileCardinalMove(moveOffset))
                return false;

            int tileCount = Mathf.Max(Mathf.Abs(moveOffset.x), Mathf.Abs(moveOffset.y));
            Vector2Int unitStep = new(
                moveOffset.x == 0 ? 0 : (moveOffset.x > 0 ? 1 : -1),
                moveOffset.y == 0 ? 0 : (moveOffset.y > 0 ? 1 : -1));

            for (int tileStep = 1; tileStep <= tileCount; tileStep++)
            {
                Vector2Int translatedMainCoord = originCoord + unitStep * tileStep;

                for (int shapeIndex = 0; shapeIndex < shapeOffsets.Count; shapeIndex++)
                {
                    Vector2Int translatedCoord = translatedMainCoord + shapeOffsets[shapeIndex];

                    if (!gridManager.IsValidCoord(translatedCoord))
                        return false;

                    int gridIndex = gridManager.CoordToIndex(translatedCoord);

                    if (occupiedIndices.Contains(gridIndex) || blockedGridEffectIndices.Contains(gridIndex))
                        return false;
                }
            }

            return true;
        }

        private static AttackCandidate FindBestImmediateAttackCandidate(
            GridManager gridManager,
            List<ReachableState> reachableStates,
            List<int> targetGridIndices,
            HashSet<int> occupiedIndices,
            HashSet<int> blockedGridEffectIndices)
        {
            AttackCandidate best = null;

            for (int stateIndex = 0; stateIndex < reachableStates.Count; stateIndex++)
            {
                ReachableState state = reachableStates[stateIndex];

                for (int targetIndex = 0; targetIndex < targetGridIndices.Count; targetIndex++)
                {
                    int targetGridIndex = targetGridIndices[targetIndex];

                    if (targetGridIndex < 0)
                        continue;

                    Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

                    if (!IsHorizontalDashPathClear(
                            gridManager,
                            state.Coord,
                            targetCoord,
                            targetGridIndex,
                            occupiedIndices,
                            blockedGridEffectIndices))
                    {
                        continue;
                    }

                    int attackDistance = Mathf.Abs(targetCoord.x - state.Coord.x);

                    if (best == null ||
                        state.Steps.Count < best.State.Steps.Count ||
                        (state.Steps.Count == best.State.Steps.Count && attackDistance < best.AttackDistance))
                    {
                        best = new AttackCandidate
                        {
                            State = state,
                            TargetGridIndex = targetGridIndex,
                            AttackDistance = attackDistance
                        };
                    }
                }
            }

            return best;
        }

        private static List<Vector2Int> BuildFutureAttackOrigins(
            GridManager gridManager,
            List<int> targetGridIndices,
            HashSet<int> occupiedIndices,
            HashSet<int> blockedGridEffectIndices)
        {
            List<Vector2Int> origins = new();
            HashSet<Vector2Int> unique = new();

            for (int targetIndex = 0; targetIndex < targetGridIndices.Count; targetIndex++)
            {
                int targetGridIndex = targetGridIndices[targetIndex];

                if (targetGridIndex < 0)
                    continue;

                Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

                for (int x = 0; x < gridManager.Width; x++)
                {
                    if (x == targetCoord.x)
                        continue;

                    Vector2Int originCoord = new(x, targetCoord.y);
                    int originGridIndex = gridManager.CoordToIndex(originCoord);

                    if (occupiedIndices.Contains(originGridIndex) ||
                        blockedGridEffectIndices.Contains(originGridIndex))
                    {
                        continue;
                    }

                    if (!IsHorizontalDashPathClear(
                            gridManager,
                            originCoord,
                            targetCoord,
                            targetGridIndex,
                            occupiedIndices,
                            blockedGridEffectIndices))
                    {
                        continue;
                    }

                    if (unique.Add(originCoord))
                        origins.Add(originCoord);
                }
            }

            return origins;
        }

        private static ReachableState FindBestFallbackState(
            GridManager gridManager,
            List<ReachableState> reachableStates,
            List<Vector2Int> futureAttackOrigins,
            List<int> targetGridIndices,
            HashSet<int> occupiedIndices,
            HashSet<int> blockedGridEffectIndices)
        {
            ReachableState best = null;
            int bestPathDistance = int.MaxValue;
            int bestTargetDistance = int.MaxValue;
            int bestOpenSpace = int.MinValue;

            for (int stateIndex = 0; stateIndex < reachableStates.Count; stateIndex++)
            {
                ReachableState state = reachableStates[stateIndex];

                if (state.Steps.Count <= 0)
                    continue;

                int pathDistance = GetShortestPathDistanceToAnyOrigin(
                    gridManager,
                    state.Coord,
                    futureAttackOrigins,
                    occupiedIndices,
                    blockedGridEffectIndices);
                int targetDistance = GetNearestTargetManhattanDistance(
                    gridManager,
                    state.Coord,
                    targetGridIndices);
                int openSpace = GetOpenSpaceScore(
                    gridManager,
                    state.Coord,
                    occupiedIndices,
                    blockedGridEffectIndices);

                bool hasFutureOriginPath = pathDistance != int.MaxValue;
                bool bestHasFutureOriginPath = bestPathDistance != int.MaxValue;

                bool isBetter =
                    best == null ||
                    (hasFutureOriginPath && !bestHasFutureOriginPath) ||
                    (hasFutureOriginPath == bestHasFutureOriginPath && pathDistance < bestPathDistance) ||
                    (hasFutureOriginPath == bestHasFutureOriginPath &&
                     pathDistance == bestPathDistance &&
                     targetDistance < bestTargetDistance) ||
                    (hasFutureOriginPath == bestHasFutureOriginPath &&
                     pathDistance == bestPathDistance &&
                     targetDistance == bestTargetDistance &&
                     openSpace > bestOpenSpace) ||
                    (hasFutureOriginPath == bestHasFutureOriginPath &&
                     pathDistance == bestPathDistance &&
                     targetDistance == bestTargetDistance &&
                     openSpace == bestOpenSpace &&
                     state.Steps.Count < best.Steps.Count);

                if (!isBetter)
                    continue;

                best = state;
                bestPathDistance = pathDistance;
                bestTargetDistance = targetDistance;
                bestOpenSpace = openSpace;
            }

            return best;
        }

        private static int GetShortestPathDistanceToAnyOrigin(
            GridManager gridManager,
            Vector2Int startCoord,
            List<Vector2Int> origins,
            HashSet<int> occupiedIndices,
            HashSet<int> blockedGridEffectIndices)
        {
            if (origins == null || origins.Count <= 0)
                return int.MaxValue;

            HashSet<Vector2Int> goals = new(origins);

            if (goals.Contains(startCoord))
                return 0;

            Queue<Vector2Int> queue = new();
            Queue<int> distances = new();
            HashSet<Vector2Int> visited = new();

            queue.Enqueue(startCoord);
            distances.Enqueue(0);
            visited.Add(startCoord);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int distance = distances.Dequeue();

                for (int directionIndex = 0; directionIndex < CardinalDirections.Length; directionIndex++)
                {
                    Vector2Int next = current + CardinalDirections[directionIndex];

                    if (!gridManager.IsValidCoord(next) || !visited.Add(next))
                        continue;

                    int nextGridIndex = gridManager.CoordToIndex(next);

                    if (occupiedIndices.Contains(nextGridIndex) || blockedGridEffectIndices.Contains(nextGridIndex))
                        continue;

                    if (goals.Contains(next))
                        return distance + 1;

                    queue.Enqueue(next);
                    distances.Enqueue(distance + 1);
                }
            }

            return int.MaxValue;
        }

        private static int GetNearestTargetManhattanDistance(
            GridManager gridManager,
            Vector2Int originCoord,
            List<int> targetGridIndices)
        {
            int best = int.MaxValue;

            for (int i = 0; i < targetGridIndices.Count; i++)
            {
                int targetGridIndex = targetGridIndices[i];

                if (targetGridIndex < 0)
                    continue;

                Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);
                int distance =
                    Mathf.Abs(targetCoord.x - originCoord.x) +
                    Mathf.Abs(targetCoord.y - originCoord.y);

                if (distance < best)
                    best = distance;
            }

            return best;
        }

        private static int GetOpenSpaceScore(
            GridManager gridManager,
            Vector2Int originCoord,
            HashSet<int> occupiedIndices,
            HashSet<int> blockedGridEffectIndices)
        {
            int score = 0;

            for (int directionIndex = 0; directionIndex < CardinalDirections.Length; directionIndex++)
            {
                Vector2Int coord = originCoord;

                for (int step = 0; step < 2; step++)
                {
                    coord += CardinalDirections[directionIndex];

                    if (!gridManager.IsValidCoord(coord))
                        break;

                    int gridIndex = gridManager.CoordToIndex(coord);

                    if (occupiedIndices.Contains(gridIndex) || blockedGridEffectIndices.Contains(gridIndex))
                        break;

                    score++;
                }
            }

            return score;
        }

        private static bool IsHorizontalDashPathClear(
            GridManager gridManager,
            Vector2Int originCoord,
            Vector2Int targetCoord,
            int targetGridIndex,
            HashSet<int> occupiedIndices,
            HashSet<int> blockedGridEffectIndices)
        {
            if (gridManager == null || originCoord.y != targetCoord.y || originCoord.x == targetCoord.x)
                return false;

            int direction = targetCoord.x > originCoord.x ? 1 : -1;

            for (int x = originCoord.x + direction; x != targetCoord.x; x += direction)
            {
                Vector2Int checkCoord = new(x, originCoord.y);

                if (!gridManager.IsValidCoord(checkCoord))
                    return false;

                int checkIndex = gridManager.CoordToIndex(checkCoord);

                if (occupiedIndices.Contains(checkIndex) || blockedGridEffectIndices.Contains(checkIndex))
                    return false;
            }

            // 대상 칸은 살아 있는 캐릭터가 점유하고 있어도 정상 공격 대상입니다.
            return gridManager.IsValidCoord(targetCoord) && targetGridIndex >= 0;
        }

        private static bool IsOneOrTwoTileCardinalMove(Vector2Int moveOffset)
        {
            int distance = Mathf.Max(Mathf.Abs(moveOffset.x), Mathf.Abs(moveOffset.y));
            bool isCardinal = moveOffset.x == 0 || moveOffset.y == 0;
            return isCardinal && distance >= 1 && distance <= 2;
        }

        private static void AddMoveActions(MonsterAIPlan plan, List<Vector2Int> moveSteps)
        {
            if (plan == null || moveSteps == null)
                return;

            for (int i = 0; i < moveSteps.Count; i++)
            {
                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    moveSteps[i],
                    i == 0
                        ? MonsterAISlotPreference.Front
                        : MonsterAISlotPreference.NextSlot,
                    1,
                    i));
            }
        }

        private static BattleDirection GetHorizontalAttackDirection(
            Vector2Int originCoord,
            Vector2Int targetCoord,
            BattleDirection fallback)
        {
            if (targetCoord.x > originCoord.x)
                return BattleDirection.Right;

            if (targetCoord.x < originCoord.x)
                return BattleDirection.Left;

            return fallback;
        }
    }
}
