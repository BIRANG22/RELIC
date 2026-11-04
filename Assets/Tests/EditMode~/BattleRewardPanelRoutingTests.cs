using System.IO;
using NUnit.Framework;

public sealed class BattleRewardPanelRoutingTests
{
    [Test]
    public void RewardCallers_DoNotReferenceRemovedEquipPanel()
    {
        string[] files =
        {
            "Assets/Project/Scripts/Gameplay/Scene/Battle/StartRoom/RelicChoiceAreaUI.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/RestRoomShopPanel.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventRoomController.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/ChestOpenButton.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleRewardPanelUI.cs",
            "Assets/Project/Scripts/BattleUIBlurRootCollector.cs"
        };

        foreach (string file in files)
            Assert.That(File.ReadAllText(file), Does.Not.Contain("BattleRewardEquipPanelUI"), file);
    }

    [Test]
    public void RemovedEquipPanelScript_DoesNotExist()
    {
        Assert.That(File.Exists("Assets/Project/Scripts/BattleRewardEquipPanelUI.cs"), Is.False);
    }
}
