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

        public int Cell1;
        public int Cell2;
        public int Cell3;
        public int Cell4;

        public string Description;

        public List<int> GetOccupiedCells()
        {
            List<int> cells = new();

            if (Cell1 > 0) cells.Add(Cell1);
            if (Cell2 > 0) cells.Add(Cell2);
            if (Cell3 > 0) cells.Add(Cell3);
            if (Cell4 > 0) cells.Add(Cell4);

            return cells;
        }
    }
}