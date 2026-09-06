using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class BlobAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_03";
        private const string AttackSkillId = "S_Monster_04";
        private const string ResidueGridEffectId = "GR_Residue";

        private static readonly Vector2Int[] MoveDirections =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
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

            MonsterSkillData attackSkill =
                DataManager.Instance?.MonsterSkillDatabase?.Get(AttackSkillId);

            // 블롭은 단순히 가장 가까운 캐릭터보다 이번 행동에서 실제로 좌우 공격을
            // 성공시킬 수 있는 캐릭터를 우선 타겟으로 고정합니다.
            int targetGridIndex = FindBestBlobTargetGridIndex(
                monsterUnit,
                attackSkill,
                gridManager);

            if (targetGridIndex < 0)
                return plan;

            BattleGridEffectController gridEffectController =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            // 블롭은 이동한 자리에 점액을 남기는 몬스터이므로 매 턴 이동을 우선 시도합니다.
            // 공격 가능한 위치가 여러 곳이라면 위험 지형이 없는 칸을 우선합니다.
            Vector2Int moveOffset = GetBestCardinalMove(
                monsterUnit,
                targetGridIndex,
                attackSkill,
                gridManager,
                gridEffectController);

            int plannedMoveGridIndex = GetProjectedMainGridIndex(
                monsterUnit,
                gridManager,
                moveOffset);

            bool isCharacterCollisionMove = IsCharacterCollisionMoveCandidate(
                monsterUnit,
                plannedMoveGridIndex,
                targetGridIndex,
                gridEffectController);

            bool canMoveNormally = moveOffset != Vector2Int.zero &&
                                   CanMonsterMove(monsterUnit, gridManager, moveOffset);
            bool canMove = moveOffset != Vector2Int.zero &&
                           (canMoveNormally || isCharacterCollisionMove);

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

            int projectedGridIndex = GetProjectedMainGridIndex(
                monsterUnit,
                gridManager,
                canMove ? moveOffset : Vector2Int.zero);

            // 이동 목표는 예약 시점에 선택한 우선 타겟으로 고정합니다.
            // 다른 캐릭터가 공격 범위에 들어오더라도 이동/공격 방향을 바꾸지 않습니다.
            int attackTargetGridIndex = CanAttackTargetFromGrid(
                monsterUnit,
                projectedGridIndex,
                targetGridIndex,
                attackSkill,
                gridManager)
                ? targetGridIndex
                : -1;

            // 공격 범위 안에 캐릭터가 있으면 그 캐릭터 방향을 공격합니다.
            // 범위 안에 캐릭터가 없어도 공격 예약을 취소하지 않고 이동 후 바라보는 정면을 공격합니다.
            BattleDirection attackDirection = attackTargetGridIndex >= 0
                ? GetBlobAttackDirection(projectedGridIndex, attackTargetGridIndex, gridManager)
                : ResolveForwardAttackDirection(
                    monsterUnit,
                    moveOffset,
                    projectedGridIndex,
                    targetGridIndex,
                    gridManager);

            plan.Add(new MonsterAIAction(
                AttackSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.SameSlot,
                group,
                canMove ? 1 : 0,
                projectedGridIndex,
                true,
                attackDirection,
                rangeOriginCasterGridIndex: projectedGridIndex
            ));

            return plan;
        }

        private int FindBestBlobTargetGridIndex(
            MonsterUnit monsterUnit,
            MonsterSkillData attackSkill,
            GridManager gridManager)
        {
            if (monsterUnit == null || gridManager == null || monsterUnit.MainGridIndex < 0)
                return -1;

            List<int> targets = FindCharacterTargetGridIndices();

            if (targets == null || targets.Count == 0)
                return -1;

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            int bestAttackableTarget = -1;
            int bestAttackableDistance = int.MaxValue;
            int nearestTarget = -1;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                int candidateGridIndex = targets[i];
                Vector2Int candidateCoord = gridManager.IndexToCoord(candidateGridIndex);
                int distance =
                    Mathf.Abs(candidateCoord.x - currentCoord.x) +
                    Mathf.Abs(candidateCoord.y - currentCoord.y);

                if (distance < nearestDistance ||
                    (distance == nearestDistance && candidateGridIndex < nearestTarget))
                {
                    nearestDistance = distance;
                    nearestTarget = candidateGridIndex;
                }

                if (!CanAttackTargetThisTurn(
                        monsterUnit,
                        candidateGridIndex,
                        attackSkill,
                        gridManager))
                {
                    continue;
                }

                if (distance < bestAttackableDistance ||
                    (distance == bestAttackableDistance && candidateGridIndex < bestAttackableTarget))
                {
                    bestAttackableDistance = distance;
                    bestAttackableTarget = candidateGridIndex;
                }
            }

            return bestAttackableTarget >= 0
                ? bestAttackableTarget
                : nearestTarget;
        }

        private bool CanAttackTargetThisTurn(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            MonsterSkillData attackSkill,
            GridManager gridManager)
        {
            if (monsterUnit == null ||
                attackSkill == null ||
                gridManager == null ||
                monsterUnit.MainGridIndex < 0 ||
                targetGridIndex < 0)
            {
                return false;
            }

            // 이미 좌우 공격 범위에 들어온 타겟도 이번 행동에서 공격 가능한 대상으로 봅니다.
            // 이 경우 실제 이동 단계에서는 후퇴하지 않고 접근/충돌 규칙을 따릅니다.
            if (CanAttackTargetFromGrid(
                    monsterUnit,
                    monsterUnit.MainGridIndex,
                    targetGridIndex,
                    attackSkill,
                    gridManager))
            {
                return true;
            }

            // 한 칸 정상 이동 후 바로 좌우 공격이 가능한지를 예약 시점에만 판단합니다.
            for (int i = 0; i < MoveDirections.Length; i++)
            {
                Vector2Int moveOffset = MoveDirections[i];

                if (!CanMonsterMove(monsterUnit, gridManager, moveOffset))
                    continue;

                int projectedGridIndex = GetProjectedMainGridIndex(
                    monsterUnit,
                    gridManager,
                    moveOffset);

                if (CanAttackTargetFromGrid(
                        monsterUnit,
                        projectedGridIndex,
                        targetGridIndex,
                        attackSkill,
                        gridManager))
                {
                    return true;
                }
            }

            return false;
        }

        private Vector2Int GetBestCardinalMove(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            MonsterSkillData attackSkill,
            GridManager gridManager,
            BattleGridEffectController gridEffectController)
        {
            if (monsterUnit == null ||
                gridManager == null ||
                monsterUnit.MainGridIndex < 0 ||
                targetGridIndex < 0)
            {
                return Vector2Int.zero;
            }

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

            // 타겟이 좌/우에 있으면 후퇴하거나 다른 방향으로 공격각을 만들지 않습니다.
            // 타겟 쪽으로 계속 접근하며, 바로 앞을 타겟이 막고 있으면 충돌을 시도합니다.
            if (currentCoord.y == targetCoord.y && currentCoord.x != targetCoord.x)
            {
                Vector2Int towardTarget = targetCoord.x > currentCoord.x
                    ? Vector2Int.right
                    : Vector2Int.left;

                int projectedGridIndex = GetProjectedMainGridIndex(
                    monsterUnit,
                    gridManager,
                    towardTarget);

                bool canMoveNormally = CanMonsterMove(monsterUnit, gridManager, towardTarget);
                bool canCollideWithTarget = IsCharacterCollisionMoveCandidate(
                    monsterUnit,
                    projectedGridIndex,
                    targetGridIndex,
                    gridEffectController);

                if (canMoveNormally || canCollideWithTarget)
                    return towardTarget;

                // 정면이 완전히 막힌 경우에만 우회 이동을 찾습니다.
                // 이때도 타겟과의 거리가 더 멀어지는 후퇴 이동은 허용하지 않습니다.
                return FindBestNonRetreatMove(
                    monsterUnit,
                    targetGridIndex,
                    attackSkill,
                    gridManager,
                    gridEffectController,
                    currentCoord,
                    targetCoord,
                    excludeVerticalCollision: false);
            }

            // 타겟이 위/아래에 있으면 그 타겟에게 직접 충돌해도 좌우 공격은 성공하지 못합니다.
            // 따라서 좌/우 이동으로 공격 위치를 준비하는 것을 우선하고 세로 충돌은 하지 않습니다.
            if (currentCoord.x == targetCoord.x && currentCoord.y != targetCoord.y)
            {
                Vector2Int horizontalMove = FindBestHorizontalSetupMove(
                    monsterUnit,
                    targetGridIndex,
                    attackSkill,
                    gridManager,
                    gridEffectController);

                if (horizontalMove != Vector2Int.zero)
                    return horizontalMove;

                return FindBestNonRetreatMove(
                    monsterUnit,
                    targetGridIndex,
                    attackSkill,
                    gridManager,
                    gridEffectController,
                    currentCoord,
                    targetCoord,
                    excludeVerticalCollision: true);
            }

            // 대각선에 있는 경우에는 고정 타겟의 좌우 공격 위치를 준비하는 정상 이동만 평가합니다.
            // 캐릭터 칸으로 직접 들어가는 충돌은 좌우 정렬이 된 경우에만 사용합니다.
            return FindBestNonRetreatMove(
                monsterUnit,
                targetGridIndex,
                attackSkill,
                gridManager,
                gridEffectController,
                currentCoord,
                targetCoord,
                excludeVerticalCollision: true);
        }

        private Vector2Int FindBestHorizontalSetupMove(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            MonsterSkillData attackSkill,
            GridManager gridManager,
            BattleGridEffectController gridEffectController)
        {
            Vector2Int bestOffset = Vector2Int.zero;
            int bestSetupDistance = int.MaxValue;
            bool bestIsRisky = true;
            int bestTargetDistance = int.MaxValue;
            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

            Vector2Int[] horizontalDirections =
            {
                Vector2Int.left,
                Vector2Int.right
            };

            for (int i = 0; i < horizontalDirections.Length; i++)
            {
                Vector2Int offset = horizontalDirections[i];

                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                int projectedGridIndex = GetProjectedMainGridIndex(monsterUnit, gridManager, offset);
                Vector2Int projectedCoord = gridManager.IndexToCoord(projectedGridIndex);
                bool candidateCanAttack = CanAttackTargetFromGrid(
                    monsterUnit,
                    projectedGridIndex,
                    targetGridIndex,
                    attackSkill,
                    gridManager);
                int setupDistance = candidateCanAttack
                    ? 0
                    : GetAttackSetupDistance(
                        monsterUnit,
                        projectedGridIndex,
                        targetGridIndex,
                        attackSkill,
                        gridManager);
                bool isRisky = IsRiskyGridEffectDestination(projectedGridIndex, gridEffectController);
                int targetDistance =
                    Mathf.Abs(targetCoord.x - projectedCoord.x) +
                    Mathf.Abs(targetCoord.y - projectedCoord.y);

                bool isBetter = bestOffset == Vector2Int.zero ||
                                setupDistance < bestSetupDistance ||
                                (setupDistance == bestSetupDistance && bestIsRisky && !isRisky) ||
                                (setupDistance == bestSetupDistance && bestIsRisky == isRisky &&
                                 targetDistance < bestTargetDistance);

                if (!isBetter)
                    continue;

                bestOffset = offset;
                bestSetupDistance = setupDistance;
                bestIsRisky = isRisky;
                bestTargetDistance = targetDistance;
            }

            return bestOffset;
        }

        private Vector2Int FindBestNonRetreatMove(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            MonsterSkillData attackSkill,
            GridManager gridManager,
            BattleGridEffectController gridEffectController,
            Vector2Int currentCoord,
            Vector2Int targetCoord,
            bool excludeVerticalCollision)
        {
            int currentTargetDistance =
                Mathf.Abs(targetCoord.x - currentCoord.x) +
                Mathf.Abs(targetCoord.y - currentCoord.y);

            Vector2Int bestOffset = Vector2Int.zero;
            bool bestCanAttack = false;
            int bestSetupDistance = int.MaxValue;
            bool bestIsRisky = true;
            int bestTargetDistance = int.MaxValue;

            for (int i = 0; i < MoveDirections.Length; i++)
            {
                Vector2Int offset = MoveDirections[i];
                int projectedGridIndex = GetProjectedMainGridIndex(monsterUnit, gridManager, offset);
                bool canMoveNormally = CanMonsterMove(monsterUnit, gridManager, offset);
                bool isCollision = IsCharacterCollisionMoveCandidate(
                    monsterUnit,
                    projectedGridIndex,
                    targetGridIndex,
                    gridEffectController);

                if (!canMoveNormally && !isCollision)
                    continue;

                if (isCollision && excludeVerticalCollision && offset.y != 0)
                    continue;

                Vector2Int projectedCoord = currentCoord + offset;
                int targetDistance =
                    Mathf.Abs(targetCoord.x - projectedCoord.x) +
                    Mathf.Abs(targetCoord.y - projectedCoord.y);

                // 공격 성공을 위해 움직이더라도 고정 타겟에게서 멀어지는 후퇴는 하지 않습니다.
                if (targetDistance > currentTargetDistance)
                    continue;

                // 충돌은 좌우 공격 위치를 만드는 정상 이동보다 우선하지 않습니다.
                bool candidateCanAttack = !isCollision && CanAttackTargetFromGrid(
                    monsterUnit,
                    projectedGridIndex,
                    targetGridIndex,
                    attackSkill,
                    gridManager);
                int setupDistance = candidateCanAttack
                    ? 0
                    : GetAttackSetupDistance(
                        monsterUnit,
                        projectedGridIndex,
                        targetGridIndex,
                        attackSkill,
                        gridManager);
                bool isRisky = IsRiskyGridEffectDestination(projectedGridIndex, gridEffectController);

                bool isBetter = bestOffset == Vector2Int.zero;

                if (!isBetter && candidateCanAttack != bestCanAttack)
                    isBetter = candidateCanAttack;
                else if (!isBetter && setupDistance != bestSetupDistance)
                    isBetter = setupDistance < bestSetupDistance;
                else if (!isBetter && isRisky != bestIsRisky)
                    isBetter = !isRisky;
                else if (!isBetter && targetDistance != bestTargetDistance)
                    isBetter = targetDistance < bestTargetDistance;

                if (!isBetter)
                    continue;

                bestOffset = offset;
                bestCanAttack = candidateCanAttack;
                bestSetupDistance = setupDistance;
                bestIsRisky = isRisky;
                bestTargetDistance = targetDistance;
            }

            return bestOffset;
        }

        private int GetAttackSetupDistance(
            MonsterUnit monsterUnit,
            int originGridIndex,
            int targetGridIndex,
            MonsterSkillData attackSkill,
            GridManager gridManager)
        {
            if (monsterUnit == null ||
                attackSkill == null ||
                gridManager == null ||
                originGridIndex < 0 ||
                targetGridIndex < 0)
            {
                return int.MaxValue;
            }

            Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
            int bestDistance = int.MaxValue;
            int cellCount = gridManager.Width * gridManager.Height;

            for (int gridIndex = 0; gridIndex < cellCount; gridIndex++)
            {
                if (gridManager.GetCellByIndex(gridIndex) == null)
                    continue;

                if (!CanAttackTargetFromGrid(
                        monsterUnit,
                        gridIndex,
                        targetGridIndex,
                        attackSkill,
                        gridManager))
                {
                    continue;
                }

                Vector2Int attackOriginCoord = gridManager.IndexToCoord(gridIndex);
                int distance =
                    Mathf.Abs(attackOriginCoord.x - originCoord.x) +
                    Mathf.Abs(attackOriginCoord.y - originCoord.y);

                if (distance < bestDistance)
                    bestDistance = distance;
            }

            return bestDistance;
        }

        private static bool IsCharacterCollisionMoveCandidate(
            MonsterUnit monsterUnit,
            int projectedGridIndex,
            int targetGridIndex,
            BattleGridEffectController gridEffectController)
        {
            if (monsterUnit == null || projectedGridIndex < 0 || targetGridIndex < 0)
                return false;

            if (projectedGridIndex != targetGridIndex)
                return false;

            if (gridEffectController != null && gridEffectController.IsBlocked(projectedGridIndex))
                return false;

            if (BattleOccupancyService.IsOccupiedByMonster(projectedGridIndex, monsterUnit))
                return false;

            if (!BattleOccupancyService.TryGetCharacterAtGrid(
                    projectedGridIndex,
                    out BattleCharacter character) ||
                character == null ||
                character.RuntimeData == null ||
                character.RuntimeData.IsDead)
            {
                return false;
            }

            return true;
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

            // 점액은 몬스터에게 적용되지 않으므로 블롭에게 위험 지형이 아닙니다.
            return !string.Equals(
                gridEffectId,
                ResidueGridEffectId,
                System.StringComparison.OrdinalIgnoreCase);
        }

        private int FindNearestAttackableCharacterTarget(
            MonsterUnit monsterUnit,
            int originGridIndex,
            MonsterSkillData attackSkill,
            GridManager gridManager)
        {
            if (monsterUnit == null ||
                originGridIndex < 0 ||
                attackSkill == null ||
                gridManager == null)
            {
                return -1;
            }

            List<int> targets = FindCharacterTargetGridIndices();
            Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
            int nearestGridIndex = -1;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                int targetGridIndex = targets[i];

                if (!CanAttackTargetFromGrid(
                    monsterUnit,
                    originGridIndex,
                    targetGridIndex,
                    attackSkill,
                    gridManager))
                {
                    continue;
                }

                Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);
                int distance =
                    Mathf.Abs(targetCoord.x - originCoord.x) +
                    Mathf.Abs(targetCoord.y - originCoord.y);

                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearestGridIndex = targetGridIndex;
            }

            return nearestGridIndex;
        }

        private bool CanAttackTargetFromGrid(
            MonsterUnit monsterUnit,
            int originGridIndex,
            int targetGridIndex,
            MonsterSkillData attackSkill,
            GridManager gridManager)
        {
            if (monsterUnit == null ||
                attackSkill == null ||
                gridManager == null ||
                originGridIndex < 0 ||
                targetGridIndex < 0)
            {
                return false;
            }

            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;

            if (rangeDatabase == null)
                return false;

            Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

            // 블롭의 공격은 좌우 방향 공격이므로 같은 가로 라인에 있는 대상만 유효합니다.
            if (originCoord.y != targetCoord.y)
                return false;

            BattleDirection direction = targetCoord.x >= originCoord.x
                ? BattleDirection.Right
                : BattleDirection.Left;

            List<int> attackRange = MonsterSkillRangeService.BuildRangeGridIndices(
                monsterUnit,
                attackSkill,
                gridManager,
                direction == BattleDirection.Right,
                originGridIndex,
                rangeDatabase);

            return attackRange != null && attackRange.Contains(targetGridIndex);
        }

        private static BattleDirection GetBlobAttackDirection(
            int originGridIndex,
            int targetGridIndex,
            GridManager gridManager)
        {
            if (gridManager == null || originGridIndex < 0 || targetGridIndex < 0)
                return BattleDirection.Right;

            Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

            return targetCoord.x >= originCoord.x
                ? BattleDirection.Right
                : BattleDirection.Left;
        }

        private static BattleDirection ResolveForwardAttackDirection(
            MonsterUnit monsterUnit,
            Vector2Int moveOffset,
            int originGridIndex,
            int targetGridIndex,
            GridManager gridManager)
        {
            // 가로 이동을 예약했다면 이동 실행 시 바라보게 되는 방향이 곧 정면입니다.
            if (moveOffset.x > 0)
                return BattleDirection.Right;

            if (moveOffset.x < 0)
                return BattleDirection.Left;

            // 세로 이동이거나 이동하지 못한 경우, 예약 시점에 고정한 타겟이 좌우 어느 쪽에 있는지로 정면을 잡습니다.
            if (gridManager != null && originGridIndex >= 0 && targetGridIndex >= 0)
            {
                Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
                Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

                if (targetCoord.x > originCoord.x)
                    return BattleDirection.Right;

                if (targetCoord.x < originCoord.x)
                    return BattleDirection.Left;
            }

            BattleUnitFacing facing = monsterUnit != null
                ? monsterUnit.GetComponent<BattleUnitFacing>()
                : null;

            return facing == null || facing.IsFacingRight
                ? BattleDirection.Right
                : BattleDirection.Left;
        }
    }
}
