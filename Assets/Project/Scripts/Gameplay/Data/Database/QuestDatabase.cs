using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class QuestDatabase
    {
        private readonly LookupDatabase<QuestData> db = new();

        public void Initialize(IEnumerable<QuestData> list) => db.Initialize(list, x => x.QuestId);
        public QuestData Get(string id) => db.Get(id);
        public bool TryGet(string id, out QuestData value) => db.TryGet(id, out value);
    }
}
