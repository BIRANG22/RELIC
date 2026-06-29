using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class VespaAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_01";
        private const string AttackSkillId = "S_Monster_07";

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

            BattleCharacter target = FindFarthestPlayer(monsterUnit, gridManager);

            if (target == null)
                return plan;

            Vector2Int moveOffset = GetVespaMoveOffset(
                monsterUnit,
                target,
                gridManager
            );

            plan.Add(new MonsterAIAction(
                MoveSkillId,
                moveOffset,
                MonsterAISlotPreference.Front,
                1,
                0
            ));

            int projectedGridIndex = GetProjectedMainGridIndex(
                monsterUnit,
                gridManager,
                moveOffset);
            BattleDirection attackDirection = GetHorizontalDirectionToTarget(
                projectedGridIndex,
                target.CurrentGridIndex,
                gridManager);

            plan.Add(new MonsterAIAction(
                AttackSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.NextSlot,
                2,
                1,
                -1,
                true,
                attackDirection
            ));

            return plan;
        }

        private Vector2Int GetVespaMoveOffset(
            MonsterUnit monsterUnit,
            BattleCharacter target,
            GridManager gridManager)
        {
            if (monsterUnit == null || target == null || gridManager == null)
                return Vector2Int.zero;

            MonsterSkillData moveSkill =
                DataManager.Instance?.MonsterSkillDatabase.Get(MoveSkillId);

            if (moveSkill == null)
                return Vector2Int.zero;

            List<Vector2Int> candidates =
                MonsterMoveRangeService.GetMoveOffsets(moveSkill.RangeId);

            if (candidates == null || candidates.Count <= 0)
                return Vector2Int.zero;

            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex);

            int dx = targetCoord.x - monsterCoord.x;
            int dy = targetCoord.y - monsterCoord.y;

            int dirX = dx == 0 ? 0 : dx > 0 ? 1 : -1;
            int dirY = dy == 0 ? 0 : dy > 0 ? 1 : -1;

            Vector2Int diagonal = new Vector2Int(dirX, dirY);
            Vector2Int horizontal = new Vector2Int(dirX, 0);
            Vector2Int vertical = new Vector2Int(0, dirY);

            if (dirY != 0 &&
                IsCandidateValid(monsterUnit, gridManager, candidates, vertical))
                return vertical;

            if (dirX != 0 && dirY != 0 &&
                IsCandidateValid(monsterUnit, gridManager, candidates, diagonal))
                return diagonal;

            if (dirX != 0 &&
                IsCandidateValid(monsterUnit, gridManager, candidates, horizontal))
                return horizontal;

            return GetAnyValidMove(monsterUnit, gridManager, candidates);
        }

        private BattleDirection GetHorizontalDirectionToTarget(
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

        private bool IsCandidateValid(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            List<Vector2Int> candidates,
            Vector2Int offset)
        {
            if (offset == Vector2Int.zero)
                return false;

            if (!candidates.Contains(offset))
                return false;

            return CanMonsterMove(monsterUnit, gridManager, offset);
        }

        private Vector2Int GetAnyValidMove(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            List<Vector2Int> candidates)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (CanMonsterMove(monsterUnit, gridManager, candidates[i]))
                    return candidates[i];
            }

            return Vector2Int.zero;
        }
    }
}
