using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the shared locale font database to every TMP text. Explicit
/// LocalizedTMPFont components keep ownership of their configured role.
/// </summary>
public static class GlobalLocalizedTMPFontApplicator
{
    private static readonly HashSet<int> ProcessingTextIds = new();
    private static readonly HashSet<int> KnownTextIds = new();
    private static readonly HashSet<int> CurrentTextIds = new();
    private static LocaleFontDatabase defaultDatabase;
    private static readonly Dictionary<int, string> SourceRoleByTextId = new();
    private static readonly Dictionary<int, TMP_FontAsset> OriginalFonts = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Unsubscribe();
        ProcessingTextIds.Clear();
        KnownTextIds.Clear();
        CurrentTextIds.Clear();
        defaultDatabase = null;
        SourceRoleByTextId.Clear();
        OriginalFonts.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        Unsubscribe();
        SceneManager.sceneLoaded += OnSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        Application.onBeforeRender += OnBeforeRender;
        Canvas.preWillRenderCanvases += OnCanvasPreRender;

        RefreshAll(LocalizationSettings.SelectedLocale);
    }

    public static bool ShouldProcessRuntimeCallback(bool isPlaying) => isPlaying;

    public static bool ApplyToText(TMP_Text text, LocaleFontDatabase database, Locale locale)
    {
        if (text == null || locale == null)
            return false;

        // A language selector shows multiple languages at once. Let its
        // DirectTemplateDropdown assign a font per option; never overwrite it
        // with the selected locale's global font.
        DirectTemplateDropdown selector = text.GetComponentInParent<DirectTemplateDropdown>(true);
        if (selector != null && selector.OwnsFont(text))
            return false;

        if (IsBlurReplica(text.transform))
            return false;

        int textId = text.GetInstanceID();
        if (!OriginalFonts.ContainsKey(textId))
            OriginalFonts[textId] = text.font;

        LocalizedTMPFont explicitOwner = text.GetComponent<LocalizedTMPFont>();
        if (explicitOwner != null)
        {
            explicitOwner.RefreshForLocale(locale);
            return true;
        }

        if (!SourceRoleByTextId.TryGetValue(textId, out string sourceRoleId))
        {
            sourceRoleId = LocaleFontDatabase.BodyRoleId;

            TMP_FontAsset sourceFont = OriginalFonts.TryGetValue(textId, out TMP_FontAsset original)
                ? original
                : text.font;
            if (database != null &&
                database.TryGetRoleIdForSourceFont(sourceFont, out string sourceRole, out bool preserveSourceFont))
            {
                if (!string.IsNullOrEmpty(sourceRole))
                    sourceRoleId = sourceRole;
                if (preserveSourceFont)
                    return true;
            }
            SourceRoleByTextId[textId] = sourceRoleId;
        }

        string roleId = IsFixedNonLocalizedLabel(text.text)
            ? LocaleFontDatabase.NumberRoleId
            : sourceRoleId;

        if (database == null ||
            !database.TryResolve(locale.Identifier.Code, roleId, out TMP_FontAsset font) ||
            font == null)
        {
            return false;
        }

        LocaleTMPFontGuard.ApplyResolvedFont(text, font);

        return true;
    }

    public static void RefreshAll(Locale locale)
    {
        if (locale == null)
            return;

        LocaleFontDatabase database = GetDefaultDatabase();
        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        KnownTextIds.Clear();
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
                KnownTextIds.Add(texts[i].GetInstanceID());
            ApplySafely(texts[i], database, locale);
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (ShouldProcessRuntimeCallback(Application.isPlaying) && scene.IsValid())
            RefreshAll(LocalizationSettings.SelectedLocale);
    }

    private static void OnLocaleChanged(Locale locale)
    {
        if (ShouldProcessRuntimeCallback(Application.isPlaying) &&
            locale == LocalizationSettings.SelectedLocale)
            RefreshAll(locale);
    }

    private static void OnTextChanged(Object changedObject)
    {
        if (changedObject is not TMP_Text text ||
            !ShouldProcessRuntimeCallback(Application.isPlaying))
            return;

        KnownTextIds.Add(text.GetInstanceID());
        ApplySafely(
            text,
            GetDefaultDatabase(),
            LocalizationSettings.SelectedLocale);
    }

    // Unity renders TMP UI through Canvas events. Apply the locale font before
    // TMP generates its mesh, including texts changed on an existing object.
    private static void OnCanvasPreRender()
    {
        RefreshExistingTexts();
    }

    private static void OnBeforeRender()
    {
        RefreshExistingTexts();
    }

    private static void RefreshExistingTexts()
    {
        if (!ShouldProcessRuntimeCallback(Application.isPlaying))
            return;

        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        CurrentTextIds.Clear();

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
                continue;

            int instanceId = text.GetInstanceID();
            CurrentTextIds.Add(instanceId);
            KnownTextIds.Add(instanceId);
            // Do not restrict the refresh to newly-created objects. Controllers
            // frequently replace the text of existing TMP labels at runtime.
            ApplySafely(
                text,
                GetDefaultDatabase(),
                LocalizationSettings.SelectedLocale);
        }

        KnownTextIds.IntersectWith(CurrentTextIds);
        // Clean up caches for scene objects destroyed since the previous frame.
        var stale = new List<int>();
        foreach (int id in SourceRoleByTextId.Keys)
            if (!CurrentTextIds.Contains(id)) stale.Add(id);
        for (int i = 0; i < stale.Count; i++) SourceRoleByTextId.Remove(stale[i]);
        stale.Clear();
        foreach (int id in OriginalFonts.Keys)
            if (!CurrentTextIds.Contains(id)) stale.Add(id);
        for (int i = 0; i < stale.Count; i++) OriginalFonts.Remove(stale[i]);
    }

    private static LocaleFontDatabase GetDefaultDatabase()
    {
        if (defaultDatabase == null)
            defaultDatabase = LocaleFontDatabaseProvider.LoadDefault();
        return defaultDatabase;
    }

    private static void Unsubscribe()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        Application.onBeforeRender -= OnBeforeRender;
        Canvas.preWillRenderCanvases -= OnCanvasPreRender;
    }

    // Fixed numeric/shortcut labels are not translation targets. In particular,
    // rune unlock labels such as "LV.2" must never inherit a locale font.
    public static bool IsFixedNonLocalizedLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string trimmed = value.Trim();
        if (trimmed.StartsWith("LV.", System.StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("LV ", System.StringComparison.OrdinalIgnoreCase))
        {
            string digits = trimmed.Substring(3).Trim();
            if (digits.Length > 0)
            {
                bool allDigits = true;
                for (int i = 0; i < digits.Length; i++)
                    allDigits &= char.IsDigit(digits[i]);
                if (allDigits) return true;
            }
        }

        // Standalone values, percentages, fractions and common keyboard hints.
        bool numericOnly = true;
        bool containsDigit = false;
        for (int i = 0; i < trimmed.Length; i++)
        {
            char ch = trimmed[i];
            if (char.IsDigit(ch)) containsDigit = true;
            else if (!char.IsWhiteSpace(ch) && "./:%+-×".IndexOf(ch) < 0)
                numericOnly = false;
        }
        return numericOnly && containsDigit;
    }

    private static bool IsBlurReplica(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (current.name == "BlurReplicaRoot")
                return true;
        }

        return false;
    }

    private static void ApplySafely(TMP_Text text, LocaleFontDatabase database, Locale locale)
    {
        if (text == null)
            return;

        int instanceId = text.GetInstanceID();
        if (!ProcessingTextIds.Add(instanceId))
            return;

        try
        {
            ApplyToText(text, database, locale);
        }
        finally
        {
            ProcessingTextIds.Remove(instanceId);
        }
    }
}
