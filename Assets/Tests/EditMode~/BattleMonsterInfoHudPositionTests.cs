using NUnit.Framework;

public sealed class BattleMonsterInfoHudPositionTests
{
    [Test]
    public void MonsterInfoOpened_MovesBattleHudDown()
    {
        BattleHudMonsterInfoPosition target = BattleMonsterInfoHudPositionPolicy.Resolve(
            monsterInfoOpen: true,
            canAcceptPlayerInput: true,
            introBlocking: false,
            battleEnded: false);

        Assert.That(target, Is.EqualTo(BattleHudMonsterInfoPosition.Down));
    }

    [Test]
    public void MonsterInfoClosed_DuringPlayerInput_MovesBattleHudUp()
    {
        BattleHudMonsterInfoPosition target = BattleMonsterInfoHudPositionPolicy.Resolve(
            monsterInfoOpen: false,
            canAcceptPlayerInput: true,
            introBlocking: false,
            battleEnded: false);

        Assert.That(target, Is.EqualTo(BattleHudMonsterInfoPosition.Up));
    }

    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    public void MonsterInfoClosed_WhenHudCannotReturnUp_KeepsBattleHudDown(
        bool canAcceptPlayerInput,
        bool introBlocking,
        bool battleEnded)
    {
        BattleHudMonsterInfoPosition target = BattleMonsterInfoHudPositionPolicy.Resolve(
            monsterInfoOpen: false,
            canAcceptPlayerInput: canAcceptPlayerInput,
            introBlocking: introBlocking,
            battleEnded: battleEnded);

        Assert.That(target, Is.EqualTo(BattleHudMonsterInfoPosition.Down));
    }
}
