using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class SkillEffectEntry
    {
        public string EffectId;

        public ValueCalcType ValueCalcType;
        public int ValueAmount;

        public int CountAmount;

        public EffectMasterData EffectData;
    }
}