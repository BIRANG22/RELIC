public class BurnEffect : BattleEffectBase
{
    public override string EffectId => "E_Burn";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        if (context.PlayerTarget != null && context.PlayerTarget.RuntimeData != null)
        {
            BattleEffectUtility.AddStatusToPlayer(
                context.PlayerTarget,
                EffectId,
                context.Value);
            return;
        }

        if (context.MonsterTarget != null && context.MonsterTarget.RuntimeData != null)
        {
            BattleEffectUtility.AddStatusToMonster(
                context.MonsterTarget,
                EffectId,
                context.Value);
        }
    }
}
