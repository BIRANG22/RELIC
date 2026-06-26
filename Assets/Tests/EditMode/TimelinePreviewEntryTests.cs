using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public void TimelineSkillHoverPopupView_ExpandsLayoutForUpgradeTooltipText()
    {
        GameObject rootObject = new("TimelinePopupPrefab", typeof(RectTransform), typeof(TimelineSkillHoverPopupView));
        GameObject backgroundObject = new("BackGround", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        GameObject nameObject = new("NameText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        GameObject effectObject = new("EffectText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));

        try
        {
            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(100f, 100f);

            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.SetParent(rootRect, false);
            backgroundRect.sizeDelta = new Vector2(300f, 80f);

            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.SetParent(rootRect, false);
            nameRect.sizeDelta = new Vector2(200f, 30f);

            RectTransform effectRect = effectObject.GetComponent<RectTransform>();
            effectRect.SetParent(rootRect, false);
            effectRect.sizeDelta = new Vector2(200f, 50f);

            TimelineSkillHoverPopupView view = rootObject.GetComponent<TimelineSkillHoverPopupView>();

            view.Set(
                "\uB3C5\uC131 \uC548\uAC1C -> \uB3C5\uC131 \uC548\uAC1C+",
                "\uD604\uC7AC\n10 \uC911\uB3C5\uC744 \uBD80\uC5EC\uD55C\uB2E4.\n\n\uAC15\uD654 \uD6C4\n15 \uC911\uB3C5\uC744 \uBD80\uC5EC\uD55C\uB2E4.",
                null);

            Assert.That(rootRect.sizeDelta.x, Is.GreaterThanOrEqualTo(320f));
            Assert.That(rootRect.sizeDelta.y, Is.GreaterThan(100f));
            Assert.That(nameRect.sizeDelta.x, Is.GreaterThanOrEqualTo(280f));
            Assert.That(effectRect.sizeDelta.x, Is.GreaterThanOrEqualTo(280f));
            Assert.That(effectRect.sizeDelta.y, Is.GreaterThan(50f));
        }
        finally
        {
            Object.DestroyImmediate(rootObject);
        }
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

    [Test]
    public void SkillTooltipFormatter_PrependsCalculatedValueWhenTextHasNoToken()
    {
        SkillMasterData skill = new()
        {
            SkillId = "S_Tooltip_Value",
            ToolTip = "\uD53C\uD574\uB97C \uC900\uB2E4."
        };
        skill.EffectEntries.Add(new SkillEffectEntry
        {
            EffectId = "E_Strike",
            ValueCalcType = ValueCalcType.Fixed,
            ValueAmount = 5
        });

        string description = SkillTooltipFormatter.Format(skill, skill.ToolTip, null, 0);

        Assert.That(description, Is.EqualTo("5 \uD53C\uD574\uB97C \uC900\uB2E4."));
    }

    [Test]
    public void SkillTooltipFormatter_RemovesValueTokenWhenMasterValueIsZero()
    {
        SkillMasterData skill = new()
        {
            SkillId = "S_Tooltip_No_Value",
            ToolTip = "\"\uC218\uCE58\" \uD654\uC0C1\uC744 \uBD80\uC5EC\uD55C\uB2E4."
        };
        skill.EffectEntries.Add(new SkillEffectEntry
        {
            EffectId = "E_Burn",
            ValueCalcType = ValueCalcType.Fixed,
            ValueAmount = 0
        });

        string description = SkillTooltipFormatter.Format(skill, skill.ToolTip, null, 0);

        Assert.That(description, Is.EqualTo("\uD654\uC0C1\uC744 \uBD80\uC5EC\uD55C\uB2E4."));
        Assert.That(description, Does.Not.Contain("\uC218\uCE58"));
    }

    [Test]
    public void SkillTooltipFormatter_ShowsZeroWhenRuntimeCalculationReachesZero()
    {
        SkillMasterData skill = new()
        {
            SkillId = "S_Tooltip_Runtime_Zero",
            ToolTip = "\uD53C\uD574\uB97C \uC900\uB2E4."
        };
        skill.EffectEntries.Add(new SkillEffectEntry
        {
            EffectId = "E_Strike",
            ValueCalcType = ValueCalcType.PerCost,
            ValueAmount = 3
        });

        string description = SkillTooltipFormatter.Format(skill, skill.ToolTip, null, 0);

        Assert.That(description, Is.EqualTo("0 \uD53C\uD574\uB97C \uC900\uB2E4."));
    }

    [Test]
    public void MonsterPreviewEntry_UsesReservedDamageForTooltipAndValueText()
    {
        MonsterSkillData skill = new()
        {
            SkillId = "M_Timeline_Damage",
            EffectIds = "E_Strike",
            ValueCalcTypes = "Fixed",
            ValueRate = "6",
            CountRate = "1",
            ValueRandomRange = 2,
            EffectDesc = "Deals \"\uC218\uCE58\" damage."
        };
        skill.EffectEntries.Add(new SkillEffectEntry
        {
            EffectId = "E_Strike",
            ValueCalcType = ValueCalcType.Fixed,
            ValueAmount = 6,
            CountAmount = 1
        });

        MonsterReservedCommand command = new(CreateMonsterRuntime(), skill);
        command.SetReservedDamage(7);

        BattleTimelinePreviewEntry entry = BattleTimelinePreviewEntry.CreateMonster(0, 0, command);

        Assert.That(entry.SkillValueText, Is.EqualTo("7"));
        Assert.That(entry.SkillEffectDescription, Does.Contain("7 damage"));
        Assert.That(entry.SkillEffectDescription, Does.Not.Contain("4-8"));
        Assert.That(entry.SkillEffectDescription, Does.Not.Contain("\uC218\uCE58"));
    }

    [Test]
    public void BattleDamageService_UsesReservedMonsterDamage()
    {
        MonsterSkillData skill = new()
        {
            SkillId = "M_Reserved_Damage",
            EffectIds = "E_Strike",
            ValueRate = "6",
            ValueRandomRange = 2
        };

        MonsterReservedCommand command = new(CreateMonsterRuntime(), skill);
        command.SetReservedDamage(7);

        BattleDamageService service = new(null);

        Assert.That(service.GetMonsterDamage(command), Is.EqualTo(7));
    }

    [Test]
    public void MonsterPreviewEntry_UsesReservedDamageForAttackNotationWithoutDamageEffectEntry()
    {
        MonsterSkillData skill = new()
        {
            SkillId = "M_Timeline_Attack_Notation",
            TimelineNotation = TimelineActionType.Attack,
            ValueRate = "6",
            ValueRandomRange = 2,
            EffectDesc = "Deals \"\uC218\uCE58\" damage."
        };

        MonsterReservedCommand command = new(CreateMonsterRuntime(), skill);
        command.SetReservedDamage(5);

        BattleTimelinePreviewEntry entry = BattleTimelinePreviewEntry.CreateMonster(0, 0, command);

        Assert.That(entry.SkillValueText, Is.EqualTo("5"));
        Assert.That(entry.SkillEffectDescription, Does.Contain("5 damage"));
        Assert.That(entry.SkillEffectDescription, Does.Not.Contain("4-8"));
        Assert.That(entry.SkillEffectDescription, Does.Not.Contain("\uC218\uCE58"));
    }

    private static MonsterRuntimeData CreateMonsterRuntime()
    {
        MonsterMasterData masterData = new()
        {
            MonsterId = "M_Timeline_Test",
            Name = "Timeline Test Monster",
            Grade = "Common",
            HP = 10
        };

        return new MonsterRuntimeData("Runtime_Timeline_Test", masterData);
    }
}
