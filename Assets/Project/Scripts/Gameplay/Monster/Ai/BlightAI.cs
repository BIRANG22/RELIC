using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class BlightAI : MonsterAIBase
    {
        private const string AttackSkillId = "S_Monster_09";
        private const string DebuffSkillId = "S_Monster_10";
        private const string MoveSkillId = "S_Monster_08";

        private static readonly Vector2Int[] EscapeOffsets =
        {
            new Vector2Int(0, 2),
            new Vector2Int(0, -2),
            new Vector2Int(-2, 0),
            new Vector2Int(2, 0)
        };

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return DebuffSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            if (monsterUnit == null || monsterUnit.RuntimeData == null || gridManager == null)
                return plan;

            // 침식은 자동 효과가 아니라 블라이트가 직접 사용하는 스킬입니다.
            plan.Add(new MonsterAIAction(
                DebuffSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Back,
                -1,
                10
            ));

            BattleCharacter target = FindNearestPlayerInActionRange(
                monsterUnit,
                gridManager);

            // 행동범위 안에 캐릭터가 없다면 공격하거나 이동하지 않습니다.
            if (target == null)
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
                target,
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

        private BattleCharacter FindNearestPlayerInActionRange(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            if (monsterUnit == null ||
                monsterUnit.RuntimeData == null ||
                gridManager == null ||
                monsterUnit.MainGridIndex < 0)
            {
                return null;
            }

            string actionRangeId = monsterUnit.RuntimeData.AttackRangeId;

            if (string.IsNullOrWhiteSpace(actionRangeId) || actionRangeId.Trim() == "0")
                return null;

            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;

            if (rangeDatabase == null)
                return null;

            List<int> actionRange = BattleRangeCalculator.GetSelectionRangeIndices(
                monsterUnit.MainGridIndex,
                actionRangeId,
                rangeDatabase,
                gridManager);

            if (actionRange == null || actionRange.Count <= 0)
                return null;

            HashSet<int> actionRangeSet = new(actionRange);
            BattleCharacter[] players = FindPlayers();
            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);

            BattleCharacter nearest = null;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) ||
                    player.CurrentGridIndex < 0 ||
                    !actionRangeSet.Contains(player.CurrentGridIndex))
                {
                    continue;
                }

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                int distance =
                    Mathf.Abs(playerCoord.x - monsterCoord.x) +
                    Mathf.Abs(playerCoord.y - monsterCoord.y);

                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearest = player;
            }

            return nearest;
        }

        private Vector2Int GetBestTwoTileEscapeMove(
            MonsterUnit monsterUnit,
            BattleCharacter target,
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
