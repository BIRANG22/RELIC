using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class SkillEffectEntry
    {
        public string EffectId;

        public ValueCalcType ValueCalcType;
        public int ValueAmount;

        public ValueCalcType CountCalcType;
        public int CountAmount;

        // EffectMaster에서 가져온 실제 효과 데이터
        public EffectMasterData EffectData;
    }
}