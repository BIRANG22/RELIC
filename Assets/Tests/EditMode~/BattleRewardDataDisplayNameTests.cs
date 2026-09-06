using NUnit.Framework;

public class BattleRewardDataDisplayNameTests
{
    [Test]
    public void GetDisplayName_RemnantIncludesAmount()
    {
        BattleRewardData reward = new()
        {
            Type = BattleRewardType.Remnant,
            Amount = 50,
            Name = "더스티움"
        };

        Assert.That(reward.GetDisplayName(), Is.EqualTo("더스티움 x50"));
    }

    [Test]
    public void GetDisplayName_RemnantUsesDefaultDustiumNameWhenNameIsEmpty()
    {
        BattleRewardData reward = new()
        {
            Type = BattleRewardType.Remnant,
            Amount = 20,
            Name = string.Empty
        };

        Assert.That(reward.GetDisplayName(), Is.EqualTo("더스티움 x20"));
    }

    [Test]
    public void GetDisplayName_NonRemnantKeepsExistingName()
    {
        BattleRewardData reward = new()
        {
            Type = BattleRewardType.Item,
            RewardId = "Item_001",
            Amount = 3,
            Name = "아이템"
        };

        Assert.That(reward.GetDisplayName(), Is.EqualTo("아이템"));
    }
}
