using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class ItemDatabase
    {
        private readonly LookupDatabase<ItemData> db = new();
        private readonly List<ItemData> allItems = new();

        public void Initialize(IEnumerable<ItemData> items)
        {
            allItems.Clear();

            if (items != null)
                allItems.AddRange(items);

            db.Initialize(allItems, x => x.ItemId);
        }

        public ItemData Get(string itemId)
        {
            return db.Get(itemId);
        }

        public IReadOnlyList<ItemData> GetAll()
        {
            return allItems;
        }
    }
}