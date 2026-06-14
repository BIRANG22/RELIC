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

        // 기존 코드 호환용: 시작 위치 반환
        public int GetGridIndex(int slotIndex)
        {
            return GetSpawnGridIndex(slotIndex);
        }

        public int GetSpawnGridIndex(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return -1;

            return slots[slotIndex].SpawnGridIndex;
        }

        public int GetCurrentGridIndex(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return -1;

            return slots[slotIndex].CurrentGridIndex;
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
            return true;
        }

        // 기존 코드 호환용: 시작 위치 저장
        public bool SetGridIndex(int slotIndex, int gridIndex)
        {
            return SetSpawnGridIndex(slotIndex, gridIndex);
        }

        public bool SetSpawnGridIndex(int slotIndex, int gridIndex)
        {
            if (!IsValidSlot(slotIndex))
            {
                Debug.LogWarning($"[PartyRuntimeStore] Invalid slot index: {slotIndex}");
                return false;
            }

            if (!IsValidGrid(gridIndex))
            {
                Debug.LogWarning($"[PartyRuntimeStore] Invalid spawn grid index: {gridIndex}");
                return false;
            }

            if (IsSpawnGridUsedByOtherSlot(slotIndex, gridIndex))
            {
                Debug.LogWarning($"[PartyRuntimeStore] Spawn grid already used: {gridIndex}");
                return false;
            }

            slots[slotIndex].SpawnGridIndex = gridIndex;
            slots[slotIndex].CurrentGridIndex = gridIndex;

            return true;
        }

        public bool SetCurrentGridIndex(int slotIndex, int gridIndex)
        {
            if (!IsValidSlot(slotIndex))
            {
                Debug.LogWarning($"[PartyRuntimeStore] Invalid slot index: {slotIndex}");
                return false;
            }

            if (!IsValidGrid(gridIndex))
            {
                Debug.LogWarning($"[PartyRuntimeStore] Invalid current grid index: {gridIndex}");
                return false;
            }

            slots[slotIndex].CurrentGridIndex = gridIndex;
            return true;
        }

        public void ResetCurrentGridIndicesToSpawn()
        {
            for (int i = 0; i < MaxPartyCount; i++)
                slots[i].CurrentGridIndex = slots[i].SpawnGridIndex;
        }

        public bool SetSlot(int slotIndex, string characterId, int gridIndex)
        {
            if (!SetCharacter(slotIndex, characterId))
                return false;

            if (!SetSpawnGridIndex(slotIndex, gridIndex))
                return false;

            return true;
        }

        public void ClearSlot(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return;

            slots[slotIndex].CharacterId = null;
            slots[slotIndex].SpawnGridIndex = -1;
            slots[slotIndex].CurrentGridIndex = -1;
        }

        public void Clear()
        {
            for (int i = 0; i < MaxPartyCount; i++)
                ClearSlot(i);
        }

        private bool IsSpawnGridUsedByOtherSlot(int slotIndex, int gridIndex)
        {
            for (int i = 0; i < MaxPartyCount; i++)
            {
                if (i == slotIndex)
                    continue;

                if (slots[i].SpawnGridIndex == gridIndex)
                    return true;
            }

            return false;
        }

        private bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < MaxPartyCount;
        }

        private bool IsValidGrid(int gridIndex)
        {
            return gridIndex >= 0 && gridIndex < MaxGridCount;
        }

        private void LogParty()
        {
            Debug.Log(
                $"[PartyRuntimeStore]\n" +
                $"Slot 0: {slots[0].CharacterId ?? "Empty"} / Spawn: {slots[0].SpawnGridIndex} / Current: {slots[0].CurrentGridIndex}\n" +
                $"Slot 1: {slots[1].CharacterId ?? "Empty"} / Spawn: {slots[1].SpawnGridIndex} / Current: {slots[1].CurrentGridIndex}\n" +
                $"Slot 2: {slots[2].CharacterId ?? "Empty"} / Spawn: {slots[2].SpawnGridIndex} / Current: {slots[2].CurrentGridIndex}"
            );
        }

        public int FindCharacterSlot(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return -1;

            for (int i = 0; i < MaxPartyCount; i++)
            {
                if (slots[i].CharacterId == characterId)
                    return i;
            }

            return -1;
        }

        public int FindEmptySlot()
        {
            for (int i = 0; i < MaxPartyCount; i++)
            {
                if (string.IsNullOrWhiteSpace(slots[i].CharacterId))
                    return i;
            }

            return -1;
        }

        // 기존 코드 호환용: 시작 위치 기준
        public bool IsGridUsed(int gridIndex)
        {
            for (int i = 0; i < MaxPartyCount; i++)
            {
                if (slots[i].SpawnGridIndex == gridIndex)
                    return true;
            }

            return false;
        }
    }
}