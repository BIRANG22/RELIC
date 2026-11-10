using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleRewardSkillEffectHoverTests
{
    private GameObject panelObject;

    [TearDown]
    public void TearDown()
    {
        if (panelObject != null)
            Object.DestroyImmediate(panelObject);
    }

    [Test]
    public void ExtractLinkedEffectIds_DeduplicatesValidDescriptionEffectsInSourceOrder()
    {
        List<string> ids = SkillDescriptionFormatter.ExtractLinkedEffectIds(
            "<link=\"effecticon:E_Strike\">Damage</link> " +
            "<link=\"effecticon:E_Bleed\">Bleed</link> " +
            "<link=\"effecticon:e_strike\">Damage</link>");

        Assert.That(ids, Is.EqualTo(new[] { "E_Strike", "E_Bleed" }));
    }

    [Test]
    public void BuildEffectPreview_PoisonEqualTargetPoison_ExplainsAuthoredPoisonStatus()
    {
        SkillMasterData skill = new SkillMasterData
        {
            Details = "Target gains {E_Poison} equal to its current {E_Poison}.",
            EffectIds = "E_PoisonEqualTargetPoison",
            EffectEntries = new List<SkillEffectEntry>
            {
                new SkillEffectEntry { EffectId = "E_PoisonEqualTargetPoison" }
            }
        };

        List<StatusEffectRuntimeData> preview = BattleRewardSkillPanelUI.BuildEffectPreview(skill);

        Assert.That(preview, Has.Count.EqualTo(1));
        Assert.That(preview[0].EffectId, Is.EqualTo("E_Poison"));
    }

    [Test]
    public void BuildEffectPreview_NoSkill_ReturnsEmptyList()
    {
        Assert.That(BattleRewardSkillPanelUI.BuildEffectPreview(null), Is.Empty);
    }

    [Test]
    public void SetCardHovered_PointerExit_ClearsEffectTooltipWithoutSlotPreview()
    {
        panelObject = new GameObject("RewardSkillPanel");
        BattleRewardSkillPanelUI panel = panelObject.AddComponent<BattleRewardSkillPanelUI>();
        FieldInfo activeCardField = typeof(BattleRewardSkillPanelUI).GetField(
            "offeredSkillEffectCardIndex",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(activeCardField, Is.Not.Null);
        activeCardField.SetValue(panel, 1);

        panel.SetCardHovered(1, false);

        Assert.That(activeCardField.GetValue(panel), Is.EqualTo(-1));
    }
}
