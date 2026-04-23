using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class CharacterStatusEffectData
    {
        public string CharacterId;
        public List<StatusEffectInstanceData> StatusEffects = new();
    }
}
