using System.IO;
using NUnit.Framework;

public sealed class DebugShortcutHelpPanelSourceTests
{
    [Test]
    public void Bootstrap_DoesNotCreateDebugShortcutHelpPanelAtRuntime()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/Core/Bootstrap.cs");

        Assert.That(source, Does.Not.Contain("DebugShortcutHelpPanel.EnsureInstance();"));
    }

    [Test]
    public void DebugShortcutHelpPanel_UsesBackQuoteToggleAndCtrlBackspaceQuestReset()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/Debug/DebugShortcutHelpPanel.cs");

        StringAssert.Contains("KeyCode.BackQuote", source);
        StringAssert.Contains("KeyCode.Backspace", source);
        StringAssert.Contains("KeyCode.LeftControl", source);
        StringAssert.Contains("KeyCode.RightControl", source);
        Assert.That(source, Does.Not.Contain("KeyCode.F10"));
        StringAssert.Contains("ResetQuestProgress", source);
        StringAssert.Contains("LobbyTutorialProgress.NotStarted", source);
        StringAssert.Contains("SaveSystem.Instance.SaveCurrentProgress()", source);
        StringAssert.Contains("LobbyQuestManager.Instance?.Refresh()", source);
        Assert.That(source, Does.Not.Contain("new GameObject"));
        Assert.That(source, Does.Not.Contain("EnsurePanel"));
        Assert.That(source, Does.Not.Contain("EnsureInstance"));
    }

    [Test]
    public void RuntimeDataDebugKey_NoLongerUsesBackQuote()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/Debug/RuntimeDataDebugKey.cs");

        Assert.That(source, Does.Not.Contain("KeyCode.BackQuote"));
        StringAssert.Contains("KeyCode.F12", source);
    }

    [Test]
    public void BackQuote_IsReservedForDebugShortcutHelpPanel()
    {
        string vfxWorkbench = File.ReadAllText("Assets/Project/Scripts/Debug/TestVfxWorkbench.cs");

        Assert.That(vfxWorkbench, Does.Not.Contain("KeyCode.BackQuote"));
        StringAssert.Contains("KeyCode.F2", vfxWorkbench);
    }

    [Test]
    public void DebugShortcutHelpPanel_ListsKnownConvenienceShortcuts()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/Debug/DebugShortcutHelpPanel.cs");

        StringAssert.Contains("전체 진행 초기화", source);
        StringAssert.Contains("퀘스트 초기화", source);
        StringAssert.Contains("런타임 데이터 로그 출력", source);
        StringAssert.Contains("로비", source);
        StringAssert.Contains("전투", source);
        StringAssert.Contains("VFX", source);
    }

    [Test]
    public void BootstrapScene_ContainsPlacedDebugShortcutHelpPanel()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Bootstrap.unity");

        StringAssert.Contains("m_Name: DebugShortcutHelpPanel", scene);
        StringAssert.Contains("m_Name: DebugShortcutHelpCanvas", scene);
        StringAssert.Contains("m_Name: DebugShortcutHelpContent", scene);
    }
}
