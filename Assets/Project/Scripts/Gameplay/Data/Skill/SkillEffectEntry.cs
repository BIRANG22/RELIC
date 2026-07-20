using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class SkillEffectEntry
    {
        public string EffectId;

        public int ValueAmount;

        public int CountAmount;

        public EffectMasterData EffectData;
    }
}
