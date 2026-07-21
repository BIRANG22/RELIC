using Relic.Gameplay.Data;
using UnityEngine;

public class UniqueRecoveryEffect : BattleEffectBase
{
    public override string EffectId => "E_UniqueRecovery";
    protected override void Apply(BattleEffectContext context)
    {
        BattleCharacter target = context?.PlayerTarget != null ? context.PlayerTarget : context?.PlayerCaster;
        if (target?.RuntimeData == null) return;
        CharacterMasterData master = DataManager.Instance?.CharacterDatabase?.Get(target.RuntimeData.CharacterId);
        int max = master != null ? Mathf.Max(0, master.MaxResource) : int.MaxValue;
        int value = BattleEffectUtility.GetRepeatedValue(context);
        target.RuntimeData.CurrentResource = Mathf.Min(max, target.RuntimeData.CurrentResource + value);
    }
}
