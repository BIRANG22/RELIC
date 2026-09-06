using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class MapRoomControllerTests
{
    [Test]
    public void RefreshForMap_WhenMapIdSkipsAllyRoot_DisablesAllyRoot()
    {
        GameObject root = new("MapRoomController_TestRoot");
        GameObject allyRoot = new("AllyRoot");
        allyRoot.transform.SetParent(root.transform, false);
        MapRoomController controller = root.AddComponent<MapRoomController>();

        try
        {
            SetPrivateField(controller, "allyRoot", allyRoot.transform);
            SetPrivateField(controller, "skipAllyRootMapIds", new List<string> { "Map_26" });

            controller.RefreshForMap("Map_26");

            Assert.That(allyRoot.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void RefreshForMap_WhenMapIdDoesNotSkipAllyRoot_EnablesAllyRoot()
    {
        GameObject root = new("MapRoomController_TestRoot");
        GameObject allyRoot = new("AllyRoot");
        allyRoot.transform.SetParent(root.transform, false);
        allyRoot.SetActive(false);
        MapRoomController controller = root.AddComponent<MapRoomController>();

        try
        {
            SetPrivateField(controller, "allyRoot", allyRoot.transform);
            SetPrivateField(controller, "skipAllyRootMapIds", new List<string> { "Map_26" });

            controller.RefreshForMap("Map_01");

            Assert.That(allyRoot.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void RefreshForMapSelection_WhenMapIdSkipsAllyRoot_EnablesAllyRoot()
    {
        GameObject root = new("MapRoomController_TestRoot");
        GameObject allyRoot = new("AllyRoot");
        allyRoot.transform.SetParent(root.transform, false);
        allyRoot.SetActive(false);
        MapRoomController controller = root.AddComponent<MapRoomController>();

        try
        {
            SetPrivateField(controller, "allyRoot", allyRoot.transform);
            SetPrivateField(controller, "skipAllyRootMapIds", new List<string> { "Map_26" });

            controller.RefreshForMapSelection("Map_26");

            Assert.That(allyRoot.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }
}
