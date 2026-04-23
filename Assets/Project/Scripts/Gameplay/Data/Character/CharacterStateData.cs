using System;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class CharacterStateData
    {
        public int CurrentHealth;
        public int CurrentStamina;
        public int CurrentUniqueResource;
        public Vector2Int GridPosition;
        public bool IsIncapacitated;
    }
}
