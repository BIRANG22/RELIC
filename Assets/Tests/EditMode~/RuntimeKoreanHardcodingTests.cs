using System.IO;
using System.Linq;
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

    [Test]
    public void PlayerFacingBattlePresenters_DoNotWriteKoreanLiteralsDirectly()
    {
        string[] paths =
        {
            "Assets/Project/Scripts/BattleCharacterPanelUI.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventRoomController.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventChoiceExecutionService.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs",
        };

        string[] findings = paths
            .SelectMany(path => LocalizationProjectScanner
                .FindDynamicDisplayLiterals(LocalizationProjectScanner.ReadScriptText(path))
                .Select(value => path + ": " + value))
            .ToArray();

        Assert.That(findings, Is.Empty, string.Join("\n", findings));
    }
}
