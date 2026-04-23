using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MonsterStatusEffectData
    {
        public string MonsterId;
        public List<StatusEffectInstanceData> StatusEffects = new();
    }
}
