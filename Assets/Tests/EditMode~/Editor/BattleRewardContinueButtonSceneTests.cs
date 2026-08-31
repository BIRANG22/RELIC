using System;
using System.IO;
using NUnit.Framework;

public sealed class BattleRewardContinueButtonSceneTests
{
    [Test]
    public void BattleResultChecker_HasNextButtonRootReference()
    {
        const string scenePath = "Assets/Project/Scenes/YDM/Battle.unity";
        string sceneText = File.ReadAllText(scenePath);
        string block = ExtractMonoBehaviourBlockByScriptGuid(sceneText, "b12eac6049579da43b5390e1fdf435c8");

        StringAssert.Contains("nextButtonRoot: {fileID: 1885927099}", block);
    }

    [Test]
    public void EventRoomController_UsesSharedRewardCanvasNextButton()
    {
        const string scenePath = "Assets/Project/Scenes/YDM/Battle.unity";
        string sceneText = File.ReadAllText(scenePath);
        string block = ExtractMonoBehaviourBlockByScriptGuid(sceneText, "810cbc2986c082345998d93e2522192b");

        StringAssert.Contains("nextButtonRoot: {fileID: 1885927099}", block);
        Assert.That(sceneText, Does.Not.Contain("152506061"));
        Assert.That(sceneText, Does.Not.Contain("152506062"));
    }

    [Test]
    public void BattleContinueButton_IsUnderBattleRewardCanvas()
    {
        const string scenePath = "Assets/Project/Scenes/YDM/Battle.unity";
        string sceneText = File.ReadAllText(scenePath);
        string buttonTransformBlock = ExtractObjectBlock(sceneText, "--- !u!224 &1885927100");
        string rewardCanvasTransformBlock = ExtractObjectBlock(sceneText, "--- !u!224 &1979314416");
        string sharedRootTransformBlock = ExtractObjectBlock(sceneText, "--- !u!4 &930000000000000001");
        string restRoomPanelBlock = ExtractObjectBlock(sceneText, "--- !u!224 &214258513");

        StringAssert.Contains("m_Father: {fileID: 1979314416}", buttonTransformBlock);
        StringAssert.Contains("- {fileID: 1885927100}", rewardCanvasTransformBlock);
        Assert.That(sharedRootTransformBlock, Does.Not.Contain("- {fileID: 1885927100}"));
        Assert.That(restRoomPanelBlock, Does.Not.Contain("- {fileID: 1885927100}"));
    }

    private static string ExtractObjectBlock(string text, string blockHeader)
    {
        int blockStart = text.IndexOf(blockHeader, StringComparison.Ordinal);
        Assert.That(blockStart, Is.GreaterThanOrEqualTo(0), $"Missing block: {blockHeader}");

        int blockEnd = text.IndexOf("\n--- !u!", blockStart + blockHeader.Length, StringComparison.Ordinal);
        return blockEnd < 0 ? text[blockStart..] : text[blockStart..blockEnd];
    }

    private static string ExtractMonoBehaviourBlockByScriptGuid(string text, string scriptGuid)
    {
        string scriptNeedle = $"guid: {scriptGuid}";
        int scriptIndex = text.IndexOf(scriptNeedle, StringComparison.Ordinal);
        Assert.That(scriptIndex, Is.GreaterThanOrEqualTo(0), $"Missing script guid: {scriptGuid}");

        int blockStart = text.LastIndexOf("--- !u!114", scriptIndex, StringComparison.Ordinal);
        Assert.That(blockStart, Is.GreaterThanOrEqualTo(0), $"Missing MonoBehaviour block for script guid: {scriptGuid}");

        int blockEnd = text.IndexOf("\n--- !u!", scriptIndex, StringComparison.Ordinal);
        return blockEnd < 0 ? text[blockStart..] : text[blockStart..blockEnd];
    }
}
