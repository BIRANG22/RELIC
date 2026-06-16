using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class StatusEffectRuntimeData
    {
        public string EffectId;
        public int Stack;
        public int TurnCount;

        public bool IsPassive;
        public string SourceSkillId;
        public StatusEffectRuntimeData()
        {
        }

        public StatusEffectRuntimeData(string effectId, int stack)
        {
            EffectId = effectId;
            Stack = stack;
            TurnCount = 1;
        }

        public StatusEffectRuntimeData(string effectId, int stack, int turnCount)
        {
            EffectId = effectId;
            Stack = stack;
            TurnCount = turnCount;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(EffectId)
                && Stack > 0
                && TurnCount > 0;
        }

        public void AddStack(int amount)
        {
            if (amount <= 0)
                return;

            Stack += amount;
        }

        public void RemoveStack(int amount)
        {
            if (amount <= 0)
                return;

            Stack -= amount;

            if (Stack < 0)
                Stack = 0;
        }

        public void DecreaseTurn()
        {
            TurnCount--;

            if (TurnCount < 0)
                TurnCount = 0;
        }
    }
}