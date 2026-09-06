using System.IO;
using NUnit.Framework;

public class StartRoomActiveRelicConfigurationTests
{
    [TestCase("Assets/Project/Scenes/YDM/Battle.unity")]
    [TestCase("Assets/Project/Scenes/YDM/DebugBattle.unity")]
    public void StartRoomRelicChoicesDoNotSerializeNumberRanges(string scenePath)
    {
        string sceneText = File.ReadAllText(scenePath);

        Assert.That(sceneText, Does.Not.Contain("useRelicNumberRange:"));
        Assert.That(sceneText, Does.Not.Contain("minRelicNumber:"));
        Assert.That(sceneText, Does.Not.Contain("maxRelicNumber:"));
        Assert.That(sceneText, Does.Not.Contain("relicIdPrefix:"));
    }

    [Test]
    public void FirstRelicSlotImagesUseActiveOrangeColor()
    {
        string sceneText = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity");
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
