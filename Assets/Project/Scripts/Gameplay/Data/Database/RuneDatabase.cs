using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public class RuneDatabase
    {
        private readonly LookupDatabase<RuneData> runeDb = new();
        private List<RuneData> allRunes = new();

        public void Initialize(IEnumerable<RuneData> runes)
        {
            allRunes = runes.ToList();
            runeDb.Initialize(allRunes, x => x.RuneId);
        }

        public RuneData Get(string id)
        {
            return runeDb.Get(id);
        }

        public bool TryGet(string id, out RuneData value)
        {
            return runeDb.TryGet(id, out value);
        }

        public List<RuneData> GetAll()
        {
            return allRunes;
        }
    }
}