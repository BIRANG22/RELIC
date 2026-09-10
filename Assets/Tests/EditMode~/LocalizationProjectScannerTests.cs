using NUnit.Framework;

public class LocalizationProjectScannerTests
{
    [Test]
    public void BindingResolver_WhenSourceHasMultipleCandidates_ReturnsAmbiguousWithoutChoosingFirstKey()
    {
        var resolver = new LocalizationBindingResolver(new[]
        {
            new LocalizationBindingEntry("ui.record.title", "기록서"),
            new LocalizationBindingEntry("ui.setting.title", "설정"),
            new LocalizationBindingEntry("ui.popup.confirm", "확인"),
            new LocalizationBindingEntry("ui.common.confirm", "확인"),
        });

        LocalizationKeyResolution resolution = resolver.ResolveSource("확인");

        Assert.That(resolution.Status, Is.EqualTo(LocalizationBindingStatus.AmbiguousKey));
        Assert.That(resolution.Key, Is.Empty);
        Assert.That(resolution.Candidates, Is.EquivalentTo(new[] { "ui.popup.confirm", "ui.common.confirm" }));
    }

    [Test]
    public void BindingResolver_WhenPersistedSourceDoesNotMatchKey_ReturnsSourceMismatch()
    {
        var resolver = new LocalizationBindingResolver(new[]
        {
            new LocalizationBindingEntry("ui.record.title", "기록서"),
            new LocalizationBindingEntry("ui.setting.title", "설정"),
        });

        LocalizationBindingStatus status = resolver.Validate("기록서", "ui.setting.title");

        Assert.That(status, Is.EqualTo(LocalizationBindingStatus.SourceMismatch));
    }

    [Test]
    public void BuildSuggestedKey_UsesStableNormalizedSourceAndObjectNames()
    {
        string result = LocalizationProjectScanner.BuildSuggestedKey(
            "Assets/Project/PrefabsR/Battle/Skill Panel.prefab",
            "Attack Button");

        Assert.That(result, Is.EqualTo("ui.battle.skill_panel.attack_button"));
    }

    [TestCase("100")]
    [TestCase("50%")]
    [TestCase("+25")]
    [TestCase("LV. 3")]
    public void IsLocalizableKoreanText_WhenDynamicOnly_ReturnsFalse(string value)
    {
        Assert.That(LocalizationProjectScanner.IsLocalizableKoreanText(value), Is.False);
    }

    [Test]
    public void IsLocalizableKoreanText_WhenKoreanLabelWithValue_ReturnsTrue()
    {
        Assert.That(LocalizationProjectScanner.IsLocalizableKoreanText("피해: 100"), Is.True);
    }

    [Test]
    public void RemoveComments_DoesNotReturnKoreanFromComments_ButPreservesStringLiteral()
    {
        string result = LocalizationProjectScanner.RemoveComments(
            "// 주석 텍스트\nvar label = \"표시 텍스트\"; /* 블록 주석 */");

        Assert.That(result, Does.Not.Contain("주석 텍스트"));
        Assert.That(result, Does.Not.Contain("블록 주석"));
        Assert.That(result, Does.Contain("표시 텍스트"));
    }

    [Test]
    public void ScanSummary_DeduplicatesNewEntriesAndExcludesReviewCandidates()
    {
        var summary = LocalizationScanSummary.Create(new[]
        {
            new LocalizationCandidate("A.cs", "공격", "ui.attack", false, true),
            new LocalizationCandidate("B.cs", "공격", "ui.attack", false, true),
            new LocalizationCandidate("C.cs", "개발 문구", "ui.debug", false, true, true),
            new LocalizationCandidate("D.prefab", "확인", "common.confirm", false, false),
        });

        Assert.That(summary.NewCount, Is.EqualTo(1));
        Assert.That(summary.ExistingCount, Is.EqualTo(1));
        Assert.That(summary.ReviewCount, Is.EqualTo(1));
        Assert.That(summary.OccurrenceCount, Is.EqualTo(4));
    }

    [Test]
    public void BuildSuggestedKey_WithDifferentSources_ProducesDifferentStableKeys()
    {
        string attack = LocalizationProjectScanner.BuildSuggestedKey(
            "Assets/Project/Scenes/YDM/Battle.unity", "text", "공격");
        string confirm = LocalizationProjectScanner.BuildSuggestedKey(
            "Assets/Project/Scenes/YDM/Battle.unity", "text", "확인");

        Assert.That(attack, Is.Not.EqualTo(confirm));
        Assert.That(attack, Does.StartWith("ui.battle.text."));
    }
}
