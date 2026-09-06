using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 루크 AI
    /// - 철옹성은 가능한 한 1번 또는 2번 슬롯에 예약합니다.
    /// - 매 턴 방패돌진 또는 방패강타 중 하나를 반드시 예약합니다.
    /// - 같은 가로 라인에 캐릭터가 있으면 방패돌진을 사용합니다.
    /// - 같은 가로 라인에 캐릭터가 없으면 이동 후 방패강타를 노립니다.
    /// - 짝수 턴(2, 4, 6, 8...)마다 대지진을 추가로 예약합니다.
    /// - 공격 가능한 이동 후보가 여러 개면 위험한 그리드 효과를 밟지 않는 후보를 우선합니다.
    /// </summary>
    public class RookAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_32";
        private const string DashSkillId = "S_Monster_33";
        private const string BashSkillId = "S_Monster_34";
        private const string EarthquakeSkillId = "S_Monster_35";
        private const string FortressSkillId = "S_Monster_36";
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

            // 철옹성은 다른 행동과 같은 슬롯에 있어도 되고 단독이어도 됩니다.
            // 단, 가능한 한 1번/2번 슬롯 중 앞쪽에 배치합니다.
            plan.Add(new MonsterAIAction(
                FortressSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.FirstTwo,
                -1,
                0));

            int battleTurn = context != null && context.CurrentTurn > 0
                ? context.CurrentTurn
                : runtime.TurnCount + 1;

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
            if (battleTurn % 2 == 0)
            {
                plan.Add(new MonsterAIAction(
                    EarthquakeSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.NextSlot,
                    -1,
                    3));
            }

            return plan;
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

                if (distance <= 0 || distance > DashMaxDistance || distance >= bestDistance)
                    continue;

                bestDistance = distance;
                direction = target.x >= origin.x
                    ? BattleDirection.Right
                    : BattleDirection.Left;
                found = true;
            }

            return found;
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
