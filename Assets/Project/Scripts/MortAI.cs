using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 모르트(Mon_11) 전용 AI입니다.
    /// - 이동/혼령폭발/피의 저주의 판단 범위는 Monster 시트의 AttackRangeId를 사용합니다.
    /// - 근접, 접근 이동, 플레이어 효과/피해를 감지하면 1칸 도망 이동을 예약합니다.
    /// - 도망이 필요하지 않아도 망령쇄도를 사용할 가로 라인을 잡기 위해 이동할 수 있습니다.
    /// - 망령쇄도는 공격 예상 위치와 같은 라인에 캐릭터가 있을 때만 예약합니다.
    /// - 혼령폭발은 범위와 관계없이 가장 가까운 생존 캐릭터에게 예약합니다.
    /// - 피의 저주는 공격 범위 안에서 가장 먼 생존 캐릭터에게 예약합니다.
    /// - 사령술은 턴 시작 특성으로 처리되므로 여기서는 예약하지 않습니다.
    /// </summary>
    public class MortAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_29";
        private const string WraithRushSkillId = "S_Monster_30";
        private const string SpiritExplosionSkillId = "S_Monster_31";
        private const string BloodCurseSkillId = "S_Monster_32";
        private const string NecromancySkillId = "S_Monster_41";

        // 이전 예약 시점의 생명력을 기록하여 이번 턴에 받은 피해를 판정합니다.
        // 체력이 최대치보다 낮다는 이유만으로 매 턴 도망치지 않습니다.
        private sealed class EscapeDamageSnapshot
        {
            public int LastHP;
            public int TurnCount;
            public bool WasDamaged;
        }

        private static readonly Dictionary<string, EscapeDamageSnapshot> EscapeDamageSnapshots = new();

        private static bool WasDamagedSinceLastPlan(MonsterRuntimeData runtime)
        {
            if (runtime == null || string.IsNullOrEmpty(runtime.RuntimeId))
                return false;

            if (!EscapeDamageSnapshots.TryGetValue(runtime.RuntimeId, out EscapeDamageSnapshot snapshot))
            {
                EscapeDamageSnapshots[runtime.RuntimeId] = new EscapeDamageSnapshot
                {
                    LastHP = runtime.CurrentHP,
                    TurnCount = runtime.TurnCount,
                    WasDamaged = false
                };
                return false;
            }

            if (snapshot.TurnCount != runtime.TurnCount)
            {
                snapshot.WasDamaged = runtime.CurrentHP < snapshot.LastHP;
                snapshot.TurnCount = runtime.TurnCount;
            }
            else if (runtime.CurrentHP < snapshot.LastHP)
            {
                snapshot.WasDamaged = true;
            }

            snapshot.LastHP = runtime.CurrentHP;
            return snapshot.WasDamaged;
        }

        private List<BattleCharacter> FindAdjacentPlayers(int originGridIndex, GridManager gridManager)
        {
            List<BattleCharacter> result = new();
            if (originGridIndex < 0 || gridManager == null)
                return result;

            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
            foreach (BattleCharacter player in FindPlayers())
            {
                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int target = gridManager.IndexToCoord(player.CurrentGridIndex);
                if (Mathf.Max(Mathf.Abs(target.x - origin.x), Mathf.Abs(target.y - origin.y)) <= 1)
                    result.Add(player);
            }
            return result;
        }

        // 직전 예약 당시의 위치를 보관하여 멀리서 접근한 캐릭터도 감지합니다.
        private sealed class ApproachSnapshot
        {
            public int MortGridIndex;
            public readonly Dictionary<string, int> PlayerPositions = new();
        }

        private static readonly Dictionary<string, ApproachSnapshot> ApproachSnapshots = new();

        private bool DidAnyPlayerApproach(MonsterUnit monsterUnit, GridManager gridManager)
        {
            string runtimeId = monsterUnit.RuntimeData.RuntimeId;
            if (string.IsNullOrEmpty(runtimeId))
                return false;

            if (!ApproachSnapshots.TryGetValue(runtimeId, out ApproachSnapshot previous))
            {
                previous = new ApproachSnapshot();
                ApproachSnapshots[runtimeId] = previous;
            }

            int currentMortIndex = monsterUnit.MainGridIndex;
            Vector2Int currentMortCoord = gridManager.IndexToCoord(currentMortIndex);
            bool approached = false;
            Dictionary<string, int> nextPositions = new();
            foreach (BattleCharacter player in FindPlayers())
            {
                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0 ||
                    string.IsNullOrEmpty(player.CharacterId))
                    continue;

                nextPositions[player.CharacterId] = player.CurrentGridIndex;
                if (!previous.PlayerPositions.TryGetValue(player.CharacterId, out int oldIndex) ||
                    oldIndex == player.CurrentGridIndex)
                    continue;

                Vector2Int oldCoord = gridManager.IndexToCoord(oldIndex);
                Vector2Int newCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                // 이전 위치와 현재 위치를 동일한 모르트 위치를 기준으로 비교합니다.
                Vector2Int mortCoord = previous.MortGridIndex >= 0
                    ? gridManager.IndexToCoord(previous.MortGridIndex) : currentMortCoord;
                int before = Mathf.Max(Mathf.Abs(oldCoord.x - mortCoord.x), Mathf.Abs(oldCoord.y - mortCoord.y));
                int after = Mathf.Max(Mathf.Abs(newCoord.x - mortCoord.x), Mathf.Abs(newCoord.y - mortCoord.y));
                if (after < before)
                    approached = true;
            }

            previous.MortGridIndex = currentMortIndex;
            previous.PlayerPositions.Clear();
            foreach (KeyValuePair<string, int> entry in nextPositions)
                previous.PlayerPositions[entry.Key] = entry.Value;
            return approached;
        }

        private static readonly Vector2Int[] EscapeOffsets =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down,
            new Vector2Int(-1, 1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1)
        };

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return WraithRushSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            if (monsterUnit == null ||
                monsterUnit.RuntimeData == null ||
                monsterUnit.RuntimeData.IsDead ||
                monsterUnit.MainGridIndex < 0 ||
                gridManager == null)
            {
                return plan;
            }

            MonsterRuntimeData runtime = monsterUnit.RuntimeData;
            int priority = 0;

            // 사령술은 별도 턴 시작 처리에서 실행합니다.


            bool receivedDamage = WasDamagedSinceLastPlan(runtime);
            bool receivedPlayerEffect = runtime.PendingPlayerEffectEscape;
            bool playerApproached = DidAnyPlayerApproach(monsterUnit, gridManager);
            List<BattleCharacter> adjacentTargets = FindAdjacentPlayers(
                monsterUnit.MainGridIndex, gridManager);

            List<BattleCharacter> currentRangeTargets = FindPlayersInAttackRange(
                monsterUnit.MainGridIndex,
                runtime.AttackRangeId,
                gridManager);

            Vector2Int moveOffset = Vector2Int.zero;
            int attackOriginGridIndex = monsterUnit.MainGridIndex;
            int projectedEscapeGridIndex = monsterUnit.MainGridIndex;

            // 모르트의 도망 이동은 한 턴에 최대 1번만 예약합니다.
            // 접근, 피해/디버프, 주변 8칸 위협 시 앞 슬롯으로 이동합니다.
            // 안전한 후보 중 망령쇄도(30)를 연계할 수 있는 가로 라인을 우선합니다.
            const int maxEscapeMoveCount = 1;
            // 모르트는 피격이나 접근 여부와 관계없이 매 턴 안전한 방향으로 이동합니다.
            // 위협 감지 신호는 유지하여 피해/디버프 추적 상태를 정상적으로 소비합니다.
            bool mustEscape = FindAlivePlayers().Count > 0;

            for (int escapeMoveIndex = 0;
                 escapeMoveIndex < maxEscapeMoveCount &&
                 mustEscape;
                 escapeMoveIndex++)
            {
                // 도망 방향은 근접한 대상만이 아니라 살아 있는 모든 캐릭터를 고려합니다.
                List<BattleCharacter> projectedRangeTargets = FindAlivePlayers();
                if (projectedRangeTargets.Count <= 0)
                    break;

                Vector2Int escapeOffset = ResolveEscapeMove(
                    monsterUnit,
                    projectedEscapeGridIndex,
                    projectedRangeTargets,
                    gridManager);

                if (escapeOffset == Vector2Int.zero)
                {
                    UnityEngine.Debug.LogWarning("[MortAI] 이동 후보 없음: 주변 점유/장애물/맵 경계를 확인하세요.");
                    break;
                }

                // 실제 이동 행동을 예약한 경우에만 효과 반응 플래그를 소비합니다.
                runtime.PendingPlayerEffectEscape = false;
                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    escapeOffset,
                    MonsterAISlotPreference.Front,
                    110,
                    priority++,
                    slotOffset: 0));

                projectedEscapeGridIndex = GetProjectedGridIndex(
                    projectedEscapeGridIndex,
                    escapeOffset,
                    gridManager);

                // 망령쇄도는 앞 슬롯의 첫 도망 이동과 같은 슬롯에서 이어질 수 있으므로
                // 공격 예상 원점은 첫 번째 이동 성공 예상 위치까지만 반영합니다.
                if (escapeMoveIndex == 0)
                {
                    moveOffset = escapeOffset;
                    attackOriginGridIndex = projectedEscapeGridIndex;
                }
            }

            List<BattleCharacter> allAlivePlayers = FindAlivePlayers();

            // 이동 가능한 칸이 없다면 기존 공격 예약을 진행합니다.
            // 이동은 항상 앞 슬롯에 한 번만 예약합니다.

            // 망령쇄도는 모르트의 공격 예상 위치와 같은 가로 라인에
            // 생존 캐릭터가 있을 때만 사용합니다. 같은 라인에 여러 명이 있다면
            // 가장 가까운 대상을 향하도록 예약합니다.
            BattleCharacter wraithRushTarget = FindNearestSameLineTarget(
                attackOriginGridIndex,
                allAlivePlayers,
                gridManager);

            if (wraithRushTarget != null && wraithRushTarget.CurrentGridIndex >= 0)
            {
                BattleDirection wraithRushDirection = ResolveHorizontalDirection(
                    attackOriginGridIndex,
                    wraithRushTarget.CurrentGridIndex,
                    runtime.Direction,
                    gridManager);

                plan.Add(new MonsterAIAction(
                    WraithRushSkillId,
                    Vector2Int.zero,
                    moveOffset != Vector2Int.zero
                        ? MonsterAISlotPreference.SameSlot
                        : MonsterAISlotPreference.Front,
                    moveOffset != Vector2Int.zero ? 110 : -1,
                    priority++,
                    attackOriginGridIndex,
                    true,
                    wraithRushDirection));
            }

            // 혼령폭발은 AttackRange와 관계없이 가장 가까운 생존 캐릭터에게 사용합니다.
            BattleCharacter explosionTarget = FindNearestTarget(
                attackOriginGridIndex,
                allAlivePlayers,
                gridManager);

            if (explosionTarget != null && explosionTarget.CurrentGridIndex >= 0)
            {
                plan.Add(new MonsterAIAction(
                    SpiritExplosionSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Center,
                    -1,
                    priority++,
                    explosionTarget.CurrentGridIndex,
                    true,
                    ResolveHorizontalDirection(attackOriginGridIndex,
                        explosionTarget.CurrentGridIndex, runtime.Direction, gridManager)));
            }

            // 피의 저주는 매 턴 사용하며, 공격 범위로 대상을 제한하지 않습니다.
            // 혼령폭발과 동일한 이동 예상 위치를 기준으로 가장 먼 생존 캐릭터를 선택합니다.
            BattleCharacter curseTarget = FindFarthestTarget(
                attackOriginGridIndex, allAlivePlayers, gridManager);

            if (curseTarget != null && curseTarget.CurrentGridIndex >= 0)
            {
                plan.Add(new MonsterAIAction(
                    BloodCurseSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Back,
                    -1,
                    priority,
                    curseTarget.CurrentGridIndex,
                    true,
                    ResolveHorizontalDirection(attackOriginGridIndex,
                        curseTarget.CurrentGridIndex, runtime.Direction, gridManager),
                    explicitRangeGridIndices: new List<int> { curseTarget.CurrentGridIndex }));
            }

            return plan;
        }


        private List<BattleCharacter> FindAlivePlayers()
        {
            List<BattleCharacter> result = new();
            BattleCharacter[] players = FindPlayers();

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (IsAlivePlayer(player) && player.CurrentGridIndex >= 0)
                    result.Add(player);
            }

            return result;
        }

        private List<BattleCharacter> FindPlayersInAttackRange(
            int originGridIndex,
            string attackRangeId,
            GridManager gridManager)
        {
            List<BattleCharacter> result = new();

            if (originGridIndex < 0 ||
                gridManager == null ||
                string.IsNullOrWhiteSpace(attackRangeId) ||
                attackRangeId.Trim() == "0")
            {
                return result;
            }

            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;

            if (rangeDatabase == null)
                return result;

            List<int> rangeIndices = BattleRangeCalculator.GetSelectionRangeIndices(
                originGridIndex,
                attackRangeId,
                rangeDatabase,
                gridManager);

            if (rangeIndices == null || rangeIndices.Count <= 0)
                return result;

            HashSet<int> rangeSet = new(rangeIndices);
            BattleCharacter[] players = FindPlayers();

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                if (rangeSet.Contains(player.CurrentGridIndex))
                    result.Add(player);
            }

            return result;
        }

        private Vector2Int ResolveEscapeMove(
            MonsterUnit monsterUnit,
            int originGridIndex,
            List<BattleCharacter> threatTargets,
            GridManager gridManager)
        {
            if (monsterUnit == null ||
                originGridIndex < 0 ||
                threatTargets == null ||
                threatTargets.Count <= 0 ||
                gridManager == null)
            {
                return Vector2Int.zero;
            }

            Vector2Int currentCoord = gridManager.IndexToCoord(originGridIndex);
            Vector2Int bestOffset = Vector2Int.zero;
            int bestSafetyTier = int.MinValue;
            int bestRushLine = int.MinValue;
            int bestNearestDistance = int.MinValue;
            int bestTotalDistance = int.MinValue;

            for (int i = 0; i < EscapeOffsets.Length; i++)
            {
                Vector2Int offset = EscapeOffsets[i];

                if (!CanMonsterMoveFromProjectedOrigin(
                        monsterUnit,
                        originGridIndex,
                        gridManager,
                        offset))
                {
                    continue;
                }

                Vector2Int movedCoord = currentCoord + offset;
                int nearestDistance = GetNearestDistance(movedCoord, threatTargets, gridManager);
                int totalDistance = GetTotalDistance(movedCoord, threatTargets, gridManager);

                // 도망은 안전이 우선이지만, 안전한 후보들 사이에서는
                // 망령쇄도(30)를 바로 연계할 수 있는 가로 라인을 우선합니다.
                // 주변 8칸에서 벗어날 수 있다면 다시 인접하는 위치는 제외합니다.
                int safetyTier = nearestDistance > 1 ? 1 : 0;
                int destination = gridManager.CoordToIndex(movedCoord);
                bool canRush = FindNearestSameLineTarget(
                    destination, threatTargets, gridManager) != null;
                int rushLine = canRush ? 1 : 0;

                // 첫 번째 기준은 가장 가까운 캐릭터와의 거리입니다.
                // 거리가 같을 때만 주변 8칸 안전도 및 망령쇄도 연계 가능성을 비교합니다.
                bool better =
                    nearestDistance > bestNearestDistance ||
                    (nearestDistance == bestNearestDistance && safetyTier > bestSafetyTier) ||
                    (nearestDistance == bestNearestDistance && safetyTier == bestSafetyTier &&
                     rushLine > bestRushLine) ||
                    (nearestDistance == bestNearestDistance && safetyTier == bestSafetyTier &&
                     rushLine == bestRushLine && totalDistance > bestTotalDistance);

                if (!better)
                    continue;

                bestSafetyTier = safetyTier;
                bestRushLine = rushLine;
                bestNearestDistance = nearestDistance;
                bestTotalDistance = totalDistance;
                bestOffset = offset;
            }

            return bestOffset;
        }


        // 도망 조건이 없을 때는 공격을 위한 재배치를 선택합니다.
        // 공격할 가로 라인을 만들 수 있는 칸만 선택하고, 인접 위험을 피합니다.
        private Vector2Int ResolveWraithRushPositionMove(
            MonsterUnit monsterUnit, int originGridIndex,
            List<BattleCharacter> targets, GridManager gridManager)
        {
            if (monsterUnit == null || originGridIndex < 0 ||
                gridManager == null || targets == null || targets.Count == 0)
                return Vector2Int.zero;

            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
            Vector2Int bestOffset = Vector2Int.zero;
            int bestSafety = int.MinValue;
            int bestDistance = int.MinValue;

            foreach (Vector2Int offset in EscapeOffsets)
            {
                if (!CanMonsterMoveFromProjectedOrigin(
                        monsterUnit, originGridIndex, gridManager, offset))
                    continue;

                Vector2Int moved = origin + offset;
                int destination = gridManager.CoordToIndex(moved);
                if (FindNearestSameLineTarget(destination, targets, gridManager) == null)
                    continue;

                int distance = GetNearestDistance(moved, targets, gridManager);
                int safety = distance > 1 ? 1 : 0;
                if (safety < bestSafety || (safety == bestSafety && distance <= bestDistance))
                    continue;

                bestSafety = safety;
                bestDistance = distance;
                bestOffset = offset;
            }

            return bestOffset;
        }

        private static int GetProjectedGridIndex(
            int originGridIndex,
            Vector2Int moveOffset,
            GridManager gridManager)
        {
            if (originGridIndex < 0 || gridManager == null || moveOffset == Vector2Int.zero)
                return originGridIndex;

            Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
            Vector2Int projectedCoord = originCoord + moveOffset;

            return gridManager.IsValidCoord(projectedCoord)
                ? gridManager.CoordToIndex(projectedCoord)
                : originGridIndex;
        }

        private static bool CanMonsterMoveFromProjectedOrigin(
            MonsterUnit monsterUnit,
            int projectedMainGridIndex,
            GridManager gridManager,
            Vector2Int moveOffset)
        {
            if (monsterUnit == null ||
                projectedMainGridIndex < 0 ||
                gridManager == null ||
                moveOffset == Vector2Int.zero)
            {
                return false;
            }

            Vector2Int actualMainCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int projectedMainCoord = gridManager.IndexToCoord(projectedMainGridIndex);
            BattleGridEffectController gridEffectController =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            for (int i = 0; i < monsterUnit.OccupiedGridIndices.Count; i++)
            {
                int occupiedGridIndex = monsterUnit.OccupiedGridIndices[i];

                if (occupiedGridIndex < 0)
                    continue;

                Vector2Int actualOccupiedCoord = gridManager.IndexToCoord(occupiedGridIndex);
                Vector2Int footprintOffset = actualOccupiedCoord - actualMainCoord;
                Vector2Int destinationCoord = projectedMainCoord + footprintOffset + moveOffset;

                if (!gridManager.IsValidCoord(destinationCoord))
                    return false;

                int destinationGridIndex = gridManager.CoordToIndex(destinationCoord);

                if (BattleOccupancyService.IsOccupiedByAnyUnit(
                        destinationGridIndex,
                        null,
                        monsterUnit))
                {
                    return false;
                }

                if (gridEffectController != null && gridEffectController.IsBlocked(destinationGridIndex))
                    return false;
            }

            return true;
        }

        private static int GetNearestDistance(
            Vector2Int origin,
            List<BattleCharacter> targets,
            GridManager gridManager)
        {
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                BattleCharacter target = targets[i];

                if (target == null || target.CurrentGridIndex < 0)
                    continue;

                Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex);
                int distance = Manhattan(origin, targetCoord);
                nearestDistance = Mathf.Min(nearestDistance, distance);
            }

            return nearestDistance == int.MaxValue ? 0 : nearestDistance;
        }

        private static int GetTotalDistance(
            Vector2Int origin,
            List<BattleCharacter> targets,
            GridManager gridManager)
        {
            int total = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                BattleCharacter target = targets[i];

                if (target == null || target.CurrentGridIndex < 0)
                    continue;

                total += Manhattan(origin, gridManager.IndexToCoord(target.CurrentGridIndex));
            }

            return total;
        }

        private static BattleCharacter FindNearestTarget(
            int originGridIndex,
            List<BattleCharacter> targets,
            GridManager gridManager)
        {
            if (originGridIndex < 0 || targets == null || gridManager == null)
                return null;

            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
            BattleCharacter best = null;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                BattleCharacter target = targets[i];

                if (target == null || target.CurrentGridIndex < 0)
                    continue;

                int distance = Manhattan(origin, gridManager.IndexToCoord(target.CurrentGridIndex));

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = target;
            }

            return best;
        }


        private static BattleCharacter FindNearestSameLineTarget(
            int originGridIndex,
            List<BattleCharacter> targets,
            GridManager gridManager)
        {
            if (originGridIndex < 0 || targets == null || gridManager == null)
                return null;

            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
            BattleCharacter best = null;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                BattleCharacter target = targets[i];

                if (target == null || target.CurrentGridIndex < 0)
                    continue;

                Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex);

                if (targetCoord.y != origin.y)
                    continue;

                int distance = Mathf.Abs(targetCoord.x - origin.x);

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = target;
            }

            return best;
        }

        private static BattleCharacter FindBestExplosionTarget(
            int originGridIndex,
            List<BattleCharacter> targets,
            GridManager gridManager)
        {
            // 현재는 가장 가까운 공격 가능 대상을 중심점으로 사용합니다.
            // 대상 자체는 예약 시점에 고정되며 실행 단계에서 다시 선택하지 않습니다.
            return FindNearestTarget(originGridIndex, targets, gridManager);
        }

        private BattleCharacter FindFarthestTarget(
            int originGridIndex,
            List<BattleCharacter> targets,
            GridManager gridManager)
        {
            if (originGridIndex < 0 || targets == null || gridManager == null)
                return null;

            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
            BattleCharacter best = null;
            int farthestDistance = int.MinValue;

            foreach (BattleCharacter target in targets)
            {
                if (target == null || target.CurrentGridIndex < 0 ||
                    !IsAlivePlayer(target))
                    continue;

                int distance = Manhattan(origin, gridManager.IndexToCoord(target.CurrentGridIndex));
                if (distance <= farthestDistance)
                    continue;

                farthestDistance = distance;
                best = target;
            }

            return best;
        }

        private static BattleDirection ResolveHorizontalDirection(
            int originGridIndex,
            int targetGridIndex,
            BattleDirection fallbackDirection,
            GridManager gridManager)
        {
            if (originGridIndex < 0 || targetGridIndex < 0 || gridManager == null)
                return fallbackDirection;

            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
            Vector2Int target = gridManager.IndexToCoord(targetGridIndex);

            if (target.x > origin.x)
                return BattleDirection.Right;

            if (target.x < origin.x)
                return BattleDirection.Left;

            return fallbackDirection;
        }

        private static int Manhattan(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
