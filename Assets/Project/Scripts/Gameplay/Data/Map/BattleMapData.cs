using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class BattleMapData
    {
        public string BattleMapId;
        public string NameSuffix;
        public string MonsterId;

        public int OccupiedCell1;
        public int OccupiedCell2;
        public int OccupiedCell3;
        public int OccupiedCell4;

        public string Description;

        public List<int> GetOccupiedCells()
        {
            List<int> cells = new();

            if (OccupiedCell1 > 0) cells.Add(OccupiedCell1);
            if (OccupiedCell2 > 0) cells.Add(OccupiedCell2);
            if (OccupiedCell3 > 0) cells.Add(OccupiedCell3);
            if (OccupiedCell4 > 0) cells.Add(OccupiedCell4);

            return cells;
        }
    }
}