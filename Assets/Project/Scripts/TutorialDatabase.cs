using System;
using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public sealed class TutorialData
    {
        public string TutorialId;
        public int Order;
        public string MapId;
        public string StartParty;
        public int InitialErosion;
        public string InitialHpPercent;
        public int DeferMonsterSpawn;
        public string Phase;
        public string SpeakerId;
        public string TextOutput;
        public string TextKey;
        public string TextKO;
        public string Advance;
        public string HighlightTarget;
        // Above / Below: location of the guide tooltip relative to the highlighted UI.
        public string TooltipPosition;
        public string CompleteTextKey;
        public string CompleteTextKO;
    }

    public sealed class TutorialDatabase
    {
        private readonly List<TutorialData> steps = new();
        public void Initialize(IEnumerable<TutorialData> source)
        {
            steps.Clear();
            if (source != null) steps.AddRange(source.Where(x => x != null && !string.IsNullOrWhiteSpace(x.TutorialId)));
            steps.Sort((a, b) => a.Order.CompareTo(b.Order));
        }
        public IReadOnlyList<TutorialData> GetForMap(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId)) return Array.Empty<TutorialData>();
            return steps.Where(x => string.Equals(x.MapId, mapId, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        public TutorialData GetFirstEntry()
        {
            return steps.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.MapId) && !string.IsNullOrWhiteSpace(x.StartParty));
        }
    }
}
