using System.Linq;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class SkillOwnershipPolicyTests
{
    [TestCase("Char_01", "Char_01", true)]
    [TestCase(" char_01 ", "CHAR_01", true)]
    [TestCase("Char_01", "Char_02", false)]
    [TestCase("ALL", "Char_02", true)]
    [TestCase("", "Char_01", false)]
    public void CanEquip_UsesNormalizedCharacterOwnership(
        string ownerId,
        string characterId,
        bool expected)
    {
        SkillMasterData skill = new() { CharacterId = ownerId };

        Assert.That(SkillOwnershipPolicy.CanEquip(skill, characterId), Is.EqualTo(expected));
    }

    [Test]
    public void GetCompatiblePartyCharacterIds_ReturnsOnlyCompatibleNonEmptyIds()
    {
        SkillMasterData skill = new() { CharacterId = "Char_02" };

        string[] result = SkillOwnershipPolicy.GetCompatiblePartyCharacterIds(
            skill,
            new[] { "Char_01", " char_02 ", "", null }).ToArray();

        Assert.That(result, Is.EqualTo(new[] { "char_02" }));
    }
}
