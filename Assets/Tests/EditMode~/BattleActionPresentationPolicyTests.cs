using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class BattleActionPresentationPolicyTests
{
    [Test]
    public void ShouldPlayGenericMoveForSkillEffect_AttackSkill_ReturnsFalse()
    {
        SkillMasterData skill = new SkillMasterData
        {
            SkillId = "S_AttackMove_Test",
            Category = Category.Ability,
            TimelineNotation = TimelineActionType.Attack
        };

        Assert.That(
            BattleActionPresentationPolicy.ShouldPlayGenericMoveForSkillEffect(skill),
            Is.False);
    }

    [Test]
    public void ShouldPlayGenericMoveForSkillEffect_StandaloneMoveSkill_ReturnsTrue()
    {
        SkillMasterData skill = new SkillMasterData
        {
            SkillId = "S_Move_Test",
            Category = Category.Move,
            TimelineNotation = TimelineActionType.Move
        };

        Assert.That(
            BattleActionPresentationPolicy.ShouldPlayGenericMoveForSkillEffect(skill),
            Is.True);
    }
}
