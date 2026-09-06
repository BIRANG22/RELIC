using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleMoveSelectionToggleTests
{
    [Test]
    public void CharacterSelection_DoesNotKeepDefaultMoveAutoSelectionPath()
    {
        MethodInfo defaultMoveRequest = typeof(BattleTimelineController).GetMethod(
            "RequestDefaultMoveSkillAfterCharacterPanelOpened",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(defaultMoveRequest, Is.Null);
    }

    [Test]
    public void ToggleMoveSkillSelection_WhenMoveIsNotActive_SelectsMove()
    {
        GameObject timelineObject = new("TimelineMoveSelect");

        try
        {
            BattleTimelineController timeline = timelineObject.AddComponent<BattleTimelineController>();
            CharacterRuntimeData runtime = CreateRuntime();
            SkillMasterData moveSkill = CreateMoveSkill();

            bool isSelected = timeline.ToggleMoveSkillSelection(runtime, moveSkill);

            Assert.That(isSelected, Is.True);
            Assert.That(GetPrivateField<SkillMasterData>(timeline, "selectedSkill"), Is.SameAs(moveSkill));
        }
        finally
        {
            Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void ToggleMoveSkillSelection_WhenSameMoveIsActive_ClearsSelection()
    {
        GameObject timelineObject = new("TimelineMoveToggle");

        try
        {
            BattleTimelineController timeline = timelineObject.AddComponent<BattleTimelineController>();
            CharacterRuntimeData runtime = CreateRuntime();
            SkillMasterData moveSkill = CreateMoveSkill();

            SetPrivateField(timeline, "selectedCharacter", runtime);
            SetPrivateField(timeline, "selectedSkill", moveSkill);

            bool isSelected = timeline.ToggleMoveSkillSelection(runtime, moveSkill);

            Assert.That(isSelected, Is.False);
            Assert.That(GetPrivateField<SkillMasterData>(timeline, "selectedSkill"), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(timelineObject);
        }
    }

    private static TValue GetPrivateField<TValue>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        return (TValue)field.GetValue(target);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        field.SetValue(target, value);
    }

    private static CharacterRuntimeData CreateRuntime()
    {
        return new CharacterRuntimeData
        {
            CharacterId = "Char_Move_Toggle",
            CurrentHP = 10,
            MaxHP = 10
        };
    }

    private static SkillMasterData CreateMoveSkill()
    {
        return new SkillMasterData
        {
            SkillId = "S_Move_Toggle",
            Category = Category.Move,
            TimelineNotation = TimelineActionType.Move,
            RangeType = RangeType.Selection
        };
    }
}
