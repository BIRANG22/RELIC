using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class AssetData
    {
        public string AssetId;
        public string Name;
        public string Category;
        public string Source;
        public int MaxAmount;
        public bool UseInShop;
        public bool UseInQuest;
        public bool UseInLobby;
    }
}
