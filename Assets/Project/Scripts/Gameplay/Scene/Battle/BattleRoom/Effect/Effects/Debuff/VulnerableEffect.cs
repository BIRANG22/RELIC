public class VulnerableEffect : BattleEffectBase
{
    public override string EffectId => "E_Vulnerable";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        if (context.PlayerTarget != null)
        {
            BattleEffectUtility.AddStatusToPlayer(context.PlayerTarget, EffectId, context.Value, context.Count);
            return;
        }

        if (context.MonsterTarget != null)
        {
            BattleEffectUtility.AddStatusToMonster(context.MonsterTarget, EffectId, context.Value, context.Count);
        }
    }
}