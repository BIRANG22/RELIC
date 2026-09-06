using NUnit.Framework;
using Relic.Gameplay.Data;

public class RecordDisplayNameResolverTests
{
    [Test]
    public void ResolveDisplayName_UsesLocalizedValueBeforeSourceName()
    {
        SkillMasterData skill = new()
        {
            SkillId = "S_TEST",
            Name = "Source Skill"
        };

        string resolved = RecordDisplayNameResolver.ResolveDisplayName(
            skill,
            skill.SkillId,
            skill.Name,
            _ => "Localized Skill");

        Assert.That(resolved, Is.EqualTo("Localized Skill"));
    }

    [Test]
    public void ResolveDisplayName_UsesStableIdWhenLocalizedAndSourceNamesAreBlank()
    {
        ItemData item = new()
        {
            ItemId = "ITEM_TEST",
            Name = ""
        };

        string resolved = RecordDisplayNameResolver.ResolveDisplayName(
            item,
            item.ItemId,
            item.Name,
            _ => "");

        Assert.That(resolved, Is.EqualTo("ITEM_TEST"));
    }

    [Test]
    public void ItemName_WhenNameIsBlank_FallsBackToItemId()
    {
        ItemData item = new()
        {
            ItemId = "ITEM_TEST",
            Name = ""
        };

        Assert.That(RecordDisplayNameResolver.ItemName(item), Is.EqualTo("ITEM_TEST"));
    }
}
