using Relic.Gameplay.Data;

public static class BattleActionOrderUtility
{
    public static bool HasSwift(PlayerReservedCommand command)
    {
        return command != null && HasSwift(command.UserRuntime);
    }

    public static bool HasSwift(CharacterRuntimeData runtime)
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

}
