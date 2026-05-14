using System;

namespace Relic.Gameplay.Data
{
    public enum FixedPosition
    {
        None,
        Front,
        Final,
        Penultimate
    }

    [Serializable]
    public class MapData
    {
        public string MapId;
        public string Name;
        public string Type;

        public string BattleMapId;
        public string EventId;

        public string Chapter;
        public string Stage;

        public int SpawnWeight;
        public FixedPosition FixedPosition;
    }
}