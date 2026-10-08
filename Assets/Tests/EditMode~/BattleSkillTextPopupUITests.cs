using System;
using System.Reflection;
using NUnit.Framework;

public sealed class BattleSkillTextPopupUITests
{
    [Test]
    public void SkillPopup_ExposesIndependentPresentationSettings()
    {
        Type type = typeof(BattleSkillTextPopupUI);

        Assert.That(GetInstanceField(type, "worldOffset"), Is.Not.Null);
        Assert.That(GetInstanceField(type, "holdDuration"), Is.Not.Null);
        Assert.That(GetInstanceField(type, "duration"), Is.Not.Null);
        Assert.That(GetInstanceField(type, "startScale"), Is.Not.Null);
        Assert.That(GetInstanceField(type, "endScale"), Is.Not.Null);
        Assert.That(GetInstanceField(type, "textColor"), Is.Not.Null);
        Assert.That(GetInstanceField(type, "fontSize"), Is.Not.Null);
    }

    [Test]
    public void DamagePopup_DoesNotOwnSkillNamePopupType()
    {
        Assert.That(
            Enum.IsDefined(typeof(BattleDamageTextPopupUI.PopupType), "SkillName"),
            Is.False);
    }

    private static FieldInfo GetInstanceField(Type type, string name)
    {
        return type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
