using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class RangeDatabase
    {
        private readonly LookupDatabase<SkillRangeData> db = new();

        public void Initialize(IEnumerable<SkillRangeData> list) => db.Initialize(list, x => x.RangeId);
        public SkillRangeData Get(string id) => db.Get(id);
        public bool TryGet(string id, out SkillRangeData value) => db.TryGet(id, out value);
    }
}
