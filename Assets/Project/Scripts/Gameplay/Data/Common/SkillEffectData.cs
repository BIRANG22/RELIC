using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class SkillEffectData
    {
        public string EffectId;
        public float Coefficient;
        public float Multiplier = 1f;
        public int Count = 1;
        public int Turn = 0;
    }
}
