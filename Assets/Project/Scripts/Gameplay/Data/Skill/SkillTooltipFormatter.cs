using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Relic.Gameplay.Data
{
    public static class SkillTooltipFormatter
    {
        private const string FocusEffectId = "E_Focus";
        private const string PowerEffectId = "E_Power";

        private static readonly Regex FocusCostFormulaRegex = new(
            "\\{\\s*\\(\\s*(?<base>-?\\d+)\\s*\\+\\s*\uC9D1\uC911\\s*\\)\\s*[xX\u00D7]\\s*\uC18C\uBAA8\uB7C9\\s*(?<power>\\+\\s*\uD798)?\\s*\\}",
            RegexOptions.Compiled);

        public static string Format(
            SkillMasterData skill,
            string text,
            CharacterRuntimeData runtime,
            int payAmount)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            _ = skill;

            return FocusCostFormulaRegex.Replace(
                text,
                match => CalculateFocusCostFormula(runtime, payAmount, match).ToString()
            );
        }

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
