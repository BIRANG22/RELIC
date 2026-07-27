using NUnit.Framework;

public class SteamLobbyLaunchCommandParserTests
{
    [Test]
    public void TryParseLobbyId_ReturnsLobbyIdFromUnquotedLaunchCommand()
    {
        bool parsed = SteamLobbyLaunchCommandParser.TryParseLobbyId(
            "+connect_lobby 109775241199441234",
            out ulong lobbyId);

        Assert.That(parsed, Is.True);
        Assert.That(lobbyId, Is.EqualTo(109775241199441234UL));
    }

    [Test]
    public void TryParseLobbyId_ReturnsLobbyIdFromQuotedLaunchCommand()
    {
        bool parsed = SteamLobbyLaunchCommandParser.TryParseLobbyId(
            "\"+connect_lobby\" \"109775241199441234\"",
            out ulong lobbyId);

        Assert.That(parsed, Is.True);
        Assert.That(lobbyId, Is.EqualTo(109775241199441234UL));
    }

    [Test]
    public void TryParseLobbyId_ReturnsFalseWhenLobbyIdIsMissing()
    {
        bool parsed = SteamLobbyLaunchCommandParser.TryParseLobbyId(
            "+connect_lobby",
            out ulong lobbyId);

        Assert.That(parsed, Is.False);
        Assert.That(lobbyId, Is.Zero);
    }

    [Test]
    public void TryParseLobbyId_ReturnsFalseWhenLobbyIdIsInvalid()
    {
        bool parsed = SteamLobbyLaunchCommandParser.TryParseLobbyId(
            "+connect_lobby not-a-lobby",
            out ulong lobbyId);

        Assert.That(parsed, Is.False);
        Assert.That(lobbyId, Is.Zero);
    }
}
