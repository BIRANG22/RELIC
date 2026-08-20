using System;
using System.Globalization;

namespace Relic.Gameplay.Data
{
    /// <summary>
    /// SkillMaster.Details의 {ValueRate1}, {ValueRate2}, {CountRate1} 토큰을
    /// SkillMaster 데이터의 실제 수치로 치환합니다.
    /// </summary>
    public static class SkillDescriptionFormatter
    {
        public static string Format(SkillMasterData data)
        {
            if (data == null)
                return string.Empty;

            return Format(data.Details, data.ValueRate, data.CountRate);
        }

        public static string Format(string description, string valueRate, string countRate)
        {
            if (string.IsNullOrWhiteSpace(description))
                return string.Empty;

            string result = description;
            result = ReplaceIndexed(result, "ValueRate", valueRate);
            result = ReplaceIndexed(result, "CountRate", countRate);

            // 이전 단일 토큰 표기도 호환합니다.
            result = ReplaceSingle(result, "{ValueRate}", valueRate);
            result = ReplaceSingle(result, "{CountRate}", countRate);
            return result;
        }

        private static string ReplaceIndexed(string source, string tokenName, string values)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrWhiteSpace(tokenName))
                return source;

            string[] splitValues = string.IsNullOrWhiteSpace(values)
                ? Array.Empty<string>()
                : values.Split(';');

            for (int i = 0; i < splitValues.Length; i++)
            {
                string token = $"{{{tokenName}{i + 1}}}";
                if (!source.Contains(token))
                    continue;

                source = source.Replace(token, GetDisplayValue(splitValues[i]));
            }

            return source;
        }

        private static string ReplaceSingle(string source, string token, string values)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(token) || !source.Contains(token))
                return source;

            string first = string.IsNullOrWhiteSpace(values)
                ? string.Empty
                : values.Split(';')[0];

            return source.Replace(token, GetDisplayValue(first));
        }

        public static string GetDisplayValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "?";

            string trimmed = value.Trim();
            if (trimmed.Length > 1 &&
                trimmed[0] == '-' &&
                float.TryParse(trimmed.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                return trimmed.Substring(1);
            }

            return trimmed;
        }
    }
}
