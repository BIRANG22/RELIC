using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class BlobAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_02";
        private const string AttackSkillId = "S_Monster_05";

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

            BattleCharacter target = FindNearestPlayer(monsterUnit, gridManager);

            if (target == null || target.CurrentGridIndex < 0)
                return plan;

            MonsterSkillData attackSkill =
                DataManager.Instance?.MonsterSkillDatabase?.Get(AttackSkillId);

            // 블롭은 십자 방향으로만 1칸 이동합니다.
            // 이동 후 실제로 공격이 닿는 위치를 최우선으로 선택하고,
            // 그런 위치가 없다면 가장 가까워지는 위치로 이동합니다.
            Vector2Int moveOffset = GetBestCardinalMove(
                monsterUnit,
                target,
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

            BattleCharacter attackTarget = FindNearestAttackablePlayer(
                monsterUnit,
                projectedGridIndex,
                attackSkill,
                gridManager);

            // 이동을 마친 위치에서 실제 공격 범위 안에 캐릭터가 없으면
            // 빈 방향으로 공격하지 않습니다.
            if (attackTarget == null)
                return plan;

            BattleDirection attackDirection = GetBlobAttackDirection(
                projectedGridIndex,
                attackTarget.CurrentGridIndex,
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
            BattleCharacter target,
            MonsterSkillData attackSkill,
            GridManager gridManager)
        {
            if (monsterUnit == null ||
                target == null ||
                gridManager == null ||
                monsterUnit.MainGridIndex < 0 ||
                target.CurrentGridIndex < 0)
            {
                return Vector2Int.zero;
            }

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex);

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
                    target,
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

        private BattleCharacter FindNearestAttackablePlayer(
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
                return null;
            }

            BattleCharacter[] players = FindPlayers();
            Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);

            BattleCharacter nearest = null;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                if (!CanAttackTargetFromGrid(
                    monsterUnit,
                    originGridIndex,
                    player,
                    attackSkill,
                    gridManager))
                {
                    continue;
                }

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                int distance =
                    Mathf.Abs(playerCoord.x - originCoord.x) +
                    Mathf.Abs(playerCoord.y - originCoord.y);

                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearest = player;
            }

            return nearest;
        }

        private bool CanAttackTargetFromGrid(
            MonsterUnit monsterUnit,
            int originGridIndex,
            BattleCharacter target,
            MonsterSkillData attackSkill,
            GridManager gridManager)
        {
            if (monsterUnit == null ||
                target == null ||
                attackSkill == null ||
                gridManager == null ||
                originGridIndex < 0 ||
                target.CurrentGridIndex < 0)
            {
                return false;
            }

            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;

            if (rangeDatabase == null)
                return false;

            Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex);

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

            return attackRange != null && attackRange.Contains(target.CurrentGridIndex);
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
