using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class PartyManager
    {
        private readonly List<string> party = new();
        public IReadOnlyList<string> CurrentParty => party;

        public void SetParty(IEnumerable<string> characterIds)
        {
            party.Clear();
            party.AddRange(characterIds);
        }
    }
}
