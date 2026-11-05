using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Applies the locale font before a localized TMP writer, mesh rebuild or
/// preferred-size calculation. Never changes a fixed counter, dropdown item
/// owned by its selector, or a TMP-generated submesh.
/// </summary>
public static class LocaleTMPFontGuard
{
    public static void Prepare(TMP_Text target)
    {
        if (target == null)
            return;

        Locale locale = LocalizationSettings.SelectedLocale;
        if (locale == null)
            return;

        LocaleFontDatabase database = LocaleFontDatabaseProvider.LoadDefault();
        if (database == null)
            return;

        GlobalLocalizedTMPFontApplicator.ApplyToText(target, database, locale);
    }

    // Explicit localization writes must not wait for TMP's render callback.
    public static void PrepareLocalized(TMP_Text target, Locale locale)
    {
        if (target == null || locale == null)
            return;

        DirectTemplateDropdown selector = target.GetComponentInParent<DirectTemplateDropdown>(true);
        if (selector != null && selector.OwnsFont(target))
            return;

        LocaleFontDatabase database = LocaleFontDatabaseProvider.LoadDefault();
        if (database == null ||
            !database.TryResolve(locale.Identifier.Code, LocaleFontDatabase.BodyRoleId, out TMP_FontAsset font) ||
            font == null)
            return;

        if (target.font != font)
            target.font = font;
    }
}
