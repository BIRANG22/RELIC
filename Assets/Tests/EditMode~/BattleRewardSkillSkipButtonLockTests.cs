using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class BattleRewardSkillSkipButtonLockTests
{
    private const string SourcePath = "Assets/Project/Scripts/BattleRewardSkillPanelUI.cs";
    private const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";

    [Test]
    public void LockSkipButtonForSelection_DisablesButtonInteraction()
    {
        GameObject panelObject = new GameObject("RewardSkillPanelTest");
        GameObject buttonObject = new GameObject("SkipButton");
        buttonObject.transform.SetParent(panelObject.transform, false);

        try
        {
            BattleRewardSkillPanelUI panel = panelObject.AddComponent<BattleRewardSkillPanelUI>();
            Button button = buttonObject.AddComponent<Button>();
            UIChildHoverTransform childHover = buttonObject.AddComponent<UIChildHoverTransform>();
            button.interactable = true;

            typeof(BattleRewardSkillPanelUI)
                .GetField("skipButton", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(panel, button);

            MethodInfo method = typeof(BattleRewardSkillPanelUI).GetMethod(
                "LockSkipButtonForSelection",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            method.Invoke(panel, null);
            Assert.That(button.interactable, Is.False);
            Assert.That(childHover.enabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void SelectionEntryPoints_LockSkipButtonBeforePresentation()
    {
        string source = File.ReadAllText(SourcePath);
        string selectMethod = ExtractMethod(source, "private void Select(int index)");
        string confirmMethod = ExtractMethod(source, "private void ConfirmSkillSelection(");

        Assert.That(selectMethod, Does.Contain("LockSkipButtonForSelection();"));
        Assert.That(confirmMethod, Does.Contain("LockSkipButtonForSelection();"));
    }

    [Test]
    public void ReplacementSelection_RemainsLockedWhenAnotherCardFinishesReveal()
    {
        GameObject panelObject = new GameObject("RewardSkillPanelLockStateTest");

        try
        {
            BattleRewardSkillPanelUI panel = panelObject.AddComponent<BattleRewardSkillPanelUI>();
            FieldInfo pendingReplacement = typeof(BattleRewardSkillPanelUI).GetField(
                "pendingReplacementCard",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo isLocked = typeof(BattleRewardSkillPanelUI).GetMethod(
                "IsSkipInteractionLocked",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(pendingReplacement, Is.Not.Null);
            Assert.That(isLocked, Is.Not.Null);

            pendingReplacement.SetValue(panel, 1);

            Assert.That(isLocked.Invoke(panel, null), Is.EqualTo(true));
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void DiscardReward_PlaysClickSoundBeforeLockingInteraction()
    {
        string source = File.ReadAllText(SourcePath);
        string method = ExtractMethod(source, "public void DiscardReward()");

        int play = method.IndexOf("PlaySkipClickSfx();", System.StringComparison.Ordinal);
        int lockButton = method.IndexOf("SetSkipRevealState(false);", System.StringComparison.Ordinal);

        Assert.That(play, Is.GreaterThanOrEqualTo(0));
        Assert.That(lockButton, Is.GreaterThan(play));
    }

    [Test]
    public void SkillRewardSkipButton_DisablesPointerClickAudioToAvoidDuplicateSound()
    {
        string scene = File.ReadAllText(BattleScenePath).Replace("\r\n", "\n");
        int button = scene.IndexOf("--- !u!1 &1618098816\n", System.StringComparison.Ordinal);
        int nextObject = scene.IndexOf("--- !u!1 &", button + 12, System.StringComparison.Ordinal);
        string block = scene.Substring(button, nextObject - button);

        Assert.That(block, Does.Contain("m_Name: SkipButton\n"));
        Assert.That(block, Does.Contain("playOnClick: 0\n"));
        Assert.That(block, Does.Contain("clickSoundId: ui.normal.click\n"));
    }

    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature, System.StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), signature);

        int bodyStart = source.IndexOf('{', start);
        int depth = 0;

        for (int i = bodyStart; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            if (source[i] != '}') continue;

            depth--;
            if (depth == 0)
                return source.Substring(start, i - start + 1);
        }

        Assert.Fail($"Method body not found: {signature}");
        return string.Empty;
    }
}
