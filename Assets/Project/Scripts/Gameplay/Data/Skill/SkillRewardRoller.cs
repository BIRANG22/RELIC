using System.Collections.Generic;
using Relic.Gameplay.Battle;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public interface ISkillRewardRandom
    {
        float Value();
        int Range(int minInclusive, int maxExclusive);
    }

    public sealed class UnitySkillRewardRandom : ISkillRewardRandom
    {
        public float Value()
        {
            return Random.value;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return Random.Range(minInclusive, maxExclusive);
        }
    }

    public sealed class BattleSkillRewardRandom : ISkillRewardRandom
    {
        public float Value()
        {
            return BattleRandom.Value();
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return BattleRandom.Range(minInclusive, maxExclusive);
        }
    }

    public static class SkillRewardRoller
    {
        public static bool TryRoll(
            BattleMapData mapData,
            IReadOnlyList<SkillMasterData> allSkills,
            IEnumerable<string> partyCharacterIds,
            ISkillRewardRandom random,
            out SkillMasterData reward)
        {
            reward = null;

            if (mapData == null || allSkills == null || random == null)
                return false;

            if (!IsChanceSuccess(mapData.MemoryDropChance, random))
                return false;

            // Draw from individual obtainable memories, rather than rolling a rarity first.
            // This also avoids losing a successful memory drop when a rolled rarity
            // has no remaining eligible skills.
            List<SkillMasterData> candidates = new();
            List<float> weights = new();
            float totalWeight = 0f;

            for (int i = 0; i < allSkills.Count; i++)
            {
                SkillMasterData skill = allSkills[i];
                if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId) ||
                    skill.Category != Category.Core ||
                    !SkillRarityUtility.IsCoreDropRarity(skill.Rarity) ||
                    !SkillOwnershipPolicy.CanRewardToParty(skill, partyCharacterIds))
                    continue;

                float weight = GetRarityWeight(mapData, skill.Rarity);
                if (weight <= 0f)
                    continue;

                candidates.Add(skill);
                weights.Add(weight);
                totalWeight += weight;
            }

            if (totalWeight <= 0f)
                return false;

            float roll = random.Value() * totalWeight;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                {
                    reward = candidates[i];
                    return true;
                }
            }

            reward = candidates[candidates.Count - 1];
            return true;
        }

        private static bool IsChanceSuccess(float chance, ISkillRewardRandom random)
        {
            chance = NormalizeChance(chance);

            if (chance <= 0f)
                return false;

            return random.Value() <= chance;
        }

        private static float GetRarityWeight(BattleMapData mapData, SkillRarity rarity)
        {
            switch (rarity)
            {
                case SkillRarity.Common: return Mathf.Max(0f, mapData.MemoryCommonChance);
                case SkillRarity.Rare: return Mathf.Max(0f, mapData.MemoryRareChance);
                case SkillRarity.Epic: return Mathf.Max(0f, mapData.MemoryEpicChance);
                case SkillRarity.Unique: return Mathf.Max(0f, mapData.MemoryUniqueChance);
                default: return 0f;
            }
        }

        public static List<SkillMasterData> GetCandidates(
            IReadOnlyList<SkillMasterData> allSkills,
            SkillRarity rarity,
            IEnumerable<string> partyCharacterIds)
        {
            List<SkillMasterData> candidates = new();

            if (allSkills == null || !SkillRarityUtility.IsCoreDropRarity(rarity))
                return candidates;

            for (int i = 0; i < allSkills.Count; i++)
            {
                SkillMasterData skill = allSkills[i];

                if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                    continue;

                if (skill.Category != Category.Core || skill.Rarity != rarity)
                    continue;

                if (!SkillOwnershipPolicy.CanRewardToParty(skill, partyCharacterIds))
                    continue;

                candidates.Add(skill);
            }

            return candidates;
        }

        private static float NormalizeChance(float chance)
        {
            if (chance > 1f)
                chance *= 0.01f;

            return Mathf.Clamp01(chance);
        }
    }
}
