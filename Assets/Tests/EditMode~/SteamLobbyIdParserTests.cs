using NUnit.Framework;

public class SteamLobbyIdParserTests
{
    [TestCase(" 109775244533745760 ", 109775244533745760UL)]
    public void TryParse_ValidDecimalLobbyId_ReturnsValue(string input, ulong expected)
    {
        bool parsed = SteamLobbyIdParser.TryParse(input, out ulong value, out string error);

        Assert.That(parsed, Is.True);
        Assert.That(value, Is.EqualTo(expected));
        Assert.That(error, Is.Empty);
    }

    [TestCase(null, "Lobby ID is empty.")]
    [TestCase("", "Lobby ID is empty.")]
    [TestCase("abc", "Lobby ID must be a positive decimal number.")]
    [TestCase("0", "Lobby ID must be greater than zero.")]
    public void TryParse_InvalidLobbyId_ReturnsSpecificError(string input, string expectedError)
    {
        bool parsed = SteamLobbyIdParser.TryParse(input, out ulong value, out string error);

        Assert.That(parsed, Is.False);
        Assert.That(value, Is.Zero);
        Assert.That(error, Is.EqualTo(expectedError));
    }
}
