using System;
using System.Collections.Generic;

/// <summary>게임데이터에서 같은 뜻으로 반복되는 몬스터 스킬 문구의 공통 번역 키.</summary>
public static class MonsterSkillSharedLocalizationKeys
{
    private static readonly Dictionary<string, string> DescriptionOwners = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "가까운 캐릭터 주변으로 1칸 접근한다.", "s_monster_01" },
        { "가장 가까운 대상의 위치에 {E_Strike}를 {ValueRate1}만큼 준다.", "s_monster_02" },
        { "전방에 {E_Strike}를 {ValueRate1}만큼 준다.", "s_monster_04" },
        { "가장 가까운 캐릭터에게서 최대 2칸 멀어진다.", "s_monster_05" },
        { "주변에 {E_Strike}를 {ValueRate1}만큼 준다.", "s_monster_06" },
        { "돌진하여 대상에게 {E_Strike}를 {ValueRate1}만큼 주고, {E_Knockback}을 {ValueRate2}칸 적용한다.", "s_monster_12" },
        { "범위에 {E_Strike}를 {ValueRate1}만큼 준다.", "s_monster_16" },
        { "가까운 캐릭터를 향해 1칸 이동한다.", "s_monster_21" },
        { "전방에 {E_Pierce}를 {ValueRate1}만큼 준다.", "s_monster_26" },
    };

    public static string NameKey(string skillId, string name)
    {
        return string.Equals(name?.Trim(), "이동", StringComparison.Ordinal)
            ? "common.move" : GameLocalization.BuildDataKey("MonsterSkill", skillId, "name");
    }

    public static string TypeKey(string type)
    {
        switch (type?.Trim())
        {
            case "이동": return "common.move";
            case "공격": return "common.attack";
            case "버프": return "common.buff";
            case "디버프": return "common.debuff";
            case "돌진": return "common.charge";
            case "설치": return "common.install";
            default: return null;
        }
    }

    public static string DescriptionKey(string skillId, string description)
    {
        string normalized = description?.Trim() ?? string.Empty;
        if (DescriptionOwners.TryGetValue(normalized, out string owner))
            return GameLocalization.BuildDataKey("MonsterSkill", owner, "effect_description");
        return GameLocalization.BuildDataKey("MonsterSkill", skillId, "effect_description");
    }
}
