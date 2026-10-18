using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class RancorAI : MonsterAIBase
    {
        private const string AttackSkillId = "S_Monster_06";
        private const string BuffSkillId = "S_Monster_07";
        private const string MoveSkillId = "S_Monster_05";

        private static readonly Vector2Int[] EscapeOffsets =
        {
            new Vector2Int(0, 2),
            new Vector2Int(0, -2),
            new Vector2Int(-2, 0),
            new Vector2Int(2, 0)
        };

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return BuffSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            if (monsterUnit == null || monsterUnit.RuntimeData == null || gridManager == null)
                return plan;

            // 행동 예약으로 플립이 확정되면, 광역 스킬도 그 방향을 유지합니다.
            BattleDirection originalDirection = monsterUnit.RuntimeData.Direction;

            int targetGridIndex = FindNearestCharacterTargetGridIndex(monsterUnit, gridManager);
            if (targetGridIndex < 0)
            {
                AddAreaEffectAction(plan);
                return plan;
            }

            // 플레이어 효과를 받았거나 주변 8칸에 캐릭터가 있으면 공격 후 도주합니다.
            // 주변 8칸은 대각선을 포함하며, 공격 스킬의 사거리와는 별개입니다.
            bool wasAffectedByPlayer = monsterUnit.RuntimeData.PendingPlayerEffectEscape;
            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            bool hasAdjacentCharacter = false;
            int adjacentTargetGridIndex = -1;
            int closestAdjacentDistance = int.MaxValue;
            List<int> characterGridIndices = FindCharacterTargetGridIndices();
            for (int i = 0; i < characterGridIndices.Count; i++)
            {
                int characterGridIndex = characterGridIndices[i];
                if (characterGridIndex < 0 || gridManager.GetCellByIndex(characterGridIndex) == null)
                    continue;

                Vector2Int characterCoord = gridManager.IndexToCoord(characterGridIndex);
                int dx = Mathf.Abs(characterCoord.x - monsterCoord.x);
                int dy = Mathf.Abs(characterCoord.y - monsterCoord.y);
                if (dx <= 1 && dy <= 1 && (dx != 0 || dy != 0))
                {
                    hasAdjacentCharacter = true;
                    // 주변에서 실제로 감지한 캐릭터를 도주 방향 계산에 사용합니다.
                    int distance = dx + dy;
                    if (distance < closestAdjacentDistance)
                    {
                        closestAdjacentDistance = distance;
                        adjacentTargetGridIndex = characterGridIndex;
                    }
                }
            }

            if (!wasAffectedByPlayer && !hasAdjacentCharacter)
            {
                AddAreaEffectAction(plan);
                return plan;
            }

            // 도주 방향을 예약 시 미리 표시하지만 공격은 실행 시점의 현재 방향을 유지합니다.
            Vector2Int moveOffset = GetBestTwoTileEscapeMove(
                monsterUnit,
                hasAdjacentCharacter ? adjacentTargetGridIndex : targetGridIndex,
                gridManager);

            // 세로 이동에는 좌우 방향이 없으므로 기존 방향을 유지합니다.
            BattleDirection escapeDirection = moveOffset.x < 0
                ? BattleDirection.Left
                : moveOffset.x > 0
                    ? BattleDirection.Right
                    : originalDirection;

            if (moveOffset.x != 0)
                SetFacingForReservation(monsterUnit, escapeDirection);

            // 광역 버프/디버프에는 방향을 강제하지 않습니다.
            AddAreaEffectAction(plan);

            const int attackEscapeGroup = 1;
            // 공격은 좌우 대칭이며 피격으로 변경된 현재 방향을 유지합니다.
            plan.Add(new MonsterAIAction(
                AttackSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Earliest,
                attackEscapeGroup,
                0
            ));

            if (moveOffset != Vector2Int.zero)
            {
                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    moveOffset,
                    MonsterAISlotPreference.SameSlot,
                    attackEscapeGroup,
                    1,
                    hasForcedDirection: true,
                    forcedDirection: escapeDirection
                ));
                monsterUnit.RuntimeData.PendingPlayerEffectEscape = false;
            }

            return plan;
        }

        private static void AddAreaEffectAction(MonsterAIPlan plan)
        {
            // 현재 바라보는 방향으로 광역 효과를 사용하며 방향을 강제하지 않습니다.
            plan.Add(new MonsterAIAction(
                BuffSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Back,
                -1,
                10
            ));
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

        private Vector2Int GetBestTwoTileEscapeMove(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            GridManager gridManager)
        {
            if (monsterUnit == null ||
                gridManager == null ||
                monsterUnit.MainGridIndex < 0 ||
                targetGridIndex < 0)
            {
                return Vector2Int.zero;
            }

            BattleGridEffectController gridEffectController =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            // 예약 시점에는 현재 비어 있는 탈출 경로를 먼저 선택합니다.
            // 현재 유닛이 있는 경로는 다른 빈 경로가 없을 때만 충돌 가능 후보로 사용합니다.
            if (TryFindBestEscapeMove(
                    monsterUnit,
                    targetGridIndex,
                    gridManager,
                    gridEffectController,
                    false,
                    out Vector2Int bestUnoccupiedOffset))
            {
                return bestUnoccupiedOffset;
            }

            if (TryFindBestEscapeMove(
                    monsterUnit,
                    targetGridIndex,
                    gridManager,
                    gridEffectController,
                    true,
                    out Vector2Int bestOccupiedOffset))
            {
                return bestOccupiedOffset;
            }

            return Vector2Int.zero;
        }

        private static bool TryFindBestEscapeMove(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            GridManager gridManager,
            BattleGridEffectController gridEffectController,
            bool requireCurrentlyOccupiedPath,
            out Vector2Int bestOffset)
        {
            bestOffset = Vector2Int.zero;

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

            int currentDistance =
                Mathf.Abs(targetCoord.x - currentCoord.x) +
                Mathf.Abs(targetCoord.y - currentCoord.y);

            Vector2Int bestSafeEscapeOffset = Vector2Int.zero;
            int bestSafeEscapeDistance = currentDistance;
            int bestSafeEscapeSpaceScore = int.MinValue;
            Vector2Int bestSafeFallbackOffset = Vector2Int.zero;
            int bestSafeFallbackSpaceScore = int.MinValue;

            Vector2Int bestRiskyEscapeOffset = Vector2Int.zero;
            int bestRiskyEscapeDistance = currentDistance;
            int bestRiskyEscapeSpaceScore = int.MinValue;
            Vector2Int bestRiskyFallbackOffset = Vector2Int.zero;
            int bestRiskyFallbackSpaceScore = int.MinValue;

            for (int i = 0; i < EscapeOffsets.Length; i++)
            {
                Vector2Int offset = EscapeOffsets[i];

                // 맵 밖이나 통과 불가 그리드 효과처럼 실행 전에도 변하지 않는 장애물은 제외합니다.
                if (!CanReserveEscapeMove(
                        monsterUnit,
                        gridManager,
                        offset,
                        gridEffectController))
                {
                    continue;
                }

                bool isCurrentlyOccupiedPath = IsEscapePathCurrentlyOccupied(
                    monsterUnit,
                    gridManager,
                    offset);

                if (isCurrentlyOccupiedPath != requireCurrentlyOccupiedPath)
                    continue;

                Vector2Int movedCoord = currentCoord + offset;
                int distance =
                    Mathf.Abs(targetCoord.x - movedCoord.x) +
                    Mathf.Abs(targetCoord.y - movedCoord.y);

                bool isRiskyMove = IsRiskyEscapeMove(
                    monsterUnit,
                    gridManager,
                    offset,
                    gridEffectController);

                int openSpaceScore = GetEscapeOpenSpaceScore(
                    monsterUnit,
                    gridManager,
                    offset,
                    gridEffectController);

                if (!isRiskyMove)
                {
                    if (distance > bestSafeEscapeDistance ||
                        (distance == bestSafeEscapeDistance &&
                         distance > currentDistance &&
                         openSpaceScore > bestSafeEscapeSpaceScore))
                    {
                        bestSafeEscapeDistance = distance;
                        bestSafeEscapeSpaceScore = openSpaceScore;
                        bestSafeEscapeOffset = offset;
                        continue;
                    }

                    if (distance == currentDistance && openSpaceScore > bestSafeFallbackSpaceScore)
                    {
                        bestSafeFallbackSpaceScore = openSpaceScore;
                        bestSafeFallbackOffset = offset;
                    }

                    continue;
                }

                if (distance > bestRiskyEscapeDistance ||
                    (distance == bestRiskyEscapeDistance &&
                     distance > currentDistance &&
                     openSpaceScore > bestRiskyEscapeSpaceScore))
                {
                    bestRiskyEscapeDistance = distance;
                    bestRiskyEscapeSpaceScore = openSpaceScore;
                    bestRiskyEscapeOffset = offset;
                    continue;
                }

                if (distance == currentDistance && openSpaceScore > bestRiskyFallbackSpaceScore)
                {
                    bestRiskyFallbackSpaceScore = openSpaceScore;
                    bestRiskyFallbackOffset = offset;
                }
            }

            if (bestSafeEscapeOffset != Vector2Int.zero)
            {
                bestOffset = bestSafeEscapeOffset;
                return true;
            }

            if (bestSafeFallbackOffset != Vector2Int.zero)
            {
                bestOffset = bestSafeFallbackOffset;
                return true;
            }

            if (bestRiskyEscapeOffset != Vector2Int.zero)
            {
                bestOffset = bestRiskyEscapeOffset;
                return true;
            }

            if (bestRiskyFallbackOffset != Vector2Int.zero)
            {
                bestOffset = bestRiskyFallbackOffset;
                return true;
            }

            return false;
        }

        private static bool IsEscapePathCurrentlyOccupied(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int moveOffset)
        {
            if (monsterUnit == null ||
                gridManager == null ||
                monsterUnit.MainGridIndex < 0 ||
                moveOffset == Vector2Int.zero)
            {
                return false;
            }

            int stepCount = Mathf.Max(Mathf.Abs(moveOffset.x), Mathf.Abs(moveOffset.y));
            Vector2Int step = new(
                moveOffset.x == 0 ? 0 : (moveOffset.x > 0 ? 1 : -1),
                moveOffset.y == 0 ? 0 : (moveOffset.y > 0 ? 1 : -1));

            List<Vector2Int> occupiedCoords = new();

            for (int i = 0; i < monsterUnit.OccupiedGridIndices.Count; i++)
            {
                int gridIndex = monsterUnit.OccupiedGridIndices[i];

                if (gridIndex >= 0)
                    occupiedCoords.Add(gridManager.IndexToCoord(gridIndex));
            }

            if (occupiedCoords.Count == 0)
                occupiedCoords.Add(gridManager.IndexToCoord(monsterUnit.MainGridIndex));

            for (int moveStep = 0; moveStep < stepCount; moveStep++)
            {
                for (int i = 0; i < occupiedCoords.Count; i++)
                {
                    Vector2Int nextCoord = occupiedCoords[i] + step;

                    if (!gridManager.IsValidCoord(nextCoord))
                        return false;

                    int nextGridIndex = gridManager.CoordToIndex(nextCoord);

                    if (BattleOccupancyService.IsOccupiedByAnyUnit(
                            nextGridIndex,
                            null,
                            monsterUnit))
                    {
                        return true;
                    }

                    occupiedCoords[i] = nextCoord;
                }
            }

            return false;
        }

        private static bool CanReserveEscapeMove(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int moveOffset,
            BattleGridEffectController gridEffectController)
        {
            if (monsterUnit == null ||
                gridManager == null ||
                monsterUnit.MainGridIndex < 0 ||
                moveOffset == Vector2Int.zero)
            {
                return false;
            }

            if (moveOffset.x != 0 && moveOffset.y != 0)
                return false;

            int stepCount = Mathf.Max(Mathf.Abs(moveOffset.x), Mathf.Abs(moveOffset.y));
            Vector2Int step = new(
                moveOffset.x == 0 ? 0 : (moveOffset.x > 0 ? 1 : -1),
                moveOffset.y == 0 ? 0 : (moveOffset.y > 0 ? 1 : -1));

            List<Vector2Int> occupiedCoords = new();

            for (int i = 0; i < monsterUnit.OccupiedGridIndices.Count; i++)
            {
                int gridIndex = monsterUnit.OccupiedGridIndices[i];

                if (gridIndex >= 0)
                    occupiedCoords.Add(gridManager.IndexToCoord(gridIndex));
            }

            if (occupiedCoords.Count == 0)
                occupiedCoords.Add(gridManager.IndexToCoord(monsterUnit.MainGridIndex));

            for (int moveStep = 0; moveStep < stepCount; moveStep++)
            {
                for (int i = 0; i < occupiedCoords.Count; i++)
                {
                    Vector2Int nextCoord = occupiedCoords[i] + step;

                    // 첫 칸이 맵 밖이면 이동할 수 없지만, 두 번째 칸만 맵 밖인 경우에는
                    // 2칸 이동을 예약하고 실제 실행 중 맵 경계에서 멈추게 합니다.
                    if (!gridManager.IsValidCoord(nextCoord))
                        return moveStep > 0;

                    int nextGridIndex = gridManager.CoordToIndex(nextCoord);

                    // 첫 칸의 고정 장애물은 한 칸도 도망갈 수 없으므로 예약 후보에서 제외합니다.
                    // 두 번째 칸의 잔해처럼 막힌 오브젝트는 첫 칸까지 이동한 뒤 충돌할 수 있으므로
                    // 2칸 이동 예약 자체는 유지합니다.
                    if (moveStep == 0 &&
                        gridEffectController != null &&
                        gridEffectController.IsBlocked(nextGridIndex))
                    {
                        return false;
                    }

                    occupiedCoords[i] = nextCoord;
                }
            }

            return true;
        }

        private static int GetEscapeOpenSpaceScore(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int moveOffset,
            BattleGridEffectController gridEffectController)
        {
            if (monsterUnit == null ||
                gridManager == null ||
                monsterUnit.MainGridIndex < 0 ||
                moveOffset == Vector2Int.zero)
            {
                return -1;
            }

            Vector2Int start = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int destination = start + moveOffset;

            // 경계 밖으로 향하는 2칸 이동은 실행 시 마지막 유효 칸에서 멈춥니다.
            // 경계 밖 좌표 대신 실제 도착 가능한 칸으로 주변 공간을 평가합니다.
            if (!gridManager.IsValidCoord(destination))
            {
                Vector2Int step = new Vector2Int(
                    moveOffset.x == 0 ? 0 : (moveOffset.x > 0 ? 1 : -1),
                    moveOffset.y == 0 ? 0 : (moveOffset.y > 0 ? 1 : -1));
                destination = start + step;
                if (!gridManager.IsValidCoord(destination))
                    return -1;
            }

            Vector2Int[] directions =
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            int score = 0;

            for (int directionIndex = 0; directionIndex < directions.Length; directionIndex++)
            {
                Vector2Int coord = destination;

                // 랜서는 2칸씩 이동하므로 도착 위치에서 각 방향으로 2칸까지
                // 다시 움직일 여유가 있는지를 탈출 공간 점수로 사용합니다.
                for (int step = 0; step < 2; step++)
                {
                    coord += directions[directionIndex];

                    if (!gridManager.IsValidCoord(coord))
                        break;

                    int gridIndex = gridManager.CoordToIndex(coord);

                    // 열린 공간 평가에서도 현재 유닛 위치는 미래에 바뀔 수 있으므로
                    // 장애물로 확정하지 않습니다. 고정 장애물만 공간을 막는 것으로 봅니다.
                    if (gridEffectController != null && gridEffectController.IsBlocked(gridIndex))
                        break;

                    score++;
                }
            }

            return score;
        }

        private static bool IsRiskyEscapeMove(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int moveOffset,
            BattleGridEffectController gridEffectController)
        {
            if (monsterUnit == null ||
                gridManager == null ||
                gridEffectController == null ||
                monsterUnit.MainGridIndex < 0)
            {
                return false;
            }

            Vector2Int coord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            int stepCount = Mathf.Max(Mathf.Abs(moveOffset.x), Mathf.Abs(moveOffset.y));

            if (stepCount <= 0)
                return false;

            Vector2Int step = new(
                moveOffset.x == 0 ? 0 : (moveOffset.x > 0 ? 1 : -1),
                moveOffset.y == 0 ? 0 : (moveOffset.y > 0 ? 1 : -1));

            for (int i = 0; i < stepCount; i++)
            {
                coord += step;

                if (!gridManager.IsValidCoord(coord))
                    return true;

                int gridIndex = gridManager.CoordToIndex(coord);

                if (gridEffectController.HasEffect(gridIndex))
                    return true;
            }

            return false;
        }
    }
}
