using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        public bool TryGet(string id, out MapData value) => db.TryGet(id, out value);

        public MapData GetStartMap(string chapterId, string stage)
        {
            return maps.FirstOrDefault(map =>
                Same(map.Chapter, chapterId) &&
                Same(map.Stage, stage) &&
                map.FixedPosition == FixedPosition.Front
            );
        }

        public MapData GetFinalMap(string chapterId, string stage)
        {
            return maps.FirstOrDefault(map =>
                Same(map.Chapter, chapterId) &&
                Same(map.Stage, stage) &&
                map.FixedPosition == FixedPosition.Final
            );
        }

        public MapData GetPenultimateMap(string chapterId, string stage)
        {
            return maps.FirstOrDefault(map =>
                Same(map.Chapter, chapterId) &&
                Same(map.Stage, stage) &&
                map.FixedPosition == FixedPosition.Penultimate
            );
        }

        private static bool Same(string a, string b)
        {
            return !string.IsNullOrWhiteSpace(a)
                && !string.IsNullOrWhiteSpace(b)
                && a.Trim() == b.Trim();
        }
    }
}