using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class StartRoomRelicSelectionUtilityTests
{
    [Test]
    public void CollectActiveRelicIds_IncludesEveryActivePrefixWithoutNumberRange()
    {
        List<RelicData> relics = new()
        {
            new RelicData { FragmentId = "Relic_A_01" },
            new RelicData { FragmentId = " Relic_A_15 " },
            new RelicData { FragmentId = "Relic_A_99" },
            new RelicData { FragmentId = "Relic_P_01" },
            new RelicData { FragmentId = string.Empty },
            null
        };

        List<string> result = StartRoomRelicSelectionUtility.CollectActiveRelicIds(relics);

        Assert.That(result, Is.EqualTo(new[]
        {
            "Relic_A_01",
            "Relic_A_15",
            "Relic_A_99"
        }));
    }
}
