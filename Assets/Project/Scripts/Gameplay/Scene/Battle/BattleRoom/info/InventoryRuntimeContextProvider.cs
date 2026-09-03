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
    private ulong lastInventorySignature;
    private bool hasInventorySignature;

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

        if (ResolveSource() == RuntimeSource.Lobby)
        {
            force |= LobbySkillUpgradePersistence.ApplyAll(
                DataManager.Instance.LobbyRuntimeStore.GetOrCreate(),
                DataManager.Instance.CharacterRuntimeStore);
        }

        PartyRuntimeStore party = DataManager.Instance.PartyRuntimeStore;
        CharacterRuntimeStore characters = DataManager.Instance.CharacterRuntimeStore;
        ulong signature = BuildInventorySignature(party, characters);
        if (!force && hasInventorySignature && signature == lastInventorySignature)
            return;

        lastInventorySignature = signature;
        hasInventorySignature = true;
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

    private static ulong BuildInventorySignature(
        PartyRuntimeStore party,
        CharacterRuntimeStore characters)
    {
        if (party == null)
            return 0UL;

        // FNV-1a 기반의 무할당 해시입니다.
        // 기존 구현처럼 파티/장착 스킬 변화를 매 프레임 즉시 감지하되,
        // string[], string.Join, 보간 문자열 생성을 없애 GC 할당을 방지합니다.
        const ulong offsetBasis = 14695981039346656037UL;
        ulong hash = offsetBasis;

        for (int partySlotIndex = 0; partySlotIndex < party.MaxPartyCountValue; partySlotIndex++)
        {
            string characterId = party.GetCharacterId(partySlotIndex) ?? string.Empty;
            AddHash(ref hash, partySlotIndex);
            AddHash(ref hash, characterId);

            if (string.IsNullOrWhiteSpace(characterId) ||
                characters == null ||
                !characters.TryGet(characterId, out CharacterRuntimeData character) ||
                character == null)
            {
                continue;
            }

            AddHash(ref hash, character.PassiveSkillId);
            AddHash(ref hash, character.UniqueSkillId);
            AddHash(ref hash, character.AbilitySkillId);
            AddHash(ref hash, GetEquippedSkillId(character, 2));
            AddHash(ref hash, GetEquippedSkillId(character, 3));
        }

        return hash;
    }

    private static void AddHash(ref ulong hash, int value)
    {
        unchecked
        {
            hash ^= (uint)value;
            hash *= 1099511628211UL;
        }
    }

    private static void AddHash(ref ulong hash, string value)
    {
        unchecked
        {
            if (string.IsNullOrEmpty(value))
            {
                hash ^= 0xFF;
                hash *= 1099511628211UL;
                return;
            }

            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 1099511628211UL;
            }

            // 문자열 경계를 구분합니다.
            hash ^= 0xFF;
            hash *= 1099511628211UL;
        }
    }

    private static string GetEquippedSkillId(CharacterRuntimeData character, int index)
    {
        if (character?.EquippedSkillIds == null ||
            index < 0 ||
            index >= character.EquippedSkillIds.Length)
        {
            return string.Empty;
        }

        return character.EquippedSkillIds[index] ?? string.Empty;
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
                EquippedRelicIds = Copy(character.EquippedRelicIds, ActiveRelicRuntimeUtility.EquippedRelicSlotCount),
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
