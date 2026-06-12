using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class SkillRangeData
    {
        public string RangeId;
        public string Name;

        public List<string> RangeRaw = new();

        public Sprite Icon;

        public List<Vector2Int> Positions = new();
    }
}