using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class PartyRuntimeStore
    {
        private const int MaxPartyCount = 3;
        private const int MaxGridCount = 15;

        private readonly PartySlotRuntimeData[] slots = new PartySlotRuntimeData[MaxPartyCount];

        public int MaxPartyCountValue => MaxPartyCount;
        public IReadOnlyList<PartySlotRuntimeData> Slots => slots;
        public bool HasAnyCharacter
        {
            get
            {
                for (int i = 0; i < MaxPartyCount; i++)
                {
                    if (!string.IsNullOrWhiteSpace(slots[i].CharacterId))
                        return true;
                }

                return false;
            }
        }

        public PartyRuntimeStore()
        {
            for (int i = 0; i < MaxPartyCount; i++)
                slots[i] = new PartySlotRuntimeData();
        }

        public string GetCharacterId(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return null;

            return slots[slotIndex].CharacterId;
        }

        public int GetGridIndex(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return -1;

            return slots[slotIndex].GridIndex;
        }

        public bool SetCharacter(int slotIndex, string characterId)
        {
            if (!IsValidSlot(slotIndex))
            {
                Debug.LogWarning($"[PartyRuntimeStore] Invalid slot index: {slotIndex}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(characterId))
            {
                Debug.LogWarning("[PartyRuntimeStore] CharacterId is empty.");
                return false;
            }

            slots[slotIndex].CharacterId = characterId;

            Debug.Log($"[PartyRuntimeStore] Slot {slotIndex} Character saved: {characterId}");
            LogParty();

            return true;
        }

        public bool SetGridIndex(int slotIndex, int gridIndex)
        {
            if (!IsValidSlot(slotIndex))
            {
                Debug.LogWarning($"[PartyRuntimeStore] Invalid slot index: {slotIndex}");
                return false;
            }

            if (gridIndex < 0 || gridIndex >= MaxGridCount)
            {
                Debug.LogWarning($"[PartyRuntimeStore] Invalid grid index: {gridIndex}");
                return false;
            }

            if (IsGridUsedByOtherSlot(slotIndex, gridIndex))
            {
                Debug.LogWarning($"[PartyRuntimeStore] Grid already used: {gridIndex}");
                return false;
            }

            slots[slotIndex].GridIndex = gridIndex;

            Debug.Log($"[PartyRuntimeStore] Slot {slotIndex} Grid saved: {gridIndex}");
            LogParty();

            return true;
        }

        public bool SetSlot(int slotIndex, string characterId, int gridIndex)
        {
            Debug.LogWarning(
                $"[PartyRuntimeStore] SetSlot called. slot={slotIndex}, id={characterId}, grid={gridIndex}"
            );
            Debug.LogWarning(UnityEngine.StackTraceUtility.ExtractStackTrace());

            if (!SetCharacter(slotIndex, characterId))
                return false;

            if (!SetGridIndex(slotIndex, gridIndex))
                return false;

            return true;
        }

        public void ClearSlot(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return;

            slots[slotIndex].CharacterId = null;
            slots[slotIndex].GridIndex = -1;

            LogParty();
        }

        public void Clear()
        {
            for (int i = 0; i < MaxPartyCount; i++)
                ClearSlot(i);
        }

        private bool IsGridUsedByOtherSlot(int slotIndex, int gridIndex)
        {
            for (int i = 0; i < MaxPartyCount; i++)
            {
                if (i == slotIndex)
                    continue;

                if (slots[i].GridIndex == gridIndex)
                    return true;
            }

            return false;
        }

        private bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < MaxPartyCount;
        }

        private void LogParty()
        {
            Debug.Log(
                $"[PartyRuntimeStore]\n" +
                $"Slot 0: {slots[0].CharacterId ?? "Empty"} / Grid: {slots[0].GridIndex}\n" +
                $"Slot 1: {slots[1].CharacterId ?? "Empty"} / Grid: {slots[1].GridIndex}\n" +
                $"Slot 2: {slots[2].CharacterId ?? "Empty"} / Grid: {slots[2].GridIndex}"
            );
        }
    }
}