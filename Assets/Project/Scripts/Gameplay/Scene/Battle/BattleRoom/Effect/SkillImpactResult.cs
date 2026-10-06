[System.Serializable]
public sealed class SkillImpactResult
{
    public string TargetRuntimeId;
    public int TargetGridIndex = -1;
    public bool Attempted;
    public bool Hit;
    public int DamageAmount;
    public bool Killed;
    public bool HadBleedingBeforeHit;
    public int VulnerableValueBeforeHit;
    public int PoisonValueBeforeEffect;
}
