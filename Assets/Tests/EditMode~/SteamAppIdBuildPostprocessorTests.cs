using NUnit.Framework;
using UnityEditor;

public class SteamAppIdBuildPostprocessorTests
{
    [Test]
    public void GetDestinationPath_WindowsExe_ReturnsSiblingSteamAppId()
    {
        string result = SteamAppIdBuildPostprocessor.GetDestinationPath(
            @"C:\Builds\RELIC\DUSTIUM.exe");

        Assert.That(result, Is.EqualTo(@"C:\Builds\RELIC\steam_appid.txt"));
    }

    [TestCase(BuildTarget.StandaloneWindows, true)]
    [TestCase(BuildTarget.StandaloneWindows64, true)]
    [TestCase(BuildTarget.StandaloneLinux64, false)]
    public void IsSupportedTarget_ReturnsExpected(BuildTarget target, bool expected)
    {
        Assert.That(SteamAppIdBuildPostprocessor.IsSupportedTarget(target), Is.EqualTo(expected));
    }
}
