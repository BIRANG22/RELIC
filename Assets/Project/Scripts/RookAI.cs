using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 루크 AI
    /// - 매 턴 철옹성을 행동으로 예약해 방어도를 획득합니다.
    /// - 같은 가로 라인에 캐릭터가 있으면 방패돌진을 우선합니다.
    /// - 가까운 캐릭터가 있으면 방패강타를 사용합니다.
    /// - 3턴마다 대지진을 사용합니다.
    /// - 공격 조건이 맞지 않으면 가장 가까운 캐릭터를 향해 8방향 중 1칸 진군합니다.
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

            // 루크는 방어도를 자동으로 얻지 않습니다.
            // 철옹성 스킬을 실제 행동으로 사용할 때만 E_Armor 효과로 방어도를 획득합니다.
            plan.Add(new MonsterAIAction(
                FortressSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Back,
                -1,
                0));

            int nextTurn = runtime.TurnCount + 1;

            if (nextTurn % 3 == 0)
            {
                plan.Add(new MonsterAIAction(
                    EarthquakeSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Front,
                    -1,
                    1));
                return plan;
            }

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
                return plan;
            }

            if (TryBuildBash(monsterUnit.MainGridIndex, gridManager, out BattleDirection bashDirection))
            {
                plan.Add(new MonsterAIAction(
                    BashSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Front,
                    -1,
                    1,
                    monsterUnit.MainGridIndex,
                    true,
                    bashDirection));
                return plan;
            }

            Vector2Int moveOffset = GetBestMoveTowardNearestPlayer(monsterUnit, gridManager, MoveOffsets);

            if (moveOffset == Vector2Int.zero)
                return plan;

            const int sameSlotGroup = 90;
            plan.Add(new MonsterAIAction(
                MoveSkillId,
                moveOffset,
                MonsterAISlotPreference.Front,
                sameSlotGroup,
                1));

            int projectedGridIndex = GetProjectedMainGridIndex(monsterUnit, gridManager, moveOffset);

            if (TryBuildDash(projectedGridIndex, gridManager, out dashDirection))
            {
                plan.Add(new MonsterAIAction(
                    DashSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.SameSlot,
                    sameSlotGroup,
                    2,
                    projectedGridIndex,
                    true,
                    dashDirection));
            }
            else if (TryBuildBash(projectedGridIndex, gridManager, out bashDirection))
            {
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

            return plan;
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
