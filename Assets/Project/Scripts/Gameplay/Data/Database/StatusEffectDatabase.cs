using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class StatusEffectDatabase
    {
        private readonly LookupDatabase<StatusEffectMasterData> db = new();

        public void Initialize(IEnumerable<StatusEffectMasterData> list) => db.Initialize(list, x => x.StatusEffectId);
        public StatusEffectMasterData Get(string id) => db.Get(id);
        public bool TryGet(string id, out StatusEffectMasterData value) => db.TryGet(id, out value);
    }
}
