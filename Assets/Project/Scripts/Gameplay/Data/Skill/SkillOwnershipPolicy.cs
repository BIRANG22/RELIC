using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class SkillOwnershipPolicy
    {
        private const string AllCharactersId = "ALL";

        public static bool CanEquip(SkillMasterData skill, string characterId)
        {
            if (skill == null || string.IsNullOrWhiteSpace(skill.CharacterId) ||
                string.IsNullOrWhiteSpace(characterId))
            {
                return false;
            }

            string ownerId = skill.CharacterId.Trim();
            string targetId = characterId.Trim();
            return string.Equals(ownerId, AllCharactersId, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ownerId, targetId, StringComparison.OrdinalIgnoreCase);
        }

        public static bool CanRewardToParty(
            SkillMasterData skill,
            IEnumerable<string> partyCharacterIds)
        {
            if (skill == null || partyCharacterIds == null)
                return false;

            foreach (string characterId in partyCharacterIds)
            {
                if (CanEquip(skill, characterId))
                    return true;
            }

            return false;
        }

        public static IReadOnlyList<string> GetCompatiblePartyCharacterIds(
            SkillMasterData skill,
            IEnumerable<string> partyCharacterIds)
        {
            List<string> compatibleIds = new();
            if (partyCharacterIds == null)
                return compatibleIds;

            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (string characterId in partyCharacterIds)
            {
                if (string.IsNullOrWhiteSpace(characterId))
                    continue;

                string normalizedId = characterId.Trim();
                if (CanEquip(skill, normalizedId) && seen.Add(normalizedId))
                    compatibleIds.Add(normalizedId);
            }

            return compatibleIds;
        }

        public static IReadOnlyList<string> GetPartyCharacterIds(PartyRuntimeStore partyStore)
        {
            List<string> ids = new();
            if (partyStore?.Slots == null)
                return ids;

            for (int i = 0; i < partyStore.Slots.Count; i++)
            {
                string characterId = partyStore.Slots[i]?.CharacterId;
                if (!string.IsNullOrWhiteSpace(characterId))
                    ids.Add(characterId.Trim());
            }
            return ids;
        }
    }
}
