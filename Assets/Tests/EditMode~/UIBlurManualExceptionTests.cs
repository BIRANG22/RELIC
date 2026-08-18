using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class UIBlurManualExceptionTests
{
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";
    private const string UIBlurIncludeGuid = "86c33ed51059a434a8a04580426b5f2f";

    [Test]
    public void GetValidBlurredUiRoots_RemovesNullsAndDuplicatesInOrder()
    {
        GameObject first = new("FirstBlurredRoot");
        GameObject second = new("SecondBlurredRoot");

        try
        {
            var roots = new[] { null, first, first, null, second, first };

            var validRoots = UIBlurBackgroundCaptureManager.GetValidBlurredUiRoots(roots);

            Assert.That(validRoots, Is.EqualTo(new[] { first, second }));
        }
        finally
        {
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }
    }

    [Test]
    public void IsTransformUnderAnyRoot_MatchesRootAndChildrenOnly()
    {
        GameObject root = new("BlurredRoot");
        GameObject child = new("BlurredChild");
        GameObject other = new("OtherRoot");

        try
        {
            child.transform.SetParent(root.transform, false);
            var roots = new[] { root };

            Assert.That(
                UIBlurBackgroundCaptureManager.IsTransformUnderAnyRoot(root.transform, roots),
                Is.True);
            Assert.That(
                UIBlurBackgroundCaptureManager.IsTransformUnderAnyRoot(child.transform, roots),
                Is.True);
            Assert.That(
                UIBlurBackgroundCaptureManager.IsTransformUnderAnyRoot(other.transform, roots),
                Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(other);
        }
    }

    [Test]
    public void BlurBackground_ExposesInspectorAssignedBlurredUiRoots()
    {
        GameObject backgroundObject = new("BlurBackground", typeof(RectTransform), typeof(Image), typeof(UIBlurBackground));
        GameObject blurredRoot = new("InspectorAssignedBlurredRoot");

        try
        {
            UIBlurBackground background = backgroundObject.GetComponent<UIBlurBackground>();
            SerializedObject serializedBackground = new(background);
            SerializedProperty rootsProperty = serializedBackground.FindProperty("blurredUiRoots");

            Assert.That(rootsProperty, Is.Not.Null);

            rootsProperty.arraySize = 1;
            rootsProperty.GetArrayElementAtIndex(0).objectReferenceValue = blurredRoot;
            serializedBackground.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(background.BlurredUiRoots, Is.EqualTo(new[] { blurredRoot }));
        }
        finally
        {
            Object.DestroyImmediate(backgroundObject);
            Object.DestroyImmediate(blurredRoot);
        }
    }

    [Test]
    public void BlurBackground_RuntimeBlurredUiRoots_MergeWithInspectorAssignedRoots()
    {
        GameObject backgroundObject = new("BlurBackground", typeof(RectTransform), typeof(Image), typeof(UIBlurBackground));
        GameObject inspectorRoot = new("InspectorAssignedBlurredRoot");
        GameObject runtimeRoot = new("RuntimeBlurredRoot");

        try
        {
            UIBlurBackground background = backgroundObject.GetComponent<UIBlurBackground>();
            SerializedObject serializedBackground = new(background);
            SerializedProperty rootsProperty = serializedBackground.FindProperty("blurredUiRoots");

            Assert.That(rootsProperty, Is.Not.Null);

            rootsProperty.arraySize = 1;
            rootsProperty.GetArrayElementAtIndex(0).objectReferenceValue = inspectorRoot;
            serializedBackground.ApplyModifiedPropertiesWithoutUndo();

            background.SetRuntimeBlurredUiRoots(new[] { null, inspectorRoot, runtimeRoot, runtimeRoot });

            Assert.That(background.BlurredUiRoots, Is.EqualTo(new[] { inspectorRoot, runtimeRoot }));
        }
        finally
        {
            Object.DestroyImmediate(backgroundObject);
            Object.DestroyImmediate(inspectorRoot);
            Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void BlurBackground_ReopenKeepsOwnBlurCanvasOutOfCaptureAndVisibleAfterCapture()
    {
        GameObject backgroundObject = new("BlurBackground", typeof(RectTransform), typeof(Image), typeof(UIBlurBackground));

        try
        {
            UIBlurBackground background = backgroundObject.GetComponent<UIBlurBackground>();
            InvokePrivate(background, "Awake");

            GameObject blurCanvasObject = GetPrivateField<GameObject>(background, "blurCanvasObject");
            RawImage blurGraphic = GetPrivateField<RawImage>(background, "blurGraphic");

            Assert.That(blurCanvasObject, Is.Not.Null);
            Assert.That(blurGraphic, Is.Not.Null);

            InvokePrivate(background, "HideBlurCanvasForCapture");
            Assert.That(blurCanvasObject.activeSelf, Is.False);

            blurGraphic.canvasRenderer.cull = true;

            InvokePrivate(background, "ShowBlurCanvasAfterCapture");

            Assert.That(blurCanvasObject.activeSelf, Is.True);
            Assert.That(
                blurGraphic.canvasRenderer.cull,
                Is.False,
                "A reopened blur background must clear the cull state left by the previous capture cycle.");
        }
        finally
        {
            Object.DestroyImmediate(backgroundObject);
        }
    }

    [Test]
    public void BlurBackground_CreatesBlurSurfaceInsideOwningBackground()
    {
        GameObject backgroundObject = new("BlurBackground", typeof(RectTransform), typeof(Image), typeof(UIBlurBackground));
        GameObject existingChild = new("ExistingBackgroundChild", typeof(RectTransform));

        try
        {
            existingChild.transform.SetParent(backgroundObject.transform, false);

            UIBlurBackground background = backgroundObject.GetComponent<UIBlurBackground>();
            InvokePrivate(background, "Awake");

            GameObject blurCanvasObject = GetPrivateField<GameObject>(background, "blurCanvasObject");
            RawImage blurGraphic = GetPrivateField<RawImage>(background, "blurGraphic");

            Assert.That(blurCanvasObject, Is.Not.Null);
            Assert.That(blurGraphic, Is.Not.Null);
            Assert.That(
                blurCanvasObject.transform.parent,
                Is.EqualTo(backgroundObject.transform),
                "The blur output must share the owning background hierarchy so it renders behind Equip_panel content instead of disappearing under global UI.");
            Assert.That(
                blurCanvasObject.transform.GetSiblingIndex(),
                Is.EqualTo(0),
                "The blur output should be the first background child so panel controls remain above it.");
        }
        finally
        {
            Object.DestroyImmediate(backgroundObject);
        }
    }

    [Test]
    public void CaptureManager_PrepareExplicitUIForCapture_MovesParentRootCanvasLayerForChildRoot()
    {
        GameObject managerObject = new("CaptureManager");
        GameObject cameraObject = new("CaptureCamera", typeof(Camera));
        GameObject canvasObject = new("ChoiceCanvas", typeof(RectTransform), typeof(Canvas));
        GameObject shopRoot = new("ShopPanel", typeof(RectTransform), typeof(Image));

        try
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            Assert.That(uiLayer, Is.GreaterThanOrEqualTo(0));
            Assert.That(uiLayer, Is.Not.EqualTo(0));

            canvasObject.layer = uiLayer;
            shopRoot.layer = uiLayer;
            shopRoot.transform.SetParent(canvasObject.transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            UIBlurBackgroundCaptureManager manager = managerObject.AddComponent<UIBlurBackgroundCaptureManager>();
            Camera camera = cameraObject.GetComponent<Camera>();

            InvokePrivate(
                manager,
                "PrepareExplicitUIForCapture",
                camera,
                1 << 0,
                new[] { shopRoot });

            Assert.That(
                canvasObject.layer,
                Is.EqualTo(0),
                "A child UI root must move its parent root canvas into the capture mask, or Screen Space Camera capture can skip the whole panel.");
            Assert.That(shopRoot.layer, Is.EqualTo(0));
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));

            InvokePrivate(manager, "RestoreCaptureIncludedUI");

            Assert.That(canvasObject.layer, Is.EqualTo(uiLayer));
            Assert.That(shopRoot.layer, Is.EqualTo(uiLayer));
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void LobbyScene_DoesNotUseAutomaticBlurIncludeMarkers()
    {
        string sceneYaml = File.ReadAllText(LobbyScenePath);

        Assert.That(sceneYaml, Does.Not.Contain(UIBlurIncludeGuid));
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"{methodName} should exist for the blur lifecycle.");
        method.Invoke(target, null);
    }

    private static void InvokePrivate(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"{methodName} should exist for the blur lifecycle.");
        method.Invoke(target, arguments);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
        where T : class
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} should exist on {target.GetType().Name}.");
        return field.GetValue(target) as T;
    }
}
