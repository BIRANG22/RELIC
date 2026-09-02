using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class StageBackgroundControllerTests
{
    [Test]
    public void BattleSceneController_UsesRoomAgnosticBackgroundMethodInsteadOfSerializedBackgroundFields()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Type type = typeof(BattleSceneController);

        Assert.That(type.GetField("stageBackgroundController", flags), Is.Null);
        Assert.That(type.GetField("normalBattleBackground", flags), Is.Null);
        Assert.That(type.GetField("bossBattleBackground", flags), Is.Null);
        Assert.That(type.GetMethod("SetBattleBackground", flags), Is.Null);

        MethodInfo method = type.GetMethod("ShowRoomBackground", flags);
        Assert.That(method, Is.Not.Null);
        Assert.That(method.GetParameters().Length, Is.EqualTo(3));
    }

    [TestCase(0, "St1_00")]
    [TestCase(2, "St1_00")]
    [TestCase(3, "St1_01")]
    [TestCase(6, "St1_01")]
    [TestCase(7, "St1_02")]
    [TestCase(9, "St1_02")]
    public void ShowForLayer_SpawnsPrefabForConfiguredRowRange(int layerIndex, string expectedName)
    {
        Fixture fixture = CreateFixture();

        try
        {
            fixture.Controller.ShowForLayer(layerIndex);

            Assert.That(fixture.SpawnRoot.childCount, Is.EqualTo(1));
            Assert.That(fixture.SpawnRoot.GetChild(0).name, Is.EqualTo(expectedName));
        }
        finally
        {
            fixture.Destroy();
        }
    }

    [Test]
    public void ShowForLayer_WhenPrefabDoesNotChange_ReusesCurrentInstance()
    {
        Fixture fixture = CreateFixture();

        try
        {
            fixture.Controller.ShowForLayer(0);
            Transform firstInstance = fixture.SpawnRoot.GetChild(0);

            fixture.Controller.ShowForLayer(2);

            Assert.That(fixture.SpawnRoot.childCount, Is.EqualTo(1));
            Assert.That(fixture.SpawnRoot.GetChild(0), Is.SameAs(firstInstance));
        }
        finally
        {
            fixture.Destroy();
        }
    }

    [Test]
    public void ShowForLayer_WhenNoRangeMatches_RemovesCurrentInstance()
    {
        Fixture fixture = CreateFixture();

        try
        {
            fixture.Controller.ShowForLayer(0);
            LogAssert.Expect(LogType.Warning, "[StageBackgroundController] No background is configured for row 11.");
            fixture.Controller.ShowForLayer(10);

            Assert.That(fixture.SpawnRoot.childCount, Is.Zero);
        }
        finally
        {
            fixture.Destroy();
        }
    }

    [Test]
    public void ShowForMap_WhenMapIdRangeExists_UsesItBeforeLayerRange()
    {
        Fixture fixture = CreateFixture();
        GameObject restBackground = new("RestBackground");

        try
        {
            fixture.Ranges.Add(
                new StageBackgroundController.BackgroundRange(1, 1, "Map_26", restBackground));

            fixture.Controller.ShowForMap("Map_26", 0);

            Assert.That(fixture.SpawnRoot.childCount, Is.EqualTo(1));
            Assert.That(fixture.SpawnRoot.GetChild(0).name, Is.EqualTo("RestBackground"));
        }
        finally
        {
            Object.DestroyImmediate(restBackground);
            fixture.Destroy();
        }
    }

    private static Fixture CreateFixture()
    {
        GameObject root = new("StageBackgroundController_TestRoot");
        GameObject spawnRootObject = new("SpawnRoot");
        spawnRootObject.transform.SetParent(root.transform, false);

        StageBackgroundController controller = root.AddComponent<StageBackgroundController>();
        SetPrivateField(controller, "spawnRoot", spawnRootObject.transform);

        GameObject st100 = new("St1_00");
        GameObject st101 = new("St1_01");
        GameObject st102 = new("St1_02");

        List<StageBackgroundController.BackgroundRange> ranges = new()
        {
            new StageBackgroundController.BackgroundRange(1, 3, st100),
            new StageBackgroundController.BackgroundRange(4, 7, st101),
            new StageBackgroundController.BackgroundRange(8, 10, st102)
        };
        SetPrivateField(controller, "backgroundRanges", ranges);

        return new Fixture(root, spawnRootObject.transform, controller, ranges, st100, st101, st102);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }

    private sealed class Fixture
    {
        private readonly GameObject root;
        private readonly GameObject[] prefabs;

        public Transform SpawnRoot { get; }
        public GameObject Root { get; }
        public StageBackgroundController Controller { get; }
        public List<StageBackgroundController.BackgroundRange> Ranges { get; }

        public Fixture(
            GameObject root,
            Transform spawnRoot,
            StageBackgroundController controller,
            List<StageBackgroundController.BackgroundRange> ranges,
            params GameObject[] prefabs)
        {
            this.root = root;
            this.prefabs = prefabs;
            Root = root;
            SpawnRoot = spawnRoot;
            Controller = controller;
            Ranges = ranges;
        }

        public void Destroy()
        {
            Object.DestroyImmediate(root);

            for (int i = 0; i < prefabs.Length; i++)
                Object.DestroyImmediate(prefabs[i]);
        }
    }
}
