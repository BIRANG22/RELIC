using UnityEngine;

public class LogOnlyEffect : BattleEffectBase
{
    private readonly string effectId;

    public LogOnlyEffect(string effectId)
    {
        this.effectId = effectId;
    }

    public override string EffectId => effectId;

    protected override void Apply(BattleEffectContext context)
    {
        Debug.Log(
            $"[Effect] Not Implemented Yet / Effect:{EffectId} / " +
            $"Value:{context.Value} / Count:{context.Count}"
        );
    }
}