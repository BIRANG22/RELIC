using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Relic.Gameplay.Data
{
    /// <summary>
    /// SkillMaster.Details의 ValueRate/CountRate/ScalingValue 토큰을 표시용 값으로 치환합니다.
    /// ScalingValue는 전투 외 UI에서 계산식의 의미(예: 잃은 체력의 50%)를 표시합니다.
    /// </summary>
    public static class SkillDescriptionFormatter
    {
        private static readonly Regex EffectTokenRegex = new(@"\{(?<id>E_[A-Za-z0-9_]+)\}", RegexOptions.Compiled);
        private static readonly Regex EffectLinkRegex = new(
            @"<link\s*=\s*""effecticon:(?<id>E_[A-Za-z0-9_]+)""[^>]*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex TmpTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);


        public static string Format(SkillMasterData data)
        {
            if (data == null)
                return string.Empty;

            return Format(data.Details, data.ValueRate, data.CountRate, data.ScalingType, false, data.EffectIds, data.ResourceCostValue);
        }

        public static string Format(string description, string valueRate, string countRate)
        {
            return Format(description, valueRate, countRate, null);
        }

        public static string Format(string description, string valueRate, string countRate, string scalingType)
        {
            return Format(description, valueRate, countRate, scalingType, false, null);
        }

        public static string Format(string description, string valueRate, string countRate,
            string scalingType, bool runtimeValues)
        {
            return Format(description, valueRate, countRate, scalingType, runtimeValues, null);
        }

        public static string Format(string description, string valueRate, string countRate,
            string scalingType, bool runtimeValues, string effectIds, int? resourceCostValue = null)
        {
            if (string.IsNullOrWhiteSpace(description))
                return string.Empty;

            // SkillMaster resource-cost placeholders are authored independently of effect values.
            if (resourceCostValue.HasValue)
                description = description.Replace("{ResourceCostValue}",
                    resourceCostValue.Value.ToString(CultureInfo.InvariantCulture));

            // Add the repetition to the matching clause rather than the whole skill.
            // This also supports independently repeated effects (e.g. Strike;Vulnerable).
            description = AddRepeatCountToDescription(description, countRate);

            // During combat the preview passes the resolved (buff/debuff-adjusted)
            // effect value, not the original missing-HP percentage. Keep the authored
            // percentage in the codex, but show a numeric damage value in battle.
            if (runtimeValues && HasEffect(effectIds, "E_MissingHPStrike"))
            {
                int missingIndex = FindSpecialEffectIndex(effectIds, "E_MissingHPStrike");
                string resolved = GetByIndexOrFirst(Split(valueRate), missingIndex, "0");
                description = description.Replace("{E_MissingHPStrike}", GetDisplayValue(resolved));
            }
            // ArmorStrike uses the caster's current armor. The battle preview has
            // already resolved that value, including 0 armor -> 0 damage.
            // Keep the effect name in the codex; only substitute in combat.
            if (runtimeValues && HasEffect(effectIds, "E_ArmorStrike"))
            {
                int armorIndex = FindSpecialEffectIndex(effectIds, "E_ArmorStrike");
                string resolved = GetByIndexOrFirst(Split(valueRate), armorIndex, "0");
                description = description.Replace("{E_ArmorStrike}", GetDisplayValue(resolved));
            }

            // Use the authored, localized Details sentence as-is. Do not rebuild
            // grammar or Korean object particles from effect IDs.
            string result = description;
            result = ReplaceScalingIndexed(result, scalingType, valueRate);
            result = ReplaceIndexed(result, "ValueRate", valueRate);
            result = ReplaceIndexed(result, "CountRate", countRate);

            // 이전 단일 토큰 표기도 호환을 위해 유지합니다.
            result = ReplaceScalingSingle(result, "{ScalingValue}", scalingType, valueRate);
            result = ReplaceSingle(result, "{ValueRate}", valueRate);
            result = ReplaceSingle(result, "{CountRate}", countRate);
            return ReplaceEffectTokens(result, valueRate);
        }


        private static string AddRepeatCountToDescription(string description, string counts)
        {
            if (string.IsNullOrWhiteSpace(description) || description.Contains("{CountRate"))
                return description;

            string[] countParts = Split(counts);
            if (countParts.Length == 0)
                return description;

            // Split on authored clause boundaries so one count cannot accidentally
            // be attached to another effect's verb. Preserve all original punctuation.
            // Separate complete sentences as well as comma/semicolon clauses.
            // Otherwise a following sentence (e.g. "보유한 버프의 종류만큼...")
            // prevents the repeated hit count from being inserted before "준다".
            // Keep the punctuation and whitespace unchanged in the final output.
            // Also split consecutive effect clauses without a comma, such as
            // "{ValueRate1}의 피해를 주고 {ValueRate2}의 취약을 부여한다."
            // The split is only performed when the next clause starts with an
            // indexed effect value, so unrelated narrative text is untouched.
            string[] clauses = Regex.Split(description,
                @"(?<=[.!?。！？])(?=\s|$)|(?<=[,，;；])|(?<=주고)(?=\s*\{ValueRate\d+\})|(?<=부여하고)(?=\s*\{ValueRate\d+\})|(?<=회복하고)(?=\s*\{ValueRate\d+\})|(?<=획득하고)(?=\s*\{ValueRate\d+\})");
            bool changed = false;
            for (int c = 0; c < clauses.Length; c++)
            {
                string clause = clauses[c];
                MatchCollection rates = Regex.Matches(clause, @"\{ValueRate(?<index>\d+)\}");
                var indices = new System.Collections.Generic.HashSet<int>();
                foreach (Match rate in rates)
                {
                    if (int.TryParse(rate.Groups["index"].Value, out int index) && index >= 1)
                        indices.Add(index - 1);
                }

                // A single-effect sentence may use an effect ID alone (e.g.
                // E_MissingHPStrike) with no explicit ValueRate token.
                if (indices.Count == 0 && countParts.Length == 1)
                    indices.Add(0);
                if (indices.Count == 0)
                    continue;

                int repeat = -1;
                bool ambiguous = false;
                foreach (int index in indices)
                {
                    if (index >= countParts.Length || !TryReadCount(countParts[index], out int next))
                    {
                        ambiguous = true;
                        break;
                    }
                    if (repeat >= 0 && repeat != next)
                    {
                        // Multiple distinct effects share a verb, with different
                        // counts. Never present one of those counts as both.
                        ambiguous = true;
                        break;
                    }
                    repeat = next;
                }
                if (ambiguous || repeat <= 1)
                    continue;

                Match verb = Regex.Match(clause, @"(?<verb>부여한다|부여하고|준다|주고|회복한다|회복하고|획득한다|획득하고)(?<tail>\s*[.!?。！？]?\s*[,，;；]?)$");
                if (!verb.Success)
                    continue;
                // The inline repetition unit must come from localization, not a Korean literal.
                string localizedRepeat = global::GameLocalization.FormatWithFallback(
                    "skill.description.repeat_inline", "{0}회", repeat);
                clauses[c] = clause.Substring(0, verb.Index) + localizedRepeat + " " + verb.Value;
                changed = true;
            }
            if (changed)
                return string.Concat(clauses);

            // For non-Korean descriptions with a single effect, use a localized
            // repetition label instead of attempting to rearrange their grammar.
            if (countParts.Length != 1 || !TryReadCount(countParts[0], out int count) || count <= 1)
                return description;
            string repeatText = global::GameLocalization.FormatWithFallback(
                "skill.description.repeat_count", "({0} times)", count.ToString(CultureInfo.InvariantCulture));
            Match punctuation = Regex.Match(description, @"(?<end>[.!?。！？]\s*)$");
            return punctuation.Success
                ? description.Substring(0, punctuation.Index).TrimEnd() + " " + repeatText + punctuation.Value
                : description.TrimEnd() + " " + repeatText;
        }

        private static bool TryReadCount(string raw, out int count)
        {
            string plain = TmpTagRegex.Replace(raw ?? string.Empty, string.Empty);
            // Numeric preview labels can be wrapped in TMP link/color markup.
            return int.TryParse(plain.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
        }

        private static bool HasEffect(string effectIds, string target)
        {
            return FindSpecialEffectIndex(effectIds, target) >= 0;
        }

        private static int FindSpecialEffectIndex(string effectIds, string target)
        {
            if (string.IsNullOrWhiteSpace(effectIds))
                return -1;
            string[] ids = effectIds.Split(';');
            for (int i = 0; i < ids.Length; i++)
                if (string.Equals(ids[i].Trim(), target, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        /// <summary>
        /// 이펙트 ID 토큰을 로컬라이징된 효과명과 인라인 아이콘으로 변환합니다.
        /// 조사와 문장 순서는 각 언어의 스킬 설명 원문에서 관리합니다.
        /// </summary>
        public static string ReplaceEffectTokens(string source)
        {
            return ReplaceEffectTokens(source, null);
        }

        public static List<string> ExtractLinkedEffectIds(string formattedDescription)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(formattedDescription))
                return result;

            var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in EffectLinkRegex.Matches(formattedDescription))
            {
                string effectId = match.Groups["id"].Value;
                if (!string.IsNullOrWhiteSpace(effectId) && added.Add(effectId))
                    result.Add(effectId);
            }

            return result;
        }

        /// <summary>
        /// EffectId 토큰을 표시용 텍스트로 변환합니다.
        /// E_MissingHPStrike는 상태효과명이 아니라 계산식 표현이므로 ValueRate를 이용해
        /// 언어별 "잃은 체력의 {0}%" 형태로 표시하고 별도 아이콘은 붙이지 않습니다.
        /// </summary>
        public static string ReplaceEffectTokens(string source, string valueRate)
        {
            if (string.IsNullOrWhiteSpace(source) || !source.Contains("{E_"))
                return source ?? string.Empty;

            string missingHpRate = GetByIndexOrFirst(Split(valueRate), 0, "0");

            return EffectTokenRegex.Replace(source, match =>
            {
                string effectId = match.Groups["id"].Value;
                if (string.IsNullOrWhiteSpace(effectId))
                    return match.Value;

                if (effectId.Equals("E_MissingHPStrike", StringComparison.OrdinalIgnoreCase))
                {
                    // Prefer the localized Effect sheet's authored rate template.
                    DataManager db = DataManager.Instance;
                    if (db?.EffectDatabase != null &&
                        db.EffectDatabase.TryGet(effectId, out EffectMasterData missingEffect) &&
                        missingEffect != null)
                    {
                        string effectName = GameDataLocalization.EffectName(missingEffect);
                        if (!string.IsNullOrWhiteSpace(effectName) && effectName.Contains("{ValueRate}"))
                            return effectName.Replace("{ValueRate}", GetDisplayValue(missingHpRate));
                    }
                    return global::GameLocalization.FormatWithFallback(
                        "skill.effect.missing_hp_strike.value_format",
                        "잃은 체력의 {0}%",
                        GetDisplayValue(missingHpRate));
                }

                string displayName = effectId;
                DataManager dataManager = DataManager.Instance;
                if (dataManager?.EffectDatabase != null &&
                    dataManager.EffectDatabase.TryGet(effectId, out EffectMasterData effect) &&
                    effect != null)
                {
                    string localizedName = GameDataLocalization.EffectName(effect);
                    if (!string.IsNullOrWhiteSpace(localizedName))
                        displayName = localizedName;
                }

                // Basic damage does not provide a tooltip. Keep its name as plain text.
                if (effectId.Equals("E_Strike", StringComparison.OrdinalIgnoreCase))
                    return displayName;

                string safeId = effectId.Replace("\"", string.Empty);
                return $"<link=\"effecticon:{safeId}\">{displayName}</link><space=1em>";
            });
        }

        private static string ReplaceScalingIndexed(string source, string scalingTypes, string values)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            string[] typeArray = Split(scalingTypes);
            string[] valueArray = Split(values);
            int count = Math.Max(typeArray.Length, valueArray.Length);

            for (int i = 0; i < count; i++)
            {
                string token = $"{{ScalingValue{i + 1}}}";
                if (!source.Contains(token))
                    continue;

                string type = GetByIndexOrFirst(typeArray, i, "None");
                string value = GetByIndexOrFirst(valueArray, i, "0");
                source = source.Replace(token, FormatScalingValue(type, value));
            }

            return source;
        }

        private static string ReplaceScalingSingle(string source, string token, string scalingTypes, string values)
        {
            if (string.IsNullOrEmpty(source) || !source.Contains(token))
                return source;

            string type = GetByIndexOrFirst(Split(scalingTypes), 0, "None");
            string value = GetByIndexOrFirst(Split(values), 0, "0");
            return source.Replace(token, FormatScalingValue(type, value));
        }

        public static string FormatScalingValue(string scalingType, string value)
        {
            string displayValue = GetDisplayValue(value);
            string normalized = string.IsNullOrWhiteSpace(scalingType) ? "None" : scalingType.Trim();

            if (normalized.Equals("MissingHP", StringComparison.OrdinalIgnoreCase))
            {
                return global::GameLocalization.FormatWithFallback(
                    "skill.scaling.missing_hp",
                    "잃은 체력의 {0}%",
                    displayValue);
            }

            return displayValue;
        }

        private static string ReplaceIndexed(string source, string tokenName, string values)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrWhiteSpace(tokenName))
                return source;

            string[] splitValues = Split(values);
            for (int i = 0; i < splitValues.Length; i++)
            {
                string token = $"{{{tokenName}{i + 1}}}";
                if (source.Contains(token))
                    source = source.Replace(token, FormatReplacementValue(tokenName, splitValues[i]));
            }
            return source;
        }

        private static string ReplaceSingle(string source, string token, string values)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(token) || !source.Contains(token))
                return source;

            string first = GetByIndexOrFirst(Split(values), 0, string.Empty);
            return source.Replace(token, token == "{CountRate}" ? FormatCountValue(first) : GetDisplayValue(first));
        }

        private static string FormatReplacementValue(string tokenName, string value)
        {
            return tokenName == "CountRate" ? FormatCountValue(value) : GetDisplayValue(value);
        }

        private static string FormatCountValue(string value)
        {
            string display = GetDisplayValue(value);
            return display == "?" ? display : "x" + display;
        }

        private static string[] Split(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? Array.Empty<string>() : text.Split(';');
        }

        private static string GetByIndexOrFirst(string[] values, int index, string fallback)
        {
            if (values == null || values.Length == 0)
                return fallback;
            if (index >= 0 && index < values.Length && !string.IsNullOrWhiteSpace(values[index]))
                return values[index].Trim();
            return !string.IsNullOrWhiteSpace(values[0]) ? values[0].Trim() : fallback;
        }

        public static string GetDisplayValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "?";

            string trimmed = value.Trim();
            if (trimmed.Length > 1 && trimmed[0] == '-' &&
                float.TryParse(trimmed.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                return trimmed.Substring(1);
            }
            return trimmed;
        }
    }
}
