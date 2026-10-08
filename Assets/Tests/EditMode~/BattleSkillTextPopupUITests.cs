using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
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

    [Test]
    public void BattleScene_ContainsConfiguredSkillPopupUnderBattleCanvas()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity");

        Assert.That(scene, Does.Contain("m_Name: BattleSkillTextPopupUI"));
        Assert.That(scene, Does.Contain("guid: 0d923d31abc44bb780f5e9bb67ad4a52"));
        Assert.That(scene, Does.Contain("- {fileID: 910000102}"),
            "Battle Canvas가 스킬명 팝업 Transform을 자식으로 보유해야 합니다.");
        Match transform = Regex.Match(
            scene,
            @"(?ms)^--- !u!4 &910000102\r?\nTransform:.*?(?=^--- !u!)");
        Assert.That(transform.Success, Is.True);
        Assert.That(transform.Value, Does.Contain("m_Father: {fileID: 742669610}"));
    }

    private static FieldInfo GetInstanceField(Type type, string name)
    {
        return type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
