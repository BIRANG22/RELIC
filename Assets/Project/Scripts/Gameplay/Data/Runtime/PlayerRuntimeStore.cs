using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class PlayerRuntimeStore
    {
        private const int MinLevel = 1;
        private const int MaxLevel = 20;

        private readonly PlayerRuntimeData data = new();

        public int Level => data.Level;
        public int Exp => data.Exp;
        public int TotalExp => data.TotalExp;
        public int MaxLevelValue => MaxLevel;

        public PlayerRuntimeData Data => data;

        public void AddExp(int amount)
        {
            if (amount <= 0)
                return;

            data.Exp += amount;
            data.TotalExp += amount;

            while (data.Level < MaxLevel && data.Exp >= GetExpRequiredForNextLevel())
            {
                data.Exp -= GetExpRequiredForNextLevel();
                data.Level++;
            }

            if (data.Level >= MaxLevel)
            {
                data.Level = MaxLevel;
                data.Exp = 0;
            }
        }

        public void SetLevelForTest(int level)
        {
            data.Level = Mathf.Clamp(level, MinLevel, MaxLevel);
            data.Exp = 0;
        }

        public void AddLevelForTest(int amount)
        {
            SetLevelForTest(data.Level + amount);
        }

        public int GetExpRequiredForNextLevel()
        {
            if (data.Level >= MaxLevel)
                return 0;

            return data.Level * 100;
        }

        public int GetTotalExpToNextLevel()
        {
            if (data.Level >= MaxLevel)
                return data.TotalExp;

            return data.TotalExp + GetExpRequiredForNextLevel() - data.Exp;
        }

        public bool IsMaxLevel()
        {
            return data.Level >= MaxLevel;
        }

        public void SetData(PlayerRuntimeData source)
        {
            if (source == null)
            {
                data.Level = MinLevel;
                data.Exp = 0;
                data.TotalExp = 0;
                data.DiscoveredSkillIds = new List<string>();
                data.DiscoveredRuneIds = new List<string>();
                data.DiscoveredRelicIds = new List<string>();
                data.DiscoveredCompoundIds = new List<string>();
                data.DiscoveredItemIds = new List<string>();
                return;
            }

            data.Level = Mathf.Clamp(source.Level, MinLevel, MaxLevel);
            data.Exp = Mathf.Max(0, source.Exp);
            data.TotalExp = Mathf.Max(0, source.TotalExp);
            data.DiscoveredSkillIds = CopyIds(source.DiscoveredSkillIds);
            data.DiscoveredRuneIds = CopyIds(source.DiscoveredRuneIds);
            data.DiscoveredRelicIds = CopyIds(source.DiscoveredRelicIds);
            data.DiscoveredCompoundIds = CopyIds(source.DiscoveredCompoundIds);
            data.DiscoveredItemIds = CopyIds(source.DiscoveredItemIds);

            RecordDiscoveryService.Normalize(data);

            if (data.Level >= MaxLevel)
                data.Exp = 0;
        }

        private static List<string> CopyIds(List<string> source)
        {
            return source == null ? new List<string>() : new List<string>(source);
        }
    }
}
