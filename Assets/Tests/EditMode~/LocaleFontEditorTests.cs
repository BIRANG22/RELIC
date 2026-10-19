using System.IO;
using NUnit.Framework;

public sealed class LocaleFontEditorTests
{
    [Test]
    public void LocaleFontEditorSupportFiles_Exist()
    {
        Assert.That(File.Exists("Assets/Editor/LocaleFontDatabaseEditor.cs"), Is.True);
        Assert.That(File.Exists("Assets/Editor/LocaleFontRoleDrawer.cs"), Is.True);
        Assert.That(File.Exists("Assets/Editor/LocalizedTMPFontMigrationTool.cs"), Is.True);
    }

    [Test]
    public void DefaultDatabase_ContainsEveryProjectLocaleAndInitialRole()
    {
        const string path = "Assets/Resources/Localization/LocaleFontDatabase.asset";
        Assert.That(File.Exists(path), Is.True);

        string yaml = File.ReadAllText(path);
        foreach (string locale in new[] { "ko", "en", "es", "ja", "zh-Hans" })
            Assert.That(yaml, Does.Contain("localeCode: " + locale));
        foreach (string role in new[] { "body", "title", "number" })
            Assert.That(yaml, Does.Contain("roleId: " + role));
    }

    [Test]
    public void OptionFontEnforcer_NoLongerCompetesForFontOwnershipEveryFrame()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/OptionTMPFontEnforcer.cs");

        Assert.That(source, Does.Not.Contain("private void LateUpdate"));
        Assert.That(source, Does.Not.Contain("text.font ="));
        Assert.That(source, Does.Not.Contain("fontSharedMaterial ="));
        Assert.That(source, Does.Contain("LocalizedTMPFont"));
    }

    [Test]
    public void MigrationTool_IsSelectionScoped()
    {
        string source = File.ReadAllText("Assets/Editor/LocalizedTMPFontMigrationTool.cs");

        Assert.That(source, Does.Contain("Selection.gameObjects"));
        Assert.That(source, Does.Not.Contain("FindObjectsByType<TMP_Text>"));
        Assert.That(source, Does.Contain("OptionTMPFontEnforcer"));
    }

    [Test]
    public void OptionPrefab_AssignsOneLocaleFontOwnerToEveryTmpText()
    {
        string prefab = File.ReadAllText("Assets/Project/PrefabsR/Option.prefab");
        const string tmpGuid = "f4688fdb7df04437aeb418b961361dc5";
        const string localizerGuid = "f1d4d9911a264e71a4ac5543ba688005";

        Assert.That(Count(prefab, tmpGuid), Is.EqualTo(Count(prefab, localizerGuid)));
        Assert.That(Count(prefab, localizerGuid), Is.GreaterThan(0));
    }

    [Test]
    public void GlobalFontApplicator_HooksSceneLocaleAndRuntimeTmpChanges()
    {
        const string path = "Assets/Project/Scripts/Core/Localization/GlobalLocalizedTMPFontApplicator.cs";
        Assert.That(File.Exists(path), Is.True);

        string source = File.ReadAllText(path);
        Assert.That(source, Does.Contain("SceneManager.sceneLoaded"));
        Assert.That(source, Does.Contain("LocalizationSettings.SelectedLocaleChanged"));
        Assert.That(source, Does.Contain("TMPro_EventManager.TEXT_CHANGED_EVENT"));
        Assert.That(source, Does.Contain("FindObjectsInactive.Include"));
        Assert.That(source, Does.Contain("LocaleFontDatabase.BodyRoleId"));
        Assert.That(source, Does.Contain("Application.onBeforeRender"));
        Assert.That(source, Does.Contain("RuntimeInitializeLoadType.SubsystemRegistration"));
        Assert.That(source, Does.Contain("ShouldProcessRuntimeCallback(Application.isPlaying)"));
    }

    private static int Count(string source, string value) =>
        source.Split(new[] { value }, System.StringSplitOptions.None).Length - 1;
}
