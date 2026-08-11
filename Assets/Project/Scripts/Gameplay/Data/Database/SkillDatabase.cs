using System;
using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public class SkillDatabase
    {
        private const string LegacyPublicPrefix = "S_Public_";
        private const string CorePrefix = "S_Core_";

        private readonly LookupDatabase<SkillMasterData> skillDb = new();
        private List<SkillMasterData> allSkills = new();

        public void Initialize(IEnumerable<SkillMasterData> skills)
        {
            allSkills = skills.ToList();
            skillDb.Initialize(allSkills, x => x.SkillId);
        }

        public SkillMasterData Get(string id)
        {
            if (skillDb.TryGet(id, out SkillMasterData value))
                return value;

            if (TryGetLegacyPublicCoreId(id, out string migratedId))
                return skillDb.Get(migratedId);

            return skillDb.Get(id);
        }

        public bool TryGet(string id, out SkillMasterData value)
        {
            if (skillDb.TryGet(id, out value))
                return true;

            return TryGetLegacyPublicCoreId(id, out string migratedId) &&
                   skillDb.TryGet(migratedId, out value);
        }

        private static bool TryGetLegacyPublicCoreId(string skillId, out string migratedCoreSkillId)
        {
            migratedCoreSkillId = null;

            if (string.IsNullOrWhiteSpace(skillId))
                return false;

            skillId = skillId.Trim();

            if (!skillId.StartsWith(LegacyPublicPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string numberText = skillId.Substring(LegacyPublicPrefix.Length);
            if (!int.TryParse(numberText, out int number) || number < 1 || number > 20)
                return false;

            migratedCoreSkillId = CorePrefix + (number + 60).ToString("D2");
            return true;
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
