using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class BattleSkillResolver
    {
        private readonly SkillDatabase skillDatabase;
        private readonly RangeDatabase rangeDatabase;

        public BattleSkillResolver(SkillDatabase skillDatabase, RangeDatabase rangeDatabase)
        {
            this.skillDatabase = skillDatabase;
            this.rangeDatabase = rangeDatabase;
        }

        public List<SkillEffectData> ResolveCommonSkill(string skillId, out SkillRangeData range)
        {
            var data = skillDatabase.GetCommon(skillId);
            range = data == null ? null : rangeDatabase.Get(data.RangeId);
            return data?.Effects ?? new List<SkillEffectData>();
        }
    }
}
