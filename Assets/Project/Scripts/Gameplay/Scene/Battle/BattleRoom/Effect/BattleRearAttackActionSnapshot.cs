using System.Collections.Generic;

public sealed class BattleRearAttackActionSnapshot
{
    private readonly Dictionary<string, bool> qualificationByTargetRuntimeId = new();

    public bool GetOrCapture(string targetRuntimeId, bool liveQualification)
    {
        if (string.IsNullOrWhiteSpace(targetRuntimeId))
            return liveQualification;

        if (qualificationByTargetRuntimeId.TryGetValue(targetRuntimeId, out bool captured))
            return captured;

        qualificationByTargetRuntimeId[targetRuntimeId] = liveQualification;
        return liveQualification;
    }
}
