using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleUniqueResourceService
{
    private const int DamagedGainAmount = 1;
    private const int SameSlotActionRequiredCount = 2;
    private const int SameSlotGainAmount = 1;
    private const int StaminaRequiredInSlot = 6;
    private const int StaminaGainAmount = 1;

    public void ApplyTimelineSlotResourceGain(BattleTimelineController timelineController)
    {
        if (timelineController == null)
            return;

        for (int slotIndex = 0; slotIndex < timelineController.SlotCount; slotIndex++)
        {
            IReadOnlyList<PlayerReservedCommand> commands =
                timelineController.GetPlayerCommands(slotIndex);

            if (commands == null || commands.Count <= 0)
                continue;

            CheckSameSlotActionTwice(commands, slotIndex);
            CheckSpendStaminaInSlot(commands, slotIndex);
        }
    }

    public void OnPlayerDamaged(BattleCharacter character)
    {
        if (character == null || character.RuntimeData == null)
            return;

        CharacterMasterData masterData = GetMasterData(character.RuntimeData.CharacterId);

        if (masterData == null)
            return;

        if (masterData.ResourceTrigger != ResourceTrigger.OnDamaged)
            return;

        AddUniqueResource(character.RuntimeData, DamagedGainAmount);
    }

    private void CheckSameSlotActionTwice(
        IReadOnlyList<PlayerReservedCommand> commands,
        int slotIndex)
    {
        Dictionary<string, int> actionCounts = new();
        Dictionary<string, CharacterRuntimeData> runtimes = new();

        for (int i = 0; i < commands.Count; i++)
        {
            PlayerReservedCommand command = commands[i];

            if (command == null || command.UserRuntime == null || command.SkillData == null)
                continue;

            if (command.ReservedMoveGridIndex >= 0)
                continue;

            string characterId = command.UserRuntime.CharacterId;

            if (!actionCounts.ContainsKey(characterId))
                actionCounts[characterId] = 0;

            actionCounts[characterId]++;
            runtimes[characterId] = command.UserRuntime;
        }

        foreach (var pair in actionCounts)
        {
            string characterId = pair.Key;
            int count = pair.Value;

            CharacterMasterData masterData = GetMasterData(characterId);

            if (masterData == null)
                continue;

            if (masterData.ResourceTrigger != ResourceTrigger.OnUseSameSlotTwice)
                continue;

            if (count < SameSlotActionRequiredCount)
                continue;

            AddUniqueResource(runtimes[characterId], SameSlotGainAmount);

            Debug.Log($"[UniqueResource] SameSlotTwice / Slot:{slotIndex} / Character:{characterId}");
        }
    }

    private void CheckSpendStaminaInSlot(
        IReadOnlyList<PlayerReservedCommand> commands,
        int slotIndex)
    {
        Dictionary<string, int> staminaSpent = new();
        Dictionary<string, CharacterRuntimeData> runtimes = new();

        for (int i = 0; i < commands.Count; i++)
        {
            PlayerReservedCommand command = commands[i];

            if (command == null || command.UserRuntime == null)
                continue;

            string characterId = command.UserRuntime.CharacterId;

            if (!staminaSpent.ContainsKey(characterId))
                staminaSpent[characterId] = 0;

            staminaSpent[characterId] += command.StaminaCost;
            runtimes[characterId] = command.UserRuntime;
        }

        foreach (var pair in staminaSpent)
        {
            string characterId = pair.Key;
            int spent = pair.Value;

            CharacterMasterData masterData = GetMasterData(characterId);

            if (masterData == null)
                continue;

            if (masterData.ResourceTrigger != ResourceTrigger.OnSpendStaminaInSlot)
                continue;

            if (spent < StaminaRequiredInSlot)
                continue;

            AddUniqueResource(runtimes[characterId], StaminaGainAmount);

            Debug.Log($"[UniqueResource] SpendStamina / Slot:{slotIndex} / Character:{characterId} / Spent:{spent}");
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

        runtime.CurrentResource =
            Mathf.Min(maxResource, runtime.CurrentResource + Mathf.Max(0, amount));

        RefreshPlayerHUDs();

        Debug.Log(
            $"[UniqueResource] {runtime.CharacterId} +{amount} / " +
            $"{runtime.CurrentResource}/{maxResource}"
        );
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