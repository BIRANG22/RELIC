using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class BattleSkillNamePopupPresentationTests
{
    [Test]
    public void PlayerMoveCommand_DoesNotShowSkillName()
    {
        PlayerReservedCommand command = new PlayerReservedCommand(
            new CharacterRuntimeData { CharacterId = "C_Test" },
            new SkillMasterData
            {
                SkillId = "S_Move_1",
                Name = "이동",
                Category = Category.Move,
                TimelineNotation = TimelineActionType.Move
            });
        command.SetSelectionResult(
            BattleDirection.Right,
            1,
            new System.Collections.Generic.List<int> { 1 },
            UnityEngine.Vector2Int.right);

        Assert.That(BattleSkillNamePopupPresentation.ShouldShow(command), Is.False);
    }

    [Test]
    public void PlayerSkillCommand_UsesLocalizedDisplayName()
    {
        PlayerReservedCommand command = new PlayerReservedCommand(
            new CharacterRuntimeData { CharacterId = "C_Test" },
            new SkillMasterData
            {
                SkillId = "S_Test",
                Name = "강타",
                Category = Category.Ability,
                TimelineNotation = TimelineActionType.Attack
            });

        Assert.That(BattleSkillNamePopupPresentation.ShouldShow(command), Is.True);
        Assert.That(BattleSkillNamePopupPresentation.GetDisplayName(command), Is.EqualTo("강타"));
    }

    [Test]
    public void MonsterMoveCommand_DoesNotShowSkillName()
    {
        MonsterReservedCommand command = new MonsterReservedCommand(
            CreateMonsterRuntime(),
            new MonsterSkillData
            {
                SkillId = "S_Monster_Move",
                Name = "이동",
                TimelineNotation = TimelineActionType.Move
            });

        Assert.That(BattleSkillNamePopupPresentation.ShouldShow(command), Is.False);
    }

    [Test]
    public void MonsterSkillCommand_FallsBackToSkillIdWhenNameIsBlank()
    {
        MonsterReservedCommand command = new MonsterReservedCommand(
            CreateMonsterRuntime(),
            new MonsterSkillData
            {
                SkillId = "S_Monster_Test",
                Name = " ",
                TimelineNotation = TimelineActionType.Attack
            });

        Assert.That(BattleSkillNamePopupPresentation.ShouldShow(command), Is.True);
        Assert.That(BattleSkillNamePopupPresentation.GetDisplayName(command), Is.EqualTo("S_Monster_Test"));
    }

    private static MonsterRuntimeData CreateMonsterRuntime()
    {
        return new MonsterRuntimeData(
            "M_Runtime",
            new MonsterMasterData { MonsterId = "M_Test", HP = 1 });
    }
}
