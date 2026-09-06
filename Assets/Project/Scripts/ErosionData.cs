using System;

namespace Relic.Gameplay.Data
{
    /// <summary>
    /// GameData의 Erosion 시트 한 행을 나타냅니다.
    /// 실제 난이도 효과 적용은 이후 시스템에서 EffectType/EffectValue를 사용해 연결합니다.
    /// </summary>
    [Serializable]
    public class ErosionData
    {
        public string DifficultyId;
        public string ErosionName;
        public string SlotName;
        public int Tier;
        public int Score;
        public string GroupId;
        public string SelectionMode;
        public string EffectName;
        public string EffectType;
        public int EffectValue;
        public bool Selectable;
        public string Description;

        public bool IsExclusive =>
            string.Equals(SelectionMode, "Exclusive", StringComparison.OrdinalIgnoreCase);

        public bool IsIndependent =>
            string.Equals(SelectionMode, "Independent", StringComparison.OrdinalIgnoreCase);
    }
}
