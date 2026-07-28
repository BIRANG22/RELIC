public sealed class LobbyBattleStartCommand
{
    public string RequestId { get; }
    public ulong HostSteamId { get; }
    public long RequiredSharedStateRevision { get; }
    public string BattleSessionId { get; }
    public int BattleSeed { get; }
    public string ChapterId { get; }
    public string StageId { get; }

    public LobbyBattleStartCommand(
        string requestId,
        ulong hostSteamId,
        long requiredSharedStateRevision,
        string battleSessionId,
        int battleSeed,
        string chapterId,
        string stageId)
    {
        RequestId = requestId ?? string.Empty;
        HostSteamId = hostSteamId;
        RequiredSharedStateRevision = requiredSharedStateRevision;
        BattleSessionId = battleSessionId ?? string.Empty;
        BattleSeed = battleSeed;
        ChapterId = chapterId ?? string.Empty;
        StageId = stageId ?? string.Empty;
    }
}
