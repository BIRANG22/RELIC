using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class ChestOpenButtonTests
{
    [Test]
    public void DefaultChestSfxTypes_UseConfirmForClickAndBoxOpenForOpen()
    {
        GameObject chestObject = new("Chest");

        try
        {
            ChestOpenButton chest = chestObject.AddComponent<ChestOpenButton>();

            Assert.That(GetPrivateField<SfxType>(chest, "clickSfxType"), Is.EqualTo(SfxType.Confirm));
            Assert.That(GetPrivateField<SfxType>(chest, "openSfxType"), Is.EqualTo(SfxType.BoxOpen));
        }
        finally
        {
            DestroyObject(chestObject);
        }
    }

    [Test]
    public void CreateChestVfxAudioSettings_DisablesEmbeddedAudioRouting()
    {
        BattleVfxSfxEntry settings = InvokePrivateStatic<BattleVfxSfxEntry>(
            typeof(ChestOpenButton),
            "CreateChestVfxAudioSettings");

        Assert.That(settings, Is.Not.Null);
        Assert.That(settings.playSfx, Is.False);
        Assert.That(settings.routeEmbeddedAudioSourcesThroughAudioManager, Is.False);
        Assert.That(settings.removeEmbeddedAudioSources, Is.True);
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

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        method.Invoke(target, null);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        return (T)field.GetValue(target);
    }

    private static T InvokePrivateStatic<T>(System.Type type, string methodName)
    {
        MethodInfo method = type.GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        return (T)method.Invoke(null, null);
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
