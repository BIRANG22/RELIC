public class ChargeEffect : BattleEffectBase
{
    public override string EffectId => "E_Charge";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        if (context.PlayerTarget != null)
        {
            BattleEffectUtility.AddStatusToPlayer(context.PlayerTarget, EffectId, context.Value, context.Count);
            return;
        }

        if (context.PlayerCaster != null)
        {
            BattleEffectUtility.AddStatusToPlayer(context.PlayerCaster, EffectId, context.Value, context.Count);
            return;
        }

        if (context.MonsterTarget != null)
        {
            BattleEffectUtility.AddStatusToMonster(context.MonsterTarget, EffectId, context.Value, context.Count);
            return;
        }

        if (context.MonsterCaster != null)
        {
            BattleEffectUtility.AddStatusToMonster(context.MonsterCaster, EffectId, context.Value, context.Count);
        }
    }
}