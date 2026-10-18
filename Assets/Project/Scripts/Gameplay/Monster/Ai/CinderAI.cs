using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 신더 AI
    /// - 이동 가능한 8방향 1칸을 비교하여 대폭발 범위에 가장 많은 캐릭터가 들어오는 위치를 선택합니다.
    /// - 캐릭터가 멀리 있으면 가장 가까운 캐릭터에게 접근하고, 근접 후보 중 대폭발 범위가 유리한 칸을 선택합니다.
    /// - E_Explode 수치가 0이 된 턴에도 먼저 이동한 뒤 실제 위치에서 대폭발을 사용합니다.
    /// </summary>
    public class CinderAI : MonsterAIBase
    {
        private static readonly HashSet<int> VacatedByEarlierReservations = new HashSet<int>();
        private static readonly HashSet<int> ReservedDestinationCells = new HashSet<int>();
        private static readonly HashSet<int> RejectedDestinationCells = new HashSet<int>();

        // 예약 플래너에서 거절된 칸은 같은 예약 시도의 대체 후보에서 제외합니다.
        public static void SetRejectedDestinationCells(IEnumerable<int> indices)
        {
            RejectedDestinationCells.Clear();
            if (indices == null) return;
            foreach (int index in indices) RejectedDestinationCells.Add(index);
        }

        // 앞서 예약된 다른 몬스터의 도착 칸은 이동 후보에서 제외합니다.
        public static void SetReservedDestinationCells(IEnumerable<int> indices)
        {
            ReservedDestinationCells.Clear();
            if (indices == null) return;
            foreach (int index in indices) ReservedDestinationCells.Add(index);
        }

        // 예약 플래너가 앞서 예약한 이동으로 비워지는 칸을 전달합니다.
        public static void SetPreviouslyVacatedCells(IEnumerable<int> indices)
        {
            VacatedByEarlierReservations.Clear();
            if (indices == null) return;
            foreach (int index in indices) VacatedByEarlierReservations.Add(index);
        }

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
            HashSet<int> occupiedCharacterGridIndices = GetOccupiedCharacterGridIndices();

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
                alivePlayers,
                occupiedCharacterGridIndices);

            int group = 1;
            int projectedGridIndex = monsterUnit.MainGridIndex;

            // 실제 이동 예약이 등록되기 전에는 스프라이트 방향을 바꾸지 않습니다.
            // 최종 예약이 승인된 경우에만 예약 플래너가 방향을 적용합니다.

            if (moveOffset != Vector2Int.zero)
            {
                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    moveOffset,
                    MonsterAISlotPreference.Front,
                    group,
                    0,
                    hasForcedDirection: moveOffset.x != 0,
                    forcedDirection: moveOffset.x > 0 ? BattleDirection.Right : BattleDirection.Left
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

        private HashSet<int> GetOccupiedCharacterGridIndices()
        {
            HashSet<int> result = new();
            BattleCharacter[] players = FindPlayers();

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (player == null || player.CurrentGridIndex < 0)
                    continue;

                result.Add(player.CurrentGridIndex);
            }

            return result;
        }

        private Vector2Int GetBestExplosionPositionMove(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            string explodeRangeId,
            RangeDatabase rangeDatabase,
            List<BattleCharacter> alivePlayers,
            HashSet<int> occupiedCharacterGridIndices)
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
            // 제자리 폭발을 우선하지 않고, 이동 가능한 칸 중 범위와 접근 거리를 비교합니다.
            // 이동할 수 있는 칸이 전혀 없을 때만 현재 위치에 머뭅니다.
            // 먼저 행동하는 신더가 다른 신더에게 가장 유리한 칸을 독점하지 않도록
            // 자신의 위치와 아직 이동을 예약하지 않은 신더의 다음 위치를 함께 평가합니다.
            // 목표 선정과 접근 비교는 MonsterAIBase의 공통 거리 규칙을 사용합니다.
            // 8방향 거리 우선, 동률이면 가로·세로 거리 합으로 비교합니다.
            // 여러 대상의 거리 합계보다 현재 가장 가까운 대상을 먼저 고려합니다.
            int nearestTargetGridIndex = FindNearestCharacterTargetGridIndex(monsterUnit, gridManager);
            if (nearestTargetGridIndex < 0)
                return Vector2Int.zero;
            Vector2Int targetCoord = gridManager.IndexToCoord(nearestTargetGridIndex);
            int bestNearestGridIndex = -1;
            int bestTeamTargets = int.MinValue;
            int bestTeamDistance = int.MaxValue;
            int bestTargetCount = int.MinValue;
            int bestDistanceScore = int.MaxValue;
            Vector2Int bestOffset = Vector2Int.zero;

            MonsterUnit[] otherCinders = Object.FindObjectsByType<MonsterUnit>(FindObjectsSortMode.None);
            for (int i = 0; i < MoveDirections.Length; i++)
            {
                Vector2Int offset = MoveDirections[i];
                if (!CanCinderMove(monsterUnit, gridManager, offset))
                    continue;

                Vector2Int projectedCoord = gridManager.IndexToCoord(currentGridIndex) + offset;
                int projectedGridIndex = gridManager.CoordToIndex(projectedCoord);
                if (RejectedDestinationCells.Contains(projectedGridIndex) ||
                    ReservedDestinationCells.Contains(projectedGridIndex) ||
                    (occupiedCharacterGridIndices != null && occupiedCharacterGridIndices.Contains(projectedGridIndex)) ||
                    BattleOccupancyService.IsOccupiedByCharacter(projectedGridIndex))
                    continue;

                int approachComparison = bestNearestGridIndex < 0
                    ? -1
                    : CompareTargetDistance(targetCoord, projectedCoord,
                        gridManager.IndexToCoord(bestNearestGridIndex));
                int targetCount = GetExplosionTargetCount(
                    projectedGridIndex, explodeRangeId, rangeDatabase, gridManager, alivePlayers);
                int distanceScore = GetExplosionDistanceScore(
                    projectedGridIndex, gridManager, alivePlayers);
                int teamTargets = targetCount;
                int teamDistance = distanceScore;

                // 다른 신더의 도달 가능한 최적 칸을 가상으로 평가합니다.
                // 다른 신더의 현재 위치는 실제 예약 플래너에서 다시 검증합니다.
                for (int peerIndex = 0; peerIndex < otherCinders.Length; peerIndex++)
                {
                    MonsterUnit peer = otherCinders[peerIndex];
                    if (peer == null || peer == monsterUnit || peer.RuntimeData == null ||
                        peer.RuntimeData.IsDead || peer.MainGridIndex < 0 ||
                        !string.Equals(peer.RuntimeData.MonsterId, "Mon_06", System.StringComparison.OrdinalIgnoreCase) ||
                        ReservedDestinationCells.Contains(peer.MainGridIndex))
                        continue;

                    int peerTargets = int.MinValue;
                    int peerDistance = int.MaxValue;
                    Vector2Int peerOrigin = gridManager.IndexToCoord(peer.MainGridIndex);
                    for (int j = 0; j < MoveDirections.Length; j++)
                    {
                        Vector2Int peerOffset = MoveDirections[j];
                        Vector2Int peerCoord = peerOrigin + peerOffset;
                        if (!gridManager.IsValidCoord(peerCoord)) continue;
                        int peerCell = gridManager.CoordToIndex(peerCoord);
                        if (peerCell == projectedGridIndex ||
                            ReservedDestinationCells.Contains(peerCell) ||
                            (occupiedCharacterGridIndices != null && occupiedCharacterGridIndices.Contains(peerCell)) ||
                            BattleOccupancyService.IsOccupiedByCharacter(peerCell))
                            continue;
                        if (!CanCinderMove(peer, gridManager, peerOffset) && peerCell != currentGridIndex)
                            continue;
                        int count = GetExplosionTargetCount(peerCell, explodeRangeId, rangeDatabase, gridManager, alivePlayers);
                        int distance = GetExplosionDistanceScore(peerCell, gridManager, alivePlayers);
                        if (count > peerTargets || (count == peerTargets && distance < peerDistance))
                        {
                            peerTargets = count;
                            peerDistance = distance;
                        }
                    }
                    // 다른 신더가 이동할 후보가 없다면 제자리 점수로 평가하되,
                    // 이동 가능한 배치보다 우선되지 않게 합니다.
                    if (peerTargets == int.MinValue)
                    {
                        peerTargets = GetExplosionTargetCount(peer.MainGridIndex, explodeRangeId, rangeDatabase, gridManager, alivePlayers);
                        peerDistance = GetExplosionDistanceScore(peer.MainGridIndex, gridManager, alivePlayers) + 1000;
                    }
                    teamTargets += peerTargets;
                    teamDistance += peerDistance;
                }

                // 최단거리 접근을 최우선으로 평가합니다. 그 안에서 팀의 폭발 범위와
                // 다른 신더의 위치를 평가하여 같은 도착 칸을 차지하지 않게 합니다.
                if (approachComparison > 0 ||
                    (approachComparison == 0 && teamTargets < bestTeamTargets) ||
                    (approachComparison == 0 && teamTargets == bestTeamTargets && teamDistance > bestTeamDistance) ||
                    (approachComparison == 0 && teamTargets == bestTeamTargets && teamDistance == bestTeamDistance && targetCount < bestTargetCount) ||
                    (approachComparison == 0 && teamTargets == bestTeamTargets && teamDistance == bestTeamDistance &&
                     targetCount == bestTargetCount && distanceScore >= bestDistanceScore))
                    continue;

                bestNearestGridIndex = projectedGridIndex;
                bestTeamTargets = teamTargets;
                bestTeamDistance = teamDistance;
                bestTargetCount = targetCount;
                bestDistanceScore = distanceScore;
                bestOffset = offset;
            }

            // 신더는 별도의 공격 행동을 예약하지 않는 턴에도 위치를 조정해야 합니다.
            // 캐릭터가 인접해 있더라도 이동 가능한 빈칸이 있으면 가장 유리한 칸으로 이동합니다.
            // 이동할 칸이 없을 때만 Vector2Int.zero를 반환합니다.
            return bestOffset;
        }

        private bool CanCinderMove(MonsterUnit unit, GridManager grid, Vector2Int offset)
        {
            if (CanMonsterMove(unit, grid, offset)) return true;
            if (unit == null || unit.MainGridIndex < 0) return false;

            Vector2Int next = grid.IndexToCoord(unit.MainGridIndex) + offset;
            if (!grid.IsValidCoord(next)) return false;
            int index = grid.CoordToIndex(next);
            if (!VacatedByEarlierReservations.Contains(index)) return false;

            // 통행 불가 지형과 캐릭터가 점유한 칸은 예외 없이 차단합니다.
            BattleGridEffectController effects =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);
            if (effects != null && effects.IsBlocked(index)) return false;
            foreach (BattleCharacter player in FindPlayers())
                if (player != null && player.CurrentGridIndex == index) return false;
            return true;
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
                // 신더는 대각선 1칸도 한 번의 이동으로 이동합니다.
                // 따라서 8방향 이동에 맞는 체비쇼프 거리로 접근도를 평가합니다.
                totalDistance += Mathf.Max(
                    Mathf.Abs(playerCoord.x - originCoord.x),
                    Mathf.Abs(playerCoord.y - originCoord.y));
            }

            return totalDistance;
        }

    }
}
