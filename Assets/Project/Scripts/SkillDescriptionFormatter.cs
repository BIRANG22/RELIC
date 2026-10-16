using System;
using System.Globalization;
using System.Text;
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
        // Matches only authored effect/value pairs. Other descriptions remain untouched.
        private static readonly Regex AutoEffectRegex = new(
            @"\{(?<id>E_[A-Za-z0-9_]+)\}\s+\{ValueRate(?<index>\d+)\}", RegexOptions.Compiled);
        private static readonly Regex MissingHpAutoRegex = new(
            @"\{E_Strike\}\s+\{E_MissingHPStrike\}\s+\{ValueRate1\}", RegexOptions.Compiled);
        private static readonly Regex ArmorAutoRegex = new(@"\{E_Strike\}\s+\{E_ArmorStrike\}", RegexOptions.Compiled);
        private static readonly Regex KillBonusRegex = new(@"처치\s*시\s*영구히\s*피해량\s*\{ValueRate(?<index>\d+)\}", RegexOptions.Compiled);
        private static readonly Regex MissPenaltyRegex = new(@"적중\s*실패\s*시\s*생명력\s*-\s*\{ValueRate(?<index>\d+)\}", RegexOptions.Compiled);
        private static readonly Regex TmpTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);


        public static string Format(SkillMasterData data)
        {
            if (data == null)
                return string.Empty;

            return Format(data.Details, data.ValueRate, data.CountRate, data.ScalingType);
        }

        public static string Format(string description, string valueRate, string countRate)
        {
            return Format(description, valueRate, countRate, null);
        }

        public static string Format(string description, string valueRate, string countRate, string scalingType)
        {
            return Format(description, valueRate, countRate, scalingType, false);
        }

        public static string Format(string description, string valueRate, string countRate,
            string scalingType, bool runtimeValues)
        {
            if (string.IsNullOrWhiteSpace(description))
                return string.Empty;

            string result = FormatAutoEffectSentences(description, valueRate, countRate, runtimeValues);
            result = ReplaceScalingIndexed(result, scalingType, valueRate);
            result = ReplaceIndexed(result, "ValueRate", valueRate);
            result = ReplaceIndexed(result, "CountRate", countRate);

            // 이전 단일 토큰 표기도 호환을 위해 유지합니다.
            result = ReplaceScalingSingle(result, "{ScalingValue}", scalingType, valueRate);
            result = ReplaceSingle(result, "{ValueRate}", valueRate);
            result = ReplaceSingle(result, "{CountRate}", countRate);
            return ReplaceEffectTokens(result, valueRate);
        }


        /// <summary>
        /// {E_Armor} 같은 EffectId 토큰을 로컬라이징된 효과명 + 인라인 아이콘 예약 영역으로 변환합니다.
        /// 실제 아이콘 Sprite는 SkillEffectInlineIconRenderer가 StatusEffectIconDatabase에서 가져옵니다.
        /// </summary>
        private static string FormatAutoEffectSentences(string source, string values, string counts, bool runtimeValues)
        {
            if (!source.Contains("{ValueRate") && !ArmorAutoRegex.IsMatch(source))
                return source;

            string[] valueParts = Split(values);
            string[] countParts = Split(counts);
            bool missingHp = MissingHpAutoRegex.IsMatch(source);
            bool armor = ArmorAutoRegex.IsMatch(source);
            // 특수 계산식은 효과 아이콘의 위치를 유지하면서 일반 피해 문장으로 조립합니다.
            string work = missingHp ? MissingHpAutoRegex.Replace(source, "{E_Strike} {ValueRate1}") : source;
            if (armor)
                work = ArmorAutoRegex.Replace(work, "{E_Strike} {ValueRate2}");

            MatchCollection matches = AutoEffectRegex.Matches(work);
            if (matches.Count == 0)
                return source;

            StringBuilder builder = new StringBuilder();
            int previous = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                builder.Append(work, previous, match.Index - previous);
                string id = match.Groups["id"].Value;
                if (!int.TryParse(match.Groups["index"].Value, out int index) || index < 1)
                    index = 1;
                string value = GetByIndexOrFirst(valueParts, index - 1, "0");
                string count = GetByIndexOrFirst(countParts, index - 1, "1");
                bool multiple = int.TryParse(TmpTagRegex.Replace(count, ""), out int parsedCount) && parsedCount > 1;
                bool isLastEffect = i == matches.Count - 1;
                string remainder = isLastEffect ? work.Substring(match.Index + match.Length).Trim() : "";
                bool conditionalTail = isLastEffect && (KillBonusRegex.IsMatch(remainder) || MissPenaltyRegex.IsMatch(remainder));
                string effectToken = "{" + id + "}";
                string unit = id == "E_Knockback" || id == "E_Grab" ? "tile" : "amount";
                string action = GetEffectAction(id);
                string key = "skill.sentence." + action + "." + unit + "." + (multiple ? "multiple" : "single") + "." + (isLastEffect || conditionalTail ? "final" : "linked");
                string fallback = GetKoreanTemplate(action, unit, multiple, isLastEffect || conditionalTail);
                string formatted = global::GameLocalization.FormatWithFallback(key, fallback,
                    effectToken, GetEffectParticle(id), GetDisplayValue(value), parsedCount);
                if (missingHp && id == "E_Strike" && !runtimeValues)
                    formatted = global::GameLocalization.FormatWithFallback(
                        "skill.sentence.special.missing_hp", "{0}{1} 잃은 생명력의 {2}%만큼 준다",
                        effectToken, GetEffectParticle(id), GetDisplayValue(value));
                else if (armor && id == "E_Strike" && !runtimeValues)
                    formatted = global::GameLocalization.FormatWithFallback(
                        "skill.sentence.special.armor", "{0}{1} 현재 방어도에 비례하여 준다",
                        effectToken, GetEffectParticle(id));
                builder.Append(formatted);
                previous = match.Index + match.Length;
                if (!isLastEffect && string.IsNullOrWhiteSpace(work.Substring(previous, matches[i + 1].Index - previous)))
                {
                    builder.Append(global::GameLocalization.Get("skill.sentence.join", ", "));
                    previous = matches[i + 1].Index;
                }
            }
            string suffix = work.Substring(previous);
            if (KillBonusRegex.IsMatch(suffix) || MissPenaltyRegex.IsMatch(suffix))
            {
                builder.Append(global::GameLocalization.Get("skill.sentence.break", ". "));
                suffix = KillBonusRegex.Replace(suffix.Trim(), m =>
                    global::GameLocalization.FormatWithFallback("skill.sentence.special.kill_bonus",
                        "처치 시 영구히 피해량이 {0} 증가한다.",
                        GetDisplayValue(GetByIndexOrFirst(valueParts, int.Parse(m.Groups["index"].Value) - 1, "0"))));
                suffix = MissPenaltyRegex.Replace(suffix, m =>
                    global::GameLocalization.FormatWithFallback("skill.sentence.special.miss_penalty",
                        "적중 실패 시 생명력이 {0} 감소한다.",
                        GetDisplayValue(GetByIndexOrFirst(valueParts, int.Parse(m.Groups["index"].Value) - 1, "0"))));
                builder.Append(suffix);
            }
            else
                builder.Append(suffix);
            string result = builder.ToString().Trim();
            string end = global::GameLocalization.Get("skill.sentence.end", ".");
            return result.EndsWith(end, StringComparison.Ordinal) ? result : result + end;
        }

        private static string GetEffectParticle(string id)
        {
            // Resolve the Korean particle from the localized effect name, not from
            // a numeric value. A final Hangul syllable with jongseong takes '을'.
            string name = null;
            DataManager manager = DataManager.Instance;
            if (manager?.EffectDatabase != null &&
                manager.EffectDatabase.TryGet(id, out EffectMasterData effect) && effect != null)
                name = GameDataLocalization.EffectName(effect);

            if (!string.IsNullOrWhiteSpace(name))
            {
                char last = name.TrimEnd()[name.TrimEnd().Length - 1];
                if (last >= 0xAC00 && last <= 0xD7A3)
                    return ((last - 0xAC00) % 28) == 0 ? "를" : "을";
            }

            switch (id)
            {
                case "E_Strike":
                case "E_Armor":
                case "E_Mana":
                case "E_Karma":
                case "E_Smite":
                case "E_Grit":
                case "E_Heal":
                case "E_Cure":
                    return "를";
                default:
                    return "을";
            }
        }

        private static string GetEffectAction(string id)
        {
            switch (id)
            {
                case "E_Strike": case "E_Pierce": case "E_MissingHPStrike": return "deal";
                case "E_Knockback": case "E_Grab": return "apply";
                case "E_Heal": case "E_Cure": return "restore";
                case "E_Boost":
                case "E_Grit":
                case "E_Charge":
                case "E_Smite":
                case "E_Swift":
                case "E_Lifesteal":
                case "E_Karma":
                case "E_Mana": return "gain";
                default: return "grant";
            }
        }

        // Korean is a fallback for the Korean locale only. All other languages must
        // supply their own grammatically ordered templates in the Text String Table.
        private static string GetKoreanTemplate(string action, string unit, bool multiple, bool final)
        {
            string ending;
            switch (action)
            {
                case "deal": ending = final ? "준다" : "주고"; break;
                case "apply": ending = final ? "적용한다" : "적용하고"; break;
                case "restore": ending = final ? "회복한다" : "회복하고"; break;
                case "gain": ending = final ? "획득한다" : "획득하고"; break;
                default: ending = final ? "부여한다" : "부여하고"; break;
            }
            return "{0}{1} {2}" + (unit == "tile" ? "칸" : "만큼") +
                   (multiple ? " {3}회" : "") + " " + ending;
        }

        public static string ReplaceEffectTokens(string source)
        {
            return ReplaceEffectTokens(source, null);
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
