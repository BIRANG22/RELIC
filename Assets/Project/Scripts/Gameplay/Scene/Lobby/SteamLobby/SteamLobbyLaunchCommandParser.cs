using System;
using System.Collections.Generic;

public static class SteamLobbyLaunchCommandParser
{
    private const string ConnectLobbyToken = "+connect_lobby";

    public static bool TryParseLobbyId(string commandLine, out ulong lobbyId)
    {
        lobbyId = 0;

        if (string.IsNullOrWhiteSpace(commandLine))
            return false;

        List<string> tokens = Tokenize(commandLine);

        for (int i = 0; i < tokens.Count - 1; i++)
        {
            if (!string.Equals(tokens[i], ConnectLobbyToken, StringComparison.OrdinalIgnoreCase))
                continue;

            return ulong.TryParse(tokens[i + 1], out lobbyId);
        }

        return false;
    }

    private static List<string> Tokenize(string commandLine)
    {
        List<string> tokens = new List<string>();
        bool inQuote = false;
        int tokenStart = -1;

        for (int i = 0; i < commandLine.Length; i++)
        {
            char c = commandLine[i];

            if (c == '"')
            {
                if (tokenStart < 0)
                    tokenStart = i + 1;

                inQuote = !inQuote;
                continue;
            }

            if (!char.IsWhiteSpace(c) || inQuote)
            {
                if (tokenStart < 0)
                    tokenStart = i;

                continue;
            }

            AddToken(commandLine, tokenStart, i, tokens);
            tokenStart = -1;
        }

        AddToken(commandLine, tokenStart, commandLine.Length, tokens);
        return tokens;
    }

    private static void AddToken(string source, int start, int end, List<string> tokens)
    {
        if (start < 0 || end <= start)
            return;

        string token = source.Substring(start, end - start).Trim().Trim('"');

        if (!string.IsNullOrWhiteSpace(token))
            tokens.Add(token);
    }
}
