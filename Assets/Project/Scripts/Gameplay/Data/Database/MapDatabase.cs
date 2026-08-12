using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class MapDatabase
    {
        private readonly LookupDatabase<MapData> db = new();
        private readonly List<MapData> maps = new();

        public void Initialize(IEnumerable<MapData> list)
        {
            maps.Clear();
            maps.AddRange(list);

            db.Initialize(maps, x => x.MapId);
        }

        public MapData Get(string id) => db.Get(id);

        public List<MapData> GetAll()
        {
            return maps;
        }
        public bool TryGet(string id, out MapData value) => db.TryGet(id, out value);
    }
}
