namespace Relic.Gameplay.Monster
{
    public static class MonsterAIFactory
    {
        public static MonsterAIBase Create(string monsterId)
        {
            switch (monsterId)
            {
                case "Mon_01":
                    return new MuckAI();

                case "Mon_02":
                    return new BlobAI();

                case "Mon_03":
                    return new RancorAI();

                case "Mon_04":
                    return new BlightAI();

                case "Mon_05":
                    return new VespaAI();

                case "Mon_06":
                    return new CinderAI();

                case "Mon_10":
                    return new EliseAI();

                default:
                    return new DefaultMonsterAI();
            }
        }
    }
}