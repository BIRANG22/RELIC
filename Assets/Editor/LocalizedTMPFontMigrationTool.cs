using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class LocalizedTMPFontMigrationTool
{
    [MenuItem("Tools/RELIC/Localization/Add Locale Font Role To Selection")]
    private static void AddToSelection()
    {
        GameObject[] roots = Selection.gameObjects;
        if (roots == null || roots.Length == 0)
        {
            Debug.LogWarning("[LocalizedTMPFontMigration] Select a hierarchy root or prefab first.");
            return;
        }

        int changed = 0;
        var visited = new HashSet<int>();
        for (int i = 0; i < roots.Length; i++)
        {
            TMP_Text[] texts = roots[i].GetComponentsInChildren<TMP_Text>(true);
            for (int textIndex = 0; textIndex < texts.Length; textIndex++)
            {
                TMP_Text text = texts[textIndex];
                if (text == null || !visited.Add(text.GetInstanceID()))
                    continue;
                if (text.GetComponent<LocalizedTMPFont>() == null)
                {
                    Undo.AddComponent<LocalizedTMPFont>(text.gameObject);
                    changed++;
                }
            }

            OptionTMPFontEnforcer enforcer = roots[i].GetComponentInChildren<OptionTMPFontEnforcer>(true);
            if (enforcer != null)
                Debug.Log($"[LocalizedTMPFontMigration] Refresh-only legacy hook remains on '{enforcer.name}'.", enforcer);
        }

        Debug.Log($"[LocalizedTMPFontMigration] Added {changed} body-role component(s) in the selection.");
    }

    [MenuItem("Tools/RELIC/Localization/Audit Selected Font Ownership")]
    private static void AuditSelection()
    {
        GameObject[] roots = Selection.gameObjects;
        int conflicts = 0;
        for (int i = 0; i < roots.Length; i++)
        {
            TMP_Text[] texts = roots[i].GetComponentsInChildren<TMP_Text>(true);
            for (int textIndex = 0; textIndex < texts.Length; textIndex++)
            {
                TMP_Text text = texts[textIndex];
                if (text != null && text.GetComponents<LocalizedTMPFont>().Length > 1)
                {
                    conflicts++;
                    Debug.LogWarning($"[LocalizedTMPFontMigration] Multiple font owners on '{text.name}'.", text);
                }
            }
        }
        Debug.Log($"[LocalizedTMPFontMigration] Audit complete. Conflicts: {conflicts}.");
    }
}
