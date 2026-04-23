using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class CharacterGrowthManager
    {
        private readonly Dictionary<string, CharacterGrowthData> growthMap = new();

        public CharacterGrowthData GetOrCreate(string characterId)
        {
            if (!growthMap.TryGetValue(characterId, out var data))
            {
                data = new CharacterGrowthData { CharacterId = characterId, IsUnlocked = true, RequiredExperience = 100 };
                growthMap.Add(characterId, data);
            }
            return data;
        }

        public void AddExperience(string characterId, int amount)
        {
            var data = GetOrCreate(characterId);
            data.TotalExperience += amount;
            while (data.TotalExperience >= data.RequiredExperience)
            {
                data.TotalExperience -= data.RequiredExperience;
                data.CurrentLevel++;
                data.RequiredExperience = (int)(data.RequiredExperience * 1.15f);
            }
        }
    }
}
