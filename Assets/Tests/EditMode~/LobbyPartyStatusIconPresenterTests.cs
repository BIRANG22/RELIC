using System;
using System.IO;
using NUnit.Framework;

public class LobbyPartyStatusIconPresenterTests
{
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";
    private const string PresenterPath =
        "Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyPartyStatusIconPresenter.cs";
    private const string PresenterGuid = "fa4fd626e6264e26983455d2ca4de529";

    [Test]
    public void Presenter_ReadsPartyRuntimeStoreAndDisplaysCharacterIcons()
    {
        string source = File.ReadAllText(PresenterPath);

        Assert.That(source, Does.Contain("Image[] partyIconImages"));
        Assert.That(source, Does.Contain("PartyRuntimeStore"));
        Assert.That(source, Does.Contain("CharacterDatabase.TryGet"));
        Assert.That(source, Does.Contain("master.Icon"));
        Assert.That(source, Does.Contain("CharacterIconDatabase.TryGetIcon"));
        Assert.That(source, Does.Contain("LateUpdate"));
    }

    [Test]
    public void LobbyScene_MovesCharacterSettingOpenFromAnchorToSettingButton()
    {
        string scene = File.ReadAllText(LobbyScenePath);
        string anchorTransition = GetYamlObjectBlock(scene, "--- !u!114 &1325759450");
        string settingGameObject = GetYamlObjectBlock(scene, "--- !u!1 &4650064");
        string settingButton = GetYamlObjectBlock(scene, "--- !u!114 &4650068");
        string settingTransition = GetYamlObjectBlock(scene, "--- !u!114 &4650069");

        Assert.That(anchorTransition, Does.Contain("panelToOpen: {fileID: 0}"));
        Assert.That(anchorTransition, Does.Contain("executeOnWorldClick: 0"));
        Assert.That(anchorTransition, Does.Not.Contain("panelToOpen: {fileID: 1880677614}"));

        Assert.That(settingGameObject, Does.Contain("- component: {fileID: 4650068}"));
        Assert.That(settingGameObject, Does.Contain("- component: {fileID: 4650069}"));
        Assert.That(settingButton, Does.Contain("m_Target: {fileID: 4650069}"));
        Assert.That(settingButton, Does.Contain("m_MethodName: Execute"));
        Assert.That(settingTransition, Does.Contain("panelToOpen: {fileID: 1880677614}"));
        Assert.That(settingTransition, Does.Contain("executeOnWorldClick: 0"));
        Assert.That(settingTransition, Does.Contain("lobbyPanelTransition: {fileID: 1512315629}"));
    }

    [Test]
    public void LobbyScene_DisablesAnchorHoverTooltip()
    {
        string scene = File.ReadAllText(LobbyScenePath);
        string anchorTooltip = GetYamlObjectBlock(scene, "--- !u!114 &1325759452");

        Assert.That(anchorTooltip, Does.Contain("m_Enabled: 0"));
        Assert.That(anchorTooltip, Does.Contain("tooltipText:"));
        Assert.That(anchorTooltip, Does.Not.Contain("\\uD3B8\\uC131"));
    }

    [Test]
    public void LobbyScene_WiresPartyStatusIconsToCharacterSettingSlots()
    {
        string scene = File.ReadAllText(LobbyScenePath);
        string characterSettingGameObject = GetYamlObjectBlock(scene, "--- !u!1 &1827125532");
        string presenter = GetYamlObjectBlock(scene, "--- !u!114 &1827125534");

        Assert.That(characterSettingGameObject, Does.Contain("- component: {fileID: 1827125534}"));
        Assert.That(presenter, Does.Contain($"guid: {PresenterGuid}"));
        Assert.That(presenter, Does.Contain("partyIconImages:"));
        Assert.That(presenter, Does.Contain("- {fileID: 361593230}"));
        Assert.That(presenter, Does.Contain("- {fileID: 1668117575}"));
        Assert.That(presenter, Does.Contain("- {fileID: 221878656}"));
    }

    private static string GetYamlObjectBlock(string yaml, string blockHeader)
    {
        int start = yaml.IndexOf(blockHeader, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"Missing block: {blockHeader}");

        int next = yaml.IndexOf("\n--- !u!", start + blockHeader.Length, StringComparison.Ordinal);
        return next < 0 ? yaml.Substring(start) : yaml.Substring(start, next - start);
    }
}
