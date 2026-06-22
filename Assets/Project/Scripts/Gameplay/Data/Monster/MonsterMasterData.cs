using System;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MonsterMasterData
    {
        public string MonsterId;
        public string Name;
        public string Grade;
        public int Health;

        public int MinRemnant;
        public int MaxRemnant;
        public string UniqueItemId;
        public float UniqueItemChance;
        public float RelicChance;

        [NonSerialized]
        public GameObject BattlePrefab;
    }
}
