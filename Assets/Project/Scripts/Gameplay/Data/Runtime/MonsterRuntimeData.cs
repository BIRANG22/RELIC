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
        public int RemainingInnateShield;

        public int MinRemnant;
        public int MaxRemnant;
        public string UniqueItemId;
        public float UniqueItemChance;
        public float RelicChance;
        public string AttackRangeId;
        public string SpecialAction1;
        public string SpecialAction2;

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
            RemainingInnateShield = CurrentShield;

            MinRemnant = masterData.MinRemnant;
            MaxRemnant = masterData.MaxRemnant;
            UniqueItemId = masterData.UniqueItemId;
            UniqueItemChance = masterData.UniqueItemChance;
            RelicChance = masterData.RelicChance;
            AttackRangeId = masterData.AttackRangeId;
            SpecialAction1 = masterData.SpecialAction1;
            SpecialAction2 = masterData.SpecialAction2;

            TurnCount = 0;
            InitializePossibleSkills(masterData);
            InitializeMonsterTraits();
        }


        public string GetDisplayName()
        {
            string localizedBaseName = GameDataLocalization.MonsterName(MonsterId, Name);

            if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                if (DisplayName == Name)
                    return localizedBaseName;

                if (!string.IsNullOrWhiteSpace(Name) && DisplayName.StartsWith(Name + "_", StringComparison.Ordinal))
                    return localizedBaseName + DisplayName.Substring(Name.Length);

                return DisplayName;
            }

            return localizedBaseName;
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

        public int AbsorbShieldDamage(int damage)
        {
            damage = Math.Max(0, damage);

            if (damage <= 0 || CurrentShield <= 0)
                return 0;

            int shieldBefore = CurrentShield;
            int temporaryShield = Math.Max(0, CurrentShield - RemainingInnateShield);
            int temporaryDamage = Math.Min(temporaryShield, damage);

            CurrentShield -= temporaryDamage;
            damage -= temporaryDamage;

            if (damage > 0 && RemainingInnateShield > 0)
            {
                int innateDamage = Math.Min(RemainingInnateShield, damage);
                RemainingInnateShield -= innateDamage;
                CurrentShield -= innateDamage;
            }

            CurrentShield = Math.Max(0, CurrentShield);
            RemainingInnateShield = Math.Max(0, Math.Min(RemainingInnateShield, CurrentShield));
            return shieldBefore - CurrentShield;
        }

        public void AddTemporaryShield(int amount)
        {
            if (amount <= 0)
                return;

            CurrentShield += amount;
        }

        public void ClearTemporaryShield()
        {
            CurrentShield = Math.Max(0, RemainingInnateShield);
        }

        public void ClearAllShield()
        {
            CurrentShield = 0;
            RemainingInnateShield = 0;
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

        public int GetPresentationActionIndexForSkill(string skillId)
        {
            int originalActionIndex = GetActionIndexForSkill(skillId);

            if (originalActionIndex <= 0 || DataManager.Instance?.MonsterSkillDatabase == null)
                return originalActionIndex;

            string normalizedSkillId = skillId.Trim();
            MonsterSkillData selectedSkillData =
                DataManager.Instance.MonsterSkillDatabase.Get(normalizedSkillId);

            // 이동은 공통 Move 상태를 사용하므로 프레젠테이션 인덱스를 사용하지 않습니다.
            if (IsActualMoveSkill(selectedSkillData))
                return 0;

            bool selectedIsAttack =
                selectedSkillData != null &&
                selectedSkillData.TimelineNotation == TimelineActionType.Attack;

            // 공격은 공격끼리, 비공격 행동은 비공격 행동끼리 보유 순서대로 세어
            // 각각의 Presentation 1, 2, 3...에 연결합니다.
            int presentationActionIndex = 0;

            for (int i = 0; i < PossibleSkillIdsByActionIndex.Length; i++)
            {
                string possibleSkillId = PossibleSkillIdsByActionIndex[i];

                if (string.IsNullOrWhiteSpace(possibleSkillId))
                    continue;

                MonsterSkillData possibleSkillData =
                    DataManager.Instance.MonsterSkillDatabase.Get(possibleSkillId);

                // 이동 스킬은 별도의 Presentation 순번에서 제외합니다.
                if (IsActualMoveSkill(possibleSkillData))
                    continue;

                bool possibleIsAttack =
                    possibleSkillData != null &&
                    possibleSkillData.TimelineNotation == TimelineActionType.Attack;

                if (possibleIsAttack == selectedIsAttack)
                    presentationActionIndex++;

                if (string.Equals(possibleSkillId, normalizedSkillId, StringComparison.Ordinal))
                    return presentationActionIndex;
            }

            return originalActionIndex;
        }



        private static bool IsActualMoveSkill(MonsterSkillData skillData)
        {
            if (skillData == null)
                return false;

            if (skillData.TimelineNotation == TimelineActionType.Move)
                return true;

            if (string.IsNullOrWhiteSpace(skillData.EffectIds))
                return false;

            string[] effectIds = skillData.EffectIds.Split(',', ';');

            for (int i = 0; i < effectIds.Length; i++)
            {
                if (string.Equals(effectIds[i].Trim(), "E_Move", StringComparison.Ordinal))
                    return true;
            }

            return false;
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
                StatusEffects.Add(new StatusEffectRuntimeData("E_Explode", 2));
            }
            else if (MonsterId == "Mon_10")
            {
                // 녹턴은 전투 내내 유지되는 기습 효과를 기본 특성으로 가집니다.
                StatusEffects.Add(new StatusEffectRuntimeData("E_Flank", 1));
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

