using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class BattleEffectDebugPreset
{
    public BattleEffectDebugPreset(
        string key,
        string label,
        string[] relicIds,
        string[] runeIds)
    {
        Key = string.IsNullOrWhiteSpace(key) ? "Preset" : key.Trim();
        Label = string.IsNullOrWhiteSpace(label) ? Key : label.Trim();
        RelicIds = relicIds ?? Array.Empty<string>();
        RuneIds = runeIds ?? Array.Empty<string>();
    }

    public string Key { get; }
    public string Label { get; }
    public string[] RelicIds { get; }
    public string[] RuneIds { get; }
}

public static class BattleEffectDebugTool
{
    private static readonly BattleEffectDebugPreset[] DefaultPresets =
    {
        Preset("P01_MaxHP", "P01 최대 체력 +5", "Relic_P_01"),
        Preset("P02_MaxCost", "P02 최대 코스트 +1", "Relic_P_02"),
        Preset("P03_CostRecovery", "P03 코스트 회복 +1", "Relic_P_03"),
        Preset("P04_HPDownCostUp", "P04 HP -8 / 코스트 +2", "Relic_P_04"),
        Preset("P05_HPDownRecoveryUp", "P05 HP -5 / 회복 +1", "Relic_P_05"),
        Preset("P06_AttackCostDamage", "P06 공격 코스트/피해", "Relic_P_06"),
        Preset("P07_AttackDamageCount", "P07 공격 피해/횟수", "Relic_P_07"),
        Preset("P08_DebuffCostValue", "P08 디버프 코스트/수치", "Relic_P_08"),
        Preset("P09_Slot1FirstCost", "P09 1번 첫 스킬 코스트", "Relic_P_09"),
        Preset("P10_Slot1AttackValue", "P10 1번 공격 수치", "Relic_P_10"),
        Preset("P11_Slot1KillFocus", "P11 1번 공격 처치 집중", "Relic_P_11"),
        Preset("P12_Slot1BuffValue", "P12 1번 버프 수치", "Relic_P_12"),
        Preset("P13_Slot5Cost", "P13 5번 슬롯 코스트", "Relic_P_13"),
        Preset("P14_Slot5AttackValue", "P14 5번 공격 수치", "Relic_P_14"),
        Preset("P15_Slot5KillRefund", "P15 5번 처치 코스트 반환", "Relic_P_15"),
        Preset("P16_Slot5Pierce", "P16 5번 공격 관통", "Relic_P_16"),
        Preset("P17_BlockSlot12", "P17 1,2번 슬롯 금지", "Relic_P_17"),
        Preset("P18_OneSlotMoveFree", "P18 한 슬롯 / 이동 무료", "Relic_P_18"),
        Preset("P19_ArmorUsedSlots", "P19 등록 슬롯 방어도", "Relic_P_19"),
        Preset("P20_ArmorEmptySlots", "P20 빈 슬롯 방어도", "Relic_P_20"),
        Preset("P21_Empty123Smite", "P21 1,2,3 빈 슬롯 강타", "Relic_P_21"),
        Preset("P22_Empty345Boost", "P22 3,4,5 빈 슬롯 증폭", "Relic_P_22"),
        Preset("P23_OneAttackBoost", "P23 공격 하나 증폭", "Relic_P_23"),
        Preset("P24_ThreeAttackBoost", "P24 공격 3개 증폭", "Relic_P_24"),
        Preset("P25_Turn1EnemyDebuff", "P25 1턴 적 취약/약화", "Relic_P_25"),
        Preset("P26_Turn1DotDebuff", "P26 1턴 적 중독/출혈", "Relic_P_26"),
        Preset("P27_Turn2Armor", "P27 2턴 방어도", "Relic_P_27"),
        Preset("P28_Turn2ChargeFocus", "P28 2턴 충전/집중", "Relic_P_28"),
        Preset("P29_Turn3SwiftBoost", "P29 3턴 신속/증폭", "Relic_P_29"),
        Preset("P30_Turn3LifestealSmite", "P30 3턴 흡혈/강타", "Relic_P_30"),
        Preset("P31_NoMoveBuffCost", "P31 이동 없음 버프 코스트", "Relic_P_31"),
        Preset("P32_AfterMoveDebuffCost", "P32 이동 후 디버프 코스트", "Relic_P_32"),
        Preset("P33_AfterMoveDebuffValue", "P33 이동 후 디버프 수치", "Relic_P_33"),
        Preset("P34_AfterMoveAttackValue", "P34 이동 코스트 비례 공격", "Relic_P_34"),
        Preset("P35_ForcedMoveImmune", "P35 넉백/그랩 면역", "Relic_P_35"),
        Preset("P36_CrashImmune", "P36 충돌 피해 면역", "Relic_P_36"),
        Preset("P37_GridEffectImmune", "P37 그리드 효과 면역", "Relic_P_37"),
        Preset("P38_OverhealArmor", "P38 최대체력 회복 방어도", "Relic_P_38"),
        Preset("P39_NoDamageSmite", "P39 피해 없음 다음 턴 강타", "Relic_P_39"),
        Preset("P40_NoHealArmorUp", "P40 회복 불가 / 방어도 증가", "Relic_P_40"),
        Preset("P41_Collision", "P41 충돌 추가 피해", "Relic_P_41"),
        Preset("P42_CollisionCharge", "P42 충돌 충전", "Relic_P_42"),
        Preset("P43_CollisionKillFocus", "P43 충돌 처치 집중", "Relic_P_43"),
        Preset("P44_LowHpCost", "P44 낮은 HP 코스트", "Relic_P_44"),
        Preset("P45_LowHpAttack", "P45 낮은 HP 공격 피해", "Relic_P_45"),
        Preset("P46_LowHpReduction", "P46 낮은 HP 피해 감소", "Relic_P_46"),
        Preset("P47_HighHpRecovery", "P47 높은 HP 회복량", "Relic_P_47"),
        Preset("P48_HighHpBuffCost", "P48 높은 HP 버프 코스트", "Relic_P_48"),
        Preset("P49_PoisonedDamage", "P49 중독 대상 추가 피해", "Relic_P_49"),
        Preset("P50_BleedingDamage", "P50 출혈 대상 추가 피해", "Relic_P_50"),
        Preset("P51_VulnerableArmor", "P51 취약 대상 방어도", "Relic_P_51"),
        Preset("P52_WeakenArmor", "P52 약화 대상 방어도", "Relic_P_52"),
        Preset("P53_ApplyPoison", "P53 비중독 대상 중독", "Relic_P_53"),
        Preset("P54_BlockSelfBuff", "P54 자신 버프 차단", "Relic_P_54"),
        Preset("P55_FullHpBuffValue", "P55 최대 HP 버프 수치", "Relic_P_55"),
        Preset("P56_AllyBuffCharge", "P56 아군 버프 충전", "Relic_P_56"),
        Preset("P57_ResourceOverflowCost", "P57 고유자원 초과 코스트", "Relic_P_57"),
        Preset("P58_ResourceMaxCostDown", "P58 고유자원 최대 코스트", "Relic_P_58"),
        Preset("P59_UniqueKillFocus", "P59 고유스킬 처치 집중", "Relic_P_59"),
        Preset("P60_UniqueValue", "P60 고유스킬 수치", "Relic_P_60"),
        Preset("P61_UniqueCount", "P61 고유스킬 횟수", "Relic_P_61"),
        Preset("P62_ZeroCostEndCharge", "P62 코스트 0 종료 충전", "Relic_P_62"),
        Preset("P63_MissCharge", "P63 미적중 충전", "Relic_P_63"),
        Preset("P64_FullHpTargetDamage", "P64 최대 HP 대상 피해", "Relic_P_64"),
        Preset("P65_RestHeal", "P65 휴식 회복량", "Relic_P_65"),
        Preset("P66_ShopDiscount", "P66 상점 할인", "Relic_P_66"),
        Preset("P67_RewardCurrency", "P67 전투 보상 재화", "Relic_P_67"),
        Preset("P68_BattleEndHeal", "P68 전투 종료 회복", "Relic_P_68"),
        Preset("A01_DamageBoost", "A01 이번 턴 피해 증가", "Compound_01"),
        Preset("A02_DamageReduction", "A02 이번 턴 피해 감소", "Compound_02"),
        Preset("A03_RecoverResource", "A03 고유자원 최대 회복", "Compound_03"),
        Preset("A04_RecoverCost", "A04 코스트 최대 회복", "Compound_04"),
        Preset("A05_GrantSwift", "A05 신속 부여", "Compound_05"),
        Preset("A06_Cleanse", "A06 디버프 제거", "Compound_06"),
        Preset("A07_MoveToGrid", "A07 빈 그리드 이동", "Compound_07"),
        Preset("A08_SwapAlly", "A08 아군 위치 교환", "Compound_08"),
        Preset("A09_PoisonGrid", "A09 독 장판", "Compound_09"),
        Preset("A10_ThornGrid", "A10 가시 장판", "Compound_10"),
        Preset("A11_TargetDamageDown", "A11 적 주는 피해 감소", "Compound_11"),
        Preset("A12_Obstacle", "A12 장애물 생성", "Compound_12"),
        Preset("A13_RemoveGrid", "A13 그리드 효과 제거", "Compound_13"),
        Preset("A14_Dummy", "A14 허수아비", "Compound_14"),
        Preset("A15_ExplosiveDoll", "A15 폭발 인형", "Compound_15")
    };

    public static IReadOnlyList<BattleEffectDebugPreset> GetDefaultPresets()
    {
        return DefaultPresets;
    }

    public static void ApplyPreset(CharacterRuntimeData runtime, BattleEffectDebugPreset preset)
    {
        if (runtime == null || preset == null)
            return;

        EquipOnlyRelics(runtime, preset.RelicIds);
        EquipOnlyRunes(runtime, preset.RuneIds);

        runtime.ActiveRelicUses ??= new List<ActiveRelicUseRuntimeData>();
        runtime.ActiveRelicUses.Clear();

        runtime.AppliedBattleEquipmentEffectIds ??= new List<string>();
        runtime.AppliedBattleEquipmentEffectIds.Clear();
    }

    public static void EquipOnlyRelics(CharacterRuntimeData runtime, IReadOnlyList<string> relicIds)
    {
        if (runtime == null)
            return;

        runtime.EquippedRelicIds = new string[7];

        if (relicIds == null)
            return;

        int passiveSlotIndex = 1;

        for (int i = 0; i < relicIds.Count; i++)
        {
            string relicId = NormalizeId(relicIds[i]);

            if (string.IsNullOrWhiteSpace(relicId))
                continue;

            if (IsActiveRelicId(relicId))
            {
                runtime.EquippedRelicIds[0] = relicId;
                continue;
            }

            if (passiveSlotIndex >= runtime.EquippedRelicIds.Length)
                break;

            runtime.EquippedRelicIds[passiveSlotIndex] = relicId;
            passiveSlotIndex++;
        }
    }

    public static void EquipOnlyRunes(CharacterRuntimeData runtime, IReadOnlyList<string> runeIds)
    {
        if (runtime == null)
            return;

        runtime.EquippedRuneIds = new string[12];

        if (runeIds == null)
            return;

        int slotIndex = 0;

        for (int i = 0; i < runeIds.Count && slotIndex < runtime.EquippedRuneIds.Length; i++)
        {
            string runeId = NormalizeId(runeIds[i]);

            if (string.IsNullOrWhiteSpace(runeId))
                continue;

            runtime.EquippedRuneIds[slotIndex] = runeId;
            slotIndex++;
        }
    }

    public static void SetHpPercent(CharacterRuntimeData runtime, float percent)
    {
        if (runtime == null)
            return;

        int maxHp = Mathf.Max(1, runtime.MaxHP);
        int hp = Mathf.RoundToInt(maxHp * Mathf.Clamp01(percent));
        runtime.CurrentHP = Mathf.Clamp(hp, 1, maxHp);
    }

    public static void SetCurrentCost(CharacterRuntimeData runtime, int cost)
    {
        if (runtime == null)
            return;

        runtime.CurrentCost = Mathf.Clamp(cost, 0, Mathf.Max(0, runtime.MaxCost));
    }

    public static void SetCurrentResource(CharacterRuntimeData runtime, int resource, int maxResource)
    {
        if (runtime == null)
            return;

        runtime.CurrentResource = Mathf.Clamp(resource, 0, Mathf.Max(0, maxResource));
    }

    public static void SetFullResources(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return;

        SetCurrentCost(runtime, runtime.MaxCost);
        int maxResource = GetMaxResource(runtime);
        SetCurrentResource(runtime, maxResource, maxResource);
    }

    public static int GetMaxResource(CharacterRuntimeData runtime)
    {
        if (runtime == null ||
            DataManager.Instance == null ||
            DataManager.Instance.CharacterDatabase == null ||
            !DataManager.Instance.CharacterDatabase.TryGet(
                runtime.CharacterId,
                out CharacterMasterData masterData) ||
            masterData == null)
        {
            return Mathf.Max(0, runtime != null ? runtime.CurrentResource : 0);
        }

        return Mathf.Max(0, masterData.MaxResource);
    }

    public static bool AddOrStackStatus(
        List<StatusEffectRuntimeData> statusEffects,
        string effectId,
        int stack,
        int turnCount)
    {
        if (statusEffects == null || string.IsNullOrWhiteSpace(effectId))
            return false;

        string normalizedEffectId = effectId.Trim();
        int safeStack = Mathf.Max(1, stack);
        int safeTurnCount = Mathf.Max(1, turnCount);

        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectRuntimeData status = statusEffects[i];

            if (status == null || status.EffectId != normalizedEffectId)
                continue;

            status.Stack += safeStack;
            status.TurnCount = Mathf.Max(status.TurnCount, safeTurnCount);
            return true;
        }

        statusEffects.Add(new StatusEffectRuntimeData
        {
            EffectId = normalizedEffectId,
            Stack = safeStack,
            TurnCount = safeTurnCount
        });
        return true;
    }

    public static void AddStatusToAllMonsters(string effectId, int stack, int turnCount)
    {
        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                continue;

            BattleEffectUtility.AddStatusToMonster(
                monster,
                effectId,
                Mathf.Max(1, stack),
                Mathf.Max(1, turnCount));
        }
    }

    public static void AddStatusToPlayer(
        CharacterRuntimeData runtime,
        string effectId,
        int stack,
        int turnCount)
    {
        if (runtime == null)
            return;

        runtime.StatusEffects ??= new List<StatusEffectRuntimeData>();
        AddOrStackStatus(runtime.StatusEffects, effectId, stack, turnCount);
    }

    public static bool TryPlaceGridEffect(int gridIndex, string gridEffectId)
    {
        BattleGridEffectController controller = Object.FindFirstObjectByType<BattleGridEffectController>(
            FindObjectsInactive.Include);

        return controller != null && controller.TryPlaceEffect(gridIndex, gridEffectId);
    }

    public static bool TryRemoveGridEffect(int gridIndex)
    {
        BattleGridEffectController controller = Object.FindFirstObjectByType<BattleGridEffectController>(
            FindObjectsInactive.Include);

        return controller != null && controller.TryRemoveEffect(gridIndex);
    }

    public static void RefreshBattle()
    {
        BattleRoomLoader loader = Object.FindFirstObjectByType<BattleRoomLoader>(
            FindObjectsInactive.Include);

        if (loader != null)
            loader.RefreshBattleHUDs();

        new BattleHUDService().RefreshHUDs();
    }

    public static void ReloadBattleRoom()
    {
        BattleRoomLoader loader = Object.FindFirstObjectByType<BattleRoomLoader>(
            FindObjectsInactive.Include);

        if (loader == null)
            return;

        loader.ResetLoadedStateForNextBattle(true);
        loader.RequestLoadBattle();
    }

    public static CharacterRuntimeData GetPartyRuntime(int partyIndex)
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.PartyRuntimeStore == null ||
            DataManager.Instance.CharacterRuntimeStore == null)
        {
            return null;
        }

        string characterId = DataManager.Instance.PartyRuntimeStore.GetCharacterId(partyIndex);

        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        return DataManager.Instance.CharacterRuntimeStore.TryGet(
            characterId,
            out CharacterRuntimeData runtime)
            ? runtime
            : null;
    }

    public static List<CharacterRuntimeData> GetPartyRuntimes()
    {
        List<CharacterRuntimeData> result = new();

        if (DataManager.Instance == null || DataManager.Instance.PartyRuntimeStore == null)
            return result;

        for (int i = 0; i < DataManager.Instance.PartyRuntimeStore.MaxPartyCountValue; i++)
        {
            CharacterRuntimeData runtime = GetPartyRuntime(i);

            if (runtime != null)
                result.Add(runtime);
        }

        return result;
    }

    private static BattleEffectDebugPreset Preset(string key, string label, params string[] relicIds)
    {
        return new BattleEffectDebugPreset(key, label, relicIds, null);
    }

    private static bool IsActiveRelicId(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return false;

        if (DataManager.Instance != null &&
            DataManager.Instance.CompoundDatabase != null &&
            DataManager.Instance.CompoundDatabase.TryGet(relicId, out CompoundData compound) &&
            compound != null)
        {
            return true;
        }

        return relicId.StartsWith("Compound_", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}
