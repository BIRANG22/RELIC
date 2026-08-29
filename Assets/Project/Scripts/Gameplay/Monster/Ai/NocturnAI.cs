using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 녹턴 전용 AI입니다.
    /// 행동 범위 밖에서는 가장 가까운 캐릭터를 향해 1칸 이동하고,
    /// 행동 범위 안에서는 후방 포탈 이동 또는 끌어당기기 후 공격을 연계합니다.
    /// </summary>
    public class NocturnAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_15";
        private const string PortalSkillId = "S_Monster_16";
        private const string ThrustSkillId = "S_Monster_17";
        private const string SlashSkillId = "S_Monster_18";
        private const string PullSkillId = "S_Monster_19";

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return ThrustSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            if (monsterUnit == null ||
                monsterUnit.RuntimeData == null ||
                gridManager == null ||
                monsterUnit.MainGridIndex < 0)
            {
                return plan;
            }

            List<BattleCharacter> playersInActionRange =
                FindPlayersInActionRange(monsterUnit, gridManager);
            List<BattleCharacter> playersInPortalRange =
                FindPlayersInSkillRange(PortalSkillId, monsterUnit.MainGridIndex, gridManager);

            bool canUsePull = TryFindSafePullTarget(
                monsterUnit,
                monsterUnit.MainGridIndex,
                gridManager,
                out BattleCharacter pullTarget,
                out BattleDirection pullDirection);

            // 당기기와 후방 재배치는 녹턴의 핵심 연계 행동입니다.
            // 후방이 1칸이면 일반 이동, 그보다 멀면 그림자걸음을 사용합니다.
            bool hasRearPortalTarget = TryFindPortalTarget(
                monsterUnit,
                playersInPortalRange,
                gridManager,
                out _,
                out Vector2Int preferredRearPortalOffset,
                out int preferredRearPortalGridIndex,
                out BattleDirection preferredRearAttackDirection,
                out bool preferredRearUseRegularMove);

            bool usePullFirst = canUsePull &&
                                (!hasRearPortalTarget || BattleRandom.Range(0, 2) == 0);

            if (usePullFirst)
            {
                AddPullCombo(plan, pullDirection, 0, 0);

                int laterSlotOffset = BattleRandom.Range(1, 3);

                // 당기기 후의 예상 위치와 예상 방향을 기준으로 후방 재배치를 계산합니다.
                if (TryFindPortalTargetAfterPull(
                        monsterUnit,
                        pullTarget,
                        pullDirection,
                        gridManager,
                        out Vector2Int pulledPortalOffset,
                        out int pulledPortalGridIndex,
                        out BattleDirection pulledPortalAttackDirection,
                        out bool pulledUseRegularMove))
                {
                    AddRearRepositionCombo(
                        plan,
                        pulledPortalOffset,
                        pulledPortalGridIndex,
                        pulledPortalAttackDirection,
                        pulledUseRegularMove,
                        laterSlotOffset,
                        2,
                        true);
                    return plan;
                }

                // 당긴 대상과 연계할 수 없어도 다른 대상의 후방이 열려 있으면 재배치 후 공격을 예약합니다.
                if (TryFindPortalTarget(
                        monsterUnit,
                        playersInPortalRange,
                        gridManager,
                        out _,
                        out Vector2Int laterPortalOffset,
                        out int laterPortalGridIndex,
                        out BattleDirection laterPortalAttackDirection,
                        out bool laterUseRegularMove))
                {
                    AddRearRepositionCombo(
                        plan,
                        laterPortalOffset,
                        laterPortalGridIndex,
                        laterPortalAttackDirection,
                        laterUseRegularMove,
                        laterSlotOffset,
                        2);
                }
                else
                {
                    AddAdditionalAttack(
                        plan,
                        monsterUnit.MainGridIndex,
                        gridManager,
                        21,
                        laterSlotOffset,
                        pullDirection);
                }

                return plan;
            }

            if (hasRearPortalTarget)
            {
                AddRearRepositionCombo(
                    plan,
                    preferredRearPortalOffset,
                    preferredRearPortalGridIndex,
                    preferredRearAttackDirection,
                    preferredRearUseRegularMove,
                    0,
                    0);

                AddAdditionalAttack(
                    plan,
                    preferredRearPortalGridIndex,
                    gridManager,
                    12,
                    BattleRandom.Range(1, 3),
                    preferredRearAttackDirection);

                return plan;
            }

            // 그림자 소용돌이와 그림자걸음을 모두 사용할 수 없을 때는 가장 가까운 캐릭터에게 접근합니다.
            if (BattleRandom.Range(0, 2) == 0 &&
                TryAddRegularMoveCombo(plan, monsterUnit, gridManager))
            {
                return plan;
            }

            // 그랩 범위에도 없고 행동 범위에도 대상이 없으면 가장 가까운 캐릭터에게 1칸 이동합니다.
            if (playersInActionRange.Count <= 0)
            {
                Vector2Int moveOffset = GetOneTileMoveTowardNearestPlayer(
                    monsterUnit,
                    gridManager);

                if (moveOffset != Vector2Int.zero)
                {
                    const int moveGroup = 5;

                    plan.Add(new MonsterAIAction(
                        MoveSkillId,
                        moveOffset,
                        MonsterAISlotPreference.Front,
                        moveGroup,
                        0));

                    int destinationGridIndex = GetDestinationGridIndex(
                        monsterUnit,
                        moveOffset,
                        gridManager);

                    // 이동 후에는 새 위치에서 그랩 가능 여부를 먼저 다시 판단합니다.
                    if (TryFindSafePullTarget(
                            monsterUnit,
                            destinationGridIndex,
                            gridManager,
                            out _,
                            out BattleDirection movedPullDirection))
                    {
                        plan.Add(new MonsterAIAction(
                            PullSkillId,
                            Vector2Int.zero,
                            MonsterAISlotPreference.SameSlot,
                            moveGroup,
                            1,
                            destinationGridIndex,
                            true,
                            movedPullDirection));

                        if (TryPickAttackFromOrigin(
                                destinationGridIndex,
                                gridManager,
                                out string movedAttackSkill,
                                out BattleDirection movedAttackDirection))
                        {
                            plan.Add(new MonsterAIAction(
                                movedAttackSkill,
                                Vector2Int.zero,
                                MonsterAISlotPreference.SameSlot,
                                moveGroup,
                                2,
                                destinationGridIndex,
                                true,
                                movedAttackDirection));
                        }
                    }
                    else if (TryPickAttackFromOrigin(
                            destinationGridIndex,
                            gridManager,
                            out string followUpSkillId,
                            out BattleDirection followUpDirection))
                    {
                        plan.Add(new MonsterAIAction(
                            followUpSkillId,
                            Vector2Int.zero,
                            MonsterAISlotPreference.SameSlot,
                            moveGroup,
                            1,
                            destinationGridIndex,
                            true,
                            followUpDirection));
                    }
                }

                return plan;
            }

            // 공격 가능한 상태에서는 일정 확률로 15/16 공격을 연속 등록합니다.
            // 두 공격 모두 실제 적중 인원을 다시 계산해 선택하므로 16번 광역 공격도 적극적으로 사용합니다.
            if (BattleRandom.Range(0, 3) == 0 &&
                TryPickAttackFromOrigin(
                    monsterUnit.MainGridIndex,
                    gridManager,
                    out string firstAttackSkill,
                    out BattleDirection firstAttackDirection))
            {
                plan.Add(new MonsterAIAction(
                    firstAttackSkill,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Front,
                    40,
                    0,
                    monsterUnit.MainGridIndex,
                    false,
                    firstAttackDirection));

                if (TryPickAttackFromOrigin(
                        monsterUnit.MainGridIndex,
                        gridManager,
                        out string secondAttackSkill,
                        out BattleDirection secondAttackDirection))
                {
                    plan.Add(new MonsterAIAction(
                        secondAttackSkill,
                        Vector2Int.zero,
                        MonsterAISlotPreference.Front,
                        41,
                        1,
                        monsterUnit.MainGridIndex,
                        false,
                        secondAttackDirection,
                        false,
                        BattleRandom.Range(1, 3)));
                }

                return plan;
            }

            // 그랩은 불가능하지만 대상의 후방이 비어 있다면 거리별로 일반 이동 또는 그림자걸음 후 공격합니다.
            if (TryFindPortalTarget(
                    monsterUnit,
                    playersInPortalRange,
                    gridManager,
                    out _,
                    out Vector2Int portalOffset,
                    out int portalGridIndex,
                    out BattleDirection portalAttackDirection,
                    out bool portalUseRegularMove))
            {
                AddRearRepositionCombo(
                    plan,
                    portalOffset,
                    portalGridIndex,
                    portalAttackDirection,
                    portalUseRegularMove,
                    0,
                    0);

                // 재배치 공격 뒤에는 다른 슬롯에서도 한 번 더 공격해 엘리트의 압박을 유지합니다.
                AddAdditionalAttack(
                    plan,
                    portalGridIndex,
                    gridManager,
                    12,
                    BattleRandom.Range(1, 3),
                    portalAttackDirection);

                return plan;
            }

            // 사용할 수 있는 연계가 없으면 가장 가까운 캐릭터에게 이동합니다.
            Vector2Int fallbackMoveOffset = GetOneTileMoveTowardNearestPlayer(
                monsterUnit,
                gridManager);

            if (fallbackMoveOffset != Vector2Int.zero)
            {
                const int fallbackMoveGroup = 30;

                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    fallbackMoveOffset,
                    MonsterAISlotPreference.Front,
                    fallbackMoveGroup,
                    0));

                int fallbackDestinationGridIndex = GetDestinationGridIndex(
                    monsterUnit,
                    fallbackMoveOffset,
                    gridManager);

                if (TryPickAttackFromOrigin(
                        fallbackDestinationGridIndex,
                        gridManager,
                        out string fallbackAttackSkillId,
                        out BattleDirection fallbackAttackDirection))
                {
                    plan.Add(new MonsterAIAction(
                        fallbackAttackSkillId,
                        Vector2Int.zero,
                        MonsterAISlotPreference.SameSlot,
                        fallbackMoveGroup,
                        1,
                        fallbackDestinationGridIndex,
                        true,
                        fallbackAttackDirection));

                    AddAdditionalAttack(
                        plan,
                        fallbackDestinationGridIndex,
                        gridManager,
                        31,
                        BattleRandom.Range(1, 4),
                        fallbackAttackDirection);
                }
            }

            return plan;
        }

        private bool TryAddRegularMoveCombo(
            MonsterAIPlan plan,
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            if (plan == null || monsterUnit == null || gridManager == null)
                return false;

            Vector2Int moveOffset = GetOneTileMoveTowardNearestPlayer(
                monsterUnit,
                gridManager);

            if (moveOffset == Vector2Int.zero)
                return false;

            int destinationGridIndex = GetDestinationGridIndex(
                monsterUnit,
                moveOffset,
                gridManager);

            if (destinationGridIndex < 0)
                return false;

            const int moveGroup = 60;

            plan.Add(new MonsterAIAction(
                MoveSkillId,
                moveOffset,
                MonsterAISlotPreference.Front,
                moveGroup,
                0));

            // 이동 방향을 공격 방향으로 재사용하지 않습니다.
            // 이동 완료 위치에서 실제로 맞힐 수 있는 대상을 다시 찾고, 그 대상이 있는 방향으로만 공격합니다.
            if (TryPickAttackFromOrigin(
                    destinationGridIndex,
                    gridManager,
                    out string movedAttackSkillId,
                    out BattleDirection movedAttackDirection))
            {
                plan.Add(new MonsterAIAction(
                    movedAttackSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.SameSlot,
                    moveGroup,
                    1,
                    destinationGridIndex,
                    true,
                    movedAttackDirection));

                AddAdditionalAttack(
                    plan,
                    destinationGridIndex,
                    gridManager,
                    moveGroup + 1,
                    BattleRandom.Range(1, 4),
                    movedAttackDirection);
            }

            return true;
        }


        private Vector2Int GetTacticalOneTileMove(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            if (monsterUnit == null || gridManager == null ||
                monsterUnit.MainGridIndex < 0)
            {
                return Vector2Int.zero;
            }

            Vector2Int[] directions =
            {
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.down
            };

            List<Vector2Int> attackEnabledMoves = new();
            List<Vector2Int> validMoves = new();

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int candidate = directions[i];

                if (!CanMonsterMove(monsterUnit, gridManager, candidate))
                    continue;

                validMoves.Add(candidate);

                int destinationGridIndex = GetDestinationGridIndex(
                    monsterUnit,
                    candidate,
                    gridManager);

                if (destinationGridIndex >= 0 &&
                    TryPickAttackFromOrigin(
                        destinationGridIndex,
                        gridManager,
                        out _,
                        out _))
                {
                    attackEnabledMoves.Add(candidate);
                }
            }

            List<Vector2Int> candidates = attackEnabledMoves.Count > 0
                ? attackEnabledMoves
                : validMoves;

            if (candidates.Count <= 0)
                return Vector2Int.zero;

            return candidates[BattleRandom.Range(0, candidates.Count)];
        }

        private void AddAdditionalAttack(
            MonsterAIPlan plan,
            int originGridIndex,
            GridManager gridManager,
            int group,
            int slotOffset,
            BattleDirection fallbackDirection,
            MonsterAISlotPreference slotPreference = MonsterAISlotPreference.Front,
            int priority = 0)
        {
            if (plan == null)
                return;

            bool foundTarget = TryPickAttackFromOrigin(
                originGridIndex,
                gridManager,
                out string attackSkillId,
                out BattleDirection attackDirection);

            // 다른 슬롯에 단독으로 예약되는 공격은 방향을 고정하지 않습니다.
            // 실행 전 피격 등으로 녹턴의 Facing이 바뀌면 실제 현재 방향으로 공격합니다.
            plan.Add(new MonsterAIAction(
                foundTarget ? attackSkillId : PickFollowUpAttack(),
                Vector2Int.zero,
                slotPreference,
                group,
                priority,
                originGridIndex,
                false,
                foundTarget ? attackDirection : fallbackDirection,
                false,
                slotOffset));
        }

        private BattleDirection GetDirectionTowardNearestPlayer(
            int originGridIndex,
            GridManager gridManager)
        {
            if (originGridIndex < 0 || gridManager == null)
                return BattleDirection.Right;

            BattleCharacter[] players = FindPlayers();
            Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
            BattleCharacter nearest = null;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                int distance = Mathf.Abs(playerCoord.x - originCoord.x) +
                               Mathf.Abs(playerCoord.y - originCoord.y);

                if (distance < nearestDistance)
                {
                    nearest = player;
                    nearestDistance = distance;
                }
            }

            if (nearest == null)
                return BattleDirection.Right;

            Vector2Int nearestCoord = gridManager.IndexToCoord(nearest.CurrentGridIndex);
            return nearestCoord.x < originCoord.x
                ? BattleDirection.Left
                : BattleDirection.Right;
        }

        private void AddRearRepositionCombo(
            MonsterAIPlan plan,
            Vector2Int moveOffset,
            int destinationGridIndex,
            BattleDirection attackDirection,
            bool useRegularMove,
            int slotOffset,
            int priorityBase,
            bool keepPredictedAttackDirection = false)
        {
            if (!useRegularMove)
            {
                AddPortalCombo(
                    plan,
                    moveOffset,
                    destinationGridIndex,
                    attackDirection,
                    slotOffset,
                    priorityBase,
                    keepPredictedAttackDirection);
                return;
            }

            if (plan == null)
                return;

            const int moveGroup = 11;

            plan.Add(new MonsterAIAction(
                MoveSkillId,
                moveOffset,
                MonsterAISlotPreference.Front,
                moveGroup,
                priorityBase,
                -1,
                false,
                BattleDirection.Right,
                false,
                slotOffset));

            string followUpSkill = PickFollowUpAttack();
            BattleDirection finalAttackDirection = attackDirection;

            if (!keepPredictedAttackDirection &&
                TryPickAttackFromOrigin(
                    destinationGridIndex,
                    Object.FindFirstObjectByType<GridManager>(),
                    out string selectedAttack,
                    out BattleDirection selectedDirection))
            {
                followUpSkill = selectedAttack;
                finalAttackDirection = selectedDirection;
            }

            plan.Add(new MonsterAIAction(
                followUpSkill,
                Vector2Int.zero,
                MonsterAISlotPreference.SameSlot,
                moveGroup,
                priorityBase + 1,
                destinationGridIndex,
                true,
                finalAttackDirection,
                false,
                slotOffset));
        }

        private static bool IsOneTileOrthogonalMove(Vector2Int moveOffset)
        {
            return Mathf.Abs(moveOffset.x) + Mathf.Abs(moveOffset.y) == 1;
        }

        private void AddPortalCombo(
            MonsterAIPlan plan,
            Vector2Int portalOffset,
            int portalGridIndex,
            BattleDirection attackDirection,
            int slotOffset,
            int priorityBase,
            bool keepPredictedAttackDirection = false)
        {
            if (plan == null)
                return;

            const int portalGroup = 10;

            plan.Add(new MonsterAIAction(
                PortalSkillId,
                portalOffset,
                MonsterAISlotPreference.Front,
                portalGroup,
                priorityBase,
                portalGridIndex,
                false,
                BattleDirection.Right,
                true,
                slotOffset));

            string portalAttackSkill = PickFollowUpAttack();
            BattleDirection finalPortalDirection = attackDirection;

            // 일반 포탈은 현재 배치에서 공격 가능한 대상을 다시 확인합니다.
            // 당기기 후 포탈은 아직 실행되지 않은 당기기의 예상 위치를 기준으로 계산했으므로,
            // 현재 배치를 다시 탐색하지 않고 미리 계산한 공격 방향을 그대로 유지합니다.
            if (!keepPredictedAttackDirection &&
                TryPickAttackFromOrigin(
                    portalGridIndex,
                    Object.FindFirstObjectByType<GridManager>(),
                    out string selectedPortalAttack,
                    out BattleDirection selectedPortalDirection))
            {
                portalAttackSkill = selectedPortalAttack;
                finalPortalDirection = selectedPortalDirection;
            }

            plan.Add(new MonsterAIAction(
                portalAttackSkill,
                Vector2Int.zero,
                MonsterAISlotPreference.SameSlot,
                portalGroup,
                priorityBase + 1,
                portalGridIndex,
                true,
                finalPortalDirection,
                false,
                slotOffset));
        }

        private void AddPullCombo(
            MonsterAIPlan plan,
            BattleDirection pullDirection,
            int slotOffset,
            int priorityBase)
        {
            if (plan == null)
                return;

            const int pullGroup = 20;

            plan.Add(new MonsterAIAction(
                PullSkillId,
                Vector2Int.zero,
                MonsterAISlotPreference.Front,
                pullGroup,
                priorityBase,
                -1,
                true,
                pullDirection,
                false,
                slotOffset));

            plan.Add(new MonsterAIAction(
                PickFollowUpAttack(),
                Vector2Int.zero,
                MonsterAISlotPreference.SameSlot,
                pullGroup,
                priorityBase + 1,
                -1,
                true,
                pullDirection,
                false,
                slotOffset));
        }

        private int GetDestinationGridIndex(
            MonsterUnit monsterUnit,
            Vector2Int moveOffset,
            GridManager gridManager)
        {
            if (monsterUnit == null ||
                monsterUnit.MainGridIndex < 0 ||
                gridManager == null)
            {
                return -1;
            }

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int destinationCoord = currentCoord + moveOffset;

            return gridManager.IsValidCoord(destinationCoord)
                ? gridManager.CoordToIndex(destinationCoord)
                : -1;
        }

        private bool TryPickAttackFromOrigin(
            int originGridIndex,
            GridManager gridManager,
            out string selectedSkillId,
            out BattleDirection selectedDirection)
        {
            selectedSkillId = string.Empty;
            selectedDirection = BattleDirection.Right;

            if (originGridIndex < 0 || gridManager == null)
                return false;

            int thrustBestHits = GetBestHitCount(
                ThrustSkillId,
                originGridIndex,
                gridManager,
                out BattleDirection thrustDirection);

            int slashBestHits = GetBestHitCount(
                SlashSkillId,
                originGridIndex,
                gridManager,
                out BattleDirection slashDirection);

            if (thrustBestHits <= 0 && slashBestHits <= 0)
                return false;

            // 16번은 위아래까지 맞힐 수 있으므로 적중 인원이 더 많으면 반드시 우선합니다.
            if (slashBestHits > thrustBestHits)
            {
                selectedSkillId = SlashSkillId;
                selectedDirection = slashDirection;
                return true;
            }

            if (thrustBestHits > slashBestHits)
            {
                selectedSkillId = ThrustSkillId;
                selectedDirection = thrustDirection;
                return true;
            }

            // 적중 인원이 같으면 15/16을 균등하게 선택합니다.
            bool useSlash = BattleRandom.Range(0, 2) == 0;
            selectedSkillId = useSlash ? SlashSkillId : ThrustSkillId;
            selectedDirection = useSlash ? slashDirection : thrustDirection;
            return true;
        }

        private int GetBestHitCount(
            string skillId,
            int originGridIndex,
            GridManager gridManager,
            out BattleDirection selectedDirection)
        {
            selectedDirection = BattleDirection.Right;

            MonsterSkillData skillData =
                DataManager.Instance?.MonsterSkillDatabase?.Get(skillId);
            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;

            if (skillData == null || rangeDatabase == null ||
                string.IsNullOrWhiteSpace(skillData.RangeId) || skillData.RangeId == "0")
            {
                return 0;
            }

            BattleCharacter[] players = FindPlayers();
            BattleDirection[] directions =
            {
                BattleDirection.Left,
                BattleDirection.Right
            };

            int bestHits = 0;

            for (int i = 0; i < directions.Length; i++)
            {
                List<int> rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
                    originGridIndex,
                    skillData.RangeId,
                    directions[i],
                    rangeDatabase,
                    gridManager);

                if (rangeIndices == null || rangeIndices.Count <= 0)
                    continue;

                HashSet<int> rangeSet = new(rangeIndices);
                int hitCount = 0;

                for (int j = 0; j < players.Length; j++)
                {
                    BattleCharacter player = players[j];

                    if (IsAlivePlayer(player) && player.CurrentGridIndex >= 0 &&
                        rangeSet.Contains(player.CurrentGridIndex))
                    {
                        hitCount++;
                    }
                }

                if (hitCount > bestHits)
                {
                    bestHits = hitCount;
                    selectedDirection = directions[i];
                }
            }

            return bestHits;
        }

        private bool TryFindHittableDirection(
            string skillId,
            int originGridIndex,
            GridManager gridManager,
            out BattleDirection selectedDirection)
        {
            return TryFindHittableTarget(
                skillId,
                originGridIndex,
                gridManager,
                out _,
                out selectedDirection);
        }

        private bool TryFindHittableTarget(
            string skillId,
            int originGridIndex,
            GridManager gridManager,
            out BattleCharacter selectedTarget,
            out BattleDirection selectedDirection)
        {
            selectedTarget = null;
            selectedDirection = BattleDirection.Right;

            MonsterSkillData skillData =
                DataManager.Instance?.MonsterSkillDatabase?.Get(skillId);
            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;

            if (skillData == null ||
                rangeDatabase == null ||
                string.IsNullOrWhiteSpace(skillData.RangeId) ||
                skillData.RangeId == "0")
            {
                return false;
            }

            BattleCharacter[] players = FindPlayers();
            BattleDirection[] directions =
            {
                BattleDirection.Left,
                BattleDirection.Right
            };

            for (int directionIndex = 0; directionIndex < directions.Length; directionIndex++)
            {
                BattleDirection direction = directions[directionIndex];
                List<int> rangeIndices =
                    BattleRangeCalculator.GetDirectionRangeIndices(
                        originGridIndex,
                        skillData.RangeId,
                        direction,
                        rangeDatabase,
                        gridManager);

                if (rangeIndices == null || rangeIndices.Count <= 0)
                    continue;

                HashSet<int> rangeSet = new(rangeIndices);

                for (int playerIndex = 0; playerIndex < players.Length; playerIndex++)
                {
                    BattleCharacter player = players[playerIndex];

                    if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                        continue;

                    if (!rangeSet.Contains(player.CurrentGridIndex))
                        continue;

                    selectedTarget = player;
                    selectedDirection = direction;
                    return true;
                }
            }

            return false;
        }

        private bool TryFindSafePullTarget(
            MonsterUnit monsterUnit,
            int originGridIndex,
            GridManager gridManager,
            out BattleCharacter selectedTarget,
            out BattleDirection selectedDirection)
        {
            selectedTarget = null;
            selectedDirection = BattleDirection.Right;

            if (monsterUnit == null || originGridIndex < 0 || gridManager == null)
                return false;

            MonsterSkillData skillData =
                DataManager.Instance?.MonsterSkillDatabase?.Get(PullSkillId);
            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;

            if (skillData == null || rangeDatabase == null ||
                string.IsNullOrWhiteSpace(skillData.RangeId) || skillData.RangeId == "0")
            {
                return false;
            }

            BattleCharacter[] players = FindPlayers();
            BattleDirection[] directions =
            {
                BattleDirection.Left,
                BattleDirection.Right
            };

            for (int directionIndex = 0; directionIndex < directions.Length; directionIndex++)
            {
                BattleDirection direction = directions[directionIndex];
                List<int> rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
                    originGridIndex,
                    skillData.RangeId,
                    direction,
                    rangeDatabase,
                    gridManager);

                if (rangeIndices == null || rangeIndices.Count <= 0)
                    continue;

                HashSet<int> rangeSet = new(rangeIndices);
                BattleCharacter firstTarget = null;
                bool wouldCollideWithNocturn = false;

                for (int playerIndex = 0; playerIndex < players.Length; playerIndex++)
                {
                    BattleCharacter player = players[playerIndex];

                    if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0 ||
                        !rangeSet.Contains(player.CurrentGridIndex))
                    {
                        continue;
                    }

                    firstTarget ??= player;

                    if (WouldPullTargetIntoNocturn(
                            monsterUnit,
                            originGridIndex,
                            player.CurrentGridIndex,
                            direction,
                            gridManager))
                    {
                        wouldCollideWithNocturn = true;
                        break;
                    }
                }

                if (firstTarget != null && !wouldCollideWithNocturn)
                {
                    selectedTarget = firstTarget;
                    selectedDirection = direction;
                    return true;
                }
            }

            return false;
        }

        private bool WouldPullTargetIntoNocturn(
            MonsterUnit monsterUnit,
            int predictedMonsterMainGridIndex,
            int targetGridIndex,
            BattleDirection pullDirection,
            GridManager gridManager)
        {
            if (monsterUnit == null || predictedMonsterMainGridIndex < 0 ||
                targetGridIndex < 0 || gridManager == null)
            {
                return false;
            }

            Vector2Int pullOffset = pullDirection == BattleDirection.Left
                ? Vector2Int.right
                : Vector2Int.left;
            Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);
            Vector2Int pulledCoord = targetCoord + pullOffset;

            if (!gridManager.IsValidCoord(pulledCoord))
                return false;

            int pulledGridIndex = gridManager.CoordToIndex(pulledCoord);
            Vector2Int currentMainCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int predictedMainCoord = gridManager.IndexToCoord(predictedMonsterMainGridIndex);
            Vector2Int predictedOffset = predictedMainCoord - currentMainCoord;

            if (monsterUnit.OccupiedGridIndices == null || monsterUnit.OccupiedGridIndices.Count <= 0)
                return pulledGridIndex == predictedMonsterMainGridIndex;

            for (int i = 0; i < monsterUnit.OccupiedGridIndices.Count; i++)
            {
                int occupiedGridIndex = monsterUnit.OccupiedGridIndices[i];

                if (occupiedGridIndex < 0)
                    continue;

                Vector2Int occupiedCoord = gridManager.IndexToCoord(occupiedGridIndex) + predictedOffset;

                if (gridManager.IsValidCoord(occupiedCoord) &&
                    gridManager.CoordToIndex(occupiedCoord) == pulledGridIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private List<BattleCharacter> FindPlayersInSkillRange(
            string skillId,
            int originGridIndex,
            GridManager gridManager)
        {
            List<BattleCharacter> result = new();

            if (string.IsNullOrWhiteSpace(skillId) || originGridIndex < 0 || gridManager == null)
                return result;

            MonsterSkillData skillData =
                DataManager.Instance?.MonsterSkillDatabase?.Get(skillId);
            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;

            if (skillData == null || rangeDatabase == null ||
                string.IsNullOrWhiteSpace(skillData.RangeId) || skillData.RangeId == "0")
            {
                return result;
            }

            List<int> rangeIndices = BattleRangeCalculator.GetSelectionRangeIndices(
                originGridIndex,
                skillData.RangeId,
                rangeDatabase,
                gridManager);

            if (rangeIndices == null || rangeIndices.Count <= 0)
                return result;

            HashSet<int> rangeSet = new(rangeIndices);
            BattleCharacter[] players = FindPlayers();

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                if (rangeSet.Contains(player.CurrentGridIndex))
                    result.Add(player);
            }

            return result;
        }

        private string PickFollowUpAttack()
        {
            return BattleRandom.Range(0, 2) == 0
                ? ThrustSkillId
                : SlashSkillId;
        }

        private List<BattleCharacter> FindPlayersInActionRange(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            List<BattleCharacter> result = new();
            string actionRangeId = monsterUnit.RuntimeData.AttackRangeId;

            if (string.IsNullOrWhiteSpace(actionRangeId) || actionRangeId.Trim() == "0")
                return result;

            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;

            if (rangeDatabase == null)
                return result;

            List<int> actionRange = BattleRangeCalculator.GetSelectionRangeIndices(
                monsterUnit.MainGridIndex,
                actionRangeId,
                rangeDatabase,
                gridManager);

            if (actionRange == null || actionRange.Count <= 0)
                return result;

            HashSet<int> actionRangeSet = new(actionRange);
            BattleCharacter[] players = FindPlayers();

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                if (actionRangeSet.Contains(player.CurrentGridIndex))
                    result.Add(player);
            }

            return result;
        }

        private Vector2Int GetOneTileMoveTowardNearestPlayer(
            MonsterUnit monsterUnit,
            GridManager gridManager)
        {
            BattleCharacter nearest = FindNearestPlayer(monsterUnit, gridManager);

            if (nearest == null || monsterUnit.MainGridIndex < 0 || nearest.CurrentGridIndex < 0)
                return Vector2Int.zero;

            Vector2Int startCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(nearest.CurrentGridIndex);

            Vector2Int[] directions =
            {
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.down
            };

            // 바로 가까워지는 칸이 막혀 있어도 멈추지 않도록,
            // 현재 점유 상태를 기준으로 장애물을 피해 갈 수 있는 경로를 탐색합니다.
            Queue<Vector2Int> open = new();
            Dictionary<Vector2Int, Vector2Int> firstSteps = new();
            Dictionary<Vector2Int, int> pathLengths = new();

            open.Enqueue(Vector2Int.zero);
            firstSteps[Vector2Int.zero] = Vector2Int.zero;
            pathLengths[Vector2Int.zero] = 0;

            Vector2Int bestOffset = Vector2Int.zero;
            int bestDistance = int.MaxValue;
            int bestPathLength = int.MaxValue;
            List<Vector2Int> bestFirstSteps = new();

            while (open.Count > 0)
            {
                Vector2Int offset = open.Dequeue();
                int pathLength = pathLengths[offset];

                if (offset != Vector2Int.zero)
                {
                    Vector2Int coord = startCoord + offset;
                    int distance = Mathf.Abs(targetCoord.x - coord.x) +
                                   Mathf.Abs(targetCoord.y - coord.y);

                    if (distance < bestDistance ||
                        (distance == bestDistance && pathLength < bestPathLength))
                    {
                        bestDistance = distance;
                        bestPathLength = pathLength;
                        bestOffset = offset;
                        bestFirstSteps.Clear();
                        bestFirstSteps.Add(firstSteps[offset]);
                    }
                    else if (distance == bestDistance && pathLength == bestPathLength)
                    {
                        Vector2Int firstStep = firstSteps[offset];
                        if (!bestFirstSteps.Contains(firstStep))
                            bestFirstSteps.Add(firstStep);
                    }
                }

                for (int i = 0; i < directions.Length; i++)
                {
                    Vector2Int nextOffset = offset + directions[i];

                    if (pathLengths.ContainsKey(nextOffset))
                        continue;

                    if (!CanOccupyTranslatedPosition(monsterUnit, gridManager, nextOffset))
                        continue;

                    pathLengths[nextOffset] = pathLength + 1;
                    firstSteps[nextOffset] = offset == Vector2Int.zero
                        ? directions[i]
                        : firstSteps[offset];
                    open.Enqueue(nextOffset);
                }
            }

            if (bestOffset == Vector2Int.zero || bestFirstSteps.Count <= 0)
                return Vector2Int.zero;

            return bestFirstSteps[BattleRandom.Range(0, bestFirstSteps.Count)];
        }

        private bool CanOccupyTranslatedPosition(
            MonsterUnit monsterUnit,
            GridManager gridManager,
            Vector2Int totalOffset)
        {
            if (monsterUnit == null || gridManager == null)
                return false;

            BattleGridEffectController gridEffectController =
                Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

            for (int i = 0; i < monsterUnit.OccupiedGridIndices.Count; i++)
            {
                Vector2Int originalCoord = gridManager.IndexToCoord(monsterUnit.OccupiedGridIndices[i]);
                Vector2Int translatedCoord = originalCoord + totalOffset;

                if (!gridManager.IsValidCoord(translatedCoord))
                    return false;

                int translatedIndex = gridManager.CoordToIndex(translatedCoord);

                if (BattleOccupancyService.IsOccupiedByAnyUnit(translatedIndex, null, monsterUnit))
                    return false;

                if (gridEffectController != null && gridEffectController.IsBlocked(translatedIndex))
                    return false;
            }

            return true;
        }

        private bool TryFindPortalTargetAfterPull(
            MonsterUnit monsterUnit,
            BattleCharacter pullTarget,
            BattleDirection pullDirection,
            GridManager gridManager,
            out Vector2Int moveOffset,
            out int destinationGridIndex,
            out BattleDirection attackDirection,
            out bool useRegularMove)
        {
            moveOffset = Vector2Int.zero;
            destinationGridIndex = -1;
            attackDirection = BattleDirection.Right;
            useRegularMove = false;

            if (monsterUnit == null || pullTarget == null ||
                pullTarget.CurrentGridIndex < 0 || gridManager == null)
            {
                return false;
            }

            Vector2Int pullMoveOffset = pullDirection == BattleDirection.Left
                ? Vector2Int.right
                : Vector2Int.left;
            Vector2Int currentPulledTargetCoord =
                gridManager.IndexToCoord(pullTarget.CurrentGridIndex);
            Vector2Int predictedPulledTargetCoord = currentPulledTargetCoord + pullMoveOffset;

            if (!gridManager.IsValidCoord(predictedPulledTargetCoord))
                return false;

            int predictedPulledTargetGridIndex = gridManager.CoordToIndex(predictedPulledTargetCoord);

            if (BattleOccupancyService.IsOccupiedByAnyUnit(
                    predictedPulledTargetGridIndex,
                    pullTarget.CharacterId,
                    monsterUnit))
            {
                return false;
            }

            MonsterSkillData portalSkillData =
                DataManager.Instance?.MonsterSkillDatabase?.Get(PortalSkillId);
            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;

            if (portalSkillData == null || rangeDatabase == null ||
                string.IsNullOrWhiteSpace(portalSkillData.RangeId) || portalSkillData.RangeId == "0")
            {
                return false;
            }

            List<int> portalRange = BattleRangeCalculator.GetSelectionRangeIndices(
                monsterUnit.MainGridIndex,
                portalSkillData.RangeId,
                rangeDatabase,
                gridManager);

            if (portalRange == null || portalRange.Count <= 0)
                return false;

            HashSet<int> portalRangeSet = new(portalRange);
            BattleCharacter[] players = FindPlayers();
            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            int farthestDistance = int.MinValue;
            List<Vector2Int> farthestBehindCoords = new();
            List<int> farthestBehindGridIndices = new();
            List<BattleDirection> farthestAttackDirections = new();

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter candidate = players[i];

                if (!IsAlivePlayer(candidate) || candidate.CurrentGridIndex < 0)
                    continue;

                Vector2Int candidateCoord = candidate == pullTarget
                    ? predictedPulledTargetCoord
                    : gridManager.IndexToCoord(candidate.CurrentGridIndex);
                int candidateGridIndex = gridManager.CoordToIndex(candidateCoord);

                if (!portalRangeSet.Contains(candidateGridIndex))
                    continue;

                bool hasBehindGrid;
                Vector2Int behindCoord;
                BattleDirection candidateAttackDirection;

                if (candidate == pullTarget)
                {
                    // 그림자 소용돌이는 E_Strike가 먼저 적중한 뒤 E_Grab이 실행됩니다.
                    // 따라서 당겨진 대상은 현재 방향이 아니라, 피격 후 녹턴을 바라보는 방향으로 바뀐 상태입니다.
                    bool predictedFacesRight = pullDirection == BattleDirection.Left;
                    hasBehindGrid = TryGetBehindGridAtCoordByFacing(
                        candidateCoord,
                        predictedFacesRight,
                        gridManager,
                        out behindCoord,
                        out candidateAttackDirection);
                }
                else
                {
                    hasBehindGrid = TryGetBehindGridAtCoord(
                        candidate,
                        candidateCoord,
                        gridManager,
                        out behindCoord,
                        out candidateAttackDirection);
                }

                if (!hasBehindGrid)
                    continue;

                int behindGridIndex = gridManager.CoordToIndex(behindCoord);

                if (!IsPortalDestinationAvailableAfterPull(
                        monsterUnit,
                        pullTarget,
                        predictedPulledTargetGridIndex,
                        behindGridIndex))
                {
                    continue;
                }

                int distance = Mathf.Abs(candidateCoord.x - monsterCoord.x) +
                               Mathf.Abs(candidateCoord.y - monsterCoord.y);

                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthestBehindCoords.Clear();
                    farthestBehindGridIndices.Clear();
                    farthestAttackDirections.Clear();
                }

                if (distance == farthestDistance)
                {
                    farthestBehindCoords.Add(behindCoord);
                    farthestBehindGridIndices.Add(behindGridIndex);
                    farthestAttackDirections.Add(candidateAttackDirection);
                }
            }

            if (farthestBehindCoords.Count <= 0)
                return false;

            int selectedIndex = BattleRandom.Range(0, farthestBehindCoords.Count);
            Vector2Int selectedBehindCoord = farthestBehindCoords[selectedIndex];
            moveOffset = selectedBehindCoord - monsterCoord;
            destinationGridIndex = farthestBehindGridIndices[selectedIndex];
            attackDirection = farthestAttackDirections[selectedIndex];

            // 당기기 이후 예상 배치에서 후방이 바로 1칸이라면 포탈을 낭비하지 않고 일반 이동을 사용합니다.
            useRegularMove = IsOneTileOrthogonalMove(moveOffset) &&
                             (destinationGridIndex == pullTarget.CurrentGridIndex ||
                              CanMonsterMove(monsterUnit, gridManager, moveOffset));
            return true;
        }

        private bool IsPortalDestinationAvailableAfterPull(
            MonsterUnit monsterUnit,
            BattleCharacter pullTarget,
            int predictedPulledTargetGridIndex,
            int destinationGridIndex)
        {
            if (monsterUnit == null || pullTarget == null || destinationGridIndex < 0)
                return false;

            if (destinationGridIndex == monsterUnit.MainGridIndex ||
                destinationGridIndex == predictedPulledTargetGridIndex ||
                IsGridEffectBlocked(destinationGridIndex))
            {
                return false;
            }

            // 당겨질 대상의 현재 칸은 그림자 소용돌이가 먼저 실행되면 비게 되므로 예약할 수 있습니다.
            if (destinationGridIndex == pullTarget.CurrentGridIndex)
                return true;

            return !BattleOccupancyService.IsOccupiedByAnyUnit(
                destinationGridIndex,
                null,
                monsterUnit);
        }

        private bool TryFindPortalTarget(
            MonsterUnit monsterUnit,
            List<BattleCharacter> candidates,
            GridManager gridManager,
            out BattleCharacter selectedTarget,
            out Vector2Int moveOffset,
            out int destinationGridIndex,
            out BattleDirection attackDirection,
            out bool useRegularMove)
        {
            selectedTarget = null;
            moveOffset = Vector2Int.zero;
            destinationGridIndex = -1;
            attackDirection = BattleDirection.Right;
            useRegularMove = false;

            if (monsterUnit == null || candidates == null || gridManager == null)
                return false;

            Vector2Int monsterCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            List<BattleCharacter> validTargets = new();
            List<Vector2Int> validBehindCoords = new();
            List<int> validBehindGridIndices = new();
            List<BattleDirection> validAttackDirections = new();

            for (int i = 0; i < candidates.Count; i++)
            {
                BattleCharacter candidate = candidates[i];

                if (!TryGetBehindGrid(
                        candidate,
                        gridManager,
                        out Vector2Int behindCoord,
                        out BattleDirection direction))
                {
                    continue;
                }

                int behindGridIndex = gridManager.CoordToIndex(behindCoord);

                if (!IsPortalDestinationAvailable(monsterUnit, behindGridIndex))
                    continue;

                validTargets.Add(candidate);
                validBehindCoords.Add(behindCoord);
                validBehindGridIndices.Add(behindGridIndex);
                validAttackDirections.Add(direction);
            }

            if (validTargets.Count <= 0)
                return false;

            // 그림자걸음은 범위 안에서 녹턴으로부터 가장 먼 대상의 후방을 우선합니다.
            // 같은 최장 거리에 여러 대상이 있으면 그 대상들 사이에서만 무작위 선택합니다.
            int farthestDistance = int.MinValue;
            List<int> farthestIndices = new();

            for (int i = 0; i < validTargets.Count; i++)
            {
                Vector2Int targetCoord = gridManager.IndexToCoord(validTargets[i].CurrentGridIndex);
                int distance = Mathf.Abs(targetCoord.x - monsterCoord.x) +
                               Mathf.Abs(targetCoord.y - monsterCoord.y);

                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthestIndices.Clear();
                    farthestIndices.Add(i);
                }
                else if (distance == farthestDistance)
                {
                    farthestIndices.Add(i);
                }
            }

            int selectedIndex = farthestIndices[BattleRandom.Range(0, farthestIndices.Count)];
            selectedTarget = validTargets[selectedIndex];
            moveOffset = validBehindCoords[selectedIndex] - monsterCoord;
            destinationGridIndex = validBehindGridIndices[selectedIndex];
            attackDirection = validAttackDirections[selectedIndex];

            // 가장 먼 대상의 후방이 일반 이동 1회로 도달 가능한 칸이면 그림자걸음 대신 일반 이동을 사용합니다.
            useRegularMove = IsOneTileOrthogonalMove(moveOffset) &&
                             CanMonsterMove(monsterUnit, gridManager, moveOffset);
            return true;
        }

        private bool TryGetBehindGrid(
            BattleCharacter target,
            GridManager gridManager,
            out Vector2Int behindCoord,
            out BattleDirection attackDirection)
        {
            behindCoord = Vector2Int.zero;
            attackDirection = BattleDirection.Right;

            if (target == null || target.CurrentGridIndex < 0 || gridManager == null)
                return false;

            Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex);

            return TryGetBehindGridAtCoord(
                target,
                targetCoord,
                gridManager,
                out behindCoord,
                out attackDirection);
        }

        private bool TryGetBehindGridAtCoord(
            BattleCharacter target,
            Vector2Int targetCoord,
            GridManager gridManager,
            out Vector2Int behindCoord,
            out BattleDirection attackDirection)
        {
            behindCoord = Vector2Int.zero;
            attackDirection = BattleDirection.Right;

            if (target == null || gridManager == null)
                return false;

            BattleUnitFacing facing = ResolveTargetFacing(target);
            bool targetFacesRight = facing != null
                ? facing.IsFacingRight
                : target.RuntimeData == null ||
                  target.RuntimeData.Direction == BattleDirection.Right;

            return TryGetBehindGridAtCoordByFacing(
                targetCoord,
                targetFacesRight,
                gridManager,
                out behindCoord,
                out attackDirection);
        }

        private bool TryGetBehindGridAtCoordByFacing(
            Vector2Int targetCoord,
            bool targetFacesRight,
            GridManager gridManager,
            out Vector2Int behindCoord,
            out BattleDirection attackDirection)
        {
            behindCoord = targetCoord +
                (targetFacesRight ? Vector2Int.left : Vector2Int.right);
            attackDirection = targetFacesRight
                ? BattleDirection.Right
                : BattleDirection.Left;

            return gridManager != null && gridManager.IsValidCoord(behindCoord);
        }


        private static BattleUnitFacing ResolveTargetFacing(BattleCharacter target)
        {
            if (target == null)
                return null;

            // 프리팹 구조에 따라 방향 컴포넌트가 본체, 자식 또는 부모에 있을 수 있습니다.
            // 실제 화면 방향을 우선 사용해야 포탈 목적지가 캐릭터의 앞쪽으로 뒤집히지 않습니다.
            BattleUnitFacing facing = target.GetComponent<BattleUnitFacing>();

            if (facing == null)
                facing = target.GetComponentInChildren<BattleUnitFacing>(true);

            if (facing == null)
                facing = target.GetComponentInParent<BattleUnitFacing>();

            return facing;
        }

        private bool IsPortalDestinationAvailable(
            MonsterUnit monsterUnit,
            int destinationGridIndex)
        {
            if (monsterUnit == null || destinationGridIndex < 0)
                return false;

            // 이미 서 있는 칸은 포탈 목적지로 사용하지 않습니다.
            // 자기 자신은 점유 검사에서 제외되므로 이 검사가 없으면 제자리 포탈이 예약될 수 있습니다.
            if (destinationGridIndex == monsterUnit.MainGridIndex)
                return false;

            if (BattleOccupancyService.IsOccupiedByAnyUnit(
                    destinationGridIndex,
                    null,
                    monsterUnit))
            {
                return false;
            }

            return !IsGridEffectBlocked(destinationGridIndex);
        }

        private bool IsGridEffectBlocked(int gridIndex)
        {
            BattleGridEffectController gridEffectController =
                Object.FindFirstObjectByType<BattleGridEffectController>(
                    FindObjectsInactive.Include);

            return gridEffectController != null &&
                   gridEffectController.IsBlocked(gridIndex);
        }
    }
}
