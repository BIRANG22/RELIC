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

        public static string SkillTooltip(SkillMasterData data) =>
            data == null ? string.Empty : GameLocalization.GetData("SkillMaster", data.SkillId, "tooltip", data.ToolTip);

        public static string SkillDetails(SkillMasterData data) =>
            data == null ? string.Empty : GameLocalization.GetData("SkillMaster", data.SkillId, "details", data.Details);

        public static string MonsterSkillName(MonsterSkillData data) =>
            data == null ? string.Empty : GameLocalization.GetData("MonsterSkill", data.SkillId, "name", data.Name);

        public static string MonsterSkillDescription(MonsterSkillData data) =>
            data == null ? string.Empty : GameLocalization.GetData("MonsterSkill", data.SkillId, "effect_description", data.EffectDesc);

        public static string MonsterPatternDescription(MonsterPatternInfoData data) =>
            data == null ? string.Empty : GameLocalization.GetData("MonsterPatternInfo", data.PatternId, "pattern_description", data.Description);

        public static string MonsterPatternSkillDescription(MonsterPatternInfoData data) =>
            data == null ? string.Empty : GameLocalization.GetData("MonsterPatternInfo", data.PatternId, "skill_description", data.SkillInfo);

        public static string RuneName(RuneData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Rune", data.RuneId, "name", data.Name);

        public static string RuneDescription(RuneData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Rune", data.RuneId, "effect_description", data.EffectDesc);

        public static string RelicName(RelicData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Relic", data.FragmentId, "name", data.Name);

        public static string RelicDescription(RelicData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Relic", data.FragmentId, "effect_description", data.EffectDesc);

        public static string RelicRarity(RelicData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Relic", data.FragmentId, "rarity", data.Rarity);

        public static string ItemName(ItemData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Item", data.ItemId, "name", data.Name);

        public static string ItemDescription(ItemData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Item", data.ItemId, "description", data.Desc);

        public static string EffectName(EffectMasterData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Effect", data.EffectId, "name", data.Name);

        public static string EffectTooltip(EffectMasterData data) =>
            data == null ? string.Empty : GameLocalization.GetData("Effect", data.EffectId, "tooltip", data.ToolTip);
    }
}
