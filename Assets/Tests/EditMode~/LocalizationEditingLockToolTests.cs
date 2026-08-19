using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization.Components;

public class LocalizationEditingLockToolTests
{
    private GameObject root;

    [TearDown]
    public void TearDown()
    {
        if (root != null)
            Object.DestroyImmediate(root);
    }

    [Test]
    public void SetHierarchyLocalizersEnabled_DisablesLocalizersOnInactiveChildren()
    {
        root = new GameObject("Root");
        var activeLocalizer = root.AddComponent<LocalizeStringEvent>();

        var child = new GameObject("Inactive Child");
        child.transform.SetParent(root.transform);
        var inactiveLocalizer = child.AddComponent<LocalizeStringEvent>();
        child.SetActive(false);

        int changed = LocalizationEditingLockTool.SetHierarchyLocalizersEnabled(root, false);

        Assert.That(changed, Is.EqualTo(2));
        Assert.That(activeLocalizer.enabled, Is.False);
        Assert.That(inactiveLocalizer.enabled, Is.False);
    }

    [Test]
    public void SetHierarchyLocalizersEnabled_DoesNotCountAlreadyMatchingLocalizers()
    {
        root = new GameObject("Root");
        var localizer = root.AddComponent<LocalizeStringEvent>();
        localizer.enabled = false;

        int changed = LocalizationEditingLockTool.SetHierarchyLocalizersEnabled(root, false);

        Assert.That(changed, Is.Zero);
        Assert.That(localizer.enabled, Is.False);
    }
}
