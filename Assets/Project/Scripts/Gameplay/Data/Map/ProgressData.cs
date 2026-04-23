using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class ProgressData
    {
        public string ProgressId;
        public string ProfileId;
        public string CurrentState;
        public string CurrentChapter;
        public string CurrentArea;
        public string CurrentMap;
        public Dictionary<string, int> LobbyAssets = new();
        public List<string> CurrentPartyCharacterIds = new();
        public int SaveSlotNumber;
        public string LastSavedAt;
    }
}
