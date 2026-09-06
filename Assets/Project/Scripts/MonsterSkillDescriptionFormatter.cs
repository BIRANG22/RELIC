using System;

namespace Relic.Gameplay.Data
{
    /// <summary>
    /// Formats MonsterSkill EffectDesc placeholders from ValueRate / CountRate.
    /// Supports {ValueRate1}, {ValueRate2}, {CountRate1} and the legacy "¼öÄ¡" token.
    /// </summary>
    public static class MonsterSkillDescriptionFormatter
    {
        private const string LegacyValueToken = "\uC218\uCE58";

        public static string Format(string description, MonsterSkillData skillData)
        {
            if (string.IsNullOrWhiteSpace(description))
                return string.Empty;

            if (skillData == null)
                return description.Trim();

            string result = description.Trim();
            string[] valueRates = SplitValues(skillData.ValueRate);
            string[] countRates = SplitValues(skillData.CountRate);

            for (int i = 0; i < valueRates.Length; i++)
            {
                string token = $"{{ValueRate{i + 1}}}";
                string value = GetValueRateDisplay(valueRates[i]);
                result = ReplaceToken(result, token, value);
            }

            for (int i = 0; i < countRates.Length; i++)
            {
                string token = $"{{CountRate{i + 1}}}";
                result = ReplaceToken(result, token, countRates[i].Trim());
            }

            // Backward compatibility for older MonsterSkill descriptions.
            string legacyValue = valueRates.Length > 0
                ? GetValueRateDisplay(valueRates[0])
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(legacyValue))
                result = ReplaceToken(result, LegacyValueToken, legacyValue);

            return result;
        }

        private static string[] SplitValues(string values)
        {
            return string.IsNullOrWhiteSpace(values)
                ? Array.Empty<string>()
                : values.Split(';');
        }

        private static string GetValueRateDisplay(string value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static string ReplaceToken(string source, string token, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(token) || !source.Contains(token))
                return source;

            return source
                .Replace($"\"{token}\"", value ?? string.Empty)
                .Replace(token, value ?? string.Empty);
        }
    }
}
