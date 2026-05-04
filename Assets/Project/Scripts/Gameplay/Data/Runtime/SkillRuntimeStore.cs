using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class SkillRuntimeStore
    {
        private readonly Dictionary<string, SkillRuntimeData> map = new();

        private string MakeKey(string characterId, string skillId)
        {
            return $"{characterId}:{skillId}";
        }

        public void AddOrUpdate(SkillRuntimeData data)
        {
            map[MakeKey(data.CharacterId, data.SkillId)] = data;
        }

        public bool TryGet(string characterId, string skillId, out SkillRuntimeData data)
        {
            return map.TryGetValue(MakeKey(characterId, skillId), out data);
        }

        public SkillRuntimeData Get(string characterId, string skillId)
        {
            map.TryGetValue(MakeKey(characterId, skillId), out var data);
            return data;
        }

        public IReadOnlyDictionary<string, SkillRuntimeData> GetAll()
        {
            return map;
        }
    }
}