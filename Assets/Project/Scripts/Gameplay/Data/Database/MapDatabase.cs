using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class MapDatabase
    {
        private readonly LookupDatabase<MapData> db = new();

        public void Initialize(IEnumerable<MapData> list) => db.Initialize(list, x => x.MapId);
        public MapData Get(string id) => db.Get(id);
        public bool TryGet(string id, out MapData value) => db.TryGet(id, out value);
    }
}
