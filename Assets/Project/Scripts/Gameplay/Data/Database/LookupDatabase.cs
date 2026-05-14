using System.Collections.Generic;
using UnityEngine;

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
                var key = keySelector(item)?.Trim();

                //Debug.Log($"[LookupDatabase<{typeof(T).Name}> Init] key='{key}', item={item}");

                if (string.IsNullOrWhiteSpace(key))
                {
                    //Debug.LogWarning($"[LookupDatabase<{typeof(T).Name}> Init] 빈 key라서 스킵");
                    continue;
                }

                if (map.ContainsKey(key))
                {
                    //Debug.LogWarning($"[LookupDatabase<{typeof(T).Name}> Init] 중복 key라서 스킵: {key}");
                    continue;
                }

                map.Add(key, item);
            }

        }

        public bool TryGet(string id, out T value)
        {
            id = id?.Trim();
            return map.TryGetValue(id, out value);
        }

        public T Get(string id)
        {
            id = id?.Trim();

            if (map.TryGetValue(id, out var v))
                return v;

            Debug.LogWarning(
                $"[LookupDatabase<{typeof(T).Name}>] 데이터 없음: {id}\n" +
                $"Loaded Keys: {string.Join(", ", map.Keys)}\n" +
                $"StackTrace:\n{System.Environment.StackTrace}"
            );

            return default;
        }

        public IReadOnlyDictionary<string, T> AsReadOnly() => map;

        public IReadOnlyDictionary<string, T> GetAll()
        {
            return map;
        }
    }
}