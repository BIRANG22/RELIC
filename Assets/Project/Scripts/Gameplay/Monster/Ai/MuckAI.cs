using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class MuckAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_01";
        private const string AttackSkillId = "S_Monster_02";
        private const string ResidueGridEffectId = "GR_Residue";

        private static readonly Vector2Int[] MoveDirections =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
            new Vector2Int(-1, 1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1)
        };

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

            string attackRangeId = monsterUnit.RuntimeData.AttackRangeId;
            bool usesRangedAttackOrigin =
                !string.IsNullOrWhiteSpace(attackRangeId) &&
                attackRangeId.Trim() != "0";
            MonsterSkillData attackSkill = usesRangedAttackOrigin
                ? DataManager.Instance?.MonsterSkillDatabase.Get(AttackSkillId)
                : null;

            BattleGridEffectController gridEffectController =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            int currentAttackOrigin = -1;

            if (usesRangedAttackOrigin)
            {
                currentAttackOrigin = FindRangedAttackOrigin(
                    monsterUnit,
                    monsterUnit.MainGridIndex,
                    attackSkill,
                    attackRangeId,
                    gridManager);
            }

            // 현재 위치에서 이미 공격이 가능하면 굳이 이동하지 않습니다.
            // 공격할 수 없을 때만 가장 가까운 캐릭터 쪽으로 1칸 이동을 시도합니다.
            Vector2Int moveOffset = currentAttackOrigin >= 0
                ? Vector2Int.zero
                : GetBestOneTileMoveTowardNearestPlayer(
                    monsterUnit,
                    gridManager,
                    attackSkill,
                    attackRangeId,
                    usesRangedAttackOrigin,
                    gridEffectController);

            bool canMove = moveOffset != Vector2Int.zero &&
                           CanMonsterMove(monsterUnit, gridManager, moveOffset);

            int group = 1;

            if (canMove)
            {
                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    moveOffset,
                    MonsterAISlotPreference.Front,
                    group,
                    0
                ));
            }

            int projectedMainGridIndex = canMove
                ? GetProjectedMainGridIndex(monsterUnit, gridManager, moveOffset)
                : monsterUnit.MainGridIndex;

            int rangeOriginGridIndex = -1;

            if (usesRangedAttackOrigin)
            {
                rangeOriginGridIndex = currentAttackOrigin >= 0 && !canMove
                    ? currentAttackOrigin
                    : FindRangedAttackOrigin(
                        monsterUnit,
                        projectedMainGridIndex,
                        attackSkill,
                        attackRangeId,
                        gridManager);

                // 계획 단계에서 이동 후에도 공격 범위가 닿지 않는다면 이동만 예약합니다.
                // 이미 타임라인에 등록된 공격은 실행 단계에서 이동 실패 여부와 관계없이 실행됩니다.
                if (rangeOriginGridIndex < 0)
                    return plan;
            }

            plan.Add(new MonsterAIAction(
                AttackSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.SameSlot,
                group,
                1,
                rangeOriginGridIndex,
                rangeOriginCasterGridIndex: projectedMainGridIndex
            ));

            return plan;
        }

        private Vector2Int GetBestOneTileMoveTowardNearestPlayer(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            MonsterSkillData attackSkill,
            string attackRangeId,
            bool usesRangedAttackOrigin,
            BattleGridEffectController gridEffectController)
        {
            int targetGridIndex = FindNearestCharacterTargetGridIndex(monsterUnit, gridManager);

            if (targetGridIndex < 0 ||
                monsterUnit.MainGridIndex < 0)
            {
                return Vector2Int.zero;
            }

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

            Vector2Int bestOffset = Vector2Int.zero;
            int bestPriorityRank = int.MaxValue;
            int bestChebyshevDistance = int.MaxValue;
            int bestManhattanDistance = int.MaxValue;

            for (int i = 0; i < MoveDirections.Length; i++)
            {
                Vector2Int offset = MoveDirections[i];

                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                int projectedMainGridIndex =
                    GetProjectedMainGridIndex(monsterUnit, gridManager, offset);

                bool candidateCanAttack = !usesRangedAttackOrigin ||
                                          FindRangedAttackOrigin(
                                              monsterUnit,
                                              projectedMainGridIndex,
                                              attackSkill,
                                              attackRangeId,
                                              gridManager) >= 0;

                bool isRiskyDestination = IsRiskyGridEffectDestination(
                    projectedMainGridIndex,
                    gridEffectController);

                // 공격 가능한 빈칸 > 공격 가능한 위험지형 > 빈칸 > 위험지형 순으로 선택합니다.
                // 위험지형을 밟아야만 공격할 수 있다면 공격을 위해 진입할 수 있습니다.
                int attackRank = candidateCanAttack ? 0 : 2;
                int priorityRank = attackRank + (isRiskyDestination ? 1 : 0);

                Vector2Int projectedCoord = currentCoord + offset;
                int deltaX = Mathf.Abs(targetCoord.x - projectedCoord.x);
                int deltaY = Mathf.Abs(targetCoord.y - projectedCoord.y);
                int chebyshevDistance = Mathf.Max(deltaX, deltaY);
                int manhattanDistance = deltaX + deltaY;

                if (priorityRank > bestPriorityRank)
                    continue;

                if (priorityRank == bestPriorityRank &&
                    chebyshevDistance > bestChebyshevDistance)
                {
                    continue;
                }

                if (priorityRank == bestPriorityRank &&
                    chebyshevDistance == bestChebyshevDistance &&
                    manhattanDistance >= bestManhattanDistance)
                {
                    continue;
                }

                bestPriorityRank = priorityRank;
                bestChebyshevDistance = chebyshevDistance;
                bestManhattanDistance = manhattanDistance;
                bestOffset = offset;
            }

            return bestOffset;
        }

        private static bool IsRiskyGridEffectDestination(
            int gridIndex,
            BattleGridEffectController gridEffectController)
        {
            if (gridIndex < 0 || gridEffectController == null)
                return false;

            if (!gridEffectController.State.TryGetEffectId(gridIndex, out string gridEffectId) ||
                string.IsNullOrWhiteSpace(gridEffectId))
            {
                return false;
            }

            // 머크가 만드는 점액은 몬스터에게 적용되지 않으므로 위험지형으로 취급하지 않습니다.
            return !string.Equals(
                gridEffectId,
                ResidueGridEffectId,
                System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
