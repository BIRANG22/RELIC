using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class LocalizedTMPFont : MonoBehaviour
{
    [SerializeField] private LocaleFontDatabase database;
    [SerializeField, LocaleFontRole] private string roleId = LocaleFontDatabase.BodyRoleId;
    [SerializeField] private TMP_Text target;

    private TMP_FontAsset originalFont;
    private Material originalMaterial;
    private bool originalCaptured;
    private bool missingDatabaseWarningLogged;

    public string RoleId => roleId;
    public TMP_Text Target => target;

    public void Configure(LocaleFontDatabase fontDatabase, string fontRoleId, TMP_Text textTarget = null)
    {
        database = fontDatabase;
        roleId = string.IsNullOrWhiteSpace(fontRoleId)
            ? LocaleFontDatabase.BodyRoleId
            : fontRoleId.Trim();
        target = textTarget != null ? textTarget : GetComponent<TMP_Text>();
        CaptureOriginalIfNeeded();

        if (isActiveAndEnabled)
            RefreshForLocale(LocalizationSettings.SelectedLocale);
    }

    public void RefreshForLocale(Locale locale)
    {
        if (target == null)
            target = GetComponent<TMP_Text>();
        if (target == null)
            return;

        CaptureOriginalIfNeeded();
        LocaleFontDatabase selectedDatabase = database != null
            ? database
            : LocaleFontDatabaseProvider.LoadDefault();

        if (selectedDatabase == null)
        {
            RestoreOriginal();
            if (!missingDatabaseWarningLogged)
            {
                missingDatabaseWarningLogged = true;
                Debug.LogWarning($"[LocalizedTMPFont] LocaleFontDatabase is missing for '{name}'.", this);
            }
            return;
        }

        string localeCode = locale != null ? locale.Identifier.Code : string.Empty;
        if (selectedDatabase.TryResolve(localeCode, roleId, out TMP_FontAsset resolvedFont))
        {
            target.font = resolvedFont;
            target.SetAllDirty();
            return;
        }

        RestoreOriginal();
    }

    private void Reset()
    {
        target = GetComponent<TMP_Text>();
        roleId = LocaleFontDatabase.BodyRoleId;
    }

    private void OnValidate()
    {
        if (target == null)
            target = GetComponent<TMP_Text>();
        if (string.IsNullOrWhiteSpace(roleId))
            roleId = LocaleFontDatabase.BodyRoleId;
    }

    private void OnEnable()
    {
        if (target == null)
            target = GetComponent<TMP_Text>();

        CaptureOriginalIfNeeded();
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RefreshForLocale(LocalizationSettings.SelectedLocale);
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale locale)
    {
        if (isActiveAndEnabled && locale == LocalizationSettings.SelectedLocale)
            RefreshForLocale(locale);
    }

    private void CaptureOriginalIfNeeded()
    {
        if (originalCaptured || target == null)
            return;

        originalCaptured = true;
        originalFont = target.font;
        originalMaterial = target.fontSharedMaterial;
    }

    private void RestoreOriginal()
    {
        if (!originalCaptured || target == null)
            return;

        target.font = originalFont;
        if (originalMaterial != null)
            target.fontSharedMaterial = originalMaterial;
        target.SetAllDirty();
    }
}
