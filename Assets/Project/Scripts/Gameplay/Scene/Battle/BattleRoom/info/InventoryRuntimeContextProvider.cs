using System;
using System.Collections.Generic;
using System.Reflection;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class InventoryRuntimeContextProvider : MonoBehaviour, IRuntimeSaveStateContributor
{
    public enum RuntimeSource
    {
        Auto,
        Lobby,
        Battle
    }

    [SerializeField] private RuntimeSource source = RuntimeSource.Auto;
    private readonly Dictionary<EquippedRelicSlotUI, int> relicDisplayRows = new();
    private string lastPartySignature;

    private static readonly FieldInfo RelicPartySlotIndexField =
        typeof(EquippedRelicSlotUI).GetField(
            "partySlotIndex",
            BindingFlags.Instance | BindingFlags.NonPublic);

    private void OnEnable()
    {
        RefreshPartyViews(true);
    }

    private void Update()
    {
        RefreshPartyViews(false);
    }

    public IInventoryRuntimeContext GetContext()
    {
        if (DataManager.Instance == null)
            return null;

        if (ResolveSource() == RuntimeSource.Lobby)
            return InventoryRuntimeContext.ForLobby(DataManager.Instance.LobbyRuntimeStore.GetOrCreate());

        return InventoryRuntimeContext.ForBattle(DataManager.Instance.BattleRuntimeStore.GetOrCreate());
    }

    public void CommitRuntimeStateForSave()
    {
        if (ResolveSource() == RuntimeSource.Lobby)
            CaptureLobbyCharacterLoadouts();
    }

    private RuntimeSource ResolveSource()
    {
        if (source != RuntimeSource.Auto)
            return source;

        return string.Equals(SceneManager.GetActiveScene().name, "Lobby", StringComparison.OrdinalIgnoreCase)
            ? RuntimeSource.Lobby
            : RuntimeSource.Battle;
    }

    private void RefreshPartyViews(bool force)
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.PartyRuntimeStore == null ||
            DataManager.Instance.CharacterRuntimeStore == null)
        {
            return;
        }

        PartyRuntimeStore party = DataManager.Instance.PartyRuntimeStore;
        string signature = BuildPartySignature(party);
        if (!force && string.Equals(signature, lastPartySignature, StringComparison.Ordinal))
            return;

        lastPartySignature = signature;
        PartyInventoryCharacterEntry[] order = PartyInventoryCharacterOrder.Build(party, 3);

        PartyCharacterSlotListUI[] characterLists =
            GetComponentsInChildren<PartyCharacterSlotListUI>(true);
        for (int i = 0; i < characterLists.Length; i++)
            characterLists[i]?.Refresh();

        RefreshSkillRows(order);
        RefreshRelicRows(order);
    }

    private void RefreshSkillRows(PartyInventoryCharacterEntry[] order)
    {
        EquippedSkillPanelUI owner = GetComponent<EquippedSkillPanelUI>();
        EquippedSkillCharacterRowUI[] rows =
            GetComponentsInChildren<EquippedSkillCharacterRowUI>(true);

        for (int i = 0; i < rows.Length; i++)
        {
            EquippedSkillCharacterRowUI row = rows[i];
            if (row == null)
                continue;

            if (i >= order.Length || string.IsNullOrWhiteSpace(order[i].CharacterId))
            {
                row.Clear();
                continue;
            }

            if (DataManager.Instance.CharacterRuntimeStore.TryGet(
                    order[i].CharacterId,
                    out CharacterRuntimeData character))
            {
                row.Setup(owner, character);
            }
            else
            {
                row.Clear();
            }
        }
    }

    private void RefreshRelicRows(PartyInventoryCharacterEntry[] order)
    {
        EquippedRelicSlotUI[] slots = GetComponentsInChildren<EquippedRelicSlotUI>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            EquippedRelicSlotUI slot = slots[i];
            if (slot == null)
                continue;

            if (!relicDisplayRows.TryGetValue(slot, out int displayRow))
            {
                displayRow = slot.PartySlotIndex;
                relicDisplayRows.Add(slot, displayRow);
            }

            int partySlotIndex = displayRow >= 0 && displayRow < order.Length
                ? order[displayRow].PartySlotIndex
                : -1;

            RelicPartySlotIndexField?.SetValue(slot, partySlotIndex);
            slot.Refresh();
        }
    }

    private static string BuildPartySignature(PartyRuntimeStore party)
    {
        PartyInventoryCharacterEntry[] order =
            PartyInventoryCharacterOrder.Build(party, party.MaxPartyCountValue);
        var parts = new string[order.Length];
        for (int i = 0; i < order.Length; i++)
            parts[i] = $"{order[i].PartySlotIndex}:{order[i].CharacterId}";
        return string.Join("\u001f", parts);
    }

    private static void CaptureLobbyCharacterLoadouts()
    {
        if (DataManager.Instance == null)
            return;

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();
        lobby.CharacterLoadouts ??= new List<LobbyCharacterLoadoutData>();
        lobby.CharacterLoadouts.Clear();

        PartyRuntimeStore party = DataManager.Instance.PartyRuntimeStore;
        CharacterRuntimeStore characters = DataManager.Instance.CharacterRuntimeStore;
        if (party == null || characters == null)
            return;

        for (int i = 0; i < party.MaxPartyCountValue; i++)
        {
            string characterId = party.GetCharacterId(i);
            if (string.IsNullOrWhiteSpace(characterId) ||
                !characters.TryGet(characterId, out CharacterRuntimeData character) ||
                character == null)
            {
                continue;
            }

            lobby.CharacterLoadouts.Add(new LobbyCharacterLoadoutData
            {
                CharacterId = character.CharacterId.Trim(),
                EquippedRelicIds = Copy(character.EquippedRelicIds, 5),
                EquippedSkillIds = Copy(character.EquippedSkillIds, 4)
            });
        }
    }

    private static string[] Copy(string[] sourceValues, int length)
    {
        var result = new string[length];
        if (sourceValues != null)
            Array.Copy(sourceValues, result, Mathf.Min(sourceValues.Length, length));
        return result;
    }
}
