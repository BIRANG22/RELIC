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

            // 원한은 자동 효과가 아니라 랜서가 직접 사용하는 스킬입니다.
            plan.Add(new MonsterAIAction(
                BuffSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Back,
                -1,
                10
            ));

            int targetGridIndex = FindNearestCharacterTargetInActionRange(
                monsterUnit,
                gridManager);

            // 행동범위 안에 캐릭터 또는 Character 타입 그리드 오브젝트가 없다면 공격하거나 이동하지 않습니다.
            if (targetGridIndex < 0)
                return plan;

            int group = 1;

            // 캐릭터가 행동범위 안에 있다면 먼저 주변 8칸 공격을 실행합니다.
            plan.Add(new MonsterAIAction(
                AttackSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Front,
                group,
                0
            ));

            Vector2Int moveOffset = GetBestTwoTileEscapeMove(
                monsterUnit,
                targetGridIndex,
                gridManager);

            if (moveOffset != Vector2Int.zero)
            {
                // 공격과 같은 행동 묶음에 등록하되 우선순위를 뒤로 두어 공격 후 이동합니다.
                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    moveOffset,
                    MonsterAISlotPreference.SameSlot,
                    group,
                    1
                ));
            }

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
            List<int> targets = FindCharacterTargetGridIndices();
            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            int nearestGridIndex = -1;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                int gridIndex = targets[i];

                if (gridIndex < 0 || !actionRangeSet.Contains(gridIndex))
                    continue;

                Vector2Int targetCoord = gridManager.IndexToCoord(gridIndex);
                int distance =
                    Mathf.Abs(targetCoord.x - monsterCoord.x) +
                    Mathf.Abs(targetCoord.y - monsterCoord.y);

                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearestGridIndex = gridIndex;
            }

            return nearestGridIndex;
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

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

            Vector2Int bestOffset = Vector2Int.zero;
            int bestDistance =
                Mathf.Abs(targetCoord.x - currentCoord.x) +
                Mathf.Abs(targetCoord.y - currentCoord.y);

            for (int i = 0; i < EscapeOffsets.Length; i++)
            {
                Vector2Int offset = EscapeOffsets[i];

                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                Vector2Int movedCoord = currentCoord + offset;
                int distance =
                    Mathf.Abs(targetCoord.x - movedCoord.x) +
                    Mathf.Abs(targetCoord.y - movedCoord.y);

                if (distance <= bestDistance)
                    continue;

                bestDistance = distance;
                bestOffset = offset;
            }

            return bestOffset;
        }
    }
}
