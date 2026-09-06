using NUnit.Framework;

public class LobbyPartyClientCommandPipelineTests
{
    [Test]
    public void MultipleSentCommands_RemainPendingUntilEachAcceptedRevisionIsApplied()
    {
        LobbyPartyClientCommandPipeline pipeline = new LobbyPartyClientCommandPipeline();

        pipeline.TrackSentCommand(Command("request-1"));
        pipeline.TrackSentCommand(Command("request-2"));
        pipeline.TrackSentCommand(Command("request-3"));

        Assert.That(pipeline.PendingCommandCount, Is.EqualTo(3));

        pipeline.MarkHostResponse(Response("request-1", true, 5));
        pipeline.MarkHostResponse(Response("request-2", true, 6));
        pipeline.MarkHostResponse(Response("request-3", true, 7));

        pipeline.RemoveAcceptedThroughRevision(5);

        Assert.That(pipeline.PendingCommandCount, Is.EqualTo(2));

        pipeline.RemoveAcceptedThroughRevision(7);

        Assert.That(pipeline.HasPendingCommands, Is.False);
    }

    [Test]
    public void RejectedCommand_ClearsAllPendingCommands()
    {
        LobbyPartyClientCommandPipeline pipeline = new LobbyPartyClientCommandPipeline();

        pipeline.TrackSentCommand(Command("request-1"));
        pipeline.TrackSentCommand(Command("request-2"));

        bool matched = pipeline.MarkHostResponse(Response("request-1", false, 0));

        Assert.That(matched, Is.True);
        Assert.That(pipeline.HasPendingCommands, Is.False);
    }

    private static LobbyPartyCharacterChangeCommand Command(string requestId)
    {
        return new LobbyPartyCharacterChangeCommand(
            requestId,
            200UL,
            2,
            "Character_A",
            1);
    }

    private static LobbyPartyCommandResponse Response(
        string requestId,
        bool accepted,
        long revision)
    {
        return new LobbyPartyCommandResponse(
            requestId,
            200UL,
            accepted,
            accepted
                ? LobbyPartyCommandRejectReason.None
                : LobbyPartyCommandRejectReason.InvalidCharacter,
            revision);
    }
}
