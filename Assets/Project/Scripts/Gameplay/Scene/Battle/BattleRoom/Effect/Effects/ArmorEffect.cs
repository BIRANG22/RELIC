public class ArmorEffect : BattleEffectBase
{
    public override string EffectId => "E_Armor";

    protected override void Apply(BattleEffectContext context)
    {
        if (context.PlayerTarget != null)
        {
            context.PlayerTarget.RuntimeData.CurrentShield += context.Value;
            return;
        }

        if (context.PlayerCaster != null)
            context.PlayerCaster.RuntimeData.CurrentShield += context.Value;
    }
}