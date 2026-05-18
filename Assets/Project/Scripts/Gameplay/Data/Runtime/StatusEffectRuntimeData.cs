using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class StatusEffectRuntimeData
    {
        public string EffectId;
        public int Stack;
        public int RemainingTurn;

        public StatusEffectRuntimeData()
        {
        }

        public StatusEffectRuntimeData(string effectId, int stack, int remainingTurn)
        {
            EffectId = effectId;
            Stack = stack;
            RemainingTurn = remainingTurn;
        }
    }
}