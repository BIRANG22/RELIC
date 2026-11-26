using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class RecordMonsterDiscoveryTests
{
    [Test]
    public void Normalize_RemovesBlankAndCaseInsensitiveDuplicates()
    {
        PlayerRuntimeData player = new()
        {
            DiscoveredMonsterIds = new List<string> { " Monster_A ", "monster_a", "", null }
        };

        RecordDiscoveryService.Normalize(player);

        Assert.That(player.DiscoveredMonsterIds, Is.EqualTo(new[] { "Monster_A" }));
    }
}
