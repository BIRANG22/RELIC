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

        public List<SkillEffectEntry> ResolveCommonSkill(string skillId, out SkillRangeData range)
        {
            var data = skillDatabase.Get(skillId);

            range = data == null ? null : rangeDatabase.Get(data.RangeId);

            return data?.EffectEntries ?? new List<SkillEffectEntry>();
        }
    }
}