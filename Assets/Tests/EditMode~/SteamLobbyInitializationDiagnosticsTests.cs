using NUnit.Framework;

public class SteamLobbyInitializationDiagnosticsTests
{
    [Test]
    public void BuildSteamInitFailureMessage_IncludesSteamStateAndExpectedAppIdPath()
    {
        string message = SteamLobbyInviteController.BuildSteamInitFailureMessage(
            true,
            @"C:\BuildTest\steam_appid.txt",
            false);

        StringAssert.Contains("Steam running: True", message);
        StringAssert.Contains(@"C:\BuildTest\steam_appid.txt", message);
        StringAssert.Contains("App ID file exists: False", message);
    }
}
