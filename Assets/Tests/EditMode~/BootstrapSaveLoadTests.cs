using System.IO;
using NUnit.Framework;

public class BootstrapSaveLoadTests
{
    private const string BootstrapPath = "Assets/Project/Scripts/Core/Bootstrap.cs";

    [Test]
    public void Bootstrap_LoadsSavedProgressAfterDataManagerInitialization()
    {
        string source = File.ReadAllText(BootstrapPath);

        int saveInitializeIndex = source.IndexOf("SaveSystem.Instance.Initialize()", System.StringComparison.Ordinal);
        int dataInitializeIndex = source.IndexOf("DataManager.Instance.Initialize()", System.StringComparison.Ordinal);
        int saveLoadIndex = source.IndexOf("SaveSystem.Instance.TryLoadProgress()", System.StringComparison.Ordinal);
        int gameManagerInitializeIndex = source.IndexOf("GameManager.Instance.Initialize()", System.StringComparison.Ordinal);

        Assert.That(saveInitializeIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(dataInitializeIndex, Is.GreaterThan(saveInitializeIndex));
        Assert.That(saveLoadIndex, Is.GreaterThan(dataInitializeIndex));
        Assert.That(saveLoadIndex, Is.LessThan(gameManagerInitializeIndex));
    }
}
