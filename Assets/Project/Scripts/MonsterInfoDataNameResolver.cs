using Relic.Gameplay.Data;

public static class MonsterInfoDataNameResolver
{
    public static string ResolveSkillName(MonsterSkillData skillData)
    {
        if (skillData == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(skillData.Name))
            return GameDataLocalization.MonsterSkillName(skillData).Trim();

        return string.IsNullOrWhiteSpace(skillData.SkillId)
            ? string.Empty
            : skillData.SkillId.Trim();
    }

    public static string ResolveEffectName(EffectMasterData effectData, string effectId)
    {
        if (effectData != null && !string.IsNullOrWhiteSpace(effectData.Name))
            return GameDataLocalization.EffectName(effectData).Trim();

        if (effectData != null && !string.IsNullOrWhiteSpace(effectData.EffectId))
            return effectData.EffectId.Trim();

        return string.IsNullOrWhiteSpace(effectId) ? string.Empty : effectId.Trim();
    }
}
