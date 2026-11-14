using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class DebugShortcutHelpPanelTests
{
    private GameObject panelObject;

    [TearDown]
    public void TearDown()
    {
        if (panelObject != null)
            Object.DestroyImmediate(panelObject);

        FieldInfo instanceField = typeof(DebugShortcutHelpPanel).GetField(
            "<Instance>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        instanceField?.SetValue(null, null);
    }

    [Test]
    public void DefaultToggleKey_UsesBackslash()
    {
        panelObject = new GameObject("DebugShortcutHelpPanelTest");
        DebugShortcutHelpPanel panel = panelObject.AddComponent<DebugShortcutHelpPanel>();
        FieldInfo toggleKeyField = typeof(DebugShortcutHelpPanel).GetField(
            "toggleKey",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(toggleKeyField, Is.Not.Null);
        Assert.That(toggleKeyField.GetValue(panel), Is.EqualTo(KeyCode.Backslash));
    }
}
