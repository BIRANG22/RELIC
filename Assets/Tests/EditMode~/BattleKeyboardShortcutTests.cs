using NUnit.Framework;
using UnityEngine;

public sealed class BattleKeyboardShortcutTests
{
    [Test]
    public void BattleActions_UseRequestedShortcutKeys()
    {
        Assert.That(BattleKeyboardShortcutBindings.Move, Is.EqualTo(KeyCode.F));
        Assert.That(BattleKeyboardShortcutBindings.Flip, Is.EqualTo(KeyCode.D));
        Assert.That(BattleKeyboardShortcutBindings.Compound, Is.EqualTo(KeyCode.C));
        Assert.That(BattleKeyboardShortcutBindings.Undo, Is.EqualTo(KeyCode.Z));
        Assert.That(BattleKeyboardShortcutBindings.Redo, Is.EqualTo(KeyCode.X));
    }

    [Test]
    public void SkillShortcuts_RemainQwer()
    {
        Assert.That(BattleKeyboardShortcutBindings.Skill01, Is.EqualTo(KeyCode.Q));
        Assert.That(BattleKeyboardShortcutBindings.Skill02, Is.EqualTo(KeyCode.W));
        Assert.That(BattleKeyboardShortcutBindings.Skill03, Is.EqualTo(KeyCode.E));
        Assert.That(BattleKeyboardShortcutBindings.Skill04, Is.EqualTo(KeyCode.R));
    }
}
