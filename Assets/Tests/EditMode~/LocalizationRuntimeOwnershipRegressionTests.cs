using System.IO;
using NUnit.Framework;

public sealed class LocalizationRuntimeOwnershipRegressionTests
{
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";
    private const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";

    [Test]
    public void LobbyShortcutLabels_UseStableLocalizationKeys()
    {
        string scene = File.ReadAllText(LobbyScenePath);

        StringAssert.Contains("localizationKey: ui.lobby.panel.erosion", scene);
        StringAssert.Contains("localizationKey: ui.lobby.panel.resonance", scene);
    }

    [Test]
    public void BlueDustiumValue_IsOwnedByNumericPresenter()
    {
        string scene = File.ReadAllText(LobbyScenePath);

        StringAssert.DoesNotContain(
            "target: {fileID: 557578910}\n  localizationKey: data.monster.mon_07.special_action_1",
            scene.Replace("\r\n", "\n"));
    }

    [Test]
    public void EventChoiceRuntimeTexts_DoNotKeepStaticAutoLocalizers()
    {
        string scene = File.ReadAllText(BattleScenePath).Replace("\r\n", "\n");

        StringAssert.DoesNotContain("target: {fileID: 31716066}\n  localizationKey: ui.battle.text.891c006d", scene);
        StringAssert.DoesNotContain("target: {fileID: 153332011}\n  localizationKey: ui.battle.text.561ed27b", scene);
    }
}
