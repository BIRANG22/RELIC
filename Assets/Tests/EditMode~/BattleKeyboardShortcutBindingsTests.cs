using NUnit.Framework;
using UnityEngine;

public class BattleKeyboardShortcutBindingsTests
{
    [SetUp]
    public void SetUp()
    {
        foreach (BattleShortcutAction action in System.Enum.GetValues(typeof(BattleShortcutAction)))
            PlayerPrefs.DeleteKey(BattleKeyboardShortcutBindings.GetPreferenceKey(action));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (BattleShortcutAction action in System.Enum.GetValues(typeof(BattleShortcutAction)))
            PlayerPrefs.DeleteKey(BattleKeyboardShortcutBindings.GetPreferenceKey(action));
    }

    [TestCase(BattleShortcutAction.Move, KeyCode.F)]
    [TestCase(BattleShortcutAction.Flip, KeyCode.D)]
    [TestCase(BattleShortcutAction.Compound, KeyCode.C)]
    [TestCase(BattleShortcutAction.Undo, KeyCode.Z)]
    [TestCase(BattleShortcutAction.Redo, KeyCode.X)]
    [TestCase(BattleShortcutAction.Skill01, KeyCode.Q)]
    [TestCase(BattleShortcutAction.Skill02, KeyCode.W)]
    [TestCase(BattleShortcutAction.Skill03, KeyCode.E)]
    [TestCase(BattleShortcutAction.Skill04, KeyCode.R)]
    [TestCase(BattleShortcutAction.CharacterCycle, KeyCode.Tab)]
    [TestCase(BattleShortcutAction.TimelineSlot01, KeyCode.Alpha1)]
    [TestCase(BattleShortcutAction.TimelineSlot02, KeyCode.Alpha2)]
    [TestCase(BattleShortcutAction.TimelineSlot03, KeyCode.Alpha3)]
    [TestCase(BattleShortcutAction.TimelineSlot04, KeyCode.Alpha4)]
    [TestCase(BattleShortcutAction.TimelineSlot05, KeyCode.Alpha5)]
    [TestCase(BattleShortcutAction.EndTurn, KeyCode.Space)]
    public void Get_WithoutSavedValue_ReturnsDefault(BattleShortcutAction action, KeyCode expected)
    {
        Assert.That(BattleKeyboardShortcutBindings.Get(action), Is.EqualTo(expected));
    }

    [Test]
    public void Set_AllowsDuplicateKeys_AndMarksEveryDuplicateAction()
    {
        BattleKeyboardShortcutBindings.Set(BattleShortcutAction.Move, KeyCode.G);
        BattleKeyboardShortcutBindings.Set(BattleShortcutAction.Flip, KeyCode.G);

        Assert.That(BattleKeyboardShortcutBindings.Get(BattleShortcutAction.Move), Is.EqualTo(KeyCode.G));
        Assert.That(BattleKeyboardShortcutBindings.Get(BattleShortcutAction.Flip), Is.EqualTo(KeyCode.G));
        Assert.That(BattleKeyboardShortcutBindings.IsDuplicate(BattleShortcutAction.Move), Is.True);
        Assert.That(BattleKeyboardShortcutBindings.IsDuplicate(BattleShortcutAction.Flip), Is.True);
        Assert.That(BattleKeyboardShortcutBindings.IsDuplicate(BattleShortcutAction.Compound), Is.False);
    }

    [Test]
    public void Set_AllowsDuplicateBetweenExistingAndAdditionalShortcut()
    {
        BattleKeyboardShortcutBindings.Set(BattleShortcutAction.Move, KeyCode.Tab);

        Assert.That(BattleKeyboardShortcutBindings.IsDuplicate(BattleShortcutAction.Move), Is.True);
        Assert.That(BattleKeyboardShortcutBindings.IsDuplicate(BattleShortcutAction.CharacterCycle), Is.True);
    }

    [Test]
    public void ResetAll_RestoresEveryDefault()
    {
        BattleKeyboardShortcutBindings.Set(BattleShortcutAction.Move, KeyCode.G);
        BattleKeyboardShortcutBindings.Set(BattleShortcutAction.Skill04, KeyCode.T);

        BattleKeyboardShortcutBindings.ResetAll();

        Assert.That(BattleKeyboardShortcutBindings.Get(BattleShortcutAction.Move), Is.EqualTo(KeyCode.F));
        Assert.That(BattleKeyboardShortcutBindings.Get(BattleShortcutAction.Skill04), Is.EqualTo(KeyCode.R));
    }

    [Test]
    public void Get_WithInvalidStoredValue_ReturnsDefault()
    {
        PlayerPrefs.SetInt(
            BattleKeyboardShortcutBindings.GetPreferenceKey(BattleShortcutAction.Move),
            int.MaxValue);

        Assert.That(BattleKeyboardShortcutBindings.Get(BattleShortcutAction.Move), Is.EqualTo(KeyCode.F));
    }

    [Test]
    public void GetDisplayName_UsesReadableKeyboardLabels()
    {
        BattleKeyboardShortcutBindings.Set(BattleShortcutAction.Move, KeyCode.LeftShift);
        Assert.That(BattleKeyboardShortcutBindings.GetDisplayName(BattleShortcutAction.Move), Is.EqualTo("L Shift"));
    }
}
