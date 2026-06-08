using Relic.Gameplay.Monster;
using System.Collections.Generic;

namespace Relic.Gameplay.Battle
{
    public class BattleContext
    {
        public int CurrentRound;
        public int CurrentTurn;

        public List<PlayerUnit> PlayerUnits = new();

        public List<MonsterUnit> MonsterUnits = new();

        //public GridManager GridManager;

        //public PlayerUnit CurrentTarget;

        //public bool IsBossBattle;

        //public int AlivePlayerCount;

        //public int AliveMonsterCount;
    }
}