using Relic.Gameplay.Data;
using UnityEngine;

public class KarmaEffect : BattleEffectBase
{
    public override string EffectId => "E_Karma";

    protected override void Apply(BattleEffectContext context)
    {
        BattleCharacter target = BattleEffectUtility.GetPlayerTargetOrCaster(context);
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        CharacterMasterData masterData = DataManager.Instance?.CharacterDatabase?.Get(
            target.RuntimeData.CharacterId);
        int maxResource = masterData != null ? Mathf.Max(0, masterData.MaxResource) : int.MaxValue;
        int requestedAmount = BattleEffectUtility.GetRepeatedValue(context);
        int previousResource = Mathf.Max(0, target.RuntimeData.CurrentResource);
        int finalAmount = BattleEquipmentEffectService.ModifyUniqueResourceGain(
            target.RuntimeData, requestedAmount);

        BattleEquipmentEffectService.ApplyUniqueResourceGainSideEffects(
            target.RuntimeData,
            finalAmount,
            previousResource,
            maxResource);

        target.RuntimeData.CurrentResource = Mathf.Min(
            maxResource,
            previousResource + finalAmount);

        BattleEffectUtility.OnPlayerHudRefreshRequested?.Invoke(target);
    }
}
