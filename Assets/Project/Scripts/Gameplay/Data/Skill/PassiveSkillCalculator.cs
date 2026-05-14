using Relic.Gameplay.Data;

public static class PassiveSkillCalculator
{
    public static bool IsActive(CharacterRuntimeData caster, SkillMasterData skill)
    {
        if (caster == null || skill == null)
            return false;

        if (skill.Category != Category.Passive)
            return false;

        if (skill.PassiveFormulaType == PassiveFormulaType.None)
            return false;

        return caster.CurrentResource >= skill.PassiveMinResource;
    }

    public static int GetStack(CharacterRuntimeData caster, SkillMasterData skill)
    {
        if (!IsActive(caster, skill))
            return 0;

        int resource = caster.CurrentResource;
        int min = skill.PassiveMinResource;

        return skill.PassiveFormulaType switch
        {
            PassiveFormulaType.Per1Resource_Stack1 =>
                resource,

            PassiveFormulaType.Per2Resource_Stack2 =>
                (resource / 2) * 2,

            PassiveFormulaType.FromMin_Per1Resource_Stack1 =>
                resource - min + 1,

            _ => 0
        };
    }
}