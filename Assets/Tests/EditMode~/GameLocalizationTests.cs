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
}
