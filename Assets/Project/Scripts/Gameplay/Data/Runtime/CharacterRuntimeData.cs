using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [System.Serializable]
    public class CharacterRuntimeData
    {
        public string CharacterId;

        public int Level = 1;
        public int Exp = 0;

        public int CurrentHealth;
        public int CurrentStamina;
        public int CurrentResource;
        public int CurrentMoveLevel;
        public int CurrentShield;

        public int ReservedHealthCost;
        public int ReservedStaminaCost;
        public int ReservedResourceCost;
        public int ReservedMoveCost;
        public int ReservedShieldCost;

        public List<StatusEffectRuntimeData> StatusEffects = new();

        public string MoveSkillId = "S_Move_1";
        public string PassiveSkillId;

        public string AbilitySkillId1;
        public string AbilitySkillId2;
        public string AbilitySkillId3;

        public string UniqueSkillId;

        public string[] EquippedSkillIds = new string[4];
        public string[] EquippedRuneIds = new string[4];
        public List<string> EquippedItemIds = new();

        public bool IsUnlocked;

        public int PreviewHealth => Mathf.Max(0, CurrentHealth - ReservedHealthCost);
        public int PreviewStamina => Mathf.Max(0, CurrentStamina - ReservedStaminaCost);
        public int PreviewResource => Mathf.Max(0, CurrentResource - ReservedResourceCost);
        public int PreviewMoveLevel => Mathf.Max(0, CurrentMoveLevel - ReservedMoveCost);
        public int PreviewShield => Mathf.Max(0, CurrentShield - ReservedShieldCost);

        public bool CanReserveHealth(int cost)
        {
            return cost <= 0 || CurrentHealth - ReservedHealthCost >= cost;
        }

        public bool CanReserveStamina(int cost)
        {
            return cost <= 0 || CurrentStamina - ReservedStaminaCost >= cost;
        }

        public bool CanReserveResource(int cost)
        {
            return cost <= 0 || CurrentResource - ReservedResourceCost >= cost;
        }

        public bool CanReserveMove(int cost)
        {
            return cost <= 0 || CurrentMoveLevel - ReservedMoveCost >= cost;
        }

        public bool CanReserveShield(int cost)
        {
            return cost <= 0 || CurrentShield - ReservedShieldCost >= cost;
        }

        public void AddReservedHealth(int cost)
        {
            ReservedHealthCost = Mathf.Clamp(ReservedHealthCost + Mathf.Max(0, cost), 0, CurrentHealth);
        }

        public void AddReservedStamina(int cost)
        {
            ReservedStaminaCost = Mathf.Clamp(ReservedStaminaCost + Mathf.Max(0, cost), 0, CurrentStamina);
        }

        public void AddReservedResource(int cost)
        {
            ReservedResourceCost = Mathf.Clamp(ReservedResourceCost + Mathf.Max(0, cost), 0, CurrentResource);
        }

        public void AddReservedMove(int cost)
        {
            ReservedMoveCost = Mathf.Clamp(ReservedMoveCost + Mathf.Max(0, cost), 0, CurrentMoveLevel);
        }

        public void AddReservedShield(int cost)
        {
            ReservedShieldCost = Mathf.Clamp(ReservedShieldCost + Mathf.Max(0, cost), 0, CurrentShield);
        }

        public void RemoveReservedHealth(int cost)
        {
            ReservedHealthCost = Mathf.Max(0, ReservedHealthCost - Mathf.Max(0, cost));
        }

        public void RemoveReservedStamina(int cost)
        {
            ReservedStaminaCost = Mathf.Max(0, ReservedStaminaCost - Mathf.Max(0, cost));
        }

        public void RemoveReservedResource(int cost)
        {
            ReservedResourceCost = Mathf.Max(0, ReservedResourceCost - Mathf.Max(0, cost));
        }

        public void RemoveReservedMove(int cost)
        {
            ReservedMoveCost = Mathf.Max(0, ReservedMoveCost - Mathf.Max(0, cost));
        }

        public void RemoveReservedShield(int cost)
        {
            ReservedShieldCost = Mathf.Max(0, ReservedShieldCost - Mathf.Max(0, cost));
        }
        public void ClearReservedCosts()
        {
            ReservedHealthCost = 0;
            ReservedStaminaCost = 0;
            ReservedResourceCost = 0;
            ReservedMoveCost = 0;
            ReservedShieldCost = 0;
        }

        public void ApplyReservedCosts()
        {
            CurrentHealth = PreviewHealth;
            CurrentStamina = PreviewStamina;
            CurrentResource = PreviewResource;
            CurrentMoveLevel = PreviewMoveLevel;
            CurrentShield = PreviewShield;

            ClearReservedCosts();
        }
    }
}