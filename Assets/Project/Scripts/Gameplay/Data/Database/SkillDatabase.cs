using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class SkillDatabase
    {
        private readonly LookupDatabase<PassiveSkillData> passiveDb = new();
        private readonly LookupDatabase<UniqueSkillData> uniqueDb = new();
        private readonly LookupDatabase<CommonSkillData> commonDb = new();
        private readonly LookupDatabase<EssenceSkillData> essenceDb = new();

        public void Initialize(IEnumerable<PassiveSkillData> passive, IEnumerable<UniqueSkillData> unique, IEnumerable<CommonSkillData> common, IEnumerable<EssenceSkillData> essence)
        {
            passiveDb.Initialize(passive, x => x.SkillId);
            uniqueDb.Initialize(unique, x => x.SkillId);
            commonDb.Initialize(common, x => x.SkillId);
            essenceDb.Initialize(essence, x => x.SkillId);
        }

        public PassiveSkillData GetPassive(string id) => passiveDb.Get(id);
        public UniqueSkillData GetUnique(string id) => uniqueDb.Get(id);
        public CommonSkillData GetCommon(string id) => commonDb.Get(id);
        public EssenceSkillData GetEssence(string id) => essenceDb.Get(id);
    }
}
