using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 바로우 AI
    /// - 행동 범위: MonsterMasterData의 AttackRange(Range_20)
    /// - 이동: 가까운 캐릭터가 행동 범위 안에 있으면 십자 방향으로 1~2칸 거리를 벌립니다.
    /// - 직사: 같은 가로 라인에 캐릭터가 있으면 정면 일직선으로 공격합니다.
    /// - 곡사: 같은 가로 라인에 캐릭터가 없으면 가장 먼 캐릭터의 예약 시점 위치와 주변 8칸을 공격합니다.
    /// </summary>
    public class BarrowAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_29";
        private const string DirectShotSkillId = "S_Monster_30";
        private const string ArcShotSkillId = "S_Monster_31";

        private static readonly Vector2Int[] EscapeOffsets =
        {
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0),
            new Vector2Int(0, 2),
            new Vector2Int(0, -2),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return DirectShotSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            if (monsterUnit == null || monsterUnit.RuntimeData == null || gridManager == null)
                return plan;

            const int group = 1;
            Vector2Int moveOffset = Vector2Int.zero;

            int nearbyTargetGridIndex = FindNearestCharacterTargetInActionRange(monsterUnit, gridManager);

            // 캐릭터가 Range_20 안으로 접근했을 때만 거리를 벌립니다.
            if (nearbyTargetGridIndex >= 0)
            {
                moveOffset = GetBestEscapeMove(
                    monsterUnit,
                    nearbyTargetGridIndex,
                    gridManager);

                if (moveOffset != Vector2Int.zero)
                {
                    plan.Add(new MonsterAIAction(
                        MoveSkillId,
                        moveOffset,
                        MonsterAISlotPreference.Front,
                        group,
                        0));
                }
            }

            int projectedGridIndex = GetProjectedMainGridIndex(
                monsterUnit,
                gridManager,
                moveOffset);

            // 이동 후 새 위치에서 같은 가로 라인에 캐릭터가 있는지 다시 판정합니다.
            if (TryBuildDirectShot(
                    projectedGridIndex,
                    gridManager,
                    out BattleDirection directDirection,
                    out List<int> directRange))
            {
                plan.Add(new MonsterAIAction(
                    DirectShotSkillId,
                    Vector2Int.zero,
                    moveOffset != Vector2Int.zero
                        ? MonsterAISlotPreference.SameSlot
                        : MonsterAISlotPreference.Front,
                    group,
                    1,
                    projectedGridIndex,
                    true,
                    directDirection,
                    false,
                    0,
                    directRange));

                return plan;
            }

            // 같은 가로 라인에 캐릭터가 없다면 가장 먼 캐릭터의 현재 그리드를 중심으로 곡사를 예약합니다.
            BattleCharacter farthestPlayer = FindFarthestPlayerFromGrid(projectedGridIndex, gridManager);

            if (farthestPlayer == null || farthestPlayer.CurrentGridIndex < 0)
                return plan;

            List<int> arcRange = BuildThreeByThreeRange(
                farthestPlayer.CurrentGridIndex,
                gridManager);

            if (arcRange.Count <= 0)
                return plan;

            plan.Add(new MonsterAIAction(
                ArcShotSkillId,
                Vector2Int.zero,
                moveOffset != Vector2Int.zero
                    ? MonsterAISlotPreference.SameSlot
                    : MonsterAISlotPreference.Front,
                group,
                1,
                projectedGridIndex,
                false,
                BattleDirection.Right,
                false,
                0,
                arcRange));

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
            BattleCharacter[] players = FindPlayers();
            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);

            int nearestGridIndex = -1;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                if (!actionRangeSet.Contains(player.CurrentGridIndex))
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                int distance =
                    Mathf.Abs(playerCoord.x - monsterCoord.x) +
                    Mathf.Abs(playerCoord.y - monsterCoord.y);

                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearestGridIndex = player.CurrentGridIndex;
            }

            return nearestGridIndex;
        }

        private Vector2Int GetBestEscapeMove(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            GridManager gridManager)
        {
            if (monsterUnit == null || targetGridIndex < 0 || gridManager == null)
                return Vector2Int.zero;

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

        private bool TryBuildDirectShot(
            int originGridIndex,
            GridManager gridManager,
            out BattleDirection direction,
            out List<int> rangeGridIndices)
        {
            direction = BattleDirection.Right;
            rangeGridIndices = new List<int>();

            if (originGridIndex < 0 || gridManager == null)
                return false;

            BattleCharacter[] players = FindPlayers();
            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);

            BattleCharacter bestTarget = null;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);

                if (playerCoord.y != origin.y || playerCoord.x == origin.x)
                    continue;

                int distance = Mathf.Abs(playerCoord.x - origin.x);

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = player;
            }

            if (bestTarget == null)
                return false;

            Vector2Int targetCoord = gridManager.IndexToCoord(bestTarget.CurrentGridIndex);
            int horizontalSign = targetCoord.x > origin.x ? 1 : -1;

            direction = horizontalSign > 0
                ? BattleDirection.Right
                : BattleDirection.Left;

            rangeGridIndices = BuildHorizontalLineToGridEdge(
                originGridIndex,
                gridManager,
                horizontalSign);

            return rangeGridIndices.Count > 0;
        }

        private static List<int> BuildHorizontalLineToGridEdge(
            int originGridIndex,
            GridManager gridManager,
            int horizontalSign)
        {
            List<int> result = new();
            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
            int step = 1;

            while (true)
            {
                Vector2Int coord = origin + new Vector2Int(horizontalSign * step, 0);

                if (!gridManager.IsValidCoord(coord))
                    break;

                result.Add(gridManager.CoordToIndex(coord));
                step++;
            }

            return result;
        }

        private BattleCharacter FindFarthestPlayerFromGrid(
            int originGridIndex,
            GridManager gridManager)
        {
            if (originGridIndex < 0 || gridManager == null)
                return null;

            BattleCharacter[] players = FindPlayers();
            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);

            BattleCharacter farthest = null;
            int farthestDistance = -1;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                int distance =
                    Mathf.Abs(playerCoord.x - origin.x) +
                    Mathf.Abs(playerCoord.y - origin.y);

                if (distance <= farthestDistance)
                    continue;

                farthestDistance = distance;
                farthest = player;
            }

            return farthest;
        }

        private static List<int> BuildThreeByThreeRange(
            int centerGridIndex,
            GridManager gridManager)
        {
            List<int> result = new();

            if (centerGridIndex < 0 || gridManager == null)
                return result;

            Vector2Int center = gridManager.IndexToCoord(centerGridIndex);

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    Vector2Int coord = center + new Vector2Int(x, y);

                    if (!gridManager.IsValidCoord(coord))
                        continue;

                    result.Add(gridManager.CoordToIndex(coord));
                }
            }

            return result;
        }
    }
}
