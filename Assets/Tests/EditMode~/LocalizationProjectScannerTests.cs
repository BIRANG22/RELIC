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
}
