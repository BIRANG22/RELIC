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
            if (data == null)
                return;

            string equippedSkills = data.EquippedSkillIds != null
                ? string.Join(", ", data.EquippedSkillIds)
                : "None";

            string skills =
                $"Move:{data.MoveSkillId}, " +
                $"Passive:{data.PassiveSkillId}, " +
                $"Unique:{data.UniqueSkillId}, " +
                $"Ability:{data.AbilitySkillId}, " +
                $"Equipped:[{equippedSkills}]";

            var items = data.EquippedItemIds != null && data.EquippedItemIds.Count > 0
                ? string.Join(", ", data.EquippedItemIds)
                : "None";
        }
    }
}