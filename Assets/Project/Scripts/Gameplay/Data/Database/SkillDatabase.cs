using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public class SkillDatabase
    {
        private readonly LookupDatabase<SkillMasterData> skillDb = new();
        private List<SkillMasterData> allSkills = new();

        public void Initialize(IEnumerable<SkillMasterData> skills)
        {
            allSkills = skills.ToList();
            skillDb.Initialize(allSkills, x => x.SkillId);
        }

        public SkillMasterData Get(string id) => skillDb.Get(id);

        public bool TryGet(string id, out SkillMasterData value)
        {
            return skillDb.TryGet(id, out value);
        }
        public List<SkillMasterData> GetByType(SkillType type)
        {
            return allSkills.Where(x => x.SkillType == type).ToList();
        }

        public List<SkillMasterData> GetAll()
        {
            return allSkills;
        }
    }
}