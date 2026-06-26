using System.Collections.Generic;
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

    public static class SkillRewardRoller
    {
        public static bool TryRoll(
            BattleMapData mapData,
            IReadOnlyList<SkillMasterData> allSkills,
            ISkillRewardRandom random,
            out SkillMasterData reward)
        {
            reward = null;

            if (mapData == null || allSkills == null || random == null)
                return false;

            if (!IsChanceSuccess(mapData.SkillDropChance, random))
                return false;

            if (!TryRollRarity(mapData, random, out SkillRarity rarity))
                return false;

            List<SkillMasterData> candidates = GetCandidates(allSkills, rarity, true);

            if (candidates.Count == 0)
                return false;

            reward = candidates[random.Range(0, candidates.Count)];
            return reward != null;
        }

        private static bool IsChanceSuccess(float chance, ISkillRewardRandom random)
        {
            chance = NormalizeChance(chance);

            if (chance <= 0f)
                return false;

            return random.Value() <= chance;
        }

        private static bool TryRollRarity(
            BattleMapData mapData,
            ISkillRewardRandom random,
            out SkillRarity rarity)
        {
            rarity = SkillRarity.None;

            float common = Mathf.Max(0f, mapData.CoreCommonChance);
            float rare = Mathf.Max(0f, mapData.CoreRareChance);
            float epic = Mathf.Max(0f, mapData.CoreEpicChance);
            float total = common + rare + epic;

            if (total <= 0f)
                return false;

            float roll = random.Value() * total;

            if (roll < common)
            {
                rarity = SkillRarity.CoreCommon;
                return true;
            }

            roll -= common;

            if (roll < rare)
            {
                rarity = SkillRarity.CoreRare;
                return true;
            }

            rarity = SkillRarity.CoreEpic;
            return true;
        }

        private static List<SkillMasterData> GetCandidates(
            IReadOnlyList<SkillMasterData> allSkills,
            SkillRarity rarity,
            bool baseOnly)
        {
            List<SkillMasterData> candidates = new();

            if (!SkillRarityUtility.IsCoreDropRarity(rarity))
                return candidates;

            for (int i = 0; i < allSkills.Count; i++)
            {
                SkillMasterData skill = allSkills[i];

                if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                    continue;

                if (skill.Category != Category.Core)
                    continue;

                if (!skill.SkillId.StartsWith("S_Core_", System.StringComparison.Ordinal))
                    continue;

                if (skill.Rarity != rarity)
                    continue;

                if (baseOnly && !SkillRarityUtility.IsBaseSkillVariant(skill.SkillId))
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
