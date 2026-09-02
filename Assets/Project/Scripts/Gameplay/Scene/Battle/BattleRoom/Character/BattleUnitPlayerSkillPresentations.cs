using System;
using Relic.Gameplay.Data;
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

    [Header("Extra")]
    public BattleUnitActionPresentation extra1 = new();
    public BattleUnitActionPresentation extra2 = new();
    public BattleUnitActionPresentation extra3 = new();
    public BattleUnitActionPresentation extra4 = new();
    public BattleUnitActionPresentation extra5 = new();

    public void EnsureSlots()
    {
        power ??= new BattleUnitActionPresentation();
        attack1 ??= new BattleUnitActionPresentation();
        attack2 ??= new BattleUnitActionPresentation();
        attack3 ??= new BattleUnitActionPresentation();
        skill ??= new BattleUnitActionPresentation();
        extra1 ??= new BattleUnitActionPresentation();
        extra2 ??= new BattleUnitActionPresentation();
        extra3 ??= new BattleUnitActionPresentation();
        extra4 ??= new BattleUnitActionPresentation();
        extra5 ??= new BattleUnitActionPresentation();
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

    public BattleUnitActionPresentation GetPresentation(SkillAttackSlot slot)
    {
        EnsureSlots();

        return slot switch
        {
            SkillAttackSlot.Power => power,
            SkillAttackSlot.Attack1 => attack1,
            SkillAttackSlot.Attack2 => attack2,
            SkillAttackSlot.Attack3 => attack3,
            SkillAttackSlot.Skill => skill,
            SkillAttackSlot.Extra1 => extra1,
            SkillAttackSlot.Extra2 => extra2,
            SkillAttackSlot.Extra3 => extra3,
            SkillAttackSlot.Extra4 => extra4,
            SkillAttackSlot.Extra5 => extra5,
            _ => null
        };
    }
}
