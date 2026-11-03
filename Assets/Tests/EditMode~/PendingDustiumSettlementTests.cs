using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class PendingDustiumSettlementTests
{
    [Test]
    public void ApplyOnceWithResult_UsesRemnantOneToOneAndTransfersBagItems()
    {
        LobbyRuntimeData lobby = CreateLobby(redDustium: 7, oldTotalBlue: 999);
        lobby.BlueDustium = 13;
        lobby.PendingResearchResult.ExplorationResult.BagItemIds.Add("Item_A");

        PendingResearchSettlementResult result =
            PendingResearchSettlementService.ApplyOnceWithResult(lobby);

        Assert.That(result.Applied, Is.True);
        Assert.That(result.BlueBefore, Is.EqualTo(13));
        Assert.That(result.BlueAfter, Is.EqualTo(20));
        Assert.That(result.ConvertedRedDustium, Is.EqualTo(7));
        Assert.That(lobby.BlueDustium, Is.EqualTo(20));
        Assert.That(lobby.BagItemIds, Does.Contain("Item_A"));
    }

    [Test]
    public void ApplyOnceWithResult_DoesNotApplySamePendingTwice()
    {
        LobbyRuntimeData lobby = CreateLobby(redDustium: 5, oldTotalBlue: 999);

        PendingResearchSettlementService.ApplyOnceWithResult(lobby);
        PendingResearchSettlementResult second =
            PendingResearchSettlementService.ApplyOnceWithResult(lobby);

        Assert.That(second.Applied, Is.False);
        Assert.That(lobby.BlueDustium, Is.EqualTo(5));
    }

    [Test]
    public void ApplyOnceWithResult_TransfersBagItemsWhenRedDustiumIsZero()
    {
        LobbyRuntimeData lobby = CreateLobby(redDustium: 0, oldTotalBlue: 999);
        lobby.PendingResearchResult.ExplorationResult.BagItemIds.Add("Item_Zero");

        PendingResearchSettlementResult result =
            PendingResearchSettlementService.ApplyOnceWithResult(lobby);

        Assert.That(result.Applied, Is.True);
        Assert.That(result.ConvertedRedDustium, Is.Zero);
        Assert.That(lobby.BlueDustium, Is.Zero);
        Assert.That(lobby.BagItemIds, Does.Contain("Item_Zero"));
    }

    private static LobbyRuntimeData CreateLobby(int redDustium, int oldTotalBlue)
    {
        return new LobbyRuntimeData
        {
            BagItemIds = new List<string>(),
            HasPendingResearchResult = true,
            PendingResearchResult = new PendingResearchResultData
            {
                ExplorationResult = new ExplorationResultData { Remnant = redDustium },
                TotalBlue = oldTotalBlue
            }
        };
    }
}
