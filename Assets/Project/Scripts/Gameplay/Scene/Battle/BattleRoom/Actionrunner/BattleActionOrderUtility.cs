using Relic.Gameplay.Data;

public static class BattleActionOrderUtility
{
    public static bool HasSwift(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;

        return HasSwift(command.SkillData);
    }

    public static bool HasSwift(SkillMasterData skillData)
    {
        if (skillData == null)
            return false;

        return HasEffectId(skillData.EffectIds, "E_Swift");
    }

    private static bool HasEffectId(string effectIds, string targetEffectId)
    {
        if (string.IsNullOrWhiteSpace(effectIds))
            return false;

        string[] split = effectIds.Split(';');

        for (int i = 0; i < split.Length; i++)
        {
            if (split[i].Trim() == targetEffectId)
                return true;
        }

        return false;
    }
}
