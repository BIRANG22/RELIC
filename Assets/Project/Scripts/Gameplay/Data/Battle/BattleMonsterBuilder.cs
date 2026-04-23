namespace Relic.Gameplay.Data
{
    public class BattleMonsterBuilder
    {
        public BattleMonsterContext Build(MonsterMasterData master, MonsterStateData state, MonsterPatternData pattern)
        {
            return new BattleMonsterContext
            {
                MonsterId = master.MonsterId,
                Name = master.Name,
                CurrentHealth = state.CurrentHealth > 0 ? state.CurrentHealth : master.Health,
                Pattern = pattern
            };
        }
    }

    public class BattleMonsterContext
    {
        public string MonsterId;
        public string Name;
        public int CurrentHealth;
        public MonsterPatternData Pattern;
    }
}
