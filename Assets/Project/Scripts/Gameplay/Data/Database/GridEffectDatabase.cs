using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class GridEffectDatabase
    {
        private readonly LookupDatabase<GridEffectData> db = new();

        public void Initialize(IEnumerable<GridEffectData> list)
        {
            db.Initialize(list, x => x.GridEffectID);
        }

        public GridEffectData Get(string id)
        {
            return db.Get(id);
        }

        public bool TryGet(string id, out GridEffectData value)
        {
            return db.TryGet(id, out value);
        }

        public IReadOnlyDictionary<string, GridEffectData> GetAll()
        {
            return db.GetAll();
        }
    }
}
