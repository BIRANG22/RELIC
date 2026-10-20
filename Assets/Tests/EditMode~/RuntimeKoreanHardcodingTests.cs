using System.IO;
using NUnit.Framework;

public sealed class RuntimeKoreanHardcodingTests
{
    [Test]
    public void RecordPanel_UndiscoveredLabelUsesLocalizationKey()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/RecordPanelUI.cs");

        StringAssert.Contains("LocalizationKeys.Record.Undiscovered", source);
        StringAssert.DoesNotContain("\"미발견\"", source);
    }
}
