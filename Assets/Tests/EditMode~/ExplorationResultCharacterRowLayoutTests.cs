using System.IO;
using NUnit.Framework;

public sealed class ExplorationResultCharacterRowLayoutTests
{
    [Test]
    public void BattleScene_HasVisibleInformationIconsAndOnePanelLevelTooltipOverlay()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        Assert.That(Count(scene, "m_Name: ExplorationReportInformationIcon"), Is.EqualTo(3));
        Assert.That(Count(scene, "m_Name: ExplorationReportStatisticsOverlay"), Is.EqualTo(1));
        Assert.That(Count(scene, "statisticsOverlayRoot: {fileID:"), Is.EqualTo(1));
        Assert.That(Count(scene, "statisticsTooltipRoot: {fileID:"), Is.EqualTo(0));
        Assert.That(scene, Does.Contain(
            "m_Sprite: {fileID: 21300000, guid: b3c1b8145715e654baaaba25ffe89195, type: 3}"));
        Assert.That(Count(scene, "informationOwner: {fileID:"), Is.EqualTo(3));
    }

    [Test]
    public void InformationIconHover_DoesNotRepeatScreenPointHitTesting()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultCharacterRowUI.cs");
        string method = ExtractMethod(source, "public void OnPointerEnter(");

        Assert.That(method, Does.Contain("ShowStatisticsOverlay"));
        Assert.That(method, Does.Not.Contain("RectangleContainsScreenPoint"));
    }

    [Test]
    public void CharacterRow_UsesLargerSkillSlotsThanRelicSlots()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultCharacterRowUI.cs");

        Assert.That(source, Does.Contain("skillSlotScale"));
        Assert.That(source, Does.Contain("relicSlotScale"));
        Assert.That(source, Does.Contain("skillSlotHorizontalSpacing"));
        Assert.That(source, Does.Contain("relicSlotHorizontalSpacing"));
        Assert.That(source, Does.Not.Contain("statisticsTooltipRoot"));
        Assert.That(source, Does.Contain("statisticsOverlayController"));
        Assert.That(source, Does.Contain("informationIcon"));
    }

    [Test]
    public void BattleScene_AlignsRelicsAndMemoriesInOneEquipmentRow()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        Assert.That(Count(scene, "m_Name: ExplorationReportRelicSlots"), Is.EqualTo(3));
        Assert.That(Count(scene, "m_Name: ExplorationReportMemorySlots"), Is.EqualTo(3));
        Assert.That(Count(scene, "maxRelicSlots: 6"), Is.EqualTo(3));
        Assert.That(Count(scene, "maxSkillSlots: 4"), Is.EqualTo(3));
        Assert.That(Count(scene, "relicSlotColumns: 6"), Is.EqualTo(3));
        Assert.That(Count(scene, "skillSlotColumns: 4"), Is.EqualTo(3));
    }

    [Test]
    public void CharacterRow_DoesNotResolveRelicsOrSkillsAsBagItems()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultCharacterRowUI.cs");
        string method = ExtractMethod(source, "private void BindIconSlots(");

        Assert.That(method, Does.Not.Contain("slot.Setup("));
    }

    private static int Count(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature, System.StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"Missing method: {signature}");

        int nextMethod = source.IndexOf("\n    private ", start + signature.Length, System.StringComparison.Ordinal);
        Assert.That(nextMethod, Is.GreaterThan(start));
        return source.Substring(start, nextMethod - start);
    }
}
