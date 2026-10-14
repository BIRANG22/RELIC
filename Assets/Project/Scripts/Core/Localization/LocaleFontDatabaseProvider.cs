using UnityEngine;

public static class LocaleFontDatabaseProvider
{
    public const string ResourcePath = "Localization/LocaleFontDatabase";

    public static LocaleFontDatabase LoadDefault() =>
        Resources.Load<LocaleFontDatabase>(ResourcePath);
}
