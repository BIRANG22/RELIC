using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class EliseAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_33";
        // First phase flees only when a player is sufficiently close (Manhattan distance).
        private const int PhaseOneEscapeThreatDistance = 2;
        private const string PhaseTwoMoveSkillId = "S_Monster_38";
        private const string PositionPressureSkillId = "S_Monster_34";
        private const string MeleeAttackSkillId = "S_Monster_35";
        private const string BarrierSkillId = "S_Monster_36";
        private const string SpawnEggSkillId = "S_Monster_37";
        private const string PhaseTwoAttackSkillId01 = "S_Monster_39";
        private const string PhaseTwoAttackSkillId02 = "S_Monster_40";

        private const string BarrierEffectId = "E_Barrier";
        private const string SpiderEggGridEffectId = "GR_spider_egg";
        private const string CinderMonsterId = "Mon_06";

        private static readonly Vector2Int[] MoveOffsets =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down,
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(1, 1),
            Vector2Int.left * 2,
            Vector2Int.right * 2,
            Vector2Int.up * 2,
            Vector2Int.down * 2
        };

        private static readonly Vector2Int[] CardinalOffsets =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down
        };

        private static readonly Vector2Int[] SurroundingOffsets =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down,
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(1, 1)
        };

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return MeleeAttackSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();
            if (monsterUnit == null || monsterUnit.RuntimeData == null || gridManager == null)
                return plan;

            MonsterRuntimeData runtime = monsterUnit.RuntimeData;
            BattleCharacter[] players = FindPlayers();
            int turn = Mathf.Max(1, context != null ? context.CurrentTurn : runtime.TurnCount + 1);

            if (runtime.MonsterId == ArabellaPhaseTransition.PhaseTwoMonsterId)
                CreatePhaseTwoPlan(plan, monsterUnit, players, gridManager);
            else
                CreatePhaseOnePlan(plan, monsterUnit, players, gridManager, turn);

            RemoveSkillsNotOwnedByRuntime(plan, runtime);
            return plan;
        }

        private void CreatePhaseOnePlan(
            MonsterAIPlan plan, MonsterUnit unit, BattleCharacter[] players,
            GridManager gridManager, int turn)
        {
            int priority = 0;
            if (turn % 2 == 0)
            {
                plan.Add(new MonsterAIAction(
                    BarrierSkillId, Vector2Int.zero,
                    MonsterAISlotPreference.Earliest, -1, priority++));
            }

            bool nearby = HasPlayerInAttackRange(unit, players, gridManager);
            if (nearby)
            {
                // 첫 행동인 사방 찢기는 뒤에 예약될 이동 방향을 따라가지 않습니다.
                // 현재 위치에서 가장 가까운 살아있는 캐릭터를 기준으로 방향을 고정합니다.
                BattleCharacter attackTarget = FindNearestPlayer(unit, gridManager);
                bool hasAttackDirection = IsAlivePlayer(attackTarget) &&
                    attackTarget.CurrentGridIndex >= 0 && unit.MainGridIndex >= 0 &&
                    gridManager.IndexToCoord(attackTarget.CurrentGridIndex).x !=
                    gridManager.IndexToCoord(unit.MainGridIndex).x;
                bool attackRight = hasAttackDirection &&
                    gridManager.IndexToCoord(attackTarget.CurrentGridIndex).x >
                    gridManager.IndexToCoord(unit.MainGridIndex).x;
                plan.Add(new MonsterAIAction(
                    MeleeAttackSkillId, Vector2Int.zero,
                    MonsterAISlotPreference.Front, -1, priority++, -1,
                    hasForcedDirection: hasAttackDirection,
                    forcedDirection: attackRight ? BattleDirection.Right : BattleDirection.Left));
            }

            // Do not flee automatically at the beginning of a turn.
            // Only retreat in response to a nearby player; other planned skills remain available.
            Vector2Int escape = Vector2Int.zero;
            if (unit.MainGridIndex >= 0)
            {
                Vector2Int origin = gridManager.IndexToCoord(unit.MainGridIndex);
                int nearestDistance = GetNearestPlayerDistance(origin, players, gridManager);
                if (nearestDistance <= PhaseOneEscapeThreatDistance)
                    escape = ResolveEscapeMove(unit, players, gridManager);
            }
            if (escape != Vector2Int.zero)
            {
                // 이동 예약과 실행이 같은 방향을 사용하도록 좌우 방향을 명시합니다.
                bool hasMoveDirection = escape.x != 0;
                BattleDirection moveDirection = hasMoveDirection
                    ? (escape.x > 0 ? BattleDirection.Right : BattleDirection.Left)
                    : unit.RuntimeData.Direction;
                plan.Add(new MonsterAIAction(
                    MoveSkillId, escape,
                    nearby ? MonsterAISlotPreference.NextSlot : MonsterAISlotPreference.Front,
                    -1, priority++, -1,
                    hasForcedDirection: hasMoveDirection,
                    forcedDirection: moveDirection));
            }

            if (turn % 2 != 0)
                AddSpiderSpawnAction(plan, unit, players, gridManager, priority++);

            AddPositionPressureActions(plan, players, priority);

            // 첫 예약 행동이 이동인 경우, 턴 실행 전에 본체 방향을 맞춥니다.
            // 거미줄 설치 방향이 왼쪽이어도 첫 이동이 오른쪽이면 오른쪽을 바라봅니다.
            // 공격이나 보호막이 선행되는 턴은 그 행동의 방향을 강제로 바꾸지 않습니다.
            // The planner applies facing from the actual earliest reserved slot.
            // Do not alter the sprite while assembling tentative AI actions.
        }

        private static void SetFacingForReservation(MonsterUnit monsterUnit, BattleDirection direction)
        {
            if (monsterUnit == null)
                return;

            BattleUnitFacing facing = monsterUnit.GetComponentInChildren<BattleUnitFacing>(true);
            if (facing != null)
                facing.FaceRight(direction == BattleDirection.Right);

            if (monsterUnit.RuntimeData != null)
                monsterUnit.RuntimeData.Direction = direction;
        }

        private void CreatePhaseTwoPlan(
            MonsterAIPlan plan, MonsterUnit unit, BattleCharacter[] players,
            GridManager gridManager)
        {
            int priority = 0;
            // Reserve the leap target. Physical landing movement must be handled
            // separately in the action executor.
            BattleCharacter nearest = FindNearestPlayer(unit, gridManager);
            int leapGrid = IsAlivePlayer(nearest) ? nearest.CurrentGridIndex : -1;
            if (leapGrid >= 0 && unit.MainGridIndex >= 0)
            {
                Vector2Int from = gridManager.IndexToCoord(unit.MainGridIndex);
                Vector2Int to = gridManager.IndexToCoord(leapGrid);
                bool hasDirection = from.x != to.x;
                plan.Add(new MonsterAIAction(
                    PhaseTwoAttackSkillId01, Vector2Int.zero,
                    MonsterAISlotPreference.FirstTwo, -1, priority++, leapGrid,
                    hasForcedDirection: hasDirection,
                    forcedDirection: to.x > from.x ? BattleDirection.Right : BattleDirection.Left));
            }

            // Move then melee in the same slot (orders 1 and 2).
            int projectedOrigin = leapGrid >= 0 ? leapGrid : unit.MainGridIndex;
            Vector2Int approach = ResolveApproachMove(unit, players, gridManager, projectedOrigin);
            if (approach != Vector2Int.zero)
            {
                const int moveAttackGroup = 1202;
                plan.Add(new MonsterAIAction(
                    PhaseTwoMoveSkillId, approach,
                    MonsterAISlotPreference.NextSlot, moveAttackGroup, priority++));

                if (HasPlayerInAttackRangeAfterMove(unit, players, gridManager, approach, projectedOrigin))
                {
                    Vector2Int projectedCoord = gridManager.IndexToCoord(projectedOrigin) + approach;
                    int attackOrigin = gridManager.CoordToIndex(projectedCoord);
                    plan.Add(new MonsterAIAction(
                        MeleeAttackSkillId, Vector2Int.zero,
                        MonsterAISlotPreference.SameSlot, moveAttackGroup, priority++,
                        attackOrigin, approach.x != 0,
                        approach.x >= 0 ? BattleDirection.Right : BattleDirection.Left));
                }
            }

            BattleCharacter farthest = FindFarthestPlayer(unit, players, gridManager);
            if (IsAlivePlayer(farthest) && farthest.CurrentGridIndex >= 0)
                plan.Add(new MonsterAIAction(
                    PhaseTwoAttackSkillId02, Vector2Int.zero,
                    MonsterAISlotPreference.Center, -1, priority++, farthest.CurrentGridIndex));

            AddPositionPressureActions(plan, players, priority);
        }

        private Vector2Int ResolveApproachMove(
            MonsterUnit unit, BattleCharacter[] players, GridManager gridManager, int originIndex = -1)
        {
            if (unit.MainGridIndex < 0) return Vector2Int.zero;
            int startIndex = originIndex >= 0 ? originIndex : unit.MainGridIndex;
            Vector2Int origin = gridManager.IndexToCoord(startIndex);
            int currentDistance = GetNearestPlayerDistance(origin, players, gridManager);
            int bestDistance = currentDistance;
            Vector2Int best = Vector2Int.zero;
            foreach (Vector2Int offset in CardinalOffsets)
            {
                Vector2Int nextCoord = origin + offset;
                if (!gridManager.IsValidCoord(nextCoord)) continue;
                int nextIndex = gridManager.CoordToIndex(nextCoord);
                if (startIndex == unit.MainGridIndex)
                {
                    if (!CanMonsterMove(unit, gridManager, offset)) continue;
                }
                else
                {
                    if (BattleOccupancyService.IsOccupiedByAnyUnit(nextIndex, null, unit)) continue;
                    BattleGridEffectController controller = Object.FindFirstObjectByType<BattleGridEffectController>(
                        FindObjectsInactive.Include);
                    if (controller != null && controller.IsBlocked(nextIndex)) continue;
                }
                int distance = GetNearestPlayerDistance(nextCoord, players, gridManager);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = offset;
            }
            return best;
        }

        private bool HasPlayerInAttackRangeAfterMove(
            MonsterUnit unit, BattleCharacter[] players, GridManager gridManager,
            Vector2Int offset, int originIndex = -1)
        {
            if (unit.MainGridIndex < 0 || players == null) return false;
            RangeDatabase ranges = DataManager.Instance?.RangeDatabase;
            if (ranges == null) return false;
            Vector2Int destination = gridManager.IndexToCoord(
                originIndex >= 0 ? originIndex : unit.MainGridIndex) + offset;
            if (!gridManager.IsValidCoord(destination)) return false;
            int gridIndex = gridManager.CoordToIndex(destination);
            bool facingRight = offset.x != 0
                ? offset.x > 0
                : unit.RuntimeData.Direction == BattleDirection.Right;
            List<int> indices = MonsterSkillRangeService.BuildRangeGridIndices(
                unit, "Range_18", gridManager, facingRight, gridIndex, ranges);
            if (indices == null) return false;
            foreach (BattleCharacter player in players)
                if (IsAlivePlayer(player) && indices.Contains(player.CurrentGridIndex))
                    return true;
            return false;
        }

        private BattleCharacter FindFarthestPlayer(
            MonsterUnit unit, BattleCharacter[] players, GridManager gridManager)
        {
            if (unit.MainGridIndex < 0 || players == null) return null;
            Vector2Int origin = gridManager.IndexToCoord(unit.MainGridIndex);
            BattleCharacter farthest = null;
            int best = int.MinValue;
            foreach (BattleCharacter player in players)
            {
                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0) continue;
                Vector2Int pos = gridManager.IndexToCoord(player.CurrentGridIndex);
                int distance = Mathf.Abs(origin.x - pos.x) + Mathf.Abs(origin.y - pos.y);
                if (distance <= best) continue;
                best = distance;
                farthest = player;
            }
            return farthest;
        }

        private void AddSpiderSpawnAction(
            MonsterAIPlan plan,
            MonsterUnit monsterUnit,
            BattleCharacter[] players,
            GridManager gridManager,
            int priority)
        {
            // 홀수 턴마다 유효한 설치 칸이 있으면 고치를 심습니다.
            // 이전 턴에 설치한 고치가 남아 있더라도 이번 턴의 설치를 막지 않습니다.
            int eggGridIndex = FindClosestEggGridTowardPlayer(monsterUnit, players, gridManager);
            if (eggGridIndex >= 0)
            {
                plan.Add(new MonsterAIAction(
                    SpawnEggSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Center,
                    -1,
                    priority,
                    eggGridIndex));
            }

        }

        private static void RemoveSkillsNotOwnedByRuntime(
            MonsterAIPlan plan,
            MonsterRuntimeData runtime)
        {
            if (plan?.Actions == null)
                return;

            List<string> candidateSkillIds = new(plan.Actions.Count);
            for (int i = 0; i < plan.Actions.Count; i++)
            {
                MonsterAIAction action = plan.Actions[i];
                if (action != null)
                    candidateSkillIds.Add(action.SkillId);
            }

            HashSet<string> ownedSkillIds = new(
                ArabellaPhaseTransition.FilterOwnedSkills(runtime, candidateSkillIds));

            plan.Actions.RemoveAll(action =>
                action == null || !ownedSkillIds.Contains(action.SkillId));
        }

        private void AddPositionPressureActions(
            MonsterAIPlan plan,
            BattleCharacter[] players,
            int basePriority)
        {
            if (players == null)
                return;

            List<int> targetGridIndices = new();

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                if (!targetGridIndices.Contains(player.CurrentGridIndex))
                    targetGridIndices.Add(player.CurrentGridIndex);
            }

            if (targetGridIndices.Count <= 0)
                return;

            plan.Add(new MonsterAIAction(
                PositionPressureSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Last,
                -1,
                basePriority,
                targetGridIndices[0],
                explicitRangeGridIndices: targetGridIndices));
        }

        // 예약 단계에서 가장 가까운 한 명을 고정해 판단합니다.
        // 다른 캐릭터와의 거리는 도주 방향을 막는 조건으로 사용하지 않습니다.
        public static bool IsStrictEscapePath(
            Vector2Int origin, Vector2Int offset,
            BattleCharacter[] players, GridManager gridManager)
        {
            if (offset == Vector2Int.zero || players == null || gridManager == null)
                return false;

            if (!TryGetNearestPlayerCoord(origin, players, gridManager, out Vector2Int threat))
                return false;

            return IsStrictEscapeAxisOrder(origin, offset, threat, gridManager, true) &&
                   IsStrictEscapeAxisOrder(origin, offset, threat, gridManager, false);
        }

        private static bool IsStrictEscapeAxisOrder(
            Vector2Int origin, Vector2Int offset, Vector2Int threat,
            GridManager gridManager, bool horizontalFirst)
        {
            Vector2Int position = origin;
            int previousDistance = Mathf.Abs(position.x - threat.x) +
                                   Mathf.Abs(position.y - threat.y);
            for (int axis = 0; axis < 2; axis++)
            {
                bool horizontal = axis == 0 ? horizontalFirst : !horizontalFirst;
                int amount = horizontal ? offset.x : offset.y;
                int step = amount > 0 ? 1 : -1;
                for (int i = 0; i < Mathf.Abs(amount); i++)
                {
                    position += horizontal ? new Vector2Int(step, 0) : new Vector2Int(0, step);
                    if (!gridManager.IsValidCoord(position))
                        return false;
                    int distance = Mathf.Abs(position.x - threat.x) +
                                   Mathf.Abs(position.y - threat.y);
                    if (distance <= previousDistance)
                        return false;
                    previousDistance = distance;
                }
            }
            return true;
        }

        private static bool TryGetNearestPlayerCoord(
            Vector2Int origin, BattleCharacter[] players,
            GridManager gridManager, out Vector2Int threat)
        {
            threat = default;
            if (players == null || gridManager == null)
                return false;

            int bestDistance = int.MaxValue;
            int bestGridIndex = int.MaxValue;
            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];
                if (player == null || player.RuntimeData == null ||
                    player.RuntimeData.IsDead || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int candidate = gridManager.IndexToCoord(player.CurrentGridIndex);
                int distance = Mathf.Abs(origin.x - candidate.x) +
                               Mathf.Abs(origin.y - candidate.y);
                if (distance > bestDistance ||
                    (distance == bestDistance && player.CurrentGridIndex >= bestGridIndex))
                    continue;

                bestDistance = distance;
                bestGridIndex = player.CurrentGridIndex;
                threat = candidate;
            }
            return bestDistance != int.MaxValue;
        }

        private Vector2Int ResolveEscapeMove(
            MonsterUnit monsterUnit,
            BattleCharacter[] players,
            GridManager gridManager)
        {
            if (monsterUnit.MainGridIndex < 0)
                return Vector2Int.zero;

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            // 예약 시점의 가장 가까운 캐릭터 한 명을 도주 기준으로 고정합니다.
            if (!TryGetNearestPlayerCoord(currentCoord, players, gridManager, out Vector2Int threat))
                return Vector2Int.zero;
            int currentThreatDistance = ManhattanDistance(currentCoord, threat);

            Vector2Int bestOffset = Vector2Int.zero;
            int bestScore = int.MinValue;
            int bestThreatDistance = int.MinValue;

            for (int i = 0; i < MoveOffsets.Length; i++)
            {
                Vector2Int offset = MoveOffsets[i];

                // 1칸과 2칸 이동을 모두 후보로 두고, 실제 이동 가능한 경로만 평가합니다.
                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                Vector2Int movedCoord = currentCoord + offset;
                int threatDistance = ManhattanDistance(movedCoord, threat);
                // 도착 칸뿐 아니라 이동 중간 칸에서도 거리가 반드시 증가해야 합니다.
                // 대각선은 실제 실행 시 가로/세로 순서가 바뀔 수 있어 양쪽 순서를 검사합니다.
                if (threatDistance <= currentThreatDistance ||
                    !IsStrictEscapePath(currentCoord, offset, players, gridManager))
                    continue;

                int spaceScore = GetEscapeSpaceScore(movedCoord, monsterUnit, gridManager);

                // 가장 가까웠던 목표로부터 멀어지는 정도를 최우선으로 평가합니다.
                // 공간 점수는 같은 거리에서만 보조 판단에 사용합니다.
                int score = threatDistance * 1000 + spaceScore;

                if (score < bestScore)
                    continue;

                if (score == bestScore && threatDistance <= bestThreatDistance)
                    continue;

                bestScore = score;
                bestThreatDistance = threatDistance;
                bestOffset = offset;
            }

            return bestOffset;
        }

        private bool HasPlayerInAttackRange(
            MonsterUnit monsterUnit,
            BattleCharacter[] players,
            GridManager gridManager)
        {
            if (monsterUnit == null ||
                monsterUnit.RuntimeData == null ||
                monsterUnit.MainGridIndex < 0 ||
                players == null ||
                gridManager == null)
            {
                return false;
            }

            string attackRangeId = monsterUnit.RuntimeData.AttackRangeId;

            if (string.IsNullOrWhiteSpace(attackRangeId))
                return false;

            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;

            if (rangeDatabase == null)
                return false;

            bool facingRight = monsterUnit.RuntimeData.Direction == BattleDirection.Right;
            List<int> rangeGridIndices = MonsterSkillRangeService.BuildRangeGridIndices(
                monsterUnit,
                attackRangeId,
                gridManager,
                facingRight,
                monsterUnit.MainGridIndex,
                rangeDatabase);

            if (rangeGridIndices == null || rangeGridIndices.Count <= 0)
                return false;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                if (rangeGridIndices.Contains(player.CurrentGridIndex))
                    return true;
            }

            return false;
        }

        private int GetEscapeSpaceScore(
            Vector2Int originCoord,
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            int immediateOpenCount = 0;
            int validSurroundingCount = 0;

            for (int i = 0; i < SurroundingOffsets.Length; i++)
            {
                Vector2Int candidateCoord = originCoord + SurroundingOffsets[i];

                if (!gridManager.IsValidCoord(candidateCoord))
                    continue;

                validSurroundingCount++;

                int candidateGridIndex = gridManager.CoordToIndex(candidateCoord);

                if (IsEscapeSpaceOpen(candidateGridIndex, monsterUnit))
                    immediateOpenCount++;
            }

            int reachableOpenArea = GetReachableOpenAreaScore(originCoord, monsterUnit, gridManager);

            // 가장자리/모서리는 유효한 주변 칸 자체가 적으므로 자연스럽게 큰 감점을 받습니다.
            return
                immediateOpenCount * 18 +
                reachableOpenArea * 8 +
                validSurroundingCount * 6;
        }

        private int GetReachableOpenAreaScore(
            Vector2Int originCoord,
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            Queue<(Vector2Int coord, int distance)> queue = new();
            HashSet<int> visited = new();

            if (!gridManager.IsValidCoord(originCoord))
                return 0;

            int originIndex = gridManager.CoordToIndex(originCoord);
            queue.Enqueue((originCoord, 0));
            visited.Add(originIndex);

            int openCount = 0;

            while (queue.Count > 0)
            {
                (Vector2Int coord, int distance) = queue.Dequeue();

                if (distance >= 2)
                    continue;

                for (int i = 0; i < CardinalOffsets.Length; i++)
                {
                    Vector2Int nextCoord = coord + CardinalOffsets[i];

                    if (!gridManager.IsValidCoord(nextCoord))
                        continue;

                    int nextIndex = gridManager.CoordToIndex(nextCoord);

                    if (!visited.Add(nextIndex) || !IsEscapeSpaceOpen(nextIndex, monsterUnit))
                        continue;

                    openCount++;
                    queue.Enqueue((nextCoord, distance + 1));
                }
            }

            return openCount;
        }

        private bool IsEscapeSpaceOpen(int gridIndex, MonsterUnit monsterUnit)
        {
            if (gridIndex < 0)
                return false;

            if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex, null, monsterUnit))
                return false;

            BattleGridEffectController controller =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            return controller == null || !controller.IsBlocked(gridIndex);
        }

        private int FindClosestEggGridTowardPlayer(
            MonsterUnit monsterUnit,
            BattleCharacter[] players,
            GridManager gridManager)
        {
            if (monsterUnit == null || gridManager == null)
                return -1;

            // Place the egg on the side facing the closest living character.
            BattleCharacter target = FindNearestPlayer(monsterUnit, gridManager);
            if (!IsAlivePlayer(target) || target.CurrentGridIndex < 0)
                return -1;

            Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex);
            int bestGridIndex = -1;
            int bestTargetDistance = int.MaxValue;
            int bestTotalPlayerDistance = int.MaxValue;
            HashSet<int> checkedGridIndices = new();

            for (int cellIndex = 0; cellIndex < monsterUnit.OccupiedGridIndices.Count; cellIndex++)
            {
                int originIndex = monsterUnit.OccupiedGridIndices[cellIndex];
                if (originIndex < 0)
                    continue;

                Vector2Int originCoord = gridManager.IndexToCoord(originIndex);
                for (int i = 0; i < SurroundingOffsets.Length; i++)
                {
                    Vector2Int candidateCoord = originCoord + SurroundingOffsets[i];
                    if (!gridManager.IsValidCoord(candidateCoord))
                        continue;

                    int candidateGridIndex = gridManager.CoordToIndex(candidateCoord);
                    if (!checkedGridIndices.Add(candidateGridIndex) ||
                        !IsSpawnGridAvailable(candidateGridIndex, gridManager))
                        continue;

                    int targetDistance = ManhattanDistance(candidateCoord, targetCoord);
                    int totalDistance = GetTotalPlayerDistance(candidateCoord, players, gridManager);
                    if (targetDistance > bestTargetDistance ||
                        (targetDistance == bestTargetDistance && totalDistance >= bestTotalPlayerDistance))
                        continue;

                    bestTargetDistance = targetDistance;
                    bestTotalPlayerDistance = totalDistance;
                    bestGridIndex = candidateGridIndex;
                }
            }

            return bestGridIndex;
        }

        private int FindBestWebGridOnPlayerApproachPath(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            BattleCharacter nearestPlayer = FindNearestPlayer(monsterUnit, gridManager);

            if (!IsAlivePlayer(nearestPlayer) || nearestPlayer.CurrentGridIndex < 0)
                return -1;

            int startGridIndex = nearestPlayer.CurrentGridIndex;
            Queue<int> queue = new();
            Dictionary<int, int> parent = new();
            HashSet<int> visited = new();

            queue.Enqueue(startGridIndex);
            visited.Add(startGridIndex);
            parent[startGridIndex] = -1;

            int goalGridIndex = -1;

            while (queue.Count > 0)
            {
                int currentGridIndex = queue.Dequeue();

                if (currentGridIndex != startGridIndex && IsAdjacentToMonster(currentGridIndex, monsterUnit, gridManager))
                {
                    goalGridIndex = currentGridIndex;
                    break;
                }

                Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);

                for (int i = 0; i < CardinalOffsets.Length; i++)
                {
                    Vector2Int nextCoord = currentCoord + CardinalOffsets[i];

                    if (!gridManager.IsValidCoord(nextCoord))
                        continue;

                    int nextGridIndex = gridManager.CoordToIndex(nextCoord);

                    if (visited.Contains(nextGridIndex) || !IsPathCellTraversable(nextGridIndex, startGridIndex))
                        continue;

                    visited.Add(nextGridIndex);
                    parent[nextGridIndex] = currentGridIndex;
                    queue.Enqueue(nextGridIndex);
                }
            }

            if (goalGridIndex < 0)
                return FindDirectPlayerSideWebGrid(nearestPlayer, monsterUnit, gridManager);

            int step = goalGridIndex;

            while (parent.TryGetValue(step, out int previous) && previous >= 0 && previous != startGridIndex)
                step = previous;

            return step != startGridIndex && IsSpawnGridAvailable(step, gridManager)
                ? step
                : FindDirectPlayerSideWebGrid(nearestPlayer, monsterUnit, gridManager);
        }

        private int FindDirectPlayerSideWebGrid(
            BattleCharacter player,
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            if (player == null || player.CurrentGridIndex < 0 || monsterUnit == null || monsterUnit.MainGridIndex < 0)
                return -1;

            Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            int currentDistance = ManhattanDistance(playerCoord, monsterCoord);
            int bestGridIndex = -1;
            int bestDistance = currentDistance;

            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                Vector2Int candidateCoord = playerCoord + CardinalOffsets[i];

                if (!gridManager.IsValidCoord(candidateCoord))
                    continue;

                int candidateGridIndex = gridManager.CoordToIndex(candidateCoord);

                if (!IsSpawnGridAvailable(candidateGridIndex, gridManager))
                    continue;

                int distance = ManhattanDistance(candidateCoord, monsterCoord);

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestGridIndex = candidateGridIndex;
            }

            return bestGridIndex;
        }

        private bool IsPathCellTraversable(int gridIndex, int playerStartGridIndex)
        {
            if (gridIndex == playerStartGridIndex)
                return true;

            if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex))
                return false;

            BattleGridEffectController controller =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            return controller == null || !controller.IsBlocked(gridIndex);
        }

        private static bool IsAdjacentToMonster(
            int gridIndex,
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            if (gridIndex < 0 || monsterUnit == null || gridManager == null)
                return false;

            Vector2Int coord = gridManager.IndexToCoord(gridIndex);

            for (int i = 0; i < monsterUnit.OccupiedGridIndices.Count; i++)
            {
                int occupiedIndex = monsterUnit.OccupiedGridIndices[i];

                if (occupiedIndex < 0)
                    continue;

                Vector2Int occupiedCoord = gridManager.IndexToCoord(occupiedIndex);

                if (ManhattanDistance(coord, occupiedCoord) == 1)
                    return true;
            }

            return false;
        }

        private bool IsSpawnGridAvailable(int gridIndex, GridManager gridManager)
        {
            if (gridIndex < 0 || gridManager.GetCellByIndex(gridIndex) == null)
                return false;

            if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex))
                return false;

            BattleGridEffectController controller =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            if (controller != null && controller.HasEffect(gridIndex))
                return false;

            return true;
        }

        private static bool HasAliveCinder()
        {
            MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < monsters.Length; i++)
            {
                MonsterRuntimeData runtime = monsters[i] != null ? monsters[i].RuntimeData : null;

                if (runtime == null || runtime.IsDead)
                    continue;

                if (string.Equals(runtime.MonsterId, CinderMonsterId, System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private int GetNearestPlayerDistance(
            Vector2Int originCoord,
            BattleCharacter[] players,
            GridManager gridManager)
        {
            if (players == null)
                return int.MaxValue;

            int nearestDistance = int.MaxValue;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                nearestDistance = Mathf.Min(nearestDistance, ManhattanDistance(playerCoord, originCoord));
            }

            return nearestDistance;
        }

        private int GetTotalPlayerDistance(
            Vector2Int originCoord,
            BattleCharacter[] players,
            GridManager gridManager)
        {
            if (players == null)
                return 0;

            int total = 0;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                total += ManhattanDistance(playerCoord, originCoord);
            }

            return total;
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static bool HasStatus(MonsterRuntimeData runtime, string effectId)
        {
            if (runtime == null || runtime.StatusEffects == null || string.IsNullOrWhiteSpace(effectId))
                return false;

            for (int i = 0; i < runtime.StatusEffects.Count; i++)
            {
                StatusEffectRuntimeData status = runtime.StatusEffects[i];

                if (status == null || status.EffectId != effectId)
                    continue;

                if (status.Stack > 0)
                    return true;
            }

            return false;
        }

        private static int CountGridEffects(string gridEffectId)
        {
            if (string.IsNullOrWhiteSpace(gridEffectId))
                return 0;

            BattleGridEffectController controller =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            if (controller == null)
                return 0;

            int count = 0;
            IReadOnlyList<BattleGridEffectPlacement> placements = controller.State.GetPlacements();

            for (int i = 0; i < placements.Count; i++)
            {
                if (placements[i].GridEffectId == gridEffectId)
                    count++;
            }

            return count;
        }
    }

    public static class EliseSlotLockService
    {
        public const string EliseMonsterId = "Mon_12_2";

        public static int RollLockedSlotIndex(
            IReadOnlyList<MonsterUnit> monsterUnits,
            int slotCount)
        {
            if (slotCount <= 0)
                return -1;

            if (!HasAliveElise(monsterUnits))
                return -1;

            return BattleRandom.Range(0, slotCount);
        }

        private static bool HasAliveElise(IReadOnlyList<MonsterUnit> monsterUnits)
        {
            if (monsterUnits == null)
                return false;

            for (int i = 0; i < monsterUnits.Count; i++)
            {
                MonsterUnit monsterUnit = monsterUnits[i];
                MonsterRuntimeData runtime = monsterUnit != null ? monsterUnit.RuntimeData : null;

                if (runtime == null || runtime.IsDead)
                    continue;

                if (runtime.MonsterId == EliseMonsterId)
                    return true;
            }

            return false;
        }
    }
}
