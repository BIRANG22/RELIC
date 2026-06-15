public class ArmorEffect : BattleEffectBase
{
    public override string EffectId => "E_Armor";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        if (context.PlayerTarget != null)
        {
            BattleEffectUtility.AddShieldToPlayer(context.PlayerTarget, context.Value);
            return;
        }

        if (context.PlayerCaster != null)
        {
            BattleEffectUtility.AddShieldToPlayer(context.PlayerCaster, context.Value);
            return;
        }

        if (context.MonsterTarget != null)
        {
            BattleEffectUtility.AddShieldToMonster(context.MonsterTarget, context.Value);
            return;
        }

        if (context.MonsterCaster != null)
        {
            BattleEffectUtility.AddShieldToMonster(context.MonsterCaster, context.Value);
        }
    }
}