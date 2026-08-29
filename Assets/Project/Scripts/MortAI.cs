using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 모르트(Mon_11) 전용 AI입니다.
    /// - 이동/혼령폭발/피의 저주의 판단 범위는 Monster 시트의 AttackRangeId를 사용합니다.
    /// - AttackRange 안에 캐릭터가 있으면 1칸 도망 이동을 우선 예약합니다.
    /// - 망령쇄도는 공격 예상 위치와 같은 라인에 캐릭터가 있을 때만 예약합니다.
    /// - 혼령폭발은 범위와 관계없이 가장 가까운 생존 캐릭터에게 예약합니다.
    /// - 피의 저주는 공격 범위 안의 대상에게 매 턴 예약합니다.
    /// - 사령술은 부활 가능한 병사가 있을 때 예약합니다.
    /// </summary>
    public class MortAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_37";
        private const string WraithRushSkillId = "S_Monster_38";
        private const string SpiritExplosionSkillId = "S_Monster_39";
        private const string BloodCurseSkillId = "S_Monster_40";
        private const string NecromancySkillId = "S_Monster_41";

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

            // 죽은 병사가 있다면 사령술을 조건부 행동으로 예약합니다.
            if (MortNecromancyTracker.HasReadySoldier(runtime.RuntimeId, runtime.TurnCount))
            {
                plan.Add(new MonsterAIAction(
                    NecromancySkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.FirstTwo,
                    -1,
                    priority++));
            }

            List<BattleCharacter> currentRangeTargets = FindPlayersInAttackRange(
                monsterUnit.MainGridIndex,
                runtime.AttackRangeId,
                gridManager);

            Vector2Int moveOffset = Vector2Int.zero;
            int attackOriginGridIndex = monsterUnit.MainGridIndex;
            int projectedEscapeGridIndex = monsterUnit.MainGridIndex;

            // 모르트의 도망 이동은 한 턴에 최대 1번만 예약합니다.
            // AttackRange 안에 캐릭터가 있다면, 충분히 멀어질 수 없더라도
            // 이동 가능한 칸 중 가장 안전한 칸으로 반드시 1칸 이동을 시도합니다.
            const int maxEscapeMoveCount = 1;

            for (int escapeMoveIndex = 0; escapeMoveIndex < maxEscapeMoveCount; escapeMoveIndex++)
            {
                List<BattleCharacter> projectedRangeTargets = FindPlayersInAttackRange(
                    projectedEscapeGridIndex,
                    runtime.AttackRangeId,
                    gridManager);

                if (projectedRangeTargets.Count <= 0)
                    break;

                Vector2Int escapeOffset = ResolveEscapeMove(
                    monsterUnit,
                    projectedEscapeGridIndex,
                    projectedRangeTargets,
                    gridManager);

                if (escapeOffset == Vector2Int.zero)
                    break;

                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    escapeOffset,
                    MonsterAISlotPreference.Front,
                    110,
                    priority++,
                    slotOffset: escapeMoveIndex));

                projectedEscapeGridIndex = GetProjectedGridIndex(
                    projectedEscapeGridIndex,
                    escapeOffset,
                    gridManager);

                // 망령쇄도는 첫 도망 이동과 같은 슬롯에서 이어질 수 있으므로
                // 공격 예상 원점은 첫 번째 이동 성공 예상 위치까지만 반영합니다.
                if (escapeMoveIndex == 0)
                {
                    moveOffset = escapeOffset;
                    attackOriginGridIndex = projectedEscapeGridIndex;
                }
            }

            List<BattleCharacter> allAlivePlayers = FindAlivePlayers();

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
                    explosionTarget.CurrentGridIndex));
            }

            // 조건부 스킬의 사용 여부는 예약 시작 시점의 Monster AttackRange로 판단합니다.
            // 도망 이동을 먼저 예약했다고 해서 예상 이동 위치 기준으로 Range_19를 다시 계산하면,
            // 원래 범위 안에 있던 캐릭터가 빠져 혼령폭발/피의 저주가 누락될 수 있습니다.
            List<BattleCharacter> attackRangeTargets = currentRangeTargets;

            if (attackRangeTargets.Count <= 0)
                return plan;

            // 피의 저주는 AttackRange 안의 캐릭터 중 현재 체력이 가장 높은 대상에게 사용합니다.
            BattleCharacter curseTarget = FindHighestHPPlayer(attackRangeTargets);

            if (curseTarget != null && curseTarget.CurrentGridIndex >= 0)
            {
                plan.Add(new MonsterAIAction(
                    BloodCurseSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Back,
                    -1,
                    priority,
                    curseTarget.CurrentGridIndex,
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

                // 거리를 실제로 늘릴 수 없는 상황이어도 이동 가능한 칸 자체를 버리지는 않습니다.
                // 가장 가까운 위협과의 거리가 가장 큰 칸을 우선하고,
                // 같다면 모든 위협과의 총 거리가 더 큰 칸을 선택합니다.
                bool better =
                    nearestDistance > bestNearestDistance ||
                    (nearestDistance == bestNearestDistance && totalDistance > bestTotalDistance);

                if (!better)
                    continue;

                bestNearestDistance = nearestDistance;
                bestTotalDistance = totalDistance;
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

        private static BattleCharacter FindHighestHPPlayer(List<BattleCharacter> targets)
        {
            BattleCharacter best = null;
            int bestHP = int.MinValue;

            if (targets == null)
                return null;

            for (int i = 0; i < targets.Count; i++)
            {
                BattleCharacter target = targets[i];

                if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
                    continue;

                int hp = target.RuntimeData.CurrentHP;

                if (hp <= bestHP)
                    continue;

                bestHP = hp;
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
