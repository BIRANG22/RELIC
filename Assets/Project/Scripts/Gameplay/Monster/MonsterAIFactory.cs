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

                case "Mon_07":
                    return new DraugrAI();

                case "Mon_08":
                    return new BarrowAI();

                case "Mon_09":
                    return new RookAI();

                case "Mon_10":
                    return new NocturnAI();

                case "Mon_11":
                    return new MortAI();

                case "Mon_12":
                    return new EliseAI();

                default:
                    return new DefaultMonsterAI();
            }
        }
    }
}