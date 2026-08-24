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
        public int BuffApplied;
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

        public static void RecordBuffApplied(BattleRuntimeData runtime, string characterId, int value)
        {
            GetOrCreate(runtime, characterId).BuffApplied += Mathf.Max(0, value);
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

            Dictionary<string, BattleRunCharacterStatisticsData> statisticsByCharacterId =
                new(StringComparer.Ordinal);
            SeedSnapshotStatistics(
                result.CharacterStatistics,
                statisticsByCharacterId,
                runtime.LobbyLoadoutSnapshots);
            MergeRuntimeStatistics(
                result.CharacterStatistics,
                statisticsByCharacterId,
                runtime.CharacterStatistics);

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

        private static void SeedSnapshotStatistics(
            List<BattleRunCharacterStatisticsData> destination,
            Dictionary<string, BattleRunCharacterStatisticsData> statisticsByCharacterId,
            List<BattleLobbyLoadoutSnapshotData> snapshots)
        {
            if (destination == null ||
                statisticsByCharacterId == null ||
                snapshots == null)
            {
                return;
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                string id = NormalizeCharacterId(snapshots[i]?.CharacterId);
                if (id == null || statisticsByCharacterId.ContainsKey(id))
                    continue;

                BattleRunCharacterStatisticsData statistics = new()
                {
                    CharacterId = id
                };
                statisticsByCharacterId.Add(id, statistics);
                destination.Add(statistics);
            }
        }

        private static void MergeRuntimeStatistics(
            List<BattleRunCharacterStatisticsData> destination,
            Dictionary<string, BattleRunCharacterStatisticsData> statisticsByCharacterId,
            List<BattleRunCharacterStatisticsData> sourceStatistics)
        {
            if (destination == null ||
                statisticsByCharacterId == null ||
                sourceStatistics == null)
            {
                return;
            }

            for (int i = 0; i < sourceStatistics.Count; i++)
            {
                BattleRunCharacterStatisticsData source = sourceStatistics[i];
                string id = NormalizeCharacterId(source?.CharacterId);
                if (id == null)
                    continue;

                if (!statisticsByCharacterId.TryGetValue(id, out BattleRunCharacterStatisticsData target))
                {
                    target = new BattleRunCharacterStatisticsData { CharacterId = id };
                    statisticsByCharacterId.Add(id, target);
                    destination.Add(target);
                }

                target.DamageDealt += Mathf.Max(0, source.DamageDealt);
                target.DamageTaken += Mathf.Max(0, source.DamageTaken);
                target.BuffApplied += Mathf.Max(0, source.BuffApplied);
                target.DeathCount += Mathf.Max(0, source.DeathCount);
                target.KillCount += Mathf.Max(0, source.KillCount);
            }
        }

        private static string NormalizeCharacterId(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return null;

            return characterId.Trim();
        }
    }

    public readonly struct BattleStageClearExperiencePreview
    {
        public string CharacterId { get; }
        public int ExperienceGained { get; }
        public int LevelBefore { get; }
        public int LevelAfter { get; }
        public int ExperienceAfter { get; }
        public int RequiredExperienceAfter { get; }

        public bool LeveledUp => LevelAfter > LevelBefore;
        public float ProgressAfter01 => RequiredExperienceAfter > 0
            ? Mathf.Clamp01((float)ExperienceAfter / RequiredExperienceAfter)
            : 1f;

        public BattleStageClearExperiencePreview(
            string characterId,
            int experienceGained,
            int levelBefore,
            int levelAfter,
            int experienceAfter,
            int requiredExperienceAfter)
        {
            CharacterId = characterId;
            ExperienceGained = experienceGained;
            LevelBefore = levelBefore;
            LevelAfter = levelAfter;
            ExperienceAfter = experienceAfter;
            RequiredExperienceAfter = requiredExperienceAfter;
        }
    }

    public readonly struct BattleStageClearExperienceContext
    {
        public static BattleStageClearExperienceContext Empty => new(0, 0, 0, 0);

        public int NormalBattleClearCount { get; }
        public int EliteBattleClearCount { get; }
        public int BossBattleClearCount { get; }
        public int EventClearCount { get; }

        public BattleStageClearExperienceContext(
            int normalBattleClearCount,
            int eliteBattleClearCount,
            int bossBattleClearCount,
            int eventClearCount)
        {
            NormalBattleClearCount = Mathf.Max(0, normalBattleClearCount);
            EliteBattleClearCount = Mathf.Max(0, eliteBattleClearCount);
            BossBattleClearCount = Mathf.Max(0, bossBattleClearCount);
            EventClearCount = Mathf.Max(0, eventClearCount);
        }
    }

    public static class BattleStageClearExperienceService
    {
        public const int ExperiencePerLevel = 1000;
        public const int NormalBattleSurvivorExperience = 90;
        public const int EliteBattleSurvivorExperience = 160;
        public const int BossBattleSurvivorExperience = 350;
        public const int NormalBattleIncapacitatedExperience = 45;
        public const int EliteBattleIncapacitatedExperience = 80;
        public const int BossBattleIncapacitatedExperience = 175;
        public const int EventRoomClearExperience = 100;
        public const int KillExperience = 12;
        public const int DamageDealtExperiencePerFive = 5;
        public const int DamageTakenExperiencePerFive = 2;
        public const int BuffAppliedExperiencePerFive = 3;

        private const int MinCharacterLevel = 1;
        private const int MaxCharacterLevel = 30;
        private const int StatExperienceUnit = 5;

        public static IReadOnlyDictionary<string, BattleStageClearExperiencePreview> Preview(
            CharacterRuntimeStore characterStore,
            IEnumerable<BattleRunCharacterStatisticsData> characterStatistics,
            BattleStageClearExperienceContext context)
        {
            return Build(characterStore, characterStatistics, context, false);
        }

        public static IReadOnlyDictionary<string, BattleStageClearExperiencePreview> Apply(
            CharacterRuntimeStore characterStore,
            IEnumerable<BattleRunCharacterStatisticsData> characterStatistics,
            BattleStageClearExperienceContext context)
        {
            return Build(characterStore, characterStatistics, context, true);
        }

        public static int GetRequiredExperienceForNextLevel(int level)
        {
            if (level >= MaxCharacterLevel)
                return 0;

            return ExperiencePerLevel;
        }

        public static BattleStageClearExperienceContext BuildContext(
            MapRuntimeData runtime,
            GeneratedMapNodeData currentNode,
            bool defeat)
        {
            if (runtime?.GeneratedNodes == null)
                return BattleStageClearExperienceContext.Empty;

            HashSet<string> clearedNodeKeys = new(StringComparer.Ordinal);
            if (runtime.ClearedMapIds != null)
            {
                for (int i = 0; i < runtime.ClearedMapIds.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(runtime.ClearedMapIds[i]))
                        clearedNodeKeys.Add(runtime.ClearedMapIds[i].Trim());
                }
            }

            if (!defeat && currentNode != null)
                clearedNodeKeys.Add(currentNode.NodeIndex.ToString());

            int normalBattleClearCount = 0;
            int eliteBattleClearCount = 0;
            int bossBattleClearCount = 0;
            int eventClearCount = 0;

            for (int i = 0; i < runtime.GeneratedNodes.Count; i++)
            {
                GeneratedMapNodeData node = runtime.GeneratedNodes[i];
                if (node == null || !clearedNodeKeys.Contains(node.NodeIndex.ToString()))
                    continue;

                switch (NormalizeNodeType(node.Type))
                {
                    case "Common":
                    case "Battle":
                        normalBattleClearCount++;
                        break;
                    case "Elite":
                        eliteBattleClearCount++;
                        break;
                    case "Boss":
                        bossBattleClearCount++;
                        break;
                    case "Special":
                    case "Event":
                        eventClearCount++;
                        break;
                }
            }

            return new BattleStageClearExperienceContext(
                normalBattleClearCount,
                eliteBattleClearCount,
                bossBattleClearCount,
                eventClearCount);
        }

        public static int CalculateRewardExperience(
            BattleRunCharacterStatisticsData statistics,
            BattleStageClearExperienceContext context)
        {
            if (statistics == null)
                return 0;

            bool incapacitated = Mathf.Max(0, statistics.DeathCount) > 0;
            int reward = 0;

            reward += context.NormalBattleClearCount *
                (incapacitated ? NormalBattleIncapacitatedExperience : NormalBattleSurvivorExperience);
            reward += context.EliteBattleClearCount *
                (incapacitated ? EliteBattleIncapacitatedExperience : EliteBattleSurvivorExperience);
            reward += context.BossBattleClearCount *
                (incapacitated ? BossBattleIncapacitatedExperience : BossBattleSurvivorExperience);
            reward += context.EventClearCount * EventRoomClearExperience;
            reward += Mathf.Max(0, statistics.KillCount) * KillExperience;
            reward += GetStatExperience(statistics.DamageDealt, DamageDealtExperiencePerFive);
            reward += GetStatExperience(statistics.DamageTaken, DamageTakenExperiencePerFive);
            reward += GetStatExperience(statistics.BuffApplied, BuffAppliedExperiencePerFive);

            return Mathf.Max(0, reward);
        }

        private static Dictionary<string, BattleStageClearExperiencePreview> Build(
            CharacterRuntimeStore characterStore,
            IEnumerable<BattleRunCharacterStatisticsData> characterStatistics,
            BattleStageClearExperienceContext context,
            bool apply)
        {
            Dictionary<string, BattleStageClearExperiencePreview> result =
                new(StringComparer.Ordinal);

            if (characterStatistics == null)
                return result;

            foreach (BattleRunCharacterStatisticsData statistics in characterStatistics)
            {
                string characterId = NormalizeCharacterId(statistics?.CharacterId);
                if (characterId == null || result.ContainsKey(characterId))
                    continue;

                CharacterRuntimeData character = ResolveCharacter(characterStore, characterId);
                int rewardExperience = CalculateRewardExperience(statistics, context);
                BattleStageClearExperiencePreview preview =
                    Calculate(characterId, character, rewardExperience);

                result.Add(characterId, preview);

                if (!apply || character == null)
                    continue;

                character.Level = preview.LevelAfter;
                character.Exp = preview.ExperienceAfter;
                characterStore.AddOrUpdate(character);
            }

            return result;
        }

        private static int GetStatExperience(int value, int rewardPerFive)
        {
            return (Mathf.Max(0, value) / StatExperienceUnit) * Mathf.Max(0, rewardPerFive);
        }

        private static CharacterRuntimeData ResolveCharacter(
            CharacterRuntimeStore characterStore,
            string characterId)
        {
            if (characterStore == null || characterId == null)
                return null;

            return characterStore.TryGet(characterId, out CharacterRuntimeData character)
                ? character
                : null;
        }

        private static BattleStageClearExperiencePreview Calculate(
            string characterId,
            CharacterRuntimeData character,
            int rewardExperience)
        {
            int levelBefore = Mathf.Clamp(
                character?.Level ?? MinCharacterLevel,
                MinCharacterLevel,
                MaxCharacterLevel);
            int levelAfter = levelBefore;
            int expAfter = levelBefore >= MaxCharacterLevel
                ? 0
                : Mathf.Max(0, character?.Exp ?? 0);
            int remaining = levelAfter >= MaxCharacterLevel
                ? 0
                : Mathf.Max(0, rewardExperience);
            int gained = remaining;

            while (remaining > 0 && levelAfter < MaxCharacterLevel)
            {
                int required = GetRequiredExperienceForNextLevel(levelAfter);
                if (required <= 0)
                    break;

                int needed = Mathf.Max(0, required - expAfter);
                if (remaining < needed)
                {
                    expAfter += remaining;
                    remaining = 0;
                    break;
                }

                remaining -= needed;
                levelAfter++;
                expAfter = 0;
            }

            if (levelAfter >= MaxCharacterLevel)
            {
                levelAfter = MaxCharacterLevel;
                expAfter = 0;
            }

            return new BattleStageClearExperiencePreview(
                characterId,
                gained,
                levelBefore,
                levelAfter,
                expAfter,
                GetRequiredExperienceForNextLevel(levelAfter));
        }

        private static string NormalizeCharacterId(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return null;

            return characterId.Trim();
        }

        private static string NormalizeNodeType(string nodeType)
        {
            if (string.IsNullOrWhiteSpace(nodeType))
                return string.Empty;

            return nodeType.Trim();
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
