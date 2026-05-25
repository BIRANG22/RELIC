using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    
    public class CharacterDatabase
    {
        private readonly LookupDatabase<CharacterMasterData> db = new();

        public void Initialize(IEnumerable<CharacterMasterData> list)
        {
            foreach (var data in list)
            {
                if (data != null)
                    data.BuildSkillLoadout();
            }

            db.Initialize(list, x => x.CharacterId);
        }
        public CharacterMasterData Get(string id) => db.Get(id);
        public bool TryGet(string id, out CharacterMasterData value) => db.TryGet(id, out value);

        public IReadOnlyDictionary<string, CharacterMasterData> GetAll()
        {
            return db.GetAll();
        }
    }
}
