using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public enum SkillType
    {
        Passive,
        Unique,
        Common,
        Essence
    }

    [System.Serializable]
    public class SkillMasterData
    {
        public string SkillId;
        public SkillType SkillType;
        public string Name;
        public string Description;

        public float CoolTime;
        public int Cost;
        public int Power;
        public string RangeId;

        // 효과
        public List<string> EffectIds = new();
        public List<SkillEffectData> Effects = new();

        // 타입별 선택 필드
        public string PassiveTrigger;
        public string UniqueOwnerId;
        public int EssenceGrade;
    }
}