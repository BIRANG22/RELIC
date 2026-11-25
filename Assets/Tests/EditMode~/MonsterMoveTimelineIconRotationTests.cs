using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class MonsterMoveTimelineIconRotationTests
{
    [TestCase(1, 0, 0f)]
    [TestCase(1, -1, 45f)]
    [TestCase(0, -1, 90f)]
    [TestCase(-1, -1, 135f)]
    [TestCase(-1, 0, 180f)]
    [TestCase(-1, 1, 225f)]
    [TestCase(0, 1, 270f)]
    [TestCase(1, 1, 315f)]
    [TestCase(4, -3, 45f)]
    public void MonsterMoveEntry_RotatesIconTowardReservedMoveDirection(
        int moveX,
        int moveY,
        float expectedRotationZ)
    {
        BattleTimelinePreviewEntry entry = CreateMonsterEntry(
            TimelineActionType.Move,
            new Vector2Int(moveX, moveY));

        Assert.That(entry.SkillIconRotationZ, Is.EqualTo(expectedRotationZ));
    }

    [Test]
    public void MonsterMoveEntry_WithZeroOffset_KeepsDefaultRightRotation()
    {
        BattleTimelinePreviewEntry entry = CreateMonsterEntry(
            TimelineActionType.Move,
            Vector2Int.zero);

        Assert.That(entry.SkillIconRotationZ, Is.Zero);
    }

    [Test]
    public void MonsterNonMoveEntry_ResetsIconRotation()
    {
        BattleTimelinePreviewEntry entry = CreateMonsterEntry(
            TimelineActionType.Attack,
            Vector2Int.left);

        Assert.That(entry.SkillIconRotationZ, Is.Zero);
    }

    private static BattleTimelinePreviewEntry CreateMonsterEntry(
        TimelineActionType actionType,
        Vector2Int moveOffset)
    {
        MonsterReservedCommand command = new MonsterReservedCommand(
            null,
            new MonsterSkillData
            {
                SkillId = "S_Test",
                TimelineNotation = actionType
            });
        command.SetMoveOffset(moveOffset);

        return BattleTimelinePreviewEntry.CreateMonster(0, 0, command);
    }
}
