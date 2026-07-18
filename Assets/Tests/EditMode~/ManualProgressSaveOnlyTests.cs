using System.IO;
using NUnit.Framework;

public class ManualProgressSaveOnlyTests
{
    [Test]
    public void ProgressSave_IsOnlyInvokedByExplicitSaveButton()
    {
        string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
        string scriptsRoot = Path.Combine(projectRoot, "Assets", "Project", "Scripts");
        string[] files = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories);

        foreach (string file in files)
        {
            string normalized = file.Replace('\\', '/');
            if (normalized.EndsWith("/Core/SaveSystem.cs") ||
                normalized.EndsWith("/UI/Title/OptionPanelUI.cs"))
            {
                continue;
            }

            Assert.That(
                File.ReadAllText(file),
                Does.Not.Contain("SaveCurrentProgress()"),
                $"자동 진행 저장 호출이 남아 있습니다: {normalized}");
        }
    }
}
