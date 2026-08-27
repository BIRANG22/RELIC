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

            int targetGridIndex = FindNearestCharacterTargetGridIndex(monsterUnit, gridManager);

            if (targetGridIndex < 0)
                return plan;

            MonsterSkillData attackSkill =
                DataManager.Instance?.MonsterSkillDatabase?.Get(AttackSkillId);

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

            int attackTargetGridIndex = FindNearestAttackableCharacterTarget(
                monsterUnit,
                projectedGridIndex,
                attackSkill,
                gridManager);

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

            Vector2Int bestOffset = Vector2Int.zero;
            int bestPriorityRank = int.MaxValue;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < MoveDirections.Length; i++)
            {
                Vector2Int offset = MoveDirections[i];
                int projectedGridIndex = GetProjectedMainGridIndex(
                    monsterUnit,
                    gridManager,
                    offset);

                bool canMoveNormally = CanMonsterMove(monsterUnit, gridManager, offset);
                bool isCharacterCollisionMove = IsCharacterCollisionMoveCandidate(
                    monsterUnit,
                    projectedGridIndex,
                    targetGridIndex,
                    gridEffectController);

                if (!canMoveNormally && !isCharacterCollisionMove)
                    continue;

                bool candidateCanAttack = FindNearestAttackableCharacterTarget(
                    monsterUnit,
                    projectedGridIndex,
                    attackSkill,
                    gridManager) >= 0;

                bool isRiskyDestination = IsRiskyGridEffectDestination(
                    projectedGridIndex,
                    gridEffectController);

                int priorityRank;

                // 블롭의 이동 목적은 먼저 좌우 공격 위치를 만드는 것입니다.
                // 캐릭터 칸으로의 충돌 이동은 허용하지만, 정상적인 좌우 공격 위치보다 우선하지 않습니다.
                if (candidateCanAttack)
                    priorityRank = isRiskyDestination ? 1 : 0;
                else if (isCharacterCollisionMove)
                    priorityRank = isRiskyDestination ? 5 : 3;
                else
                    priorityRank = isRiskyDestination ? 4 : 2;

                Vector2Int projectedCoord = currentCoord + offset;
                int distance =
                    Mathf.Abs(targetCoord.x - projectedCoord.x) +
                    Mathf.Abs(targetCoord.y - projectedCoord.y);

                if (priorityRank > bestPriorityRank)
                    continue;

                if (priorityRank == bestPriorityRank && distance >= bestDistance)
                    continue;

                bestPriorityRank = priorityRank;
                bestDistance = distance;
                bestOffset = offset;
            }

            return bestOffset;
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

            // 세로 이동이거나 이동하지 못한 경우, 가장 가까운 캐릭터가 좌우 어느 쪽에 있는지로 정면을 잡습니다.
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
