using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public class MonsterPatternInfoDatabase
    {
        private readonly Dictionary<string, List<MonsterPatternInfoData>> map = new();

        public void Initialize(IEnumerable<MonsterPatternInfoData> list)
        {
            map.Clear();

            if (list == null)
                return;

            foreach (MonsterPatternInfoData data in list)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.MonsterId))
                    continue;

                string monsterId = data.MonsterId.Trim();
                if (!map.TryGetValue(monsterId, out List<MonsterPatternInfoData> patternList))
                {
                    patternList = new List<MonsterPatternInfoData>();
                    map.Add(monsterId, patternList);
                }

                patternList.Add(data);
            }

            foreach (List<MonsterPatternInfoData> patternList in map.Values)
                patternList.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public IReadOnlyList<MonsterPatternInfoData> GetByMonsterId(string monsterId)
        {
            if (string.IsNullOrWhiteSpace(monsterId))
                return System.Array.Empty<MonsterPatternInfoData>();

            return map.TryGetValue(monsterId.Trim(), out List<MonsterPatternInfoData> list)
                ? list
                : System.Array.Empty<MonsterPatternInfoData>();
        }

        public IReadOnlyDictionary<string, List<MonsterPatternInfoData>> GetAll()
        {
            return map;
        }
    }
}
