using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleUniqueResourceService
{
    private const int ResourceGainAmount = 1;
    private const int SameSlotActionRequiredCount = 2;
    private const int CostRequiredInTurn = 8;

    private readonly Dictionary<int, Dictionary<string, int>> executedActionCountsBySlot = new();
    private readonly Dictionary<string, int> executedCostByCharacter = new();
    private readonly Dictionary<CharacterRuntimeData, int> pendingResourceGainByCharacter = new();
    private bool isTurnExecutionActive;

    public void BeginTurnExecution()
    {
        executedActionCountsBySlot.Clear();
        executedCostByCharacter.Clear();
        pendingResourceGainByCharacter.Clear();
        isTurnExecutionActive = true;
    }

    public void FlushPendingUniqueResourceGains()
    {
        if (pendingResourceGainByCharacter.Count == 0)
        {
            isTurnExecutionActive = false;
            return;
        }

        KeyValuePair<CharacterRuntimeData, int>[] pendingEntries =
            new KeyValuePair<CharacterRuntimeData, int>[pendingResourceGainByCharacter.Count];

        int index = 0;
        foreach (KeyValuePair<CharacterRuntimeData, int> entry in pendingResourceGainByCharacter)
            pendingEntries[index++] = entry;

        pendingResourceGainByCharacter.Clear();
        isTurnExecutionActive = false;

        for (int i = 0; i < pendingEntries.Length; i++)
            ApplyUniqueResourceGainNow(pendingEntries[i].Key, pendingEntries[i].Value);
    }

    public void OnPlayerCommandExecuted(PlayerReservedCommand command, int slotIndex)
    {
        if (command == null || command.UserRuntime == null || command.SkillData == null)
            return;

        CharacterRuntimeData runtime = command.UserRuntime;

        CheckKayaAfterExecutedAction(runtime, command, slotIndex);
        CheckHazeAfterExecutedAction(runtime, command);
    }

    public void OnAnyPlayerDamaged(BattleCharacter damagedCharacter)
    {
        if (damagedCharacter == null ||
            damagedCharacter.RuntimeData == null ||
            damagedCharacter.RuntimeData.IsDead)
        {
            return;
        }

        CharacterMasterData masterData = GetMasterData(damagedCharacter.RuntimeData.CharacterId);

        if (masterData == null ||
            masterData.ResourceTrigger != ResourceTrigger.OnAnyAllyDamaged)
        {
            return;
        }

        // 힐트는 체력 감소 여부와 관계없이 직접 피격된 순간에 분노를 획득합니다.
        AddUniqueResource(damagedCharacter.RuntimeData, ResourceGainAmount);
    }

    public void OnPlayerBuffApplied(BattleCharacter buffedCharacter)
    {
        if (buffedCharacter == null || buffedCharacter.RuntimeData == null)
            return;

        AddResourceToTriggerOwners(ResourceTrigger.OnAllyBuffApplied);
    }

    public void OnPlayerDamagedEnemy(BattleCharacter attacker)
    {
        if (attacker == null || attacker.RuntimeData == null || attacker.RuntimeData.IsDead)
            return;

        CharacterMasterData masterData = GetMasterData(attacker.RuntimeData.CharacterId);

        if (masterData == null || masterData.ResourceTrigger != ResourceTrigger.OnDamageEnemy)
            return;

        AddUniqueResource(attacker.RuntimeData, ResourceGainAmount);
    }

    private void CheckKayaAfterExecutedAction(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command,
        int slotIndex)
    {
        CharacterMasterData masterData = GetMasterData(runtime.CharacterId);

        if (masterData == null ||
            masterData.ResourceTrigger != ResourceTrigger.OnThreeActionsInSameSlot)
        {
            return;
        }

        // 이동 예약은 카야의 같은 슬롯 행동 횟수에서 제외합니다.
        if (command.ReservedMoveGridIndex >= 0)
            return;

        if (!executedActionCountsBySlot.TryGetValue(slotIndex, out Dictionary<string, int> counts))
        {
            counts = new Dictionary<string, int>();
            executedActionCountsBySlot[slotIndex] = counts;
        }

        counts.TryGetValue(runtime.CharacterId, out int count);
        count++;
        counts[runtime.CharacterId] = count;

        if (count % SameSlotActionRequiredCount != 0)
            return;

        AddUniqueResource(runtime, ResourceGainAmount);

        Debug.Log(
            $"[UniqueResource] ActionsInSameSlot / " +
            $"Slot:{slotIndex} / Character:{runtime.CharacterId} / Count:{count}");
    }

    private void CheckHazeAfterExecutedAction(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command)
    {
        CharacterMasterData masterData = GetMasterData(runtime.CharacterId);

        if (masterData == null ||
            masterData.ResourceTrigger != ResourceTrigger.OnSpendEightCostInTurn)
        {
            return;
        }

        executedCostByCharacter.TryGetValue(runtime.CharacterId, out int previousSpent);

        int spent = previousSpent + Mathf.Max(0, command.Cost);
        executedCostByCharacter[runtime.CharacterId] = spent;

        int previousMilestone = previousSpent / CostRequiredInTurn;
        int currentMilestone = spent / CostRequiredInTurn;
        int gainedCount = currentMilestone - previousMilestone;

        if (gainedCount <= 0)
            return;

        AddUniqueResource(runtime, gainedCount * ResourceGainAmount);

        Debug.Log(
            $"[UniqueResource] SpendEightCostInTurn / " +
            $"Character:{runtime.CharacterId} / Spent:{spent} / Gain:{gainedCount}");
    }

    private void AddResourceToTriggerOwners(ResourceTrigger trigger)
    {
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
                continue;

            CharacterMasterData masterData = GetMasterData(character.RuntimeData.CharacterId);

            if (masterData == null || masterData.ResourceTrigger != trigger)
                continue;

            AddUniqueResource(character.RuntimeData, ResourceGainAmount);
        }
    }

    private void AddUniqueResource(CharacterRuntimeData runtime, int amount)
    {
        if (runtime == null || amount <= 0)
            return;

        if (!isTurnExecutionActive)
        {
            ApplyUniqueResourceGainNow(runtime, amount);
            return;
        }

        pendingResourceGainByCharacter.TryGetValue(runtime, out int pendingAmount);
        pendingResourceGainByCharacter[runtime] = pendingAmount + amount;

        Debug.Log(
            $"[UniqueResource] Pending / Character:{runtime.CharacterId} / " +
            $"Added:{amount} / Pending:{pendingResourceGainByCharacter[runtime]}");
    }

    private void ApplyUniqueResourceGainNow(CharacterRuntimeData runtime, int amount)
    {
        if (runtime == null || amount <= 0)
            return;

        CharacterMasterData masterData = GetMasterData(runtime.CharacterId);

        int maxResource = masterData != null
            ? Mathf.Max(0, masterData.MaxResource)
            : 999;

        int finalAmount = BattleEquipmentEffectService.ModifyUniqueResourceGain(runtime, amount);
        int previousResource = runtime.CurrentResource;

        BattleEquipmentEffectService.ApplyUniqueResourceGainSideEffects(
            runtime,
            finalAmount,
            previousResource,
            maxResource);

        runtime.CurrentResource = Mathf.Min(maxResource, runtime.CurrentResource + finalAmount);

        int gainedAmount = Mathf.Max(0, runtime.CurrentResource - previousResource);
        if (gainedAmount <= 0)
            return;

        RefreshPlayerHUDs();
        ShowUniqueResourcePopup(runtime, masterData, gainedAmount);

        Debug.Log($"[UniqueResource] {runtime.CharacterId} +{gainedAmount} / {runtime.CurrentResource}/{maxResource}");
    }


    private void ShowUniqueResourcePopup(
        CharacterRuntimeData runtime,
        CharacterMasterData masterData,
        int gainedAmount)
    {
        if (runtime == null || masterData == null || gainedAmount <= 0)
            return;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData != runtime)
                continue;

            BattleDamageTextPopupUI.ShowUniqueResource(
                character.transform,
                GetResourceDisplayName(masterData.ResourceType),
                gainedAmount
            );
            return;
        }
    }

    private string GetResourceDisplayName(ResourceType resourceType)
    {
        return resourceType switch
        {
            ResourceType.Rage => "분노",
            ResourceType.Momentum => "기세",
            ResourceType.Aether => "에테르",
            ResourceType.Faith => "신앙",
            ResourceType.Blood => "혈기",
            _ => "카르마"
        };
    }

    private CharacterMasterData GetMasterData(string characterId)
    {
        if (DataManager.Instance == null || DataManager.Instance.CharacterDatabase == null)
            return null;

        DataManager.Instance.CharacterDatabase.TryGet(characterId, out CharacterMasterData data);
        return data;
    }

    private void RefreshPlayerHUDs()
    {
        PlayerHUDSlot[] hudSlots = Object.FindObjectsByType<PlayerHUDSlot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < hudSlots.Length; i++)
        {
            if (hudSlots[i] != null)
                hudSlots[i].Refresh();
        }
    }
}
