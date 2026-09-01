using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Relic.Gameplay.Battle;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public delegate bool EventChoiceRewardGrant(out string resultMessage);
    public delegate bool EventChoiceRemnantRewardGrant(int amount, out string resultMessage);
    public delegate void EventChoiceRemnantRewardRevoke(int amount);
    public delegate bool EventChoiceEquippedRelicCostRevoke(
        EventChoiceEquippedRelicCost cost,
        out string resultMessage);
    public delegate bool EventChoiceSkillAwakenGrant(
        EventChoiceSkillAwakenTarget target,
        out string resultMessage);
    public delegate bool EventChoiceSkillAwakenRollback(
        IReadOnlyList<EventChoiceSkillAwakenTarget> targets,
        out string resultMessage);
    public enum EventChoiceSkillRewardFilter
    {
        Attack,
        Buff,
        Debuff,
        CommonToRare,
        Epic
    }

    public delegate bool EventChoiceFilteredSkillRewardGrant(
        EventChoiceSkillRewardFilter filter,
        int count,
        out string resultMessage);

    public enum EventChoiceSkillSlotKind
    {
        Passive,
        Unique,
        Ability,
        Equipped
    }

    public readonly struct EventChoiceSkillAwakenTarget
    {
        public EventChoiceSkillAwakenTarget(
            string characterId,
            EventChoiceSkillSlotKind slotKind,
            int slotIndex,
            string skillId,
            string upgradeSkillId)
        {
            CharacterId = Normalize(characterId);
            SlotKind = slotKind;
            SlotIndex = slotIndex;
            SkillId = Normalize(skillId);
            UpgradeSkillId = Normalize(upgradeSkillId);
        }

        public string CharacterId { get; }
        public EventChoiceSkillSlotKind SlotKind { get; }
        public int SlotIndex { get; }
        public string SkillId { get; }
        public string UpgradeSkillId { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(CharacterId) &&
            !string.IsNullOrWhiteSpace(SkillId) &&
            !string.IsNullOrWhiteSpace(UpgradeSkillId) &&
            (SlotKind != EventChoiceSkillSlotKind.Equipped || SlotIndex >= 0);

        private static string Normalize(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }
    }

    public readonly struct EventChoiceEquippedRelicCost
    {
        public EventChoiceEquippedRelicCost(string characterId, int relicSlotIndex, string relicId)
        {
            CharacterId = Normalize(characterId);
            RelicSlotIndex = relicSlotIndex;
            RelicId = Normalize(relicId);
        }

        public string CharacterId { get; }
        public int RelicSlotIndex { get; }
        public string RelicId { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(CharacterId) &&
            RelicSlotIndex >= 0 &&
            !string.IsNullOrWhiteSpace(RelicId);

        private static string Normalize(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }
    }

    public sealed class EventChoiceSessionState
    {
        public int AccumulatedRemnant;
        public int LastImmediateRemnant;
        public readonly List<EventChoiceSkillAwakenTarget> AwakenedSkillTargets = new();
    }

    public sealed class EventChoiceExecutionContext
    {
        public BattleRuntimeData BattleRuntime;
        public IReadOnlyList<CharacterRuntimeData> PartyCharacters;
        public EventChoiceSessionState SessionState;
        public Func<int[]> RollDiceFaces;
        public Func<int> RollThreeDice;
        public Func<float> RollChanceValue;
        public EventChoiceRewardGrant GrantRandomRelic;
        public EventChoiceRewardGrant GrantRandomSkill;
        public EventChoiceRewardGrant UpgradeRandomSkill;
        public EventChoiceRemnantRewardGrant GrantRemnant;
        public EventChoiceRemnantRewardRevoke RevokeRemnant;
        public EventChoiceEquippedRelicCost SelectedEquippedRelicCost;
        public EventChoiceEquippedRelicCostRevoke RevokeEquippedRelic;
        public EventChoiceSkillAwakenTarget SelectedSkillAwakenTarget;
        public EventChoiceSkillAwakenGrant UpgradeSelectedSkill;
        public EventChoiceSkillAwakenRollback RollbackAwakenedSkills;
        public EventChoiceFilteredSkillRewardGrant OfferFilteredSkillRewards;
        public Func<bool> HasUpgradeableEquippedSkill;
        public Func<bool> OpenShop;
        public Action RefreshRemnantHud;
        public bool SuppressRewardResultMessages;
    }

    public readonly struct EventChoiceExecutionResult
    {
        public EventChoiceExecutionResult(
            bool accepted,
            string resultMessage,
            string nextEventId,
            string visualObjectId = "",
            string visualActionId = "",
            bool succeeded = true,
            int diceRoll = 0,
            IReadOnlyList<int> diceFaces = null)
        {
            Accepted = accepted;
            Succeeded = accepted && succeeded;
            ResultMessage = resultMessage ?? string.Empty;
            NextEventId = EventIdUtility.Normalize(nextEventId);
            VisualObjectId = NormalizeId(visualObjectId);
            VisualActionId = NormalizeId(visualActionId);
            DiceRoll = Mathf.Max(0, diceRoll);
            DiceFaces = CopyDiceFaces(diceFaces);
        }

        public bool Accepted { get; }
        public bool Succeeded { get; }
        public string ResultMessage { get; }
        public string NextEventId { get; }
        public bool HasNextEvent => !string.IsNullOrWhiteSpace(NextEventId);
        public string VisualObjectId { get; }
        public string VisualActionId { get; }
        public int DiceRoll { get; }
        public IReadOnlyList<int> DiceFaces { get; }
        public bool HasVisualAction =>
            !string.IsNullOrWhiteSpace(VisualObjectId) &&
            !string.IsNullOrWhiteSpace(VisualActionId);

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }

        private static int[] CopyDiceFaces(IReadOnlyList<int> diceFaces)
        {
            if (diceFaces == null || diceFaces.Count == 0)
                return Array.Empty<int>();

            int[] copy = new int[diceFaces.Count];
            for (int i = 0; i < diceFaces.Count; i++)
                copy[i] = diceFaces[i];
            return copy;
        }
    }

    public static class EventChoiceExecutionService
    {
        public const int DefaultImmediateRemnantAmount = 100;
        public const int DefaultTradeRemnantCost = 100;
        public const int DefaultRestHealAmount = 10;
        public const int DefaultFailureHpDamage = -5;
        public const int DefaultOfferChoiceSkillCount = 2;
        public const int SmallRemnantAmount = 30;
        public const int MediumRemnantAmount = 60;
        public const int LargeRemnantAmount = 100;

        public static bool CanSelect(
            EventData choice,
            EventChoiceExecutionContext context,
            out string unavailableReason)
        {
            unavailableReason = string.Empty;

            if (choice == null)
            {
                unavailableReason = "선택지 데이터가 없습니다.";
                return false;
            }

            context = NormalizeContext(context);

            if (RequiresSkillAwakenSelection(choice) && !HasAnyUpgradeableEquippedSkill(context))
            {
                unavailableReason = "강화 가능한 장착 기억이 없습니다.";
                return false;
            }

            if (RequiresTargetSelection(choice))
            {
                unavailableReason = "대상 선택 UI가 필요합니다.";
                return false;
            }

            if (IsToken(choice.ResultType, "OfferChoice") ||
                IsToken(choice.ResultType, "SelectReward") ||
                IsToken(choice.ChoiceType, "SelectReward"))
            {
                if (!IsSupportedTypedSkillRewardOffer(choice))
                {
                    unavailableReason = "보상 선택 UI가 필요합니다.";
                    return false;
                }
            }

            if (IsToken(choice.ResultType, "CommitAccumulated") ||
                ContainsAny(choice.SelectCondition, "채굴 1회 이상 성공"))
            {
                if (context.SessionState.AccumulatedRemnant <= 0)
                {
                    unavailableReason = "채굴에 한 번 이상 성공해야 합니다.";
                    return false;
                }
            }

            if (RequiresEquippedRelicCostSelection(choice))
            {
                if (!HasAnyEquippedRelic(context))
                {
                    unavailableReason = "장착 중인 유물이 없습니다.";
                    return false;
                }
            }
            else if (ContainsAny(choice.SelectCondition, "유물 1개 이상 보유") &&
                !HasAnyOwnedRelic(context))
            {
                unavailableReason = "보유한 유물이 없습니다.";
                return false;
            }

            if (ContainsAny(choice.SelectCondition, "미각성 기억", "각성하지 않은 기억") &&
                !HasAnyUpgradeableEquippedSkill(context))
            {
                unavailableReason = "강화 가능한 장착 기억이 없습니다.";
                return false;
            }

            if (ContainsAny(choice.SelectCondition, "기억 1개 이상 보유") &&
                !HasAnyOwnedSkill(context))
            {
                unavailableReason = "보유한 기억이 없습니다.";
                return false;
            }

            if (IsRedDustiumCost(choice))
            {
                int cost = ResolveRemnantAmount(choice.CostValue, DefaultTradeRemnantCost);
                if (context.BattleRuntime == null || context.BattleRuntime.Remnant < cost)
                {
                    unavailableReason = $"레드 더스티움 {cost}이 필요합니다.";
                    return false;
                }
            }

            return true;
        }

        public static EventChoiceExecutionResult Execute(
            EventData choice,
            EventChoiceExecutionContext context)
        {
            context = NormalizeContext(context);

            if (!CanSelect(choice, context, out string unavailableReason))
                return new EventChoiceExecutionResult(false, unavailableReason, string.Empty);

            if (RequiresSkillAwakenSelection(choice) &&
                !context.SelectedSkillAwakenTarget.IsValid)
            {
                return new EventChoiceExecutionResult(false, "강화할 기억을 선택해야 합니다.", string.Empty);
            }

            List<string> messages = new();

            if (!TryApplyCost(choice, context, messages, out unavailableReason))
                return new EventChoiceExecutionResult(false, unavailableReason, string.Empty);

            int diceRoll = 0;
            IReadOnlyList<int> diceFaces = Array.Empty<int>();
            bool success = true;

            if (IsToken(choice.ChoiceType, "Dice"))
            {
                diceFaces = RollDiceFaces(context);
                diceRoll = SumDiceFaces(diceFaces);
                messages.Add($"주사위 결과: {diceRoll}");

                if (!string.IsNullOrWhiteSpace(choice.SuccessCondition))
                    success = IsDiceSuccess(diceRoll, choice.SuccessCondition);
            }
            else if (IsToken(choice.ChoiceType, "Chance"))
            {
                success = RollChance(choice.SuccessRate, context);
                messages.Add(success ? "판정 성공" : "판정 실패");
            }

            if (!success)
            {
                string failure = ApplyFailureResult(choice.FailResult, context);
                if (!string.IsNullOrWhiteSpace(failure))
                    messages.Add(failure);

                string awakenRollback = ApplyAwakenFailureRollback(choice, context);
                if (!string.IsNullOrWhiteSpace(awakenRollback))
                    messages.Add(awakenRollback);

                string nextEventId = RequiresSkillAwakenSelection(choice)
                    ? string.Empty
                    : choice.NextEventId;

                return new EventChoiceExecutionResult(
                    true,
                    JoinMessages(messages),
                    nextEventId,
                    choice.FailureVisualObjectId,
                    choice.FailureVisualActionId,
                    succeeded: false,
                    diceRoll: diceRoll,
                    diceFaces: diceFaces);
            }

            string result = ApplySuccessResult(choice, diceRoll, context);
            if (!string.IsNullOrWhiteSpace(result))
                messages.Add(result);

            return new EventChoiceExecutionResult(
                true,
                JoinMessages(messages),
                choice.NextEventId,
                choice.SuccessVisualObjectId,
                choice.SuccessVisualActionId,
                diceRoll: diceRoll,
                diceFaces: diceFaces);
        }

        private static EventChoiceExecutionContext NormalizeContext(EventChoiceExecutionContext context)
        {
            context ??= new EventChoiceExecutionContext();
            context.SessionState ??= new EventChoiceSessionState();
            return context;
        }

        public static bool RequiresEquippedRelicCostSelection(EventData choice)
        {
            return choice != null &&
                   ContainsAny(choice.CostType, "유물") &&
                   (ContainsAny(choice.CostTarget, "선택 유물") ||
                    ContainsAny(choice.CostValue, "선택 유물"));
        }

        public static bool RequiresSkillAwakenSelection(EventData choice)
        {
            if (choice == null)
                return false;

            if (IsToken(choice.ResultType, "Awaken"))
                return true;

            return ContainsAny(choice.ResultTarget, "선택 기억") &&
                   ContainsAny(choice.ResultValue, "각성", "강화");
        }

        private static bool RequiresTargetSelection(EventData choice)
        {
            if (choice == null)
                return false;

            if (RequiresSkillAwakenSelection(choice))
                return false;

            if (ContainsAny(choice.ResultTarget, "선택 기억"))
            {
                return true;
            }

            return false;
        }

        private static bool TryApplyCost(
            EventData choice,
            EventChoiceExecutionContext context,
            List<string> messages,
            out string unavailableReason)
        {
            unavailableReason = string.Empty;

            if (RequiresEquippedRelicCostSelection(choice))
            {
                if (!context.SelectedEquippedRelicCost.IsValid)
                {
                    unavailableReason = "삭제할 장착 유물을 선택해야 합니다.";
                    return false;
                }

                if (context.RevokeEquippedRelic == null)
                {
                    unavailableReason = "장착 유물 삭제 처리가 준비되지 않았습니다.";
                    return false;
                }

                if (!context.RevokeEquippedRelic(context.SelectedEquippedRelicCost, out string resultMessage))
                {
                    unavailableReason = string.IsNullOrWhiteSpace(resultMessage)
                        ? "선택한 장착 유물을 삭제하지 못했습니다."
                        : resultMessage;
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(resultMessage))
                    messages.Add(resultMessage);
            }

            if (!IsRedDustiumCost(choice))
                return true;

            if (context.BattleRuntime == null)
            {
                unavailableReason = "전투 런타임 데이터가 없습니다.";
                return false;
            }

            int cost = ResolveRemnantAmount(choice.CostValue, DefaultTradeRemnantCost);
            context.BattleRuntime.Remnant = Mathf.Max(0, context.BattleRuntime.Remnant - cost);
            context.RefreshRemnantHud?.Invoke();
            messages.Add($"레드 더스티움 {cost} 지불");
            return true;
        }

        private static string ApplySuccessResult(
            EventData choice,
            int diceRoll,
            EventChoiceExecutionContext context)
        {
            string resultType = choice.ResultType?.Trim();

            if (string.IsNullOrWhiteSpace(resultType))
                return BuildResultSummary(choice);

            if (IsToken(resultType, "RollTable"))
                return ApplyRollTable(choice, diceRoll, context);

            if (IsToken(resultType, "Gain"))
                return ApplyGain(choice, context);

            if (IsToken(resultType, "GainRandom"))
                return ApplyGainRandom(choice, context);

            if (IsToken(resultType, "GainMultiple"))
                return ApplyGainMultiple(choice, context);

            if (IsToken(resultType, "OfferChoice") || IsToken(resultType, "SelectReward"))
                return ApplyOfferChoice(choice, context);

            if (IsToken(resultType, "Modify"))
                return ApplyModify(choice, context);

            if (IsToken(resultType, "Heal"))
                return ApplyHeal(choice, context);

            if (IsToken(resultType, "Accumulate"))
                return ApplyAccumulate(choice, context);

            if (IsToken(resultType, "CommitAccumulated"))
                return ApplyCommitAccumulated(context);

            if (IsToken(resultType, "OpenPanel"))
                return context.OpenShop != null && context.OpenShop()
                    ? "상점 패널을 열었습니다."
                    : BuildResultSummary(choice);

            if (IsToken(resultType, "Awaken"))
                return ApplyAwaken(choice, context);

            if (IsToken(resultType, "UpgradeRandom"))
                return TryInvokeGrant(context.UpgradeRandomSkill, "강화 가능한 기억이 없습니다.");

            if (IsToken(resultType, "EndEvent"))
                return "이벤트를 종료합니다.";

            return BuildResultSummary(choice);
        }

        private static string ApplyAwaken(EventData choice, EventChoiceExecutionContext context)
        {
            if (!context.SelectedSkillAwakenTarget.IsValid)
                return "강화할 기억을 선택해야 합니다.";

            if (context.UpgradeSelectedSkill == null)
                return "선택 기억 강화 처리가 준비되지 않았습니다.";

            bool upgraded = context.UpgradeSelectedSkill(
                context.SelectedSkillAwakenTarget,
                out string resultMessage);
            if (!upgraded)
            {
                return string.IsNullOrWhiteSpace(resultMessage)
                    ? "선택한 기억을 강화하지 못했습니다."
                    : resultMessage;
            }

            context.SessionState?.AwakenedSkillTargets.Add(context.SelectedSkillAwakenTarget);

            if (!string.IsNullOrWhiteSpace(resultMessage))
                return resultMessage;

            string upgradeSkillId = context.SelectedSkillAwakenTarget.UpgradeSkillId;
            return string.IsNullOrWhiteSpace(upgradeSkillId)
                ? BuildResultSummary(choice)
                : $"기억 강화: {upgradeSkillId}";
        }

        private static string ApplyAwakenFailureRollback(
            EventData choice,
            EventChoiceExecutionContext context)
        {
            if (!RequiresSkillAwakenSelection(choice))
                return string.Empty;

            List<EventChoiceSkillAwakenTarget> awakenedTargets =
                context.SessionState?.AwakenedSkillTargets;
            if (awakenedTargets == null || awakenedTargets.Count == 0)
                return string.Empty;

            List<EventChoiceSkillAwakenTarget> targets = new(awakenedTargets);
            bool rolledBack = false;
            string resultMessage = string.Empty;

            if (context.RollbackAwakenedSkills != null)
            {
                rolledBack = context.RollbackAwakenedSkills(targets, out resultMessage);
            }
            else
            {
                resultMessage = "이번 이벤트로 얻은 기억 제거 처리가 준비되지 않았습니다.";
            }

            awakenedTargets.Clear();

            if (!rolledBack && string.IsNullOrWhiteSpace(resultMessage))
                return "이번 이벤트로 얻은 기억을 제거하지 못했습니다.";

            return resultMessage;
        }

        private static string ApplyRollTable(
            EventData choice,
            int diceRoll,
            EventChoiceExecutionContext context)
        {
            string tableId = choice.ResultValue?.Trim();

            if (IsToken(tableId, "RT001"))
            {
                int amount = diceRoll <= 8 ? 3 : diceRoll <= 15 ? 5 : 10;
                int count = ModifyPartyCurrentHp(context, amount);
                return $"파티 전원 체력 {amount} 회복 ({count}명)";
            }

            if (IsToken(tableId, "RT002"))
            {
                int amount = diceRoll <= 8 ? 2 : diceRoll <= 15 ? 4 : 8;
                int count = ModifyPartyMaxHp(context, amount);
                return $"파티 전원 최대 체력 {amount} 증가 ({count}명)";
            }

            if (IsToken(tableId, "RT003"))
            {
                int amount = diceRoll <= 8
                    ? SmallRemnantAmount
                    : diceRoll <= 15
                        ? MediumRemnantAmount
                        : LargeRemnantAmount;

                return GrantRemnantReward(context, amount);
            }

            return BuildResultSummary(choice);
        }

        private static string ApplyGain(EventData choice, EventChoiceExecutionContext context)
        {
            if (ContainsAny(choice.ResultTarget, "레드 더스티움"))
            {
                int amount = ResolveRemnantAmount(choice.ResultValue, DefaultImmediateRemnantAmount);
                context.SessionState.LastImmediateRemnant = amount;
                return GrantRemnantReward(context, amount);
            }

            return BuildResultSummary(choice);
        }

        private static string ApplyGainRandom(EventData choice, EventChoiceExecutionContext context)
        {
            if (ContainsAny(choice.ResultTarget, "유물"))
                return TryInvokeRewardGrant(context, context.GrantRandomRelic, "획득 가능한 유물이 없습니다.");

            if (ContainsAny(choice.ResultTarget, "기억"))
                return TryInvokeRewardGrant(context, context.GrantRandomSkill, "획득 가능한 기억이 없습니다.");

            return BuildResultSummary(choice);
        }

        private static string ApplyGainMultiple(EventData choice, EventChoiceExecutionContext context)
        {
            List<string> messages = new();

            if (ContainsAny(choice.ResultTarget, "유물"))
                messages.Add(TryInvokeRewardGrant(context, context.GrantRandomRelic, "획득 가능한 유물이 없습니다."));

            if (ContainsAny(choice.ResultTarget, "레드 더스티움") &&
                TryParseEffectAmount(choice.ResultValue, out int amount))
            {
                messages.Add(GrantRemnantReward(context, Mathf.Max(0, amount)));
            }
            else if (ContainsAny(choice.ResultTarget, "레드 더스티움"))
            {
                messages.Add("기존 레드 더스티움 유지");
            }

            return JoinMessages(messages);
        }

        private static string ApplyOfferChoice(EventData choice, EventChoiceExecutionContext context)
        {
            if (TryResolveSkillRewardOffer(choice, out EventChoiceSkillRewardFilter filter))
            {
                int count = ResolveOfferChoiceCount(choice.ResultValue, DefaultOfferChoiceSkillCount);
                return TryInvokeFilteredSkillRewardGrant(
                    context,
                    context.OfferFilteredSkillRewards,
                    filter,
                    count,
                    "획득 가능한 기억이 없습니다.");
            }

            return BuildResultSummary(choice);
        }

        private static string ApplyModify(EventData choice, EventChoiceExecutionContext context)
        {
            if (!TryParseEffectAmount(choice.ResultValue, out int amount))
                return BuildResultSummary(choice);

            if (ContainsAny(choice.ResultTarget, "코스트 회복량"))
            {
                int count = ModifyPartyCostRecovery(context, amount);
                return $"파티 마나 회복량 {amount:+#;-#;0} 적용 ({count}명)";
            }

            if (ContainsAny(choice.ResultTarget, "최대 코스트"))
            {
                int count = ModifyPartyMaxCost(context, amount);
                return $"파티 최대 마나 {amount:+#;-#;0} 적용 ({count}명)";
            }

            return BuildResultSummary(choice);
        }

        private static string ApplyHeal(EventData choice, EventChoiceExecutionContext context)
        {
            int amount = TryParseEffectAmount(choice.ResultValue, out int parsed)
                ? Mathf.Max(0, parsed)
                : DefaultRestHealAmount;

            int count = ModifyPartyCurrentHp(context, amount);
            return $"파티 전원 체력 {amount} 회복 ({count}명)";
        }

        private static string ApplyAccumulate(EventData choice, EventChoiceExecutionContext context)
        {
            int amount = ResolveRemnantAmount(choice.ResultValue, SmallRemnantAmount);
            context.SessionState.AccumulatedRemnant += amount;
            return $"레드 더스티움 {amount} 누적 (현재 {context.SessionState.AccumulatedRemnant})";
        }

        private static string ApplyCommitAccumulated(EventChoiceExecutionContext context)
        {
            int amount = Mathf.Max(0, context.SessionState.AccumulatedRemnant);

            if (amount <= 0)
                return "확정할 누적 보상이 없습니다.";

            string message = GrantRemnantReward(context, amount);
            context.SessionState.AccumulatedRemnant = 0;
            return message;
        }

        private static string ApplyFailureResult(
            string failResult,
            EventChoiceExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(failResult))
                return "실패했습니다.";

            string effectText = TrimRangePrefix(failResult);

            if (ContainsAny(effectText, "누적") &&
                ContainsAny(effectText, "소실", "잃"))
            {
                context.SessionState.AccumulatedRemnant = 0;
            }

            if (ContainsAny(effectText, "앞서 획득한", "레드 더스티움") &&
                ContainsAny(effectText, "잃"))
            {
                int amount = Mathf.Max(0, context.SessionState.LastImmediateRemnant);
                if (amount > 0 && context.RevokeRemnant != null)
                {
                    context.RevokeRemnant(amount);
                    context.SessionState.LastImmediateRemnant = 0;
                }
                else if (amount > 0 && context.BattleRuntime != null)
                {
                    context.BattleRuntime.Remnant = Mathf.Max(0, context.BattleRuntime.Remnant - amount);
                    context.SessionState.LastImmediateRemnant = 0;
                    context.RefreshRemnantHud?.Invoke();
                }
            }

            if (ContainsAny(effectText, "현재 체력"))
            {
                int amount = TryParseEffectAmount(effectText, out int parsed)
                    ? parsed
                    : DefaultFailureHpDamage;
                ModifyPartyCurrentHp(context, amount);
            }

            if (ContainsAny(effectText, "최대 코스트") &&
                TryParseEffectAmount(effectText, out int maxCostAmount))
            {
                ModifyPartyMaxCost(context, maxCostAmount);
            }

            return effectText.Trim();
        }

        private static int ModifyPartyCurrentHp(EventChoiceExecutionContext context, int amount)
        {
            int count = 0;

            if (context.PartyCharacters == null)
                return count;

            for (int i = 0; i < context.PartyCharacters.Count; i++)
            {
                CharacterRuntimeData character = context.PartyCharacters[i];
                if (character == null)
                    continue;

                character.CurrentHP = Mathf.Clamp(character.CurrentHP + amount, 0, Mathf.Max(0, character.MaxHP));
                count++;
            }

            return count;
        }

        private static int ModifyPartyMaxHp(EventChoiceExecutionContext context, int amount)
        {
            int count = 0;

            if (context.PartyCharacters == null)
                return count;

            for (int i = 0; i < context.PartyCharacters.Count; i++)
            {
                CharacterRuntimeData character = context.PartyCharacters[i];
                if (character == null)
                    continue;

                character.RunMaxHPBonus += amount;
                character.MaxHP = Mathf.Max(0, character.MaxHP + amount);
                character.CurrentHP = Mathf.Clamp(character.CurrentHP + Mathf.Max(0, amount), 0, character.MaxHP);
                count++;
            }

            return count;
        }

        private static int ModifyPartyMaxCost(EventChoiceExecutionContext context, int amount)
        {
            int count = 0;

            if (context.PartyCharacters == null)
                return count;

            for (int i = 0; i < context.PartyCharacters.Count; i++)
            {
                CharacterRuntimeData character = context.PartyCharacters[i];
                if (character == null)
                    continue;

                character.RunMaxCostBonus += amount;
                character.MaxCost = Mathf.Max(0, character.MaxCost + amount);
                character.CurrentCost = Mathf.Clamp(character.CurrentCost, 0, character.MaxCost);
                count++;
            }

            return count;
        }

        private static int ModifyPartyCostRecovery(EventChoiceExecutionContext context, int amount)
        {
            int count = 0;

            if (context.PartyCharacters == null)
                return count;

            for (int i = 0; i < context.PartyCharacters.Count; i++)
            {
                CharacterRuntimeData character = context.PartyCharacters[i];
                if (character == null)
                    continue;

                character.BonusCostRecovery += amount;
                count++;
            }

            return count;
        }

        private static void AddRemnant(EventChoiceExecutionContext context, int amount)
        {
            if (context.BattleRuntime == null)
                return;

            context.BattleRuntime.Remnant = Mathf.Max(0, context.BattleRuntime.Remnant + amount);
            context.RefreshRemnantHud?.Invoke();
        }

        private static string GrantRemnantReward(EventChoiceExecutionContext context, int amount)
        {
            amount = Mathf.Max(0, amount);

            if (amount <= 0)
                return string.Empty;

            if (context.GrantRemnant != null)
            {
                bool granted = context.GrantRemnant(amount, out string resultMessage);

                if (!granted)
                {
                    return string.IsNullOrWhiteSpace(resultMessage)
                        ? "획득 가능한 레드 더스티움이 없습니다."
                        : resultMessage;
                }

                if (context.SuppressRewardResultMessages)
                    return string.Empty;

                return string.IsNullOrWhiteSpace(resultMessage)
                    ? $"레드 더스티움 {amount} 획득"
                    : resultMessage;
            }

            AddRemnant(context, amount);
            return $"레드 더스티움 {amount} 획득";
        }

        private static bool HasAnyOwnedRelic(EventChoiceExecutionContext context)
        {
            if (context.BattleRuntime?.OwnedRelicIds != null)
            {
                for (int i = 0; i < context.BattleRuntime.OwnedRelicIds.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(context.BattleRuntime.OwnedRelicIds[i]))
                        return true;
                }
            }

            if (context.PartyCharacters == null)
                return false;

            for (int i = 0; i < context.PartyCharacters.Count; i++)
            {
                CharacterRuntimeData character = context.PartyCharacters[i];
                if (character?.EquippedRelicIds == null)
                    continue;

                for (int slot = 0; slot < character.EquippedRelicIds.Length; slot++)
                {
                    if (!string.IsNullOrWhiteSpace(character.EquippedRelicIds[slot]))
                        return true;
                }
            }

            return false;
        }

        private static bool HasAnyEquippedRelic(EventChoiceExecutionContext context)
        {
            if (context.PartyCharacters == null)
                return false;

            for (int i = 0; i < context.PartyCharacters.Count; i++)
            {
                CharacterRuntimeData character = context.PartyCharacters[i];
                if (character?.EquippedRelicIds == null)
                    continue;

                for (int slot = 0; slot < character.EquippedRelicIds.Length; slot++)
                {
                    if (!string.IsNullOrWhiteSpace(character.EquippedRelicIds[slot]))
                        return true;
                }
            }

            return false;
        }

        private static bool HasAnyOwnedSkill(EventChoiceExecutionContext context)
        {
            if (context.BattleRuntime?.SkillInventoryIds != null)
            {
                for (int i = 0; i < context.BattleRuntime.SkillInventoryIds.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(context.BattleRuntime.SkillInventoryIds[i]))
                        return true;
                }
            }

            if (context.PartyCharacters == null)
                return false;

            for (int i = 0; i < context.PartyCharacters.Count; i++)
            {
                CharacterRuntimeData character = context.PartyCharacters[i];
                if (character?.EquippedSkillIds == null)
                    continue;

                for (int slot = 0; slot < character.EquippedSkillIds.Length; slot++)
                {
                    if (!string.IsNullOrWhiteSpace(character.EquippedSkillIds[slot]))
                        return true;
                }
            }

            return false;
        }

        private static bool HasAnyUpgradeableEquippedSkill(EventChoiceExecutionContext context)
        {
            if (context.HasUpgradeableEquippedSkill != null)
                return context.HasUpgradeableEquippedSkill();

            if (context.PartyCharacters == null)
                return false;

            for (int i = 0; i < context.PartyCharacters.Count; i++)
            {
                CharacterRuntimeData character = context.PartyCharacters[i];
                if (character == null)
                    continue;

                if (HasUpgradeableSkillId(character.PassiveSkillId) ||
                    HasUpgradeableSkillId(character.UniqueSkillId) ||
                    HasUpgradeableSkillId(character.AbilitySkillId))
                {
                    return true;
                }

                if (character.EquippedSkillIds == null)
                    continue;

                for (int slot = 0; slot < character.EquippedSkillIds.Length; slot++)
                {
                    if (HasUpgradeableSkillId(character.EquippedSkillIds[slot]))
                        return true;
                }
            }

            return false;
        }

        private static bool HasUpgradeableSkillId(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                return false;

            string normalizedSkillId = skillId.Trim();
            return !SkillRarityUtility.IsUpgradeSkillVariant(normalizedSkillId) &&
                   SkillRarityUtility.TryGetPairedVariantId(normalizedSkillId, out string pairedSkillId) &&
                   !string.IsNullOrWhiteSpace(pairedSkillId);
        }

        private static bool IsSupportedTypedSkillRewardOffer(EventData choice)
        {
            return TryResolveSkillRewardOffer(choice, out _);
        }

        private static bool TryResolveSkillRewardOffer(
            EventData choice,
            out EventChoiceSkillRewardFilter filter)
        {
            filter = EventChoiceSkillRewardFilter.Attack;

            if (choice == null ||
                !ContainsAny(choice.ResultTarget, "기억") ||
                (!IsToken(choice.ResultType, "OfferChoice") &&
                 !IsToken(choice.ResultType, "SelectReward") &&
                 !IsToken(choice.ChoiceType, "SelectReward")))
            {
                return false;
            }

            if (ContainsAny(choice.ResultTarget, "일반~레어", "일반-레어", "Common~Rare", "Common-Rare"))
            {
                filter = EventChoiceSkillRewardFilter.CommonToRare;
                return true;
            }

            if (ContainsAny(choice.ResultTarget, "에픽", "Epic"))
            {
                filter = EventChoiceSkillRewardFilter.Epic;
                return true;
            }

            if (ContainsAny(choice.ResultTarget, "공격", "Attack"))
            {
                filter = EventChoiceSkillRewardFilter.Attack;
                return true;
            }

            if (ContainsAny(choice.ResultTarget, "디버프", "Debuff"))
            {
                filter = EventChoiceSkillRewardFilter.Debuff;
                return true;
            }

            if (ContainsAny(choice.ResultTarget, "버프", "Buff"))
            {
                filter = EventChoiceSkillRewardFilter.Buff;
                return true;
            }

            return false;
        }

        private static int ResolveOfferChoiceCount(string value, int fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Mathf.Max(0, fallback);

            Match match = Regex.Match(value, @"\d+");
            return match.Success && int.TryParse(match.Value, out int parsed)
                ? Mathf.Max(0, parsed)
                : Mathf.Max(0, fallback);
        }

        private static string TryInvokeGrant(EventChoiceRewardGrant grant, string fallback)
        {
            if (grant == null)
                return fallback;

            return grant(out string resultMessage)
                ? resultMessage
                : string.IsNullOrWhiteSpace(resultMessage) ? fallback : resultMessage;
        }

        private static string TryInvokeRewardGrant(
            EventChoiceExecutionContext context,
            EventChoiceRewardGrant grant,
            string fallback)
        {
            if (grant == null)
                return fallback;

            bool granted = grant(out string resultMessage);

            if (!granted)
                return string.IsNullOrWhiteSpace(resultMessage) ? fallback : resultMessage;

            return context.SuppressRewardResultMessages ? string.Empty : resultMessage;
        }

        private static string TryInvokeFilteredSkillRewardGrant(
            EventChoiceExecutionContext context,
            EventChoiceFilteredSkillRewardGrant grant,
            EventChoiceSkillRewardFilter filter,
            int count,
            string fallback)
        {
            if (grant == null)
                return fallback;

            bool granted = grant(filter, count, out string resultMessage);

            if (!granted)
                return string.IsNullOrWhiteSpace(resultMessage) ? fallback : resultMessage;

            return context.SuppressRewardResultMessages ? string.Empty : resultMessage;
        }

        private static int RollThreeDice(EventChoiceExecutionContext context)
        {
            if (context.RollThreeDice != null)
                return context.RollThreeDice();

            return BattleRandom.Range(1, 7) +
                   BattleRandom.Range(1, 7) +
                   BattleRandom.Range(1, 7);
        }

        private static IReadOnlyList<int> RollDiceFaces(EventChoiceExecutionContext context)
        {
            if (context.RollDiceFaces != null)
            {
                int[] provided = context.RollDiceFaces();
                return NormalizeDiceFaces(provided);
            }

            if (context.RollThreeDice != null)
                return SplitTotalIntoDiceFaces(context.RollThreeDice());

            return new[]
            {
                BattleRandom.Range(1, 7),
                BattleRandom.Range(1, 7),
                BattleRandom.Range(1, 7)
            };
        }

        private static int[] NormalizeDiceFaces(IReadOnlyList<int> diceFaces)
        {
            int count = Mathf.Max(3, diceFaces?.Count ?? 0);
            int[] normalized = new int[count];

            for (int i = 0; i < normalized.Length; i++)
            {
                int value = diceFaces != null && i < diceFaces.Count
                    ? diceFaces[i]
                    : BattleRandom.Range(1, 7);
                normalized[i] = Mathf.Clamp(value, 1, 6);
            }

            return normalized;
        }

        private static int[] SplitTotalIntoDiceFaces(int total)
        {
            total = Mathf.Clamp(total, 3, 18);
            int[] faces = { 1, 1, 1 };
            int remaining = total - 3;

            for (int i = 0; i < faces.Length && remaining > 0; i++)
            {
                int add = Mathf.Min(5, remaining);
                faces[i] += add;
                remaining -= add;
            }

            return faces;
        }

        private static int SumDiceFaces(IReadOnlyList<int> diceFaces)
        {
            if (diceFaces == null || diceFaces.Count == 0)
                return 0;

            int total = 0;
            for (int i = 0; i < diceFaces.Count; i++)
                total += diceFaces[i];
            return total;
        }

        private static bool RollChance(string successRate, EventChoiceExecutionContext context)
        {
            if (!TryParsePercentage(successRate, out float rate))
                rate = 1f;

            float value = context.RollChanceValue != null
                ? context.RollChanceValue()
                : BattleRandom.Value();

            return value <= rate;
        }

        private static bool IsDiceSuccess(int diceRoll, string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return true;

            string[] ranges = condition.Split(new[] { ',', '/' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < ranges.Length; i++)
            {
                if (TryParseRange(ranges[i], out int min, out int max) &&
                    diceRoll >= min &&
                    diceRoll <= max)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseRange(string text, out int min, out int max)
        {
            min = 0;
            max = 0;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            string normalized = text.Trim().Replace("~", "-");
            string[] parts = normalized.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1 && int.TryParse(parts[0].Trim(), out int single))
            {
                min = single;
                max = single;
                return true;
            }

            if (parts.Length != 2)
                return false;

            if (!int.TryParse(parts[0].Trim(), out min) ||
                !int.TryParse(parts[1].Trim(), out max))
            {
                return false;
            }

            if (min > max)
                (min, max) = (max, min);

            return true;
        }

        private static bool TryParsePercentage(string value, out float rate)
        {
            rate = 0f;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            string normalized = value.Trim().Replace("%", string.Empty);

            if (!float.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsed))
                return false;

            rate = Mathf.Clamp01(parsed > 1f ? parsed / 100f : parsed);
            return true;
        }

        private static int ResolveRemnantAmount(string value, int fallback)
        {
            if (TryParseEffectAmount(value, out int amount))
                return Mathf.Max(0, amount);

            if (ContainsAny(value, "대량"))
                return LargeRemnantAmount;

            if (ContainsAny(value, "중간"))
                return MediumRemnantAmount;

            if (ContainsAny(value, "소량"))
                return SmallRemnantAmount;

            return fallback;
        }

        private static bool TryParseEffectAmount(string value, out int amount)
        {
            amount = 0;

            if (string.IsNullOrWhiteSpace(value) || ContainsAny(value, "TBD"))
                return false;

            string effectText = TrimRangePrefix(value);
            Match signedMatch = Regex.Match(effectText, @"[+-]\s*\d+");
            if (signedMatch.Success &&
                int.TryParse(signedMatch.Value.Replace(" ", string.Empty), out amount))
            {
                return true;
            }

            MatchCollection matches = Regex.Matches(effectText, @"\d+");
            if (matches.Count == 0)
                return false;

            if (!int.TryParse(matches[matches.Count - 1].Value, out amount))
                return false;

            if (ContainsAny(effectText, "감소", "소실", "잃", "decrease", "lose"))
                amount = -amount;

            return true;
        }

        private static string TrimRangePrefix(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            int colonIndex = value.IndexOf(':');
            return colonIndex >= 0 && colonIndex < value.Length - 1
                ? value.Substring(colonIndex + 1).Trim()
                : value.Trim();
        }

        private static bool IsRedDustiumCost(EventData choice)
        {
            return choice != null &&
                   ContainsAny(choice.CostType, "레드 더스티움") &&
                   ContainsAny(choice.CostTarget, "파티");
        }

        private static string BuildResultSummary(EventData choice)
        {
            if (choice == null)
                return string.Empty;

            List<string> parts = new();

            if (!string.IsNullOrWhiteSpace(choice.ResultType))
                parts.Add(choice.ResultType.Trim());

            if (!string.IsNullOrWhiteSpace(choice.ResultTarget))
                parts.Add(choice.ResultTarget.Trim());

            if (!string.IsNullOrWhiteSpace(choice.ResultValue))
                parts.Add(choice.ResultValue.Trim());

            return parts.Count > 0 ? string.Join(" / ", parts) : string.Empty;
        }

        private static string JoinMessages(List<string> messages)
        {
            if (messages == null || messages.Count == 0)
                return string.Empty;

            List<string> nonEmpty = new();
            for (int i = 0; i < messages.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(messages[i]))
                    nonEmpty.Add(messages[i].Trim());
            }

            return string.Join("\n", nonEmpty);
        }

        private static bool IsToken(string left, string right)
        {
            return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsAny(string source, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(source) || values == null)
                return false;

            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                if (!string.IsNullOrWhiteSpace(value) &&
                    source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }
    }
}
