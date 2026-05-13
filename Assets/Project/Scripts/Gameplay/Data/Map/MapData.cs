using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MapData
    {
        public string MapId;
        public string MapName;
        public string RoomType;

        public string BattleMapId;
        public string EventId;

        public string Chapter;
        public string StageMap;
        public string RoomGrade;
        public int SpawnWeight;
        public bool AllowRepeat;
    }
}