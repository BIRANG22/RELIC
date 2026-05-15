namespace Relic.Gameplay.Data
{
    public static class MonsterRuntimeIdGenerator
    {
        private static int sequence = 0;

        public static string Create()
        {
            sequence++;
            return $"RuntimeMonster_{sequence:0000}";
        }

        public static void Reset()
        {
            sequence = 0;
        }
    }
}