using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class CharacterRuntimeStore
    {
        private readonly Dictionary<string, CharacterRuntimeData> map = new();

        public void AddOrUpdate(CharacterRuntimeData data)
        {
            map[data.CharacterId] = data;

            Debug.Log($"[CharacterRuntimeStore] Saved: {data.CharacterId}");
            LogCharacterRuntime(data);
        }

        public CharacterRuntimeData Get(string characterId)
        {
            map.TryGetValue(characterId, out var data);
            return data;
        }

        public bool TryGet(string characterId, out CharacterRuntimeData data)
        {
            return map.TryGetValue(characterId, out data);
        }

        public IReadOnlyDictionary<string, CharacterRuntimeData> GetAll()
        {
            return map;
        }

        private void LogCharacterRuntime(CharacterRuntimeData data)
        {
            string skills =
                $"Move:{data.MoveSkillId}, " +
                $"Passive:{data.PassiveSkillId}, " +
                $"Ability1:{data.AbilitySkillId1}, " +
                $"Ability2:{data.AbilitySkillId2}, " +
                $"Ability3:{data.AbilitySkillId3}, " +
                $"Unique:{data.UniqueSkillId}";

            var items = data.EquippedItemIds != null && data.EquippedItemIds.Count > 0
                ? string.Join(", ", data.EquippedItemIds)
                : "None";

            Debug.Log(
                $"[CharacterRuntime]\n" +
                $"ID: {data.CharacterId}\n" +
                $"Level: {data.Level}, Exp: {data.Exp}\n" +
                $"HP: {data.CurrentHealth}, Stamina: {data.CurrentStamina}, Resource: {data.CurrentResource}\n" +
                $"MoveLevel: {data.CurrentMoveLevel}\n" +
                $"Unlocked: {data.IsUnlocked}\n" +
                $"Skills: {skills}\n" +
                $"Items: {items}"
            );
        }
    }
}