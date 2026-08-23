using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public sealed class BattleRunCharacterStatisticsData
    {
        public string CharacterId;
        public int DamageDealt;
        public int DamageTaken;
        public int DeathCount;
        public int KillCount;
    }

    [Serializable]
    public sealed class ExplorationResultData
    {
        public int Remnant;
        public List<string> RelicIds = new();
        public List<string> NewSkillIds = new();
        public List<string> BagItemIds = new();
        public List<BattleRunCharacterStatisticsData> CharacterStatistics = new();
    }

    [Serializable]
    public sealed class PendingResearchResultData
    {
        public ExplorationResultData ExplorationResult = new();
        public int RemnantBlue;
        public int RelicBlue;
        public int SkillBlue;
        public int TotalBlue;
        public bool IsApplied;
    }

    public readonly struct ResearchConversionBreakdown
    {
        public int RemnantBlue { get; }
        public int RelicBlue { get; }
        public int SkillBlue { get; }
        public int TotalBlue => RemnantBlue + RelicBlue + SkillBlue;

        public ResearchConversionBreakdown(int remnantBlue, int relicBlue, int skillBlue)
        {
            RemnantBlue = remnantBlue;
            RelicBlue = relicBlue;
            SkillBlue = skillBlue;
        }
    }

    public static class BattleRunStatisticsService
    {
        public static void RecordDamageDealt(BattleRuntimeData runtime, string characterId, int value)
        {
            GetOrCreate(runtime, characterId).DamageDealt += Mathf.Max(0, value);
        }

        public static void RecordDamageTaken(BattleRuntimeData runtime, string characterId, int value)
        {
            GetOrCreate(runtime, characterId).DamageTaken += Mathf.Max(0, value);
        }

        public static void RecordDeath(BattleRuntimeData runtime, string characterId)
        {
            GetOrCreate(runtime, characterId).DeathCount++;
        }

        public static void RecordKill(BattleRuntimeData runtime, string characterId)
        {
            GetOrCreate(runtime, characterId).KillCount++;
        }

        private static BattleRunCharacterStatisticsData GetOrCreate(
            BattleRuntimeData runtime,
            string characterId)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));
            if (string.IsNullOrWhiteSpace(characterId))
                throw new ArgumentException("CharacterId is required.", nameof(characterId));

            runtime.CharacterStatistics ??= new List<BattleRunCharacterStatisticsData>();
            string id = characterId.Trim();
            BattleRunCharacterStatisticsData stats = runtime.CharacterStatistics.Find(item =>
                item != null && string.Equals(item.CharacterId, id, StringComparison.Ordinal));

            if (stats != null)
                return stats;

            stats = new BattleRunCharacterStatisticsData { CharacterId = id };
            runtime.CharacterStatistics.Add(stats);
            return stats;
        }
    }

    public static class ExplorationResultBuilder
    {
        public static ExplorationResultData Build(BattleRuntimeData runtime)
        {
            ExplorationResultData result = new();
            if (runtime == null)
                return result;

            result.Remnant = Mathf.Max(0, runtime.Remnant);
            CopyUnique(runtime.OwnedRelicIds, result.RelicIds, null);

            HashSet<string> startingSkills = ToSet(runtime.StartingSkillInventoryIds);
            CopyUnique(runtime.SkillInventoryIds, result.NewSkillIds, startingSkills);
            CopyUnique(runtime.AcquiredSkillIds, result.NewSkillIds, startingSkills);
            CopyAll(runtime.BagItemIds, result.BagItemIds);

            if (runtime.CharacterStatistics != null)
            {
                for (int i = 0; i < runtime.CharacterStatistics.Count; i++)
                {
                    BattleRunCharacterStatisticsData source = runtime.CharacterStatistics[i];
                    if (source == null || string.IsNullOrWhiteSpace(source.CharacterId))
                        continue;

                    result.CharacterStatistics.Add(new BattleRunCharacterStatisticsData
                    {
                        CharacterId = source.CharacterId.Trim(),
                        DamageDealt = Mathf.Max(0, source.DamageDealt),
                        DamageTaken = Mathf.Max(0, source.DamageTaken),
                        DeathCount = Mathf.Max(0, source.DeathCount),
                        KillCount = Mathf.Max(0, source.KillCount)
                    });
                }
            }

            return result;
        }

        private static HashSet<string> ToSet(List<string> source)
        {
            HashSet<string> result = new(StringComparer.Ordinal);
            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(source[i]))
                    result.Add(source[i].Trim());
            }

            return result;
        }

        private static void CopyUnique(List<string> source, List<string> destination, HashSet<string> excluded)
        {
            if (source == null)
                return;

            HashSet<string> added = new(destination, StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(source[i]))
                    continue;

                string id = source[i].Trim();
                if ((excluded == null || !excluded.Contains(id)) && added.Add(id))
                    destination.Add(id);
            }
        }

        private static void CopyAll(List<string> source, List<string> destination)
        {
            if (source == null || destination == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(source[i]))
                    destination.Add(source[i].Trim());
            }
        }
    }

    public static class ResearchConversionPolicy
    {
        public static ResearchConversionBreakdown Calculate(
            int remnant,
            IEnumerable<RelicRarity> relicRarities,
            IEnumerable<SkillRarity> skillRarities)
        {
            int relicBlue = 0;
            if (relicRarities != null)
            {
                foreach (RelicRarity rarity in relicRarities)
                    relicBlue += GetRelicBlue(rarity);
            }

            int skillBlue = 0;
            if (skillRarities != null)
            {
                foreach (SkillRarity rarity in skillRarities)
                    skillBlue += GetSkillBlue(rarity);
            }

            return new ResearchConversionBreakdown(Mathf.Max(0, remnant) / 2, relicBlue, skillBlue);
        }

        public static int GetRelicBlue(RelicRarity rarity) => rarity switch
        {
            RelicRarity.Common => 10,
            RelicRarity.Rare => 25,
            RelicRarity.Epic => 50,
            RelicRarity.Unique => 100,
            _ => 0
        };

        public static int GetSkillBlue(SkillRarity rarity) => rarity switch
        {
            SkillRarity.Common => 10,
            SkillRarity.Rare => 25,
            SkillRarity.Epic => 50,
            SkillRarity.Unique => 100,
            _ => 0
        };
    }

    public static class PendingResearchSettlementService
    {
        public static bool HasPending(LobbyRuntimeData lobby)
        {
            return lobby != null &&
                   lobby.HasPendingResearchResult &&
                   lobby.PendingResearchResult != null;
        }

        public static bool ApplyOnce(LobbyRuntimeData lobby)
        {
            if (!HasPending(lobby))
                return false;

            PendingResearchResultData pending = lobby.PendingResearchResult;
            if (pending.IsApplied)
                return false;

            lobby.BlueDustium += Mathf.Max(0, pending.TotalBlue);
            lobby.BagItemIds ??= new List<string>();
            CopyAll(pending.ExplorationResult?.BagItemIds, lobby.BagItemIds);
            pending.IsApplied = true;
            return true;
        }

        public static void Clear(LobbyRuntimeData lobby)
        {
            if (lobby == null)
                return;

            lobby.HasPendingResearchResult = false;
            lobby.PendingResearchResult = null;
        }

        private static void CopyAll(List<string> source, List<string> destination)
        {
            if (source == null || destination == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(source[i]))
                    destination.Add(source[i].Trim());
            }
        }
    }
}
