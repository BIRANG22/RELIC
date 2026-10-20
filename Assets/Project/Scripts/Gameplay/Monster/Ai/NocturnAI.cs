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
        private const string MoveSkillId = "S_Monster_25";
        private const string PortalSkillId = "S_Monster_25";
        private const string ThrustSkillId = "S_Monster_26";
        private const string SlashSkillId = "S_Monster_27";
        private const string PullSkillId = "S_Monster_28";

        /// <summary>
        /// 녹턴의 두 번째 행동은 가까운 슬롯을 우선하되 3칸 뒤까지 선택합니다.
        /// 실제 사용 가능한 슬롯인지 여부는 예약 플래너가 최종 검증합니다.
        /// </summary>
        private static int PickForwardSlotOffset()
        {
            int roll = BattleRandom.Range(0, 100);
            if (roll < 45) return 1;
            if (roll < 80) return 2;
            return 3;
        }

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return ThrustSkillId;
        }

        /// <summary>
        /// 첫 턴: 25-26 / 28-27 고정.
        /// 이후 주변에 공격할 캐릭터가 있으면 일반 공격 2회 / 25-26,
        /// 주변에 없으면 28-27 / 25-26을 시도합니다.
        /// 모든 패턴은 서로 다른 두 슬롯에 행동 두 개씩 예약합니다.
        /// </summary>
        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            if (monsterUnit == null || monsterUnit.RuntimeData == null ||
                gridManager == null || monsterUnit.MainGridIndex < 0)
                return new MonsterAIPlan();

            bool firstTurn = IsFirstTurn(monsterUnit.RuntimeData);
            bool nearby = GetBestHitCount(SlashSkillId, monsterUnit.MainGridIndex,
                gridManager, out _) > 0 ||
                GetBestHitCount(ThrustSkillId, monsterUnit.MainGridIndex,
                gridManager, out _) > 0;

            // 일반 공격이 가능하더라도 끌어당긴 후 27의 예상 적중 수가
            // 현재 26/27 단일 공격의 최대 적중 수보다 많으면 당기기 연계를 우선합니다.
            // 첫 턴의 25-26 / 28-27 고정 규칙은 변경하지 않습니다.
            bool preferPull = !firstTurn && ShouldPreferPullAttack(monsterUnit, gridManager);
            int[] order = firstTurn ? new[] { 0 } :
                (!nearby || preferPull ? new[] { 2, 1, 0 } : new[] { 1, 2, 0 });
            foreach (int pattern in order)
            {
                MonsterAIPlan candidate = new();
                int nextOffset = PickForwardSlotOffset();
                bool built = pattern switch
                {
                    0 => TryBuildMoveAttackThenPullAttack(
                        candidate, monsterUnit, gridManager, nextOffset),
                    1 => TryBuildDoubleAttackThenMoveAttack(
                        candidate, monsterUnit, gridManager, nextOffset),
                    _ => TryBuildPullAttackThenMoveAttack(
                        candidate, monsterUnit, gridManager, nextOffset)
                };
                if (built && IsCompleteNocturnPlan(candidate))
                    return candidate;
            }

            // 모든 후보가 실패하면 예외 원인을 콘솔에서 확인할 수 있도록 남깁니다.
            UnityEngine.Debug.LogWarning(
                $"[NocturnAI] 행동 예약 후보 없음: Monster={monsterUnit.RuntimeData.MonsterId}, " +
                $"Turn={monsterUnit.RuntimeData.TurnCount}, Grid={monsterUnit.MainGridIndex}, " +
                $"FirstTurn={firstTurn}, Nearby={nearby}");
            return new MonsterAIPlan();
        }

        /// <summary>
        /// 당기기 후 27이 현재 위치에서의 일반 공격보다 더 많은 대상을
        /// 적중시킬 수 있으면 당기기 패턴을 우선합니다.
        /// 두 번째 슬롯의 이동 가능 여부는 실제 패턴 생성 단계에서 검증합니다.
        /// </summary>
        private bool ShouldPreferPullAttack(MonsterUnit monsterUnit, GridManager gridManager)
        {
            int origin = monsterUnit.MainGridIndex;
            if (!TryFindSafePullTarget(monsterUnit, origin, gridManager,
                    out _, out BattleDirection pullDirection))
                return false;

            int immediateHits = Mathf.Max(
                GetBestHitCount(ThrustSkillId, origin, gridManager, out _),
                GetBestHitCount(SlashSkillId, origin, gridManager, out _));
            Dictionary<int, int> pulledPositions = PredictPullPositions(
                monsterUnit, origin, gridManager, pullDirection);
            int pulledHits = CountPredictedHitsInDirection(
                SlashSkillId, origin, gridManager, pullDirection, pulledPositions);
            return pulledHits > immediateHits;
        }

        private bool TryBuildMoveAttackThenPullAttack(
            MonsterAIPlan plan, MonsterUnit monsterUnit,
            GridManager gridManager, int nextOffset)
        {
            // 가장 먼 대상 한 명만 고집하면 이동 후 끌어당기기 패턴이
            // 다른 유효한 대상이 있어도 누락될 수 있으므로 후보를 모두 확인한다.
            List<BattleCharacter> targets = FindPlayersInSkillRange(
                PortalSkillId, monsterUnit.MainGridIndex, gridManager);
            if (targets.Count == 0)
                return false;

            int start = BattleRandom.Range(0, targets.Count);
            for (int i = 0; i < targets.Count; i++)
            {
                List<BattleCharacter> oneTarget = new() { targets[(start + i) % targets.Count] };
                if (!TryFindPortalTarget(monsterUnit, oneTarget, gridManager,
                        out _, out Vector2Int offset, out int destination,
                        out BattleDirection attackDirection, out _))
                    continue;

                // 이동 전 점유 정보로 이동 후 당기기의 성공 여부를 확정하지 않습니다.
                // 당기기는 이후 슬롯이므로 캐릭터의 위치도 달라질 수 있습니다.
                BattleDirection pullDirection;
                if (!TryFindSafePullTarget(monsterUnit, destination, gridManager,
                        out _, out pullDirection))
                    pullDirection = GetDirectionTowardNearestPlayer(destination, gridManager);

                AddPortalCombo(plan, offset, destination, attackDirection, 0, 0);
                AddPullCombo(plan, monsterUnit, destination, gridManager,
                    pullDirection, nextOffset, 0);
                return true;
            }
            return false;
        }

        private bool TryBuildDoubleAttackThenMoveAttack(
            MonsterAIPlan plan, MonsterUnit monsterUnit,
            GridManager gridManager, int nextOffset)
        {
            // 일반 공격 연속 사용 조합: 27-27, 26-27, 27-26.
            string[][] combinations =
            {
                new[] { SlashSkillId, SlashSkillId },
                new[] { ThrustSkillId, SlashSkillId },
                new[] { SlashSkillId, ThrustSkillId }
            };
            int first = BattleRandom.Range(0, combinations.Length);
            for (int i = 0; i < combinations.Length; i++)
            {
                string[] attacks = combinations[(first + i) % combinations.Length];
                if (!TryFindHittableTarget(attacks[0], monsterUnit.MainGridIndex,
                        gridManager, out BattleCharacter attackedTarget,
                        out BattleDirection direction))
                    continue;
                if (CountHitsInDirection(attacks[1], monsterUnit.MainGridIndex,
                        gridManager, direction) <= 0)
                    continue;

                List<BattleCharacter> targets = FindPlayersInSkillRange(
                    PortalSkillId, monsterUnit.MainGridIndex, gridManager);
                targets.Remove(attackedTarget);
                if (!TryFindPortalTarget(monsterUnit, targets, gridManager,
                        out _, out Vector2Int offset, out int destination,
                        out BattleDirection attackDirection, out _))
                    continue;

                const int attackGroup = 90;
                plan.Add(new MonsterAIAction(attacks[0], Vector2Int.zero,
                    MonsterAISlotPreference.Front, attackGroup, 0,
                    monsterUnit.MainGridIndex, true, direction));
                plan.Add(new MonsterAIAction(attacks[1], Vector2Int.zero,
                    MonsterAISlotPreference.SameSlot, attackGroup, 1,
                    monsterUnit.MainGridIndex, true, direction));
                AddPortalCombo(plan, offset, destination, attackDirection, nextOffset, 0);
                return true;
            }
            return false;
        }

        private bool TryBuildPullAttackThenMoveAttack(
            MonsterAIPlan plan, MonsterUnit monsterUnit,
            GridManager gridManager, int nextOffset)
        {
            if (!TryFindSafePullTarget(monsterUnit, monsterUnit.MainGridIndex,
                    gridManager, out BattleCharacter target,
                    out BattleDirection direction))
                return false;

            if (!TryFindPortalTargetAfterPull(monsterUnit, target, direction,
                    gridManager, out Vector2Int offset, out int destination,
                    out BattleDirection attackDirection, out _, true))
            {
                // 끌어당긴 대상에게 이동할 수 없으면 다른 대상의 후방도 확인한다.
                if (!TryFindAlternatePortalAfterPull(monsterUnit, gridManager,
                        direction, out offset, out destination, out attackDirection))
                    return false;
            }

            AddPullCombo(plan, monsterUnit, monsterUnit.MainGridIndex,
                gridManager, direction, 0, 0);
            AddPortalCombo(plan, offset, destination, attackDirection, nextOffset, 0, true);
            return true;
        }

        private bool TryFindAlternatePortalAfterPull(
            MonsterUnit monsterUnit, GridManager gridManager,
            BattleDirection pullDirection, out Vector2Int moveOffset,
            out int destinationGridIndex, out BattleDirection attackDirection)
        {
            moveOffset = Vector2Int.zero;
            destinationGridIndex = -1;
            attackDirection = BattleDirection.Right;

            Dictionary<int, int> predicted = PredictPullPositions(
                monsterUnit, monsterUnit.MainGridIndex, gridManager, pullDirection);
            List<BattleCharacter> candidates = FindPlayersInSkillRange(
                PortalSkillId, monsterUnit.MainGridIndex, gridManager);
            Vector2Int origin = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            int start = candidates.Count > 0 ? BattleRandom.Range(0, candidates.Count) : 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                BattleCharacter candidate = candidates[(start + i) % candidates.Count];
                if (predicted.ContainsKey(candidate.CurrentGridIndex))
                    continue;
                if (!TryGetBehindGrid(candidate, gridManager,
                        out Vector2Int behind, out BattleDirection direction))
                    continue;
                int index = gridManager.CoordToIndex(behind);
                if (!IsPortalDestinationAvailableAfterPull(
                        monsterUnit, candidate, predicted, index))
                    continue;
                moveOffset = behind - origin;
                destinationGridIndex = index;
                attackDirection = direction;
                return true;
            }
            return false;
        }

        private static bool IsCompleteNocturnPlan(MonsterAIPlan plan)
        {
            if (plan == null || plan.Actions.Count != 4)
                return false;

            int firstOffset = plan.Actions[0].SlotOffset;
            int secondOffset = -1;
            int firstCount = 0;
            int secondCount = 0;
            int moveCount = 0;
            foreach (MonsterAIAction action in plan.Actions)
            {
                if (action == null) return false;
                if (action.SkillId == PortalSkillId) moveCount++;
                if (action.SlotOffset == firstOffset)
                {
                    firstCount++;
                }
                else
                {
                    if (secondOffset < 0) secondOffset = action.SlotOffset;
                    if (action.SlotOffset != secondOffset) return false;
                    secondCount++;
                }
            }
            return firstCount == 2 && secondCount == 2 && moveCount == 1 &&
                   firstOffset == 0 && secondOffset > firstOffset && secondOffset <= 3;
        }

        /// <summary>
        /// 녹턴의 매 턴 행동에는 반드시 이동을 포함합니다.
        /// 공격/당기기를 먼저 예약했으면 뒤 슬롯에 이동과 후속 공격을 시도합니다.
        /// 이동 가능한 칸이 전혀 없을 때는 불가능한 이동을 강제로 예약하지 않습니다.
        /// </summary>
        private MonsterAIPlan CompletePlanWithRequiredMove(
            MonsterAIPlan plan, MonsterUnit monsterUnit, GridManager gridManager)
        {
            if (plan == null || monsterUnit == null || gridManager == null)
                return EnsureTwoActionSlots(plan, monsterUnit, gridManager);

            foreach (MonsterAIAction action in plan.Actions)
            {
                if (action != null && action.SkillId == MoveSkillId)
                    return EnsureTwoActionSlots(plan, monsterUnit, gridManager);
            }

            int nextOffset = 1;
            foreach (MonsterAIAction action in plan.Actions)
            {
                if (action != null && action.SlotOffset >= nextOffset)
                    nextOffset = action.SlotOffset + 1;
            }
            // 앞 슬롯에 이미 공격 2회가 있으면 이동은 다음 슬롯부터 시작합니다.
            // 예약 슬롯이 5개이므로 존재하지 않는 슬롯은 생성하지 않습니다.
            if (nextOffset > 4)
                return EnsureTwoActionSlots(plan, monsterUnit, gridManager);

            bool hasPull = false;
            BattleDirection pullDirection = BattleDirection.Right;
            foreach (MonsterAIAction action in plan.Actions)
            {
                if (action != null && action.SkillId == PullSkillId)
                {
                    hasPull = true;
                    pullDirection = action.ForcedDirection;
                }
            }

            if (hasPull && TryFindSafePullTarget(
                    monsterUnit, monsterUnit.MainGridIndex, gridManager,
                    out BattleCharacter pulledTarget, out BattleDirection predictedDirection) &&
                TryFindPortalTargetAfterPull(
                    monsterUnit, pulledTarget, pullDirection, gridManager,
                    out Vector2Int afterPullOffset, out int afterPullIndex,
                    out BattleDirection afterPullAttackDirection, out bool afterPullRegular))
            {
                AddRearRepositionCombo(plan, afterPullOffset, afterPullIndex,
                    afterPullAttackDirection, afterPullRegular, nextOffset, 0, true);
                return EnsureTwoActionSlots(plan, monsterUnit, gridManager);
            }

            List<BattleCharacter> portalCandidates =
                FindPlayersInSkillRange(PortalSkillId, monsterUnit.MainGridIndex, gridManager);
            if (!hasPull && TryFindPortalTarget(
                    monsterUnit, portalCandidates, gridManager, out _,
                    out Vector2Int portalOffset, out int portalIndex,
                    out BattleDirection portalAttackDirection, out bool regularMove))
            {
                AddRearRepositionCombo(plan, portalOffset, portalIndex,
                    portalAttackDirection, regularMove, nextOffset, 0);
                return EnsureTwoActionSlots(plan, monsterUnit, gridManager);
            }

            // 당기기 후에는 원래 점유 정보만으로 포털 목적지를 추정하면
            // 끌려온 캐릭터가 포털을 막을 수 있으므로 위험한 포털 이동을 제외합니다.
            if (!hasPull)
            {
                Vector2Int step = GetOneTileMoveTowardNearestPlayer(monsterUnit, gridManager);
                if (step != Vector2Int.zero)
                {
                    int destination = GetDestinationGridIndex(monsterUnit, step, gridManager);
                    const int moveGroup = 96;
                    plan.Add(new MonsterAIAction(MoveSkillId, step,
                        MonsterAISlotPreference.Front, moveGroup, 0,
                        destination, false, BattleDirection.Right, true, nextOffset));
                    if (TryPickAttackFromOrigin(destination, gridManager,
                            out string followSkill, out BattleDirection followDirection))
                        plan.Add(new MonsterAIAction(followSkill, Vector2Int.zero,
                            MonsterAISlotPreference.SameSlot, moveGroup, 1,
                            destination, true, followDirection, false, nextOffset));
                }
            }
            return EnsureTwoActionSlots(plan, monsterUnit, gridManager);
        }

        /// <summary>
        /// 녹턴은 두 개의 서로 다른 슬롯을 사용합니다.
        /// 첫 슬롯만 계획된 경우 다음 슬롯에 유효한 당기기 또는 공격을 추가합니다.
        /// </summary>
        private MonsterAIPlan EnsureTwoActionSlots(
            MonsterAIPlan plan, MonsterUnit monsterUnit, GridManager gridManager)
        {
            if (plan == null || monsterUnit == null || gridManager == null || plan.Actions.Count == 0)
                return CompleteSoloAttackSlots(plan);

            HashSet<int> used = new();
            int lastOffset = 0;
            int attackOrigin = monsterUnit.MainGridIndex;
            foreach (MonsterAIAction action in plan.Actions)
            {
                if (action == null) continue;
                used.Add(action.SlotOffset);
                lastOffset = Mathf.Max(lastOffset, action.SlotOffset);
                if (action.SkillId == MoveSkillId && action.RangeOriginGridIndex >= 0)
                    attackOrigin = action.RangeOriginGridIndex;
            }
            if (used.Count >= 2 || lastOffset >= 4)
                return CompleteSoloAttackSlots(plan);

            int followingOffset = lastOffset + 1;
            // 포털 이동이 먼저 실행된 경우에는 다음 슬롯에 26/27 단독 공격을
            // 보충하지 않습니다. 캐릭터가 이동했을 가능성이 있기 때문입니다.
            bool movedInEarlierSlot = false;
            foreach (MonsterAIAction action in plan.Actions)
            {
                if (action != null && action.SkillId == MoveSkillId &&
                    action.SlotOffset < followingOffset)
                {
                    movedInEarlierSlot = true;
                    break;
                }
            }

            if (TryFindSafePullTarget(monsterUnit, attackOrigin, gridManager,
                    out _, out BattleDirection pullDirection))
            {
                AddPullCombo(plan, monsterUnit, attackOrigin, gridManager,
                    pullDirection, followingOffset, 0);
            }
            else if (!movedInEarlierSlot &&
                     TryPickAttackFromOrigin(attackOrigin, gridManager,
                         out string skill, out BattleDirection direction))
            {
                const int followGroup = 198;
                plan.Add(new MonsterAIAction(skill, Vector2Int.zero,
                    MonsterAISlotPreference.Front, followGroup, 0,
                    attackOrigin, true, direction, false, followingOffset));
            }
            else
            {
                // 공격 대상이 현재 사거리 밖이라면 다음 슬롯에는 위치 제어 스킬을 예약합니다.
                // 유효 타겟이 없어도 무효 이동을 강제하는 것보다 안전합니다.
                plan.Add(new MonsterAIAction(PullSkillId, Vector2Int.zero,
                    MonsterAISlotPreference.Front, 199, 0,
                    attackOrigin, true,
                    GetDirectionTowardNearestPlayer(attackOrigin, gridManager),
                    false, followingOffset));
            }
            return CompleteSoloAttackSlots(plan);
        }

        /// <summary>
        /// 26/27 공격만 단독으로 예약된 슬롯은 동일 공격을 한 번 더 사용합니다.
        /// 이동/당기기/다른 공격과 같은 슬롯에 있는 경우에는 추가하지 않습니다.
        /// </summary>
        private static MonsterAIPlan CompleteSoloAttackSlots(MonsterAIPlan plan)
        {
            if (plan == null || plan.Actions.Count == 0)
                return plan;

            // 플래너의 SameSlotGroup 및 SlotOffset이 동일한 행동은 같은 슬롯에서 실행됩니다.
            List<MonsterAIAction> extra = new();
            for (int i = 0; i < plan.Actions.Count; i++)
            {
                MonsterAIAction action = plan.Actions[i];
                if (action == null ||
                    (action.SkillId != ThrustSkillId && action.SkillId != SlashSkillId))
                    continue;

                bool hasCompanion = false;
                for (int j = 0; j < plan.Actions.Count; j++)
                {
                    if (i == j) continue;
                    MonsterAIAction other = plan.Actions[j];
                    if (other != null &&
                        other.SameSlotGroup == action.SameSlotGroup &&
                        other.SlotOffset == action.SlotOffset)
                    {
                        hasCompanion = true;
                        break;
                    }
                }
                if (hasCompanion)
                    continue;

                // 이동 이후 슬롯의 일반 공격은 연속 공격으로 확장하지 않습니다.
                // 이동 전 슬롯에서의 공격 2회 패턴은 그대로 유지합니다.
                bool hadEarlierMove = false;
                for (int j = 0; j < plan.Actions.Count; j++)
                {
                    MonsterAIAction previous = plan.Actions[j];
                    if (previous != null && previous.SkillId == MoveSkillId &&
                        previous.SlotOffset < action.SlotOffset)
                    {
                        hadEarlierMove = true;
                        break;
                    }
                }
                if (hadEarlierMove)
                    continue;

                extra.Add(new MonsterAIAction(
                    action.SkillId, Vector2Int.zero, MonsterAISlotPreference.SameSlot,
                    action.SameSlotGroup, action.Priority + 1,
                    action.RangeOriginGridIndex, action.HasForcedDirection,
                    action.ForcedDirection, false, action.SlotOffset,
                    action.ExplicitRangeGridIndices, action.RangeOriginCasterGridIndex));
            }
            for (int i = 0; i < extra.Count; i++)
                plan.Add(extra[i]);

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
                0,
                destinationGridIndex, false, BattleDirection.Right, true));

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
            // 목적지가 바로 옆 칸이어도 녹턴은 항상 포털로 순간이동합니다.
            AddPortalCombo(plan, moveOffset, destinationGridIndex, attackDirection,
                slotOffset, priorityBase, keepPredictedAttackDirection);
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

            // 포탈 이동 후에는 26으로 공격하며 후방 대상을 향하도록 고정합니다.
            // 다른 캐릭터가 옆에 있더라도 공격 방향을 반대로 바꾸지 않습니다.
            string portalAttackSkill = ThrustSkillId;
            BattleDirection finalPortalDirection = attackDirection;

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
            MonsterUnit monsterUnit,
            int originGridIndex,
            GridManager gridManager,
            BattleDirection pullDirection,
            int slotOffset,
            int priorityBase)
        {
            if (plan == null)
                return;

            const int pullGroup = 20;
            plan.Add(new MonsterAIAction(
                PullSkillId, Vector2Int.zero, MonsterAISlotPreference.Front,
                pullGroup, priorityBase, originGridIndex, true,
                pullDirection, false, slotOffset));

            // 28 이후 이동한 캐릭터의 예상 좌표를 고려하여 27을 연계합니다.
            // 다음 공격은 당기기와 같은 슬롯에 항상 예약합니다.
            // 당기기 이후 예상 적중이 0이어도 캐릭터가 이전 슬롯에서 이동할 수 있으므로
            // 27을 생략하지 않고 해당 방향의 공격으로 연계합니다.
            string attackSkill = SlashSkillId;
            plan.Add(new MonsterAIAction(
                attackSkill, Vector2Int.zero, MonsterAISlotPreference.SameSlot,
                pullGroup, priorityBase + 1, originGridIndex, true,
                pullDirection, false, slotOffset));
        }

        private void AddPostRepositionCombo(
            MonsterAIPlan plan, MonsterUnit monsterUnit,
            int originGridIndex, GridManager gridManager,
            int slotOffset, BattleDirection fallbackDirection)
        {
            // 이동 후에도 당기기를 독립적인 위치 제어 후보로 평가합니다.
            // 후속 공격이 지금 적중하지 않더라도, 캐릭터의 이동을 제한할 수 있습니다.
            // 이동 후 후속 슬롯에는 일반 공격 연속 2회를 사용하지 않습니다.
            // 유효한 끌어당기기 대상이 있으면 28을 우선 예약하고,
            // 당긴 뒤의 예상 위치에서 적중 가능한 경우에만 26/27을 이어서 예약합니다.
            if (TryFindSafePullTarget(monsterUnit, originGridIndex, gridManager,
                    out _, out BattleDirection pullDirection))
            {
                const int group = 71;
                plan.Add(new MonsterAIAction(
                    PullSkillId, Vector2Int.zero, MonsterAISlotPreference.Front,
                    group, 0, originGridIndex, true, pullDirection, false, slotOffset));
                // 이동 후 후속 슬롯에서도 28 다음에는 26/27을 반드시 예약합니다.
                string skill = PickAttackAfterPullOrFallback(
                    monsterUnit, originGridIndex, gridManager, pullDirection);
                plan.Add(new MonsterAIAction(
                    skill, Vector2Int.zero, MonsterAISlotPreference.SameSlot,
                    group, 1, originGridIndex, true, pullDirection, false, slotOffset));
                return;
            }

            // 이동 후 다음 슬롯에서 당기기가 불가능하다면 추가 일반 공격을 예약하지 않습니다.
            // 캐릭터는 이동할 수 있으므로 이동 시점의 위치만으로
            // 후속 슬롯에 26/27 연속 공격을 확정하지 않습니다.
        }

        /// <summary>
        /// 당기기 이후 적중 예측이 불확실하더라도 같은 방향의 후속 공격을 선택합니다.
        /// 예측된 적중 대상이 있으면 그 결과를 우선하고, 없으면 현재 범위를 비교합니다.
        /// </summary>
        private string PickAttackAfterPullOrFallback(
            MonsterUnit monsterUnit, int originGridIndex, GridManager gridManager,
            BattleDirection pullDirection)
        {
            if (TryPickAttackAfterPull(monsterUnit, originGridIndex, gridManager,
                    pullDirection, out string skill, out _))
                return skill;

            return PickAttackInFixedDirection(
                originGridIndex, gridManager, pullDirection, false);
        }

        private bool TryPickAttackAfterPull(
            MonsterUnit monsterUnit, int originGridIndex, GridManager gridManager,
            BattleDirection pullDirection, out string skillId,
            out BattleDirection attackDirection)
        {
            skillId = string.Empty;
            attackDirection = pullDirection;
            Dictionary<int, int> predicted = PredictPullPositions(
                monsterUnit, originGridIndex, gridManager, pullDirection);
            if (predicted.Count == 0)
                return false;

            // 당겨진 대상과의 연계 공격은 당긴 방향을 유지합니다.
            // 반대편의 다른 캐릭터가 더 많이 맞는다는 이유로 방향을 바꾸지 않습니다.
            int thrustHits = CountPredictedHitsInDirection(
                ThrustSkillId, originGridIndex, gridManager, pullDirection, predicted);
            int slashHits = CountPredictedHitsInDirection(
                SlashSkillId, originGridIndex, gridManager, pullDirection, predicted);
            if (thrustHits <= 0 && slashHits <= 0)
                return false;

            skillId = slashHits > thrustHits ? SlashSkillId
                : thrustHits > slashHits ? ThrustSkillId
                : BattleRandom.Range(0, 2) == 0 ? SlashSkillId : ThrustSkillId;
            return true;
        }

        private int CountPredictedHitsInDirection(
            string skillId, int originGridIndex, GridManager gridManager,
            BattleDirection direction, Dictionary<int, int> predictedPositions)
        {
            MonsterSkillData skillData = DataManager.Instance?.MonsterSkillDatabase?.Get(skillId);
            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;
            if (skillData == null || rangeDatabase == null || gridManager == null ||
                string.IsNullOrWhiteSpace(skillData.RangeId))
                return 0;

            List<int> range = BattleRangeCalculator.GetDirectionRangeIndices(
                originGridIndex, skillData.RangeId, direction, rangeDatabase, gridManager);
            if (range == null || range.Count == 0)
                return 0;

            HashSet<int> cells = new(range);
            int hits = 0;
            foreach (BattleCharacter player in FindPlayers())
            {
                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                int position = predictedPositions != null &&
                               predictedPositions.TryGetValue(player.CurrentGridIndex, out int next)
                    ? next : player.CurrentGridIndex;
                if (cells.Contains(position))
                    hits++;
            }
            return hits;
        }

        private Dictionary<int, int> PredictPullPositions(
            MonsterUnit monsterUnit, int originGridIndex, GridManager gridManager,
            BattleDirection direction)
        {
            Dictionary<int, int> predicted = new();
            if (monsterUnit == null || originGridIndex < 0 || gridManager == null)
                return predicted;

            MonsterSkillData pullData = DataManager.Instance?.MonsterSkillDatabase?.Get(PullSkillId);
            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;
            if (pullData == null || rangeDatabase == null || string.IsNullOrWhiteSpace(pullData.RangeId))
                return predicted;

            List<int> range = BattleRangeCalculator.GetDirectionRangeIndices(
                originGridIndex, pullData.RangeId, direction, rangeDatabase, gridManager);
            if (range == null)
                return predicted;

            HashSet<int> affected = new(range);
            Vector2Int delta = direction == BattleDirection.Left
                ? Vector2Int.right : Vector2Int.left;
            BattleCharacter[] players = FindPlayers();
            foreach (BattleCharacter player in players)
            {
                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0 ||
                    !affected.Contains(player.CurrentGridIndex))
                    continue;

                Vector2Int nextCoord = gridManager.IndexToCoord(player.CurrentGridIndex) + delta;
                if (!gridManager.IsValidCoord(nextCoord))
                    continue;
                int next = gridManager.CoordToIndex(nextCoord);
                // 이미 차지된 칸이나 녹턴의 점유 칸으로는 당겨지지 않는 것으로 예측합니다.
                // 실제 GrabEffect와 동일하게 장애물과 유닛 점유를 검사합니다.
                // 이동 불가 그리드 효과가 있으면 당기기는 실패하며 원래 칸을 유지합니다.
                if (BattleOccupancyService.IsOccupiedByAnyUnit(next, player.CharacterId) ||
                    IsGridEffectBlocked(next) ||
                    BattleEquipmentEffectService.IsForcedMoveImmune(player.RuntimeData) ||
                    WouldPullTargetIntoNocturn(monsterUnit, originGridIndex,
                        player.CurrentGridIndex, direction, gridManager))
                    continue;
                predicted[player.CurrentGridIndex] = next;
            }
            return predicted;
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

        private string PickAttackInFixedDirection(
            int originGridIndex,
            GridManager gridManager,
            BattleDirection attackDirection,
            bool usePredictedPlacement)
        {
            // 후방 진입 후에는 방향을 새로 탐색하지 않고 그 방향에서만 스킬을 비교합니다.
            // 끌어당기기 후 위치 예측은 별도의 연계 단계에서 처리합니다.
            if (usePredictedPlacement || originGridIndex < 0 || gridManager == null)
                return SlashSkillId;

            int thrustHits = CountHitsInDirection(
                ThrustSkillId, originGridIndex, gridManager, attackDirection);
            int slashHits = CountHitsInDirection(
                SlashSkillId, originGridIndex, gridManager, attackDirection);

            if (slashHits > thrustHits)
                return SlashSkillId;
            if (thrustHits > slashHits)
                return ThrustSkillId;
            return BattleRandom.Range(0, 2) == 0 ? SlashSkillId : ThrustSkillId;
        }

        private int CountHitsInDirection(
            string skillId,
            int originGridIndex,
            GridManager gridManager,
            BattleDirection direction)
        {
            MonsterSkillData skillData =
                DataManager.Instance?.MonsterSkillDatabase?.Get(skillId);
            RangeDatabase rangeDatabase = DataManager.Instance?.RangeDatabase;
            if (skillData == null || rangeDatabase == null ||
                string.IsNullOrWhiteSpace(skillData.RangeId) || skillData.RangeId == "0")
                return 0;

            List<int> indices = BattleRangeCalculator.GetDirectionRangeIndices(
                originGridIndex, skillData.RangeId, direction, rangeDatabase, gridManager);
            if (indices == null || indices.Count == 0)
                return 0;

            HashSet<int> hitCells = new(indices);
            int hits = 0;
            foreach (BattleCharacter player in FindPlayers())
            {
                if (IsAlivePlayer(player) && player.CurrentGridIndex >= 0 &&
                    hitCells.Contains(player.CurrentGridIndex))
                    hits++;
            }
            return hits;
        }

        private bool TryPickAttackFromOrigin(
            int originGridIndex,
            GridManager gridManager,
            out string selectedSkillId,
            out BattleDirection selectedDirection,
            Dictionary<int, int> predictedPositions = null)
        {
            selectedSkillId = string.Empty;
            selectedDirection = BattleDirection.Right;

            if (originGridIndex < 0 || gridManager == null)
                return false;

            int thrustBestHits = GetBestHitCount(
                ThrustSkillId,
                originGridIndex,
                gridManager,
                out BattleDirection thrustDirection, predictedPositions);

            int slashBestHits = GetBestHitCount(
                SlashSkillId,
                originGridIndex,
                gridManager,
                out BattleDirection slashDirection, predictedPositions);

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

        /// <summary>
        /// 양방향으로 공격이 가능한 경우 맵 중앙을 향한 방향을 먼저 검사합니다.
        /// 좌우 가장자리까지 남은 칸 수로 판단하므로 맵 크기가 달라도 적용됩니다.
        /// </summary>
        private static BattleDirection[] GetInwardFirstDirections(
            int originGridIndex, GridManager gridManager)
        {
            BattleDirection inward = BattleDirection.Right;
            if (gridManager != null && originGridIndex >= 0)
            {
                Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
                int leftSpace = 0;
                int rightSpace = 0;
                while (gridManager.IsValidCoord(origin + Vector2Int.left * (leftSpace + 1)))
                    leftSpace++;
                while (gridManager.IsValidCoord(origin + Vector2Int.right * (rightSpace + 1)))
                    rightSpace++;
                inward = rightSpace >= leftSpace ? BattleDirection.Right : BattleDirection.Left;
            }
            return inward == BattleDirection.Right
                ? new[] { BattleDirection.Right, BattleDirection.Left }
                : new[] { BattleDirection.Left, BattleDirection.Right };
        }

        private int GetBestHitCount(
            string skillId,
            int originGridIndex,
            GridManager gridManager,
            out BattleDirection selectedDirection,
            Dictionary<int, int> predictedPositions = null)
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
            BattleDirection[] directions = GetInwardFirstDirections(originGridIndex, gridManager);

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

                    if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                        continue;
                    int playerIndex = player.CurrentGridIndex;
                    if (predictedPositions != null && predictedPositions.TryGetValue(
                            playerIndex, out int predictedIndex))
                        playerIndex = predictedIndex;
                    if (rangeSet.Contains(playerIndex))
                        hitCount++;
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
            BattleDirection[] directions = GetInwardFirstDirections(originGridIndex, gridManager);

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
            BattleDirection[] directions = GetInwardFirstDirections(originGridIndex, gridManager);

            int bestPredictedHits = -1;
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
                    Dictionary<int, int> predicted = PredictPullPositions(
                        monsterUnit, originGridIndex, gridManager, direction);
                    int hits = CountPredictedHitsInDirection(
                        SlashSkillId, originGridIndex, gridManager, direction, predicted);
                    if (hits > bestPredictedHits)
                    {
                        bestPredictedHits = hits;
                        selectedTarget = firstTarget;
                        selectedDirection = direction;
                    }
                }
            }

            return selectedTarget != null;
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
            out bool useRegularMove,
            bool otherCharacterOnly = false)
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

            Dictionary<int, int> predictedPositions = PredictPullPositions(
                monsterUnit, monsterUnit.MainGridIndex, gridManager, pullDirection);
            if (!predictedPositions.TryGetValue(
                    pullTarget.CurrentGridIndex, out int predictedPulledTargetGridIndex))
                return false;
            Vector2Int predictedPulledTargetCoord =
                gridManager.IndexToCoord(predictedPulledTargetGridIndex);

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

                if (!IsAlivePlayer(candidate) || candidate.CurrentGridIndex < 0 ||
                    (otherCharacterOnly && candidate == pullTarget))
                    continue;

                int predictedCandidateIndex = predictedPositions.TryGetValue(
                    candidate.CurrentGridIndex, out int nextCandidateIndex)
                    ? nextCandidateIndex : candidate.CurrentGridIndex;
                Vector2Int candidateCoord = gridManager.IndexToCoord(predictedCandidateIndex);
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
                        predictedPositions,
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

            // 당기기 이후 후방이 1칸 거리라도 포털 이동을 사용합니다.
            useRegularMove = false;
            return true;
        }

        private bool IsPortalDestinationAvailableAfterPull(
            MonsterUnit monsterUnit,
            BattleCharacter pullTarget,
            Dictionary<int, int> predictedPositions,
            int destinationGridIndex)
        {
            if (monsterUnit == null || pullTarget == null || destinationGridIndex < 0 ||
                predictedPositions == null)
                return false;

            if (destinationGridIndex == monsterUnit.MainGridIndex ||
                IsGridEffectBlocked(destinationGridIndex) ||
                predictedPositions.ContainsValue(destinationGridIndex))
                return false;

            // 실제로 당겨져 비워질 칸만 예외로 인정합니다.
            // 다른 캐릭터가 당겨져 목적지를 차지한다면 포털 후보에서 제외합니다.
            if (predictedPositions.ContainsKey(destinationGridIndex))
                return true;

            return !BattleOccupancyService.IsOccupiedByAnyUnit(
                destinationGridIndex, null, monsterUnit);
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

            // 이동 거리에 상관없이 그림자걸음 포털로 이동합니다.
            useRegularMove = false;
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
