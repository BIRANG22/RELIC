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
            if (TryGet(id, out SkillMasterData value))
                return value;

            return skillDb.Get(id);
        }

        public bool TryGet(string id, out SkillMasterData value)
        {
            if (skillDb.TryGet(id, out value))
                return true;

            if (TryGetLegacyPublicCoreId(id, out string migratedId) &&
                skillDb.TryGet(migratedId, out value))
            {
                return true;
            }

            return TryGetLegacyPaddedNumericId(id, out string normalizedId) &&
                   skillDb.TryGet(normalizedId, out value);
        }

        private static bool TryGetLegacyPaddedNumericId(string skillId, out string normalizedSkillId)
        {
            normalizedSkillId = null;

            if (string.IsNullOrWhiteSpace(skillId))
                return false;

            string trimmedId = skillId.Trim();
            int separatorIndex = trimmedId.LastIndexOf('_');

            if (separatorIndex < 0 || separatorIndex >= trimmedId.Length - 1)
                return false;

            string numberText = trimmedId.Substring(separatorIndex + 1);
            if (!int.TryParse(numberText, out int number) || number < 0)
                return false;

            normalizedSkillId = trimmedId.Substring(0, separatorIndex + 1) + number.ToString("D2");
            return !string.Equals(normalizedSkillId, trimmedId, StringComparison.OrdinalIgnoreCase);
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
