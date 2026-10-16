using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

public sealed class DarkPanelTransformOverscanTests
{
    [TestCase("Assets/Project/PrefabsR/MenuPanel.prefab", "1675059425")]
    [TestCase("Assets/Project/PrefabsR/Equip_panel.prefab", "4707039292615324943")]
    [TestCase("Assets/Project/Scenes/YDM/Title.unity", "1778834499")]
    [TestCase("Assets/Project/Scenes/YDM/Lobby.unity", "1832086189")]
    [TestCase("Assets/Project/Scenes/YDM/Battle.unity", "2023796364")]
    [TestCase("Assets/Project/Scenes/YDM/Battle.unity", "5200133220976910175")]
    [TestCase("Assets/Project/Scenes/YDM/DebugBattle.unity", "441877298")]
    [TestCase("Assets/Project/Scenes/YDH/Art Test Scene.unity", "1021848144")]
    public void DarkPanelRectTransform_IsActuallyScaledUp(string assetPath, string rectTransformFileId)
    {
        string yaml = File.ReadAllText(assetPath);
        Match rectTransform = Regex.Match(
            yaml,
            $@"--- !u!224 &{rectTransformFileId}\r?\nRectTransform:\r?\n(?<body>.*?)(?=\r?\n--- !u!)",
            RegexOptions.Singleline);

        Assert.That(rectTransform.Success, Is.True, $"RectTransform {rectTransformFileId} not found in {assetPath}");
        StringAssert.Contains("m_LocalScale: {x: 1.01, y: 1.01, z: 1}", rectTransform.Groups["body"].Value);
    }
}
