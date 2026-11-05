using UnityEngine;

public static class LocaleFontDatabaseProvider
{
    public const string ResourcePath = "Localization/LocaleFontDatabase";

    private static LocaleFontDatabase cachedDatabase;
    private static bool warnedMissing;

    public static LocaleFontDatabase LoadDefault()
    {
        if (cachedDatabase != null)
            return cachedDatabase;

        cachedDatabase = Resources.Load<LocaleFontDatabase>(ResourcePath);

#if UNITY_EDITOR
        // Existing projects may keep the asset outside Resources. Support Editor Play
        // Mode without silently changing references, but require Resources for builds.
        if (cachedDatabase == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:LocaleFontDatabase");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                LocaleFontDatabase candidate = UnityEditor.AssetDatabase.LoadAssetAtPath<LocaleFontDatabase>(path);
                if (candidate == null)
                    continue;
                cachedDatabase = candidate;
                break;
            }
        }
#endif

        if (cachedDatabase == null && !warnedMissing)
        {
            warnedMissing = true;
            Debug.LogWarning("[Localization] LocaleFontDatabase not found. For builds, place the database asset at Assets/Resources/Localization/LocaleFontDatabase.asset.");
        }

        return cachedDatabase;
    }
}
