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
        private const string ResidueGridEffectId = "GR_Residue";

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

            MonsterSkillData attackSkill =
                DataManager.Instance?.MonsterSkillDatabase?.Get(AttackSkillId);

            // 블롭은 단순히 가장 가까운 캐릭터보다 이번 행동에서 실제로 좌우 공격을
            // 성공시킬 수 있는 캐릭터를 우선 타겟으로 고정합니다.
            BattleGridEffectController gridEffectController =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            // Choose a character with a reachable horizontal attack position.
            // Distance alone can cause endless oscillation around a blocked attack lane.
            int targetGridIndex = FindReachableBlobTarget(
                monsterUnit, attackSkill, gridManager, gridEffectController,
                out Vector2Int moveOffset);

            if (targetGridIndex < 0)
                return plan;

            int plannedMoveGridIndex = GetProjectedMainGridIndex(
                monsterUnit,
                gridManager,
                moveOffset);

            bool isCharacterCollisionMove = IsCharacterCollisionMoveCandidate(
                monsterUnit,
                plannedMoveGridIndex,
                targetGridIndex,
                gridEffectController);

            bool canMoveNormally = moveOffset != Vector2Int.zero &&
                                   CanMonsterMove(monsterUnit, gridManager, moveOffset);
            bool canMove = moveOffset != Vector2Int.zero &&
                           (canMoveNormally || isCharacterCollisionMove);

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

            // 이동 목표는 예약 시점에 선택한 우선 타겟으로 고정합니다.
            // 다른 캐릭터가 공격 범위에 들어오더라도 이동/공격 방향을 바꾸지 않습니다.
            int attackTargetGridIndex = CanAttackTargetFromGrid(
                monsterUnit,
                projectedGridIndex,
                targetGridIndex,
                attackSkill,
                gridManager)
                ? targetGridIndex
                : -1;

            // 공격 범위 안에 캐릭터가 있으면 그 캐릭터 방향을 공격합니다.
            // 범위 안에 캐릭터가 없어도 공격 예약을 취소하지 않고 이동 후 바라보는 정면을 공격합니다.
            BattleDirection attackDirection = attackTargetGridIndex >= 0
                ? GetBlobAttackDirection(projectedGridIndex, attackTargetGridIndex, gridManager)
                : ResolveForwardAttackDirection(
                    monsterUnit,
                    moveOffset,
                    projectedGridIndex,
                    targetGridIndex,
                    gridManager);

            plan.Add(new MonsterAIAction(
                AttackSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.SameSlot,
                group,
                canMove ? 1 : 0,
                projectedGridIndex,
                true,
                attackDirection,
                rangeOriginCasterGridIndex: projectedGridIndex
            ));

            return plan;
        }

        private int FindReachableBlobTarget(
            MonsterUnit monsterUnit,
            MonsterSkillData attackSkill,
            GridManager gridManager,
            BattleGridEffectController gridEffectController,
            out Vector2Int moveOffset)
        {
            moveOffset = Vector2Int.zero;
            if (monsterUnit == null || attackSkill == null || gridManager == null ||
                monsterUnit.MainGridIndex < 0)
                return -1;

            List<int> targets = FindCharacterTargetGridIndices();
            int chosen = -1;
            int lowestCost = int.MaxValue;
            List<int> tiedTargets = new();
            List<Vector2Int> tiedMoves = new();

            for (int i = 0; i < targets.Count; i++)
            {
                int target = targets[i];
                if (target < 0 || gridManager.GetCellByIndex(target) == null)
                    continue;

                if (!TryFindAttackRoute(monsterUnit, target, attackSkill, gridManager,
                        gridEffectController, out Vector2Int step, out int cost))
                    continue;

                if (cost < lowestCost)
                {
                    lowestCost = cost;
                    tiedTargets.Clear();
                    tiedMoves.Clear();
                }
                if (cost == lowestCost)
                {
                    tiedTargets.Add(target);
                    tiedMoves.Add(step);
                }
            }

            if (tiedTargets.Count > 0)
            {
                int pick = BattleRandom.Range(0, tiedTargets.Count);
                chosen = tiedTargets[pick];
                moveOffset = tiedMoves[pick];
                return chosen;
            }

            // A blocked target must not keep the Blob bouncing between cells.
            // If there is no reachable attacking lane, move once to spread residue,
            // without locking onto an impossible character.
            List<Vector2Int> validSteps = new();
            for (int i = 0; i < MoveDirections.Length; i++)
                if (CanMonsterMove(monsterUnit, gridManager, MoveDirections[i]))
                    validSteps.Add(MoveDirections[i]);

            if (validSteps.Count > 0)
                moveOffset = validSteps[BattleRandom.Range(0, validSteps.Count)];

            // Preserve the existing forward attack reservation even without a target route.
            // Prefer an actual candidate as the facing reference.
            return targets.Count > 0 ? targets[BattleRandom.Range(0, targets.Count)] : -1;
        }

        private bool TryFindAttackRoute(
            MonsterUnit monsterUnit, int targetGridIndex, MonsterSkillData attackSkill,
            GridManager gridManager, BattleGridEffectController gridEffectController,
            out Vector2Int firstMove, out int routeCost)
        {
            firstMove = Vector2Int.zero;
            routeCost = int.MaxValue;
            int count = gridManager.Width * gridManager.Height;
            int origin = monsterUnit.MainGridIndex;
            if (origin < 0 || origin >= count || targetGridIndex < 0 ||
                targetGridIndex >= count)
                return false;

            // A character one step ahead is a valid collision destination.
            Vector2Int sourceCoord = gridManager.IndexToCoord(origin);
            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);
            Vector2Int delta = targetCoord - sourceCoord;
            if (delta.y == 0 && Mathf.Abs(delta.x) == 1 &&
                IsCharacterCollisionMoveCandidate(monsterUnit, targetGridIndex,
                    targetGridIndex, gridEffectController))
            {
                firstMove = delta;
                routeCost = 1;
                return true;
            }

            int[] costs = new int[count];
            Vector2Int[] initialSteps = new Vector2Int[count];
            bool[] done = new bool[count];
            for (int i = 0; i < count; i++) costs[i] = int.MaxValue;
            costs[origin] = 0;

            for (int iteration = 0; iteration < count; iteration++)
            {
                int current = -1;
                for (int i = 0; i < count; i++)
                    if (!done[i] && costs[i] != int.MaxValue &&
                        (current < 0 || costs[i] < costs[current])) current = i;
                if (current < 0) break;
                done[current] = true;

                if (CanAttackTargetFromGrid(monsterUnit, current, targetGridIndex,
                    attackSkill, gridManager))
                {
                    // Even when already in attack range, Blob still attempts to move.
                    // Try an adjacent safe step (and attack from the new position).
                    if (current == origin)
                    {
                        List<Vector2Int> options = new();
                        for (int d = 0; d < MoveDirections.Length; d++)
                            if (CanMonsterMove(monsterUnit, gridManager, MoveDirections[d]))
                                options.Add(MoveDirections[d]);
                        if (options.Count > 0)
                        {
                            firstMove = options[BattleRandom.Range(0, options.Count)];
                            routeCost = 2;
                            return true;
                        }
                        return false;
                    }
                    firstMove = initialSteps[current];
                    routeCost = costs[current];
                    return true;
                }

                Vector2Int currentCoord = gridManager.IndexToCoord(current);
                for (int d = 0; d < MoveDirections.Length; d++)
                {
                    Vector2Int direction = MoveDirections[d];
                    Vector2Int nextCoord = currentCoord + direction;
                    if (!gridManager.IsValidCoord(nextCoord)) continue;
                    int next = gridManager.CoordToIndex(nextCoord);
                    if (next < 0 || next >= count || done[next] ||
                        gridManager.GetCellByIndex(next) == null ||
                        (gridEffectController != null && gridEffectController.IsBlocked(next)) ||
                        BattleOccupancyService.IsOccupiedByAnyUnit(next, null, monsterUnit))
                        continue;
                    if (current == origin && !CanMonsterMove(monsterUnit, gridManager, direction))
                        continue;

                    int cost = costs[current] + 10 +
                        (IsRiskyGridEffectDestination(next, gridEffectController) ? 1000 : 0);
                    if (cost >= costs[next]) continue;
                    costs[next] = cost;
                    initialSteps[next] = current == origin ? direction : initialSteps[current];
                }
            }
            return false;
        }

        private int FindBestBlobTargetGridIndex(
            MonsterUnit monsterUnit,
            MonsterSkillData attackSkill,
            GridManager gridManager)
        {
            if (monsterUnit == null || gridManager == null || monsterUnit.MainGridIndex < 0)
                return -1;

            List<int> targets = FindCharacterTargetGridIndices();

            if (targets == null || targets.Count == 0)
                return -1;

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            int bestAttackableTarget = -1;
            int bestAttackableDistance = int.MaxValue;
            int nearestTarget = -1;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                int candidateGridIndex = targets[i];
                Vector2Int candidateCoord = gridManager.IndexToCoord(candidateGridIndex);
                int distance =
                    Mathf.Abs(candidateCoord.x - currentCoord.x) +
                    Mathf.Abs(candidateCoord.y - currentCoord.y);

                if (distance < nearestDistance ||
                    (distance == nearestDistance && candidateGridIndex < nearestTarget))
                {
                    nearestDistance = distance;
                    nearestTarget = candidateGridIndex;
                }

                if (!CanAttackTargetThisTurn(
                        monsterUnit,
                        candidateGridIndex,
                        attackSkill,
                        gridManager))
                {
                    continue;
                }

                if (distance < bestAttackableDistance ||
                    (distance == bestAttackableDistance && candidateGridIndex < bestAttackableTarget))
                {
                    bestAttackableDistance = distance;
                    bestAttackableTarget = candidateGridIndex;
                }
            }

            return bestAttackableTarget >= 0
                ? bestAttackableTarget
                : nearestTarget;
        }

        private bool CanAttackTargetThisTurn(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            MonsterSkillData attackSkill,
            GridManager gridManager)
        {
            if (monsterUnit == null ||
                attackSkill == null ||
                gridManager == null ||
                monsterUnit.MainGridIndex < 0 ||
                targetGridIndex < 0)
            {
                return false;
            }

            // 이미 좌우 공격 범위에 들어온 타겟도 이번 행동에서 공격 가능한 대상으로 봅니다.
            // 이 경우 실제 이동 단계에서는 후퇴하지 않고 접근/충돌 규칙을 따릅니다.
            if (CanAttackTargetFromGrid(
                    monsterUnit,
                    monsterUnit.MainGridIndex,
                    targetGridIndex,
                    attackSkill,
                    gridManager))
            {
                return true;
            }

            // 한 칸 정상 이동 후 바로 좌우 공격이 가능한지를 예약 시점에만 판단합니다.
            for (int i = 0; i < MoveDirections.Length; i++)
            {
                Vector2Int moveOffset = MoveDirections[i];

                if (!CanMonsterMove(monsterUnit, gridManager, moveOffset))
                    continue;

                int projectedGridIndex = GetProjectedMainGridIndex(
                    monsterUnit,
                    gridManager,
                    moveOffset);

                if (CanAttackTargetFromGrid(
                        monsterUnit,
                        projectedGridIndex,
                        targetGridIndex,
                        attackSkill,
                        gridManager))
                {
                    return true;
                }
            }

            return false;
        }

        private Vector2Int GetBestCardinalMove(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            MonsterSkillData attackSkill,
            GridManager gridManager,
            BattleGridEffectController gridEffectController)
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

            // 블롭은 출발 위치에 점액을 남기기 위해 공격 가능 여부와 무관하게 이동을 시도합니다.
            // 정면의 캐릭터가 자리를 지키면 해당 칸으로 이동을 예약해 충돌하게 됩니다.

            // 타겟이 좌/우에 있으면 후퇴하거나 다른 방향으로 공격각을 만들지 않습니다.
            // 타겟 쪽으로 계속 접근하며, 바로 앞을 타겟이 막고 있으면 충돌을 시도합니다.
            if (currentCoord.y == targetCoord.y && currentCoord.x != targetCoord.x)
            {
                Vector2Int towardTarget = targetCoord.x > currentCoord.x
                    ? Vector2Int.right
                    : Vector2Int.left;

                int projectedGridIndex = GetProjectedMainGridIndex(
                    monsterUnit,
                    gridManager,
                    towardTarget);

                bool canMoveNormally = CanMonsterMove(monsterUnit, gridManager, towardTarget);
                bool canCollideWithTarget = IsCharacterCollisionMoveCandidate(
                    monsterUnit,
                    projectedGridIndex,
                    targetGridIndex,
                    gridEffectController);

                if (canMoveNormally || canCollideWithTarget)
                    return towardTarget;

                // 정면이 완전히 막힌 경우에만 우회 이동을 찾습니다.
                // 이때도 타겟과의 거리가 더 멀어지는 후퇴 이동은 허용하지 않습니다.
                return FindBestNonRetreatMove(
                    monsterUnit,
                    targetGridIndex,
                    attackSkill,
                    gridManager,
                    gridEffectController,
                    currentCoord,
                    targetCoord,
                    excludeVerticalCollision: false);
            }

            // 타겟이 위/아래에 있으면 그 타겟에게 직접 충돌해도 좌우 공격은 성공하지 못합니다.
            // 따라서 좌/우 이동으로 공격 위치를 준비하는 것을 우선하고 세로 충돌은 하지 않습니다.
            if (currentCoord.x == targetCoord.x && currentCoord.y != targetCoord.y)
            {
                Vector2Int horizontalMove = FindBestHorizontalSetupMove(
                    monsterUnit,
                    targetGridIndex,
                    attackSkill,
                    gridManager,
                    gridEffectController);

                if (horizontalMove != Vector2Int.zero)
                    return horizontalMove;

                return FindBestNonRetreatMove(
                    monsterUnit,
                    targetGridIndex,
                    attackSkill,
                    gridManager,
                    gridEffectController,
                    currentCoord,
                    targetCoord,
                    excludeVerticalCollision: true);
            }

            // 대각선에 있는 경우에는 고정 타겟의 좌우 공격 위치를 준비하는 정상 이동만 평가합니다.
            // 캐릭터 칸으로 직접 들어가는 충돌은 좌우 정렬이 된 경우에만 사용합니다.
            return FindBestNonRetreatMove(
                monsterUnit,
                targetGridIndex,
                attackSkill,
                gridManager,
                gridEffectController,
                currentCoord,
                targetCoord,
                excludeVerticalCollision: true);
        }

        private Vector2Int FindBestHorizontalSetupMove(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            MonsterSkillData attackSkill,
            GridManager gridManager,
            BattleGridEffectController gridEffectController)
        {
            Vector2Int bestOffset = Vector2Int.zero;
            int bestSetupDistance = int.MaxValue;
            bool bestIsRisky = true;
            int bestTargetDistance = int.MaxValue;
            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

            Vector2Int[] horizontalDirections =
            {
                Vector2Int.left,
                Vector2Int.right
            };

            for (int i = 0; i < horizontalDirections.Length; i++)
            {
                Vector2Int offset = horizontalDirections[i];

                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                int projectedGridIndex = GetProjectedMainGridIndex(monsterUnit, gridManager, offset);
                Vector2Int projectedCoord = gridManager.IndexToCoord(projectedGridIndex);
                bool candidateCanAttack = CanAttackTargetFromGrid(
                    monsterUnit,
                    projectedGridIndex,
                    targetGridIndex,
                    attackSkill,
                    gridManager);
                int setupDistance = candidateCanAttack
                    ? 0
                    : GetAttackSetupDistance(
                        monsterUnit,
                        projectedGridIndex,
                        targetGridIndex,
                        attackSkill,
                        gridManager);
                bool isRisky = IsRiskyGridEffectDestination(projectedGridIndex, gridEffectController);
                int targetDistance =
                    Mathf.Abs(targetCoord.x - projectedCoord.x) +
                    Mathf.Abs(targetCoord.y - projectedCoord.y);

                bool isBetter = bestOffset == Vector2Int.zero ||
                                setupDistance < bestSetupDistance ||
                                (setupDistance == bestSetupDistance && bestIsRisky && !isRisky) ||
                                (setupDistance == bestSetupDistance && bestIsRisky == isRisky &&
                                 targetDistance < bestTargetDistance);

                if (!isBetter)
                    continue;

                bestOffset = offset;
                bestSetupDistance = setupDistance;
                bestIsRisky = isRisky;
                bestTargetDistance = targetDistance;
            }

            return bestOffset;
        }

        private Vector2Int FindBestNonRetreatMove(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            MonsterSkillData attackSkill,
            GridManager gridManager,
            BattleGridEffectController gridEffectController,
            Vector2Int currentCoord,
            Vector2Int targetCoord,
            bool excludeVerticalCollision)
        {
            int currentTargetDistance =
                Mathf.Abs(targetCoord.x - currentCoord.x) +
                Mathf.Abs(targetCoord.y - currentCoord.y);

            Vector2Int bestOffset = Vector2Int.zero;
            bool bestCanAttack = false;
            int bestSetupDistance = int.MaxValue;
            bool bestIsRisky = true;
            int bestTargetDistance = int.MaxValue;

            for (int i = 0; i < MoveDirections.Length; i++)
            {
                Vector2Int offset = MoveDirections[i];
                int projectedGridIndex = GetProjectedMainGridIndex(monsterUnit, gridManager, offset);
                bool canMoveNormally = CanMonsterMove(monsterUnit, gridManager, offset);
                bool isCollision = IsCharacterCollisionMoveCandidate(
                    monsterUnit,
                    projectedGridIndex,
                    targetGridIndex,
                    gridEffectController);

                if (!canMoveNormally && !isCollision)
                    continue;

                if (isCollision && excludeVerticalCollision && offset.y != 0)
                    continue;

                Vector2Int projectedCoord = currentCoord + offset;
                int targetDistance =
                    Mathf.Abs(targetCoord.x - projectedCoord.x) +
                    Mathf.Abs(targetCoord.y - projectedCoord.y);

                // 공격 성공을 위해 움직이더라도 고정 타겟에게서 멀어지는 후퇴는 하지 않습니다.
                if (targetDistance > currentTargetDistance)
                    continue;

                // 충돌은 좌우 공격 위치를 만드는 정상 이동보다 우선하지 않습니다.
                bool candidateCanAttack = !isCollision && CanAttackTargetFromGrid(
                    monsterUnit,
                    projectedGridIndex,
                    targetGridIndex,
                    attackSkill,
                    gridManager);
                int setupDistance = candidateCanAttack
                    ? 0
                    : GetAttackSetupDistance(
                        monsterUnit,
                        projectedGridIndex,
                        targetGridIndex,
                        attackSkill,
                        gridManager);
                bool isRisky = IsRiskyGridEffectDestination(projectedGridIndex, gridEffectController);

                bool isBetter = bestOffset == Vector2Int.zero;

                if (!isBetter && candidateCanAttack != bestCanAttack)
                    isBetter = candidateCanAttack;
                else if (!isBetter && setupDistance != bestSetupDistance)
                    isBetter = setupDistance < bestSetupDistance;
                else if (!isBetter && isRisky != bestIsRisky)
                    isBetter = !isRisky;
                else if (!isBetter && targetDistance != bestTargetDistance)
                    isBetter = targetDistance < bestTargetDistance;

                if (!isBetter)
                    continue;

                bestOffset = offset;
                bestCanAttack = candidateCanAttack;
                bestSetupDistance = setupDistance;
                bestIsRisky = isRisky;
                bestTargetDistance = targetDistance;
            }

            // The strict non-retreat rule can strand a Blob behind another monster.
            // When no normal step is possible, allow a safe detour toward a tile
            // from which its one-cell forward attack can actually reach the target.
            if (bestOffset == Vector2Int.zero)
                return FindAttackPositionDetour(monsterUnit, targetGridIndex,
                    attackSkill, gridManager, gridEffectController);

            return bestOffset;
        }

        private Vector2Int FindAttackPositionDetour(
            MonsterUnit monsterUnit,
            int targetGridIndex,
            MonsterSkillData attackSkill,
            GridManager gridManager,
            BattleGridEffectController gridEffectController)
        {
            if (monsterUnit == null || attackSkill == null || gridManager == null)
                return Vector2Int.zero;

            int cellCount = gridManager.Width * gridManager.Height;
            int[] cost = new int[cellCount];
            Vector2Int[] firstStep = new Vector2Int[cellCount];
            bool[] visited = new bool[cellCount];
            for (int i = 0; i < cellCount; i++)
                cost[i] = int.MaxValue;

            int origin = monsterUnit.MainGridIndex;
            if (origin < 0 || origin >= cellCount)
                return Vector2Int.zero;

            cost[origin] = 0;
            for (int iteration = 0; iteration < cellCount; iteration++)
            {
                int current = -1;
                for (int i = 0; i < cellCount; i++)
                {
                    if (visited[i] || cost[i] == int.MaxValue ||
                        (current >= 0 && cost[i] >= cost[current]))
                        continue;
                    current = i;
                }
                if (current < 0)
                    break;
                visited[current] = true;

                if (current != origin && firstStep[current] != Vector2Int.zero &&
                    CanAttackTargetFromGrid(monsterUnit, current, targetGridIndex,
                        attackSkill, gridManager))
                    return firstStep[current];

                Vector2Int coord = gridManager.IndexToCoord(current);
                for (int d = 0; d < MoveDirections.Length; d++)
                {
                    Vector2Int step = MoveDirections[d];
                    Vector2Int nextCoord = coord + step;
                    if (!gridManager.IsValidCoord(nextCoord))
                        continue;

                    int next = gridManager.CoordToIndex(nextCoord);
                    if (next < 0 || next >= cellCount || visited[next] ||
                        gridManager.GetCellByIndex(next) == null ||
                        (gridEffectController != null && gridEffectController.IsBlocked(next)))
                        continue;

                    if (BattleOccupancyService.TryGetCharacterAtGrid(next, out BattleCharacter character) &&
                        character != null && character.RuntimeData != null && !character.RuntimeData.IsDead)
                        continue;

                    bool occupiedByMonster = BattleOccupancyService.IsOccupiedByMonster(next, monsterUnit);
                    if (current == origin &&
                        (occupiedByMonster || !CanMonsterMove(monsterUnit, gridManager, step)))
                        continue;

                    // Another monster may vacate its cell on a subsequent turn.
                    // Penalize that future route, but never move into it right now.
                    int stepCost = 10 + (occupiedByMonster ? 100 : 0) +
                        (IsRiskyGridEffectDestination(next, gridEffectController) ? 1000 : 0);
                    int newCost = cost[current] + stepCost;
                    if (newCost >= cost[next])
                        continue;

                    cost[next] = newCost;
                    firstStep[next] = current == origin ? step : firstStep[current];
                }
            }

            return Vector2Int.zero;
        }

        private int GetAttackSetupDistance(
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
                return int.MaxValue;
            }

            Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
            int bestDistance = int.MaxValue;
            int cellCount = gridManager.Width * gridManager.Height;

            for (int gridIndex = 0; gridIndex < cellCount; gridIndex++)
            {
                if (gridManager.GetCellByIndex(gridIndex) == null)
                    continue;

                if (!CanAttackTargetFromGrid(
                        monsterUnit,
                        gridIndex,
                        targetGridIndex,
                        attackSkill,
                        gridManager))
                {
                    continue;
                }

                Vector2Int attackOriginCoord = gridManager.IndexToCoord(gridIndex);
                int distance =
                    Mathf.Abs(attackOriginCoord.x - originCoord.x) +
                    Mathf.Abs(attackOriginCoord.y - originCoord.y);

                if (distance < bestDistance)
                    bestDistance = distance;
            }

            return bestDistance;
        }

        private static bool IsCharacterCollisionMoveCandidate(
            MonsterUnit monsterUnit,
            int projectedGridIndex,
            int targetGridIndex,
            BattleGridEffectController gridEffectController)
        {
            if (monsterUnit == null || projectedGridIndex < 0 || targetGridIndex < 0)
                return false;

            if (projectedGridIndex != targetGridIndex)
                return false;

            if (gridEffectController != null && gridEffectController.IsBlocked(projectedGridIndex))
                return false;

            if (BattleOccupancyService.IsOccupiedByMonster(projectedGridIndex, monsterUnit))
                return false;

            if (!BattleOccupancyService.TryGetCharacterAtGrid(
                    projectedGridIndex,
                    out BattleCharacter character) ||
                character == null ||
                character.RuntimeData == null ||
                character.RuntimeData.IsDead)
            {
                return false;
            }

            return true;
        }

        private static bool IsRiskyGridEffectDestination(
            int gridIndex,
            BattleGridEffectController gridEffectController)
        {
            if (gridIndex < 0 || gridEffectController == null)
                return false;

            if (!gridEffectController.State.TryGetEffectId(gridIndex, out string gridEffectId) ||
                string.IsNullOrWhiteSpace(gridEffectId))
            {
                return false;
            }

            // 점액은 몬스터에게 적용되지 않으므로 블롭에게 위험 지형이 아닙니다.
            return !string.Equals(
                gridEffectId,
                ResidueGridEffectId,
                System.StringComparison.OrdinalIgnoreCase);
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

            // 블롭의 공격은 좌우 방향 공격이므로 같은 가로 라인에 있는 대상만 유효합니다.
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

        private static BattleDirection ResolveForwardAttackDirection(
            MonsterUnit monsterUnit,
            Vector2Int moveOffset,
            int originGridIndex,
            int targetGridIndex,
            GridManager gridManager)
        {
            // 가로 이동을 예약했다면 이동 실행 시 바라보게 되는 방향이 곧 정면입니다.
            if (moveOffset.x > 0)
                return BattleDirection.Right;

            if (moveOffset.x < 0)
                return BattleDirection.Left;

            // 세로 이동이거나 이동하지 못한 경우, 예약 시점에 고정한 타겟이 좌우 어느 쪽에 있는지로 정면을 잡습니다.
            if (gridManager != null && originGridIndex >= 0 && targetGridIndex >= 0)
            {
                Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
                Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

                if (targetCoord.x > originCoord.x)
                    return BattleDirection.Right;

                if (targetCoord.x < originCoord.x)
                    return BattleDirection.Left;
            }

            BattleUnitFacing facing = monsterUnit != null
                ? monsterUnit.GetComponent<BattleUnitFacing>()
                : null;

            return facing == null || facing.IsFacingRight
                ? BattleDirection.Right
                : BattleDirection.Left;
        }
    }
}
