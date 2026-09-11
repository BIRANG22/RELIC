using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.TestTools;

public class LocalizationEditingLockToolTests
{
    private GameObject root;

    [SetUp]
    public void SetUp()
    {
        LocalizationEditingLockState.SetEnabledForEditor(true);
    }

    [TearDown]
    public void TearDown()
    {
        LocalizationEditingLockState.SetEnabledForEditor(true);

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

    [Test]
    public void SetHierarchyLocalizersEnabled_EnablesMigratedRuntimeOwner()
    {
        root = new GameObject("Root", typeof(TMPro.TextMeshProUGUI), typeof(LocalizeStringEvent), typeof(LocalizedTMPText));
        LocalizeStringEvent legacyLocalizer = root.GetComponent<LocalizeStringEvent>();
        LocalizedTMPText runtimeLocalizer = root.GetComponent<LocalizedTMPText>();
        legacyLocalizer.enabled = false;
        runtimeLocalizer.enabled = false;

        int changed = LocalizationEditingLockTool.SetHierarchyLocalizersEnabled(root, true);

        Assert.That(changed, Is.EqualTo(1));
        Assert.That(legacyLocalizer.enabled, Is.False);
        Assert.That(runtimeLocalizer.enabled, Is.True);
    }

    [Test]
    public void SetHierarchyLocalizersEnabled_DisablesRuntimeOwnerAndRestoresKoreanSource()
    {
        root = new GameObject("Root", typeof(TextMeshProUGUI), typeof(LocalizedTMPText));
        TextMeshProUGUI text = root.GetComponent<TextMeshProUGUI>();
        LocalizedTMPText runtimeLocalizer = root.GetComponent<LocalizedTMPText>();
        runtimeLocalizer.Configure("TEST_KEY", "원문", false);
        text.text = "Translated text";

        int changed = LocalizationEditingLockTool.SetHierarchyLocalizersEnabled(root, false);

        Assert.That(changed, Is.EqualTo(1));
        Assert.That(runtimeLocalizer.enabled, Is.False);
        Assert.That(text.text, Is.EqualTo("원문"));
    }

    [Test]
    public void SetHierarchyLocalizersEnabled_DoesNotChangeIgnoredRuntimeOwner()
    {
        root = new GameObject("Root", typeof(TextMeshProUGUI), typeof(LocalizedTMPText), typeof(LocalizationIgnore));
        LocalizedTMPText runtimeLocalizer = root.GetComponent<LocalizedTMPText>();

        int changed = LocalizationEditingLockTool.SetHierarchyLocalizersEnabled(root, false);

        Assert.That(changed, Is.Zero);
        Assert.That(runtimeLocalizer.enabled, Is.True);
    }

    [Test]
    public void SetLocalizationEnabledForEditor_DisablesSharedRuntimeLocalizationPath()
    {
        LocalizationEditingLockState.SetEnabledForEditor(false);

        Assert.That(LocalizationEditingLockState.IsEnabledForEditor, Is.False);
    }

    [UnityTest]
    public IEnumerator Get_KeyOnlyLocalizationUsesKoreanTableWhileEditingLockIsDisabled()
    {
        yield return LocalizationSettings.InitializationOperation;
        LocalizationEditingLockState.SetEnabledForEditor(false);

        string result = GameLocalization.Get(LocalizationKeys.RelicRarity.Common);

        Assert.That(result, Is.EqualTo("일반 유물"));
    }
}
