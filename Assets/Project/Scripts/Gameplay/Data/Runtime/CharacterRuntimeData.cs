using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Relic.Gameplay.Data
{
    [System.Serializable]
    public class CharacterRuntimeData
    {
        public string CharacterId;

        public int Level = 1;
        public int Exp = 0;

        [FormerlySerializedAs("MaxHealth")]
        public int MaxHP;
        [FormerlySerializedAs("MaxStamina")]
        public int MaxCost;
        [FormerlySerializedAs("StaminaRecovery")]
        public int CostRecovery;
        [FormerlySerializedAs("BonusStaminaRecovery")]
        public int BonusCostRecovery;

        [FormerlySerializedAs("CurrentHealth")]
        public int CurrentHP;
        [FormerlySerializedAs("CurrentStamina")]
        public int CurrentCost;
        public int CurrentResource;
        public int CurrentMoveLevel;
        public int CurrentShield;

        [FormerlySerializedAs("ReservedHealthCost")]
        public int ReservedHPCost;
        [FormerlySerializedAs("ReservedStaminaCost")]
        public int ReservedCost;
        public int ReservedResourceCost;
        public int ReservedShieldCost;

        public BattleDirection Direction = BattleDirection.Right;

        public List<StatusEffectRuntimeData> StatusEffects = new();

        public string MoveSkillId = "S_Move_1";

        public string PassiveSkillId;
        public string UniqueSkillId;
        public string AbilitySkillId;

        public string[] EquippedSkillIds = new string[4];
        public string[] EquippedRuneIds = new string[12];
        public string[] EquippedRelicIds = new string[5];
        public List<ActiveRelicUseRuntimeData> ActiveRelicUses = new();
        public List<string> AppliedBattleEquipmentEffectIds = new();

        public bool IsUnlocked;

        public int TotalCostRecovery => Mathf.Max(0, CostRecovery + BonusCostRecovery);
        public bool IsDead => CurrentHP <= 0;
        public int PreviewHP => Mathf.Max(0, CurrentHP - ReservedHPCost);
        public int PreviewCost => Mathf.Max(0, CurrentCost - ReservedCost);
        public int PreviewResource => Mathf.Max(0, CurrentResource - ReservedResourceCost);
        public int PreviewShield => Mathf.Max(0, CurrentShield - ReservedShieldCost);

        public bool CanReserveHP(int cost)
        {
            if (IsDead)
                return false;

            return cost <= 0 || CurrentHP - ReservedHPCost > cost;
        }

        public bool CanReserveCost(int cost)
        {
            if (IsDead)
                return false;

            return cost <= 0 || CurrentCost - ReservedCost >= cost;
        }

        public bool CanReserveResource(int cost)
        {
            if (IsDead)
                return false;

            return cost <= 0 || CurrentResource - ReservedResourceCost >= cost;
        }

        public bool CanReserveShield(int cost)
        {
            if (IsDead)
                return false;

            return cost <= 0 || CurrentShield - ReservedShieldCost >= cost;
        }

        public void AddReservedHP(int cost)
        {
            ReservedHPCost = Mathf.Clamp(ReservedHPCost + Mathf.Max(0, cost), 0, CurrentHP);
        }

        public void AddReservedCost(int cost)
        {
            ReservedCost = Mathf.Clamp(ReservedCost + Mathf.Max(0, cost), 0, CurrentCost);
        }

        public void AddReservedResource(int cost)
        {
            ReservedResourceCost = Mathf.Clamp(ReservedResourceCost + Mathf.Max(0, cost), 0, CurrentResource);
        }

        public void AddReservedShield(int cost)
        {
            ReservedShieldCost = Mathf.Clamp(ReservedShieldCost + Mathf.Max(0, cost), 0, CurrentShield);
        }

        public void RemoveReservedHP(int cost)
        {
            ReservedHPCost = Mathf.Max(0, ReservedHPCost - Mathf.Max(0, cost));
        }

        public void RemoveReservedCost(int cost)
        {
            ReservedCost = Mathf.Max(0, ReservedCost - Mathf.Max(0, cost));
        }

        public void RemoveReservedResource(int cost)
        {
            ReservedResourceCost = Mathf.Max(0, ReservedResourceCost - Mathf.Max(0, cost));
        }

        public void RemoveReservedShield(int cost)
        {
            ReservedShieldCost = Mathf.Max(0, ReservedShieldCost - Mathf.Max(0, cost));
        }
        public void ClearReservedCosts()
        {
            ReservedHPCost = 0;
            ReservedCost = 0;
            ReservedResourceCost = 0;
            ReservedShieldCost = 0;
        }

        public void ClearBattleRoomTemporaryStatusEffects()
        {
            CurrentShield = 0;
            ClearReservedCosts();

            if (StatusEffects != null)
                StatusEffects.Clear();
        }

        public void HandleDeath()
        {
            CurrentHP = 0;
            CurrentShield = 0;
            ClearReservedCosts();

            if (StatusEffects != null)
                StatusEffects.Clear();
        }

        public void ApplyReservedCosts()
        {
            CurrentHP = PreviewHP;
            CurrentCost = PreviewCost;
            CurrentResource = PreviewResource;
            CurrentShield = PreviewShield;

            ClearReservedCosts();

            if (IsDead)
                HandleDeath();
        }
    }
}
