using System.Collections.Generic;
using UnityEngine;

public class BattleEffectRegistry
{
    private readonly Dictionary<string, BattleEffectBase> effects = new();

    public BattleEffectRegistry()
    {
        Register(new StrikeEffect());
        Register(new PierceEffect());
        Register(new HealEffect());
        Register(new CostRecoveryEffect());
        Register(new UniqueRecoveryEffect());

        Register(new BleedEffect());
        Register(new PoisonEffect());

        Register(new BoostEffect());
        Register(new ArmorEffect());
        Register(new ChargeEffect());
        Register(new FocusEffect());
        Register(new SwiftEffect());
        Register(new SmiteEffect());
        Register(new BarrierEffect());

        Register(new VulnerableEffect());
        Register(new WeakenEffect());
        Register(new GrudgeEffect());
        Register(new CorrosionEffect());

        Register(new BlockEffect());
        Register(new WardEffect());
        Register(new LifestealEffect());

        Register(new KnockbackEffect());
        Register(new GrabEffect());
        Register(new LogOnlyEffect("E_Rush"));
        Register(new CrashEffect());
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

        effectId = effectId.Trim();

        if (effects.TryGetValue(effectId, out BattleEffectBase effect))
            return effect;

        Debug.LogWarning($"[BattleEffectRegistry] 등록되지 않은 EffectId: {effectId}");
        return null;
    }
}