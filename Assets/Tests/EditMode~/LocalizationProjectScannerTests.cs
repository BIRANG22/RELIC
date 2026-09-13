using NUnit.Framework;

public class LocalizationProjectScannerTests
{
    [Test]
    public void DecodeUnityYamlText_DecodesEscapedKoreanText()
    {
        Assert.That(
            LocalizationProjectScanner.DecodeUnityYamlText("\\\"\\uBCF4\\uAD00\\uD568\\\""),
            Is.EqualTo("보관함"));
    }

    [Test]
    public void DecodeUnityYamlText_PreservesPlainKoreanText()
    {
        Assert.That(LocalizationProjectScanner.DecodeUnityYamlText("유물 정보"), Is.EqualTo("유물 정보"));
    }

    [Test]
    public void BuildEventChoiceKey_UsesEventIdAndChoiceOrder()
    {
        Assert.That(
            LocalizationProjectScanner.BuildEventChoiceKey("Event_02_A", 3, "선택지이름"),
            Is.EqualTo("data.event.event_02_a.choice_3_name"));
    }

    [Test]
    public void BuildEventChoiceKey_AcceptsSpacedGameDataHeader()
    {
        Assert.That(
            LocalizationProjectScanner.BuildEventChoiceKey("Event_02_A", 3, "선택지 내용"),
            Is.EqualTo("data.event.event_02_a.choice_3_description"));
    }

    [Test]
    public void BuildGameDataKey_MapsMonsterSkillTypeColumn()
    {
        Assert.That(
            LocalizationProjectScanner.BuildGameDataKey("MonsterSkill", "MS_01", "타입"),
            Is.EqualTo("data.monsterskill.ms_01.type"));
    }
}
