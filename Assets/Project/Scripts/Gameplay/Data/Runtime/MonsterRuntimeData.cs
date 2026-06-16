using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MonsterRuntimeData
    {
        public string RuntimeId;
        public string MonsterId;
        public string Name;
        public string Grade;

        public int MaxHp;
        public int CurrentHp;
        public int CurrentShield;

        public int MinRemnant;
        public int MaxRemnant;
        public string UniqueItemId;
        public float UniqueItemChance;
        public float RelicChance;

        public List<string> PossSkillIds = new();
        public int TurnCount;
        public bool IsDead => CurrentHp <= 0;
        public List<StatusEffectRuntimeData> StatusEffects = new();

        public bool IsDeathHandled;

        public BattleDirection Direction = BattleDirection.Left;

        public MonsterRuntimeData(string runtimeId, MonsterMasterData masterData)
        {
            RuntimeId = runtimeId;

            MonsterId = masterData.MonsterId;
            Name = masterData.Name;
            Grade = masterData.Grade;

            MaxHp = masterData.Health;
            CurrentHp = masterData.Health;

            MinRemnant = masterData.MinRemnant;
            MaxRemnant = masterData.MaxRemnant;
            UniqueItemId = masterData.UniqueItemId;
            UniqueItemChance = masterData.UniqueItemChance;
            RelicChance = masterData.RelicChance;

            TurnCount = 0;

            AddSkillIfValid(masterData.PossSkillId01);
            AddSkillIfValid(masterData.PossSkillId02);
            AddSkillIfValid(masterData.PossSkillId03);
            AddSkillIfValid(masterData.PossSkillId04);
            AddSkillIfValid(masterData.PossSkillId05);
            AddSkillIfValid(masterData.PossSkillId06);
            AddSkillIfValid(masterData.PossSkillId07);
            AddSkillIfValid(masterData.PossSkillId08);
            AddSkillIfValid(masterData.PossSkillId09);
            AddSkillIfValid(masterData.PossSkillId10);
        }

        private void AddSkillIfValid(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                return;

            if (skillId == "0")
                return;

            PossSkillIds.Add(skillId);
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0)
                return;

            CurrentHp -= damage;

            if (CurrentHp < 0)
                CurrentHp = 0;
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
                return;

            CurrentHp += amount;

            if (CurrentHp > MaxHp)
                CurrentHp = MaxHp;
        }

        public void IncreaseTurnCount()
        {
            TurnCount++;
        }

        public bool HasSkill(string skillId)
        {
            return PossSkillIds.Contains(skillId);
        }

        public float GetHpPercent()
        {
            if (MaxHp <= 0)
                return 0f;

            return (float)CurrentHp / MaxHp;
        }
    }
}
