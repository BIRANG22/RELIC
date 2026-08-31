using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleRewardContinueButtonTests
{
    [Test]
    public void RewardPanelCompleted_WaitsForContinueButtonBeforeInvokingCompletion()
    {
        GameObject checkerObject = new("BattleResultChecker");
        GameObject nextButtonObject = new("NextButton", typeof(Button));

        try
        {
            BattleResultChecker checker = checkerObject.AddComponent<BattleResultChecker>();
            Button nextButton = nextButtonObject.GetComponent<Button>();
            nextButtonObject.SetActive(false);

            SetPrivateField(checker, "nextButtonRoot", nextButtonObject);

            bool completed = false;
            MethodInfo completedMethod = typeof(BattleResultChecker).GetMethod(
                "OnBattleRewardPanelCompleted",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(completedMethod, Is.Not.Null);

            completedMethod.Invoke(checker, new object[] { (Action)(() => completed = true) });

            Assert.That(completed, Is.False);
            Assert.That(nextButtonObject.activeSelf, Is.True);

            nextButton.onClick.Invoke();

            Assert.That(completed, Is.True);
            Assert.That(nextButtonObject.activeSelf, Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(checkerObject);
            UnityEngine.Object.DestroyImmediate(nextButtonObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }
}
