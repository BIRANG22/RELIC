using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class ArabellaPhaseTransition
{
    public const string PhaseOneMonsterId = "Mon_12_1";
    public const string PhaseTwoMonsterId = "Mon_12_2";

    public static bool IsArabellaMonsterId(string monsterId)
    {
        return string.Equals(monsterId, PhaseOneMonsterId, StringComparison.Ordinal) ||
               string.Equals(monsterId, PhaseTwoMonsterId, StringComparison.Ordinal);
    }

    public static bool TryCreatePhaseTwo(
        MonsterRuntimeData current,
        MonsterMasterData phaseTwoMaster,
        out MonsterRuntimeData phaseTwo)
    {
        phaseTwo = null;

        if (current == null ||
            !current.IsDead ||
            current.IsDeathHandled ||
            !string.Equals(current.MonsterId, PhaseOneMonsterId, StringComparison.Ordinal) ||
            phaseTwoMaster == null ||
            !string.Equals(phaseTwoMaster.MonsterId, PhaseTwoMonsterId, StringComparison.Ordinal))
        {
            return false;
        }

        phaseTwo = new MonsterRuntimeData(current.RuntimeId, phaseTwoMaster)
        {
            Direction = current.Direction
        };
        return true;
    }

    public static string ResolvePhaseTwoAnimationState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName) ||
            stateName.EndsWith("_b", StringComparison.Ordinal))
        {
            return stateName;
        }

        if (stateName == "Idle" ||
            stateName == "Move" ||
            stateName == "Guard" ||
            stateName == "Hit" ||
            stateName == "Dead" ||
            stateName.StartsWith("AttackAction", StringComparison.Ordinal))
        {
            return stateName + "_b";
        }

        return stateName;
    }

    public static List<string> FilterOwnedSkills(
        MonsterRuntimeData runtime,
        IEnumerable<string> candidateSkillIds)
    {
        List<string> result = new();
        if (runtime == null || candidateSkillIds == null)
            return result;

        foreach (string skillId in candidateSkillIds)
        {
            if (!string.IsNullOrWhiteSpace(skillId) &&
                runtime.HasSkill(skillId) &&
                !result.Contains(skillId))
            {
                result.Add(skillId);
            }
        }

        return result;
    }
}
