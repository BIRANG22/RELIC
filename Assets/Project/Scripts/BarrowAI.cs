using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 바로우 AI
    /// - 행동 범위: MonsterMasterData의 AttackRange(Range_20)
    /// - 이동: 가까운 캐릭터가 행동 범위 안에 있으면 상/하/좌/우로 정확히 2칸 거리를 벌립니다.
    /// - 직사: 같은 가로 라인에 캐릭터가 있으면 정면 일직선으로 공격합니다.
    /// - 곡사: 같은 가로 라인에 캐릭터가 없으면 가장 먼 캐릭터의 예약 시점 위치와 주변 8칸을 공격합니다.
    /// </summary>
    public class BarrowAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_29";
        private const string DirectShotSkillId = "S_Monster_30";
        private const string ArcShotSkillId = "S_Monster_31";

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
            Vector2Int moveOffset = Vector2Int.zero;

            int nearbyTargetGridIndex = FindNearestCharacterTargetInActionRange(monsterUnit, gridManager);

            // 캐릭터가 Range_20 안으로 접근했을 때만 거리를 벌립니다.
            if (nearbyTargetGridIndex >= 0)
            {
                moveOffset = GetBestEscapeMove(
                    monsterUnit,
                    gridManager);

                if (moveOffset != Vector2Int.zero)
                {
                    plan.Add(new MonsterAIAction(
                        MoveSkillId,
                        moveOffset,
                        MonsterAISlotPreference.Front,
                        group,
                        0));
                }
            }

            int projectedGridIndex = GetExpectedEscapeGridIndex(
                monsterUnit,
                gridManager,
                moveOffset);

            // 상/하로 다른 라인으로 도망친 경우에는 직사로 다시 전환하지 않고 곡사를 사용합니다.
            // 제자리 또는 좌/우 도망인 경우에만 같은 가로 라인의 캐릭터를 찾아 직사를 예약합니다.
            bool escapedToDifferentLine = moveOffset.y != 0;

            if (!escapedToDifferentLine &&
                TryBuildDirectShot(
                    projectedGridIndex,
                    gridManager,
                    out BattleDirection directDirection,
                    out List<int> directRange))
            {
                // 이동 없이 직사만 예약할 때는 예약 시점의 목표 방향을 바라보게 합니다.
                // 이후 피격 등으로 Facing이 바뀌면 실행 시점의 현재 Facing을 사용합니다.
                if (moveOffset == Vector2Int.zero)
                    SetFacingForReservedAttack(monsterUnit, directDirection);

                plan.Add(new MonsterAIAction(
                    DirectShotSkillId,
                    Vector2Int.zero,
                    moveOffset != Vector2Int.zero
                        ? MonsterAISlotPreference.SameSlot
                        : MonsterAISlotPreference.Front,
                    group,
                    1,
                    projectedGridIndex,
                    moveOffset != Vector2Int.zero,
                    directDirection,
                    false,
                    0,
                    directRange));

                return plan;
            }

            // 같은 가로 라인에 캐릭터가 없다면 가장 먼 캐릭터의 현재 그리드를 중심으로 곡사를 예약합니다.
            BattleCharacter farthestPlayer = FindFarthestPlayerFromGrid(projectedGridIndex, gridManager);

            if (farthestPlayer == null || farthestPlayer.CurrentGridIndex < 0)
                return plan;

            List<int> arcRange = BuildThreeByThreeRange(
                farthestPlayer.CurrentGridIndex,
                gridManager);

            if (arcRange.Count <= 0)
                return plan;

            plan.Add(new MonsterAIAction(
                ArcShotSkillId,
                Vector2Int.zero,
                moveOffset != Vector2Int.zero
                    ? MonsterAISlotPreference.SameSlot
                    : MonsterAISlotPreference.Front,
                group,
                1,
                projectedGridIndex,
                false,
                BattleDirection.Right,
                false,
                0,
                arcRange));

            return plan;
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
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                if (!actionRangeSet.Contains(player.CurrentGridIndex))
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                int distance =
                    Mathf.Abs(playerCoord.x - monsterCoord.x) +
                    Mathf.Abs(playerCoord.y - monsterCoord.y);

                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
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
            int bestMinDistance = int.MinValue;
            int bestTotalDistance = int.MinValue;

            Vector2Int bestCollisionOffset = Vector2Int.zero;
            bool bestCollisionIsHorizontal = false;
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
                        Mathf.Abs(playerCoord.x - currentCoord.x) +
                        Mathf.Abs(playerCoord.y - currentCoord.y);
                    int candidateDistance =
                        Mathf.Abs(playerCoord.x - movedCoord.x) +
                        Mathf.Abs(playerCoord.y - movedCoord.y);

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
                        (isHorizontal && !bestCollisionIsHorizontal) ||
                        (isHorizontal == bestCollisionIsHorizontal && candidateMinDistance > bestCollisionMinDistance) ||
                        (isHorizontal == bestCollisionIsHorizontal &&
                         candidateMinDistance == bestCollisionMinDistance &&
                         candidateTotalDistance > bestCollisionTotalDistance);

                    if (collisionBetter)
                    {
                        bestCollisionIsHorizontal = isHorizontal;
                        bestCollisionMinDistance = candidateMinDistance;
                        bestCollisionTotalDistance = candidateTotalDistance;
                        bestCollisionOffset = offset;
                    }

                    continue;
                }

                // 완전히 비어 있는 도망에서는 어느 캐릭터에게도 가까워지지 않는 후보만 사용합니다.
                if (candidateGetsCloserToAnyPlayer)
                    continue;

                // 직사를 유지할 수 있도록 좌/우 2칸 도망을 최우선으로 합니다.
                // 같은 축 후보끼리는 가장 가까운 캐릭터와의 거리를 먼저 벌리고,
                // 동률이면 모든 캐릭터와의 총거리가 더 큰 방향을 선택합니다.
                bool better =
                    (isHorizontal && !bestIsHorizontal) ||
                    (isHorizontal == bestIsHorizontal && candidateMinDistance > bestMinDistance) ||
                    (isHorizontal == bestIsHorizontal &&
                     candidateMinDistance == bestMinDistance &&
                     candidateTotalDistance > bestTotalDistance);

                if (!better)
                    continue;

                bestIsHorizontal = isHorizontal;
                bestMinDistance = candidateMinDistance;
                bestTotalDistance = candidateTotalDistance;
                bestOffset = offset;
            }

            if (bestOffset != Vector2Int.zero)
                return bestOffset;

            return bestCollisionOffset;
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
            int farthestDistance = -1;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                int distance =
                    Mathf.Abs(playerCoord.x - origin.x) +
                    Mathf.Abs(playerCoord.y - origin.y);

                if (distance <= farthestDistance)
                    continue;

                farthestDistance = distance;
                farthest = player;
            }

            return farthest;
        }

        private static List<int> BuildThreeByThreeRange(
            int centerGridIndex,
            GridManager gridManager)
        {
            List<int> result = new();

            if (centerGridIndex < 0 || gridManager == null)
                return result;

            Vector2Int center = gridManager.IndexToCoord(centerGridIndex);

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    Vector2Int coord = center + new Vector2Int(x, y);

                    if (!gridManager.IsValidCoord(coord))
                        continue;

                    result.Add(gridManager.CoordToIndex(coord));
                }
            }

            return result;
        }
    }
}
