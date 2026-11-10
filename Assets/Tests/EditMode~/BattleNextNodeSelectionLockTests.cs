using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleNextNodeSelectionLockTests
{
    private GameObject panelObject;

    [TearDown]
    public void TearDown()
    {
        if (panelObject != null)
            Object.DestroyImmediate(panelObject);
    }

    [Test]
    public void LockInputAfterSelection_BlocksRaycastsUntilNextOpen()
    {
        panelObject = new GameObject("NextNodeSelectionPanel", typeof(RectTransform));
        BattleNextNodeSelectionPanel panel = panelObject.AddComponent<BattleNextNodeSelectionPanel>();
        CanvasGroup canvasGroup = panelObject.GetComponent<CanvasGroup>();

        panel.LockInputAfterSelection();

        Assert.That(panel.IsInputLocked, Is.True);
        Assert.That(canvasGroup.blocksRaycasts, Is.False);
        Assert.That(canvasGroup.interactable, Is.False);

        panel.Open(new MapRuntimeData(), _ => { });

        Assert.That(panel.IsInputLocked, Is.False);
        Assert.That(canvasGroup.blocksRaycasts, Is.True);
        Assert.That(canvasGroup.interactable, Is.True);
    }

    [Test]
    public void CanBeginSelection_WhenSharedStateEditIsBlocked_ReturnsFalse()
    {
        bool permissionChecked = false;

        bool accepted = BattleNextNodeChoiceButton.CanBeginSelection(() =>
        {
            permissionChecked = true;
            return true;
        });

        Assert.That(permissionChecked, Is.True);
        Assert.That(accepted, Is.False);
    }

    [Test]
    public void CanBeginSelection_WhenSharedStateEditIsAllowed_ReturnsTrue()
    {
        Assert.That(BattleNextNodeChoiceButton.CanBeginSelection(() => false), Is.True);
    }
}
