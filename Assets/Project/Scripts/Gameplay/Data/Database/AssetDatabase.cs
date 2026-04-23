using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class AssetDatabase
    {
        private readonly LookupDatabase<AssetData> db = new();

        public void Initialize(IEnumerable<AssetData> list) => db.Initialize(list, x => x.AssetId);
        public AssetData Get(string id) => db.Get(id);
        public bool TryGet(string id, out AssetData value) => db.TryGet(id, out value);
    }
}
