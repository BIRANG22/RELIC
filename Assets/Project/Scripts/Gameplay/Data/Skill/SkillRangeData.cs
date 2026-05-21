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
        public bool IncludeSelf;

        public List<string> RangeRaw = new();

        public List<Vector2Int> Positions = new();
    }
}