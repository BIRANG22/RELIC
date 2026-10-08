using Relic.Gameplay.Data;

public static class BattleSkillNamePopupPresentation
{
    public static bool ShouldShow(PlayerReservedCommand command)
    {
        return command != null &&
               command.SkillData != null &&
               command.ReservedMoveGridIndex < 0 &&
               command.SkillData.Category != Category.Move &&
               command.SkillData.TimelineNotation != TimelineActionType.Move;
    }

    public static bool ShouldShow(MonsterReservedCommand command)
    {
        return command != null &&
               command.SkillData != null &&
               command.SkillData.TimelineNotation != TimelineActionType.Move;
    }

    public static string GetDisplayName(PlayerReservedCommand command)
    {
        if (!ShouldShow(command))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(command.SkillData.Name))
            return command.SkillData.SkillId ?? string.Empty;

        string localizedName = GameDataLocalization.SkillName(command.SkillData);
        return string.IsNullOrWhiteSpace(localizedName)
            ? command.SkillData.SkillId ?? string.Empty
            : localizedName;
    }

    public static string GetDisplayName(MonsterReservedCommand command)
    {
        if (!ShouldShow(command))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(command.SkillData.Name))
            return command.SkillData.SkillId ?? string.Empty;

        string localizedName = GameDataLocalization.MonsterSkillName(command.SkillData);
        return string.IsNullOrWhiteSpace(localizedName)
            ? command.SkillData.SkillId ?? string.Empty
            : localizedName;
    }
}
