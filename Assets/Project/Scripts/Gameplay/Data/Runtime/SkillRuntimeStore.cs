using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class SkillRuntimeStore
    {
        private readonly Dictionary<string, SkillRuntimeData> map = new();

        public void AddOrUpdate(SkillRuntimeData data)
        {
            map[data.SkillId] = data;
        }

        public SkillRuntimeData Get(string skillId)
        {
            map.TryGetValue(skillId, out var data);
            return data;
        }

        public bool TryGet(string skillId, out SkillRuntimeData data)
        {
            return map.TryGetValue(skillId, out data);
        }

        public IReadOnlyDictionary<string, SkillRuntimeData> GetAll()
        {
            return map;
        }
    }
}