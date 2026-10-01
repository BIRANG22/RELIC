using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BattleUiCornerFeedbackTests
{
    private const string SkillPrefabPath =
        "Assets/Project/PrefabsR/UI/BattleUiSkillClickFeedback.prefab";
    private const string TimelinePrefabPath =
        "Assets/Project/PrefabsR/UI/BattleUiTimelineRegisterFeedback.prefab";

    [Test]
    public void SkillClickFeedback_UsesFourCornersAndExpands()
    {
        BattleUiCornerFeedback feedback = LoadFeedback(SkillPrefabPath);

        Assert.That(ReadField<BattleUiCornerFeedbackStyle>(feedback, "style"),
            Is.EqualTo(BattleUiCornerFeedbackStyle.FourCorners));
        Assert.That(ReadField<float>(feedback, "endScale"),
            Is.GreaterThan(ReadField<float>(feedback, "startScale")));
    }

    [Test]
    public void TimelineFeedback_UsesRadialBurstAndExpands()
    {
        BattleUiCornerFeedback feedback = LoadFeedback(TimelinePrefabPath);

        Assert.That(ReadField<BattleUiCornerFeedbackStyle>(feedback, "style"),
            Is.EqualTo(BattleUiCornerFeedbackStyle.RadialBurst));
        Assert.That(ReadField<int>(feedback, "rayCount"), Is.GreaterThanOrEqualTo(12));
        Assert.That(ReadField<float>(feedback, "rayMaxLength"),
            Is.GreaterThan(ReadField<float>(feedback, "rayMinLength")));
        Assert.That(ReadField<float>(feedback, "rayMaxLength"), Is.LessThanOrEqualTo(46f));
        Assert.That(ReadField<float>(feedback, "endScale"),
            Is.GreaterThan(ReadField<float>(feedback, "startScale")));
    }

    private static BattleUiCornerFeedback LoadFeedback(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab, Is.Not.Null, path);

        BattleUiCornerFeedback feedback = prefab.GetComponent<BattleUiCornerFeedback>();
        Assert.That(feedback, Is.Not.Null, path);
        return feedback;
    }

    private static T ReadField<T>(BattleUiCornerFeedback target, string fieldName)
    {
        FieldInfo field = typeof(BattleUiCornerFeedback).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return (T)field.GetValue(target);
    }
}
