using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class StatusEffectInstanceData
    {
        public string StatusEffectId;
        public int StackCount = 1;
        public float Value;
        public int RemainingTurn;
    }
}
