using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MonsterRuntimeData
    {
        // 전투 중 개별 몬스터 고유 ID
        // 예: RuntimeMonster_0001, RuntimeMonster_0002
        public string RuntimeId;

        // 원본 마스터 데이터 ID
        // 예: Mon_01, Mon_12
        public string MonsterId;

        public string Name;
        public string Grade;

        public int MaxHp;
        public int CurrentHp;
        public int CurrentShield;

        public string DropTableId;

        // 몬스터가 실제로 사용 가능한 스킬 목록
        public List<string> PossSkillIds = new();

        // 전투 중 턴 카운트
        public int TurnCount;

        // 생존 여부
        public bool IsDead => CurrentHp <= 0;

        // 버프 / 디버프는 나중에 전용 RuntimeData로 확장 가능
        public List<StatusEffectRuntimeData> StatusEffects = new();

        public MonsterRuntimeData()
        {
        }

        public MonsterRuntimeData(string runtimeId, MonsterMasterData masterData)
        {
            RuntimeId = runtimeId;

            MonsterId = masterData.MonsterId;
            Name = masterData.Name;
            Grade = masterData.Grade;

            MaxHp = masterData.Health;
            CurrentHp = masterData.Health;

            DropTableId = masterData.DropTableId;

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