using NUnit.Framework;
using Relic.Gameplay.Data;

public class BattleGameDataLocalizationTests
{
    [Test]
    public void GameDataLocalization_UsesStableMonsterSpecialActionKey()
    {
        Assert.That(
            GameLocalization.BuildDataKey("Monster", "Monster_01", "special_action_1"),
            Is.EqualTo("data.monster.monster_01.special_action_1"));
    }

    [Test]
    public void GameDataLocalization_UsesStableCharacterNameKey()
    {
        Assert.That(
            GameLocalization.BuildDataKey("Character", "Char_02", "name"),
            Is.EqualTo("data.character.char_02.name"));
    }
}
