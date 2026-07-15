using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MonsterRuntimeData
    {
        public string RuntimeId;
        public string MonsterId;
        public string Name;
        public string DisplayName;
        public string Grade;

        [FormerlySerializedAs("MaxHp")]
        public int MaxHP;
        [FormerlySerializedAs("CurrentHp")]
        public int CurrentHP;
        public int CurrentShield;

        public int MinRemnant;
        public int MaxRemnant;
        public string UniqueItemId;
        public float UniqueItemChance;
        public float RelicChance;
        public string AttackRangeId;

        public string[] PossibleSkillIdsByActionIndex = new string[MonsterMasterData.PossibleSkillSlotCount];
        public List<string> PossSkillIds = new();
        public int TurnCount;
        public bool IsDead => CurrentHP <= 0;
        public List<StatusEffectRuntimeData> StatusEffects = new();

        public bool IsDeathHandled;
        public bool IsExplodeReady;

        public BattleDirection Direction = BattleDirection.Left;

        public MonsterRuntimeData(string runtimeId, MonsterMasterData masterData)
        {
            RuntimeId = runtimeId;

            if (masterData == null)
            {
                TurnCount = 0;
                InitializePossibleSkills(null);
                return;
            }

            MonsterId = masterData.MonsterId;
            Name = masterData.Name;
            DisplayName = masterData.Name;
            Grade = masterData.Grade;

            MaxHP = masterData.HP;
            CurrentHP = masterData.HP;
            CurrentShield = Math.Max(0, masterData.Armor);

            MinRemnant = masterData.MinRemnant;
            MaxRemnant = masterData.MaxRemnant;
            UniqueItemId = masterData.UniqueItemId;
            UniqueItemChance = masterData.UniqueItemChance;
            RelicChance = masterData.RelicChance;
            AttackRangeId = masterData.AttackRangeId;

            TurnCount = 0;
            InitializePossibleSkills(masterData);
            InitializeMonsterTraits();
        }


        public string GetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(DisplayName))
                return DisplayName;

            return string.IsNullOrWhiteSpace(Name) ? string.Empty : Name;
        }

        public void SetDisplaySuffix(string suffix)
        {
            string baseName = string.IsNullOrWhiteSpace(Name) ? MonsterId : Name;

            if (string.IsNullOrWhiteSpace(suffix))
            {
                DisplayName = baseName;
                return;
            }

            DisplayName = $"{baseName}_{suffix.Trim()}";
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0)
                return;

            CurrentHP -= damage;

            if (CurrentHP < 0)
                CurrentHP = 0;
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
                return;

            CurrentHP += amount;

            if (CurrentHP > MaxHP)
                CurrentHP = MaxHP;
        }

        public void IncreaseTurnCount()
        {
            TurnCount++;
        }

        public bool HasSkill(string skillId)
        {
            return PossSkillIds.Contains(skillId);
        }

        public int GetActionIndexForSkill(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                return 0;

            string normalizedSkillId = skillId.Trim();

            if (normalizedSkillId == "0")
                return 0;

            for (int i = 0; i < PossibleSkillIdsByActionIndex.Length; i++)
            {
                if (PossibleSkillIdsByActionIndex[i] == normalizedSkillId)
                    return i + 1;
            }

            return 0;
        }

        public float GetHPPercent()
        {
            if (MaxHP <= 0)
                return 0f;

            return (float)CurrentHP / MaxHP;
        }


        private void InitializeMonsterTraits()
        {
            if (StatusEffects == null)
                StatusEffects = new List<StatusEffectRuntimeData>();

            if (MonsterId == "Mon_01")
            {
                StatusEffects.Add(new StatusEffectRuntimeData("E_Split", 5));
            }
            else if (MonsterId == "Mon_06")
            {
                StatusEffects.Add(new StatusEffectRuntimeData("E_Explode", 3));
            }
        }

        private void InitializePossibleSkills(MonsterMasterData masterData)
        {
            string[] slots = masterData != null
                ? masterData.GetPossibleSkillIdSlots()
                : Array.Empty<string>();
            PossibleSkillIdsByActionIndex = new string[MonsterMasterData.PossibleSkillSlotCount];
            PossSkillIds.Clear();

            for (int i = 0; i < PossibleSkillIdsByActionIndex.Length; i++)
            {
                string skillId = i < slots.Length ? slots[i] : "";
                PossibleSkillIdsByActionIndex[i] = skillId;

                if (!string.IsNullOrEmpty(skillId))
                    PossSkillIds.Add(skillId);
            }
        }
    }
}

