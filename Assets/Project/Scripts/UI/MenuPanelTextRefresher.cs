using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public sealed class MenuPanelTextRefresher : MonoBehaviour
{
    private Coroutine refreshRoutine;

    private void OnEnable()
    {
        RefreshNow();
        RefreshNextFrame();
    }

    private void OnDisable()
    {
        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }

    public void RefreshNow()
    {
        // Resolve locale fonts before any layout/canvas text generation.
        // This also covers TMP labels populated directly by runtime scripts.
        Locale locale = LocalizationSettings.SelectedLocale;
        LocaleFontDatabase database = LocaleFontDatabaseProvider.LoadDefault();
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
            RefreshText(texts[i], database, locale);

        if (transform is RectTransform rectTransform)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        Canvas.ForceUpdateCanvases();
    }

    public void RefreshNextFrame()
    {
        if (!isActiveAndEnabled)
            return;

        if (refreshRoutine != null)
            StopCoroutine(refreshRoutine);

        refreshRoutine = StartCoroutine(RefreshDelayed());
    }

    private IEnumerator RefreshDelayed()
    {
        yield return null;
        RefreshNow();
        yield return null;
        RefreshNow();
        refreshRoutine = null;
    }

    private static void RefreshText(TMP_Text text, LocaleFontDatabase database, Locale locale)
    {
        if (text == null)
            return;

        // Preserve Inspector fonts for numeric/fixed labels. Localized text uses
        // the current locale's database font before Unity generates glyphs.
        if (database != null && locale != null)
            GlobalLocalizedTMPFontApplicator.ApplyToText(text, database, locale);

        if (text.font != null && text.font.material != null)
            text.fontSharedMaterial = text.font.material;

        text.UpdateMeshPadding();
        text.SetAllDirty();
        // Do not force glyph generation with a stale font. The canvas refresh
        // below will rebuild the text after the locale font has been resolved.
    }
}
