using System;
using UnityEngine;

[Serializable]
public class BattleUnitPlayerSkillPresentations
{
    [Header("Power")]
    public BattleUnitActionPresentation power = new();

    [Header("Attack")]
    public BattleUnitActionPresentation attack1 = new();
    public BattleUnitActionPresentation attack2 = new();
    public BattleUnitActionPresentation attack3 = new();

    [Header("Skill")]
    public BattleUnitActionPresentation skill = new();

    public void EnsureSlots()
    {
        power ??= new BattleUnitActionPresentation();
        attack1 ??= new BattleUnitActionPresentation();
        attack2 ??= new BattleUnitActionPresentation();
        attack3 ??= new BattleUnitActionPresentation();
        skill ??= new BattleUnitActionPresentation();
    }

    public BattleUnitActionPresentation GetAttack(int attackIndex)
    {
        EnsureSlots();

        return attackIndex switch
        {
            1 => attack1,
            2 => attack2,
            3 => attack3,
            _ => attack1
        };
    }
}
