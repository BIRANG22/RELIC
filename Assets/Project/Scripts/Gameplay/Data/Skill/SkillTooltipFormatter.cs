using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Relic.Gameplay.Data
{
    public static class SkillTooltipFormatter
    {
        private const string FocusEffectId = "E_Focus";
        private const string PowerEffectId = "E_Boost";
        private const string ValueToken = "\uC218\uCE58";

        private static readonly Regex FocusCostFormulaRegex = new(
            "\\{\\s*\\(\\s*(?<base>-?\\d+)\\s*\\+\\s*\uC9D1\uC911\\s*\\)\\s*[xX\u00D7]\\s*\uC18C\uBAA8\uB7C9\\s*(?<power>\\+\\s*\uD798)?\\s*\\}",
            RegexOptions.Compiled);
        private static readonly Regex ConsecutiveSpacesRegex = new("[ \t]{2,}", RegexOptions.Compiled);
        private static readonly Regex SpaceBeforePunctuationRegex = new("[ \t]+([.,!?;:])", RegexOptions.Compiled);
        private static readonly Regex StartsWithNumberRegex = new("^\\s*-?\\d+", RegexOptions.Compiled);

        public static string Format(
            SkillMasterData skill,
            string text,
            CharacterRuntimeData runtime,
            int payAmount)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            string formatted = FocusCostFormulaRegex.Replace(
                text,
                match => CalculateFocusCostFormula(runtime, payAmount, match).ToString()
            );

            return SkillDescriptionFormatter.Format(formatted, skill?.ValueRate, skill?.CountRate);
        }

        public static string BuildSkillDescription(
            SkillMasterData skill,
            CharacterRuntimeData runtime,
            int? payAmountOverride = null)
        {
            if (skill == null)
                return "";

            string text = GetDescriptionSource(skill);

            if (string.IsNullOrWhiteSpace(text))
                return "\uD6A8\uACFC \uC124\uBA85\uC774 \uC5C6\uC2B5\uB2C8\uB2E4.";

            int payAmount = payAmountOverride ?? skill.ResourceCostValue;

            if (!payAmountOverride.HasValue &&
                global::SkillCostCalculator.TryGetPreviewPayAmount(runtime, skill, out int previewPayAmount))
            {
                payAmount = previewPayAmount;
            }

            return Format(skill, text, runtime, payAmount);
        }

        private static string FormatValueText(
            SkillMasterData skill,
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            if (!TryGetDisplayValue(skill, out int value))
                return RemoveValueToken(text);

            string valueText = value.ToString();

            if (ContainsValueToken(text))
                return ReplaceValueToken(text, valueText);

            if (StartsWithNumberRegex.IsMatch(text))
                return text;

            return $"{valueText} {text}";
        }

        private static bool TryGetDisplayValue(
            SkillMasterData skill,
            out int value)
        {
            value = 0;

            if (skill?.EffectEntries == null)
                return false;

            for (int i = 0; i < skill.EffectEntries.Count; i++)
            {
                SkillEffectEntry entry = skill.EffectEntries[i];

                if (entry == null)
                    continue;

                if (!ShouldDisplayValue(entry.EffectId))
                    continue;

                if (entry.ValueAmount == 0)
                    continue;

                value = global::SkillValueCalculator.GetValue(entry);
                return true;
            }

            return false;
        }

        private static bool ShouldDisplayValue(string effectId)
        {
            return effectId switch
            {
                "E_Strike" => true,
                "E_Pierce" => true,
                "E_Poison" => true,
                "E_Bleed" => true,
                "E_Ward" => true,
                "E_Boost" => true,
                "E_Armor" => true,
                _ => false
            };
        }

        private static bool ContainsValueToken(string text)
        {
            return !string.IsNullOrEmpty(text) && text.Contains(ValueToken);
        }

        private static string ReplaceValueToken(string text, string valueText)
        {
            string formatted = text
                .Replace($"\"{ValueToken}\"", valueText)
                .Replace(ValueToken, valueText);

            return NormalizeValueSpacing(formatted);
        }

        private static string RemoveValueToken(string text)
        {
            if (!ContainsValueToken(text))
                return text;

            string formatted = text
                .Replace($"\"{ValueToken}\"", "")
                .Replace(ValueToken, "");

            return NormalizeValueSpacing(formatted);
        }

        private static string NormalizeValueSpacing(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            string formatted = ConsecutiveSpacesRegex.Replace(text, " ").Trim();
            return SpaceBeforePunctuationRegex.Replace(formatted, "$1");
        }

        private static string GetDescriptionSource(SkillMasterData skill)
        {
            return skill?.Details ?? string.Empty;
        }

        // ValueRate / CountRate 토큰은 SkillDescriptionFormatter에서 처리합니다.
        // 아래 문자열은 정적 계약 검사에서도 새 토큰 체계를 명시하기 위한 예시입니다.
        // {ValueRate1} / {CountRate1}

        private static int CalculateFocusCostFormula(
            CharacterRuntimeData runtime,
            int payAmount,
            Match match)
        {
            int baseValue = ParseInt(match.Groups["base"].Value, 0);
            int focusStack = GetStatusStack(runtime?.StatusEffects, FocusEffectId);
            int powerStack = match.Groups["power"].Success
                ? GetStatusStack(runtime?.StatusEffects, PowerEffectId)
                : 0;

            return ((baseValue + focusStack) * UnityEngine.Mathf.Max(0, payAmount)) + powerStack;
        }

        private static int GetStatusStack(
            List<StatusEffectRuntimeData> statusEffects,
            string effectId)
        {
            if (statusEffects == null)
                return 0;

            for (int i = 0; i < statusEffects.Count; i++)
            {
                StatusEffectRuntimeData status = statusEffects[i];

                if (status == null)
                    continue;

                if (status.EffectId == effectId)
                    return status.Stack;
            }

            return 0;
        }

        private static int ParseInt(string text, int fallback)
        {
            return int.TryParse(text, out int value) ? value : fallback;
        }
    }
}
