using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    /// <summary>
    /// 로비 파티에서 캐릭터가 빠질 때 장착 중인 유물과 연성제를 보관 상태로 되돌립니다.
    /// 기억/스킬 장착은 변경하지 않습니다.
    /// </summary>
    public static class LobbyCharacterEquipmentReleaseUtility
    {
        public static bool ReleaseAll(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || global::DataManager.Instance == null)
                return false;

            string normalizedCharacterId = characterId.Trim();
            global::DataManager dataManager = global::DataManager.Instance;

            if (dataManager.CharacterRuntimeStore == null ||
                !dataManager.CharacterRuntimeStore.TryGet(normalizedCharacterId, out CharacterRuntimeData runtime) ||
                runtime == null)
            {
                return false;
            }

            LobbyRuntimeData lobby = dataManager.LobbyRuntimeStore?.GetOrCreate();
            if (lobby == null)
                return false;

            lobby.OwnedRelicIds ??= new List<string>();
            lobby.StoredCompoundIds ??= new List<string>();
            lobby.CharacterLoadouts ??= new List<LobbyCharacterLoadoutData>();

            ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);

            bool changed = false;
            int activeSlotIndex = ActiveRelicRuntimeUtility.ActiveRelicSlotIndex;

            for (int slotIndex = 0; slotIndex < runtime.EquippedRelicIds.Length; slotIndex++)
            {
                string equippedId = runtime.EquippedRelicIds[slotIndex]?.Trim();
                if (string.IsNullOrWhiteSpace(equippedId))
                    continue;

                if (slotIndex == activeSlotIndex)
                {
                    // 연성제는 수량형 보관 데이터이므로 장착 해제 시 1개를 그대로 반환합니다.
                    lobby.StoredCompoundIds.Add(equippedId);
                }
                else
                {
                    AddUniqueId(lobby.OwnedRelicIds, equippedId);
                }

                runtime.EquippedRelicIds[slotIndex] = null;
                changed = true;
            }

            if (runtime.ActiveRelicUses != null && runtime.ActiveRelicUses.Count > 0)
            {
                runtime.ActiveRelicUses.Clear();
                changed = true;
            }

            ClearSavedRelicLoadout(lobby.CharacterLoadouts, normalizedCharacterId);
            return changed;
        }

        private static void AddUniqueId(IList<string> ids, string id)
        {
            if (ids == null || string.IsNullOrWhiteSpace(id))
                return;

            string normalizedId = id.Trim();

            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i]?.Trim(), normalizedId, StringComparison.Ordinal))
                    return;
            }

            ids.Add(normalizedId);
        }

        private static void ClearSavedRelicLoadout(
            IList<LobbyCharacterLoadoutData> loadouts,
            string characterId)
        {
            if (loadouts == null || string.IsNullOrWhiteSpace(characterId))
                return;

            for (int i = 0; i < loadouts.Count; i++)
            {
                LobbyCharacterLoadoutData loadout = loadouts[i];
                if (loadout == null ||
                    !string.Equals(loadout.CharacterId?.Trim(), characterId, StringComparison.Ordinal))
                {
                    continue;
                }

                loadout.EquippedRelicIds = new string[ActiveRelicRuntimeUtility.EquippedRelicSlotCount];
            }
        }
    }
}
