using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public sealed class LocaleFontDatabaseTests
{
    private LocaleFontDatabase database;
    private TMP_FontAsset koreanBody;
    private TMP_FontAsset englishBody;
    private TMP_FontAsset englishDefault;

    [SetUp]
    public void SetUp()
    {
        database = ScriptableObject.CreateInstance<LocaleFontDatabase>();
        koreanBody = ScriptableObject.CreateInstance<TMP_FontAsset>();
        englishBody = ScriptableObject.CreateInstance<TMP_FontAsset>();
        englishDefault = ScriptableObject.CreateInstance<TMP_FontAsset>();

        SetProfiles(
            CreateProfile("ko", null, CreateRole("body", koreanBody)),
            CreateProfile("en", englishDefault, CreateRole("body", englishBody)));
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(database);
        UnityEngine.Object.DestroyImmediate(koreanBody);
        UnityEngine.Object.DestroyImmediate(englishBody);
        UnityEngine.Object.DestroyImmediate(englishDefault);
    }

    [Test]
    public void TryResolve_UsesExactLocaleAndRole()
    {
        Assert.That(database.TryResolve("ko", "body", out TMP_FontAsset resolved), Is.True);
        Assert.That(resolved, Is.SameAs(koreanBody));
    }

    [Test]
    public void TryResolve_UsesParentLocaleRoleBeforeParentDefault()
    {
        Assert.That(database.TryResolve("en-US", "body", out TMP_FontAsset resolved), Is.True);
        Assert.That(resolved, Is.SameAs(englishBody));
    }

    [Test]
    public void TryResolve_UsesLocaleDefaultWhenRoleIsMissing()
    {
        Assert.That(database.TryResolve("en", "title", out TMP_FontAsset resolved), Is.True);
        Assert.That(resolved, Is.SameAs(englishDefault));
    }

    [Test]
    public void TryResolve_DoesNotTreatScriptLocaleAsParentLanguage()
    {
        Assert.That(database.TryResolve("zh-Hans", "body", out _), Is.False);
    }

    [Test]
    public void TryResolve_UnknownLocaleFailsWithoutBorrowingAnotherLanguage()
    {
        Assert.That(database.TryResolve("ja", "body", out TMP_FontAsset resolved), Is.False);
        Assert.That(resolved, Is.Null);
    }

    [Test]
    public void Validate_ReportsDuplicateLocaleAndRoleIds()
    {
        SetProfiles(
            CreateProfile("en", null, CreateRole("body", englishBody), CreateRole("body", englishDefault)),
            CreateProfile("en", null, CreateRole("title", englishBody)));

        LocaleFontDatabase.ValidationIssue[] issues = database.Validate();

        Assert.That(Array.Exists(issues, issue => issue.Code == "duplicate-locale"), Is.True);
        Assert.That(Array.Exists(issues, issue => issue.Code == "duplicate-role"), Is.True);
    }

    [Test]
    public void Validate_ReportsInitialRoleMissingFromLocaleProfile()
    {
        SetProfiles(CreateProfile("en", englishDefault, CreateRole("body", englishBody)));

        LocaleFontDatabase.ValidationIssue[] issues = database.Validate();

        Assert.That(
            Array.Exists(issues, issue => issue.Code == "missing-role" && issue.Message.Contains("title")),
            Is.True);
        Assert.That(
            Array.Exists(issues, issue => issue.Code == "missing-role" && issue.Message.Contains("number")),
            Is.True);
    }

    [Test]
    public void LocalizedTMPFont_UsesResolvedFontAndRestoresOriginalForUnknownLocale()
    {
        var gameObject = new GameObject("Localized Font Test", typeof(RectTransform));
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset original = ScriptableObject.CreateInstance<TMP_FontAsset>();
        text.font = original;

        try
        {
            LocalizedTMPFont localizer = gameObject.AddComponent<LocalizedTMPFont>();
            localizer.Configure(database, "body", text);

            localizer.RefreshForLocale(Locale.CreateLocale("ko"));
            Assert.That(text.font, Is.SameAs(koreanBody));

            localizer.RefreshForLocale(Locale.CreateLocale("ja"));
            Assert.That(text.font, Is.SameAs(original));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
            UnityEngine.Object.DestroyImmediate(original);
        }
    }

    [Test]
    public void GlobalApplicator_UsesBodyRoleForTextWithoutExplicitOwner()
    {
        var gameObject = new GameObject("Global Font Test", typeof(RectTransform));
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();

        try
        {
            bool applied = GlobalLocalizedTMPFontApplicator.ApplyToText(
                text,
                database,
                Locale.CreateLocale("ko"));

            Assert.That(applied, Is.True);
            Assert.That(text.font, Is.SameAs(koreanBody));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void GlobalApplicator_PreservesLegacyStyledTextWhenSourceMappingRequestsIt()
    {
        TMP_FontAsset legacyNumberFont = ScriptableObject.CreateInstance<TMP_FontAsset>();
        TMP_FontAsset numberFont = ScriptableObject.CreateInstance<TMP_FontAsset>();
        SetProfiles(CreateProfile(
            "ko",
            null,
            CreateRole("body", koreanBody),
            CreateRole("number", numberFont)));
        SetSourceFontRoles(CreateSourceFontRole(legacyNumberFont, "number", true));

        var gameObject = new GameObject("Legacy Number Font Test", typeof(RectTransform));
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.font = legacyNumberFont;

        try
        {
            Assert.That(
                database.TryGetRoleIdForSourceFont(
                    legacyNumberFont,
                    out string mappedRoleId,
                    out bool preserveSourceFont),
                Is.True);
            Assert.That(mappedRoleId, Is.EqualTo("number"));
            Assert.That(preserveSourceFont, Is.True);

            bool applied = GlobalLocalizedTMPFontApplicator.ApplyToText(
                text,
                database,
                Locale.CreateLocale("ko"));

            Assert.That(applied, Is.True);
            Assert.That(text.font, Is.SameAs(legacyNumberFont));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
            UnityEngine.Object.DestroyImmediate(legacyNumberFont);
            UnityEngine.Object.DestroyImmediate(numberFont);
        }
    }

    [Test]
    public void GlobalApplicator_PreservesExplicitRoleOwner()
    {
        TMP_FontAsset numberFont = ScriptableObject.CreateInstance<TMP_FontAsset>();
        SetProfiles(CreateProfile(
            "ko",
            null,
            CreateRole("body", koreanBody),
            CreateRole("number", numberFont)));

        var gameObject = new GameObject("Explicit Font Role Test", typeof(RectTransform));
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();

        try
        {
            LocalizedTMPFont localizer = gameObject.AddComponent<LocalizedTMPFont>();
            localizer.Configure(database, "number", text);

            bool applied = GlobalLocalizedTMPFontApplicator.ApplyToText(
                text,
                database,
                Locale.CreateLocale("ko"));

            Assert.That(applied, Is.True);
            Assert.That(text.font, Is.SameAs(numberFont));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
            UnityEngine.Object.DestroyImmediate(numberFont);
        }
    }

    [Test]
    public void GlobalApplicator_MissingLocaleLeavesCurrentFontUntouched()
    {
        TMP_FontAsset original = ScriptableObject.CreateInstance<TMP_FontAsset>();
        var gameObject = new GameObject("Missing Locale Font Test", typeof(RectTransform));
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.font = original;

        try
        {
            bool applied = GlobalLocalizedTMPFontApplicator.ApplyToText(
                text,
                database,
                Locale.CreateLocale("ja"));

            Assert.That(applied, Is.False);
            Assert.That(text.font, Is.SameAs(original));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
            UnityEngine.Object.DestroyImmediate(original);
        }
    }

    [Test]
    public void GlobalApplicator_RuntimeCallbacksOnlyRunDuringPlayMode()
    {
        Assert.That(GlobalLocalizedTMPFontApplicator.ShouldProcessRuntimeCallback(false), Is.False);
        Assert.That(GlobalLocalizedTMPFontApplicator.ShouldProcessRuntimeCallback(true), Is.True);
    }

    private void SetProfiles(params LocaleFontDatabase.LocaleFontProfile[] profiles)
    {
        SetField(database, "profiles", profiles);
    }

    private void SetSourceFontRoles(params LocaleFontDatabase.SourceFontRoleEntry[] entries)
    {
        SetField(database, "sourceFontRoles", entries);
    }

    private static LocaleFontDatabase.SourceFontRoleEntry CreateSourceFontRole(
        TMP_FontAsset sourceFont,
        string roleId,
        bool preserveSourceFont)
    {
        var entry = new LocaleFontDatabase.SourceFontRoleEntry();
        SetField(entry, "sourceFont", sourceFont);
        SetField(entry, "roleId", roleId);
        SetField(entry, "preserveSourceFont", preserveSourceFont);
        return entry;
    }

    private static LocaleFontDatabase.LocaleFontProfile CreateProfile(
        string localeCode,
        TMP_FontAsset defaultFont,
        params LocaleFontDatabase.RoleFontEntry[] roles)
    {
        var profile = new LocaleFontDatabase.LocaleFontProfile();
        SetField(profile, "localeCode", localeCode);
        SetField(profile, "defaultFont", defaultFont);
        SetField(profile, "roleFonts", roles);
        return profile;
    }

    private static LocaleFontDatabase.RoleFontEntry CreateRole(string roleId, TMP_FontAsset font)
    {
        var role = new LocaleFontDatabase.RoleFontEntry();
        SetField(role, "roleId", roleId);
        SetField(role, "font", font);
        return role;
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing serialized field: {name}");
        field.SetValue(target, value);
    }
}
