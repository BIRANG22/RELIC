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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Unsubscribe();
        ProcessingTextIds.Clear();
        KnownTextIds.Clear();
        CurrentTextIds.Clear();
        defaultDatabase = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        Unsubscribe();
        SceneManager.sceneLoaded += OnSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        Application.onBeforeRender += OnBeforeRender;

        RefreshAll(LocalizationSettings.SelectedLocale);
    }

    public static bool ShouldProcessRuntimeCallback(bool isPlaying) => isPlaying;

    public static bool ApplyToText(TMP_Text text, LocaleFontDatabase database, Locale locale)
    {
        if (text == null || locale == null)
            return false;

        LocalizedTMPFont explicitOwner = text.GetComponent<LocalizedTMPFont>();
        if (explicitOwner != null)
        {
            explicitOwner.RefreshForLocale(locale);
            return true;
        }

        string roleId = LocaleFontDatabase.BodyRoleId;
        if (database != null &&
            database.TryGetRoleIdForSourceFont(
                text.font,
                out roleId,
                out bool preserveSourceFont) &&
            preserveSourceFont)
        {
            return true;
        }
        if (string.IsNullOrEmpty(roleId))
            roleId = LocaleFontDatabase.BodyRoleId;

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

    private static void OnBeforeRender()
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
            if (KnownTextIds.Add(instanceId))
            {
                ApplySafely(
                    text,
                    GetDefaultDatabase(),
                    LocalizationSettings.SelectedLocale);
            }
        }

        KnownTextIds.IntersectWith(CurrentTextIds);
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
