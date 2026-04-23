using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class LookupDatabase<T>
    {
        private readonly Dictionary<string, T> map = new();

        public void Initialize(IEnumerable<T> source, System.Func<T, string> keySelector)
        {
            map.Clear();
            foreach (var item in source)
            {
                var key = keySelector(item);
                if (string.IsNullOrWhiteSpace(key) || map.ContainsKey(key))
                    continue;
                map.Add(key, item);
            }
        }

        public bool TryGet(string id, out T value) => map.TryGetValue(id, out value);
        public T Get(string id) => map.TryGetValue(id, out var v) ? v : default;
        public IReadOnlyDictionary<string,T> AsReadOnly() => map;
    }
}
