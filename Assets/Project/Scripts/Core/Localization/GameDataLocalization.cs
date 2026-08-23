namespace Relic.Gameplay.Data
{
    public static class GameDataLocalization
    {
        public static string CharacterName(CharacterMasterData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Character", data.CharacterId, "name", data.Name);

        public static string CharacterIntroduction(CharacterMasterData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Character", data.CharacterId, "introduction", data.Introduction);

        public static string MonsterName(string monsterId, string fallback) =>
            GameLocalization.GetData("Monster", monsterId, "name", fallback);

        public static string MonsterSpecialAction(string monsterId, int index, string fallback) =>
            GameLocalization.GetData("Monster", monsterId, $"special_action_{index}", fallback);

        public static string SkillName(SkillMasterData data) =>
            data == null ? string.Empty : GameLocalization.GetData("SkillMaster", data.SkillId, "name", data.Name);

        public static string SkillDetails(SkillMasterData data) =>
            SkillDescriptionFormatter.Format(data);

        public static string MonsterSkillName(MonsterSkillData data) =>
            data == null ? string.Empty : GameLocalization.GetData("MonsterSkill", data.SkillId, "name", data.Name);

        public static string MonsterSkillDescription(MonsterSkillData data) =>
            data == null ? string.Empty : GameLocalization.GetData("MonsterSkill", data.SkillId, "effect_description", data.EffectDesc);

        public static string MonsterPatternDescription(MonsterPatternInfoData data) =>
            data == null ? string.Empty : GameLocalization.GetData("MonsterPatternInfo", data.PatternId, "pattern_description", data.Description);

        public static string MonsterPatternSkillDescription(MonsterPatternInfoData data) =>
            data == null ? string.Empty : GameLocalization.GetData("MonsterPatternInfo", data.PatternId, "skill_description", data.SkillInfo);

        public static string RuneName(RuneData data) =>
            data == null ? string.Empty : data.Name;

        public static string RuneDescription(RuneData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Rune", data.RuneId, "effect_description", data.EffectDesc);

        public static string RelicName(RelicData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Relic", data.FragmentId, "name", data.Name);

        public static string RelicDescription(RelicData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Relic", data.FragmentId, "effect_description", data.EffectDesc);

        /// <summary>
        /// 유물 효과 설명은 Localization 설명문이 아니라 GameData Relic.EffectDesc를 원본으로 사용합니다.
        /// ValueRate/CountRate 자리표시자는 실제 데이터 값으로 치환합니다.
        /// </summary>
        public static string RelicEffectDescription(RelicData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.EffectDesc))
                return string.Empty;

            string result = data.EffectDesc;
            result = ReplaceRelicIndexedValues(result, "ValueRate", data.ValueRate);
            result = ReplaceRelicIndexedValues(result, "CountRate", data.CountRate);
            result = ReplaceRelicValue(result, "{ValueRate}", data.ValueRate);
            result = ReplaceRelicValue(result, "{CountRate}", data.CountRate);
            return result;
        }

        private static string ReplaceRelicIndexedValues(string source, string tokenName, string values)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrWhiteSpace(tokenName))
                return source;

            string[] splitValues = string.IsNullOrWhiteSpace(values)
                ? System.Array.Empty<string>()
                : values.Split(';');

            for (int i = 0; i < splitValues.Length; i++)
            {
                string token = $"{{{tokenName}{i + 1}}}";
                if (source.Contains(token))
                    source = source.Replace(token, GetRelicDisplayRateValue(splitValues[i]));
            }

            return source;
        }

        private static string ReplaceRelicValue(string source, string token, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(token) || !source.Contains(token))
                return source;

            return source.Replace(token, GetRelicDisplayRateValue(value));
        }

        private static string GetRelicDisplayRateValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "?";

            string displayValue = value.Trim();
            if (displayValue.Length > 1 &&
                displayValue[0] == '-' &&
                float.TryParse(
                    displayValue.Substring(1),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out _))
            {
                return displayValue.Substring(1);
            }

            return displayValue;
        }

        public static string RelicRarity(RelicData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Relic", data.FragmentId, "rarity", data.Rarity);

        public static string ItemName(ItemData data) =>
            data == null ? string.Empty : data.Name;

        public static string ItemDescription(ItemData data) =>
            data == null ? string.Empty : data.Desc;

        public static string EffectName(EffectMasterData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Effect", data.EffectId, "name", data.Name);

        public static string EffectTooltip(EffectMasterData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Effect", data.EffectId, "tooltip", data.ToolTip);
    }
}
