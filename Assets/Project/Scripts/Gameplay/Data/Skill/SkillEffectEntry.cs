using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class SkillEffectEntry
    {
        public string EffectId;

        public int ValueAmount;

        // SkillMaster.ScalingType (None, MissingHP µî)
        public string ScalingType;

        public int CountAmount;

        public EffectMasterData EffectData;
    }
}
