using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleTimelineGrindVfxTests
{
    [Test]
    public void BattleTimelineController_SpawnsGrindVfxAtConfiguredWorldPosition()
    {
        GameObject timelineObject = new("TimelineGrindVfxTimeline");
        GameObject vfxPrefab = new("TimelineGrindVfxPrefab");

        try
        {
            BattleTimelineController controller =
                timelineObject.AddComponent<BattleTimelineController>();

            Vector3 expectedPosition = new(-8.3f, -3.3f, 0f);
            SetPrivateField(controller, "timelineGrindVfxPrefab", vfxPrefab);
            SetPrivateField(controller, "timelineGrindVfxPosition", expectedPosition);
            SetPrivateField(controller, "timelineGrindVfxLifeTime", 0f);

            MethodInfo spawnMethod = typeof(BattleTimelineController).GetMethod(
                "SpawnTimelineGrindVfx",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(spawnMethod, Is.Not.Null);

            GameObject spawned = (GameObject)spawnMethod.Invoke(controller, null);

            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.name, Is.EqualTo("TimelineGrindVfxPrefab(Clone)"));
            Assert.That(spawned.transform.position, Is.EqualTo(expectedPosition));

            UnityEngine.Object.DestroyImmediate(spawned);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(vfxPrefab);
            UnityEngine.Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void BattleScene_TimelineControllerUsesWheelSharpenVfx()
    {
        const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";
        const string TimelineControllerGuid = "f9aeefdfd731b8d47b8a5c9b767b0501";
        const string WheelSharpenVfxGuid = "e3b7c6476d04da945829ad77d4086393";

        string sceneYaml = File.ReadAllText(BattleScenePath);
        string controllerNeedle =
            $"m_Script: {{fileID: 11500000, guid: {TimelineControllerGuid}, type: 3}}";
        int controllerIndex = sceneYaml.IndexOf(controllerNeedle, StringComparison.Ordinal);

        Assert.That(controllerIndex, Is.GreaterThanOrEqualTo(0));

        int nextObjectIndex = sceneYaml.IndexOf(
            "--- !u!",
            controllerIndex + controllerNeedle.Length,
            StringComparison.Ordinal);

        Assert.That(nextObjectIndex, Is.GreaterThan(controllerIndex));

        string controllerBlock = sceneYaml.Substring(
            controllerIndex,
            nextObjectIndex - controllerIndex);

        Assert.That(
            controllerBlock,
            Does.Contain(
                $"timelineGrindVfxPrefab: {{fileID: 497387045911033213, guid: {WheelSharpenVfxGuid}, type: 3}}"));
        Assert.That(controllerBlock, Does.Contain("timelineGrindVfxPosition: {x: -8.3, y: -3.3, z: 0}"));
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"Missing private field: {fieldName}");

        field.SetValue(target, value);
    }
}
