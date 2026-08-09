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

            // 현재 위치에서 이미 공격 가능한 캐릭터가 있다면 이동하지 않고 바로 공격합니다.
            // 블롭의 잔여물은 이동했을 때만 생성되어야 하므로, 공격 가능 상태에서 불필요한 이동을 예약하지 않습니다.
            int currentAttackTargetGridIndex = FindNearestAttackableCharacterTarget(
                monsterUnit,
                monsterUnit.MainGridIndex,
                attackSkill,
                gridManager);

            if (currentAttackTargetGridIndex >= 0)
            {
                BattleDirection currentAttackDirection = GetBlobAttackDirection(
                    monsterUnit.MainGridIndex,
                    currentAttackTargetGridIndex,
                    gridManager);

                plan.Add(new MonsterAIAction(
                    AttackSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.SameSlot,
                    1,
                    0,
                    monsterUnit.MainGridIndex,
                    true,
                    currentAttackDirection
                ));

                return plan;
            }

            // 현재 위치에서 공격할 수 없을 때만 십자 방향으로 1칸 이동합니다.
            // 이동 후 실제로 공격이 닿는 위치를 최우선으로 선택하고,
            // 그런 위치가 없다면 가장 가까워지는 위치로 이동합니다.
            Vector2Int moveOffset = GetBestCardinalMove(
                monsterUnit,
                targetGridIndex,
                attackSkill,
                gridManager);

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

            int projectedGridIndex = GetProjectedMainGridIndex(
                monsterUnit,
                gridManager,
                canMove ? moveOffset : Vector2Int.zero);

            int attackTargetGridIndex = FindNearestAttackableCharacterTarget(
                monsterUnit,
                projectedGridIndex,
                attackSkill,
                gridManager);

            // 이동을 마친 위치에서 실제 공격 범위 안에 캐릭터 또는 Character 타입 오브젝트가 없으면
            // 빈 방향으로 공격하지 않습니다.
            if (attackTargetGridIndex < 0)
                return plan;

            BattleDirection attackDirection = GetBlobAttackDirection(
                projectedGridIndex,
                attackTargetGridIndex,
                gridManager);

            plan.Add(new MonsterAIAction(
                AttackSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.SameSlot,
                group,
                canMove ? 1 : 0,
                projectedGridIndex,
                true,
                attackDirection
            ));

            return plan;
        }

        private Vector2Int GetBestCardinalMove(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            MonsterSkillData attackSkill,
            GridManager gridManager)
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

            Vector2Int bestAttackOffset = Vector2Int.zero;
            int bestAttackDistance = int.MaxValue;

            Vector2Int bestApproachOffset = Vector2Int.zero;
            int bestApproachDistance = int.MaxValue;

            for (int i = 0; i < MoveDirections.Length; i++)
            {
                Vector2Int offset = MoveDirections[i];

                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                int projectedGridIndex = GetProjectedMainGridIndex(
                    monsterUnit,
                    gridManager,
                    offset);

                Vector2Int projectedCoord = currentCoord + offset;
                int distance =
                    Mathf.Abs(targetCoord.x - projectedCoord.x) +
                    Mathf.Abs(targetCoord.y - projectedCoord.y);

                if (CanAttackTargetFromGrid(
                    monsterUnit,
                    projectedGridIndex,
                    targetGridIndex,
                    attackSkill,
                    gridManager))
                {
                    if (distance < bestAttackDistance)
                    {
                        bestAttackDistance = distance;
                        bestAttackOffset = offset;
                    }

                    continue;
                }

                if (distance < bestApproachDistance)
                {
                    bestApproachDistance = distance;
                    bestApproachOffset = offset;
                }
            }

            return bestAttackOffset != Vector2Int.zero
                ? bestAttackOffset
                : bestApproachOffset;
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

            // 블롭의 근거리 공격은 좌우 방향 공격이므로 같은 가로 라인만 유효합니다.
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
    }
}
