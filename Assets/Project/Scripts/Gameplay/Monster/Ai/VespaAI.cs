using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class VespaAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_03";
        private const string AttackSkillId = "S_Monster_07";

        private static readonly List<ReservedDashLine> reservedDashLines = new();
        private static int reservedDashFrame = -1;

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
            PrepareReservedDashLinesForCurrentFrame();

            if (monsterUnit == null || monsterUnit.RuntimeData == null || gridManager == null)
                return plan;

            List<Vector2Int> candidates = GetVespaMoveCandidates();

            Vector2Int moveOffset = GetMoveOffsetForReachableLineTarget(
                monsterUnit,
                gridManager,
                candidates,
                out BattleDirection attackDirection);

            bool canAttackAfterMove = HasPlayerInDashLine(
                monsterUnit,
                gridManager,
                moveOffset,
                out attackDirection);

            if (!canAttackAfterMove)
            {
                moveOffset = GetAttackableMoveOffset(
                    monsterUnit,
                    gridManager,
                    candidates,
                    out attackDirection);

                canAttackAfterMove = HasPlayerInDashLine(
                    monsterUnit,
                    gridManager,
                    moveOffset,
                    out attackDirection);
            }

            if (!canAttackAfterMove)
                moveOffset = GetApproachMoveOffset(monsterUnit, gridManager, candidates);

            bool hasMove = moveOffset != Vector2Int.zero &&
                           CanMonsterMove(monsterUnit, gridManager, moveOffset);

            int group = 1;

            if (hasMove)
            {
                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    moveOffset,
                    MonsterAISlotPreference.Front,
                    group,
                    0
                ));
            }

            if (!canAttackAfterMove)
            {
                canAttackAfterMove = HasPlayerInDashLine(
                    monsterUnit,
                    gridManager,
                    hasMove ? moveOffset : Vector2Int.zero,
                    out attackDirection);
            }

            if (canAttackAfterMove)
            {
                int projectedGridIndex = GetProjectedMainGridIndex(
                    monsterUnit,
                    gridManager,
                    hasMove ? moveOffset : Vector2Int.zero);

                plan.Add(new MonsterAIAction(
                    AttackSkillId,
                    Vector2Int.zero,
                    hasMove ? MonsterAISlotPreference.NextSlot : MonsterAISlotPreference.Front,
                    group,
                    hasMove ? 1 : 0,
                    projectedGridIndex,
                    true,
                    attackDirection
                ));

                ReserveDashLine(monsterUnit, gridManager, hasMove ? moveOffset : Vector2Int.zero, attackDirection);
            }

            return plan;
        }


        private Vector2Int GetMoveOffsetForReachableLineTarget(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            List<Vector2Int> candidates,
            out BattleDirection attackDirection)
        {
            attackDirection = BattleDirection.Left;

            if (monsterUnit == null || gridManager == null || candidates == null || monsterUnit.MainGridIndex < 0)
                return Vector2Int.zero;

            BattleCharacter[] players = FindPlayers();
            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);

            Vector2Int bestOffset = Vector2Int.zero;
            int bestScore = int.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2Int offset = candidates[i];

                if (offset != Vector2Int.zero && !CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                Vector2Int projectedCoord = monsterCoord + offset;

                for (int j = 0; j < players.Length; j++)
                {
                    BattleCharacter player = players[j];

                    if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                        continue;

                    Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);

                    if (playerCoord.y != projectedCoord.y)
                        continue;

                    int dx = playerCoord.x - projectedCoord.x;

                    if (dx == 0)
                        continue;

                    if (ConflictsWithReservedDashLines(monsterUnit, projectedCoord, playerCoord))
                        continue;

                    int lineMovePriority = monsterCoord.y == playerCoord.y ? 1 : 0;
                    int targetDistance = Mathf.Abs(dx);
                    int moveDistance = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);
                    int score = lineMovePriority * 10000 + targetDistance * 100 + moveDistance;

                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    bestOffset = offset;
                    attackDirection = dx > 0 ? BattleDirection.Right : BattleDirection.Left;
                }
            }

            if (bestScore == int.MaxValue)
            {
                attackDirection = BattleDirection.Left;
                return Vector2Int.zero;
            }

            return bestOffset;
        }

        private Vector2Int GetAttackableMoveOffset(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            List<Vector2Int> candidates,
            out BattleDirection attackDirection)
        {
            attackDirection = BattleDirection.Left;

            if (monsterUnit == null || gridManager == null || candidates == null)
                return Vector2Int.zero;

            Vector2Int bestOffset = Vector2Int.zero;
            int bestScore = int.MaxValue;
            int bestMoveDistance = int.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2Int offset = candidates[i];

                if (offset != Vector2Int.zero && !CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                if (!TryFindDashTargetAfterMove(
                        monsterUnit,
                        gridManager,
                        offset,
                        out BattleDirection candidateDirection,
                        out int targetDistance))
                {
                    continue;
                }

                int moveDistance = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);
                int score = targetDistance * 10 + moveDistance;

                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestMoveDistance = moveDistance;
                bestOffset = offset;
                attackDirection = candidateDirection;
            }

            if (bestScore == int.MaxValue)
            {
                attackDirection = BattleDirection.Left;
                return Vector2Int.zero;
            }

            return bestOffset;
        }

        private Vector2Int GetApproachMoveOffset(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            List<Vector2Int> candidates)
        {
            BattleCharacter target = FindFarthestPlayer(monsterUnit, gridManager);

            if (monsterUnit == null || target == null || gridManager == null || candidates == null)
                return Vector2Int.zero;

            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex);

            Vector2Int bestOffset = Vector2Int.zero;
            int bestScore = int.MaxValue;
            int bestMoveDistance = -1;

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2Int offset = candidates[i];

                if (offset == Vector2Int.zero)
                    continue;

                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                Vector2Int projectedCoord = monsterCoord + offset;
                int verticalDistance = Mathf.Abs(targetCoord.y - projectedCoord.y);
                int horizontalDistance = Mathf.Abs(targetCoord.x - projectedCoord.x);
                int moveDistance = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);

                int score = verticalDistance * 100 + horizontalDistance;

                if (score > bestScore)
                    continue;

                if (score == bestScore && moveDistance <= bestMoveDistance)
                    continue;

                bestScore = score;
                bestMoveDistance = moveDistance;
                bestOffset = offset;
            }

            return bestOffset;
        }

        private bool HasPlayerInDashLine(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int moveOffset,
            out BattleDirection attackDirection)
        {
            return TryFindDashTargetAfterMove(
                monsterUnit,
                gridManager,
                moveOffset,
                out attackDirection,
                out _);
        }

        private bool TryFindDashTargetAfterMove(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int moveOffset,
            out BattleDirection attackDirection,
            out int targetDistance)
        {
            return TryFindDashTargetAfterMove(
                monsterUnit,
                gridManager,
                moveOffset,
                out attackDirection,
                out targetDistance,
                out _);
        }

        private bool TryFindDashTargetAfterMove(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int moveOffset,
            out BattleDirection attackDirection,
            out int targetDistance,
            out Vector2Int targetCoord)
        {
            attackDirection = BattleDirection.Left;
            targetDistance = int.MaxValue;
            targetCoord = Vector2Int.zero;

            if (monsterUnit == null || gridManager == null || monsterUnit.MainGridIndex < 0)
                return false;

            Vector2Int originCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex) + moveOffset;
            BattleCharacter[] players = FindPlayers();
            bool found = false;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);

                if (playerCoord.y != originCoord.y)
                    continue;

                int dx = playerCoord.x - originCoord.x;

                if (dx == 0)
                    continue;

                if (ConflictsWithReservedDashLines(monsterUnit, originCoord, playerCoord))
                    continue;

                int distance = Mathf.Abs(dx);

                if (distance >= targetDistance)
                    continue;

                targetDistance = distance;
                targetCoord = playerCoord;
                attackDirection = dx > 0 ? BattleDirection.Right : BattleDirection.Left;
                found = true;
            }

            return found;
        }


        private void ReserveDashLine(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int moveOffset,
            BattleDirection attackDirection)
        {
            if (monsterUnit == null || gridManager == null || monsterUnit.MainGridIndex < 0)
                return;

            if (!TryFindDashTargetAfterMove(
                    monsterUnit,
                    gridManager,
                    moveOffset,
                    out _,
                    out _,
                    out Vector2Int targetCoord))
            {
                return;
            }

            Vector2Int originCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex) + moveOffset;

            for (int i = reservedDashLines.Count - 1; i >= 0; i--)
            {
                if (reservedDashLines[i].RuntimeId == monsterUnit.RuntimeData.RuntimeId)
                    reservedDashLines.RemoveAt(i);
            }

            reservedDashLines.Add(new ReservedDashLine(
                monsterUnit.RuntimeData.RuntimeId,
                originCoord,
                targetCoord,
                attackDirection));
        }

        private static void PrepareReservedDashLinesForCurrentFrame()
        {
            if (reservedDashFrame == Time.frameCount)
                return;

            reservedDashFrame = Time.frameCount;
            reservedDashLines.Clear();
        }

        private bool ConflictsWithReservedDashLines(
            MonsterUnit monsterUnit,
            Vector2Int originCoord,
            Vector2Int targetCoord)
        {
            if (monsterUnit == null || monsterUnit.RuntimeData == null)
                return true;

            for (int i = 0; i < reservedDashLines.Count; i++)
            {
                ReservedDashLine reserved = reservedDashLines[i];

                if (reserved.RuntimeId == monsterUnit.RuntimeData.RuntimeId)
                    continue;

                if (IsCoordOnDashSegment(originCoord, reserved.OriginCoord, reserved.TargetCoord))
                    return true;

                if (IsCoordOnDashSegment(reserved.OriginCoord, originCoord, targetCoord))
                    return true;
            }

            return false;
        }

        private bool IsCoordOnDashSegment(
            Vector2Int coord,
            Vector2Int originCoord,
            Vector2Int targetCoord)
        {
            if (coord.y != originCoord.y || coord.y != targetCoord.y)
                return false;

            int minX = Mathf.Min(originCoord.x, targetCoord.x);
            int maxX = Mathf.Max(originCoord.x, targetCoord.x);

            return coord.x > minX && coord.x < maxX;
        }

        private List<Vector2Int> GetVespaMoveCandidates()
        {
            MonsterSkillData moveSkill =
                DataManager.Instance?.MonsterSkillDatabase.Get(MoveSkillId);

            List<Vector2Int> source = moveSkill != null
                ? MonsterMoveRangeService.GetMoveOffsets(moveSkill.RangeId)
                : new List<Vector2Int>();

            return BuildOneOrTwoCellStraightMoveCandidates(source);
        }

        private List<Vector2Int> BuildOneOrTwoCellStraightMoveCandidates(List<Vector2Int> source)
        {
            List<Vector2Int> result = new();

            AddCandidate(result, Vector2Int.zero);

            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
            {
                Vector2Int offset = source[i];

                AddCandidate(result, offset);

                if (offset.x != 0 && offset.y == 0 && Mathf.Abs(offset.x) >= 2)
                    AddCandidate(result, new Vector2Int(offset.x > 0 ? 1 : -1, 0));

                if (offset.y != 0 && offset.x == 0 && Mathf.Abs(offset.y) >= 2)
                    AddCandidate(result, new Vector2Int(0, offset.y > 0 ? 1 : -1));
            }

            return result;
        }

        private void AddCandidate(List<Vector2Int> candidates, Vector2Int offset)
        {
            if (candidates == null)
                return;

            if (candidates.Contains(offset))
                return;

            candidates.Add(offset);
        }

        private readonly struct ReservedDashLine
        {
            public readonly string RuntimeId;
            public readonly Vector2Int OriginCoord;
            public readonly Vector2Int TargetCoord;
            public readonly BattleDirection Direction;

            public ReservedDashLine(
                string runtimeId,
                Vector2Int originCoord,
                Vector2Int targetCoord,
                BattleDirection direction)
            {
                RuntimeId = runtimeId;
                OriginCoord = originCoord;
                TargetCoord = targetCoord;
                Direction = direction;
            }
        }
    }
}
