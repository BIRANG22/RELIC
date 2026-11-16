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
        public static List<SkillMasterData> RollOffersWithoutReplacement(
            BattleMapData mapData,
            IReadOnlyList<SkillMasterData> candidates,
            int count,
            ISkillRewardRandom random)
        {
            List<SkillMasterData> remaining = candidates != null
                ? new List<SkillMasterData>(candidates)
                : new List<SkillMasterData>();
            List<SkillMasterData> offers = new();

            if (mapData == null || random == null || count <= 0)
                return offers;

            // Each card is rolled independently against the map rarity weights.
            // Once an exact memory is offered it is removed, but other rarities
            // remain eligible for the next card. Never lock all cards to one rarity.
            while (offers.Count < count && remaining.Count > 0)
            {
                float totalRarityWeight = 0f;
                foreach (SkillRarity rarity in CoreDropRarities)
                {
                    if (HasCandidateOfRarity(remaining, rarity))
                        totalRarityWeight += GetRarityWeight(mapData, rarity);
                }

                // A map can set a rarity weight to zero. If every remaining
                // candidate has zero weight, still fill the requested cards
                // rather than returning an incomplete reward selection.
                int selectedIndex;
                if (totalRarityWeight <= 0f)
                {
                    selectedIndex = random.Range(0, remaining.Count);
                }
                else
                {
                    float rarityRoll = random.Value() * totalRarityWeight;
                    SkillRarity selectedRarity = SkillRarity.Common;
                    bool chosen = false;
                    foreach (SkillRarity rarity in CoreDropRarities)
                    {
                        if (!HasCandidateOfRarity(remaining, rarity))
                            continue;

                        float weight = GetRarityWeight(mapData, rarity);
                        if (weight <= 0f)
                            continue;

                        selectedRarity = rarity;
                        rarityRoll -= weight;
                        if (rarityRoll <= 0f)
                        {
                            chosen = true;
                            break;
                        }
                    }
                    // In floating point edge cases, choose the last positive
                    // rarity encountered rather than implicitly selecting Common.
                    if (!chosen && !HasCandidateOfRarity(remaining, selectedRarity))
                    {
                        selectedIndex = random.Range(0, remaining.Count);
                    }
                    else
                    {
                        List<int> matchingIndices = new();
                        for (int i = 0; i < remaining.Count; i++)
                        {
                            if (remaining[i] != null && remaining[i].Rarity == selectedRarity)
                                matchingIndices.Add(i);
                        }
                        selectedIndex = matchingIndices.Count > 0
                            ? matchingIndices[random.Range(0, matchingIndices.Count)]
                            : random.Range(0, remaining.Count);
                    }
                }

                offers.Add(remaining[selectedIndex]);
                remaining.RemoveAt(selectedIndex);
            }

            return offers;
        }

        private static readonly SkillRarity[] CoreDropRarities =
        {
            SkillRarity.Common,
            SkillRarity.Rare,
            SkillRarity.Epic,
            SkillRarity.Unique
        };

        private static bool HasCandidateOfRarity(
            IReadOnlyList<SkillMasterData> candidates,
            SkillRarity rarity)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null && candidates[i].Rarity == rarity)
                    return true;
            }

            return false;
        }

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
