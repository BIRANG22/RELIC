using System.IO;
using NUnit.Framework;

public class StartRoomActiveRelicConfigurationTests
{
    private const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";

    [Test]
    public void StartRoomRelicRangesCoverAllFiveActiveRelics()
    {
        string sceneText = File.ReadAllText(BattleScenePath).Replace("\r\n", "\n");

        Assert.That(sceneText, Does.Not.Contain("minRelicNumber: 1\n  maxRelicNumber: 10"));
        Assert.That(
            CountOccurrences(sceneText, "minRelicNumber: 11\n  maxRelicNumber: 15"),
            Is.EqualTo(2));
    }

    [Test]
    public void FirstRelicSlotImagesUseActiveOrangeColor()
    {
        string sceneText = File.ReadAllText(BattleScenePath);
        const string activeOrange =
            "m_Color: {r: 1, g: 0.6392157, b: 0.24313726, a: 1}";
        const string activeNormalBorder =
            "normalBorderColor: {r: 1, g: 0.6392157, b: 0.24313726, a: 1}";

        Assert.That(CountOccurrences(sceneText, activeOrange), Is.EqualTo(3));
        for (int partySlotIndex = 0; partySlotIndex < 3; partySlotIndex++)
        {
            string activeSlotBlock =
                $"partySlotIndex: {partySlotIndex}\n  relicSlotIndex: 0";
            int blockStart = sceneText.IndexOf(
                activeSlotBlock,
                System.StringComparison.Ordinal);

            Assert.That(blockStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(
                sceneText.IndexOf(
                    activeNormalBorder,
                    blockStart,
                    System.StringComparison.Ordinal),
                Is.InRange(blockStart, blockStart + 500));
        }
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;

        while ((index = text.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
