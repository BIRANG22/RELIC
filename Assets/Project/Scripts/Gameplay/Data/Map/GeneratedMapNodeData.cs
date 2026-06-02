using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [System.Serializable]
    public class GeneratedMapNodeData
    {
        public int NodeIndex;
        public int LayerIndex;

        public string MapId;
        public string Type;

        public Vector2 Position;
        public List<int> NextNodeIndices = new();
    }
}