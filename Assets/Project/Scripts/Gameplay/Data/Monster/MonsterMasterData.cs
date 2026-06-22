using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MonsterMasterData
    {
        public string MonsterId;
        public string Name;
        public string Grade;
        [FormerlySerializedAs("Health")]
        public int HP;

        public int MinRemnant;
        public int MaxRemnant;
        public string UniqueItemId;
        public float UniqueItemChance;
        public float RelicChance;

        [NonSerialized]
        public GameObject BattlePrefab;
    }
}
