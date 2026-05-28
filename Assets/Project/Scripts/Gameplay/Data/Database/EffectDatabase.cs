using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class EffectDatabase
    {
        private readonly Dictionary<string, EffectMasterData> effects = new();

        public void Initialize(IEnumerable<EffectMasterData> effectList)
        {
            effects.Clear();

            if (effectList == null)
            {
                Debug.LogWarning("[EffectDatabase] effectList가 null입니다.");
                return;
            }

            foreach (var effect in effectList)
            {
                if (effect == null || string.IsNullOrWhiteSpace(effect.EffectId))
                    continue;

                if (effects.ContainsKey(effect.EffectId))
                {
                    Debug.LogWarning($"[EffectDatabase] 중복 EffectId: {effect.EffectId}");
                    continue;
                }

                effects.Add(effect.EffectId, effect);
            }
        }

        public EffectMasterData Get(string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                return null;

            effects.TryGetValue(effectId, out var effect);
            return effect;
        }

        public bool TryGet(string effectId, out EffectMasterData effect)
        {
            effect = null;

            if (string.IsNullOrWhiteSpace(effectId))
                return false;

            return effects.TryGetValue(effectId, out effect);
        }

        public bool Contains(string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                return false;

            return effects.ContainsKey(effectId);
        }

        public IReadOnlyDictionary<string, EffectMasterData> All => effects;
    }
}