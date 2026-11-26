using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public sealed class BattleConsumableSlotRuntimeData
    {
        public string ItemId = string.Empty;
        public bool IsUsed;
    }

    public static class PartyConsumableRuntime
    {
        public const int SlotCount = 4;
        public const int CurrentSchemaVersion = 1;

        public static void NormalizeLobby(LobbyRuntimeData lobby)
        {
            if (lobby == null) return;
            lobby.StoredCompoundIds ??= new List<string>();
            lobby.EquippedConsumableIds = NormalizeIds(lobby.EquippedConsumableIds);
            if (lobby.ConsumableSchemaVersion < CurrentSchemaVersion)
            {
                MigrateLoadoutCompounds(lobby);
                lobby.ConsumableSchemaVersion = CurrentSchemaVersion;
            }
        }

        public static void NormalizeBattle(BattleRuntimeData battle)
        {
            if (battle == null) return;
            battle.ConsumableSlots ??= new List<BattleConsumableSlotRuntimeData>();
            while (battle.ConsumableSlots.Count < SlotCount)
                battle.ConsumableSlots.Add(new BattleConsumableSlotRuntimeData());
            if (battle.ConsumableSlots.Count > SlotCount)
                battle.ConsumableSlots.RemoveRange(SlotCount, battle.ConsumableSlots.Count - SlotCount);
            for (int i = 0; i < battle.ConsumableSlots.Count; i++)
            {
                battle.ConsumableSlots[i] ??= new BattleConsumableSlotRuntimeData();
                battle.ConsumableSlots[i].ItemId = NormalizeId(battle.ConsumableSlots[i].ItemId);
                if (string.IsNullOrEmpty(battle.ConsumableSlots[i].ItemId))
                    battle.ConsumableSlots[i].IsUsed = false;
            }
        }

        public static bool TryEquip(LobbyRuntimeData lobby, int slotIndex, string itemId)
        {
            NormalizeLobby(lobby);
            string normalized = NormalizeId(itemId);
            if (lobby == null || slotIndex < 0 || slotIndex >= SlotCount || string.IsNullOrEmpty(normalized)) return false;
            string previous = NormalizeId(lobby.EquippedConsumableIds[slotIndex]);
            if (previous == normalized) return true;
            int storedIndex = FindIndex(lobby.StoredCompoundIds, normalized);
            if (storedIndex < 0) return false;
            lobby.StoredCompoundIds.RemoveAt(storedIndex);
            if (!string.IsNullOrEmpty(previous)) lobby.StoredCompoundIds.Add(previous);
            lobby.EquippedConsumableIds[slotIndex] = normalized;
            return true;
        }

        public static bool TryUnequip(LobbyRuntimeData lobby, int slotIndex)
        {
            NormalizeLobby(lobby);
            if (lobby == null || slotIndex < 0 || slotIndex >= SlotCount) return false;
            string previous = NormalizeId(lobby.EquippedConsumableIds[slotIndex]);
            if (string.IsNullOrEmpty(previous)) return false;
            lobby.StoredCompoundIds.Add(previous);
            lobby.EquippedConsumableIds[slotIndex] = null;
            return true;
        }

        public static void MigrateCharacterCompounds(LobbyRuntimeData lobby, IEnumerable<CharacterRuntimeData> characters)
        {
            NormalizeLobby(lobby);
            if (lobby == null || characters == null) return;
            foreach (CharacterRuntimeData character in characters)
            {
                if (character?.EquippedRelicIds == null || character.EquippedRelicIds.Length <= 0) continue;
                string itemId = NormalizeId(character.EquippedRelicIds[0]);
                character.EquippedRelicIds[0] = null;
                AddMigratedItem(lobby, itemId);
            }
            lobby.ConsumableSchemaVersion = CurrentSchemaVersion;
        }

        public static void CopyToBattle(LobbyRuntimeData lobby, BattleRuntimeData battle)
        {
            NormalizeLobby(lobby);
            if (lobby == null || battle == null) return;
            battle.ConsumableSlots = new List<BattleConsumableSlotRuntimeData>(SlotCount);
            for (int i = 0; i < SlotCount; i++)
                battle.ConsumableSlots.Add(new BattleConsumableSlotRuntimeData { ItemId = NormalizeId(lobby.EquippedConsumableIds[i]), IsUsed = false });
            battle.ConsumablesSettled = false;
        }

        public static void ReturnUnusedToLobby(BattleRuntimeData battle, LobbyRuntimeData lobby)
        {
            if (battle == null || lobby == null || battle.ConsumablesSettled) return;
            NormalizeBattle(battle);
            NormalizeLobby(lobby);
            for (int i = 0; i < battle.ConsumableSlots.Count; i++)
            {
                BattleConsumableSlotRuntimeData slot = battle.ConsumableSlots[i];
                if (slot == null || slot.IsUsed || string.IsNullOrWhiteSpace(slot.ItemId)) continue;
                lobby.StoredCompoundIds.Add(slot.ItemId.Trim());
            }
            battle.ConsumablesSettled = true;
        }

        private static void MigrateLoadoutCompounds(LobbyRuntimeData lobby)
        {
            if (lobby.CharacterLoadouts == null) return;
            for (int i = 0; i < lobby.CharacterLoadouts.Count; i++)
            {
                LobbyCharacterLoadoutData loadout = lobby.CharacterLoadouts[i];
                if (loadout?.EquippedRelicIds == null || loadout.EquippedRelicIds.Length <= 0) continue;
                string itemId = NormalizeId(loadout.EquippedRelicIds[0]);
                loadout.EquippedRelicIds[0] = null;
                AddMigratedItem(lobby, itemId);
            }
        }

        private static void AddMigratedItem(LobbyRuntimeData lobby, string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            for (int i = 0; i < SlotCount; i++)
            {
                if (!string.IsNullOrWhiteSpace(lobby.EquippedConsumableIds[i])) continue;
                lobby.EquippedConsumableIds[i] = itemId;
                return;
            }
            lobby.StoredCompoundIds.Add(itemId);
        }

        private static string[] NormalizeIds(string[] source)
        {
            string[] result = new string[SlotCount];
            if (source == null) return result;
            for (int i = 0; i < Math.Min(source.Length, result.Length); i++) result[i] = NormalizeId(source[i]);
            return result;
        }

        private static int FindIndex(List<string> ids, string target)
        {
            if (ids == null) return -1;
            for (int i = 0; i < ids.Count; i++) if (NormalizeId(ids[i]) == target) return i;
            return -1;
        }

        private static string NormalizeId(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
