using NUnit.Framework;

public sealed class DynamicLocalizationSourceStateTests
{
    [Test]
    public void UpdateSource_StoresUniqueKeyForLocaleRefresh()
    {
        var resolver = new LocalizationBindingResolver(new[]
        {
            new LocalizationBindingEntry("battle.warning.not_enough", "자원이 부족합니다."),
        });
        var state = new DynamicLocalizationSourceState(resolver);

        bool resolved = state.UpdateSource("  자원이 부족합니다.\r\n");

        Assert.That(resolved, Is.True);
        Assert.That(state.LocalizationKey, Is.EqualTo("battle.warning.not_enough"));
        Assert.That(state.KoreanSource, Is.EqualTo("자원이 부족합니다."));
    }

    [Test]
    public void UpdateSource_DoesNotSelectAmbiguousKoreanText()
    {
        var resolver = new LocalizationBindingResolver(new[]
        {
            new LocalizationBindingEntry("first", "전투 시작"),
            new LocalizationBindingEntry("second", "전투 시작"),
        });
        var state = new DynamicLocalizationSourceState(resolver);

        bool resolved = state.UpdateSource("전투 시작");

        Assert.That(resolved, Is.False);
        Assert.That(state.LocalizationKey, Is.Empty);
    }
}
