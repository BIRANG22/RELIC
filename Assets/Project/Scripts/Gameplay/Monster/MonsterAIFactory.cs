namespace Relic.Gameplay.Monster
{
    public static class MonsterAIFactory
    {
        public static MonsterAIBase Create(string monsterId)
        {
            switch (monsterId)
            {
                case "Mon_01":
                    return new SlimeAI();

                default:
                    return new DefaultMonsterAI();
            }
        }
    }
}