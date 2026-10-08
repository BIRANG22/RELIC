using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class SkillRewardPoolPolicy
    {
        public static List<SkillMasterData> FilterCandidates(
            IReadOnlyList<SkillMasterData> candidates,
            SkillRewardPoolDatabase database)
        {
            return FilterCandidates(
                candidates,
                database != null && database.RestrictRewards,
                database != null ? database.AllowedSkillIds : null);
        }

        public static bool IsAllowed(string skillId, SkillRewardPoolDatabase database)
        {
            if (database == null || !database.RestrictRewards)
                return true;

            if (string.IsNullOrWhiteSpace(skillId))
                return false;

            HashSet<string> allowedIds = NormalizeIds(database.AllowedSkillIds);
            return allowedIds.Contains(skillId.Trim());
        }

        public static List<SkillMasterData> FilterCandidates(
            IReadOnlyList<SkillMasterData> candidates,
            bool restrictRewards,
            IEnumerable<string> allowedSkillIds)
        {
            List<SkillMasterData> result = new();

            if (candidates == null)
                return result;

            if (!restrictRewards)
            {
                for (int i = 0; i < candidates.Count; i++)
                    result.Add(candidates[i]);

                return result;
            }

            HashSet<string> allowedIds = NormalizeIds(allowedSkillIds);
            if (allowedIds.Count == 0)
                return result;

            for (int i = 0; i < candidates.Count; i++)
            {
                SkillMasterData skill = candidates[i];
                if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                    continue;

                if (allowedIds.Contains(skill.SkillId.Trim()))
                    result.Add(skill);
            }

            return result;
        }

        private static HashSet<string> NormalizeIds(IEnumerable<string> skillIds)
        {
            HashSet<string> result = new(StringComparer.Ordinal);

            if (skillIds == null)
                return result;

            foreach (string skillId in skillIds)
            {
                if (!string.IsNullOrWhiteSpace(skillId))
                    result.Add(skillId.Trim());
            }

            return result;
        }
    }
}
