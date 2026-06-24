using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class TimelinePreviewEntryTests
{
    [Test]
    public void MonsterPreviewEntry_ExposesRangeIconSeparatelyFromActionIcon()
    {
        PropertyInfo rangeIconProperty = typeof(BattleTimelinePreviewEntry).GetProperty(
            "SkillRangeIcon",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(rangeIconProperty, Is.Not.Null);
        Assert.That(rangeIconProperty.PropertyType, Is.EqualTo(typeof(Sprite)));
    }

    [Test]
    public void TimelineSkillHoverPopupView_AcceptsRangeIconForDescriptionRow()
    {
        MethodInfo setMethod = typeof(TimelineSkillHoverPopupView).GetMethod(
            "Set",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new[] { typeof(string), typeof(string), typeof(Sprite) },
            null);

        Assert.That(setMethod, Is.Not.Null);
    }

    [Test]
    public void PlayerPreviewEntry_FormatsReservedSkillTooltipWithActualPayAmount()
    {
        SkillMasterData skill = new()
        {
            SkillId = "S_Timeline_Tooltip",
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostType = ResourceCostType.Fixed,
            ResourceCostValue = 2,
            ToolTip = "{(3+\uC9D1\uC911)x\uC18C\uBAA8\uB7C9}\uC758 \uBC29\uC5B4\uB3C4\uB97C \uC5BB\uB294\uB2E4."
        };

        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Timeline_Tooltip",
            CurrentCost = 5
        };
        runtime.StatusEffects.Add(new StatusEffectRuntimeData("E_Focus", 1));

        PlayerReservedCommand command = new(runtime, skill);
        BattleTimelinePreviewEntry entry = BattleTimelinePreviewEntry.CreatePlayer(0, 0, command, 0);

        Assert.That(entry.SkillEffectDescription, Does.Contain("8\uC758 \uBC29\uC5B4\uB3C4"));
        Assert.That(entry.SkillEffectDescription, Does.Not.Contain("{"));
        Assert.That(entry.SkillEffectDescription, Does.Not.Contain("\uC18C\uBAA8\uB7C9"));
    }
}
