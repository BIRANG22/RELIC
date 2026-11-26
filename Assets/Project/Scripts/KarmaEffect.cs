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
        bool isKarma = UniqueResourceRuntimeUtility.IsKarma(target.RuntimeData);
        int previousResource = KarmaFixedPoint.GetDisplayFloor(target.RuntimeData, isKarma);
        int finalAmount = BattleEquipmentEffectService.ModifyUniqueResourceGain(
            target.RuntimeData, requestedAmount);

        BattleEquipmentEffectService.ApplyUniqueResourceGainSideEffects(
            target.RuntimeData,
            finalAmount,
            previousResource,
            maxResource);

        KarmaFixedPoint.AddDisplayUnits(target.RuntimeData, finalAmount, maxResource, isKarma);

        BattleEffectUtility.OnPlayerHudRefreshRequested?.Invoke(target);
    }
}
