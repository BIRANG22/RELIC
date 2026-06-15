using System.Collections.Generic;
using UnityEngine;

public class BattleEffectRegistry
{
    private readonly Dictionary<string, BattleEffectBase> effects = new();

    public BattleEffectRegistry()
    {
        Register(new StrikeEffect());
        Register(new PierceEffect());

        Register(new BurnEffect());
        Register(new BleedingEffect());
        Register(new AddictedEffect());

        Register(new PowerEffect());
        Register(new ArmorEffect());
        Register(new AimingEffect());
        Register(new RecoverEffect());
        Register(new RechargeEffect());
        Register(new FocusEffect());
        Register(new SwiftEffect());

        Register(new VulnerableEffect());
        Register(new WeakenEffect());
        Register(new GrudgeEffect());
        Register(new CorrosionEffect());

        Register(new BlockEffect());
        Register(new ThornsEffect());
        Register(new DrainEffect());

        Register(new KnockbackEffect());
        Register(new GrabEffect());
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