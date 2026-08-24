using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public sealed class ActiveRelicAvailability
{
    public bool CanUse;
    public string Message;
    public string RelicId;
    public RelicData RelicData;
    public string EffectId;
    public ActiveRelicTargetMode TargetMode;
    public int RemainingUses;
    public int MaxUses;

    public bool RequiresTarget =>
        TargetMode == ActiveRelicTargetMode.Grid ||
        TargetMode == ActiveRelicTargetMode.AllyGrid ||
        TargetMode == ActiveRelicTargetMode.EnemyGrid;
}

public sealed class ActiveRelicUseResult
{
    public bool Succeeded;
    public bool ConsumedUse;
    public string Message;
    public ActiveRelicAvailability Availability;

    public static ActiveRelicUseResult Success(
        ActiveRelicAvailability availability,
        bool consumedUse = true,
        string message = "")
    {
        return new ActiveRelicUseResult
        {
            Succeeded = true,
            ConsumedUse = consumedUse,
            Message = message,
            Availability = availability
        };
    }

    public static ActiveRelicUseResult Fail(
        ActiveRelicAvailability availability,
        string message)
    {
        return new ActiveRelicUseResult
        {
            Succeeded = false,
            ConsumedUse = false,
            Message = message,
            Availability = availability
        };
    }
}

public sealed class ActiveRelicService
{
    private readonly CompoundDatabase compoundDatabase;

    public ActiveRelicService(CompoundDatabase compoundDatabase)
    {
        this.compoundDatabase = compoundDatabase;
    }

    public ActiveRelicAvailability GetAvailability(CharacterRuntimeData runtime)
    {
        ActiveRelicAvailability availability = new()
        {
            CanUse = false,
            Message = "Active relic is unavailable."
        };

        if (runtime == null)
        {
            availability.Message = "Character is missing.";
            return availability;
        }

        if (runtime.IsDead)
        {
            availability.Message = "Dead character cannot use relic.";
            return availability;
        }

        string relicId = ActiveRelicRuntimeUtility.GetActiveRelicId(runtime);

        if (string.IsNullOrWhiteSpace(relicId))
        {
            availability.Message = "No active relic equipped.";
            return availability;
        }

        availability.RelicId = relicId;

        if (compoundDatabase == null ||
            !compoundDatabase.TryGet(relicId, out CompoundData relic) ||
            relic == null)
        {
            availability.Message = "Compound data is missing.";
            return availability;
        }

        availability.RelicData = relic;
        availability.MaxUses = ActiveRelicRuntimeUtility.GetMaxUses(relic);
        availability.RemainingUses = ActiveRelicRuntimeUtility.GetRemainingUses(runtime, relic);
        availability.EffectId = ActiveRelicEffectResolver.ResolveEffectId(relic);
        availability.TargetMode = ActiveRelicEffectResolver.ResolveTargetMode(availability.EffectId);

        if (!ActiveRelicEffectResolver.IsActiveRelic(relic))
        {
            availability.Message = "Equipped relic is not active.";
            return availability;
        }

        if (availability.MaxUses <= 0)
        {
            availability.Message = "Relic has no uses.";
            return availability;
        }

        if (availability.TargetMode == ActiveRelicTargetMode.None ||
            string.IsNullOrWhiteSpace(availability.EffectId))
        {
            availability.Message = "Relic effect is not supported.";
            return availability;
        }

        if (availability.RemainingUses <= 0)
        {
            availability.Message = "No relic uses remain.";
            return availability;
        }

        availability.CanUse = true;
        availability.Message = string.Empty;
        return availability;
    }

    public ActiveRelicUseResult TryUseImmediate(CharacterRuntimeData runtime)
    {
        ActiveRelicAvailability availability = GetAvailability(runtime);

        if (!availability.CanUse)
            return ActiveRelicUseResult.Fail(availability, availability.Message);

        if (availability.TargetMode != ActiveRelicTargetMode.Self)
            return ActiveRelicUseResult.Fail(availability, "Relic requires a target.");

        if (!TryApplyImmediateSelfEffect(runtime, availability, out string message))
            return ActiveRelicUseResult.Fail(availability, message);

        if (!ActiveRelicRuntimeUtility.TryConsumeUse(runtime, availability.RelicData))
            return ActiveRelicUseResult.Fail(availability, "No relic uses remain.");

        availability.RemainingUses = ActiveRelicRuntimeUtility.GetRemainingUses(
            runtime,
            availability.RelicData);

        RefreshBattleHud();
        return ActiveRelicUseResult.Success(availability);
    }

    public ActiveRelicUseResult TryUseTarget(
        CharacterRuntimeData runtime,
        int gridIndex,
        GridManager gridManager,
        BattleGridEffectController gridEffectController)
    {
        ActiveRelicAvailability availability = GetAvailability(runtime);

        if (!availability.CanUse)
            return ActiveRelicUseResult.Fail(availability, availability.Message);

        if (!availability.RequiresTarget)
            return ActiveRelicUseResult.Fail(availability, "Relic does not require a target.");

        string targetMessage;
        bool succeeded;

        switch (availability.EffectId)
        {
            case ActiveRelicEffectIds.MoveToGrid:
                succeeded = TryMoveToGrid(
                    runtime,
                    gridIndex,
                    gridManager,
                    gridEffectController,
                    out targetMessage);
                break;

            case ActiveRelicEffectIds.SwapAlly:
                succeeded = TrySwapWithAlly(
                    runtime,
                    gridIndex,
                    gridManager,
                    out targetMessage);
                break;

            case ActiveRelicEffectIds.TargetOutgoingDamageReductionThisTurn:
                succeeded = TryApplyTargetOutgoingDamageReduction(
                    availability.RelicData,
                    gridIndex,
                    gridManager,
                    out targetMessage);
                break;

            case ActiveRelicEffectIds.RemoveGridEffect:
                succeeded = TryRemoveGridEffect(
                    gridIndex,
                    gridManager,
                    gridEffectController,
                    out targetMessage);
                break;

            case ActiveRelicEffectIds.SpawnGridEffect:
            case ActiveRelicEffectIds.SpawnPoisonGridEffect:
            case ActiveRelicEffectIds.SpawnThornGridEffect:
            case ActiveRelicEffectIds.SpawnObstacleGridEffect:
            case ActiveRelicEffectIds.SpawnDummyGridEffect:
            case ActiveRelicEffectIds.SpawnExplosiveDollGridEffect:
                succeeded = TrySpawnGridEffect(
                    availability.RelicData,
                    gridIndex,
                    gridManager,
                    gridEffectController,
                    out targetMessage);
                break;

            default:
                succeeded = FailUnsupported(out targetMessage);
                break;
        }

        if (!succeeded)
            return ActiveRelicUseResult.Fail(availability, targetMessage);

        if (!ActiveRelicRuntimeUtility.TryConsumeUse(runtime, availability.RelicData))
            return ActiveRelicUseResult.Fail(availability, "No relic uses remain.");

        availability.RemainingUses = ActiveRelicRuntimeUtility.GetRemainingUses(
            runtime,
            availability.RelicData);

        RefreshBattleHud();
        return ActiveRelicUseResult.Success(availability);
    }

    private static bool TryMoveToGrid(
        CharacterRuntimeData runtime,
        int gridIndex,
        GridManager gridManager,
        BattleGridEffectController gridEffectController,
        out string message)
    {
        message = string.Empty;

        if (!TryGetValidTargetCell(gridManager, gridIndex, out _))
        {
            message = "Invalid target cell.";
            return false;
        }

        if (!TryFindBattleCharacter(runtime?.CharacterId, out BattleCharacter character))
        {
            message = "Battle character is missing.";
            return false;
        }

        if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex, runtime.CharacterId))
        {
            message = "Target cell is occupied.";
            return false;
        }

        if (gridEffectController != null && gridEffectController.IsBlocked(gridIndex))
        {
            message = "Target cell is blocked.";
            return false;
        }

        MoveCharacterToGrid(character, gridIndex, gridManager);
        CancelInvalidatedReservations(runtime);
        gridEffectController?.ApplyToPlayer(gridIndex, character);
        return true;
    }

    private static bool TrySwapWithAlly(
        CharacterRuntimeData runtime,
        int targetGridIndex,
        GridManager gridManager,
        out string message)
    {
        message = string.Empty;

        if (!TryGetValidTargetCell(gridManager, targetGridIndex, out _))
        {
            message = "Invalid target cell.";
            return false;
        }

        if (!TryFindBattleCharacter(runtime?.CharacterId, out BattleCharacter character))
        {
            message = "Battle character is missing.";
            return false;
        }

        if (!TryFindAllyAtGrid(targetGridIndex, runtime.CharacterId, out BattleCharacter ally))
        {
            message = "No ally at target cell.";
            return false;
        }

        int sourceGridIndex = character.CurrentGridIndex;

        if (sourceGridIndex < 0)
        {
            message = "Character grid is invalid.";
            return false;
        }

        MoveCharacterToGrid(character, targetGridIndex, gridManager);
        MoveCharacterToGrid(ally, sourceGridIndex, gridManager);
        CancelInvalidatedReservations(runtime);
        CancelInvalidatedReservations(ally.RuntimeData);
        return true;
    }


    private static void CancelInvalidatedReservations(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return;

        BattleTimelineController timelineController =
            Object.FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);

        if (timelineController == null)
            return;

        timelineController.CancelMoveAndAttackReservations(runtime);
    }

    private static bool TryApplyImmediateSelfEffect(
        CharacterRuntimeData runtime,
        ActiveRelicAvailability availability,
        out string message)
    {
        message = string.Empty;

        if (runtime == null || availability == null)
        {
            message = "Character is missing.";
            return false;
        }

        switch (availability.EffectId)
        {
            case ActiveRelicEffectIds.DamageBoostThisTurn:
            case ActiveRelicEffectIds.DamageReductionThisTurn:
                if (!ActiveRelicRuntimeUtility.TryAddTurnScopedStatus(runtime, availability.EffectId))
                {
                    message = "Relic effect is already active.";
                    return false;
                }

                return true;

            case ActiveRelicEffectIds.RecoverCostToMax:
                {
                    // Compound_04: 현재/최대 마나와 관계없이 마나를 정확히 10 회복한다.
                    // 최대 마나(MaxCost)와 예약 마나(ReservedCost)는 변경하지 않는다.
                    // 예) CurrentCost 10, ReservedCost 8 -> PreviewCost 2/10
                    //     사용 후 CurrentCost 20, ReservedCost 8 -> PreviewCost 12/10
                    //     예약 취소 후 ReservedCost 0 -> PreviewCost 20/10
                    int recoverAmount = Mathf.Max(0, GetRelicValue(availability.RelicData, 10));
                    runtime.CurrentCost = Mathf.Max(0, runtime.CurrentCost) + recoverAmount;
                    return true;
                }

            case ActiveRelicEffectIds.RecoverUniqueResourceToMax:
                {
                    // Compound_03: 카르마를 정확히 3 회복하되 총 보유량은 최대치를 넘지 않는다.
                    // CurrentResource는 예약된 카르마까지 포함한 총 보유량이므로
                    // ReservedResourceCost를 건드리지 않고 CurrentResource만 회복하면
                    // 예약 취소로 카르마가 복제되는 문제를 막을 수 있다.
                    // 예) 최대 5, CurrentResource 5, ReservedResourceCost 3 -> PreviewResource 2
                    //     다시 사용해도 총 보유량이 이미 5이므로 회복 0
                    //     예약 취소 후 PreviewResource는 5
                    int maxKarma = ResolveMaxUniqueResource(runtime);
                    int recoverAmount = Mathf.Max(0, GetRelicValue(availability.RelicData, 3));
                    int finalAmount = BattleEquipmentEffectService.ModifyUniqueResourceGain(runtime, recoverAmount);
                    int previousResource = Mathf.Max(0, runtime.CurrentResource);

                    BattleEquipmentEffectService.ApplyUniqueResourceGainSideEffects(
                        runtime,
                        finalAmount,
                        previousResource,
                        maxKarma);

                    runtime.CurrentResource = Mathf.Min(maxKarma, previousResource + finalAmount);
                    return true;
                }

            case ActiveRelicEffectIds.GrantSwift:
                AddOrStackStatus(runtime, "E_Swift", GetRelicValue(availability.RelicData, 2));
                return true;

            case ActiveRelicEffectIds.CleanseDebuffs:
                RemoveDebuffs(runtime);
                return true;

            default:
                message = "Relic effect is not supported.";
                return false;
        }
    }

    private static bool TrySpawnGridEffect(
        RelicData relic,
        int gridIndex,
        GridManager gridManager,
        BattleGridEffectController gridEffectController,
        out string message)
    {
        message = string.Empty;

        if (gridEffectController == null)
        {
            message = "Grid effect controller is missing.";
            return false;
        }

        if (!TryGetValidTargetCell(gridManager, gridIndex, out _))
        {
            message = "Invalid target cell.";
            return false;
        }

        if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex))
        {
            message = "Target cell is occupied.";
            return false;
        }

        if (gridEffectController.HasEffect(gridIndex))
        {
            message = "Target cell already has an effect.";
            return false;
        }

        string gridEffectId = ActiveRelicEffectResolver.ResolveGridEffectId(relic);

        if (string.IsNullOrWhiteSpace(gridEffectId))
        {
            message = "Relic grid effect is missing.";
            return false;
        }

        if (!gridEffectController.TryPlaceEffect(gridIndex, gridEffectId))
        {
            message = "Failed to place grid effect.";
            return false;
        }

        return true;
    }

    private static bool TryApplyTargetOutgoingDamageReduction(
        RelicData relic,
        int gridIndex,
        GridManager gridManager,
        out string message)
    {
        message = string.Empty;

        if (!TryGetValidTargetCell(gridManager, gridIndex, out _))
        {
            message = "Invalid target cell.";
            return false;
        }

        if (!TryFindMonsterAtGrid(gridIndex, out MonsterUnit monster))
        {
            message = "No enemy at target cell.";
            return false;
        }

        AddOrStackStatus(
            monster.RuntimeData.StatusEffects,
            ActiveRelicEffectIds.TargetOutgoingDamageReductionThisTurn,
            1);
        monster.ShowAndRefreshHUD();
        return true;
    }

    private static bool TryRemoveGridEffect(
        int gridIndex,
        GridManager gridManager,
        BattleGridEffectController gridEffectController,
        out string message)
    {
        message = string.Empty;

        if (gridEffectController == null)
        {
            message = "Grid effect controller is missing.";
            return false;
        }

        if (!TryGetValidTargetCell(gridManager, gridIndex, out _))
        {
            message = "Invalid target cell.";
            return false;
        }

        if (!gridEffectController.HasEffect(gridIndex))
        {
            message = "Target cell has no grid effect.";
            return false;
        }

        if (!gridEffectController.TryRemoveEffect(gridIndex))
        {
            message = "Failed to remove grid effect.";
            return false;
        }

        return true;
    }

    private static int ResolveMaxUniqueResource(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return 0;

        CharacterMasterData masterData =
            DataManager.Instance?.CharacterDatabase?.Get(runtime.CharacterId);

        if (masterData != null)
            return Mathf.Max(0, masterData.MaxResource);

        return Mathf.Max(0, runtime.CurrentResource);
    }

    private static int GetRelicValue(RelicData relic, int fallback)
    {
        if (relic != null && int.TryParse(relic.ValueRate, out int value))
            return Mathf.Max(0, value);

        if (relic?.EffectEntries != null && relic.EffectEntries.Count > 0)
            return Mathf.Max(0, relic.EffectEntries[0].ValueAmount);

        return Mathf.Max(0, fallback);
    }

    private static void AddOrStackStatus(
        CharacterRuntimeData runtime,
        string effectId,
        int stack)
    {
        if (runtime == null || string.IsNullOrWhiteSpace(effectId) || stack <= 0)
            return;

        runtime.StatusEffects ??= new System.Collections.Generic.List<StatusEffectRuntimeData>();
        AddOrStackStatus(runtime.StatusEffects, effectId, stack);
    }

    private static void AddOrStackStatus(
        System.Collections.Generic.List<StatusEffectRuntimeData> statuses,
        string effectId,
        int stack)
    {
        if (statuses == null || string.IsNullOrWhiteSpace(effectId) || stack <= 0)
            return;

        for (int i = 0; i < statuses.Count; i++)
        {
            StatusEffectRuntimeData status = statuses[i];

            if (status == null || status.EffectId != effectId)
                continue;

            status.Stack += stack;
            status.TurnCount = Mathf.Max(status.TurnCount, 1);
            return;
        }

        statuses.Add(new StatusEffectRuntimeData
        {
            EffectId = effectId,
            Stack = stack,
            TurnCount = 1
        });
    }

    private static void RemoveDebuffs(CharacterRuntimeData runtime)
    {
        if (runtime?.StatusEffects == null)
            return;

        for (int i = runtime.StatusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffectRuntimeData status = runtime.StatusEffects[i];

            if (status == null)
                continue;

            if (IsDebuff(status.EffectId))
                runtime.StatusEffects.RemoveAt(i);
        }
    }

    private static bool IsDebuff(string effectId)
    {
        return effectId == "E_Poison" ||
               effectId == "E_Bleed" ||
               effectId == "E_Vulnerable" ||
               effectId == "E_Weaken" ||
               effectId == "E_Corrosion" ||
               effectId == "E_Burn";
    }

    private static bool TryGetValidTargetCell(
        GridManager gridManager,
        int gridIndex,
        out GridCell cell)
    {
        cell = null;

        if (gridManager == null || gridIndex < 0)
            return false;

        cell = gridManager.GetCellByIndex(gridIndex);
        return cell != null;
    }

    private static bool TryFindBattleCharacter(
        string characterId,
        out BattleCharacter character)
    {
        character = null;

        if (string.IsNullOrWhiteSpace(characterId))
            return false;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter candidate = characters[i];

            if (candidate == null ||
                candidate.RuntimeData == null ||
                candidate.RuntimeData.IsDead)
            {
                continue;
            }

            if (candidate.CharacterId == characterId)
            {
                character = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool TryFindAllyAtGrid(
        int gridIndex,
        string selfCharacterId,
        out BattleCharacter ally)
    {
        ally = null;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter candidate = characters[i];

            if (candidate == null ||
                candidate.RuntimeData == null ||
                candidate.RuntimeData.IsDead ||
                candidate.CurrentGridIndex != gridIndex)
            {
                continue;
            }

            if (candidate.CharacterId == selfCharacterId)
                continue;

            ally = candidate;
            return true;
        }

        return false;
    }

    private static bool TryFindMonsterAtGrid(
        int gridIndex,
        out MonsterUnit monster)
    {
        monster = null;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit candidate = monsters[i];

            if (candidate == null ||
                candidate.RuntimeData == null ||
                candidate.RuntimeData.IsDead)
            {
                continue;
            }

            if (candidate.MainGridIndex == gridIndex)
            {
                monster = candidate;
                return true;
            }

            IReadOnlyList<int> occupiedGridIndices = candidate.OccupiedGridIndices;

            if (occupiedGridIndices == null)
                continue;

            for (int j = 0; j < occupiedGridIndices.Count; j++)
            {
                if (occupiedGridIndices[j] != gridIndex)
                    continue;

                monster = candidate;
                return true;
            }
        }

        return false;
    }

    private static void MoveCharacterToGrid(
        BattleCharacter character,
        int gridIndex,
        GridManager gridManager)
    {
        if (character == null || gridManager == null)
            return;

        character.SetGridIndex(gridIndex);
        character.transform.position = gridManager.GetWorldPositionByIndex(gridIndex);
        UpdatePartyCurrentGridIndex(character.CharacterId, gridIndex);
    }

    private static void UpdatePartyCurrentGridIndex(string characterId, int gridIndex)
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.PartyRuntimeStore == null ||
            string.IsNullOrWhiteSpace(characterId))
        {
            return;
        }

        int slotIndex = DataManager.Instance.PartyRuntimeStore.FindCharacterSlot(characterId);

        if (slotIndex >= 0)
            DataManager.Instance.PartyRuntimeStore.SetCurrentGridIndex(slotIndex, gridIndex);
    }

    private static void RefreshBattleHud()
    {
        BattleRoomLoader loader = Object.FindFirstObjectByType<BattleRoomLoader>(
            FindObjectsInactive.Include);

        if (loader != null)
            loader.RefreshBattleHUDs();
    }

    private static bool FailUnsupported(out string message)
    {
        message = "Relic effect is not supported.";
        return false;
    }
}
