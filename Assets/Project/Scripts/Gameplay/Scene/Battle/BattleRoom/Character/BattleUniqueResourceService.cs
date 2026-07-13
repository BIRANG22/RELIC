using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleUniqueResourceService
{
    private const int ResourceGainAmount = 1;
    private const int SameSlotActionRequiredCount = 3;
    private const int CostRequiredInTurn = 8;

    public void ApplyTimelineResourceGain(BattleTimelineController timelineController)
    {
        if (timelineController == null)
            return;

        CheckThreeActionsInSameSlot(timelineController);
        CheckSpendEightCostInTurn(timelineController);
    }

    public void OnAnyPlayerDamaged(BattleCharacter damagedCharacter)
    {
        if (damagedCharacter == null || damagedCharacter.RuntimeData == null)
            return;

        AddResourceToTriggerOwners(ResourceTrigger.OnAnyAllyDamaged);
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

    private void CheckThreeActionsInSameSlot(BattleTimelineController timelineController)
    {
        for (int slotIndex = 0; slotIndex < timelineController.SlotCount; slotIndex++)
        {
            IReadOnlyList<PlayerReservedCommand> commands = timelineController.GetPlayerCommands(slotIndex);

            if (commands == null || commands.Count <= 0)
                continue;

            Dictionary<string, int> actionCounts = new();
            Dictionary<string, CharacterRuntimeData> runtimes = new();

            for (int i = 0; i < commands.Count; i++)
            {
                PlayerReservedCommand command = commands[i];

                if (command == null || command.UserRuntime == null || command.SkillData == null)
                    continue;

                // 이동 예약은 특정 행동 횟수에서 제외합니다.
                if (command.ReservedMoveGridIndex >= 0)
                    continue;

                string characterId = command.UserRuntime.CharacterId;

                if (!actionCounts.ContainsKey(characterId))
                    actionCounts[characterId] = 0;

                actionCounts[characterId]++;
                runtimes[characterId] = command.UserRuntime;
            }

            foreach (KeyValuePair<string, int> pair in actionCounts)
            {
                CharacterMasterData masterData = GetMasterData(pair.Key);

                if (masterData == null ||
                    masterData.ResourceTrigger != ResourceTrigger.OnThreeActionsInSameSlot ||
                    pair.Value < SameSlotActionRequiredCount)
                {
                    continue;
                }

                AddUniqueResource(runtimes[pair.Key], ResourceGainAmount);

                Debug.Log($"[UniqueResource] ThreeActionsInSameSlot / Slot:{slotIndex} / Character:{pair.Key}");
            }
        }
    }

    private void CheckSpendEightCostInTurn(BattleTimelineController timelineController)
    {
        Dictionary<string, int> costSpent = new();
        Dictionary<string, CharacterRuntimeData> runtimes = new();

        for (int slotIndex = 0; slotIndex < timelineController.SlotCount; slotIndex++)
        {
            IReadOnlyList<PlayerReservedCommand> commands = timelineController.GetPlayerCommands(slotIndex);

            if (commands == null)
                continue;

            for (int i = 0; i < commands.Count; i++)
            {
                PlayerReservedCommand command = commands[i];

                if (command == null || command.UserRuntime == null)
                    continue;

                string characterId = command.UserRuntime.CharacterId;

                if (!costSpent.ContainsKey(characterId))
                    costSpent[characterId] = 0;

                costSpent[characterId] += Mathf.Max(0, command.Cost);
                runtimes[characterId] = command.UserRuntime;
            }
        }

        foreach (KeyValuePair<string, int> pair in costSpent)
        {
            CharacterMasterData masterData = GetMasterData(pair.Key);

            if (masterData == null ||
                masterData.ResourceTrigger != ResourceTrigger.OnSpendEightCostInTurn ||
                pair.Value < CostRequiredInTurn)
            {
                continue;
            }

            AddUniqueResource(runtimes[pair.Key], ResourceGainAmount);

            Debug.Log($"[UniqueResource] SpendEightCostInTurn / Character:{pair.Key} / Spent:{pair.Value}");
        }
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
        if (runtime == null)
            return;

        CharacterMasterData masterData = GetMasterData(runtime.CharacterId);

        int maxResource = masterData != null
            ? Mathf.Max(0, masterData.MaxResource)
            : 999;

        int finalAmount = BattleEquipmentEffectService.ModifyUniqueResourceGain(runtime, amount);

        runtime.CurrentResource = Mathf.Min(maxResource, runtime.CurrentResource + finalAmount);

        RefreshPlayerHUDs();

        Debug.Log($"[UniqueResource] {runtime.CharacterId} +{finalAmount} / {runtime.CurrentResource}/{maxResource}");
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
