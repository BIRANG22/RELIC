using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Relic.Gameplay.Data
{
    public enum ResourceType
    {
        None,
        Rage,
        Momentum,
        Aether,
        Faith,
        Blood
    }
    public enum ResourceTrigger
    {
        None,

        OnAnyAllyDamaged,          // 아군 또는 자신이 피해를 받았을 때
        OnThreeActionsInSameSlot,  // 한 슬롯에서 이동을 제외한 행동을 3회 했을 때
        OnSpendEightCostInTurn,    // 한 턴 동안 코스트를 8 이상 소모했을 때
        OnAllyBuffApplied,         // 아군 또는 자신이 이로운 효과를 받았을 때
        OnDamageEnemy              // 공격으로 적에게 피해를 주었을 때
    }

    [Serializable]
    public class CharacterMasterData
    {
        public string CharacterId;
        public string Name;
        public string Introduction;
        public string Regeneration;

        [FormerlySerializedAs("MaxHealth")]
        public int MaxHP;
        [FormerlySerializedAs("MaxStamina")]
        public int MaxCost;
        [FormerlySerializedAs("StaminaRecovery")]
        public int CostRecovery;
        public int MaxResource;
        public ResourceType ResourceType;
        public ResourceTrigger ResourceTrigger;

        public bool IsDefaultProvided;
        public string UnlockCondition;

        public string PassiveSkill1;
        public string PassiveSkill2;

        public string UniqueSkill1;
        public string UniqueSkill2;

        public string CharacterSkill1;
        public string CharacterSkill2;

        public string CommonSkill1;
        public string CommonSkill2;

        public string Rune1;
        public string Rune2;
        public string Rune3;
        public string Rune4;
        public string Rune5;

        public int[] RuneSlotUnlockLevels;
        public int[] RuneUnlockLevels;
        public int[] PassiveSkillUnlockLevels;
        public int[] UniqueSkillUnlockLevels;
        public int[] CharacterSkillUnlockLevels;

        [NonSerialized]
        public GameObject BattlePrefab;

        [NonSerialized]
        public Sprite Icon;

        public string[] GetRuneIds()
        {
            return new string[]
            {
                Rune1,
                Rune2,
                Rune3,
                Rune4,
                Rune5
            };
        }
    }

    public static class CharacterLevelUnlockService
    {
        private const string RuneSlotUnlockText = "룬 슬롯 해금";
        private const string RuneUnlockText = "룬 해금";
        private const string SkillMemoryUnlockText = "기억 해금";

        private static readonly int[] DefaultRuneSlotUnlockLevels = { 1, 1, 3, 5, 7, 10 };
        private static readonly int[] DefaultPassiveSkillUnlockLevels = { 1, 5 };
        private static readonly int[] DefaultUniqueSkillUnlockLevels = { 1, 10 };
        private static readonly int[] DefaultCharacterSkillUnlockLevels = { 1, 1 };

        public static int GetRuneSlotUnlockLevel(CharacterMasterData character, int slotIndex)
        {
            return GetConfiguredLevel(
                character?.RuneSlotUnlockLevels,
                DefaultRuneSlotUnlockLevels,
                slotIndex,
                0);
        }

        public static int GetRuneUnlockLevel(
            CharacterMasterData character,
            RuneData rune,
            int runeIndex)
        {
            if (TryGetConfiguredLevel(character?.RuneUnlockLevels, runeIndex, out int configuredLevel))
                return configuredLevel;

            return Mathf.Max(0, rune?.UnlockLevel ?? 0);
        }

        public static int GetSkillMemoryUnlockLevel(
            CharacterMasterData character,
            int slotIndex,
            int candidateIndex)
        {
            int[] configuredLevels = slotIndex switch
            {
                0 => character?.PassiveSkillUnlockLevels,
                1 => character?.UniqueSkillUnlockLevels,
                2 => character?.CharacterSkillUnlockLevels,
                _ => null
            };

            int[] defaultLevels = slotIndex switch
            {
                0 => DefaultPassiveSkillUnlockLevels,
                1 => DefaultUniqueSkillUnlockLevels,
                2 => DefaultCharacterSkillUnlockLevels,
                _ => null
            };

            return GetConfiguredLevel(configuredLevels, defaultLevels, candidateIndex, 1);
        }

        public static IReadOnlyList<string> GetUnlockTexts(
            CharacterMasterData character,
            RuneDatabase runeDatabase,
            SkillDatabase skillDatabase,
            int levelBefore,
            int levelAfter)
        {
            if (character == null || levelAfter <= levelBefore)
                return Array.Empty<string>();

            List<string> texts = new();

            if (HasRuneSlotUnlock(character, levelBefore, levelAfter))
                AddUnique(texts, RuneSlotUnlockText);

            if (HasRuneUnlock(character, runeDatabase, levelBefore, levelAfter))
                AddUnique(texts, RuneUnlockText);

            if (HasSkillMemoryUnlock(character, skillDatabase, levelBefore, levelAfter))
                AddUnique(texts, SkillMemoryUnlockText);

            return texts;
        }

        private static bool HasRuneSlotUnlock(
            CharacterMasterData character,
            int levelBefore,
            int levelAfter)
        {
            int slotCount = GetRuneSlotUnlockCount(character);
            for (int i = 0; i < slotCount; i++)
            {
                if (IsCrossed(GetRuneSlotUnlockLevel(character, i), levelBefore, levelAfter))
                    return true;
            }

            return false;
        }

        private static bool HasRuneUnlock(
            CharacterMasterData character,
            RuneDatabase runeDatabase,
            int levelBefore,
            int levelAfter)
        {
            string[] runeIds = character.GetRuneIds();
            for (int i = 0; i < runeIds.Length; i++)
            {
                string runeId = NormalizeId(runeIds[i]);
                RuneData rune = ResolveRune(runeDatabase, runeId);
                int requiredLevel = GetRuneUnlockLevel(character, rune, i);

                if (IsCrossed(requiredLevel, levelBefore, levelAfter))
                    return true;
            }

            return false;
        }

        private static bool HasSkillMemoryUnlock(
            CharacterMasterData character,
            SkillDatabase skillDatabase,
            int levelBefore,
            int levelAfter)
        {
            for (int slotIndex = 0; slotIndex < 3; slotIndex++)
            {
                string[] skillIds = GetSkillIds(character, slotIndex);
                for (int candidateIndex = 0; candidateIndex < skillIds.Length; candidateIndex++)
                {
                    string skillId = NormalizeId(skillIds[candidateIndex]);
                    if (string.IsNullOrEmpty(skillId) &&
                        !TryGetConfiguredLevel(GetSkillUnlockLevels(character, slotIndex), candidateIndex, out _))
                    {
                        continue;
                    }

                    SkillMasterData skill = ResolveSkill(skillDatabase, skillId);
                    if (skillId.Length > 0 && skill == null && skillDatabase != null)
                        continue;

                    int requiredLevel = GetSkillMemoryUnlockLevel(character, slotIndex, candidateIndex);
                    if (IsCrossed(requiredLevel, levelBefore, levelAfter))
                        return true;
                }
            }

            return false;
        }

        private static int GetRuneSlotUnlockCount(CharacterMasterData character)
        {
            int configuredCount = character?.RuneSlotUnlockLevels?.Length ?? 0;
            return Mathf.Max(DefaultRuneSlotUnlockLevels.Length, configuredCount);
        }

        private static string[] GetSkillIds(CharacterMasterData character, int slotIndex)
        {
            if (character == null)
                return Array.Empty<string>();

            return slotIndex switch
            {
                0 => new[] { character.PassiveSkill1, character.PassiveSkill2 },
                1 => new[] { character.UniqueSkill1, character.UniqueSkill2 },
                2 => new[] { character.CharacterSkill1, character.CharacterSkill2 },
                _ => Array.Empty<string>()
            };
        }

        private static int[] GetSkillUnlockLevels(CharacterMasterData character, int slotIndex)
        {
            return slotIndex switch
            {
                0 => character?.PassiveSkillUnlockLevels,
                1 => character?.UniqueSkillUnlockLevels,
                2 => character?.CharacterSkillUnlockLevels,
                _ => null
            };
        }

        private static RuneData ResolveRune(RuneDatabase runeDatabase, string runeId)
        {
            if (runeDatabase == null || string.IsNullOrEmpty(runeId))
                return null;

            return runeDatabase.TryGet(runeId, out RuneData rune)
                ? rune
                : null;
        }

        private static SkillMasterData ResolveSkill(SkillDatabase skillDatabase, string skillId)
        {
            if (skillDatabase == null || string.IsNullOrEmpty(skillId))
                return null;

            return skillDatabase.TryGet(skillId, out SkillMasterData skill)
                ? skill
                : null;
        }

        private static int GetConfiguredLevel(
            int[] configuredLevels,
            int[] fallbackLevels,
            int index,
            int defaultLevel)
        {
            if (TryGetConfiguredLevel(configuredLevels, index, out int configuredLevel))
                return configuredLevel;

            if (TryGetConfiguredLevel(fallbackLevels, index, out int fallbackLevel))
                return fallbackLevel;

            return Mathf.Max(0, defaultLevel);
        }

        private static bool TryGetConfiguredLevel(int[] levels, int index, out int level)
        {
            level = 0;
            if (levels == null || index < 0 || index >= levels.Length)
                return false;

            level = Mathf.Max(0, levels[index]);
            return true;
        }

        private static bool IsCrossed(int requiredLevel, int levelBefore, int levelAfter)
        {
            return requiredLevel > levelBefore && requiredLevel <= levelAfter;
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (values == null || string.IsNullOrWhiteSpace(value))
                return;

            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                    return;
            }

            values.Add(value);
        }

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }
    }
}
