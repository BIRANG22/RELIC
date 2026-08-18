using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public sealed class EventEquippedRelicSelectionPanelUITests
{
    [Test]
    public void OpenCreatesVisibleOptionForEveryEquippedRelic()
    {
        GameObject root = CreateSceneBoundPanel(out EventEquippedRelicSelectionPanelUI panel);

        try
        {
            List<EventChoiceEquippedRelicCost> options = new List<EventChoiceEquippedRelicCost>
            {
                new EventChoiceEquippedRelicCost("C_001", 0, "Relic_A"),
                new EventChoiceEquippedRelicCost("C_001", 2, "Relic_B"),
                new EventChoiceEquippedRelicCost("C_002", 1, "Relic_C")
            };

            bool opened = panel.Open(options, CreateEntry, _ => true, () => { });

            Assert.That(opened, Is.True);
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.VisibleOptionCount, Is.EqualTo(options.Count));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void TrySelectInvokesSelectedCallbackAndClosesPanel()
    {
        GameObject root = CreateSceneBoundPanel(out EventEquippedRelicSelectionPanelUI panel);

        try
        {
            EventChoiceEquippedRelicCost option =
                new EventChoiceEquippedRelicCost("C_001", 2, "Relic_B");
            EventChoiceEquippedRelicCost selected = default(EventChoiceEquippedRelicCost);
            bool selectedInvoked = false;
            bool cancelInvoked = false;

            bool opened = panel.Open(
                new[] { option },
                CreateEntry,
                cost =>
                {
                    selected = cost;
                    selectedInvoked = true;
                    return true;
                },
                () => cancelInvoked = true);

            bool didSelect = panel.TrySelect(option);

            Assert.That(opened, Is.True);
            Assert.That(didSelect, Is.True);
            Assert.That(selectedInvoked, Is.True);
            Assert.That(cancelInvoked, Is.False);
            Assert.That(selected.CharacterId, Is.EqualTo("C_001"));
            Assert.That(selected.RelicSlotIndex, Is.EqualTo(2));
            Assert.That(selected.RelicId, Is.EqualTo("Relic_B"));
            Assert.That(panel.IsOpen, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void TrySelectKeepsPanelOpenWhenSelectionIsRejected()
    {
        GameObject root = CreateSceneBoundPanel(out EventEquippedRelicSelectionPanelUI panel);

        try
        {
            EventChoiceEquippedRelicCost option =
                new EventChoiceEquippedRelicCost("C_001", 2, "Relic_B");
            bool selectedInvoked = false;

            bool opened = panel.Open(
                new[] { option },
                CreateEntry,
                _ =>
                {
                    selectedInvoked = true;
                    return false;
                },
                () => { });

            bool didSelect = panel.TrySelect(option);

            Assert.That(opened, Is.True);
            Assert.That(didSelect, Is.False);
            Assert.That(selectedInvoked, Is.True);
            Assert.That(panel.IsOpen, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void CancelSelectionInvokesCancelCallbackAndClosesPanel()
    {
        GameObject root = CreateSceneBoundPanel(out EventEquippedRelicSelectionPanelUI panel);

        try
        {
            bool selectedInvoked = false;
            bool cancelInvoked = false;

            bool opened = panel.Open(
                new[] { new EventChoiceEquippedRelicCost("C_001", 0, "Relic_A") },
                CreateEntry,
                _ =>
                {
                    selectedInvoked = true;
                    return true;
                },
                () => cancelInvoked = true);

            panel.CancelSelection();

            Assert.That(opened, Is.True);
            Assert.That(selectedInvoked, Is.False);
            Assert.That(cancelInvoked, Is.True);
            Assert.That(panel.IsOpen, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void OpenFailsWithoutSceneBoundPanelReferences()
    {
        GameObject root = new GameObject("SelectionPanel");

        try
        {
            EventEquippedRelicSelectionPanelUI panel =
                root.AddComponent<EventEquippedRelicSelectionPanelUI>();
            int initialChildCount = root.transform.childCount;

            bool opened = panel.Open(
                new[] { new EventChoiceEquippedRelicCost("C_001", 0, "Relic_A") },
                CreateEntry,
                _ => true,
                () => { });

            Assert.That(opened, Is.False);
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(root.transform.childCount, Is.EqualTo(initialChildCount));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreateSceneBoundPanel(
        out EventEquippedRelicSelectionPanelUI panel)
    {
        GameObject root = new GameObject("SelectionPanel", typeof(RectTransform));
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(root.transform, false);

        GameObject template = new GameObject(
            "OptionTemplate",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));
        template.transform.SetParent(content.transform, false);
        template.SetActive(false);

        CreateImage(template.transform, "Icon");
        CreateText(template.transform, "RelicNameText");
        CreateText(template.transform, "CharacterNameText");
        CreateText(template.transform, "SlotNameText");

        GameObject emptyText = CreateText(root.transform, "EmptyText");
        GameObject cancel = new GameObject("CancelButton", typeof(RectTransform), typeof(Button));
        cancel.transform.SetParent(root.transform, false);

        panel = root.AddComponent<EventEquippedRelicSelectionPanelUI>();
        SetPrivateField(panel, "panelRoot", root);
        SetPrivateField(panel, "contentRoot", content.GetComponent<RectTransform>());
        SetPrivateField(panel, "emptyText", emptyText.GetComponent<TMP_Text>());
        SetPrivateField(panel, "cancelButton", cancel.GetComponent<Button>());
        SetPrivateField(panel, "optionTemplate", template);
        root.SetActive(false);
        return root;
    }

    private static GameObject CreateText(Transform parent, string name)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        return textObject;
    }

    private static GameObject CreateImage(Transform parent, string name)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        return imageObject;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private static EventEquippedRelicSelectionPanelEntry CreateEntry(
        EventChoiceEquippedRelicCost cost)
    {
        return new EventEquippedRelicSelectionPanelEntry(
            cost,
            cost.CharacterId,
            "Slot " + cost.RelicSlotIndex,
            cost.RelicId);
    }
}
