using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class RelicDescriptionFormatterTests
{
    [Test]
    public void Format_EffectTokens_AddsLinksAndUnderlines()
    {
        string result = RelicDescriptionFormatter.Format("적에게 {E_Vulnerable}과 {E_Weaken}를 부여한다.");

        Assert.That(result, Does.Contain("<link=\"effecticon:E_Vulnerable\"><u>"));
        Assert.That(result, Does.Contain("</u></link>"));
        Assert.That(result, Does.Not.Contain("{E_"));
    }

    [Test]
    public void Format_PlainDescription_PreservesSource()
    {
        const string source = "전투 시작 시 공격력이 증가한다.";

        Assert.That(RelicDescriptionFormatter.Format(source), Is.EqualTo(source));
    }

    [Test]
    public void ExtractEffectIds_RepeatedEffects_ReturnsDistinctSourceOrder()
    {
        string formatted =
            "<link=\"effecticon:E_Vulnerable\"><u>취약</u></link> " +
            "<link=\"effecticon:E_Weaken\"><u>약화</u></link> " +
            "<link=\"effecticon:e_vulnerable\"><u>취약</u></link>";

        List<string> ids = RelicDescriptionFormatter.ExtractEffectIds(formatted);

        Assert.That(ids, Is.EqualTo(new[] { "E_Vulnerable", "E_Weaken" }));
    }

    [Test]
    public void ResolveTooltipEffectIds_RelicWrapperEffects_ReturnsDisplayedStatuses()
    {
        List<string> ids = RelicDescriptionFormatter.ResolveTooltipEffectIds(
            "E_Turn_Start_All_Enemy_Vulnerable;E_Turn_Start_All_Enemy_Weaken");

        Assert.That(ids, Is.EqualTo(new[] { "E_Vulnerable", "E_Weaken" }));
    }
}
