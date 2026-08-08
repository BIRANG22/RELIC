using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class BattleRewardEquipPanelUIRegressionTests
{
    [Test]
    public void Open_AfterPreviousResolution_ReenablesDeleteButton()
    {
        GameObject panelObject = new("Equip_panel");
        GameObject deleteObject = new("Delete_Button");

        try
        {
            deleteObject.transform.SetParent(panelObject.transform, false);
            Button deleteButton = deleteObject.AddComponent<Button>();
            BattleRewardEquipPanelUI panel = panelObject.AddComponent<BattleRewardEquipPanelUI>();

            SetPrivateField(panel, "deleteButton", deleteButton);
            deleteButton.interactable = false;

            panel.Open(
                new BattleRewardData
                {
                    Type = BattleRewardType.Skill,
                    RewardId = "S_Core_01",
                    Name = "Test Skill"
                },
                null);

            Assert.That(deleteButton.interactable, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }
}
