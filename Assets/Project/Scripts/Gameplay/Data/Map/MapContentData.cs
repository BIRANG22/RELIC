using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MapContentData
    {
        public string MapId;
        public string RoomType;
        public string EnterCondition;
        public string ClearCondition;
    }
}
