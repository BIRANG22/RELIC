using System;
using System.IO;
using NUnit.Framework;

public class ArabellaHuntingLeapIdleVfxTests
{
    [Test]
    public void ExecuteArabellaLeap_SuspendsIdleVfxAndRestoresItInFinally()
    {
        const string path =
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs";
        string source = File.ReadAllText(path);
        int methodStart = source.IndexOf(
            "private IEnumerator ExecuteArabellaLeap(MonsterReservedCommand command)",
            StringComparison.Ordinal);
        int methodEnd = source.IndexOf("private ", methodStart + 1, StringComparison.Ordinal);
        string methodSource = source.Substring(methodStart, methodEnd - methodStart);

        int suspend = methodSource.IndexOf("SuspendPersistentIdleVfx()", StringComparison.Ordinal);
        int tryBlock = methodSource.IndexOf("try", suspend, StringComparison.Ordinal);
        int leap = methodSource.IndexOf("yield return AnimateArabellaHuntingLeap(", tryBlock, StringComparison.Ordinal);
        int finallyBlock = methodSource.IndexOf("finally", leap, StringComparison.Ordinal);
        int resume = methodSource.IndexOf("ResumePersistentIdleVfx()", finallyBlock, StringComparison.Ordinal);

        Assert.That(suspend, Is.GreaterThanOrEqualTo(0));
        Assert.That(tryBlock, Is.GreaterThan(suspend));
        Assert.That(leap, Is.GreaterThan(tryBlock));
        Assert.That(finallyBlock, Is.GreaterThan(leap));
        Assert.That(resume, Is.GreaterThan(finallyBlock));
    }
}
