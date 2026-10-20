using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 바로우 AI
    /// - 행동 범위: MonsterMasterData의 AttackRange(Range_20)
    /// - 이동: 공격 후 주변 8칸에 캐릭터가 있거나 기존 행동 범위 조건을 만족하면 최대 2칸 도주를 시도합니다.
    /// - 직사: 같은 가로 라인에 캐릭터가 있으면 정면 일직선으로 공격합니다.
    /// - 곡사: 같은 가로 라인에 캐릭터가 없으면 가장 먼 캐릭터의 위치를 중심으로 게임데이터의 범위를 공격합니다.
    /// </summary>
    public class BarrowAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_18";
        private const string DirectShotSkillId = "S_Monster_19";
        private const string ArcShotSkillId = "S_Monster_20";

        private static readonly Vector2Int[] EscapeOffsets =
        {
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0),
            new Vector2Int(0, 2),
            new Vector2Int(0, -2)
        };

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return DirectShotSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            if (monsterUnit == null || monsterUnit.RuntimeData == null || gridManager == null)
                return plan;

            const int group = 1;
            int attackOrigin = monsterUnit.MainGridIndex;
            if (attackOrigin < 0)
                return plan;

            // Determine the attack from the current position, before escaping.
            if (TryBuildDirectShot(
                    attackOrigin,
                    gridManager,
                    out BattleDirection directDirection,
                    out List<int> directRange))
            {
                // 직사는 예약 시 목표 방향으로 예고하지만 실행 직전의 피격 방향을 존중합니다.
                SetFacingForReservedAttack(monsterUnit, directDirection);
                plan.Add(new MonsterAIAction(
                    DirectShotSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Front,
                    group,
                    0,
                    attackOrigin,
                    false,
                    directDirection,
                    false,
                    0,
                    directRange));
            }
            else
            {
                BattleCharacter farthestPlayer = FindFarthestPlayerFromGrid(attackOrigin, gridManager);
                if (farthestPlayer == null || farthestPlayer.CurrentGridIndex < 0)
                    return plan;

                List<int> arcRange = BuildArcShotRange(
                    farthestPlayer.CurrentGridIndex,
                    gridManager);
                if (arcRange.Count <= 0)
                    return plan;

                Vector2Int originCoord = gridManager.IndexToCoord(attackOrigin);
                Vector2Int targetCoord = gridManager.IndexToCoord(farthestPlayer.CurrentGridIndex);
                BattleDirection arcDirection = targetCoord.x < originCoord.x
                    ? BattleDirection.Left
                    : targetCoord.x > originCoord.x
                        ? BattleDirection.Right
                        : GetCurrentFacing(monsterUnit);
                SetFacingForReservedAttack(monsterUnit, arcDirection);

                plan.Add(new MonsterAIAction(
                    ArcShotSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Front,
                    group,
                    0,
                    attackOrigin,
                    false,
                    arcDirection,
                    false,
                    0,
                    arcRange));
            }

            // 공격 이후에는 첫 턴을 포함해 매 턴 도망 이동을 시도합니다.
            // 이동 가능한 위치가 있을 때 같은 슬롯의 두 번째 행동으로 예약합니다.
            if (FindPlayers().Length > 0)
            {
                Vector2Int moveOffset = GetBestEscapeMove(monsterUnit, gridManager);
                if (moveOffset != Vector2Int.zero)
                {
                    plan.Add(new MonsterAIAction(
                        MoveSkillId,
                        moveOffset,
                        MonsterAISlotPreference.SameSlot,
                        group,
                        1));
                }
            }

            return plan;
        }

        // 공격을 받지 않았더라도 주변 8칸에 캐릭터가 있으면 도주를 시도합니다.
        private bool HasCharacterInAdjacentEightCells(MonsterUnit monsterUnit, GridManager gridManager)
        {
            if (monsterUnit == null || gridManager == null || monsterUnit.MainGridIndex < 0)
                return false;

            Vector2Int origin = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            BattleCharacter[] players = FindPlayers();
            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];
                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int position = gridManager.IndexToCoord(player.CurrentGridIndex);
                int dx = Mathf.Abs(position.x - origin.x);
                int dy = Mathf.Abs(position.y - origin.y);
                if (dx <= 1 && dy <= 1 && (dx != 0 || dy != 0))
                    return true;
            }

            return false;
        }

        private int FindNearestCharacterTargetInActionRange(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            if (monsterUnit == null ||
                monsterUnit.RuntimeData == null ||
                gridManager == null ||
                monsterUnit.MainGridIndex < 0)
            {
                return -1;
            }

            string actionRangeId = monsterUnit.RuntimeData.AttackRangeId;

            if (string.IsNullOrWhiteSpace(actionRangeId) || actionRangeId.Trim() == "0")
                return -1;

            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;

            if (rangeDatabase == null)
                return -1;

            List<int> actionRange = BattleRangeCalculator.GetSelectionRangeIndices(
                monsterUnit.MainGridIndex,
                actionRangeId,
                rangeDatabase,
                gridManager);

            if (actionRange == null || actionRange.Count <= 0)
                return -1;

            HashSet<int> actionRangeSet = new(actionRange);
            BattleCharacter[] players = FindPlayers();
            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);

            int nearestGridIndex = -1;
            int tiedCount = 0;
            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                if (!actionRangeSet.Contains(player.CurrentGridIndex))
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                if (nearestGridIndex >= 0)
                {
                    int comparison = CompareTargetDistance(monsterCoord, playerCoord,
                        gridManager.IndexToCoord(nearestGridIndex));
                    if (comparison > 0)
                        continue;
                    if (comparison == 0)
                    {
                        tiedCount++;
                        if (BattleRandom.Range(0, tiedCount) != 0)
                            continue;
                    }
                    else
                        tiedCount = 1;
                }
                else
                    tiedCount = 1;

                nearestGridIndex = player.CurrentGridIndex;
            }

            return nearestGridIndex;
        }

        private Vector2Int GetBestEscapeMove(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            if (monsterUnit == null || gridManager == null)
                return Vector2Int.zero;

            BattleCharacter[] players = FindPlayers();
            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);

            Vector2Int bestOffset = Vector2Int.zero;
            bool bestIsHorizontal = false;
            int bestSafetyScore = int.MinValue;
            int bestMinDistance = int.MinValue;
            int bestTotalDistance = int.MinValue;

            Vector2Int bestCollisionOffset = Vector2Int.zero;
            bool bestCollisionIsHorizontal = false;
            int bestCollisionSafetyScore = int.MinValue;
            int bestCollisionMinDistance = int.MinValue;
            int bestCollisionTotalDistance = int.MinValue;

            for (int i = 0; i < EscapeOffsets.Length; i++)
            {
                Vector2Int offset = EscapeOffsets[i];

                if (!CanReserveTwoTileEscape(
                        monsterUnit,
                        gridManager,
                        offset,
                        out bool secondCellStopsEarly))
                {
                    continue;
                }

                Vector2Int step = new(
                    offset.x == 0 ? 0 : (offset.x > 0 ? 1 : -1),
                    offset.y == 0 ? 0 : (offset.y > 0 ? 1 : -1));
                Vector2Int movedCoord = secondCellStopsEarly
                    ? currentCoord + step
                    : currentCoord + offset;
                bool isHorizontal = offset.y == 0;
                int safetyScore = GetEscapeSpaceScore(monsterUnit, gridManager, movedCoord);
                bool candidateGetsCloserToAnyPlayer = false;
                int candidateMinDistance = int.MaxValue;
                int candidateTotalDistance = 0;
                int alivePlayerCount = 0;

                for (int playerIndex = 0; playerIndex < players.Length; playerIndex++)
                {
                    BattleCharacter player = players[playerIndex];

                    if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                        continue;

                    alivePlayerCount++;
                    Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);

                    int currentDistance =
                        TargetGridDistance(playerCoord, currentCoord);
                    int candidateDistance =
                        TargetGridDistance(playerCoord, movedCoord);

                    // 완전히 비어 있는 도망은 어느 캐릭터에게도 가까워지지 않는 방향을 우선합니다.
                    // 두 번째 칸 충돌 후보도 전체 거리 점수는 끝까지 계산해야 합니다.
                    if (candidateDistance < currentDistance)
                        candidateGetsCloserToAnyPlayer = true;

                    candidateMinDistance = Mathf.Min(candidateMinDistance, candidateDistance);
                    candidateTotalDistance += candidateDistance;
                }

                if (alivePlayerCount <= 0)
                    continue;

                // 두 번째 칸에 유닛이 있는 2칸 도망은 첫 칸까지 이동한 뒤 충돌하는
                // 비상 탈출 후보로 남깁니다. 완전히 비어 있는 안전한 도망이 있으면
                // 그것을 우선하고, 없을 때만 이 후보를 사용합니다.
                if (secondCellStopsEarly)
                {
                    bool collisionBetter =
                        safetyScore > bestCollisionSafetyScore ||
                        (safetyScore == bestCollisionSafetyScore && isHorizontal && !bestCollisionIsHorizontal) ||
                        (safetyScore == bestCollisionSafetyScore && isHorizontal == bestCollisionIsHorizontal &&
                         candidateMinDistance > bestCollisionMinDistance) ||
                        (safetyScore == bestCollisionSafetyScore && isHorizontal == bestCollisionIsHorizontal &&
                         candidateMinDistance == bestCollisionMinDistance &&
                         candidateTotalDistance > bestCollisionTotalDistance);

                    if (collisionBetter)
                    {
                        bestCollisionSafetyScore = safetyScore;
                        bestCollisionIsHorizontal = isHorizontal;
                        bestCollisionMinDistance = candidateMinDistance;
                        bestCollisionTotalDistance = candidateTotalDistance;
                        bestCollisionOffset = offset;
                    }

                    continue;
                }

                // 이동 가능한 도주 후보는 캐릭터에게 가까워지더라도 제외하지 않습니다.
                // 충돌하는 이동보다 정상적으로 빠져나갈 수 있는 방향을 우선하고,
                // 가까워지는 정도는 후보 사이의 점수 비교에만 반영합니다.
                if (candidateGetsCloserToAnyPlayer)
                    safetyScore -= 80;

                // 막다른 길을 피하는 것이 공격 방향 유지보다 우선입니다.
                // 탈출 공간이 같을 때만 좌/우 방향, 캐릭터와의 거리를 비교합니다.
                bool better =
                    safetyScore > bestSafetyScore ||
                    (safetyScore == bestSafetyScore && isHorizontal && !bestIsHorizontal) ||
                    (safetyScore == bestSafetyScore && isHorizontal == bestIsHorizontal &&
                     candidateMinDistance > bestMinDistance) ||
                    (safetyScore == bestSafetyScore && isHorizontal == bestIsHorizontal &&
                     candidateMinDistance == bestMinDistance &&
                     candidateTotalDistance > bestTotalDistance);

                if (!better)
                    continue;

                bestSafetyScore = safetyScore;
                bestIsHorizontal = isHorizontal;
                bestMinDistance = candidateMinDistance;
                bestTotalDistance = candidateTotalDistance;
                bestOffset = offset;
            }

            if (bestOffset != Vector2Int.zero)
                return bestOffset;

            return bestCollisionOffset;
        }

        // 도주 도착 지점의 실제 탈출 가능성을 평가합니다.
        // 장애물뿐 아니라 캐릭터 점유 칸, 캐릭터에 의해 좁아진 통로도 반영합니다.
        private int GetEscapeSpaceScore(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int destination)
        {
            BattleGridEffectController effects =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);
            BattleCharacter[] players = FindPlayers();
            Vector2Int original = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            HashSet<Vector2Int> visited = new HashSet<Vector2Int> { destination };
            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            frontier.Enqueue(destination);
            int immediateExits = 0;
            int secondStepExits = 0;
            int safeImmediateExits = 0;
            int nearbyPlayers = 0;
            int closestPlayerDistance = int.MaxValue;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];
                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                int distance = Mathf.Max(Mathf.Abs(playerCoord.x - destination.x),
                    Mathf.Abs(playerCoord.y - destination.y));
                closestPlayerDistance = Mathf.Min(closestPlayerDistance, distance);
                if (distance <= 1)
                    nearbyPlayers++;
            }

            for (int depth = 0; depth < 2; depth++)
            {
                int levelCount = frontier.Count;
                for (int i = 0; i < levelCount; i++)
                {
                    Vector2Int origin = frontier.Dequeue();
                    for (int direction = 0; direction < EscapeOffsets.Length; direction++)
                    {
                        Vector2Int delta = EscapeOffsets[direction] / 2;
                        Vector2Int next = origin + delta;
                        if (!gridManager.IsValidCoord(next) || visited.Contains(next))
                            continue;

                        // 원래 서 있던 칸을 탈출 경로로 계산하면 구석의 탈출 가능성을 과대평가합니다.
                        if (next == original)
                            continue;

                        int index = gridManager.CoordToIndex(next);
                        if (BattleOccupancyService.IsOccupiedByAnyUnit(index, null, monsterUnit) ||
                            (effects != null && effects.IsBlocked(index)))
                            continue;

                        visited.Add(next);
                        if (depth == 0)
                        {
                            immediateExits++;
                            bool adjacentToPlayer = false;
                            for (int p = 0; p < players.Length; p++)
                            {
                                BattleCharacter player = players[p];
                                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                                    continue;
                                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                                if (Mathf.Max(Mathf.Abs(playerCoord.x - next.x),
                                    Mathf.Abs(playerCoord.y - next.y)) <= 1)
                                {
                                    adjacentToPlayer = true;
                                    break;
                                }
                            }
                            if (!adjacentToPlayer)
                                safeImmediateExits++;
                        }
                        else
                        {
                            secondStepExits++;
                        }
                        frontier.Enqueue(next);
                    }
                }
            }

            // 탈출 방향이 하나도 없거나 캐릭터에 둘러싸인 지점은 강하게 감점합니다.
            int score = immediateExits * 12 + safeImmediateExits * 14 + secondStepExits * 2;
            if (immediateExits == 0)
                score -= 100;
            else if (immediateExits == 1)
                score -= 28;
            if (safeImmediateExits == 0)
                score -= 30;

            // A corner with its only exits controlled by a nearby character is a trap.
            // Do not let the second-step open-space bonus outweigh that risk.
            // Keep it as a last-resort candidate if no safer escape exists.
            if (nearbyPlayers > 0 && safeImmediateExits == 0)
                score -= 2000;
            else if (nearbyPlayers > 0 && immediateExits <= 1)
                score -= 1000;

            score -= nearbyPlayers * 35;
            if (closestPlayerDistance == 2)
                score -= 8;
            return score;
        }

        private bool CanReserveTwoTileEscape(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int moveOffset,
            out bool secondCellStopsEarly)
        {
            secondCellStopsEarly = false;

            if (monsterUnit == null || gridManager == null)
                return false;

            bool horizontal = moveOffset.y == 0 && Mathf.Abs(moveOffset.x) == 2;
            bool vertical = moveOffset.x == 0 && Mathf.Abs(moveOffset.y) == 2;

            if (!horizontal && !vertical)
                return false;

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int step = new(
                moveOffset.x == 0 ? 0 : (moveOffset.x > 0 ? 1 : -1),
                moveOffset.y == 0 ? 0 : (moveOffset.y > 0 ? 1 : -1));

            Vector2Int firstCoord = currentCoord + step;
            Vector2Int secondCoord = firstCoord + step;

            if (!gridManager.IsValidCoord(firstCoord))
                return false;

            int firstTargetIndex = gridManager.CoordToIndex(firstCoord);

            // 첫 칸이 막혀 있으면 출발 자체를 할 수 없으므로 후보에서 제외합니다.
            if (BattleOccupancyService.IsOccupiedByAnyUnit(firstTargetIndex, null, monsterUnit))
                return false;

            BattleGridEffectController gridEffectController =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            if (gridEffectController != null && gridEffectController.IsBlocked(firstTargetIndex))
                return false;

            // 두 번째 칸이 맵 밖이면 2칸 이동 자체는 예약합니다.
            // 실행 시 첫 번째 칸까지 이동한 뒤 맵 경계에 막혀 종료됩니다.
            if (!gridManager.IsValidCoord(secondCoord))
            {
                secondCellStopsEarly = true;
                return true;
            }

            int secondTargetIndex = gridManager.CoordToIndex(secondCoord);

            // 잔해/고정 장애물은 예약 단계에서 피합니다. 두 번째 칸의 유닛만
            // 실행 시 1칸 이동 후 충돌할 수 있도록 허용합니다.
            if (gridEffectController != null && gridEffectController.IsBlocked(secondTargetIndex))
                return false;

            secondCellStopsEarly =
                BattleOccupancyService.IsOccupiedByAnyUnit(secondTargetIndex, null, monsterUnit);

            return true;
        }

        private int GetExpectedEscapeGridIndex(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int moveOffset)
        {
            if (monsterUnit == null || gridManager == null || monsterUnit.MainGridIndex < 0)
                return -1;

            if (moveOffset == Vector2Int.zero)
                return monsterUnit.MainGridIndex;

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int step = new(
                moveOffset.x == 0 ? 0 : (moveOffset.x > 0 ? 1 : -1),
                moveOffset.y == 0 ? 0 : (moveOffset.y > 0 ? 1 : -1));
            Vector2Int firstCoord = currentCoord + step;

            if (!gridManager.IsValidCoord(firstCoord))
                return monsterUnit.MainGridIndex;

            Vector2Int secondCoord = firstCoord + step;

            if (!gridManager.IsValidCoord(secondCoord))
                return gridManager.CoordToIndex(firstCoord);

            int secondTargetIndex = gridManager.CoordToIndex(secondCoord);
            bool secondCellOccupied =
                BattleOccupancyService.IsOccupiedByAnyUnit(secondTargetIndex, null, monsterUnit);

            return secondCellOccupied
                ? gridManager.CoordToIndex(firstCoord)
                : secondTargetIndex;
        }

        private bool TryBuildDirectShot(
            int originGridIndex,
            GridManager gridManager,
            out BattleDirection direction,
            out List<int> rangeGridIndices)
        {
            direction = BattleDirection.Right;
            rangeGridIndices = new List<int>();

            if (originGridIndex < 0 || gridManager == null)
                return false;

            BattleCharacter[] players = FindPlayers();
            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);

            BattleCharacter bestTarget = null;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);

                if (playerCoord.y != origin.y || playerCoord.x == origin.x)
                    continue;

                int distance = Mathf.Abs(playerCoord.x - origin.x);

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = player;
            }

            if (bestTarget == null)
                return false;

            Vector2Int targetCoord = gridManager.IndexToCoord(bestTarget.CurrentGridIndex);
            int horizontalSign = targetCoord.x > origin.x ? 1 : -1;

            direction = horizontalSign > 0
                ? BattleDirection.Right
                : BattleDirection.Left;

            rangeGridIndices = BuildHorizontalLineToGridEdge(
                originGridIndex,
                gridManager,
                horizontalSign);

            return rangeGridIndices.Count > 0;
        }


        private static BattleDirection GetCurrentFacing(MonsterUnit monsterUnit)
        {
            return monsterUnit != null && monsterUnit.RuntimeData != null &&
                   monsterUnit.RuntimeData.Direction == BattleDirection.Left
                ? BattleDirection.Left
                : BattleDirection.Right;
        }

        private static void SetFacingForReservedAttack(
            MonsterUnit monsterUnit,
            BattleDirection direction)
        {
            if (monsterUnit == null)
                return;

            BattleUnitFacing facing = monsterUnit.GetComponent<BattleUnitFacing>();
            bool faceRight = direction == BattleDirection.Right;

            if (facing != null)
                facing.FaceRight(faceRight);

            if (monsterUnit.RuntimeData != null)
                monsterUnit.RuntimeData.Direction = direction;
        }

        private static List<int> BuildHorizontalLineToGridEdge(
            int originGridIndex,
            GridManager gridManager,
            int horizontalSign)
        {
            List<int> result = new();
            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
            int step = 1;

            while (true)
            {
                Vector2Int coord = origin + new Vector2Int(horizontalSign * step, 0);

                if (!gridManager.IsValidCoord(coord))
                    break;

                result.Add(gridManager.CoordToIndex(coord));
                step++;
            }

            return result;
        }

        private BattleCharacter FindFarthestPlayerFromGrid(
            int originGridIndex,
            GridManager gridManager)
        {
            if (originGridIndex < 0 || gridManager == null)
                return null;

            BattleCharacter[] players = FindPlayers();
            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);

            BattleCharacter farthest = null;
            int tiedCount = 0;
            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                if (farthest != null)
                {
                    int comparison = CompareTargetDistance(origin, playerCoord,
                        gridManager.IndexToCoord(farthest.CurrentGridIndex));
                    if (comparison < 0)
                        continue;
                    if (comparison == 0)
                    {
                        tiedCount++;
                        if (BattleRandom.Range(0, tiedCount) != 0)
                            continue;
                    }
                    else
                        tiedCount = 1;
                }
                else
                    tiedCount = 1;

                farthest = player;
            }

            return farthest;
        }

        private static List<int> BuildArcShotRange(
            int centerGridIndex,
            GridManager gridManager)
        {
            if (centerGridIndex < 0 || gridManager == null)
                return new List<int>();

            // Read the area-of-effect offsets from the game data rather than
            // constructing a fixed 3x3 square.
            const string arcShotRangeId = "Range_m_02";
            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;
            return BattleRangeCalculator.GetSelectionRangeIndices(
                centerGridIndex,
                arcShotRangeId,
                rangeDatabase,
                gridManager);
        }
    }
}
