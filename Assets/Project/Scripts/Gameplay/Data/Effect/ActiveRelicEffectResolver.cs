namespace Relic.Gameplay.Data
{
    public enum ActiveRelicTargetMode { None, Party }

    public static class ActiveRelicEffectResolver
    {
        // Compounds cannot be equipped as ordinary relics.
        public static bool IsActiveRelic(RelicData relic) => relic is CompoundData;
    }
}
