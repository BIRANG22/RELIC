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

            bool canUsePull = TryFindHittableTarget(
                PullSkillId,
                monsterUnit.MainGridIndex,
                gridManager,
                out BattleCharacter pullTarget,
                out BattleDirection pullDirection);

            // 당기기와 포탈은 녹턴의 핵심 연계 행동입니다.
            // 둘 다 가능하면 동일한 확률로 하나를 선택하고, 하나만 가능하면 해당 행동을 우선합니다.
            bool hasRearPortalTarget = TryFindPortalTarget(
                monsterUnit,
                playersInActionRange,
                gridManager,
                out _,
                out Vector2Int preferredRearPortalOffset,
                out int preferredRearPortalGridIndex,
                out BattleDirection preferredRearAttackDirection);

            bool usePullFirst = canUsePull &&
                                (!hasRearPortalTarget || BattleRandom.Range(0, 2) == 0);

            if (usePullFirst)
            {
                AddPullCombo(plan, pullDirection, 0, 0);

                int laterSlotOffset = BattleRandom.Range(1, 3);

                // 당기기로 대상이 이동한 뒤 새 위치를 기준으로 후방 포탈을 다시 계산합니다.
                if (TryFindPortalTargetAfterPull(
                        monsterUnit,
                        pullTarget,
                        pullDirection,
                        gridManager,
                        out Vector2Int pulledPortalOffset,
                        out int pulledPortalGridIndex,
                        out BattleDirection pulledPortalAttackDirection))
                {
                    AddPortalCombo(
                        plan,
                        pulledPortalOffset,
                        pulledPortalGridIndex,
                        pulledPortalAttackDirection,
                        laterSlotOffset,
                        2,
                        true);
                    return plan;
                }

                // 당긴 대상과 연계할 수 없어도 다른 대상의 후방이 열려 있으면 포탈 공격을 예약합니다.
                if (TryFindPortalTarget(
                        monsterUnit,
                        playersInActionRange,
                        gridManager,
                        out _,
                        out Vector2Int laterPortalOffset,
                        out int laterPortalGridIndex,
                        out BattleDirection laterPortalAttackDirection))
                {
                    AddPortalCombo(
                        plan,
                        laterPortalOffset,
                        laterPortalGridIndex,
                        laterPortalAttackDirection,
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
                AddPortalCombo(
                    plan,
                    preferredRearPortalOffset,
                    preferredRearPortalGridIndex,
                    preferredRearAttackDirection,
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

            // 당기기와 포탈을 사용할 수 없을 때만 일반 이동을 전술적 재배치로 사용합니다.
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
                    if (TryFindHittableTarget(
                            PullSkillId,
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
                    true,
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
                        true,
                        secondAttackDirection,
                        false,
                        BattleRandom.Range(1, 3)));
                }

                return plan;
            }

            // 그랩은 불가능하지만 대상의 후방이 비어 있다면 포탈 이동 후 공격합니다.
            if (TryFindPortalTarget(
                    monsterUnit,
                    playersInActionRange,
                    gridManager,
                    out _,
                    out Vector2Int portalOffset,
                    out int portalGridIndex,
                    out BattleDirection portalAttackDirection))
            {
                AddPortalCombo(
                    plan,
                    portalOffset,
                    portalGridIndex,
                    portalAttackDirection,
                    0,
                    0);

                // 포탈 공격 뒤에는 다른 슬롯에서도 한 번 더 공격해 엘리트의 압박을 유지합니다.
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

            Vector2Int moveOffset = GetTacticalOneTileMove(
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

            plan.Add(new MonsterAIAction(
                foundTarget ? attackSkillId : PickFollowUpAttack(),
                Vector2Int.zero,
                slotPreference,
                group,
                priority,
                originGridIndex,
                true,
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

            if (nearest == null)
                return Vector2Int.zero;

            Vector2Int preferred = GetMoveTowardTarget(
                monsterUnit.MainGridIndex,
                nearest.CurrentGridIndex,
                gridManager,
                1);

            if (CanMonsterMove(monsterUnit, gridManager, preferred))
                return preferred;

            Vector2Int currentCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int targetCoord = gridManager.IndexToCoord(nearest.CurrentGridIndex);
            Vector2Int difference = targetCoord - currentCoord;

            Vector2Int alternate = preferred.x != 0
                ? new Vector2Int(0, difference.y > 0 ? 1 : -1)
                : new Vector2Int(difference.x > 0 ? 1 : -1, 0);

            return CanMonsterMove(monsterUnit, gridManager, alternate)
                ? alternate
                : Vector2Int.zero;
        }

        private bool TryFindPortalTargetAfterPull(
            MonsterUnit monsterUnit,
            BattleCharacter pullTarget,
            BattleDirection pullDirection,
            GridManager gridManager,
            out Vector2Int moveOffset,
            out int destinationGridIndex,
            out BattleDirection attackDirection)
        {
            moveOffset = Vector2Int.zero;
            destinationGridIndex = -1;
            attackDirection = BattleDirection.Right;

            if (monsterUnit == null || pullTarget == null ||
                pullTarget.CurrentGridIndex < 0 || gridManager == null)
            {
                return false;
            }

            Vector2Int pullMoveOffset = pullDirection == BattleDirection.Left
                ? Vector2Int.right
                : Vector2Int.left;

            Vector2Int currentTargetCoord =
                gridManager.IndexToCoord(pullTarget.CurrentGridIndex);
            Vector2Int pulledTargetCoord = currentTargetCoord + pullMoveOffset;

            if (!gridManager.IsValidCoord(pulledTargetCoord))
                return false;

            int pulledTargetGridIndex = gridManager.CoordToIndex(pulledTargetCoord);

            if (BattleOccupancyService.IsOccupiedByAnyUnit(
                    pulledTargetGridIndex,
                    pullTarget.CharacterId))
            {
                return false;
            }

            if (!TryGetBehindGridAtCoord(
                    pullTarget,
                    pulledTargetCoord,
                    gridManager,
                    out Vector2Int behindCoord,
                    out attackDirection))
            {
                return false;
            }

            destinationGridIndex = gridManager.CoordToIndex(behindCoord);

            if (destinationGridIndex == pulledTargetGridIndex)
                return false;

            bool targetWillVacateDestination =
                destinationGridIndex == pullTarget.CurrentGridIndex;

            if (!targetWillVacateDestination &&
                !IsPortalDestinationAvailable(monsterUnit, destinationGridIndex))
            {
                return false;
            }

            if (targetWillVacateDestination &&
                IsGridEffectBlocked(destinationGridIndex))
            {
                return false;
            }

            Vector2Int monsterCoord =
                gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            moveOffset = behindCoord - monsterCoord;
            return true;
        }

        private bool TryFindPortalTarget(
            MonsterUnit monsterUnit,
            List<BattleCharacter> candidates,
            GridManager gridManager,
            out BattleCharacter selectedTarget,
            out Vector2Int moveOffset,
            out int destinationGridIndex,
            out BattleDirection attackDirection)
        {
            selectedTarget = null;
            moveOffset = Vector2Int.zero;
            destinationGridIndex = -1;
            attackDirection = BattleDirection.Right;

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

            // 같은 라인이나 가장 가까운 대상만 반복하지 않고,
            // 후방이 열린 모든 캐릭터를 동일한 후보로 취급합니다.
            int selectedIndex = BattleRandom.Range(0, validTargets.Count);
            selectedTarget = validTargets[selectedIndex];
            moveOffset = validBehindCoords[selectedIndex] - monsterCoord;
            destinationGridIndex = validBehindGridIndices[selectedIndex];
            attackDirection = validAttackDirections[selectedIndex];
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

            // 대상이 오른쪽을 보면 왼쪽 칸이 뒤이며, 녹턴은 오른쪽을 바라보고 공격합니다.
            behindCoord = targetCoord +
                (targetFacesRight ? Vector2Int.left : Vector2Int.right);
            attackDirection = targetFacesRight
                ? BattleDirection.Right
                : BattleDirection.Left;

            return gridManager.IsValidCoord(behindCoord);
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
