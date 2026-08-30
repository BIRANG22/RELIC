using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class ChestOpenButtonTests
{
    [Test]
    public void DefaultChestSfxIds_UseConfirmForClickAndBoxOpenForOpen()
    {
        GameObject chestObject = new("Chest");

        try
        {
            ChestOpenButton chest = chestObject.AddComponent<ChestOpenButton>();

            Assert.That(GetPrivateField<string>(chest, "clickSfxId"), Is.EqualTo(AudioIds.Sfx.Confirm));
            Assert.That(GetPrivateField<string>(chest, "openSfxId"), Is.EqualTo(AudioIds.Sfx.BoxOpen));
        }
        finally
        {
            DestroyObject(chestObject);
        }
    }

    [Test]
    public void VfxSortingLayer_UsesChestSpriteLayer()
    {
        GameObject chestObject = new("Chest");
        GameObject underObject = new("chest_under");
        GameObject openObject = new("chest_open_0");

        try
        {
            ChestOpenButton chest = chestObject.AddComponent<ChestOpenButton>();
            SpriteRenderer under = underObject.AddComponent<SpriteRenderer>();
            SpriteRenderer open = openObject.AddComponent<SpriteRenderer>();

            under.sortingLayerName = "Default";
            open.sortingLayerName = "Default";

            SetPrivateField(chest, "chestUnder", under);
            SetPrivateField(chest, "chestOpen", open);
            SetPrivateField(chest, "vfxSortingLayerName", "Unit");

            string sortingLayerName = InvokePrivateString(chest, "GetVfxSortingLayerName");

            Assert.That(sortingLayerName, Is.EqualTo("Default"));
        }
        finally
        {
            DestroyObject(chestObject);
            DestroyObject(underObject);
            DestroyObject(openObject);
        }
    }

    [Test]
    public void EventRoomController_OnEnable_HidesNextButton()
    {
        GameObject eventRoomObject = new("EventRoom");
        GameObject nextButtonObject = new("NextButton");

        try
        {
            EventRoomController controller = eventRoomObject.AddComponent<EventRoomController>();
            nextButtonObject.SetActive(true);

            SetPrivateField(controller, "nextButtonRoot", nextButtonObject);
            InvokePrivate(controller, "OnEnable");

            Assert.That(nextButtonObject.activeSelf, Is.False);
        }
        finally
        {
            DestroyObject(eventRoomObject);
            DestroyObject(nextButtonObject);
        }
    }

    [Test]
    public void EventRoomController_NotifyChestOpened_ShowsNextButton()
    {
        GameObject eventRoomObject = new("EventRoom");
        GameObject nextButtonObject = new("NextButton");

        try
        {
            EventRoomController controller = eventRoomObject.AddComponent<EventRoomController>();
            nextButtonObject.SetActive(false);

            SetPrivateField(controller, "nextButtonRoot", nextButtonObject);
            controller.NotifyChestOpened();

            Assert.That(nextButtonObject.activeSelf, Is.True);
        }
        finally
        {
            DestroyObject(eventRoomObject);
            DestroyObject(nextButtonObject);
        }
    }

    [Test]
    public void EventRoomController_LoadEvent02A_ShowsNextButton()
    {
        GameObject eventRoomObject = new("EventRoom");
        GameObject nextButtonObject = new("NextButton");

        try
        {
            EventRoomController controller = eventRoomObject.AddComponent<EventRoomController>();
            nextButtonObject.SetActive(false);

            SetPrivateField(controller, "nextButtonRoot", nextButtonObject);
            InvokePrivate(
                controller,
                "LoadEventDefinition",
                new EventDefinition { EventId = "Event_02_A" },
                string.Empty);

            Assert.That(nextButtonObject.activeSelf, Is.True);
        }
        finally
        {
            DestroyObject(eventRoomObject);
            DestroyObject(nextButtonObject);
        }
    }

    [Test]
    public void EventRoomController_LoadRegularDataEvent_HidesNextButton()
    {
        GameObject eventRoomObject = new("EventRoom");
        GameObject nextButtonObject = new("NextButton");

        try
        {
            EventRoomController controller = eventRoomObject.AddComponent<EventRoomController>();
            nextButtonObject.SetActive(true);

            SetPrivateField(controller, "nextButtonRoot", nextButtonObject);
            InvokePrivate(
                controller,
                "LoadEventDefinition",
                new EventDefinition { EventId = "Event_06" },
                string.Empty);

            Assert.That(nextButtonObject.activeSelf, Is.False);
        }
        finally
        {
            DestroyObject(eventRoomObject);
            DestroyObject(nextButtonObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static string InvokePrivateString(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        return (string)method.Invoke(target, null);
    }

    private static void InvokePrivate(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        method.Invoke(target, args);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        return (T)field.GetValue(target);
    }

    private static void DestroyObject(UnityEngine.Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(target);
        else
            UnityEngine.Object.DestroyImmediate(target);
    }
}
