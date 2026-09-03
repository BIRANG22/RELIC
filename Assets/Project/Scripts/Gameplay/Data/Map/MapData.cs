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

        // Common: Weak / Normal / Hard, Elite: 1 / 2 / 3
        public string BattleGroup;
    }
}
