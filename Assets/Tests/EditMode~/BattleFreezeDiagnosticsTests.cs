using NUnit.Framework;

public sealed class BattleFreezeDiagnosticsTests
{
    [Test]
    public void FormatState_IncludesInputPauseSelectionAndExecutionContext()
    {
        string message = BattleFreezeDiagnostics.FormatState(
            "MonsterInfo.Close",
            42,
            12.5f,
            0f,
            true,
            true,
            false,
            "RuntimeMonster_0001",
            "input=Escape");

        Assert.That(message, Does.Contain("source=MonsterInfo.Close"));
        Assert.That(message, Does.Contain("frame=42"));
        Assert.That(message, Does.Contain("unscaled=12.500"));
        Assert.That(message, Does.Contain("timeScale=0.000"));
        Assert.That(message, Does.Contain("menuOpen=True"));
        Assert.That(message, Does.Contain("executing=True"));
        Assert.That(message, Does.Contain("canInput=False"));
        Assert.That(message, Does.Contain("monster=RuntimeMonster_0001"));
        Assert.That(message, Does.EndWith("detail=input=Escape"));
    }
}
