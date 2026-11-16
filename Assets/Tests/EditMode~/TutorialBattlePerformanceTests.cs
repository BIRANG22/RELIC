using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class TutorialBattlePerformanceTests
{
    [Test]
    public void ButtonRefresh_OnlyWhenStageChanges()
    {
        Assert.That(TutorialPresentationRefreshPolicy.ShouldRefreshButtons(int.MinValue, 4), Is.True);
        Assert.That(TutorialPresentationRefreshPolicy.ShouldRefreshButtons(4, 4), Is.False);
        Assert.That(TutorialPresentationRefreshPolicy.ShouldRefreshButtons(4, 5), Is.True);
    }

    [Test]
    public void TooltipRefresh_OnlyWhenScreenSizeChanges()
    {
        Assert.That(TutorialPresentationRefreshPolicy.ShouldRefreshTooltip(false, -1, -1, 1920, 1080), Is.True);
        Assert.That(TutorialPresentationRefreshPolicy.ShouldRefreshTooltip(false, 1920, 1080, 1920, 1080), Is.False);
        Assert.That(TutorialPresentationRefreshPolicy.ShouldRefreshTooltip(false, 1920, 1080, 2560, 1440), Is.True);
    }

    [Test]
    public void TooltipRefresh_WhenContentIsDirtyAtSameResolution()
    {
        Assert.That(TutorialPresentationRefreshPolicy.ShouldRefreshTooltip(true, 1920, 1080, 1920, 1080), Is.True);
    }

    [Test]
    public void TutorialDatabase_HasForMap_DoesNotRequireMaterializedResult()
    {
        TutorialDatabase database = new TutorialDatabase();
        database.Initialize(new[]
        {
            new TutorialData { TutorialId = "Tutorial_01", Order = 1, MapId = "Map_26" },
            new TutorialData { TutorialId = "Tutorial_03", Order = 1, MapId = "Map_27" }
        });

        Assert.That(database.HasForMap("Map_27"), Is.True);
        Assert.That(database.HasForMap("Map_99"), Is.False);
        Assert.That(database.HasForMap(null), Is.False);
    }
}

public sealed class TutorialFocusTargetCacheTests
{
    private GameObject root;

    [TearDown]
    public void TearDown()
    {
        if (root != null)
            Object.DestroyImmediate(root);
    }

    [Test]
    public void NeedsRefresh_ReusesUnchangedHierarchy_AndDetectsChildChange()
    {
        root = new GameObject("Target", typeof(RectTransform));
        TutorialFocusTargetCache cache = new TutorialFocusTargetCache();
        Assert.That(cache.NeedsRefresh(root.transform), Is.True);
        cache.Capture(root.transform);
        Assert.That(cache.NeedsRefresh(root.transform), Is.False);
        new GameObject("Child", typeof(RectTransform)).transform.SetParent(root.transform, false);
        Assert.That(cache.NeedsRefresh(root.transform), Is.True);
    }
}
