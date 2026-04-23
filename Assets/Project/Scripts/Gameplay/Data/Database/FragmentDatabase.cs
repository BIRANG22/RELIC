using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class FragmentDatabase
    {
        private readonly LookupDatabase<FragmentData> db = new();

        public void Initialize(IEnumerable<FragmentData> list) => db.Initialize(list, x => x.FragmentId);
        public FragmentData Get(string id) => db.Get(id);
        public bool TryGet(string id, out FragmentData value) => db.TryGet(id, out value);
    }
}
