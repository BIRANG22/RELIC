using Relic.Gameplay.Data;

public static class BattleActionOrderUtility
{
    public static bool HasSwift(PlayerReservedCommand command)
    {
        return command != null &&
            ((command.SkillData != null && command.SkillData.IsFastSkill == 1) || HasSwift(command.UserRuntime));
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


    // Reservation never grants Swift. Only an applied runtime status (or
    // the skill's own fast flag) can change action order.
    public static bool HasSwiftForSlot(PlayerReservedCommand command,
        BattleTimelineController timeline, int slotIndex)
    {
        return HasSwift(command);
    }
}
