using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class BattleRewardSkillSkipButtonLockTests
{
    private const string SourcePath = "Assets/Project/Scripts/BattleRewardSkillPanelUI.cs";

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
