using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Components;
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
    private static readonly HashSet<int> PreserveSourceTextIds = new();
    // Dynamic localized labels can lack a LocalizedTMPText component.
    private static readonly HashSet<int> RecognizedLocalizedTextIds = new();
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
        PreserveSourceTextIds.Clear();
        RecognizedLocalizedTextIds.Clear();
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

        int textId = text.GetInstanceID();
        if (!OriginalFonts.ContainsKey(textId))
            OriginalFonts[textId] = text.font;

        // Level counters and other fixed numeric UI labels retain their source font,
        // even if this TMP previously displayed a localized CJK string.
        if (IsFixedNonLocalizedLabel(text.text))
        {
            if (OriginalFonts.TryGetValue(textId, out TMP_FontAsset original) &&
                original != null && text.font != original)
            {
                text.font = original;
                text.SetAllDirty();
            }
            return false;
        }

        // Only actual localized content may receive a locale-dependent font.
        // Numeric counters, input hints and fixed English identifiers must
        // retain the font assigned in the Inspector.
        if (!ShouldApplyLocaleFont(text))
            return false;

        LocalizedTMPFont explicitOwner = text.GetComponent<LocalizedTMPFont>();
        if (explicitOwner != null)
        {
            explicitOwner.RefreshForLocale(locale);
            return true;
        }

        // Cache each TMP's role on its first encounter. Later font changes must not
        // change the role when the user switches locales repeatedly.
        if (PreserveSourceTextIds.Contains(textId))
        {
            if (!ContainsLocalizedScript(text.text))
                return true;
            PreserveSourceTextIds.Remove(textId);
            SourceRoleByTextId[textId] = LocaleFontDatabase.BodyRoleId;
        }

        if (!SourceRoleByTextId.TryGetValue(textId, out string roleId))
        {
            roleId = LocaleFontDatabase.BodyRoleId;
            if (database != null &&
                database.TryGetRoleIdForSourceFont(text.font, out string sourceRole, out bool preserveSourceFont))
            {
                if (!string.IsNullOrEmpty(sourceRole))
                    roleId = sourceRole;
                // A preserved number font is for numerical counters, not
                // localized words dynamically written into the same TMP.
                if (preserveSourceFont && !ContainsLocalizedScript(text.text))
                {
                    PreserveSourceTextIds.Add(textId);
                    return true;
                }
                if (preserveSourceFont && ContainsLocalizedScript(text.text))
                    roleId = LocaleFontDatabase.BodyRoleId;
            }
            if (ContainsLocalizedScript(text.text) && roleId == LocaleFontDatabase.NumberRoleId)
                roleId = LocaleFontDatabase.BodyRoleId;
            SourceRoleByTextId[textId] = roleId;
        }

        if (database == null ||
            !database.TryResolve(locale.Identifier.Code, roleId, out TMP_FontAsset font) ||
            font == null)
        {
            return false;
        }

        if (text.font != font)
        {
            text.font = font;
            text.SetAllDirty();
        }

        return true;
    }

    public static bool ShouldApplyLocaleFont(TMP_Text text)
    {
        if (text == null || IsBlurReplica(text.transform) ||
            IsFixedNonLocalizedLabel(text.text))
            return false;

        // Controller-owned TMPs often use GameLocalization.Get directly,
        // without a binding component. Recognize translated CJK labels while
        // preserving fixed Latin UI labels and numeric counters. Once a TMP
        // has shown localized script, keep its role across locale changes.
        int textId = text.GetInstanceID();
        if (RecognizedLocalizedTextIds.Contains(textId))
            return true;
        if (ContainsLocalizedScript(text.text))
        {
            RecognizedLocalizedTextIds.Add(textId);
            return true;
        }

        LocalizedTMPText staticText = text.GetComponent<LocalizedTMPText>();
        if (staticText != null && staticText.isActiveAndEnabled &&
            !string.IsNullOrWhiteSpace(staticText.LocalizationKey) &&
            LocalizedTMPText.ShouldManageText(text))
            return true;

        DynamicLocalizedTMPText dynamicText = text.GetComponent<DynamicLocalizedTMPText>();
        if (dynamicText != null && dynamicText.isActiveAndEnabled &&
            dynamicText.HasLocalizationKey)
            return true;

        LocalizeStringEvent localizedString = text.GetComponent<LocalizeStringEvent>();
        if (localizedString != null && localizedString.isActiveAndEnabled &&
            localizedString.StringReference.TableEntryReference.ReferenceType != UnityEngine.Localization.Tables.TableEntryReference.Type.Empty &&
            text.GetComponentInParent<LocalizationIgnore>(true) == null)
            return true;

        return false;
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
        PreserveSourceTextIds.IntersectWith(CurrentTextIds);
        RecognizedLocalizedTextIds.IntersectWith(CurrentTextIds);
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
            else if (!char.IsWhiteSpace(ch) && "./:%+-¡¿".IndexOf(ch) < 0)
                numericOnly = false;
        }
        return numericOnly && containsDigit;
    }

    private static bool ContainsLocalizedScript(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            char ch = value[i];
            if ((ch >= '\u3040' && ch <= '\u30ff') || // Japanese kana
                (ch >= '\u3400' && ch <= '\u9fff') || // Chinese ideographs
                (ch >= '\uac00' && ch <= '\ud7a3') || // Hangul syllables
                (ch >= '\u1100' && ch <= '\u11ff'))   // Hangul jamo
                return true;
        }
        return false;
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
