using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class ItemDatabase
    {
        private readonly LookupDatabase<ItemData> db = new();

        public void Initialize(IEnumerable<ItemData> items)
        {
            db.Initialize(items, x => x.ItemId);
        }

        public ItemData Get(string itemId)
        {
            return db.Get(itemId);
        }
    }
}