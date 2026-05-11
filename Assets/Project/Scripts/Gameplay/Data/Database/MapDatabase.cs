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

            Debug.Log($"[MapDatabase] Loaded Map Count: {maps.Count}");

            foreach (var map in maps)
            {
                Debug.Log($"[MapDatabase] {map.MapId} / {map.MapName} / {map.Chapter} / {map.StageMap}");
            }
        }

        public MapData Get(string id) => db.Get(id);

        public bool TryGet(string id, out MapData value) => db.TryGet(id, out value);

        public MapData GetFirstMap(string chapterId, string stageMap)
        {
            return maps.FirstOrDefault(map =>
                map.Chapter.Trim() == chapterId.Trim() &&
                map.StageMap.Trim() == stageMap.Trim()
            );
        }
    }
}