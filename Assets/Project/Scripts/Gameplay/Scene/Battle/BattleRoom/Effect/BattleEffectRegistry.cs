using System.Collections.Generic;
using UnityEngine;

public class BattleEffectRegistry
{
    private readonly Dictionary<string, BattleEffectBase> effects = new();

    public BattleEffectRegistry()
    {
        Register(new ArmorEffect());
        Register(new BurnEffect());
        Register(new PowerEffect());

        Register(new StrikeEffect());

        Register(new LogOnlyEffect("E_Corrosion"));
        Register(new LogOnlyEffect("E_Block"));
        Register(new LogOnlyEffect("E_Grudge"));
    }

    private void Register(BattleEffectBase effect)
    {
        if (effect == null)
            return;

        effects[effect.EffectId] = effect;
    }

    public BattleEffectBase Get(string effectId)
    {
        if (string.IsNullOrWhiteSpace(effectId))
            return null;

        if (effects.TryGetValue(effectId.Trim(), out BattleEffectBase effect))
            return effect;

        Debug.LogWarning($"[BattleEffectRegistry] 등록되지 않은 EffectId: {effectId}");
        return null;
    }
}