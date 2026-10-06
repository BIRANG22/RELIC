using System;
using System.Collections.Generic;

[Serializable]
public sealed class SkillExecutionResult
{
    public string CasterCharacterId;
    public string SkillId;
    public int AttemptedHitCount { get; private set; }
    public int SuccessfulHitCount { get; private set; }
    public int DistinctHitTargetCount => distinctHitTargetIds.Count;
    public int KillCount { get; private set; }
    public int CasterBeneficialEffectTypeCount;
    public int RushDestinationGridIndex = -1;
    public string RushCollisionRuntimeId;
    public int RushCollisionGridIndex = -1;
    public IReadOnlyList<SkillImpactResult> Impacts => impacts;

    private readonly List<SkillImpactResult> impacts = new();
    private readonly HashSet<string> distinctHitTargetIds = new(StringComparer.Ordinal);

    public void RecordImpact(SkillImpactResult impact)
    {
        if (impact == null)
            return;

        impacts.Add(impact);
        if (impact.Attempted)
            AttemptedHitCount++;
        if (impact.Hit)
        {
            SuccessfulHitCount++;
            if (!string.IsNullOrWhiteSpace(impact.TargetRuntimeId))
                distinctHitTargetIds.Add(impact.TargetRuntimeId);
        }
        if (impact.Killed)
            KillCount++;
    }
}
