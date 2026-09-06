using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class MonsterDatabase
    {
        private readonly LookupDatabase<MonsterMasterData> db = new();

        public void Initialize(IEnumerable<MonsterMasterData> list) => db.Initialize(list, x => x.MonsterId);
        public MonsterMasterData Get(string id) => db.Get(id);
        public bool TryGet(string id, out MonsterMasterData value) => db.TryGet(id, out value);
        public IReadOnlyDictionary<string, MonsterMasterData> GetAll() => db.GetAll();
    }
}
