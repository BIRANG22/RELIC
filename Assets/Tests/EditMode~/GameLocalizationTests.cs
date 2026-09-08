using NUnit.Framework;

public class GameLocalizationTests
{
    [TestCase("SkillMaster", "SKILL 001-A", "EffectDesc", "data.skill_master.skill_001_a.effect_desc")]
    [TestCase("Monster", "M_Boss-01", "Name", "data.monster.m_boss_01.name")]
    public void BuildDataKey_NormalizesStableSegments(
        string category,
        string stableId,
        string field,
        string expected)
    {
        Assert.That(GameLocalization.BuildDataKey(category, stableId, field), Is.EqualTo(expected));
    }

    [Test]
    public void Format_WithEmptyKey_FormatsFallback()
    {
        Assert.That(
            GameLocalization.Format(string.Empty, "최대 {0}개", 5),
            Is.EqualTo("최대 5개"));
    }

    [Test]
    public void Get_WithEmptyKey_ReturnsFallback()
    {
        Assert.That(GameLocalization.Get(string.Empty, "fallback"), Is.EqualTo("fallback"));
    }

    [Test]
    public void ResolveMissingTranslation_UsesShortLabelForSelectedLocale()
    {
        Assert.That(GameLocalization.ResolveMissingTranslation("ko"), Is.EqualTo("미번역"));
        Assert.That(GameLocalization.ResolveMissingTranslation("en"), Is.EqualTo("Untranslated"));
        Assert.That(GameLocalization.ResolveMissingTranslation("ja"), Is.EqualTo("未翻訳"));
        Assert.That(GameLocalization.ResolveMissingTranslation("zh-Hans"), Is.EqualTo("未翻译"));
        Assert.That(GameLocalization.ResolveMissingTranslation("es"), Is.EqualTo("Sin traducir"));
    }

    [Test]
    public void ResolveMissingTranslation_PreservesKoreanSourceForKoreanLocale()
    {
        Assert.That(
            GameLocalization.ResolveMissingTranslation("ko", "타이틀로"),
            Is.EqualTo("타이틀로"));
        Assert.That(
            GameLocalization.ResolveMissingTranslation("en", "타이틀로"),
            Is.EqualTo("Untranslated"));
    }

    [Test]
    public void BuildGameDataKey_UsesStableDataIdAndPresentationField()
    {
        Assert.That(
            LocalizationProjectScanner.BuildGameDataKey("Item", "I_001", "설명"),
            Is.EqualTo("data.item.i_001.description"));
    }

    [Test]
    public void BuildGameDataKey_UsesEffectDescriptionFieldUsedByRuntimeDataLookup()
    {
        Assert.That(
            LocalizationProjectScanner.BuildGameDataKey("Relic", "relic_p_27", "효과설명"),
            Is.EqualTo("data.relic.relic_p_27.effect_description"));
    }

    [Test]
    public void BuildUniqueGameDataKey_AddsSourceSuffixForRepeatedDataFields()
    {
        Assert.That(
            LocalizationProjectScanner.BuildUniqueGameDataKey("Event", "E_001", "선택지내용", "첫 번째 선택지"),
            Does.StartWith("data.event.e_001.choice_description."));
    }

    [Test]
    public void IsPlayerFacingGameDataColumn_RecognizesNameAndDescriptionColumns()
    {
        Assert.That(LocalizationProjectScanner.IsPlayerFacingGameDataColumn("이름"), Is.True);
        Assert.That(LocalizationProjectScanner.IsPlayerFacingGameDataColumn("효과설명"), Is.True);
        Assert.That(LocalizationProjectScanner.IsPlayerFacingGameDataColumn("최대 HP"), Is.False);
    }

    [Test]
    public void IsPlayerFacingGameDataColumn_RecognizesRarityColumn()
    {
        Assert.That(LocalizationProjectScanner.IsPlayerFacingGameDataColumn("레어도"), Is.True);
    }

    [Test]
    public void NormalizeRuntimeSource_RemovesIncidentalWhitespaceWithoutChangingText()
    {
        Assert.That(
            RuntimeTMPTextAutoLocalizer.NormalizeKoreanSource("  기록서\r\n"),
            Is.EqualTo("기록서"));
    }
}
