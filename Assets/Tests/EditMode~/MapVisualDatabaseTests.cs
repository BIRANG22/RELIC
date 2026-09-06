using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class MapVisualDatabaseTests
{
    private const string MapVisualDatabaseGuid = "3686190205ec417ca06c680bcf53da8c";
    private const string MapVisualControllerGuid = "6ec1b8e7e72644d98d09ccec70eca556";
    private const string MapVisualActorGuid = "31bba6d4fa734d6c9c88ca8b3b28f3a9";
    private const string MapVisualTestPrefabGuid = "54f1f6cc87734fe1961e7aefc6848ddf";
    private const string MapVisualTestSpriteGuid = "822c09c083645384ba86f958acff31dd";
    private const string WorldVfxRendererRootName = "__BattleWorldVfxRenderer";

    [Test]
    public void TryGetEntry_ReturnsConfiguredEntryForTrimmedMapId()
    {
        MapVisualDatabase database = ScriptableObject.CreateInstance<MapVisualDatabase>();

        try
        {
            MapVisualEntry entry = new()
            {
                MapId = "Map_Event_01",
                Spawns = new List<MapVisualSpawnEntry>()
            };

            SetEntries(database, new List<MapVisualEntry> { entry });

            bool found = database.TryGetEntry(" Map_Event_01 ", out MapVisualEntry loaded);

            Assert.That(found, Is.True);
            Assert.That(loaded, Is.SameAs(entry));
        }
        finally
        {
            Object.DestroyImmediate(database);
        }
    }

    [Test]
    public void TryGetEntry_ReturnsFalseForUnknownMapId()
    {
        MapVisualDatabase database = ScriptableObject.CreateInstance<MapVisualDatabase>();

        try
        {
            SetEntries(database, new List<MapVisualEntry>
            {
                new() { MapId = "Map_Event_01" }
            });

            bool found = database.TryGetEntry("Map_Event_02", out MapVisualEntry loaded);

            Assert.That(found, Is.False);
            Assert.That(loaded, Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(database);
        }
    }

    [Test]
    public void ApplyMapVisual_SpawnsConfiguredPrefabsUnderNamedAnchor()
    {
        GameObject room = new("Room");
        GameObject visualRoot = new("VisualRoot");
        GameObject anchor = new("NpcAnchor");
        GameObject prefab = new("NpcPrefab");
        MapVisualDatabase database = ScriptableObject.CreateInstance<MapVisualDatabase>();

        try
        {
            visualRoot.transform.SetParent(room.transform, false);
            anchor.transform.SetParent(visualRoot.transform, false);

            SetEntries(database, new List<MapVisualEntry>
            {
                new()
                {
                    MapId = "Map_Event_01",
                    Spawns = new List<MapVisualSpawnEntry>
                    {
                        new()
                        {
                            Prefab = prefab,
                            AnchorName = "NpcAnchor",
                            LocalPosition = new Vector3(1f, 2f, 3f),
                            LocalEulerAngles = new Vector3(0f, 90f, 0f),
                            LocalScale = new Vector3(2f, 2f, 2f),
                            Active = true
                        }
                    }
                }
            });

            MapVisualController controller = room.AddComponent<MapVisualController>();
            SetPrivateField(controller, "visualRoot", visualRoot.transform);
            SetPrivateField(controller, "anchors", new[] { anchor.transform });
            SetPrivateField(controller, "databaseOverride", database);

            controller.ApplyMapVisual("Map_Event_01");

            Transform spawned = anchor.transform.Find("NpcPrefab");

            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.localPosition, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(spawned.localEulerAngles.y, Is.EqualTo(90f).Within(0.001f));
            Assert.That(spawned.localScale, Is.EqualTo(new Vector3(2f, 2f, 2f)));
        }
        finally
        {
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(room);
        }
    }

    [Test]
    public void ApplyMapVisual_ReplacesPreviousMapVisuals()
    {
        GameObject room = new("Room");
        GameObject visualRoot = new("VisualRoot");
        GameObject firstPrefab = new("FirstNpc");
        GameObject secondPrefab = new("SecondNpc");
        MapVisualDatabase database = ScriptableObject.CreateInstance<MapVisualDatabase>();

        try
        {
            visualRoot.transform.SetParent(room.transform, false);

            SetEntries(database, new List<MapVisualEntry>
            {
                new()
                {
                    MapId = "Map_Event_01",
                    Spawns = new List<MapVisualSpawnEntry>
                    {
                        new() { Prefab = firstPrefab, Active = true }
                    }
                },
                new()
                {
                    MapId = "Map_Event_02",
                    Spawns = new List<MapVisualSpawnEntry>
                    {
                        new() { Prefab = secondPrefab, Active = true }
                    }
                }
            });

            MapVisualController controller = room.AddComponent<MapVisualController>();
            SetPrivateField(controller, "visualRoot", visualRoot.transform);
            SetPrivateField(controller, "databaseOverride", database);

            controller.ApplyMapVisual("Map_Event_01");
            controller.ApplyMapVisual("Map_Event_02");

            Assert.That(visualRoot.transform.Find("FirstNpc"), Is.Null);
            Assert.That(visualRoot.transform.Find("SecondNpc"), Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(firstPrefab);
            Object.DestroyImmediate(secondPrefab);
            Object.DestroyImmediate(room);
        }
    }

    [Test]
    public void TryPlayAction_DispatchesActionToSpawnedActorByVisualObjectId()
    {
        GameObject room = new("Room");
        GameObject visualRoot = new("VisualRoot");
        GameObject prefab = new("CrystalPrefab");
        MapVisualDatabase database = ScriptableObject.CreateInstance<MapVisualDatabase>();

        try
        {
            visualRoot.transform.SetParent(room.transform, false);

            SpriteRenderer renderer = prefab.AddComponent<SpriteRenderer>();
            MapVisualActor actor = prefab.AddComponent<MapVisualActor>();
            SetPrivateField(actor, "actions", new List<MapVisualActionEntry>
            {
                new()
                {
                    ActionId = "shine",
                    TintTarget = renderer,
                    ApplyTint = true,
                    TintColor = Color.cyan,
                    ScaleTarget = prefab.transform,
                    ApplyLocalScale = true,
                    LocalScale = new Vector3(1.25f, 1.25f, 1f)
                }
            });

            SetEntries(database, new List<MapVisualEntry>
            {
                new()
                {
                    MapId = "Map_09",
                    Spawns = new List<MapVisualSpawnEntry>
                    {
                        new()
                        {
                            Prefab = prefab,
                            VisualObjectId = "event_visual_test_crystal",
                            Active = true
                        }
                    }
                }
            });

            MapVisualController controller = room.AddComponent<MapVisualController>();
            SetPrivateField(controller, "visualRoot", visualRoot.transform);
            SetPrivateField(controller, "databaseOverride", database);

            controller.ApplyMapVisual("Map_09");
            bool played = controller.TryPlayAction(" event_visual_test_crystal ", " shine ");

            Transform spawned = visualRoot.transform.Find("CrystalPrefab");
            Assert.That(spawned, Is.Not.Null);

            SpriteRenderer spawnedRenderer = spawned.GetComponent<SpriteRenderer>();
            Assert.That(played, Is.True);
            Assert.That(spawnedRenderer.color, Is.EqualTo(Color.cyan));
            Assert.That(spawned.localScale, Is.EqualTo(new Vector3(1.25f, 1.25f, 1f)));
        }
        finally
        {
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(room);
        }
    }

    [Test]
    public void TryPlayAction_WithoutAnimatorCommand_DoesNotEnableAnimatorDefaultState()
    {
        GameObject actorObject = new("Actor");

        try
        {
            Animator animator = actorObject.AddComponent<Animator>();
            animator.enabled = false;
            MapVisualActor actor = actorObject.AddComponent<MapVisualActor>();
            SetPrivateField(actor, "animator", animator);
            SetPrivateField(actor, "actions", new List<MapVisualActionEntry>
            {
                new() { ActionId = "event_choice_02_success" }
            });

            bool played = actor.TryPlayAction("event_choice_02_success");

            Assert.That(played, Is.True);
            Assert.That(animator.enabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(actorObject);
        }
    }

    [Test]
    public void TryPlayAction_SpawnsVfxThroughWorldProxyInsteadOfActorChild()
    {
        GameObject actorObject = new("Actor");
        GameObject vfxRootObject = new("VfxRoot");
        GameObject vfxPrefab = new("MapActionVfx");

        try
        {
            actorObject.transform.position = new Vector3(2f, 3f, 0f);
            vfxRootObject.transform.SetParent(actorObject.transform, false);
            vfxRootObject.transform.localPosition = new Vector3(1f, 0f, 0f);

            MapVisualActor actor = actorObject.AddComponent<MapVisualActor>();
            SetPrivateField(actor, "vfxRoot", vfxRootObject.transform);
            SetPrivateField(actor, "actions", new List<MapVisualActionEntry>
            {
                new()
                {
                    ActionId = "shine",
                    VfxPrefab = vfxPrefab,
                    VfxLocalPosition = new Vector3(0.5f, 0.25f, -0.1f),
                    VfxLocalEulerAngles = new Vector3(0f, 45f, 0f),
                    VfxLocalScale = new Vector3(2f, 2f, 2f),
                    VfxLifetime = 1f
                }
            });

            bool played = actor.TryPlayAction("shine");

            Assert.That(played, Is.True);
            Assert.That(
                vfxRootObject.GetComponentsInChildren<Transform>(true)
                    .Any(t => t != vfxRootObject.transform && t.name.StartsWith("MapActionVfx")),
                Is.False);

            GameObject proxy = GameObject.Find("MapActionVfx_WorldVfxProxy");
            Assert.That(proxy, Is.Not.Null);
            Assert.That(
                proxy.transform.position,
                Is.EqualTo(vfxRootObject.transform.TransformPoint(new Vector3(0.5f, 0.25f, -0.1f))));

            Transform renderSpace = GameObject.Find(WorldVfxRendererRootName)?.transform.Find("RenderSpace");
            Assert.That(renderSpace, Is.Not.Null);

            Transform renderVfx = renderSpace
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name.StartsWith("MapActionVfx"));

            Assert.That(renderVfx, Is.Not.Null);
            Assert.That(renderVfx.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(renderVfx.localEulerAngles.y, Is.EqualTo(45f).Within(0.001f));
            Assert.That(renderVfx.localScale, Is.EqualTo(new Vector3(2f, 2f, 2f)));
        }
        finally
        {
            DestroyObject(GameObject.Find(WorldVfxRendererRootName));
            Object.DestroyImmediate(vfxPrefab);
            Object.DestroyImmediate(actorObject);
        }
    }

    [Test]
    public void TryPlayAction_ReturnsFalseWhenActorOrActionIsMissing()
    {
        GameObject room = new("Room");

        try
        {
            MapVisualController controller = room.AddComponent<MapVisualController>();

            Assert.That(controller.TryPlayAction("missing_actor", "shine"), Is.False);
            Assert.That(controller.TryPlayAction(string.Empty, "shine"), Is.False);
            Assert.That(controller.TryPlayAction("missing_actor", string.Empty), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(room);
        }
    }

    [Test]
    public void BattleSceneController_HasRoomVisualApplyMethod()
    {
        MethodInfo method = typeof(BattleSceneController).GetMethod(
            "ApplyRoomVisual",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void BattleSceneController_AppliesRoomVisualAfterOpenRoom()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs");

        AssertCallOrderInMethod(
            source,
            "private void OpenStartEvent",
            "OpenRoom(startRoom, \"StartRoom\")",
            "ApplyRoomVisual(startRoom, nodeData)");

        AssertCallOrderInMethod(
            source,
            "private void OpenBattleMap",
            "OpenRoom(battleRoom, \"BattleRoom\")",
            "ApplyRoomVisual(battleRoom, nodeData)");

        AssertCallOrderInMethod(
            source,
            "private void OpenBossBattle",
            "OpenRoom(battleRoom, \"BattleRoom\")",
            "ApplyRoomVisual(battleRoom, nodeData)");

        AssertCallOrderInMethod(
            source,
            "private void OpenRestEvent",
            "OpenRoom(restRoom, \"RestRoom\")",
            "ApplyRoomVisual(restRoom, nodeData)");

        AssertCallOrderInMethod(
            source,
            "private void OpenSpecialEvent",
            "OpenRoom(eventRoom, \"EventRoom\")",
            "ApplyRoomVisual(eventRoom, nodeData)");
    }

    [Test]
    public void BootstrapDataManager_ReferencesMapVisualDatabaseAsset()
    {
        string sceneText = File.ReadAllText("Assets/Project/Scenes/YDM/Bootstrap.unity");

        Assert.That(
            sceneText,
            Does.Contain($"mapVisualDatabase: {{fileID: 11400000, guid: {MapVisualDatabaseGuid}, type: 2}}"));
    }

    [Test]
    public void BattleScene_RuntimeRoomsHaveMapVisualControllers()
    {
        string sceneText = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity");

        Assert.That(CountOccurrences(sceneText, MapVisualControllerGuid), Is.EqualTo(4));
    }

    [Test]
    public void DebugBattleAndBattletest_DoNotReferenceMapVisualController()
    {
        string debugBattleText = File.ReadAllText("Assets/Project/Scenes/YDM/DebugBattle.unity");
        string battleTestText = File.ReadAllText("Assets/Project/Scenes/YDH/Battletest.unity");

        Assert.That(debugBattleText, Does.Not.Contain(MapVisualControllerGuid));
        Assert.That(battleTestText, Does.Not.Contain(MapVisualControllerGuid));
    }

    [Test]
    public void MapVisualTestPrefab_UsesTestSpriteAndActorActions()
    {
        string prefabText = File.ReadAllText("Assets/Project/Data/MapVisual/MapVisual_TestCrystal.prefab");

        Assert.That(prefabText, Does.Contain(MapVisualTestSpriteGuid));
        Assert.That(prefabText, Does.Contain(MapVisualActorGuid));
        Assert.That(prefabText, Does.Contain("visualObjectId: event_visual_test_crystal"));
        Assert.That(prefabText, Does.Contain("ActionId: event_choice_01_success"));
        Assert.That(prefabText, Does.Contain("ActionId: event_choice_01_failure"));
        Assert.That(prefabText, Does.Contain("AnimatorStateName: New Animation"));
        Assert.That(prefabText, Does.Contain("- component: {fileID: 5000000000000000001}"));
        Assert.That(prefabText, Does.Contain("animator: {fileID: 5000000000000000001}"));
        Assert.That(prefabText, Does.Contain("m_Controller: {fileID: 9100000, guid: dc80ed60007fb6d4a94cfc8f5311133c, type: 2}"));
        Assert.That(prefabText, Does.Match("(?s)--- !u!95 &5000000000000000001.*?m_Enabled: 0"));

        for (int choice = 1; choice <= 5; choice++)
        {
            string prefix = $"event_choice_{choice:00}";
            Assert.That(CountOccurrences(prefabText, $"ActionId: {prefix}_success"), Is.EqualTo(1));
            Assert.That(CountOccurrences(prefabText, $"ActionId: {prefix}_failure"), Is.EqualTo(1));
        }
    }

    [Test]
    public void MapVisualDatabase_Map09UsesTestCrystalPrefab()
    {
        string databaseText = File.ReadAllText("Assets/Project/Data/MapVisual/Map Visual Database.asset");

        Assert.That(databaseText, Does.Contain("MapId: Map_09"));
        Assert.That(databaseText, Does.Contain(MapVisualTestPrefabGuid));
        Assert.That(databaseText, Does.Contain("VisualObjectId: event_visual_test_crystal"));
    }

    private static void SetEntries(MapVisualDatabase database, List<MapVisualEntry> entries)
    {
        FieldInfo field = typeof(MapVisualDatabase).GetField(
            "entries",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        field.SetValue(database, entries);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
            return 0;

        int count = 0;
        int index = 0;

        while ((index = text.IndexOf(pattern, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    private static void DestroyObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(target);
        else
            Object.DestroyImmediate(target);
    }

    private static void AssertCallOrderInMethod(
        string source,
        string methodSignature,
        string firstCall,
        string secondCall)
    {
        string body = ExtractMethodBody(source, methodSignature);
        int firstIndex = body.IndexOf(firstCall, System.StringComparison.Ordinal);
        int secondIndex = body.IndexOf(secondCall, System.StringComparison.Ordinal);

        Assert.That(firstIndex, Is.GreaterThanOrEqualTo(0), firstCall);
        Assert.That(secondIndex, Is.GreaterThanOrEqualTo(0), secondCall);
        Assert.That(secondIndex, Is.GreaterThan(firstIndex));
    }

    private static string ExtractMethodBody(string source, string methodSignature)
    {
        int methodIndex = source.IndexOf(methodSignature, System.StringComparison.Ordinal);
        Assert.That(methodIndex, Is.GreaterThanOrEqualTo(0), methodSignature);

        int braceStart = source.IndexOf('{', methodIndex);
        Assert.That(braceStart, Is.GreaterThanOrEqualTo(0), methodSignature);

        int depth = 0;

        for (int i = braceStart; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
                continue;
            }

            if (source[i] != '}')
                continue;

            depth--;

            if (depth == 0)
                return source.Substring(braceStart, i - braceStart + 1);
        }

        Assert.Fail($"Method body not closed: {methodSignature}");
        return string.Empty;
    }
}
