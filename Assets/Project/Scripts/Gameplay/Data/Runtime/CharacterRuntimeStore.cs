using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class CharacterRuntimeStore
    {
        private readonly Dictionary<string, CharacterRuntimeData> map = new();

        public void AddOrUpdate(CharacterRuntimeData data)
        {
            map[data.CharacterId] = data;
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
    }
}