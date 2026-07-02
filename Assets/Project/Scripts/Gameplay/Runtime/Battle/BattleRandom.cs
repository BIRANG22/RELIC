using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Battle
{
    public static class BattleRandom
    {
        private static System.Random seededRandom;

        public static bool IsSeeded => seededRandom != null;

        public static void SetSeed(int seed)
        {
            seededRandom = new System.Random(seed);
        }

        public static void ClearSeed()
        {
            seededRandom = null;
        }

        public static int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                return minInclusive;

            if (seededRandom != null)
                return seededRandom.Next(minInclusive, maxExclusive);

            return Random.Range(minInclusive, maxExclusive);
        }

        public static float Range(float minInclusive, float maxInclusive)
        {
            if (maxInclusive <= minInclusive)
                return minInclusive;

            if (seededRandom != null)
            {
                double t = seededRandom.NextDouble();
                return minInclusive + (float)t * (maxInclusive - minInclusive);
            }

            return Random.Range(minInclusive, maxInclusive);
        }

        public static float Value()
        {
            if (seededRandom != null)
                return (float)seededRandom.NextDouble();

            return Random.value;
        }

        public static T Pick<T>(IReadOnlyList<T> candidates)
        {
            if (candidates == null || candidates.Count <= 0)
                return default;

            return candidates[Range(0, candidates.Count)];
        }
    }
}
