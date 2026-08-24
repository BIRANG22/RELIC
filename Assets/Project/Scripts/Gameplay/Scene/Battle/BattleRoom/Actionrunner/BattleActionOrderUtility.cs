using Relic.Gameplay.Data;

public static class BattleActionOrderUtility
{
    public static bool HasSwift(PlayerReservedCommand command)
    {
        if (command == null)
            return false;

        if (HasSwift(command.SkillData))
            return true;

        return HasSwiftStatus(command.UserRuntime);
    }

    private static bool HasSwiftStatus(CharacterRuntimeData runtime)
    {
        if (runtime?.StatusEffects == null)
            return false;

        for (int i = 0; i < runtime.StatusEffects.Count; i++)
        {
            StatusEffectRuntimeData status = runtime.StatusEffects[i];

            if (status == null || status.EffectId != "E_Swift")
                continue;

            if (status.Stack > 0 && status.TurnCount > 0)
                return true;
        }

        return false;
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
