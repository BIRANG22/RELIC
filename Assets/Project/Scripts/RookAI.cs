using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// Rook AI: odd turns favor dash and bash combinations when targets are reachable.
    /// Even turns reserve one main attack plus earthquake.
    /// </summary>
    public class RookAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_21";
        private const string DashSkillId = "S_Monster_22";
        private const string BashSkillId = "S_Monster_23";
        private const string EarthquakeSkillId = "S_Monster_24";
        private const int DashMaxDistance = 6;

        private static readonly List<Vector2Int> MoveOffsets = new()
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
            return BashSkillId;
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

            int battleTurn = context != null && context.CurrentTurn > 0
                ? context.CurrentTurn
                : runtime.TurnCount + 1;

            if (battleTurn % 2 != 0)
            {
                // Elite odd turns prioritize two *valid* attacks. Dash advances Rook,
                // so any later bash must use the projected landing position.
                // The dashed target is excluded because it is knocked back six cells.
                if (TryBuildDash(monsterUnit.MainGridIndex, gridManager,
                        out BattleDirection openingDashDirection))
                {
                    plan.Add(new MonsterAIAction(
                        DashSkillId, Vector2Int.zero,
                        MonsterAISlotPreference.Front, -1, 1,
                        monsterUnit.MainGridIndex, true, openingDashDirection));

                    if (TryGetDashLandingAndHitTarget(
                            monsterUnit.MainGridIndex, gridManager,
                            openingDashDirection,
                            out int landingGridIndex,
                            out int dashedTargetIndex) &&
                        TryFindProjectedMoveForBash(
                            monsterUnit, gridManager, landingGridIndex,
                            dashedTargetIndex,
                            out Vector2Int followMove,
                            out int bashOrigin,
                            out BattleDirection followBashDirection))
                    {
                        const int comboGroup = 92;
                        plan.Add(new MonsterAIAction(
                            MoveSkillId, followMove,
                            MonsterAISlotPreference.NextSlot, comboGroup, 2));
                        plan.Add(new MonsterAIAction(
                            BashSkillId, Vector2Int.zero,
                            MonsterAISlotPreference.SameSlot, comboGroup, 3,
                            bashOrigin, true, followBashDirection));
                    }
                }
                else if (TryBuildBash(monsterUnit.MainGridIndex, gridManager,
                             out BattleDirection stationaryBashDirection))
                {
                    // Already in bash range: preserve position rather than wasting a move.
                    plan.Add(new MonsterAIAction(
                        BashSkillId, Vector2Int.zero,
                        MonsterAISlotPreference.Front, -1, 1,
                        monsterUnit.MainGridIndex, true, stationaryBashDirection));

                    // A stationary bash does not end an odd-turn attack plan.
                    // If stepping into a horizontal line enables a second attack,
                    // put the move and the dash together in the following slot.
                    if (TryFindMoveForDashAfterBash(
                            monsterUnit, gridManager, stationaryBashDirection,
                            out Vector2Int followMove, out int dashOrigin,
                            out BattleDirection followDashDirection))
                    {
                        const int followGroup = 93;
                        plan.Add(new MonsterAIAction(
                            MoveSkillId, followMove,
                            MonsterAISlotPreference.NextSlot, followGroup, 2));
                        plan.Add(new MonsterAIAction(
                            DashSkillId, Vector2Int.zero,
                            MonsterAISlotPreference.SameSlot, followGroup, 3,
                            dashOrigin, true, followDashDirection));
                    }
                }
                else if (TryFindMoveForBash(monsterUnit, gridManager,
                             out Vector2Int moveOffset,
                             out int bashOrigin,
                             out BattleDirection bashDirection))
                {
                    const int comboGroup = 90;
                    plan.Add(new MonsterAIAction(
                        MoveSkillId, moveOffset,
                        MonsterAISlotPreference.Front, comboGroup, 1));
                    plan.Add(new MonsterAIAction(
                        BashSkillId, Vector2Int.zero,
                        MonsterAISlotPreference.SameSlot, comboGroup, 2,
                        bashOrigin, true, bashDirection));
                    // Bash may knock the target one cell away. Dash is considered
                    // only when a valid horizontal target remains in range.
                    if (TryBuildDash(bashOrigin, gridManager,
                            out BattleDirection followDashDirection))
                    {
                        plan.Add(new MonsterAIAction(
                            DashSkillId, Vector2Int.zero,
                            MonsterAISlotPreference.NextSlot, -1, 3,
                            bashOrigin, true, followDashDirection));
                    }
                }
                else
                {
                    // Chase only. Do not reserve a bash with no valid target.
                    Vector2Int chase = GetBestFallbackMoveTowardNearestPlayer(
                        monsterUnit, gridManager);
                    if (chase != Vector2Int.zero)
                    {
                        plan.Add(new MonsterAIAction(
                            MoveSkillId, chase, MonsterAISlotPreference.Front, -1, 1));
                    }
                }
            }
            else
            {
                // 매 턴 방패돌진 또는 방패강타 중 하나는 반드시 예약합니다.
                if (TryBuildDash(monsterUnit.MainGridIndex, gridManager, out BattleDirection dashDirection))
                {
                    plan.Add(new MonsterAIAction(
                        DashSkillId,
                        Vector2Int.zero,
                        MonsterAISlotPreference.Front,
                        -1,
                        1,
                        monsterUnit.MainGridIndex,
                        true,
                        dashDirection));
                }
                else if (TryBuildBash(monsterUnit.MainGridIndex, gridManager,
                             out BattleDirection stationaryBashDirection))
                {
                    // Keep the main attack in slot one; earthquake remains unchanged.
                    plan.Add(new MonsterAIAction(
                        BashSkillId, Vector2Int.zero,
                        MonsterAISlotPreference.Front, -1, 1,
                        monsterUnit.MainGridIndex, true, stationaryBashDirection));
                }
                else if (TryFindMoveForBash(
                             monsterUnit,
                             gridManager,
                             out Vector2Int moveOffset,
                             out int projectedGridIndex,
                             out BattleDirection bashDirection))
                {
                    const int sameSlotGroup = 90;

                    plan.Add(new MonsterAIAction(
                        MoveSkillId,
                        moveOffset,
                        MonsterAISlotPreference.Front,
                        sameSlotGroup,
                        1));

                    plan.Add(new MonsterAIAction(
                        BashSkillId,
                        Vector2Int.zero,
                        MonsterAISlotPreference.SameSlot,
                        sameSlotGroup,
                        2,
                        projectedGridIndex,
                        true,
                        bashDirection));
                }
                else
                {
                    AddFallbackMoveAndBash(plan, monsterUnit, gridManager);
                }

                // 대지진은 기본 공격 행동을 대체하지 않고 짝수 턴마다 추가합니다.
                {
                    plan.Add(new MonsterAIAction(
                        EarthquakeSkillId,
                        Vector2Int.zero,
                        MonsterAISlotPreference.NextSlot,
                        -1,
                        3));
                }
            }

            FaceFirstStationaryAttack(monsterUnit, plan);
            return plan;
        }

        // Match Rook's visible facing to a stationary opening attack during reservation.
        // Attacks following a movement keep their reserved direction and are not pre-flipped.
        private static void FaceFirstStationaryAttack(MonsterUnit monsterUnit, MonsterAIPlan plan)
        {
            if (monsterUnit == null || monsterUnit.RuntimeData == null ||
                plan == null || plan.Actions == null || plan.Actions.Count == 0)
                return;

            MonsterAIAction first = null;
            for (int i = 0; i < plan.Actions.Count; i++)
            {
                MonsterAIAction candidate = plan.Actions[i];
                if (candidate == null)
                    continue;
                if (first == null || candidate.Priority < first.Priority)
                    first = candidate;
            }

            if (first == null || first.SkillId == MoveSkillId ||
                first.SkillId == EarthquakeSkillId || !first.HasForcedDirection)
                return;

            if (first.SkillId != DashSkillId && first.SkillId != BashSkillId)
                return;

            BattleDirection direction = first.ForcedDirection;
            BattleUnitFacing facing = monsterUnit.GetComponentInChildren<BattleUnitFacing>(true);
            if (facing != null)
                facing.FaceRight(direction == BattleDirection.Right);
            monsterUnit.RuntimeData.Direction = direction;
        }

        private bool TryGetDashLandingAndHitTarget(
            int originIndex, GridManager gridManager, BattleDirection direction,
            out int landingIndex, out int dashedTargetIndex)
        {
            landingIndex = -1;
            dashedTargetIndex = -1;
            Vector2Int origin = gridManager.IndexToCoord(originIndex);
            int sign = direction == BattleDirection.Right ? 1 : -1;
            int nearest = int.MaxValue;
            List<int> targets = FindCharacterTargetGridIndices();
            for (int i = 0; i < targets.Count; i++)
            {
                Vector2Int target = gridManager.IndexToCoord(targets[i]);
                int distance = (target.x - origin.x) * sign;
                if (target.y != origin.y || distance <= 0 ||
                    distance > DashMaxDistance || distance >= nearest ||
                    !IsDashPathClear(origin, target, gridManager))
                    continue;
                nearest = distance;
                dashedTargetIndex = targets[i];
            }
            if (dashedTargetIndex < 0)
                return false;
            Vector2Int destination = origin + new Vector2Int(sign * (nearest - 1), 0);
            if (!gridManager.IsValidCoord(destination))
                return false;
            landingIndex = gridManager.CoordToIndex(destination);
            return true;
        }

        private bool TryFindProjectedMoveForBash(
            MonsterUnit monsterUnit, GridManager gridManager,
            int projectedOrigin, int knockedTargetIndex,
            out Vector2Int bestOffset, out int bestOrigin,
            out BattleDirection bestDirection)
        {
            bestOffset = Vector2Int.zero;
            bestOrigin = -1;
            bestDirection = BattleDirection.Right;
            Vector2Int source = gridManager.IndexToCoord(projectedOrigin);
            Vector2Int actual = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            List<int> targets = FindCharacterTargetGridIndices();
            BattleGridEffectController effects =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);
            int bestRisk = int.MaxValue;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < MoveOffsets.Count; i++)
            {
                Vector2Int offset = MoveOffsets[i];
                Vector2Int candidate = source + offset;
                if (!gridManager.IsValidCoord(candidate))
                    continue;
                bool blocked = false;
                for (int cell = 0; cell < monsterUnit.OccupiedGridIndices.Count; cell++)
                {
                    Vector2Int footprint = gridManager.IndexToCoord(
                        monsterUnit.OccupiedGridIndices[cell]) - actual;
                    Vector2Int destination = candidate + footprint;
                    if (!gridManager.IsValidCoord(destination))
                    {
                        blocked = true;
                        break;
                    }
                    int destinationIndex = gridManager.CoordToIndex(destination);
                    if (BattleOccupancyService.IsOccupiedByAnyUnit(
                            destinationIndex, null, monsterUnit) ||
                        (effects != null && effects.IsBlocked(destinationIndex)))
                    {
                        blocked = true;
                        break;
                    }
                }
                if (blocked)
                    continue;
                for (int j = 0; j < targets.Count; j++)
                {
                    if (targets[j] == knockedTargetIndex)
                        continue;
                    Vector2Int target = gridManager.IndexToCoord(targets[j]);
                    int dx = target.x - candidate.x;
                    int dy = target.y - candidate.y;
                    if (Mathf.Abs(dx) > 1 || Mathf.Abs(dy) > 1 ||
                        (dx == 0 && dy == 0))
                        continue;
                    int originIndex = gridManager.CoordToIndex(candidate);
                    int risk = IsRiskyGridEffectDestination(originIndex, effects) ? 1 : 0;
                    int distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (risk > bestRisk || (risk == bestRisk && distance >= bestDistance))
                        continue;
                    bestRisk = risk;
                    bestDistance = distance;
                    bestOffset = offset;
                    bestOrigin = originIndex;
                    bestDirection = dx >= 0 ? BattleDirection.Right : BattleDirection.Left;
                }
            }
            return bestOrigin >= 0;
        }

        // Search a safe one-cell reposition after a stationary bash. The victim
        // of that bash is projected one cell away, so follow-up dash checks do
        // not use its outdated pre-bash position.
        private bool TryFindMoveForDashAfterBash(
            MonsterUnit monsterUnit, GridManager gridManager,
            BattleDirection bashDirection,
            out Vector2Int bestOffset, out int bestOrigin,
            out BattleDirection bestDirection)
        {
            bestOffset = Vector2Int.zero;
            bestOrigin = -1;
            bestDirection = BattleDirection.Right;

            Vector2Int origin = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            List<int> targetIndices = FindCharacterTargetGridIndices();
            if (targetIndices == null || targetIndices.Count == 0)
                return false;

            // Mirror TryBuildBash target priority (nearest adjacent target).
            int bashTargetIndex = -1;
            int bashTargetDistance = int.MaxValue;
            for (int i = 0; i < targetIndices.Count; i++)
            {
                Vector2Int target = gridManager.IndexToCoord(targetIndices[i]);
                int dx = Mathf.Abs(target.x - origin.x);
                int dy = Mathf.Abs(target.y - origin.y);
                int distance = dx + dy;
                if (dx <= 1 && dy <= 1 && distance > 0 && distance < bashTargetDistance)
                {
                    bashTargetDistance = distance;
                    bashTargetIndex = targetIndices[i];
                }
            }

            BattleGridEffectController effects =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);
            int bestRisk = int.MaxValue;
            int bestDistance = int.MaxValue;
            for (int m = 0; m < MoveOffsets.Count; m++)
            {
                Vector2Int offset = MoveOffsets[m];
                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;
                int moveDestination = GetProjectedMainGridIndex(monsterUnit, gridManager, offset);
                if (moveDestination < 0)
                    continue;
                Vector2Int candidate = gridManager.IndexToCoord(moveDestination);
                int risk = IsRiskyGridEffectDestination(moveDestination, effects) ? 1 : 0;
                for (int t = 0; t < targetIndices.Count; t++)
                {
                    Vector2Int target = gridManager.IndexToCoord(targetIndices[t]);
                    if (targetIndices[t] == bashTargetIndex)
                    {
                        target.x += bashDirection == BattleDirection.Right ? 1 : -1;
                        if (!gridManager.IsValidCoord(target))
                            continue;
                    }
                    int dx = target.x - candidate.x;
                    int distance = Mathf.Abs(dx);
                    if (target.y != candidate.y || distance < 1 || distance > DashMaxDistance ||
                        !IsDashPathClear(candidate, target, gridManager))
                        continue;
                    if (risk > bestRisk || (risk == bestRisk && distance >= bestDistance))
                        continue;
                    bestRisk = risk;
                    bestDistance = distance;
                    bestOffset = offset;
                    bestOrigin = moveDestination;
                    bestDirection = dx > 0 ? BattleDirection.Right : BattleDirection.Left;
                }
            }
            return bestOrigin >= 0;
        }

        private void AddFallbackMoveAndBash(
            MonsterAIPlan plan,
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            if (plan == null || monsterUnit == null || gridManager == null)
                return;

            Vector2Int moveOffset = GetBestFallbackMoveTowardNearestPlayer(monsterUnit, gridManager);
            int projectedGridIndex = monsterUnit.MainGridIndex;

            if (moveOffset != Vector2Int.zero)
            {
                projectedGridIndex = GetProjectedMainGridIndex(monsterUnit, gridManager, moveOffset);

                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    moveOffset,
                    MonsterAISlotPreference.Front,
                    91,
                    1));
            }

            BattleDirection bashDirection;

            if (!TryBuildBash(projectedGridIndex, gridManager, out bashDirection))
                bashDirection = GetDirectionToNearestPlayer(projectedGridIndex, gridManager);

            plan.Add(new MonsterAIAction(
                BashSkillId,
                Vector2Int.zero,
                moveOffset != Vector2Int.zero
                    ? MonsterAISlotPreference.SameSlot
                    : MonsterAISlotPreference.Front,
                moveOffset != Vector2Int.zero ? 91 : -1,
                2,
                projectedGridIndex,
                true,
                bashDirection));
        }

        private bool TryFindMoveForBash(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            out Vector2Int moveOffset,
            out int projectedGridIndex,
            out BattleDirection bashDirection)
        {
            moveOffset = Vector2Int.zero;
            projectedGridIndex = -1;
            bashDirection = BattleDirection.Right;

            if (monsterUnit == null || gridManager == null)
                return false;

            BattleGridEffectController gridEffectController =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            List<int> targets = FindCharacterTargetGridIndices();
            int bestRiskRank = int.MaxValue;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < MoveOffsets.Count; i++)
            {
                Vector2Int candidateOffset = MoveOffsets[i];

                if (!CanMonsterMove(monsterUnit, gridManager, candidateOffset))
                    continue;

                int candidateGridIndex = GetProjectedMainGridIndex(
                    monsterUnit,
                    gridManager,
                    candidateOffset);

                BattleDirection candidateDirection = BattleDirection.Right;
                bool candidateCanBash = candidateGridIndex >= 0 &&
                                        TryBuildBash(
                                            candidateGridIndex,
                                            gridManager,
                                            out candidateDirection);

                if (!candidateCanBash)
                    continue;

                bool isRisky = IsRiskyGridEffectDestination(candidateGridIndex, gridEffectController);
                int riskRank = isRisky ? 1 : 0;
                Vector2Int candidateCoord = currentCoord + candidateOffset;
                int nearestDistance = GetNearestTargetDistance(candidateCoord, targets, gridManager);

                if (riskRank > bestRiskRank)
                    continue;

                if (riskRank == bestRiskRank && nearestDistance >= bestDistance)
                    continue;

                bestRiskRank = riskRank;
                bestDistance = nearestDistance;
                moveOffset = candidateOffset;
                projectedGridIndex = candidateGridIndex;
                bashDirection = candidateDirection;
            }

            return projectedGridIndex >= 0;
        }

        private Vector2Int GetBestFallbackMoveTowardNearestPlayer(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            if (monsterUnit == null || gridManager == null)
                return Vector2Int.zero;

            int targetGridIndex = FindNearestCharacterTargetGridIndex(monsterUnit, gridManager);

            if (targetGridIndex < 0)
                return Vector2Int.zero;

            BattleGridEffectController gridEffectController =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);
            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int bestOffset = Vector2Int.zero;
            int bestRiskRank = int.MaxValue;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < MoveOffsets.Count; i++)
            {
                Vector2Int candidateOffset = MoveOffsets[i];

                if (!CanMonsterMove(monsterUnit, gridManager, candidateOffset))
                    continue;

                int candidateGridIndex = GetProjectedMainGridIndex(monsterUnit, gridManager, candidateOffset);
                bool isRisky = IsRiskyGridEffectDestination(candidateGridIndex, gridEffectController);
                int riskRank = isRisky ? 1 : 0;
                Vector2Int candidateCoord = currentCoord + candidateOffset;
                int distance =
                    Mathf.Abs(targetCoord.x - candidateCoord.x) +
                    Mathf.Abs(targetCoord.y - candidateCoord.y);

                if (riskRank > bestRiskRank)
                    continue;

                if (riskRank == bestRiskRank && distance >= bestDistance)
                    continue;

                bestRiskRank = riskRank;
                bestDistance = distance;
                bestOffset = candidateOffset;
            }

            return bestOffset;
        }

        private static int GetNearestTargetDistance(
            Vector2Int originCoord,
            List<int> targets,
            GridManager gridManager)
        {
            if (targets == null || gridManager == null)
                return int.MaxValue;

            int nearestDistance = int.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                int targetGridIndex = targets[i];

                if (targetGridIndex < 0)
                    continue;

                Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);
                int distance =
                    Mathf.Abs(targetCoord.x - originCoord.x) +
                    Mathf.Abs(targetCoord.y - originCoord.y);

                if (distance < nearestDistance)
                    nearestDistance = distance;
            }

            return nearestDistance;
        }

        private static bool IsRiskyGridEffectDestination(
            int gridIndex,
            BattleGridEffectController gridEffectController)
        {
            if (gridIndex < 0 || gridEffectController == null)
                return false;

            if (!gridEffectController.State.TryGetEffectId(gridIndex, out string gridEffectId))
                return false;

            return !string.IsNullOrWhiteSpace(gridEffectId);
        }

        private bool TryBuildDash(
            int originGridIndex,
            GridManager gridManager,
            out BattleDirection direction)
        {
            direction = BattleDirection.Right;

            if (originGridIndex < 0 || gridManager == null)
                return false;

            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
            List<int> targets = FindCharacterTargetGridIndices();
            int bestDistance = int.MaxValue;
            bool found = false;

            for (int i = 0; i < targets.Count; i++)
            {
                int gridIndex = targets[i];

                if (gridIndex < 0)
                    continue;

                Vector2Int target = gridManager.IndexToCoord(gridIndex);

                if (target.y != origin.y)
                    continue;

                int distance = Mathf.Abs(target.x - origin.x);

                if (distance <= 0 || distance > DashMaxDistance || distance >= bestDistance ||
                    !IsDashPathClear(origin, target, gridManager))
                    continue;

                bestDistance = distance;
                direction = target.x >= origin.x
                    ? BattleDirection.Right
                    : BattleDirection.Left;
                found = true;
            }

            return found;
        }

        // A dash cannot hit a character through an impassable grid effect.
        // Check every cell before the target, excluding the target's own cell.
        private static bool IsDashPathClear(
            Vector2Int origin, Vector2Int target, GridManager gridManager)
        {
            if (gridManager == null || origin.y != target.y)
                return false;

            int sign = target.x > origin.x ? 1 : -1;
            BattleGridEffectController effects =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            for (int x = origin.x + sign; x != target.x; x += sign)
            {
                Vector2Int cell = new Vector2Int(x, origin.y);
                if (!gridManager.IsValidCoord(cell))
                    return false;

                int index = gridManager.CoordToIndex(cell);
                if (effects != null && effects.IsBlocked(index))
                    return false;
            }

            return true;
        }

        private bool TryBuildBash(
            int originGridIndex,
            GridManager gridManager,
            out BattleDirection direction)
        {
            direction = BattleDirection.Right;

            if (originGridIndex < 0 || gridManager == null)
                return false;

            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
            List<int> targets = FindCharacterTargetGridIndices();
            int bestDistance = int.MaxValue;
            bool found = false;

            for (int i = 0; i < targets.Count; i++)
            {
                int gridIndex = targets[i];

                if (gridIndex < 0)
                    continue;

                Vector2Int target = gridManager.IndexToCoord(gridIndex);
                int dx = Mathf.Abs(target.x - origin.x);
                int dy = Mathf.Abs(target.y - origin.y);

                if (dx > 1 || dy > 1 || (dx == 0 && dy == 0))
                    continue;

                int distance = dx + dy;

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                direction = target.x >= origin.x
                    ? BattleDirection.Right
                    : BattleDirection.Left;
                found = true;
            }

            return found;
        }
    }
}
