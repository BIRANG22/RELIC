using System.Reflection;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class BattleRewardEquipPanelUIRegressionTests
{
    [Test]
    public void Open_AfterPreviousResolution_ReenablesDeleteButton()
    {
        GameObject panelObject = new("Equip_panel");
        GameObject deleteObject = new("Delete_Button");

        try
        {
            deleteObject.transform.SetParent(panelObject.transform, false);
            Button deleteButton = deleteObject.AddComponent<Button>();
            BattleRewardEquipPanelUI panel = panelObject.AddComponent<BattleRewardEquipPanelUI>();

            SetPrivateField(panel, "deleteButton", deleteButton);
            deleteButton.interactable = false;

            panel.Open(
                new BattleRewardData
                {
                    Type = BattleRewardType.Skill,
                    RewardId = "S_Core_01",
                    Name = "Test Skill"
                },
                null);

            Assert.That(deleteButton.interactable, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void Open_AssignsRewardPanelAndBattleHudCanvasesToBlurBackground()
    {
        GameObject panelObject = new("Equip_panel");
        panelObject.SetActive(false);

        GameObject blurObject = new("Background", typeof(RectTransform), typeof(Image), typeof(UIBlurBackground));
        GameObject deleteObject = new("Delete_Button", typeof(Button));
        GameObject rewardPanelObject = new("BattleRewardPanelUI");
        GameObject firstHudObject = new("BattleHUDCanvas", typeof(RectTransform), typeof(Canvas));
        GameObject secondHudObject = new("BattleHUDCanvas", typeof(RectTransform), typeof(Canvas));
        GameObject inactiveHudObject = new("BattleHUDCanvas", typeof(RectTransform), typeof(Canvas));
        GameObject unrelatedCanvasObject = new("OtherCanvas", typeof(RectTransform), typeof(Canvas));

        try
        {
            blurObject.transform.SetParent(panelObject.transform, false);
            deleteObject.transform.SetParent(panelObject.transform, false);
            inactiveHudObject.SetActive(false);

            rewardPanelObject.AddComponent<BattleRewardPanelUI>();
            rewardPanelObject.SetActive(true);

            BattleRewardEquipPanelUI panel = panelObject.AddComponent<BattleRewardEquipPanelUI>();
            SetPrivateField(panel, "deleteButton", deleteObject.GetComponent<Button>());

            panel.Open(
                new BattleRewardData
                {
                    Type = BattleRewardType.Skill,
                    RewardId = "S_Core_01",
                    Name = "Test Skill"
                },
                null);

            UIBlurBackground blurBackground = blurObject.GetComponent<UIBlurBackground>();

            Assert.That(
                blurBackground.BlurredUiRoots,
                Is.EquivalentTo(new[] { rewardPanelObject, firstHudObject, secondHudObject }));
            Assert.That(blurBackground.BlurredUiRoots, Has.No.Member(inactiveHudObject));
            Assert.That(blurBackground.BlurredUiRoots, Has.No.Member(unrelatedCanvasObject));
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
            Object.DestroyImmediate(rewardPanelObject);
            Object.DestroyImmediate(firstHudObject);
            Object.DestroyImmediate(secondHudObject);
            Object.DestroyImmediate(inactiveHudObject);
            Object.DestroyImmediate(unrelatedCanvasObject);
            DestroyBlurCaptureManagers();
        }
    }

    [UnityTest]
    public IEnumerator CompleteResolvedReward_WithFadeComponent_DeactivatesAfterFadeOut()
    {
        GameObject panelObject = new("Equip_panel", typeof(RectTransform), typeof(UIFadeInOnEnable));
        GameObject imageObject = new("FadeTarget", typeof(RectTransform), typeof(Image));
        GameObject deleteObject = new("Delete_Button", typeof(Button));

        try
        {
            imageObject.transform.SetParent(panelObject.transform, false);
            deleteObject.transform.SetParent(panelObject.transform, false);

            UIFadeInOnEnable fade = panelObject.GetComponent<UIFadeInOnEnable>();
            SetPrivateField(fade, "fadeDuration", 0.02f);

            BattleRewardEquipPanelUI panel = panelObject.AddComponent<BattleRewardEquipPanelUI>();
            SetPrivateField(panel, "deleteButton", deleteObject.GetComponent<Button>());

            bool callbackCalled = false;
            panel.Open(
                new BattleRewardData
                {
                    Type = BattleRewardType.Skill,
                    RewardId = "S_Core_01",
                    Name = "Test Skill"
                },
                () => callbackCalled = true);

            yield return null;

            InvokePrivate(panel, "CompleteResolvedReward");

            Assert.That(panelObject.activeSelf, Is.True);
            Assert.That(callbackCalled, Is.False);

            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(panelObject.activeSelf, Is.False);
            Assert.That(callbackCalled, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    [UnityTest]
    public IEnumerator BeginAppliedPreviewAndClose_IgnoresPreviewDelayBeforeFadeOut()
    {
        GameObject panelObject = new("Equip_panel", typeof(RectTransform), typeof(UIFadeInOnEnable));
        GameObject imageObject = new("FadeTarget", typeof(RectTransform), typeof(Image));
        GameObject deleteObject = new("Delete_Button", typeof(Button));

        try
        {
            imageObject.transform.SetParent(panelObject.transform, false);
            deleteObject.transform.SetParent(panelObject.transform, false);

            UIFadeInOnEnable fade = panelObject.GetComponent<UIFadeInOnEnable>();
            SetPrivateField(fade, "fadeDuration", 0f);

            BattleRewardEquipPanelUI panel = panelObject.AddComponent<BattleRewardEquipPanelUI>();
            SetPrivateField(panel, "deleteButton", deleteObject.GetComponent<Button>());
            SetPrivateField(panel, "closeDelayAfterApply", 10f);

            bool callbackCalled = false;
            panel.Open(
                new BattleRewardData
                {
                    Type = BattleRewardType.Skill,
                    RewardId = "S_Core_01",
                    Name = "Test Skill"
                },
                () => callbackCalled = true);

            yield return null;

            InvokePrivate(panel, "BeginAppliedPreviewAndClose");

            Assert.That(
                callbackCalled,
                Is.True,
                "Applied rewards should begin closing immediately instead of waiting closeDelayAfterApply.");
            Assert.That(panelObject.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(target, null);
    }

    private static void DestroyBlurCaptureManagers()
    {
        UIBlurBackgroundCaptureManager[] managers = Object.FindObjectsByType<UIBlurBackgroundCaptureManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < managers.Length; i++)
            Object.DestroyImmediate(managers[i].gameObject);
    }
}
