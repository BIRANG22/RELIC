using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MapData
    {
        public string MapId;
        public string Name;
        public string Type;

        public string BattleMapId;
        public string EventId;

        public string Stage;

        public int SpawnWeight;
    }
}
