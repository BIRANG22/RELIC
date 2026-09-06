using System.IO;
using NUnit.Framework;

public class ManualProgressSaveOnlyTests
{
    [Test]
    public void CheckpointAutosave_UsesDedicatedApiAndNeverSavesDuringTitleExitOrAbandon()
    {
        string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
        string scriptsRoot = Path.Combine(projectRoot, "Assets", "Project", "Scripts");
        string saveSystem = File.ReadAllText(Path.Combine(scriptsRoot, "Core", "SaveSystem.cs"));
        string titleAbandon = File.ReadAllText(Path.Combine(scriptsRoot, "TitleAbandonBattleButton.cs"));
        string uiManager = File.ReadAllText(Path.Combine(scriptsRoot, "Core", "Managers", "UIManager.cs"));

        Assert.That(saveSystem, Does.Contain("public bool SaveCheckpoint()"));
        Assert.That(titleAbandon, Does.Contain("DeleteSaveFile()"));
        Assert.That(titleAbandon, Does.Not.Contain("SaveCurrentProgress()"));

        int saveAndExitStart = uiManager.IndexOf("public async void SaveAndReturnToTitle()", System.StringComparison.Ordinal);
        int saveAndExitEnd = uiManager.IndexOf("private void AbandonCurrentBattleRunIfPossible()", saveAndExitStart, System.StringComparison.Ordinal);
        Assert.That(saveAndExitStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(saveAndExitEnd, Is.GreaterThan(saveAndExitStart));
        Assert.That(uiManager.Substring(saveAndExitStart, saveAndExitEnd - saveAndExitStart),
            Does.Not.Contain("SaveCurrentProgress()"));
    }

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
