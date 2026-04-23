using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MonsterMasterData
    {
        public string MonsterId;
        public string Name;
        public string Grade;
        public int Health;
        public string DropTableId;
        public string PatternId;
    }
}
