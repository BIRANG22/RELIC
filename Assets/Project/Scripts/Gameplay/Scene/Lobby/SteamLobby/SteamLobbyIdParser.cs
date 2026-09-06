public static class SteamLobbyIdParser
{
    public static bool TryParse(string input, out ulong lobbyId, out string error)
    {
        lobbyId = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Lobby ID is empty.";
            return false;
        }

        if (!ulong.TryParse(input.Trim(), out lobbyId))
        {
            error = "Lobby ID must be a positive decimal number.";
            return false;
        }

        if (lobbyId == 0)
        {
            error = "Lobby ID must be greater than zero.";
            return false;
        }

        error = "";
        return true;
    }
}
