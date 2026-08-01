public sealed class DiscordPresenceSnapshot
{
    public DiscordPresenceSnapshot(string details, string state, long startUnixSeconds)
    {
        Details = details ?? string.Empty;
        State = state ?? string.Empty;
        StartUnixSeconds = startUnixSeconds;
    }

    public string Details { get; }
    public string State { get; }
    public long StartUnixSeconds { get; }
}
