using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public sealed class BattleBugfixRulesTests
{
    [TestCase(-1, -1)]
    [TestCase(0, -1)]
    [TestCase(1, -1)]
    [TestCase(-1, 0)]
    [TestCase(1, 0)]
    [TestCase(-1, 1)]
    [TestCase(0, 1)]
    [TestCase(1, 1)]
    public void MortMove_AllowsEachAdjacentCell(int x, int y)
    {
        Assert.That(MortAI.IsMoveOffsetInSkillRange(new Vector2Int(x, y)), Is.True);
    }

    [TestCase(0, 2)]
    [TestCase(0, -2)]
    [TestCase(2, 0)]
    [TestCase(-2, 0)]
    [TestCase(0, 0)]
    public void MortMove_RejectsCrossTwoAndNoMove(int x, int y)
    {
        Assert.That(MortAI.IsMoveOffsetInSkillRange(new Vector2Int(x, y)), Is.False);
    }

    [Test]
    public void BleedMoveStep_DealsCurrentStackThenConsumesOne()
    {
        var statuses = new List<StatusEffectRuntimeData>
        {
            new("E_Bleed", 3, 1)
        };

        Assert.That(BattleStatusEffectService.ConsumeBleedMoveStep(statuses), Is.EqualTo(3));
        Assert.That(BattleStatusEffectService.ConsumeBleedMoveStep(statuses), Is.EqualTo(2));
        Assert.That(BattleStatusEffectService.ConsumeBleedMoveStep(statuses), Is.EqualTo(1));
        Assert.That(BattleStatusEffectService.ConsumeBleedMoveStep(statuses), Is.Zero);
        Assert.That(statuses.Exists(status => status != null && status.EffectId == "E_Bleed"), Is.False);
    }

    [Test]
    public void GridEffectHp_ForcedVisibilityExpiresAtDeadline()
    {
        Assert.That(GridEffectHpUI.ShouldForceVisible(10f, 9.99f), Is.True);
        Assert.That(GridEffectHpUI.ShouldForceVisible(10f, 10f), Is.False);
    }

    [Test]
    public void TimelinePulse_FinalPhaseReturnsBaseMultiplier()
    {
        Assert.That(BattleTimelineGroupUI.ResolveOrderValuePulseMultiplier(1f, 1.1f),
            Is.EqualTo(1f).Within(0.0001f));
    }
}
