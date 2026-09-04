using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public static class BattleEquipmentEffectService
{
    private const int LastTimelineSlotIndex = 4;
    private const string MoveSkillLevelOneId = "S_Move_1";
    private const string Relic06Turn2ArmorAppliedId = "Relic_06_Turn2Armor";
    private const string MaxHpEffectId = "E_Max_HP";
    private const string MaxCostEffectId = "E_Max_Cost";
    private const string MoveValueEffectId = "E_Move_Value";
    private const string CostRecoveryDeltaEffectId = "E_Cost_Recovery_Delta";
    private const string BattleStartCostEffectId = "E_Battle_Start_Cost";
    private const string BattleStartUniqueResourceEffectId = "E_Battle_Start_UniqueResource";
    private const string UniqueResourceGainIfEmptyDeltaEffectId = "E_UniqueResource_Gain_If_Empty_Delta";
    private const string UniqueResourceMinUseDeltaEffectId = "E_UniqueResource_Min_Use_Delta";
    private const string UniqueResourceOverflowToCostEffectId = "E_UniqueResource_Overflow_To_Cost";
    private const string UniqueResourceMaxSkillCostDeltaEffectId = "E_UniqueResource_Max_Skill_Cost_Delta";
    private const string UniqueResource3TurnStartAimingEffectId = "E_UniqueResource3_Turn_Start_Aiming";
    private const string UniqueResource3TurnStartArmorEffectId = "E_UniqueResource3_Turn_Start_Armor";
    private const string UniqueResource3TurnStartBoostEffectId = "E_UniqueResource3_Turn_Start_Boost";
    private const string UniqueResource5TurnStartBoostEffectId = "E_UniqueResource5_Turn_Start_Boost";
    private const string RangeDeltaEffectId = "E_Range_Delta";
    private const string FirstMoveCostDeltaEffectId = "E_First_Move_Cost_Delta";
    private const string MoveCostDeltaEffectId = "E_Move_Cost_Delta";
    private const string ArmorGainDeltaEffectId = "E_Armor_Gain_Delta";
    private const string ArmorGainPercentEffectId = "E_Armor_Gain_Percent";
    private const string AttackCostDeltaEffectId = "E_Attack_Cost_Delta";
    private const string BuffCostDeltaEffectId = "E_Buff_Cost_Delta";
    private const string DebuffCostDeltaEffectId = "E_Debuff_Cost_Delta";
    private const string MoveCostZeroEffectId = "E_Move_Cost_Zero";
    private const string BlockedSlotMaskEffectId = "E_Blocked_Slot_Mask";
    private const string MaxRegistrableSlotCountEffectId = "E_Max_Registrable_Slot_Count";
    private const string NoMoveInSlotBuffCostDeltaEffectId = "E_No_Move_In_Slot_Buff_Cost_Delta";
    private const string AfterMoveDebuffCostDeltaEffectId = "E_After_Move_Debuff_Cost_Delta";
    private const string Slot1SkillCostDeltaEffectId = "E_Slot1_Skill_Cost_Delta";
    private const string Slot1FirstSkillCostDeltaEffectId = "E_Slot1_First_Skill_Cost_Delta";
    private const string Slot5SkillCostDeltaEffectId = "E_Slot5_Skill_Cost_Delta";
    private const string LastSlotDuplicateCostIgnoreEffectId = "E_Last_Slot_Duplicate_Cost_Ignore";
    private const string LowHpSkillCostDeltaEffectId = "E_Low_HP_Skill_Cost_Delta";
    private const string HighHpCostRecoveryDeltaEffectId = "E_High_HP_Cost_Recovery_Delta";
    private const string HighHpBuffCostDeltaEffectId = "E_High_HP_Buff_Cost_Delta";
    private const string AttackValueDeltaEffectId = "E_Attack_Value_Delta";
    private const string AttackCountDeltaEffectId = "E_Attack_Count_Delta";
    private const string RandomAttackCountDeltaEffectId = "E_Random_Attack_Count_Delta";
    private const string BuffValueDeltaEffectId = "E_Buff_Value_Delta";
    private const string DebuffValueDeltaEffectId = "E_Debuff_Value_Delta";
    private const string AfterMoveDebuffValueDeltaEffectId = "E_After_Move_Debuff_Value_Delta";
    private const string AfterMoveAttackValuePerCostEffectId = "E_After_Move_Attack_Value_Per_Cost";
    private const string Slot1AttackValueDeltaEffectId = "E_Slot1_Attack_Value_Delta";
    private const string Slot1BuffValueDeltaEffectId = "E_Slot1_Buff_Value_Delta";
    private const string Slot1DebuffValueDeltaEffectId = "E_Slot1_Debuff_Value_Delta";
    private const string Slot5AttackValueDeltaEffectId = "E_Slot5_Attack_Value_Delta";
    private const string LowHpAttackValueDeltaEffectId = "E_Low_HP_Attack_Value_Delta";
    private const string SelfBuffBlockedBuffValueDeltaEffectId = "E_Self_Buff_Blocked_Buff_Value_Delta";
    private const string FullHpBuffValueDeltaEffectId = "E_Full_HP_Buff_Value_Delta";
    private const string UniqueSkillValuePercentEffectId = "E_Unique_Skill_Value_Percent";
    private const string UniqueSkillCountDeltaEffectId = "E_Unique_Skill_Count_Delta";
    private const string Slot5AttackPierceEffectId = "E_Slot5_Attack_Pierce";
    private const string Slot1AttackKillFocusEffectId = "E_Slot1_Attack_Kill_Focus";
    private const string Slot5AttackKillCostRefundEffectId = "E_Slot5_Attack_Kill_Cost_Refund";
    private const string UniqueSkillKillFocusEffectId = "E_Unique_Skill_Kill_Focus";
    private const string DamagePoisonedFixedEffectId = "E_Damage_Poisoned_Fixed";
    private const string DamageBleedingFixedEffectId = "E_Damage_Bleeding_Fixed";
    private const string DamageVulnerableArmorEffectId = "E_Damage_Vulnerable_Armor";
    private const string DamageWeakenedArmorEffectId = "E_Damage_Weakened_Armor";
    private const string DamageNonPoisonedApplyPoisonEffectId = "E_Damage_Non_Poisoned_Apply_Poison";
    private const string AllyBuffChargeEffectId = "E_Ally_Buff_Charge";
    private const string LowHpDamageReductionPercentEffectId = "E_Low_HP_Damage_Reduction_Percent";
    private const string ForcedMoveImmuneEffectId = "E_Forced_Move_Immune";
    private const string CrashDamageImmuneEffectId = "E_Crash_Damage_Immune";
    private const string GridEffectImmuneEffectId = "E_Grid_Effect_Immune";
    private const string OverhealToArmorEffectId = "E_Overheal_To_Armor";
    private const string HealingBlockedEffectId = "E_Healing_Blocked";
    private const string NoDamagePreviousTurnSmiteEffectId = "E_No_Damage_Previous_Turn_Smite";
    private const string OnceBattleEndTurnZeroCostChargeEffectId = "E_Once_Battle_End_Turn_Zero_Cost_Charge";
    private const string OnceBattleAttackMissChargeEffectId = "E_Once_Battle_Attack_Miss_Charge";
    private const string OnceBattleFullHpTargetDamagePercentEffectId = "E_Once_Battle_Full_HP_Target_Damage_Percent";
    private const string DamageTakenThisTurnStateId = "State_DamageTakenThisTurn";
    private const string TurnStartArmorEffectId = "E_Turn_Start_Armor";
    private const string TurnStartArmorPerUsedSlotEffectId = "E_Turn_Start_Armor_Per_Used_Slot";
    private const string TurnStartArmorPerEmptySlotEffectId = "E_Turn_Start_Armor_Per_Empty_Slot";
    private const string TurnStartChargeEffectId = "E_Turn_Start_Charge";
    private const string TurnStartFocusEffectId = "E_Turn_Start_Focus";
    private const string TurnStartCostRecoveryEffectId = "E_Turn_Start_Cost_Recovery";
    private const string TurnStartUniqueResourceRecoveryEffectId = "E_Turn_Start_Unique_Resource_Recovery";
    private const string TurnStartSwiftEffectId = "E_Turn_Start_Swift";
    private const string TurnStartBoostEffectId = "E_Turn_Start_Boost";
    private const string TurnStartSmiteEffectId = "E_Turn_Start_Smite";
    private const string TurnStartLifestealEffectId = "E_Turn_Start_Lifesteal";
    private const string LifestealEffectId = "E_Lifesteal";
    private const string TurnStartSmiteIfSlotsEmptyMaskEffectId = "E_Turn_Start_Smite_If_Slots_Empty_Mask";
    private const string TurnStartBoostIfSlotsEmptyMaskEffectId = "E_Turn_Start_Boost_If_Slots_Empty_Mask";
    private const string TurnStartBoostIfOneAttackCommandEffectId = "E_Turn_Start_Boost_If_One_Attack_Command";
    private const string TurnStartBoostIfAttackCountAtLeastEffectId = "E_Turn_Start_Boost_If_Attack_Command_Count_At_Least";
    private const string TurnStartAllEnemyVulnerableEffectId = "E_Turn_Start_All_Enemy_Vulnerable";
    private const string TurnStartAllEnemyWeakenEffectId = "E_Turn_Start_All_Enemy_Weaken";
    private const string TurnStartAllEnemyPoisonEffectId = "E_Turn_Start_All_Enemy_Poison";
    private const string TurnStartAllEnemyBleedEffectId = "E_Turn_Start_All_Enemy_Bleed";
    private const string CollisionTargetDamageDeltaEffectId = "E_Collision_Target_Damage_Delta";
    private const string CollisionChargeEffectId = "E_Collision_Charge";
    private const string CollisionKillFocusEffectId = "E_Collision_Kill_Focus";
    private const string RestHealPercentBonusEffectId = "E_Rest_Heal_Percent_Bonus";
    private const string ShopPriceDiscountPercentEffectId = "E_Shop_Price_Discount_Percent";
    private const string BattleRewardCurrencyPercentEffectId = "E_Battle_Reward_Currency_Percent";
    private const string BattleEndHealEffectId = "E_Battle_End_Heal";
    private const string HealValueDeltaEffectId = "E_Heal_Value_Delta";
    private const string SkillArmorValueDeltaEffectId = "E_Skill_Armor_Value_Delta";
    private const string PoisonValueDeltaEffectId = "E_Poison_Value_Delta";
    private const string BleedValueDeltaEffectId = "E_Bleed_Value_Delta";
    private const string ExplorationStartUniqueResourceEffectId = "E_Exploration_Start_Unique_Resource";
    private const string UniqueResourceMaxReachedBoostEffectId = "E_Unique_Resource_Max_Reached_Boost";
    private const string UniqueResourceMaxReachedSmiteEffectId = "E_Unique_Resource_Max_Reached_Smite";
    private const string UniqueResourceMaxReachedCostRecoveryEffectId = "E_Unique_Resource_Max_Reached_Cost_Recovery";
    private const string UniqueResourceMaxReachedArmorEffectId = "E_Unique_Resource_Max_Reached_Armor";
    private const string UniqueResourceMaxReachedSelfLowestAllyArmorEffectId = "E_Unique_Resource_Max_Reached_Self_Lowest_Ally_Armor";
    private const string RuneTargetDamageValueDeltaEffectId = "E_Rune_Target_Damage_Value_Delta";
    private const string RuneTargetKnockbackValueDeltaEffectId = "E_Rune_Target_Knockback_Value_Delta";
    private const string RuneTargetAttackCountDeltaEffectId = "E_Rune_Target_Attack_Count_Delta";
    private const string RuneTargetSkillCostDeltaEffectId = "E_Rune_Target_Skill_Cost_Delta";
    private const string RuneTargetArmorMultiplierEffectId = "E_Rune_Target_Armor_Multiplier";
    private const string RuneTargetBoostValueDeltaEffectId = "E_Rune_Target_Boost_Value_Delta";
    private const string RuneTargetHealValueDeltaEffectId = "E_Rune_Target_Heal_Value_Delta";
    private const string RuneTargetRangeExpandEffectId = "E_Rune_Target_Range_Expand";
    private const string MoveFirstAttackPowerEffectId = "E_Move_First_Attack_Power";
    private const string PoisonApplyDoubleEffectId = "E_Poison_Apply_Double";
    private const string BleedingApplyDoubleEffectId = "E_Bleeding_Apply_Double";
    private const string MaxHpUpEffectId = "E_Max_HP_Up";
    private const string KillHealEffectId = "E_Kill_Heal";
    private const string SkillResourceGainUpEffectId = "E_Skill_Resource_Gain_Up";
    private const string BuffApplyDoubleEffectId = "E_Buff_Apply_Double";
    private const string MovePointUpEffectId = "E_Move_Point_Up";
    private const string MoveFirstAttackReadyStateId = "State_MoveFirstAttackPowerReady";
    private const string SpiderWebMoveCostPenaltyEffectId = "E_Spider_Web";
    private const int SpiderWebActiveTurnCount = 1;
    private const int SpiderWebMoveCostMultiplier = 2;

    private readonly struct EquipmentEffectEntry
    {
        public readonly string SourceId;
        public readonly SkillEffectEntry Entry;

        public EquipmentEffectEntry(string sourceId, SkillEffectEntry entry)
        {
            SourceId = sourceId;
            Entry = entry;
        }
    }

    public static void ApplyBattleStartEffects(
        CharacterRuntimeData runtime,
        CharacterMasterData masterData)
    {
        if (runtime == null)
            return;

        ResetBattleOnlyEffectState(runtime);

        int baseMaxHP = GetRunAdjustedBaseMaxHP(runtime, masterData);

        int previousMaxHP = runtime.MaxHP > 0
            ? runtime.MaxHP
            : baseMaxHP;

        bool wasDead = runtime.CurrentHP <= 0;
        bool shouldFillHP = !wasDead && runtime.CurrentHP >= previousMaxHP;

        runtime.MaxHP = Mathf.Max(1, baseMaxHP + GetMaxHPBonus(runtime, baseMaxHP));

        if (wasDead)
            runtime.CurrentHP = 1;
        else if (shouldFillHP)
            runtime.CurrentHP = runtime.MaxHP;
        else
            runtime.CurrentHP = Mathf.Clamp(runtime.CurrentHP, 1, runtime.MaxHP);

        int baseMaxCost = GetRunAdjustedBaseMaxCost(runtime, masterData);

        runtime.MaxCost = Mathf.Max(0, baseMaxCost + GetMaxCostBonus(runtime));
        runtime.CurrentCost = Mathf.Max(0, runtime.MaxCost + GetBattleStartCostBonus(runtime));

        int maxResource = masterData != null
            ? Mathf.Max(0, masterData.MaxResource)
            : Mathf.Max(0, runtime.CurrentResource);
        int explorationStartResource = ConsumeExplorationStartUniqueResource(runtime);
        if (explorationStartResource > 0)
        {
            int previousResource = Mathf.Max(0, runtime.CurrentResource);
            ApplyUniqueResourceGainSideEffects(
                runtime, explorationStartResource, previousResource, maxResource);
            runtime.CurrentResource = Mathf.Clamp(
                previousResource + explorationStartResource, 0, maxResource);
        }

        int battleStartResource = GetBattleStartUniqueResource(runtime, masterData);
        runtime.CurrentResource = Mathf.Clamp(Mathf.Max(runtime.CurrentResource, battleStartResource), 0, maxResource);
        runtime.CurrentMoveLevel = Mathf.Max(0, GetEffectiveMoveValue(runtime, masterData));
        SyncMoveSkillForMoveValue(runtime);
        runtime.ClearReservedCosts();
    }

    public static void SyncMoveSkillForMoveValue(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return;

        runtime.MoveSkillId = MoveSkillLevelOneId;
    }

    public static int GetEffectiveMaxHP(
        CharacterRuntimeData runtime,
        CharacterMasterData masterData)
    {
        int baseMaxHP = GetRunAdjustedBaseMaxHP(runtime, masterData);

        return Mathf.Max(1, baseMaxHP + GetMaxHPBonus(runtime, baseMaxHP));
    }

    public static int GetEffectiveMaxCost(
        CharacterRuntimeData runtime,
        CharacterMasterData masterData)
    {
        int baseMaxCost = GetRunAdjustedBaseMaxCost(runtime, masterData);

        return Mathf.Max(0, baseMaxCost + GetMaxCostBonus(runtime));
    }

    public static int GetEffectiveCostRecovery(
        CharacterRuntimeData runtime,
        CharacterMasterData masterData)
    {
        int baseRecovery = masterData != null
            ? Mathf.Max(0, masterData.CostRecovery)
            : runtime != null ? Mathf.Max(0, runtime.CostRecovery) : 0;

        int bonusRecovery = runtime != null ? runtime.BonusCostRecovery : 0;

        return Mathf.Max(0, baseRecovery + bonusRecovery + GetCostRecoveryBonus(runtime));
    }

    private static int GetRunAdjustedBaseMaxHP(
        CharacterRuntimeData runtime,
        CharacterMasterData masterData)
    {
        int runBonus = runtime != null ? runtime.RunMaxHPBonus : 0;
        int baseMaxHP = masterData != null
            ? Mathf.Max(1, masterData.MaxHP)
            : runtime != null ? Mathf.Max(1, runtime.MaxHP - runBonus) : 1;

        return Mathf.Max(1, baseMaxHP + runBonus);
    }

    private static int GetRunAdjustedBaseMaxCost(
        CharacterRuntimeData runtime,
        CharacterMasterData masterData)
    {
        int runBonus = runtime != null ? runtime.RunMaxCostBonus : 0;
        int baseMaxCost = masterData != null
            ? Mathf.Max(0, masterData.MaxCost)
            : runtime != null ? Mathf.Max(0, runtime.MaxCost - runBonus) : 0;

        return Mathf.Max(0, baseMaxCost + runBonus);
    }

    public static void ResetBattleOnlyEffectState(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return;

        if (runtime.AppliedBattleEquipmentEffectIds == null)
            runtime.AppliedBattleEquipmentEffectIds = new List<string>();
        else
            runtime.AppliedBattleEquipmentEffectIds.Clear();

        runtime.PendingNextTurnSmiteStack = 0;
        runtime.PendingNextTurnSmiteTurn = 0;
        runtime.PendingNextTurnBoostStack = 0;
        runtime.PendingNextTurnBoostTurn = 0;

        runtime.PendingUniqueResourceMaxBoost = 0;
        runtime.PendingUniqueResourceMaxSmite = 0;
        runtime.PendingUniqueResourceMaxCostRecovery = 0;
        runtime.PendingUniqueResourceMaxArmor = 0;
        runtime.PendingUniqueResourceMaxSelfLowestAllyArmor = 0;
    }

    public static void MarkMovedBeforeNextAttack(CharacterRuntimeData runtime)
    {
        if (GetStatusStack(runtime, MoveFirstAttackPowerEffectId) <= 0)
            return;

        TryMarkBattleEffectApplied(runtime, MoveFirstAttackReadyStateId);
    }

    public static bool IsMoveFirstAttackPowerReady(CharacterRuntimeData runtime)
    {
        if (GetStatusStack(runtime, MoveFirstAttackPowerEffectId) <= 0)
            return false;

        return HasBattleEffectApplied(runtime, MoveFirstAttackReadyStateId);
    }

    public static void ClearMoveFirstAttackPowerIfAttack(
        CharacterRuntimeData runtime,
        SkillMasterData skillData)
    {
        if (runtime == null || skillData == null)
            return;

        if (skillData.SkillType != SkillType.Attack)
            return;

        RemoveBattleEffectApplied(runtime, MoveFirstAttackReadyStateId);
    }

    public static void ApplyPlayerLifesteal(BattleEffectContext context, int dealtDamage)
    {
        if (context == null ||
            context.PlayerCaster == null ||
            context.PlayerCaster.RuntimeData == null ||
            dealtDamage <= 0)
        {
            return;
        }

        if (GetStatusStack(context.PlayerCaster.RuntimeData, LifestealEffectId) <= 0)
            return;

        int healAmount = Mathf.CeilToInt(dealtDamage * 0.3f);

        if (healAmount > 0)
            BattleEffectUtility.HealPlayer(context.PlayerCaster, healAmount);
    }

    public static int GetKillHealAmount(CharacterRuntimeData runtime)
    {
        int stack = GetStatusStack(runtime, KillHealEffectId);

        if (runtime == null || stack <= 0)
            return 0;

        int maxHP = Mathf.Max(0, runtime.MaxHP);

        if (maxHP <= 0)
            return 0;

        return Mathf.Max(1, Mathf.CeilToInt(maxHP * 0.05f * stack));
    }

    public static void ApplyPlayerTurnStartEffects(
        CharacterRuntimeData runtime,
        int playerTurnNumber)
    {
        if (runtime == null)
            return;

        ApplyQueuedNextTurnSmite(runtime, playerTurnNumber);
        ApplyQueuedNextTurnBoost(runtime, playerTurnNumber);
        ApplyQueuedUniqueResourceMaxRuneEffects(runtime);
        ApplyConfiguredTurnStartEffects(runtime, playerTurnNumber);
        ApplyNoDamagePreviousTurnEffect(runtime, playerTurnNumber);

        if (playerTurnNumber == 2 &&
            HasRelic(runtime, "Relic_06") &&
            !HasConfiguredRelicEffect(runtime, "Relic_06", TurnStartArmorEffectId) &&
            TryMarkBattleEffectApplied(runtime, Relic06Turn2ArmorAppliedId))
        {
            int gainedArmor = ModifyArmorGain(runtime, 10);
            runtime.CurrentShield += gainedArmor;
            BattleDamageTextPopupUI.ShowArmorGain(runtime.CharacterId, gainedArmor);
        }
    }

    public static int GetEffectiveMoveValue(
        CharacterRuntimeData runtime,
        CharacterMasterData masterData)
    {
        // 캐릭터 마스터의 기본 이동값은 더 이상 사용하지 않습니다.
        // 이동 단계는 장비/룬 등 E_Move_Value 효과의 합계로만 결정합니다.
        return Mathf.Max(0, GetMoveValueBonus(runtime));
    }

    public static int ModifyUniqueResourceGain(
        CharacterRuntimeData runtime,
        int amount)
    {
        int safeAmount = Mathf.Max(0, amount);

        if (runtime == null || safeAmount <= 0)
            return safeAmount;

        if (runtime.CurrentResource <= 0)
            safeAmount += SumConfiguredEffectValues(runtime, UniqueResourceGainIfEmptyDeltaEffectId);

        return safeAmount;
    }

    public static void ApplyUniqueResourceGainSideEffects(
        CharacterRuntimeData runtime,
        int requestedAmount,
        int previousResource,
        int maxResource)
    {
        if (runtime == null || requestedAmount <= 0 || maxResource <= 0)
            return;

        if (previousResource < maxResource &&
            previousResource + requestedAmount >= maxResource)
        {
            ApplyUniqueResourceMaxReachedRuneEffects(runtime);
        }

        if (previousResource < maxResource)
            return;

        int costRestorePerResource = SumConfiguredEffectValues(
            runtime,
            UniqueResourceOverflowToCostEffectId);

        if (costRestorePerResource <= 0)
            return;

        int restoredCost = requestedAmount * costRestorePerResource;
        runtime.CurrentCost = Mathf.Min(
            Mathf.Max(0, runtime.MaxCost),
            Mathf.Max(0, runtime.CurrentCost) + restoredCost);
    }

    private static int ConsumeExplorationStartUniqueResource(CharacterRuntimeData runtime)
    {
        if (runtime == null ||
            string.IsNullOrWhiteSpace(runtime.CharacterId) ||
            DataManager.Instance == null ||
            DataManager.Instance.BattleRuntimeStore == null)
        {
            return 0;
        }

        int amount = SumConfiguredEffectValues(runtime, ExplorationStartUniqueResourceEffectId);
        if (amount <= 0)
            return 0;

        BattleRuntimeData battleRuntime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();
        battleRuntime.AppliedExplorationStartRuneCharacterIds ??= new List<string>();

        string characterId = runtime.CharacterId.Trim();
        if (battleRuntime.AppliedExplorationStartRuneCharacterIds.Contains(characterId))
            return 0;

        battleRuntime.AppliedExplorationStartRuneCharacterIds.Add(characterId);
        DataManager.Instance.BattleRuntimeStore.Set(battleRuntime);
        return amount;
    }

    private static void ApplyUniqueResourceMaxReachedRuneEffects(CharacterRuntimeData runtime)
    {
        if (runtime == null || runtime.IsDead)
            return;

        // Rune_52 / 57 / 62 / 67 / 72
        // 카르마가 최대에 도달한 순간에는 효과를 바로 주지 않고,
        // 다음 플레이어 턴 시작 시 적용할 수치만 예약합니다.
        runtime.PendingUniqueResourceMaxBoost += Mathf.Max(
            0,
            SumConfiguredEffectValues(runtime, UniqueResourceMaxReachedBoostEffectId));

        runtime.PendingUniqueResourceMaxSmite += Mathf.Max(
            0,
            SumConfiguredEffectValues(runtime, UniqueResourceMaxReachedSmiteEffectId));

        runtime.PendingUniqueResourceMaxCostRecovery += Mathf.Max(
            0,
            SumConfiguredEffectValues(runtime, UniqueResourceMaxReachedCostRecoveryEffectId));

        runtime.PendingUniqueResourceMaxArmor += Mathf.Max(
            0,
            SumConfiguredEffectValues(runtime, UniqueResourceMaxReachedArmorEffectId));

        runtime.PendingUniqueResourceMaxSelfLowestAllyArmor += Mathf.Max(
            0,
            SumConfiguredEffectValues(runtime, UniqueResourceMaxReachedSelfLowestAllyArmorEffectId));
    }

    private static void ApplyQueuedUniqueResourceMaxRuneEffects(CharacterRuntimeData runtime)
    {
        if (runtime == null || runtime.IsDead)
            return;

        int boost = runtime.PendingUniqueResourceMaxBoost;
        int smite = runtime.PendingUniqueResourceMaxSmite;
        int costRecovery = runtime.PendingUniqueResourceMaxCostRecovery;
        int armor = runtime.PendingUniqueResourceMaxArmor;
        int partyArmor = runtime.PendingUniqueResourceMaxSelfLowestAllyArmor;

        runtime.PendingUniqueResourceMaxBoost = 0;
        runtime.PendingUniqueResourceMaxSmite = 0;
        runtime.PendingUniqueResourceMaxCostRecovery = 0;
        runtime.PendingUniqueResourceMaxArmor = 0;
        runtime.PendingUniqueResourceMaxSelfLowestAllyArmor = 0;

        if (boost > 0)
            AddTemporaryStatus(runtime, "E_Boost", boost, UniqueResourceMaxReachedBoostEffectId);

        if (smite > 0)
            AddTemporaryStatus(runtime, "E_Smite", smite, UniqueResourceMaxReachedSmiteEffectId);

        if (costRecovery > 0)
        {
            int maxCost = Mathf.Max(0, runtime.MaxCost);
            runtime.CurrentCost = Mathf.Min(maxCost, Mathf.Max(0, runtime.CurrentCost) + costRecovery);
        }

        if (armor > 0)
            AddRuneTriggeredArmor(runtime, armor);

        if (partyArmor > 0)
        {
            AddRuneTriggeredArmor(runtime, partyArmor);

            CharacterRuntimeData lowestAlly = FindLowestHpLivingAlly(runtime);
            if (lowestAlly != null)
                AddRuneTriggeredArmor(lowestAlly, partyArmor);
        }
    }

    private static void AddRuneTriggeredArmor(CharacterRuntimeData runtime, int amount)
    {
        if (runtime == null || runtime.IsDead || amount <= 0)
            return;

        int gainedArmor = ModifyArmorGain(runtime, amount);
        if (gainedArmor <= 0)
            return;

        runtime.CurrentShield += gainedArmor;
        BattleDamageTextPopupUI.ShowArmorGain(runtime.CharacterId, gainedArmor);
    }

    private static CharacterRuntimeData FindLowestHpLivingAlly(CharacterRuntimeData owner)
    {
        if (owner == null)
            return null;

        List<CharacterRuntimeData> party = GetCurrentPartyRuntimeData();
        CharacterRuntimeData best = null;
        int bestCurrentHp = int.MaxValue;

        for (int i = 0; i < party.Count; i++)
        {
            CharacterRuntimeData candidate = party[i];
            if (candidate == null || candidate.IsDead || candidate == owner || candidate.MaxHP <= 0)
                continue;

            int currentHp = Mathf.Max(0, candidate.CurrentHP);
            if (best == null || currentHp < bestCurrentHp)
            {
                best = candidate;
                bestCurrentHp = currentHp;
            }
        }

        return best;
    }

    public static void ApplyReservationCostModifiers(
        PlayerReservedCommand command,
        int slotIndex,
        bool isFirstMoveCommand,
        bool isLastTimelineSlot,
        bool isFirstSkillInSlot = true,
        bool hadEarlierMoveInSlot = false,
        int sameSlotMoveCostBeforeCommand = 0)
    {
        if (command == null)
            return;

        command.ResetCostsToBase();
        command.SetTimelineSlotIndex(slotIndex);
        command.SetSlotReservationContext(
            isFirstSkillInSlot,
            hadEarlierMoveInSlot,
            sameSlotMoveCostBeforeCommand);

        if (command.IsMoveContinuationCommand)
        {
            command.SetCosts(0, 0, 0, 0);
            command.MarkReservationCostModifiersApplied();
            return;
        }

        int hpCost = command.HPCost;
        int cost = command.Cost;
        int resourceCost = command.ResourceCost;
        int shieldCost = command.ShieldCost;

        if (IsMoveCommand(command) && SumConfiguredEffectValues(command.UserRuntime, MoveCostZeroEffectId) > 0)
        {
            command.SetCosts(0, 0, 0, 0);
            command.MarkReservationCostModifiersApplied();
            return;
        }

        if (isFirstMoveCommand && IsMoveCommand(command))
            cost = Mathf.Max(0, cost + SumConfiguredEffectValues(command.UserRuntime, FirstMoveCostDeltaEffectId));

        if (HasRelic(command.UserRuntime, "Relic_05") &&
            slotIndex == 0 &&
            !HasConfiguredRelicEffect(command.UserRuntime, "Relic_05", Slot1SkillCostDeltaEffectId))
        {
            ReduceFirstPositiveCost(
                ref hpCost,
                ref cost,
                ref resourceCost,
                ref shieldCost);
        }

        cost = Mathf.Max(
            0,
            cost + GetConfiguredSkillCostDelta(
                command,
                slotIndex,
                isFirstSkillInSlot,
                hadEarlierMoveInSlot));
        resourceCost = Mathf.Max(0, resourceCost + GetConfiguredUniqueResourceCostDelta(command));

        if (ShouldApplyLowHp(runtime: command.UserRuntime))
        {
            int lowHpCostDelta = SumConfiguredEffectValues(
                command.UserRuntime,
                LowHpSkillCostDeltaEffectId);

            if (lowHpCostDelta < 0)
                ApplyNegativeCostDelta(ref hpCost, ref cost, ref resourceCost, ref shieldCost, lowHpCostDelta);
            else if (lowHpCostDelta > 0)
                cost += lowHpCostDelta;
        }

        if (IsMoveCommand(command))
        {
            int multiplier = Mathf.Max(
                command.MoveReservationCostMultiplier,
                GetSpiderWebMoveCostMultiplier(command.UserRuntime));

            if (multiplier > 1)
                cost *= multiplier;
        }

        command.SetCosts(
            hpCost,
            cost,
            resourceCost,
            shieldCost);
        command.MarkReservationCostModifiersApplied();
    }

    public static int GetSpiderWebMoveCostMultiplier(CharacterRuntimeData runtime)
    {
        if (runtime == null || runtime.StatusEffects == null)
            return 1;

        for (int i = 0; i < runtime.StatusEffects.Count; i++)
        {
            StatusEffectRuntimeData status = runtime.StatusEffects[i];

            if (status == null ||
                status.EffectId != SpiderWebMoveCostPenaltyEffectId ||
                status.TurnCount != SpiderWebActiveTurnCount ||
                status.Stack <= 0)
            {
                continue;
            }

            return SpiderWebMoveCostMultiplier;
        }

        return 1;
    }

    public static bool TryApplyAndConsumeSpiderWebMoveCostPenalty(PlayerReservedCommand command)
    {
        if (command == null || !IsMoveCommand(command))
            return false;

        if (command.MoveReservationCostMultiplier > 1)
            return false;

        int multiplier = GetSpiderWebMoveCostMultiplier(command.UserRuntime);

        if (multiplier <= 1)
            return false;

        command.SetMoveReservationCostMultiplier(multiplier);
        return true;
    }

    public static bool TryConsumeSpiderWebMoveCostPenalty(CharacterRuntimeData runtime)
    {
        if (runtime == null || runtime.StatusEffects == null)
            return false;

        for (int i = runtime.StatusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffectRuntimeData status = runtime.StatusEffects[i];

            if (status == null || status.EffectId != SpiderWebMoveCostPenaltyEffectId)
                continue;

            runtime.StatusEffects.RemoveAt(i);
            return true;
        }

        return false;
    }

    public static int ModifyPassiveEffectStack(
        CharacterRuntimeData runtime,
        string effectId,
        int baseStack)
    {
        int stack = Mathf.Max(0, baseStack);

        if (effectId == "E_Armor")
            return ModifyArmorGain(runtime, stack);

        return stack;
    }

    public static string GetEffectivePlayerDamageEffectId(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command,
        string effectId)
    {
        if (string.IsNullOrWhiteSpace(effectId))
            return effectId;

        string normalizedEffectId = effectId.Trim();

        if (normalizedEffectId != "E_Strike")
            return normalizedEffectId;

        if (command == null ||
            command.SkillData == null ||
            command.SkillData.SkillType != SkillType.Attack ||
            command.TimelineSlotIndex != LastTimelineSlotIndex)
        {
            return normalizedEffectId;
        }

        return SumConfiguredEffectValues(runtime, Slot5AttackPierceEffectId) > 0
            ? "E_Pierce"
            : normalizedEffectId;
    }

    public static bool IsSlotBlockedByEquipment(CharacterRuntimeData runtime, int slotIndex)
    {
        if (runtime == null || slotIndex < 0)
            return false;

        int mask = SumConfiguredEffectValues(runtime, BlockedSlotMaskEffectId);
        int slotFlag = 1 << slotIndex;
        return (mask & slotFlag) != 0;
    }

    public static int GetMaxRegistrableSlotCount(CharacterRuntimeData runtime)
    {
        int configuredValue = SumConfiguredEffectValues(runtime, MaxRegistrableSlotCountEffectId);
        return configuredValue > 0 ? configuredValue : int.MaxValue;
    }

    public static float ModifyIncomingDamageToPlayer(
        CharacterRuntimeData runtime,
        float damage)
    {
        float result = Mathf.Max(0f, damage);

        if (!ShouldApplyLowHp(runtime))
            return result;

        int reductionPercent = SumConfiguredEffectValues(
            runtime,
            LowHpDamageReductionPercentEffectId);

        if (reductionPercent <= 0)
            return result;

        float multiplier = Mathf.Clamp01(1f - reductionPercent / 100f);
        return result * multiplier;
    }

    public static void ApplyPlayerDamageDealtEffects(
        BattleEffectContext context,
        int dealtDamage,
        bool killedTarget)
    {
        if (context == null ||
            context.PlayerCaster == null ||
            context.PlayerCaster.RuntimeData == null ||
            context.MonsterTarget == null ||
            context.MonsterTarget.RuntimeData == null ||
            dealtDamage <= 0)
        {
            return;
        }

        CharacterRuntimeData runtime = context.PlayerCaster.RuntimeData;
        MonsterUnit target = context.MonsterTarget;

        ApplyTargetStatusDamageExtras(runtime, context.PlayerCaster, target);

        bool targetKilledAfterExtras = killedTarget || target.RuntimeData.IsDead;
        if (!targetKilledAfterExtras)
            return;

        ApplyKillTriggeredEffects(runtime, context.PlayerCaster, context.PlayerCommand);
    }

    public static bool ShouldBlockSelfBuff(BattleEffectContext context)
    {
        if (context == null ||
            context.PlayerCaster == null ||
            context.PlayerCaster.RuntimeData == null ||
            context.PlayerSkillData == null ||
            context.PlayerSkillData.SkillType != SkillType.Buff)
        {
            return false;
        }

        if (SumConfiguredEffectValues(
                context.PlayerCaster.RuntimeData,
                SelfBuffBlockedBuffValueDeltaEffectId) <= 0)
        {
            return false;
        }

        BattleCharacter buffTarget = context.PlayerTarget != null
            ? context.PlayerTarget
            : context.PlayerCaster;

        if (buffTarget == null || buffTarget.RuntimeData == null)
            return false;

        return buffTarget.RuntimeData.CharacterId == context.PlayerCaster.RuntimeData.CharacterId;
    }

    public static void HandlePlayerBuffApplied(
        BattleEffectContext context,
        BattleCharacter buffTarget)
    {
        if (context == null ||
            context.PlayerCaster == null ||
            context.PlayerCaster.RuntimeData == null ||
            buffTarget == null ||
            buffTarget.RuntimeData == null ||
            context.PlayerCommand == null)
        {
            return;
        }

        CharacterRuntimeData runtime = context.PlayerCaster.RuntimeData;

        if (buffTarget.RuntimeData.CharacterId == runtime.CharacterId)
            return;

        int charge = SumConfiguredEffectValues(runtime, AllyBuffChargeEffectId);

        if (charge <= 0 || !context.PlayerCommand.TryMarkAllyBuffChargeApplied())
            return;

        AddPassiveStatus(runtime, "E_Charge", charge, AllyBuffChargeEffectId);
    }

    public static bool IgnoresLastSlotDuplicateCost(CharacterRuntimeData runtime)
    {
        return SumConfiguredEffectValues(runtime, LastSlotDuplicateCostIgnoreEffectId) > 0;
    }

    public static void ApplyReservationTurnStartEffects(
        CharacterRuntimeData runtime,
        int playerTurnNumber,
        int occupiedSlotCount,
        int emptySlotCount,
        int emptySlotMask,
        int attackSkillCommandCount)
    {
        if (runtime == null)
            return;

        List<EquipmentEffectEntry> effects = CollectConfiguredEquipmentEffects(runtime);

        for (int i = 0; i < effects.Count; i++)
        {
            EquipmentEffectEntry effect = effects[i];
            SkillEffectEntry entry = effect.Entry;

            if (entry == null)
                continue;

            string applyKey =
                $"ReservationTurnStart:{effect.SourceId}:{entry.EffectId}:{playerTurnNumber}";
            int value = Mathf.Max(0, entry.ValueAmount);

            if (value <= 0)
                continue;

            switch (entry.EffectId)
            {
                case TurnStartArmorPerUsedSlotEffectId:
                    if (TryMarkBattleEffectApplied(runtime, applyKey))
                        ApplyTurnStartArmor(runtime, occupiedSlotCount * value);
                    break;

                case TurnStartArmorPerEmptySlotEffectId:
                    if (TryMarkBattleEffectApplied(runtime, applyKey))
                        ApplyTurnStartArmor(runtime, emptySlotCount * value);
                    break;

                case TurnStartSmiteIfSlotsEmptyMaskEffectId:
                    if (IsSlotMaskEmpty(emptySlotMask, entry.CountAmount) &&
                        TryMarkBattleEffectApplied(runtime, applyKey))
                    {
                        QueueNextTurnSmite(
                            runtime,
                            effect.SourceId,
                            value,
                            playerTurnNumber + 1);
                    }
                    break;

                case TurnStartBoostIfSlotsEmptyMaskEffectId:
                    if (IsSlotMaskEmpty(emptySlotMask, entry.CountAmount) &&
                        TryMarkBattleEffectApplied(runtime, applyKey))
                    {
                        QueueNextTurnBoost(
                            runtime,
                            effect.SourceId,
                            value,
                            playerTurnNumber + 1);
                    }
                    break;

                case TurnStartBoostIfOneAttackCommandEffectId:
                    if (attackSkillCommandCount == 1 &&
                        TryMarkBattleEffectApplied(runtime, applyKey))
                    {
                        AddPassiveStatus(runtime, "E_Boost", value, effect.SourceId);
                    }
                    break;

                case TurnStartBoostIfAttackCountAtLeastEffectId:
                    int threshold = Mathf.Max(1, entry.CountAmount);
                    if (attackSkillCommandCount >= threshold &&
                        TryMarkBattleEffectApplied(runtime, applyKey))
                    {
                        AddPassiveStatus(runtime, "E_Boost", value, effect.SourceId);
                    }
                    break;
            }
        }
    }

    public static void ApplyEndTurnTriggeredEffects(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return;

        int recovery = SumConfiguredEffectValues(runtime, OnceBattleEndTurnZeroCostChargeEffectId);

        if (recovery <= 0)
            return;

        int remainingCost = Mathf.Max(0, runtime.CurrentCost - runtime.ReservedCost);

        if (remainingCost > 0)
            return;

        runtime.CurrentCost = Mathf.Min(
            Mathf.Max(0, runtime.MaxCost),
            Mathf.Max(0, runtime.CurrentCost) + recovery);
    }

    public static void TryApplyAttackMissCharge(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return;

        int recovery = SumConfiguredEffectValues(runtime, OnceBattleAttackMissChargeEffectId);

        if (recovery <= 0)
            return;

        runtime.CurrentCost = Mathf.Min(
            Mathf.Max(0, runtime.MaxCost),
            Mathf.Max(0, runtime.CurrentCost) + recovery);
    }

    public static float ModifyPlayerDamageToMonster(
        BattleEffectContext context,
        float damage)
    {
        float result = Mathf.Max(0f, damage);

        if (context == null ||
            context.PlayerCaster == null ||
            context.PlayerCaster.RuntimeData == null ||
            context.MonsterTarget == null ||
            context.MonsterTarget.RuntimeData == null)
        {
            return result;
        }

        MonsterRuntimeData targetRuntime = context.MonsterTarget.RuntimeData;

        if (targetRuntime.MaxHP <= 0 || targetRuntime.CurrentHP < targetRuntime.MaxHP)
            return result;

        CharacterRuntimeData runtime = context.PlayerCaster.RuntimeData;
        int multiplier = SumConfiguredEffectValues(runtime, OnceBattleFullHpTargetDamagePercentEffectId);

        if (multiplier <= 0)
            return result;

        return result * multiplier;
    }

    public static int GetCollisionTargetDamageDelta(CharacterRuntimeData runtime)
    {
        if (runtime == null || runtime.IsDead)
            return 0;

        return Mathf.Max(0, SumConfiguredEffectValues(runtime, CollisionTargetDamageDeltaEffectId));
    }

    public static void ApplyPlayerCollisionEffects(
        BattleCharacter owner,
        BattleCharacter playerTarget,
        MonsterUnit monsterTarget,
        bool targetKilledOverride = false)
    {
        CharacterRuntimeData runtime = owner != null ? owner.RuntimeData : null;

        if (runtime == null || runtime.IsDead)
            return;

        bool targetKilled =
            targetKilledOverride ||
            (playerTarget != null && playerTarget.RuntimeData != null && playerTarget.RuntimeData.IsDead) ||
            (monsterTarget != null && monsterTarget.RuntimeData != null && monsterTarget.RuntimeData.IsDead);

        int charge = SumConfiguredEffectValues(runtime, CollisionChargeEffectId);

        if (charge > 0)
            AddPassiveStatus(runtime, "E_Charge", charge, CollisionChargeEffectId);

        if (!targetKilled)
            return;

        int karmaRecovery = SumConfiguredEffectValues(runtime, CollisionKillFocusEffectId);

        if (karmaRecovery > 0)
            RecoverUniqueResource(runtime, karmaRecovery);
    }

    public static int ModifyRestHealAmountForParty(int baseAmount)
    {
        return ApplyPercentBonus(
            baseAmount,
            SumConfiguredPartyEffectValues(RestHealPercentBonusEffectId));
    }

    public static int ModifyShopPrice(int basePrice)
    {
        int price = Mathf.Max(0, basePrice);
        int discountPercent = Mathf.Clamp(
            SumConfiguredPartyEffectValues(ShopPriceDiscountPercentEffectId),
            0,
            100);

        if (discountPercent <= 0)
            return price;

        return Mathf.Max(0, Mathf.CeilToInt(price * (1f - discountPercent / 100f)));
    }

    public static int ModifyBattleRewardCurrencyAmount(int baseAmount)
    {
        return ApplyPercentBonus(
            baseAmount,
            SumConfiguredPartyEffectValues(BattleRewardCurrencyPercentEffectId));
    }

    public static void ApplyBattleEndHealToParty()
    {
        List<CharacterRuntimeData> partyRuntimes = GetCurrentPartyRuntimeData();

        for (int i = 0; i < partyRuntimes.Count; i++)
        {
            CharacterRuntimeData runtime = partyRuntimes[i];

            if (runtime == null || runtime.IsDead || runtime.MaxHP <= 0)
                continue;

            int heal = SumConfiguredEffectValues(runtime, BattleEndHealEffectId);

            if (heal <= 0)
                continue;

            runtime.CurrentHP = Mathf.Clamp(runtime.CurrentHP + heal, 1, runtime.MaxHP);
        }
    }

    public static bool IsForcedMoveImmune(CharacterRuntimeData runtime)
    {
        return SumConfiguredEffectValues(runtime, ForcedMoveImmuneEffectId) > 0;
    }

    public static bool IsCrashDamageImmune(CharacterRuntimeData runtime)
    {
        return SumConfiguredEffectValues(runtime, CrashDamageImmuneEffectId) > 0;
    }

    public static bool IgnoresGridEffects(CharacterRuntimeData runtime)
    {
        return SumConfiguredEffectValues(runtime, GridEffectImmuneEffectId) > 0;
    }

    public static bool ShouldBlockPlayerHealing(CharacterRuntimeData runtime)
    {
        return SumConfiguredEffectValues(runtime, HealingBlockedEffectId) > 0;
    }

    public static int GetOverhealArmorAmount(CharacterRuntimeData runtime, int healValue)
    {
        if (runtime == null || healValue <= 0)
            return 0;

        if (runtime.MaxHP <= 0 || runtime.CurrentHP < runtime.MaxHP)
            return 0;

        int multiplier = SumConfiguredEffectValues(runtime, OverhealToArmorEffectId);
        return multiplier > 0 ? healValue * multiplier : 0;
    }

    public static int ModifyArmorGainForPlayer(CharacterRuntimeData runtime, int baseValue)
    {
        return ModifyArmorGain(runtime, baseValue);
    }

    public static void MarkPlayerDamagedThisTurn(CharacterRuntimeData runtime)
    {
        TryMarkBattleEffectApplied(runtime, DamageTakenThisTurnStateId);
    }

    public static void ApplyPassiveExtras(CharacterRuntimeData runtime)
    {
        // Rune 효과는 GameData EffectIds 기반으로 처리합니다.
        // 기존 Rune_01~25 하드코딩 패시브는 현재 룬 데이터와 충돌하므로 사용하지 않습니다.
    }

    public static int ModifyPlayerKnockbackValue(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command,
        SkillEffectEntry entry,
        int baseValue)
    {
        int value = Mathf.Max(0, baseValue);

        if (runtime == null || command == null || entry == null)
            return value;

        int runeKnockbackValue = SumConfiguredRuneTargetEffectValues(
            runtime, RuneTargetKnockbackValueDeltaEffectId, command.SkillId);

        // 전용 룬의 넉백 값은 추가 수치가 아니라 최종 넉백 거리입니다.
        // 예: 기본 1칸인 대지가르기에 Rune_54 값 3이 적용되면 최종 3칸 넉백입니다.
        if (runeKnockbackValue > 0)
            value = runeKnockbackValue;

        // 넉백 자체에 적용되는 일반 장비 보정이 있다면 함께 반영합니다.
        value += GetConfiguredEffectValueDelta(runtime, command, entry);

        return Mathf.Max(0, value);
    }

    public static int ModifyPlayerEffectValue(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command,
        SkillEffectEntry entry,
        int baseValue)
    {
        int value = Mathf.Max(0, baseValue);

        if (runtime == null || command == null || entry == null)
            return value;

        if (entry.EffectId == "E_Heal")
        {
            // 파편 보정을 먼저 확정한 뒤 유물 보정을 적용합니다.
            value += SumConfiguredRuneTargetEffectValues(
                runtime, RuneTargetHealValueDeltaEffectId, command.SkillId);
            value += SumConfiguredRelicEffectValues(runtime, HealValueDeltaEffectId);
        }

        if (entry.EffectId == "E_Armor")
        {
            // 파편 배율도 유물보다 먼저 계산합니다.
            int armorMultiplier = SumConfiguredRuneTargetEffectValues(
                runtime, RuneTargetArmorMultiplierEffectId, command.SkillId);
            if (armorMultiplier > 1)
                value *= armorMultiplier;

            value += SumConfiguredRelicEffectValues(runtime, SkillArmorValueDeltaEffectId);
        }

        if (entry.EffectId == "E_Poison")
            value += SumConfiguredRelicEffectValues(runtime, PoisonValueDeltaEffectId);

        if (entry.EffectId == "E_Bleed")
            value += SumConfiguredRelicEffectValues(runtime, BleedValueDeltaEffectId);

        if (entry.EffectId == "E_Boost")
        {
            value += SumConfiguredRuneTargetEffectValues(
                runtime, RuneTargetBoostValueDeltaEffectId, command.SkillId);
        }

        if (IsDamageEffect(entry.EffectId))
        {
            value += SumConfiguredRuneTargetEffectValues(
                runtime, RuneTargetDamageValueDeltaEffectId, command.SkillId);
        }

        value = Mathf.Max(
            0,
            value + GetConfiguredEffectValueDelta(runtime, command, entry));

        if (command.SkillData != null &&
            command.SkillData.Category == Category.Unique)
        {
            int uniquePercent = SumConfiguredRelicEffectValues(runtime, UniqueSkillValuePercentEffectId);

            if (uniquePercent != 0)
                value = Mathf.Max(0, Mathf.CeilToInt(value * (1f + uniquePercent / 100f)));
        }

        if (IsLastTimelineSlot(command) &&
            HasRelic(runtime, "Relic_03") &&
            !HasConfiguredRelicEffect(runtime, "Relic_03", Slot5AttackValueDeltaEffectId) &&
            command.SkillData != null &&
            command.SkillData.SkillType == SkillType.Attack)
        {
            value += 1;
        }

        if (command.TimelineSlotIndex == 0 &&
            HasRelic(runtime, "Relic_04") &&
            !HasConfiguredRelicEffect(runtime, "Relic_04", Slot1DebuffValueDeltaEffectId) &&
            command.SkillData != null &&
            command.SkillData.SkillType == SkillType.Debuff)
        {
            value += 1;
        }

        // 전투 중 획득한 적용량 변경 상태는 파편/유물 계산 이후에 반영합니다.
        // 같은 종류가 여러 개라면 StatusEffects에 기록된 부여 순서를 그대로 따릅니다.
        if (runtime.StatusEffects != null)
        {
            for (int i = 0; i < runtime.StatusEffects.Count; i++)
            {
                StatusEffectRuntimeData status = runtime.StatusEffects[i];
                if (status == null || status.Stack <= 0)
                    continue;

                if (entry.EffectId == "E_Poison" && status.EffectId == PoisonApplyDoubleEffectId)
                    value *= 2;
                else if (entry.EffectId == "E_Bleed" && status.EffectId == BleedingApplyDoubleEffectId)
                    value *= 2;
                else if (command.SkillData != null &&
                         command.SkillData.SkillType == SkillType.Buff &&
                         status.EffectId == BuffApplyDoubleEffectId)
                    value *= 2;
            }
        }

        return Mathf.Max(0, value);
    }

    public static int ModifyPlayerEffectCount(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command,
        SkillEffectEntry entry,
        int baseCount)
    {
        int count = Mathf.Max(0, baseCount);

        if (runtime == null || command == null || entry == null)
            return count;

        // 횟수 역시 기본값 + 파편 -> 유물 순서로 계산합니다.
        if (IsDamageEffect(entry.EffectId) &&
            command.SkillData != null &&
            command.SkillData.SkillType == SkillType.Attack)
        {
            count = Mathf.Max(
                0,
                count + SumConfiguredRuneTargetEffectValues(
                    runtime, RuneTargetAttackCountDeltaEffectId, command.SkillId));

            count = Mathf.Max(
                0,
                count + SumConfiguredRelicEffectValues(runtime, AttackCountDeltaEffectId));
        }

        if (command.SkillData != null &&
            command.SkillData.Category == Category.Unique)
        {
            count = Mathf.Max(
                0,
                count + SumConfiguredRelicEffectValues(runtime, UniqueSkillCountDeltaEffectId));
        }

        if (!IsDamageEffect(entry.EffectId) ||
            command.SkillData == null ||
            command.SkillData.SkillType != SkillType.Attack)
        {
            return count;
        }

        int randomAttackCountDelta = SumConfiguredRelicEffectValues(runtime, RandomAttackCountDeltaEffectId);
        if (randomAttackCountDelta != 0 &&
            IsRelic07SelectedAttackSkill(runtime, command.SkillId))
        {
            count = Mathf.Max(0, count + randomAttackCountDelta);
        }

        if (HasRelic(runtime, "Relic_07") &&
            !HasConfiguredRelicEffect(runtime, "Relic_07", RandomAttackCountDeltaEffectId) &&
            IsRelic07SelectedAttackSkill(runtime, command.SkillId))
        {
            count += 1;
        }

        return count;
    }

    public static string GetEffectiveRangeId(
        CharacterRuntimeData runtime,
        SkillMasterData skillData)
    {
        if (skillData == null)
            return string.Empty;

        if (SumConfiguredRuneTargetEffectValues(
                runtime, RuneTargetRangeExpandEffectId, skillData.SkillId) > 0 &&
            skillData.RangeId == "Range_17")
        {
            return "Range_05";
        }

        if (SumConfiguredEffectValues(runtime, RangeDeltaEffectId) >= 2 &&
            skillData.RangeId == "Range_21")
        {
            return "Range_18";
        }

        return skillData.RangeId;
    }

    public static bool HasRune(CharacterRuntimeData runtime, string runeId)
    {
        if (runtime == null || runtime.EquippedRuneIds == null || string.IsNullOrWhiteSpace(runeId))
            return false;

        for (int i = 0; i < runtime.EquippedRuneIds.Length; i++)
        {
            if (runtime.EquippedRuneIds[i] == runeId)
                return true;
        }

        return false;
    }

    public static bool HasRelic(CharacterRuntimeData runtime, string relicId)
    {
        if (runtime == null || runtime.EquippedRelicIds == null || string.IsNullOrWhiteSpace(relicId))
            return false;

        for (int i = 0; i < runtime.EquippedRelicIds.Length; i++)
        {
            if (runtime.EquippedRelicIds[i] == relicId)
                return true;
        }

        return false;
    }

    public static bool IsMoveCommand(PlayerReservedCommand command)
    {
        if (command == null)
            return false;

        if (command.ReservedMoveGridIndex >= 0)
            return true;

        SkillMasterData skillData = command.SkillData;

        if (skillData == null)
            return false;

        return skillData.Category == Category.Move;
    }

    private static int GetMaxHPBonus(CharacterRuntimeData runtime, int baseMaxHP)
    {
        int bonus = SumConfiguredEffectValues(runtime, MaxHpEffectId);

        if (HasRelic(runtime, "Relic_09") &&
            !HasConfiguredRelicEffect(runtime, "Relic_09", MaxHpEffectId))
        {
            bonus += 5;
        }

        int maxHpUpStack = GetStatusStack(runtime, MaxHpUpEffectId);
        if (maxHpUpStack > 0 && baseMaxHP > 0)
            bonus += Mathf.CeilToInt(baseMaxHP * 0.1f * maxHpUpStack);

        return bonus;
    }

    private static int GetMaxCostBonus(CharacterRuntimeData runtime)
    {
        int bonus = SumConfiguredEffectValues(runtime, MaxCostEffectId);

        if (HasRelic(runtime, "Relic_08") &&
            !HasConfiguredRelicEffect(runtime, "Relic_08", MaxCostEffectId))
        {
            bonus += 1;
        }

        return bonus;
    }

    private static int GetCostRecoveryBonus(CharacterRuntimeData runtime)
    {
        int bonus = GetStatusStack(runtime, SkillResourceGainUpEffectId) +
            SumConfiguredEffectValues(runtime, CostRecoveryDeltaEffectId);

        if (ShouldApplyHighHp(runtime))
            bonus += SumConfiguredEffectValues(runtime, HighHpCostRecoveryDeltaEffectId);

        return bonus;
    }

    private static int GetMoveValueBonus(CharacterRuntimeData runtime)
    {
        int bonus = SumConfiguredEffectValues(runtime, MoveValueEffectId);

        if (HasRelic(runtime, "Relic_10") &&
            !HasConfiguredRelicEffect(runtime, "Relic_10", MoveValueEffectId))
        {
            bonus += 8;
        }

        bonus += GetStatusStack(runtime, MovePointUpEffectId);

        return bonus;
    }

    private static int GetBattleStartCostBonus(CharacterRuntimeData runtime)
    {
        int bonus = SumConfiguredEffectValues(runtime, BattleStartCostEffectId);

        if (HasRelic(runtime, "Relic_01") &&
            !HasConfiguredRelicEffect(runtime, "Relic_01", BattleStartCostEffectId))
        {
            bonus += 2;
        }

        return bonus;
    }

    private static int GetBattleStartUniqueResource(
        CharacterRuntimeData runtime,
        CharacterMasterData masterData)
    {
        int resource = SumConfiguredEffectValues(runtime, BattleStartUniqueResourceEffectId);

        int maxResource = masterData != null
            ? Mathf.Max(0, masterData.MaxResource)
            : Mathf.Max(0, resource);

        return Mathf.Clamp(resource, 0, maxResource);
    }

    private static int ModifyArmorGain(CharacterRuntimeData runtime, int baseValue)
    {
        int value = Mathf.Max(0, baseValue);

        value += SumConfiguredEffectValues(runtime, ArmorGainDeltaEffectId);

        int percent = SumConfiguredEffectValues(runtime, ArmorGainPercentEffectId);
        if (percent != 0)
            value = Mathf.Max(0, Mathf.CeilToInt(value * (1f + percent / 100f)));

        return value;
    }

    private static void ApplyConfiguredTurnStartEffects(
        CharacterRuntimeData runtime,
        int playerTurnNumber)
    {
        List<EquipmentEffectEntry> effects = CollectConfiguredEquipmentEffects(runtime);

        for (int i = 0; i < effects.Count; i++)
        {
            EquipmentEffectEntry effect = effects[i];
            SkillEffectEntry entry = effect.Entry;

            if (entry == null || !MatchesConfiguredTurn(entry, playerTurnNumber))
                continue;

            string applyKey =
                $"TurnStart:{effect.SourceId}:{entry.EffectId}:{playerTurnNumber}";

            if (!TryMarkBattleEffectApplied(runtime, applyKey))
                continue;

            if (entry.EffectId == TurnStartArmorEffectId)
            {
                ApplyTurnStartArmor(runtime, Mathf.Max(0, entry.ValueAmount));

                continue;
            }

            if (entry.EffectId == TurnStartCostRecoveryEffectId)
            {
                ApplyTurnStartCostRecovery(runtime, Mathf.Max(0, entry.ValueAmount));
                continue;
            }

            if (entry.EffectId == TurnStartUniqueResourceRecoveryEffectId)
            {
                ApplyTurnStartUniqueResourceRecovery(runtime, Mathf.Max(0, entry.ValueAmount));
                continue;
            }

            if (TryApplyEnemyTurnStartStatusEffect(entry, effect.SourceId))
                continue;

            if (TryApplyConditionalUniqueResourceTurnStartEffect(runtime, entry))
                continue;

            string statusEffectId = ResolveTurnStartStatusEffectId(entry.EffectId);

            if (string.IsNullOrWhiteSpace(statusEffectId))
                continue;

            AddTemporaryStatus(
                runtime,
                statusEffectId,
                Mathf.Max(0, entry.ValueAmount),
                effect.SourceId);
        }
    }

    private static void ApplyNoDamagePreviousTurnEffect(
        CharacterRuntimeData runtime,
        int playerTurnNumber)
    {
        if (runtime == null)
            return;

        if (playerTurnNumber > 1 &&
            !HasBattleEffectApplied(runtime, DamageTakenThisTurnStateId))
        {
            int smite = SumConfiguredEffectValues(runtime, NoDamagePreviousTurnSmiteEffectId);

            if (smite > 0)
                AddTemporaryStatus(runtime, "E_Smite", smite, NoDamagePreviousTurnSmiteEffectId);
        }

        RemoveBattleEffectApplied(runtime, DamageTakenThisTurnStateId);
    }

    private static void ApplyTurnStartCostRecovery(CharacterRuntimeData runtime, int amount)
    {
        if (runtime == null || amount <= 0)
            return;

        runtime.CurrentCost = Mathf.Min(
            Mathf.Max(0, runtime.MaxCost),
            Mathf.Max(0, runtime.CurrentCost) + amount);
    }

    private static void ApplyTurnStartUniqueResourceRecovery(CharacterRuntimeData runtime, int amount)
    {
        RecoverUniqueResource(runtime, amount);
    }

    private static int GetMaxUniqueResource(CharacterRuntimeData runtime)
    {
        if (runtime == null ||
            DataManager.Instance == null ||
            DataManager.Instance.CharacterDatabase == null ||
            !DataManager.Instance.CharacterDatabase.TryGet(
                runtime.CharacterId,
                out CharacterMasterData masterData) ||
            masterData == null)
        {
            return 0;
        }

        return Mathf.Max(0, masterData.MaxResource);
    }

    private static void RecoverUniqueResource(CharacterRuntimeData runtime, int amount)
    {
        if (runtime == null || amount <= 0)
            return;

        int maxResource = GetMaxUniqueResource(runtime);
        if (maxResource <= 0)
            return;

        int previousResource = Mathf.Max(0, runtime.CurrentResource);
        int modifiedAmount = Mathf.Max(0, ModifyUniqueResourceGain(runtime, amount));

        ApplyUniqueResourceGainSideEffects(
            runtime,
            modifiedAmount,
            previousResource,
            maxResource);

        runtime.CurrentResource = Mathf.Clamp(
            previousResource + modifiedAmount,
            0,
            maxResource);

        if (runtime.CurrentResource == previousResource)
            return;

        PlayerHUDSlot[] hudSlots = Object.FindObjectsByType<PlayerHUDSlot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < hudSlots.Length; i++)
        {
            if (hudSlots[i] != null)
                hudSlots[i].Refresh();
        }
    }

    private static void ApplyTurnStartArmor(CharacterRuntimeData runtime, int baseArmor)
    {
        if (runtime == null)
            return;

        int gainedArmor = ModifyArmorGain(runtime, Mathf.Max(0, baseArmor));

        if (gainedArmor <= 0)
            return;

        runtime.CurrentShield += gainedArmor;
        BattleDamageTextPopupUI.ShowArmorGain(runtime.CharacterId, gainedArmor);
    }

    private static bool TryApplyEnemyTurnStartStatusEffect(
        SkillEffectEntry entry,
        string sourceId)
    {
        string statusEffectId = ResolveEnemyTurnStartStatusEffectId(entry?.EffectId);

        if (string.IsNullOrWhiteSpace(statusEffectId))
            return false;

        int stack = Mathf.Max(0, entry.ValueAmount);

        if (stack <= 0)
            return true;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                continue;

            BattleEffectUtility.AddStatusToMonster(monster, statusEffectId, stack, 1);
        }

        return true;
    }

    private static bool TryApplyConditionalUniqueResourceTurnStartEffect(
        CharacterRuntimeData runtime,
        SkillEffectEntry entry)
    {
        if (runtime == null || entry == null)
            return false;

        int threshold = ResolveUniqueResourceThreshold(entry.EffectId);

        if (threshold <= 0)
            return false;

        if (runtime.CurrentResource < threshold)
            return true;

        if (entry.EffectId == UniqueResource3TurnStartArmorEffectId)
        {
            int gainedArmor = ModifyArmorGain(runtime, Mathf.Max(0, entry.ValueAmount));

            if (gainedArmor > 0)
            {
                runtime.CurrentShield += gainedArmor;
                BattleDamageTextPopupUI.ShowArmorGain(runtime.CharacterId, gainedArmor);
            }

            return true;
        }

        string statusEffectId = entry.EffectId switch
        {
            UniqueResource3TurnStartAimingEffectId => "E_Aiming",
            UniqueResource3TurnStartBoostEffectId => "E_Boost",
            UniqueResource5TurnStartBoostEffectId => "E_Boost",
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(statusEffectId))
            AddPassiveStatus(runtime, statusEffectId, Mathf.Max(0, entry.ValueAmount), entry.EffectId);

        return true;
    }

    private static int ResolveUniqueResourceThreshold(string effectId)
    {
        return effectId switch
        {
            UniqueResource3TurnStartAimingEffectId => 3,
            UniqueResource3TurnStartArmorEffectId => 3,
            UniqueResource3TurnStartBoostEffectId => 3,
            UniqueResource5TurnStartBoostEffectId => 5,
            _ => 0
        };
    }

    private static int GetConfiguredSkillCostDelta(
        PlayerReservedCommand command,
        int slotIndex,
        bool isFirstSkillInSlot,
        bool hadEarlierMoveInSlot)
    {
        if (command == null || command.SkillData == null)
            return 0;

        CharacterRuntimeData runtime = command.UserRuntime;
        SkillType skillType = command.SkillData.SkillType;
        int delta = SumConfiguredRuneTargetEffectValues(
            runtime, RuneTargetSkillCostDeltaEffectId, command.SkillId);

        if (skillType == SkillType.Attack)
            delta += SumConfiguredRelicEffectValues(runtime, AttackCostDeltaEffectId);

        if (skillType == SkillType.Buff)
            delta += SumConfiguredRelicEffectValues(runtime, BuffCostDeltaEffectId);

        if (skillType == SkillType.Debuff)
            delta += SumConfiguredRelicEffectValues(runtime, DebuffCostDeltaEffectId);

        if (IsMoveCommand(command))
            delta += SumConfiguredRelicEffectValues(runtime, MoveCostDeltaEffectId);

        if (skillType == SkillType.Buff && !hadEarlierMoveInSlot)
            delta += SumConfiguredRelicEffectValues(runtime, NoMoveInSlotBuffCostDeltaEffectId);

        if (skillType == SkillType.Debuff && hadEarlierMoveInSlot)
            delta += SumConfiguredRelicEffectValues(runtime, AfterMoveDebuffCostDeltaEffectId);

        if (skillType == SkillType.Buff && ShouldApplyHighHp(runtime))
            delta += SumConfiguredRelicEffectValues(runtime, HighHpBuffCostDeltaEffectId);

        if (IsUniqueResourceFull(runtime))
            delta += SumConfiguredRelicEffectValues(runtime, UniqueResourceMaxSkillCostDeltaEffectId);

        if (slotIndex == 0)
        {
            delta += SumConfiguredRelicEffectValues(runtime, Slot1SkillCostDeltaEffectId);

            if (isFirstSkillInSlot)
                delta += SumConfiguredRelicEffectValues(runtime, Slot1FirstSkillCostDeltaEffectId);
        }

        if (slotIndex == LastTimelineSlotIndex)
            delta += SumConfiguredRelicEffectValues(runtime, Slot5SkillCostDeltaEffectId);

        return delta;
    }

    private static int GetConfiguredUniqueResourceCostDelta(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return 0;

        if (command.SkillData.ReferenceResource != ReferenceResource.UniqueResource)
            return 0;

        if (command.SkillData.Category != Category.Unique)
            return 0;

        return SumConfiguredEffectValues(command.UserRuntime, UniqueResourceMinUseDeltaEffectId);
    }

    private static void ApplyTargetStatusDamageExtras(
        CharacterRuntimeData runtime,
        BattleCharacter caster,
        MonsterUnit target)
    {
        if (runtime == null || caster == null || target?.RuntimeData == null)
            return;

        int extraFixedDamage = 0;

        if (HasStatus(target.RuntimeData.StatusEffects, "E_Poison"))
            extraFixedDamage += SumConfiguredEffectValues(runtime, DamagePoisonedFixedEffectId);

        if (HasStatus(target.RuntimeData.StatusEffects, "E_Bleed"))
            extraFixedDamage += SumConfiguredEffectValues(runtime, DamageBleedingFixedEffectId);

        if (extraFixedDamage > 0)
            BattleEffectUtility.StatusDamageMonster(target, extraFixedDamage);

        int gainedArmor = 0;

        if (HasStatus(target.RuntimeData.StatusEffects, "E_Vulnerable"))
            gainedArmor += SumConfiguredEffectValues(runtime, DamageVulnerableArmorEffectId);

        if (HasStatus(target.RuntimeData.StatusEffects, "E_Weaken"))
            gainedArmor += SumConfiguredEffectValues(runtime, DamageWeakenedArmorEffectId);

        if (gainedArmor > 0)
            BattleEffectUtility.AddShieldToPlayer(caster, gainedArmor);

        if (!HasStatus(target.RuntimeData.StatusEffects, "E_Poison"))
        {
            int poison = SumConfiguredEffectValues(runtime, DamageNonPoisonedApplyPoisonEffectId);

            if (poison > 0)
                BattleEffectUtility.AddStatusToMonster(target, "E_Poison", poison, 1);
        }
    }

    private static void ApplyKillTriggeredEffects(
        CharacterRuntimeData runtime,
        BattleCharacter caster,
        PlayerReservedCommand command)
    {
        if (runtime == null || caster == null)
            return;

        if (command != null &&
            command.SkillData != null &&
            command.SkillData.SkillType == SkillType.Attack &&
            command.TimelineSlotIndex == 0)
        {
            int focus = SumConfiguredEffectValues(runtime, Slot1AttackKillFocusEffectId);

            if (focus > 0)
                AddPassiveStatus(runtime, "E_Focus", focus, Slot1AttackKillFocusEffectId);
        }

        if (command != null &&
            command.SkillData != null &&
            command.SkillData.SkillType == SkillType.Attack &&
            command.TimelineSlotIndex == LastTimelineSlotIndex)
        {
            int refundToggle = SumConfiguredEffectValues(runtime, Slot5AttackKillCostRefundEffectId);

            if (refundToggle > 0)
                RefundCommandCost(runtime, command);
        }

        if (command != null &&
            command.SkillData != null &&
            command.SkillData.Category == Category.Unique)
        {
            int focus = SumConfiguredEffectValues(runtime, UniqueSkillKillFocusEffectId);

            if (focus > 0)
                AddPassiveStatus(runtime, "E_Focus", focus, UniqueSkillKillFocusEffectId);
        }
    }

    private static void RefundCommandCost(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command)
    {
        if (runtime == null || command == null)
            return;

        int refund = Mathf.Max(0, command.Cost);

        if (refund <= 0)
            return;

        int maxCost = runtime.MaxCost > 0
            ? runtime.MaxCost
            : runtime.CurrentCost + refund;

        runtime.CurrentCost = Mathf.Min(maxCost, runtime.CurrentCost + refund);
    }

    private static int GetConfiguredEffectValueDelta(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command,
        SkillEffectEntry entry)
    {
        if (runtime == null ||
            command == null ||
            command.SkillData == null ||
            entry == null)
        {
            return 0;
        }

        SkillType skillType = command.SkillData.SkillType;
        bool isDamageEffect = IsDamageEffect(entry.EffectId);
        int delta = 0;

        if (skillType == SkillType.Attack && isDamageEffect)
        {
            delta += SumConfiguredRelicEffectValues(runtime, AttackValueDeltaEffectId);

            if (command.TimelineSlotIndex == 0)
                delta += SumConfiguredRelicEffectValues(runtime, Slot1AttackValueDeltaEffectId);

            if (command.TimelineSlotIndex == LastTimelineSlotIndex)
                delta += SumConfiguredRelicEffectValues(runtime, Slot5AttackValueDeltaEffectId);

            if (command.HadEarlierMoveInSlot)
            {
                int perCost = SumConfiguredRelicEffectValues(runtime, AfterMoveAttackValuePerCostEffectId);
                delta += Mathf.Max(0, command.SameSlotMoveCostBeforeCommand) * perCost;
            }

            if (ShouldApplyLowHp(runtime))
                delta += SumConfiguredRelicEffectValues(runtime, LowHpAttackValueDeltaEffectId);
        }

        if (skillType == SkillType.Buff && !isDamageEffect)
        {
            delta += SumConfiguredRelicEffectValues(runtime, BuffValueDeltaEffectId);

            if (command.TimelineSlotIndex == 0)
                delta += SumConfiguredRelicEffectValues(runtime, Slot1BuffValueDeltaEffectId);

            if (ShouldApplyFullHp(runtime))
                delta += SumConfiguredRelicEffectValues(runtime, FullHpBuffValueDeltaEffectId);

            delta += SumConfiguredRelicEffectValues(runtime, SelfBuffBlockedBuffValueDeltaEffectId);
        }

        if (skillType == SkillType.Debuff && !isDamageEffect)
        {
            delta += SumConfiguredRelicEffectValues(runtime, DebuffValueDeltaEffectId);

            if (command.TimelineSlotIndex == 0)
                delta += SumConfiguredRelicEffectValues(runtime, Slot1DebuffValueDeltaEffectId);

            if (command.HadEarlierMoveInSlot)
                delta += SumConfiguredRelicEffectValues(runtime, AfterMoveDebuffValueDeltaEffectId);
        }

        return delta;
    }

    private static int SumConfiguredRuneTargetEffectValues(
        CharacterRuntimeData runtime,
        string effectId,
        string skillId)
    {
        if (runtime?.EquippedRuneIds == null ||
            string.IsNullOrWhiteSpace(effectId) ||
            string.IsNullOrWhiteSpace(skillId) ||
            DataManager.Instance == null ||
            DataManager.Instance.RuneDatabase == null)
        {
            return 0;
        }

        int total = 0;
        string normalizedSkillId = skillId.Trim();

        for (int i = 0; i < runtime.EquippedRuneIds.Length; i++)
        {
            string runeId = runtime.EquippedRuneIds[i];

            if (string.IsNullOrWhiteSpace(runeId) ||
                !DataManager.Instance.RuneDatabase.TryGet(runeId, out RuneData rune) ||
                rune == null ||
                !RuneTargetsSkill(rune, normalizedSkillId))
            {
                continue;
            }

            List<SkillEffectEntry> entries = ResolveRuneEntries(rune);
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                SkillEffectEntry entry = entries[entryIndex];
                if (entry != null && entry.EffectId == effectId)
                    total += GetRepeatedEntryValue(entry);
            }
        }

        return total;
    }

    private static bool RuneTargetsSkill(RuneData rune, string skillId)
    {
        if (rune == null ||
            string.IsNullOrWhiteSpace(rune.TargetSkillId) ||
            string.IsNullOrWhiteSpace(skillId))
        {
            return false;
        }

        string[] targetSkillIds = rune.TargetSkillId.Split(';');
        for (int i = 0; i < targetSkillIds.Length; i++)
        {
            string targetSkillId = targetSkillIds[i]?.Trim();
            if (!string.IsNullOrWhiteSpace(targetSkillId) && targetSkillId == skillId)
                return true;
        }

        return false;
    }

    private static int SumConfiguredRelicEffectValues(
        CharacterRuntimeData runtime,
        string effectId)
    {
        if (runtime == null || string.IsNullOrWhiteSpace(effectId))
            return 0;

        int total = 0;
        List<EquipmentEffectEntry> effects = new();
        AddConfiguredRelicEffects(effects, runtime);

        for (int i = 0; i < effects.Count; i++)
        {
            SkillEffectEntry entry = effects[i].Entry;
            if (entry != null && entry.EffectId == effectId)
                total += GetRepeatedEntryValue(entry);
        }

        return total;
    }

    private static int SumConfiguredEffectValues(
        CharacterRuntimeData runtime,
        string effectId)
    {
        if (runtime == null || string.IsNullOrWhiteSpace(effectId))
            return 0;

        int total = 0;
        List<EquipmentEffectEntry> effects = CollectConfiguredEquipmentEffects(runtime);

        for (int i = 0; i < effects.Count; i++)
        {
            SkillEffectEntry entry = effects[i].Entry;

            if (entry == null || entry.EffectId != effectId)
                continue;

            total += GetRepeatedEntryValue(entry);
        }

        return total;
    }

    private static int SumConfiguredPartyEffectValues(string effectId)
    {
        if (string.IsNullOrWhiteSpace(effectId))
            return 0;

        int total = 0;
        List<CharacterRuntimeData> partyRuntimes = GetCurrentPartyRuntimeData();

        for (int i = 0; i < partyRuntimes.Count; i++)
            total += SumConfiguredEffectValues(partyRuntimes[i], effectId);

        return total;
    }

    private static List<CharacterRuntimeData> GetCurrentPartyRuntimeData()
    {
        List<CharacterRuntimeData> result = new();

        if (DataManager.Instance == null ||
            DataManager.Instance.CharacterRuntimeStore == null)
        {
            return result;
        }

        HashSet<string> addedIds = new(System.StringComparer.Ordinal);
        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        if (partyStore != null)
        {
            for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
            {
                string characterId = partyStore.GetCharacterId(i);

                if (string.IsNullOrWhiteSpace(characterId))
                    continue;

                characterId = characterId.Trim();

                if (!addedIds.Add(characterId))
                    continue;

                if (DataManager.Instance.CharacterRuntimeStore.TryGet(
                        characterId,
                        out CharacterRuntimeData runtime) &&
                    runtime != null)
                {
                    result.Add(runtime);
                }
            }
        }

        if (result.Count > 0)
            return result;

        IReadOnlyDictionary<string, CharacterRuntimeData> allCharacters =
            DataManager.Instance.CharacterRuntimeStore.GetAll();

        if (allCharacters == null)
            return result;

        foreach (KeyValuePair<string, CharacterRuntimeData> pair in allCharacters)
        {
            CharacterRuntimeData runtime = pair.Value;

            if (runtime == null || string.IsNullOrWhiteSpace(runtime.CharacterId))
                continue;

            if (addedIds.Add(runtime.CharacterId.Trim()))
                result.Add(runtime);
        }

        return result;
    }

    private static int ApplyPercentBonus(int baseAmount, int percent)
    {
        int amount = Mathf.Max(0, baseAmount);

        if (percent == 0)
            return amount;

        return Mathf.Max(0, Mathf.CeilToInt(amount * (1f + percent / 100f)));
    }

    private static bool ApplyCollisionExtraDamage(
        CharacterRuntimeData runtime,
        BattleCharacter playerTarget,
        MonsterUnit monsterTarget)
    {
        int extraDamage = SumConfiguredEffectValues(runtime, CollisionTargetDamageDeltaEffectId);

        if (extraDamage <= 0)
            return false;

        if (playerTarget != null &&
            playerTarget.RuntimeData != null &&
            !playerTarget.RuntimeData.IsDead &&
            playerTarget.RuntimeData.CharacterId != runtime.CharacterId &&
            !IsCrashDamageImmune(playerTarget.RuntimeData))
        {
            int hpBefore = playerTarget.RuntimeData.CurrentHP;
            BattleEffectUtility.StatusDamagePlayer(playerTarget, extraDamage);
            return hpBefore > 0 && playerTarget.RuntimeData.CurrentHP <= 0;
        }

        if (monsterTarget != null &&
            monsterTarget.RuntimeData != null &&
            !monsterTarget.RuntimeData.IsDead)
        {
            int hpBefore = monsterTarget.RuntimeData.CurrentHP;
            BattleEffectUtility.StatusDamageMonster(monsterTarget, extraDamage);
            return hpBefore > 0 && monsterTarget.RuntimeData.CurrentHP <= 0;
        }

        return false;
    }

    private static bool HasConfiguredRelicEffect(
        CharacterRuntimeData runtime,
        string relicId,
        string effectId)
    {
        if (runtime == null ||
            string.IsNullOrWhiteSpace(relicId) ||
            string.IsNullOrWhiteSpace(effectId) ||
            DataManager.Instance == null ||
            DataManager.Instance.RelicDatabase == null ||
            !DataManager.Instance.RelicDatabase.TryGet(relicId, out RelicData relic))
        {
            return false;
        }

        List<SkillEffectEntry> entries = ResolveRelicEntries(relic);

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].EffectId == effectId)
                return true;
        }

        return false;
    }

    private static List<EquipmentEffectEntry> CollectConfiguredEquipmentEffects(
        CharacterRuntimeData runtime)
    {
        List<EquipmentEffectEntry> result = new();

        if (runtime == null || DataManager.Instance == null)
            return result;

        AddConfiguredRuneEffects(result, runtime);
        AddConfiguredRelicEffects(result, runtime);
        return result;
    }

    private static void AddConfiguredRuneEffects(
        List<EquipmentEffectEntry> result,
        CharacterRuntimeData runtime)
    {
        if (result == null ||
            runtime?.EquippedRuneIds == null ||
            DataManager.Instance == null ||
            DataManager.Instance.RuneDatabase == null)
        {
            return;
        }

        for (int i = 0; i < runtime.EquippedRuneIds.Length; i++)
        {
            string runeId = runtime.EquippedRuneIds[i];

            if (string.IsNullOrWhiteSpace(runeId) ||
                !DataManager.Instance.RuneDatabase.TryGet(runeId, out RuneData rune) ||
                rune == null)
            {
                continue;
            }

            AddEntries(result, rune.RuneId, ResolveRuneEntries(rune));
        }
    }

    private static void AddConfiguredRelicEffects(
        List<EquipmentEffectEntry> result,
        CharacterRuntimeData runtime)
    {
        if (result == null ||
            runtime?.EquippedRelicIds == null ||
            DataManager.Instance == null ||
            DataManager.Instance.RelicDatabase == null)
        {
            return;
        }

        for (int i = 0; i < runtime.EquippedRelicIds.Length; i++)
        {
            string relicId = runtime.EquippedRelicIds[i];

            if (string.IsNullOrWhiteSpace(relicId) ||
                !DataManager.Instance.RelicDatabase.TryGet(relicId, out RelicData relic) ||
                relic == null ||
                ActiveRelicEffectResolver.IsActiveRelic(relic))
            {
                continue;
            }

            AddEntries(result, relic.FragmentId, ResolveRelicEntries(relic));
        }
    }

    private static void AddEntries(
        List<EquipmentEffectEntry> result,
        string sourceId,
        List<SkillEffectEntry> entries)
    {
        if (result == null || entries == null)
            return;

        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId)
            ? "Unknown"
            : sourceId.Trim();

        for (int i = 0; i < entries.Count; i++)
        {
            SkillEffectEntry entry = entries[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.EffectId))
                continue;

            entry.EffectId = entry.EffectId.Trim();
            result.Add(new EquipmentEffectEntry(normalizedSourceId, entry));
        }
    }

    private static List<SkillEffectEntry> ResolveRelicEntries(RelicData relic)
    {
        if (relic == null)
            return new List<SkillEffectEntry>();

        // 유물은 현재 GameData의 EffectIds / ValueRate / CountRate를 기준으로 다시 파싱합니다.
        // 런타임에 남아 있는 이전 EffectEntries 캐시 때문에 새로 추가한 효과가 누락되는 것을 방지합니다.
        return SkillEffectParser.Parse(
            relic,
            DataManager.Instance != null ? DataManager.Instance.EffectDatabase : null);
    }

    private static List<SkillEffectEntry> ResolveRuneEntries(RuneData rune)
    {
        if (rune == null)
            return new List<SkillEffectEntry>();

        // 룬도 현재 GameData 값을 기준으로 매번 파싱합니다.
        // 이전 EffectEntries 캐시가 남아 새 EffectIds가 누락되는 것을 방지합니다.
        return SkillEffectParser.Parse(
            rune,
            DataManager.Instance != null ? DataManager.Instance.EffectDatabase : null);
    }

    private static int GetRepeatedEntryValue(SkillEffectEntry entry)
    {
        if (entry == null)
            return 0;

        return entry.ValueAmount * Mathf.Max(1, entry.CountAmount);
    }

    private static bool MatchesConfiguredTurn(
        SkillEffectEntry entry,
        int playerTurnNumber)
    {
        if (entry == null)
            return false;

        int configuredTurn = entry.CountAmount;
        return configuredTurn <= 0 || configuredTurn == playerTurnNumber;
    }

    private static string ResolveTurnStartStatusEffectId(string turnStartEffectId)
    {
        return turnStartEffectId switch
        {
            TurnStartChargeEffectId => "E_Charge",
            TurnStartFocusEffectId => "E_Focus",
            TurnStartSwiftEffectId => "E_Swift",
            TurnStartBoostEffectId => "E_Boost",
            TurnStartSmiteEffectId => "E_Smite",
            TurnStartLifestealEffectId => "E_Lifesteal",
            _ => string.Empty
        };
    }

    private static string ResolveEnemyTurnStartStatusEffectId(string turnStartEffectId)
    {
        return turnStartEffectId switch
        {
            TurnStartAllEnemyVulnerableEffectId => "E_Vulnerable",
            TurnStartAllEnemyWeakenEffectId => "E_Weaken",
            TurnStartAllEnemyPoisonEffectId => "E_Poison",
            TurnStartAllEnemyBleedEffectId => "E_Bleed",
            _ => string.Empty
        };
    }

    private static bool IsSlotMaskEmpty(int emptySlotMask, int requiredSlotMask)
    {
        int mask = Mathf.Max(0, requiredSlotMask);

        if (mask <= 0)
            return true;

        return (emptySlotMask & mask) == mask;
    }

    private static void QueueNextTurnSmite(
        CharacterRuntimeData runtime,
        string sourceId,
        int stack,
        int targetTurnNumber)
    {
        if (runtime == null || stack <= 0 || targetTurnNumber <= 0)
            return;

        if (runtime.PendingNextTurnSmiteTurn != targetTurnNumber)
        {
            runtime.PendingNextTurnSmiteTurn = targetTurnNumber;
            runtime.PendingNextTurnSmiteStack = 0;
        }

        runtime.PendingNextTurnSmiteStack += stack;
    }

    private static void ApplyQueuedNextTurnSmite(
        CharacterRuntimeData runtime,
        int playerTurnNumber)
    {
        if (runtime == null ||
            runtime.PendingNextTurnSmiteStack <= 0 ||
            runtime.PendingNextTurnSmiteTurn != playerTurnNumber)
        {
            return;
        }

        int stack = runtime.PendingNextTurnSmiteStack;
        runtime.PendingNextTurnSmiteStack = 0;
        runtime.PendingNextTurnSmiteTurn = 0;

        AddTemporaryStatus(runtime, "E_Smite", stack, TurnStartSmiteIfSlotsEmptyMaskEffectId);
    }

    private static void QueueNextTurnBoost(
        CharacterRuntimeData runtime,
        string sourceId,
        int stack,
        int targetTurnNumber)
    {
        if (runtime == null || stack <= 0 || targetTurnNumber <= 0)
            return;

        if (runtime.PendingNextTurnBoostTurn != targetTurnNumber)
        {
            runtime.PendingNextTurnBoostTurn = targetTurnNumber;
            runtime.PendingNextTurnBoostStack = 0;
        }

        runtime.PendingNextTurnBoostStack += stack;
    }

    private static void ApplyQueuedNextTurnBoost(
        CharacterRuntimeData runtime,
        int playerTurnNumber)
    {
        if (runtime == null ||
            runtime.PendingNextTurnBoostStack <= 0 ||
            runtime.PendingNextTurnBoostTurn != playerTurnNumber)
        {
            return;
        }

        int stack = runtime.PendingNextTurnBoostStack;
        runtime.PendingNextTurnBoostStack = 0;
        runtime.PendingNextTurnBoostTurn = 0;

        AddTemporaryStatus(runtime, "E_Boost", stack, TurnStartBoostIfSlotsEmptyMaskEffectId);
    }

    private static void AddTemporaryStatus(
        CharacterRuntimeData runtime,
        string effectId,
        int stack,
        string sourceId)
    {
        if (runtime == null || string.IsNullOrWhiteSpace(effectId) || stack <= 0)
            return;

        if (runtime.StatusEffects == null)
            runtime.StatusEffects = new List<StatusEffectRuntimeData>();

        for (int i = 0; i < runtime.StatusEffects.Count; i++)
        {
            StatusEffectRuntimeData existing = runtime.StatusEffects[i];
            if (existing == null || existing.EffectId != effectId)
                continue;

            existing.Stack += stack;
            existing.TurnCount = Mathf.Max(existing.TurnCount, 1);
            existing.IsPassive = false;
            existing.SourceSkillId = sourceId;
            return;
        }

        runtime.StatusEffects.Add(new StatusEffectRuntimeData
        {
            EffectId = effectId,
            Stack = stack,
            TurnCount = 1,
            IsPassive = false,
            SourceSkillId = sourceId
        });
    }

    private static void AddPassiveStatus(
        CharacterRuntimeData runtime,
        string effectId,
        int stack,
        string sourceId)
    {
        if (runtime == null || string.IsNullOrWhiteSpace(effectId) || stack <= 0)
            return;

        if (runtime.StatusEffects == null)
            runtime.StatusEffects = new List<StatusEffectRuntimeData>();

        runtime.StatusEffects.Add(new StatusEffectRuntimeData
        {
            EffectId = effectId,
            Stack = stack,
            TurnCount = 1,
            IsPassive = true,
            SourceSkillId = sourceId
        });
    }

    private static bool IsRelic07SelectedAttackSkill(
        CharacterRuntimeData runtime,
        string skillId)
    {
        if (runtime == null || string.IsNullOrWhiteSpace(skillId))
            return false;

        List<string> attackSkillIds = GetEquippedAttackSkillIds(runtime);

        if (attackSkillIds.Count <= 0)
            return true;

        attackSkillIds.Sort(System.StringComparer.Ordinal);

        int selectedIndex = GetStableHash(runtime.CharacterId) % attackSkillIds.Count;
        return attackSkillIds[selectedIndex] == skillId;
    }

    private static List<string> GetEquippedAttackSkillIds(CharacterRuntimeData runtime)
    {
        List<string> result = new();

        AddAttackSkillId(result, runtime.UniqueSkillId);
        AddAttackSkillId(result, runtime.AbilitySkillId);

        if (runtime.EquippedSkillIds != null)
        {
            for (int i = 0; i < runtime.EquippedSkillIds.Length; i++)
                AddAttackSkillId(result, runtime.EquippedSkillIds[i]);
        }

        return result;
    }

    private static void AddAttackSkillId(List<string> result, string skillId)
    {
        if (result == null || string.IsNullOrWhiteSpace(skillId))
            return;

        if (result.Contains(skillId))
            return;

        if (DataManager.Instance == null || DataManager.Instance.SkillDatabase == null)
        {
            result.Add(skillId);
            return;
        }

        SkillMasterData skillData = DataManager.Instance.SkillDatabase.Get(skillId);

        if (skillData != null && skillData.SkillType == SkillType.Attack)
            result.Add(skillId);
    }

    private static int GetStableHash(string text)
    {
        unchecked
        {
            int hash = 23;

            if (!string.IsNullOrEmpty(text))
            {
                for (int i = 0; i < text.Length; i++)
                    hash = hash * 31 + text[i];
            }

            return Mathf.Abs(hash);
        }
    }

    private static bool IsDamageEffect(string effectId)
    {
        return effectId == "E_Strike" || effectId == "E_Pierce";
    }

    private static bool ShouldDoubleBuffApplication(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command,
        SkillEffectEntry entry)
    {
        if (GetStatusStack(runtime, BuffApplyDoubleEffectId) <= 0)
            return false;

        if (command == null || command.SkillData == null || entry == null)
            return false;

        if (command.SkillData.SkillType != SkillType.Buff)
            return false;

        return !IsDamageEffect(entry.EffectId);
    }

    private static bool IsLastTimelineSlot(PlayerReservedCommand command)
    {
        return command != null && command.TimelineSlotIndex == LastTimelineSlotIndex;
    }

    private static bool ShouldApplyLowHp(CharacterRuntimeData runtime)
    {
        if (runtime == null || runtime.MaxHP <= 0)
            return false;

        return runtime.CurrentHP / (float)runtime.MaxHP <= 0.3f;
    }

    private static bool ShouldApplyHighHp(CharacterRuntimeData runtime)
    {
        if (runtime == null || runtime.MaxHP <= 0)
            return false;

        return runtime.CurrentHP / (float)runtime.MaxHP >= 0.9f;
    }

    private static bool ShouldApplyFullHp(CharacterRuntimeData runtime)
    {
        if (runtime == null || runtime.MaxHP <= 0)
            return false;

        return runtime.CurrentHP >= runtime.MaxHP;
    }

    private static bool IsUniqueResourceFull(CharacterRuntimeData runtime)
    {
        if (runtime == null ||
            DataManager.Instance == null ||
            DataManager.Instance.CharacterDatabase == null)
        {
            return false;
        }

        if (!DataManager.Instance.CharacterDatabase.TryGet(
                runtime.CharacterId,
                out CharacterMasterData masterData) ||
            masterData == null ||
            masterData.MaxResource <= 0)
        {
            return false;
        }

        return runtime.CurrentResource >= masterData.MaxResource;
    }

    private static bool IsCharacter(CharacterRuntimeData runtime, string characterId)
    {
        return runtime != null && runtime.CharacterId == characterId;
    }

    private static bool TryMarkBattleEffectApplied(
        CharacterRuntimeData runtime,
        string effectId)
    {
        if (runtime == null || string.IsNullOrWhiteSpace(effectId))
            return false;

        if (runtime.AppliedBattleEquipmentEffectIds == null)
            runtime.AppliedBattleEquipmentEffectIds = new List<string>();

        if (runtime.AppliedBattleEquipmentEffectIds.Contains(effectId))
            return false;

        runtime.AppliedBattleEquipmentEffectIds.Add(effectId);
        return true;
    }

    private static bool HasBattleEffectApplied(
        CharacterRuntimeData runtime,
        string effectId)
    {
        if (runtime == null ||
            runtime.AppliedBattleEquipmentEffectIds == null ||
            string.IsNullOrWhiteSpace(effectId))
        {
            return false;
        }

        return runtime.AppliedBattleEquipmentEffectIds.Contains(effectId);
    }

    private static void RemoveBattleEffectApplied(
        CharacterRuntimeData runtime,
        string effectId)
    {
        if (runtime == null ||
            runtime.AppliedBattleEquipmentEffectIds == null ||
            string.IsNullOrWhiteSpace(effectId))
        {
            return;
        }

        runtime.AppliedBattleEquipmentEffectIds.Remove(effectId);
    }

    private static int GetStatusStack(
        CharacterRuntimeData runtime,
        string effectId)
    {
        if (runtime == null ||
            runtime.StatusEffects == null ||
            string.IsNullOrWhiteSpace(effectId))
        {
            return 0;
        }

        for (int i = 0; i < runtime.StatusEffects.Count; i++)
        {
            StatusEffectRuntimeData status = runtime.StatusEffects[i];

            if (status == null)
                continue;

            if (status.EffectId == effectId)
                return Mathf.Max(0, status.Stack);
        }

        return 0;
    }

    private static bool HasStatus(
        List<StatusEffectRuntimeData> statuses,
        string effectId)
    {
        if (statuses == null || string.IsNullOrWhiteSpace(effectId))
            return false;

        for (int i = 0; i < statuses.Count; i++)
        {
            StatusEffectRuntimeData status = statuses[i];

            if (status != null && status.EffectId == effectId)
                return Mathf.Max(0, status.Stack) > 0;
        }

        return false;
    }

    private static void ApplyNegativeCostDelta(
        ref int hpCost,
        ref int cost,
        ref int resourceCost,
        ref int shieldCost,
        int delta)
    {
        int remaining = Mathf.Abs(delta);

        while (remaining > 0)
        {
            int beforeHp = hpCost;
            int beforeCost = cost;
            int beforeResource = resourceCost;
            int beforeShield = shieldCost;

            ReduceFirstPositiveCost(
                ref hpCost,
                ref cost,
                ref resourceCost,
                ref shieldCost);

            if (beforeHp == hpCost &&
                beforeCost == cost &&
                beforeResource == resourceCost &&
                beforeShield == shieldCost)
            {
                return;
            }

            remaining--;
        }
    }

    private static void ReduceFirstPositiveCost(
        ref int hpCost,
        ref int cost,
        ref int resourceCost,
        ref int shieldCost)
    {
        if (cost > 0)
        {
            cost -= 1;
            return;
        }

        if (resourceCost > 0)
        {
            resourceCost -= 1;
            return;
        }

        if (shieldCost > 0)
        {
            shieldCost -= 1;
            return;
        }

        if (hpCost > 0)
            hpCost -= 1;
    }
}
