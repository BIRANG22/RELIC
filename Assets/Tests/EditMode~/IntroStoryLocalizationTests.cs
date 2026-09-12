using System.IO;
using NUnit.Framework;

public sealed class IntroStoryLocalizationTests
{
    private const string BootstrapScenePath = "Assets/Project/Scenes/YDM/Bootstrap.unity";
    private const string SharedTablePath = "Assets/Language/Text Shared Data.asset";
    private const string KoreanTablePath = "Assets/Language/Text_ko.asset";

    [Test]
    public void IntroStory_UsesStableKeysAndKoreanEntries()
    {
        string scene = File.ReadAllText(BootstrapScenePath);
        string sharedTable = File.ReadAllText(SharedTablePath);
        string koreanTable = File.ReadAllText(KoreanTablePath);

        for (int index = 1; index <= 9; index++)
        {
            string key = $"intro.story.{index:00}";
            string id = (11012039836704775L + index).ToString();
            StringAssert.Contains("- " + key, scene);
            StringAssert.Contains("m_Key: " + key, sharedTable);
            StringAssert.Contains("m_Id: " + id, koreanTable);
        }
    }

    [Test]
    public void IntroText_IsDynamicLocalizationOutput()
    {
        string scene = File.ReadAllText(BootstrapScenePath);
        string source = File.ReadAllText("Assets/Project/Scripts/IntroSequenceController.cs");

        StringAssert.Contains("guid: 426c88f35f804f14a7d43a9f9611e21b", scene);
        StringAssert.Contains("return GameLocalization.Get(introLines[index] ?? string.Empty);", source);
        StringAssert.Contains("LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;", source);
    }
}
