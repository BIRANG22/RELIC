using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class PartyRuntimeStore
    {
        private const int MaxPartyCount = 3;
        private const int MaxGridCount = 35;

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

        /// <summary>
        /// 이미 편성된 두 캐릭터의 시작 위치를 서로 교환합니다.
        /// 캐릭터 등록 및 장착 정보는 변경하지 않습니다.
        /// </summary>
        public bool TrySwapSpawnGridIndices(int firstSlotIndex, int secondSlotIndex)
        {
            if (!IsValidSlot(firstSlotIndex) || !IsValidSlot(secondSlotIndex) ||
                firstSlotIndex == secondSlotIndex)
                return false;

            if (string.IsNullOrWhiteSpace(GetCharacterId(firstSlotIndex)) ||
                string.IsNullOrWhiteSpace(GetCharacterId(secondSlotIndex)))
                return false;

            int firstGrid = GetSpawnGridIndex(firstSlotIndex);
            int secondGrid = GetSpawnGridIndex(secondSlotIndex);
            if (!IsValidGrid(firstGrid) || !IsValidGrid(secondGrid))
                return false;

            // 직접 교환하므로 잠깐 동일한 Grid가 중복되는 중간 상태가 없습니다.
            slots[firstSlotIndex].SpawnGridIndex = secondGrid;
            slots[firstSlotIndex].CurrentGridIndex = secondGrid;
            slots[secondSlotIndex].SpawnGridIndex = firstGrid;
            slots[secondSlotIndex].CurrentGridIndex = firstGrid;
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

        /// <summary>
        /// 파티 캐릭터는 유지하고 전투 시작 배치 위치만 해제합니다.
        /// Ready_Panel에서 배치된 Grid를 클릭해 등록을 해제할 때 사용합니다.
        /// </summary>
        public bool ClearSpawnGridIndex(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
            {
                Debug.LogWarning($"[PartyRuntimeStore] Invalid slot index: {slotIndex}");
                return false;
            }

            slots[slotIndex].SpawnGridIndex = -1;
            slots[slotIndex].CurrentGridIndex = -1;
            return true;
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

        /// <summary>
        /// Ready 패널에서 확정한 캐릭터 표시 순서를 실제 파티 슬롯 순서에 반영합니다.
        /// 캐릭터와 시작/현재 그리드 좌표를 함께 옮기므로 배치 위치는 바뀌지 않습니다.
        /// 그리드에 배치되지 않은 파티원 슬롯은 그대로 유지합니다.
        /// </summary>
        /// <summary>
        /// 등록 해제한 캐릭터를 제거하고 뒤의 파티 슬롯을 앞으로 당깁니다.
        /// 남은 캐릭터의 시작 및 현재 그리드 좌표는 그대로 따라갑니다.
        /// </summary>
        public bool RemoveCharacterAndCompact(string characterId)
        {
            int removedIndex = FindCharacterSlot(characterId);
            if (removedIndex < 0) return false;
            for (int i = removedIndex; i < MaxPartyCount - 1; i++)
                slots[i] = slots[i + 1];
            slots[MaxPartyCount - 1] = new PartySlotRuntimeData();
            return true;
        }

        public bool ReorderPlacedCharacters(IList<string> orderedCharacterIds)
        {
            if (orderedCharacterIds == null)
                return false;

            var placedSlots = new List<int>(MaxPartyCount);
            var byCharacterId = new Dictionary<string, PartySlotRuntimeData>(System.StringComparer.Ordinal);
            for (int i = 0; i < MaxPartyCount; i++)
            {
                PartySlotRuntimeData slot = slots[i];
                if (string.IsNullOrWhiteSpace(slot.CharacterId) || slot.SpawnGridIndex < 0)
                    continue;
                if (byCharacterId.ContainsKey(slot.CharacterId))
                    return false;
                placedSlots.Add(i);
                byCharacterId.Add(slot.CharacterId, slot);
            }

            if (placedSlots.Count != orderedCharacterIds.Count)
                return false;

            var reordered = new PartySlotRuntimeData[placedSlots.Count];
            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            for (int i = 0; i < reordered.Length; i++)
            {
                string id = orderedCharacterIds[i];
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id) || !byCharacterId.TryGetValue(id, out reordered[i]))
                    return false;
            }

            for (int i = 0; i < reordered.Length; i++)
                slots[placedSlots[i]] = reordered[i];
            return true;
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

        // 기존 코드 호환용: 시작 위치 사용 여부 확인
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