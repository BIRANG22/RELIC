using System.Collections;
using NUnit.Framework;
using Relic.Gameplay.Data;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class EventDiceRollPresentationTests
{
    [Test]
    public void EventDiceRollPresenter_UsesSoundIdInsteadOfDirectAudioClip()
    {
        GameObject root = new("DicePresenter");

        try
        {
            EventDiceRollPresenter presenter = root.AddComponent<EventDiceRollPresenter>();

            Assert.That(GetPrivateField<string>(presenter, "rollSfxId"), Is.EqualTo(AudioIds.Sfx.BattleEventDiceRoll));
            Assert.That(GetPrivateFieldInfo(typeof(EventDiceRollPresenter), "rollSound"), Is.Null);
            Assert.That(GetPrivateFieldInfo(typeof(EventDiceRollPresenter), "audioSource"), Is.Null);
            AssertSoundIdField(typeof(EventDiceRollPresenter), "rollSfxId", SoundCategory.Sfx);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void TimedObjectRevealSequence_RevealTarget_UsesSoundIdInsteadOfDirectAudioClip()
    {
        Assert.That(GetPublicFieldInfo(typeof(TimedObjectRevealSequence.RevealTarget), "revealSound"), Is.Null);
        AssertSoundIdField(typeof(TimedObjectRevealSequence.RevealTarget), "revealSoundId", SoundCategory.Sfx);
    }

    [Test]
    public void ExecuteDiceChoice_StoresThreeDiceFacesAndTotal()
    {
        EventData choice = new()
        {
            ChoiceType = "Dice",
            ResultType = "RollTable",
            ResultTarget = "레드 더스티움",
            ResultValue = "RT003"
        };

        EventChoiceExecutionContext context = new()
        {
            RollDiceFaces = () => new[] { 1, 4, 6 },
            GrantRemnant = (int amount, out string message) =>
            {
                message = $"레드 더스티움 {amount} 획득";
                return true;
            }
        };

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.That(result.DiceRoll, Is.EqualTo(11));
        Assert.That(result.DiceFaces, Is.EquivalentTo(new[] { 1, 4, 6 }));
        Assert.That(result.ResultMessage, Does.Contain("주사위 결과: 11"));
    }

    [Test]
    public void EventRoomController_InstantiatesDiceRollPresenterPrefabWhenSceneInstanceIsMissing()
    {
        GameObject controllerObject = new("EventRoomController");
        GameObject dataEventRoot = new("DataEventRoot", typeof(RectTransform));
        GameObject prefabObject = new("DicePresenterPrefab", typeof(RectTransform));
        prefabObject.AddComponent<EventDiceRollPresenter>();

        try
        {
            EventRoomController controller = controllerObject.AddComponent<EventRoomController>();
            SetPrivateField(controller, "dataEventRoot", dataEventRoot);
            SetPrivateField(controller, "diceRollPresenterPrefab", prefabObject.GetComponent<EventDiceRollPresenter>());

            InvokePrivateMethod(controller, "EnsureDiceRollPresenter");

            EventDiceRollPresenter spawned =
                dataEventRoot.GetComponentInChildren<EventDiceRollPresenter>(true);
            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.gameObject.scene.IsValid(), Is.True);
            Assert.That(spawned.transform.parent, Is.EqualTo(dataEventRoot.transform));
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(dataEventRoot);
            Object.DestroyImmediate(prefabObject);
        }
    }

    [UnityTest]
    public IEnumerator EventRoomController_BeginDiceRollKeepsPresenterVisibleUntilRollCompletes()
    {
        GameObject controllerObject = new("EventRoomController");
        GameObject dataEventRoot = new("DataEventRoot", typeof(RectTransform));
        dataEventRoot.transform.SetParent(controllerObject.transform, false);
        GameObject presenterObject = new("DicePresenter", typeof(RectTransform));
        presenterObject.transform.SetParent(dataEventRoot.transform, false);
        EventDiceRollPresenter presenter = presenterObject.AddComponent<EventDiceRollPresenter>();
        Image image = CreateDiceImage(presenterObject.transform, "Die1");
        Sprite[] sprites =
        {
            CreateSprite("One"),
            CreateSprite("Two"),
            CreateSprite("Three"),
            CreateSprite("Four"),
            CreateSprite("Five"),
            CreateSprite("Six")
        };

        try
        {
            presenter.ConfigureForTest(new[] { image }, sprites, 0.02f);
            EventRoomController controller = controllerObject.AddComponent<EventRoomController>();
            SetPrivateField(controller, "dataEventRoot", dataEventRoot);
            SetPrivateField(controller, "diceRollPresenter", presenter);

            EventData choice = new()
            {
                ChoiceType = "Dice",
                ResultType = "None"
            };

            bool started = (bool)InvokePrivateMethod(controller, "TryBeginDiceRollChoice", choice);
            Assert.That(started, Is.True);
            Assert.That(presenterObject.activeSelf, Is.True);

            yield return null;

            Assert.That(presenterObject.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
            for (int i = 0; i < sprites.Length; i++)
                Object.DestroyImmediate(sprites[i].texture);
        }
    }

    [UnityTest]
    public IEnumerator EventDiceRollPresenter_StopsOnResultSpritesAfterRollDuration()
    {
        GameObject root = new("DicePresenter");
        EventDiceRollPresenter presenter = root.AddComponent<EventDiceRollPresenter>();

        Image firstImage = CreateDiceImage(root.transform, "Die1");
        Image secondImage = CreateDiceImage(root.transform, "Die2");
        Image thirdImage = CreateDiceImage(root.transform, "Die3");

        Sprite[] sprites =
        {
            CreateSprite("One"),
            CreateSprite("Two"),
            CreateSprite("Three"),
            CreateSprite("Four"),
            CreateSprite("Five"),
            CreateSprite("Six")
        };

        bool completed = false;

        try
        {
            presenter.ConfigureForTest(
                new[] { firstImage, secondImage, thirdImage },
                sprites,
                0.02f);

            presenter.Play(new[] { 2, 5, 6 }, () => completed = true);

            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(completed, Is.True);
            Assert.That(firstImage.sprite, Is.SameAs(sprites[1]));
            Assert.That(secondImage.sprite, Is.SameAs(sprites[4]));
            Assert.That(thirdImage.sprite, Is.SameAs(sprites[5]));
        }
        finally
        {
            Object.DestroyImmediate(root);
            for (int i = 0; i < sprites.Length; i++)
                Object.DestroyImmediate(sprites[i].texture);
        }
    }

    [UnityTest]
    public IEnumerator EventDiceRollPresenter_HideImmediateStopsActiveRollAndDeactivatesPresenter()
    {
        GameObject root = new("DicePresenter");
        EventDiceRollPresenter presenter = root.AddComponent<EventDiceRollPresenter>();

        Image firstImage = CreateDiceImage(root.transform, "Die1");
        Sprite[] sprites =
        {
            CreateSprite("One"),
            CreateSprite("Two"),
            CreateSprite("Three"),
            CreateSprite("Four"),
            CreateSprite("Five"),
            CreateSprite("Six")
        };

        bool completed = false;

        try
        {
            presenter.ConfigureForTest(
                new[] { firstImage },
                sprites,
                1f);

            presenter.Play(new[] { 6 }, () => completed = true);

            yield return null;

            Assert.That(root.activeSelf, Is.True);

            presenter.HideImmediate();

            Assert.That(root.activeSelf, Is.False);

            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(completed, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
            for (int i = 0; i < sprites.Length; i++)
                Object.DestroyImmediate(sprites[i].texture);
        }
    }

    [UnityTest]
    public IEnumerator EventDiceRollPresenter_PlayFromHostCanRunWhenPresenterStartsInactive()
    {
        GameObject hostObject = new("CoroutineHost");
        GameObject root = new("DicePresenter");
        EventDiceRollPresenter presenter = root.AddComponent<EventDiceRollPresenter>();

        Image firstImage = CreateDiceImage(root.transform, "Die1");
        Sprite[] sprites =
        {
            CreateSprite("One"),
            CreateSprite("Two"),
            CreateSprite("Three"),
            CreateSprite("Four"),
            CreateSprite("Five"),
            CreateSprite("Six")
        };

        bool completed = false;

        try
        {
            MonoBehaviour host = hostObject.AddComponent<TestCoroutineHost>();
            presenter.ConfigureForTest(
                new[] { firstImage },
                sprites,
                0.02f);
            root.SetActive(false);

            yield return host.StartCoroutine(presenter.PlayFromHost(new[] { 4 }, () => completed = true));

            Assert.That(root.activeSelf, Is.True);
            Assert.That(completed, Is.True);
            Assert.That(firstImage.sprite, Is.SameAs(sprites[3]));
        }
        finally
        {
            Object.DestroyImmediate(hostObject);
            Object.DestroyImmediate(root);
            for (int i = 0; i < sprites.Length; i++)
                Object.DestroyImmediate(sprites[i].texture);
        }
    }

    [UnityTest]
    public IEnumerator EventDiceRollPresenter_PlayFromHostShowsDiceBeforeAnimatorCanAdvance()
    {
        GameObject hostObject = new("CoroutineHost");
        GameObject root = new("DicePresenter");
        EventDiceRollPresenter presenter = root.AddComponent<EventDiceRollPresenter>();
        Animator animator = root.AddComponent<Animator>();

        Image firstImage = CreateDiceImage(root.transform, "Die1");
        Sprite[] sprites =
        {
            CreateSprite("One"),
            CreateSprite("Two"),
            CreateSprite("Three"),
            CreateSprite("Four"),
            CreateSprite("Five"),
            CreateSprite("Six")
        };

        try
        {
            MonoBehaviour host = hostObject.AddComponent<TestCoroutineHost>();
            presenter.ConfigureForTest(
                new[] { firstImage },
                sprites,
                0.02f);
            SetPrivateField(presenter, "animator", animator);
            root.SetActive(false);

            IEnumerator routine = presenter.PlayFromHost(new[] { 5 }, null);
            Assert.That(routine.MoveNext(), Is.True);

            Assert.That(root.activeSelf, Is.True);
            Assert.That(firstImage.enabled, Is.True);
            Assert.That(firstImage.sprite, Is.SameAs(sprites[4]));

            yield return host.StartCoroutine(routine);
        }
        finally
        {
            Object.DestroyImmediate(hostObject);
            Object.DestroyImmediate(root);
            for (int i = 0; i < sprites.Length; i++)
                Object.DestroyImmediate(sprites[i].texture);
        }
    }

    private sealed class TestCoroutineHost : MonoBehaviour
    {
    }

    private static Image CreateDiceImage(Transform parent, string name)
    {
        GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        return imageObject.GetComponent<Image>();
    }

    private static Sprite CreateSprite(string name)
    {
        Texture2D texture = new(1, 1);
        texture.name = name + "Texture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f));
        sprite.name = name;
        return sprite;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static object InvokePrivateMethod(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        return method.Invoke(target, args);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = GetPrivateFieldInfo(target.GetType(), fieldName);
        Assert.That(field, Is.Not.Null);
        return (T)field.GetValue(target);
    }

    private static FieldInfo GetPrivateFieldInfo(System.Type type, string fieldName)
    {
        return type.GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private static FieldInfo GetPublicFieldInfo(System.Type type, string fieldName)
    {
        return type.GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public);
    }

    private static void AssertSoundIdField(
        System.Type type,
        string fieldName,
        SoundCategory category)
    {
        FieldInfo field =
            GetPrivateFieldInfo(type, fieldName) ??
            GetPublicFieldInfo(type, fieldName);

        Assert.That(field, Is.Not.Null);
        Assert.That(field.FieldType, Is.EqualTo(typeof(string)));

        SoundIdAttribute attribute = field.GetCustomAttribute<SoundIdAttribute>();
        Assert.That(attribute, Is.Not.Null);
        Assert.That(attribute.Category, Is.EqualTo(category));
    }
}
