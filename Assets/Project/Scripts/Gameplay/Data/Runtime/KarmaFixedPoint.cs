using UnityEngine;

namespace Relic.Gameplay.Data
{
    public static class KarmaFixedPoint
    {
        public const int CurrentVersion = 1;
        public const int UnitsPerDisplayValue = 2;

        public static void EnsureMigrated(CharacterRuntimeData runtime, bool isKarma)
        {
            if (runtime == null || !isKarma || runtime.UniqueResourceUnitVersion >= CurrentVersion) return;
            runtime.CurrentResource = Mathf.Max(0, runtime.CurrentResource) * UnitsPerDisplayValue;
            runtime.UniqueResourceUnitVersion = CurrentVersion;
        }

        public static int ToInternal(int displayUnits, bool isKarma) =>
            Mathf.Max(0, displayUnits) * (isKarma ? UnitsPerDisplayValue : 1);

        public static float GetDisplayValue(CharacterRuntimeData runtime, bool isKarma)
        {
            EnsureMigrated(runtime, isKarma);
            return runtime == null ? 0f : runtime.CurrentResource / (isKarma ? 2f : 1f);
        }

        public static int GetDisplayFloor(CharacterRuntimeData runtime, bool isKarma) =>
            Mathf.FloorToInt(GetDisplayValue(runtime, isKarma));

        public static bool HasHalf(CharacterRuntimeData runtime, bool isKarma)
        {
            EnsureMigrated(runtime, isKarma);
            return isKarma && runtime != null && (runtime.CurrentResource & 1) != 0;
        }

        public static int GetPreviewDisplayFloor(CharacterRuntimeData runtime, bool isKarma)
        {
            if (runtime == null) return 0;
            EnsureMigrated(runtime, isKarma);
            int scale = isKarma ? 2 : 1;
            return Mathf.Max(0, runtime.CurrentResource - runtime.ReservedResourceCost * scale) / scale;
        }

        public static bool PreviewHasHalf(CharacterRuntimeData runtime, bool isKarma)
        {
            if (runtime == null || !isKarma) return false;
            EnsureMigrated(runtime, true);
            return ((runtime.CurrentResource - ToInternal(runtime.ReservedResourceCost, true)) & 1) != 0;
        }

        public static bool CanPayDisplayUnits(CharacterRuntimeData runtime, int displayCost, bool isKarma)
        {
            if (runtime == null) return false;
            EnsureMigrated(runtime, isKarma);
            return runtime.CurrentResource - ToInternal(runtime.ReservedResourceCost, isKarma) >= ToInternal(displayCost, isKarma);
        }

        public static void SpendDisplayUnits(CharacterRuntimeData runtime, int displayCost, bool isKarma)
        {
            if (runtime == null) return;
            EnsureMigrated(runtime, isKarma);
            runtime.CurrentResource = Mathf.Max(0, runtime.CurrentResource - ToInternal(displayCost, isKarma));
        }

        public static int AddDisplayUnits(CharacterRuntimeData runtime, int displayAmount, int maxDisplay, bool isKarma)
        {
            return AddInternal(runtime, ToInternal(displayAmount, isKarma), maxDisplay, isKarma);
        }

        public static int AddHalfUnits(CharacterRuntimeData runtime, int halfUnits, int maxDisplay, bool isKarma)
        {
            return AddInternal(runtime, isKarma ? Mathf.Max(0, halfUnits) : 0, maxDisplay, isKarma);
        }

        private static int AddInternal(CharacterRuntimeData runtime, int amount, int maxDisplay, bool isKarma)
        {
            if (runtime == null || amount <= 0) return 0;
            EnsureMigrated(runtime, isKarma);
            int before = runtime.CurrentResource;
            runtime.CurrentResource = Mathf.Min(ToInternal(maxDisplay, isKarma), before + amount);
            return runtime.CurrentResource - before;
        }
    }

    public static class UniqueResourceRuntimeUtility
    {
        public static bool IsKarma(CharacterRuntimeData runtime)
        {
            if (runtime == null || string.IsNullOrWhiteSpace(runtime.CharacterId)) return false;
            DataManager data = DataManager.Instance;
            return data != null && data.CharacterDatabase != null &&
                   data.CharacterDatabase.TryGet(runtime.CharacterId, out CharacterMasterData master) &&
                   master.ResourceType == ResourceType.None;
        }

        public static int GetDisplayCurrent(CharacterRuntimeData runtime) =>
            KarmaFixedPoint.GetDisplayFloor(runtime, IsKarma(runtime));

        public static int AddDisplay(CharacterRuntimeData runtime, int amount, int maxDisplay) =>
            KarmaFixedPoint.AddDisplayUnits(runtime, amount, maxDisplay, IsKarma(runtime));

        public static void SetAtLeastDisplay(CharacterRuntimeData runtime, int displayValue, int maxDisplay)
        {
            if (runtime == null) return;
            bool karma = IsKarma(runtime);
            KarmaFixedPoint.EnsureMigrated(runtime, karma);
            runtime.CurrentResource = Mathf.Clamp(
                Mathf.Max(runtime.CurrentResource, KarmaFixedPoint.ToInternal(displayValue, karma)),
                0, KarmaFixedPoint.ToInternal(maxDisplay, karma));
        }

        public static bool IsAtMax(CharacterRuntimeData runtime, int maxDisplay)
        {
            bool karma = IsKarma(runtime);
            KarmaFixedPoint.EnsureMigrated(runtime, karma);
            return runtime != null && runtime.CurrentResource >= KarmaFixedPoint.ToInternal(maxDisplay, karma);
        }
    }
}
