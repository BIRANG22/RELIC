using System;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MonsterStateData
    {
        public string MonsterId;
        public int CurrentHealth;
        public Vector2Int GridPosition;
        public bool IsAlive = true;
    }
}
