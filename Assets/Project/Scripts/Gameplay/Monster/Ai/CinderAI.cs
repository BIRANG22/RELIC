using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 신더 AI
    /// - 매 턴 충돌하지 않는 8방향 1칸 이동 후보 중 대폭발로 가장 많은 살아 있는 캐릭터를 맞출 수 있는 위치를 선택합니다.
    /// - 같은 수의 캐릭터를 맞출 수 있다면 살아 있는 캐릭터들과의 총거리가 더 가까운 위치를 우선합니다.
    /// - E_Explode 수치가 0이 된 턴에도 먼저 이동한 뒤 실제 위치에서 대폭발을 사용합니다.
    /// </summary>
    public class CinderAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_13";
        private const string ExplodeSkillId = "S_Monster_14";

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
            return monster != null && monster.IsExplodeReady
                ? ExplodeSkillId
                : MoveSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            if (monsterUnit == null || monsterUnit.RuntimeData == null || gridManager == null)
                return plan;

            List<BattleCharacter> alivePlayers = GetAlivePlayers();

            if (alivePlayers.Count <= 0)
                return plan;

            MonsterSkillData explodeSkill =
                DataManager.Instance?.MonsterSkillDatabase?.Get(ExplodeSkillId);
            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;
            string explodeRangeId = explodeSkill?.RangeId;

            if (string.IsNullOrWhiteSpace(explodeRangeId) || rangeDatabase == null)
                return plan;

            Vector2Int moveOffset = GetBestExplosionPositionMove(
                monsterUnit,
                gridManager,
                explodeRangeId,
                rangeDatabase,
                alivePlayers);

            int group = 1;
            int projectedGridIndex = monsterUnit.MainGridIndex;

            if (moveOffset != Vector2Int.zero)
            {
                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    moveOffset,
                    MonsterAISlotPreference.Front,
                    group,
                    0
                ));

                Vector2Int projectedCoord =
                    gridManager.IndexToCoord(monsterUnit.MainGridIndex) + moveOffset;

                if (gridManager.IsValidCoord(projectedCoord))
                    projectedGridIndex = gridManager.CoordToIndex(projectedCoord);
            }

            if (monsterUnit.RuntimeData.IsExplodeReady)
            {
                plan.Add(new MonsterAIAction(
                    ExplodeSkillId,
                    Vector2Int.zero,
                    moveOffset != Vector2Int.zero
                        ? MonsterAISlotPreference.SameSlot
                        : MonsterAISlotPreference.Front,
                    group,
                    moveOffset != Vector2Int.zero ? 1 : 0,
                    projectedGridIndex,
                    rangeOriginCasterGridIndex: projectedGridIndex
                ));
            }

            return plan;
        }

        private List<BattleCharacter> GetAlivePlayers()
        {
            List<BattleCharacter> result = new();
            BattleCharacter[] players = FindPlayers();

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                result.Add(player);
            }

            return result;
        }

        private Vector2Int GetBestExplosionPositionMove(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            string explodeRangeId,
            RangeDatabase rangeDatabase,
            List<BattleCharacter> alivePlayers)
        {
            if (monsterUnit == null ||
                monsterUnit.MainGridIndex < 0 ||
                gridManager == null ||
                alivePlayers == null ||
                alivePlayers.Count <= 0)
            {
                return Vector2Int.zero;
            }

            int currentGridIndex = monsterUnit.MainGridIndex;
            int bestTargetCount = int.MinValue;
            int bestDistanceScore = int.MaxValue;
            Vector2Int bestOffset = Vector2Int.zero;

            for (int i = 0; i < MoveDirections.Length; i++)
            {
                Vector2Int offset = MoveDirections[i];

                // 예약 시점에 이미 점유된 유닛이나 잔해와 충돌하는 이동은 선택하지 않습니다.
                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                Vector2Int projectedCoord =
                    gridManager.IndexToCoord(currentGridIndex) + offset;

                // 기존 신더 규칙대로 폭발 범위가 크게 잘리는 맵 가장자리 칸은 선택하지 않습니다.
                if (IsOuterGrid(projectedCoord, gridManager))
                    continue;

                int projectedGridIndex = gridManager.CoordToIndex(projectedCoord);
                int targetCount = GetExplosionTargetCount(
                    projectedGridIndex,
                    explodeRangeId,
                    rangeDatabase,
                    gridManager,
                    alivePlayers);
                int distanceScore = GetExplosionDistanceScore(
                    projectedGridIndex,
                    gridManager,
                    alivePlayers);

                if (targetCount < bestTargetCount)
                    continue;

                if (targetCount == bestTargetCount && distanceScore >= bestDistanceScore)
                    continue;

                bestTargetCount = targetCount;
                bestDistanceScore = distanceScore;
                bestOffset = offset;
            }

            return bestOffset;
        }

        private static int GetExplosionTargetCount(
            int originGridIndex,
            string explodeRangeId,
            RangeDatabase rangeDatabase,
            GridManager gridManager,
            List<BattleCharacter> alivePlayers)
        {
            List<int> rangeIndices = BattleRangeCalculator.GetSelectionRangeIndices(
                originGridIndex,
                explodeRangeId,
                rangeDatabase,
                gridManager);

            if (rangeIndices == null || rangeIndices.Count <= 0)
                return 0;

            HashSet<int> rangeSet = new(rangeIndices);
            int count = 0;

            for (int i = 0; i < alivePlayers.Count; i++)
            {
                BattleCharacter player = alivePlayers[i];

                if (player == null || player.CurrentGridIndex < 0)
                    continue;

                if (rangeSet.Contains(player.CurrentGridIndex))
                    count++;
            }

            return count;
        }

        private static int GetExplosionDistanceScore(
            int originGridIndex,
            GridManager gridManager,
            List<BattleCharacter> alivePlayers)
        {
            Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
            int totalDistance = 0;

            for (int i = 0; i < alivePlayers.Count; i++)
            {
                BattleCharacter player = alivePlayers[i];

                if (player == null || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                totalDistance +=
                    Mathf.Abs(playerCoord.x - originCoord.x) +
                    Mathf.Abs(playerCoord.y - originCoord.y);
            }

            return totalDistance;
        }

        private bool IsOuterGrid(Vector2Int coord, GridManager gridManager)
        {
            if (gridManager == null)
                return true;

            return coord.x <= 0 ||
                   coord.y <= 0 ||
                   coord.x >= gridManager.Width - 1 ||
                   coord.y >= gridManager.Height - 1;
        }
    }
}
