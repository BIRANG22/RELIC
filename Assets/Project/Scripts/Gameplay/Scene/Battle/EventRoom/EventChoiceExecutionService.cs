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

    public sealed class EventChoiceSessionState
    {
        public int AccumulatedRemnant;
        public int LastImmediateRemnant;
    }

    public sealed class EventChoiceExecutionContext
    {
        public BattleRuntimeData BattleRuntime;
        public IReadOnlyList<CharacterRuntimeData> PartyCharacters;
        public EventChoiceSessionState SessionState;
        public Func<int> RollThreeDice;
        public Func<float> RollChanceValue;
        public EventChoiceRewardGrant GrantRandomRelic;
        public EventChoiceRewardGrant GrantRandomSkill;
        public EventChoiceRewardGrant UpgradeRandomSkill;
        public EventChoiceRemnantRewardGrant GrantRemnant;
        public EventChoiceRemnantRewardRevoke RevokeRemnant;
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
            string visualActionId = "")
        {
            Accepted = accepted;
            ResultMessage = resultMessage ?? string.Empty;
            NextEventId = EventIdUtility.Normalize(nextEventId);
            VisualObjectId = NormalizeId(visualObjectId);
            VisualActionId = NormalizeId(visualActionId);
        }

        public bool Accepted { get; }
        public string ResultMessage { get; }
        public string NextEventId { get; }
        public bool HasNextEvent => !string.IsNullOrWhiteSpace(NextEventId);
        public string VisualObjectId { get; }
        public string VisualActionId { get; }
        public bool HasVisualAction =>
            !string.IsNullOrWhiteSpace(VisualObjectId) &&
            !string.IsNullOrWhiteSpace(VisualActionId);

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }
    }

    public static class EventChoiceExecutionService
    {
        public const int DefaultImmediateRemnantAmount = 100;
        public const int DefaultTradeRemnantCost = 100;
        public const int DefaultRestHealAmount = 10;
        public const int DefaultFailureHpDamage = -5;
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

            if (RequiresTargetSelection(choice))
            {
                unavailableReason = "대상 선택 UI가 필요합니다.";
                return false;
            }

            if (IsToken(choice.ResultType, "OfferChoice") ||
                IsToken(choice.ChoiceType, "SelectReward"))
            {
                unavailableReason = "보상 선택 UI가 필요합니다.";
                return false;
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

            if (ContainsAny(choice.SelectCondition, "유물 1개 이상 보유") &&
                !HasAnyOwnedRelic(context))
            {
                unavailableReason = "보유한 유물이 없습니다.";
                return false;
            }

            if (ContainsAny(choice.SelectCondition, "기억 1개 이상 보유", "미각성 기억 1개 이상") &&
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

            List<string> messages = new();

            if (!string.IsNullOrWhiteSpace(choice.ChoiceDesc))
                messages.Add(choice.ChoiceDesc.Trim());

            ApplyCost(choice, context, messages);

            int diceRoll = 0;
            bool success = true;

            if (IsToken(choice.ChoiceType, "Dice"))
            {
                diceRoll = RollThreeDice(context);
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

                return new EventChoiceExecutionResult(
                    true,
                    JoinMessages(messages),
                    choice.NextEventId,
                    choice.FailureVisualObjectId,
                    choice.FailureVisualActionId);
            }

            string result = ApplySuccessResult(choice, diceRoll, context);
            if (!string.IsNullOrWhiteSpace(result))
                messages.Add(result);

            return new EventChoiceExecutionResult(
                true,
                JoinMessages(messages),
                choice.NextEventId,
                choice.SuccessVisualObjectId,
                choice.SuccessVisualActionId);
        }

        private static EventChoiceExecutionContext NormalizeContext(EventChoiceExecutionContext context)
        {
            context ??= new EventChoiceExecutionContext();
            context.SessionState ??= new EventChoiceSessionState();
            return context;
        }

        private static bool RequiresTargetSelection(EventData choice)
        {
            if (choice == null)
                return false;

            if (ContainsAny(choice.CostValue, "선택 유물") ||
                ContainsAny(choice.ResultTarget, "선택 기억") ||
                IsToken(choice.ResultType, "Awaken"))
            {
                return true;
            }

            return false;
        }

        private static void ApplyCost(
            EventData choice,
            EventChoiceExecutionContext context,
            List<string> messages)
        {
            if (!IsRedDustiumCost(choice) || context.BattleRuntime == null)
                return;

            int cost = ResolveRemnantAmount(choice.CostValue, DefaultTradeRemnantCost);
            context.BattleRuntime.Remnant = Mathf.Max(0, context.BattleRuntime.Remnant - cost);
            context.RefreshRemnantHud?.Invoke();
            messages.Add($"레드 더스티움 {cost} 지불");
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

            if (IsToken(resultType, "UpgradeRandom"))
                return TryInvokeGrant(context.UpgradeRandomSkill, "강화 가능한 기억이 없습니다.");

            if (IsToken(resultType, "EndEvent"))
                return "이벤트를 종료합니다.";

            return BuildResultSummary(choice);
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

        private static string ApplyModify(EventData choice, EventChoiceExecutionContext context)
        {
            if (!TryParseEffectAmount(choice.ResultValue, out int amount))
                return BuildResultSummary(choice);

            if (ContainsAny(choice.ResultTarget, "코스트 회복량"))
            {
                int count = ModifyPartyCostRecovery(context, amount);
                return $"파티 코스트 회복량 {amount:+#;-#;0} 적용 ({count}명)";
            }

            if (ContainsAny(choice.ResultTarget, "최대 코스트"))
            {
                int count = ModifyPartyMaxCost(context, amount);
                return $"파티 최대 코스트 {amount:+#;-#;0} 적용 ({count}명)";
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

        private static int RollThreeDice(EventChoiceExecutionContext context)
        {
            if (context.RollThreeDice != null)
                return context.RollThreeDice();

            return BattleRandom.Range(1, 7) +
                   BattleRandom.Range(1, 7) +
                   BattleRandom.Range(1, 7);
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
