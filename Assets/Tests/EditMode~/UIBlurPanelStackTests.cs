using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIBlurPanelStackTests
{
    [TearDown]
    public void TearDown()
    {
        UIBlurBackgroundManager[] managers = Object.FindObjectsByType<UIBlurBackgroundManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < managers.Length; i++)
            Object.DestroyImmediate(managers[i].gameObject);

        foreach (GameObject root in Object.FindObjectsByType<GameObject>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (root != null &&
                (root.name.StartsWith("BlurStackTest", System.StringComparison.Ordinal) ||
                 root.name == "Setting_upper" ||
                 root.name == "BlurredUIRoot" ||
                 root.name == "SharpUIRoot"))
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    [Test]
    public void EnsureForPanel_UsesPreconfiguredPanelRootComponent()
    {
        GameObject panel = CreatePanel("BlurStackTestPanel", null, out _);
        UIBlurBackground blur = AddBlurBackground(panel);

        Assert.That(blur.PanelRoot, Is.SameAs(panel));
        Assert.That(UIBlurBackground.EnsureForPanel(panel), Is.SameAs(blur));
    }

    [Test]
    public void EnsureForPanel_DoesNotAddBlurComponentsAtRuntime()
    {
        GameObject panel = new("BlurStackTestPanel", typeof(RectTransform), typeof(Image));

        UIBlurBackground blur = UIBlurBackground.EnsureForPanel(panel);

        Assert.That(blur, Is.Null);
        Assert.That(panel.GetComponent<UIBlurBackground>(), Is.Null);
        Assert.That(panel.GetComponent<Canvas>(), Is.Null);
        Assert.That(panel.GetComponent<GraphicRaycaster>(), Is.Null);
    }

    [Test]
    public void Request_KeepsAllPresentationCanvasesEnabledWithoutMovingHierarchy()
    {
        GameObject canvas = new("BlurStackTestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        GameObject root = new("BlurStackTestRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        GameObject panelA = CreatePanel("BlurStackTestPanelA", root.transform, out Canvas canvasA);
        GameObject panelB = CreatePanel("BlurStackTestPanelB", root.transform, out Canvas canvasB);
        int panelASibling = panelA.transform.GetSiblingIndex();
        int panelBSibling = panelB.transform.GetSiblingIndex();

        UIBlurBackground blurA = AddBlurBackground(panelA, canvasA);
        AddBlurBackground(panelB, canvasB);
        panelA.SetActive(true);
        panelB.SetActive(true);

        Assert.That(UIBlurBackgroundManager.Instance.TopRequester, Is.Not.SameAs(blurA));
        Assert.That(panelA.transform.parent, Is.EqualTo(root.transform));
        Assert.That(panelB.transform.parent, Is.EqualTo(root.transform));
        Assert.That(panelA.transform.GetSiblingIndex(), Is.EqualTo(panelASibling));
        Assert.That(panelB.transform.GetSiblingIndex(), Is.EqualTo(panelBSibling));
        Assert.That(canvasA.enabled, Is.True);
        Assert.That(canvasB.enabled, Is.True);
        Assert.That(UIBlurBackgroundManager.Instance.ContainsRequester(blurA), Is.True);
    }

    [Test]
    public void Request_DoesNotConvertExistingLobbyCanvas()
    {
        GameObject canvasObject = new("BlurStackTestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        GameObject lobbyButton = new("BlurStackTestLobbyButton", typeof(RectTransform));
        lobbyButton.transform.SetParent(canvasObject.transform, false);
        GameObject panel = CreatePanel("BlurStackTestPanel", canvasObject.transform, out Canvas panelCanvas);
        Transform lobbyButtonParent = lobbyButton.transform.parent;

        AddBlurBackground(panel, panelCanvas);
        panel.SetActive(true);

        Assert.That(lobbyButton.transform.parent, Is.EqualTo(lobbyButtonParent));
        Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        Assert.That(canvas.overrideSorting, Is.False);
        Assert.That(panelCanvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
    }

    [Test]
    public void Release_RestoresMovedUiWhenStackBecomesEmpty()
    {
        GameObject canvas = new("BlurStackTestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        GameObject root = new("BlurStackTestRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        GameObject panel = CreatePanel("BlurStackTestPanel", root.transform, out Canvas panelCanvas);

        AddBlurBackground(panel, panelCanvas);
        panel.SetActive(true);
        panel.SetActive(false);

        Assert.That(panel.transform.parent, Is.EqualTo(root.transform));
        Assert.That(UIBlurBackgroundManager.Instance.RequesterCount, Is.Zero);
        Assert.That(panelCanvas.enabled, Is.True);
    }

    [Test]
    public void ReleaseTopRequester_KeepsPreviousPanelPresentationEnabledWithoutMoving()
    {
        GameObject canvas = new("BlurStackTestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        GameObject root = new("BlurStackTestRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        GameObject panelA = CreatePanel("BlurStackTestPanelA", root.transform, out Canvas canvasA);
        GameObject panelB = CreatePanel("BlurStackTestPanelB", root.transform, out Canvas canvasB);

        UIBlurBackground blurA = AddBlurBackground(panelA, canvasA);
        AddBlurBackground(panelB, canvasB);
        panelA.SetActive(true);
        panelB.SetActive(true);

        Assert.That(canvasA.enabled, Is.True);
        Assert.That(canvasB.enabled, Is.True);

        panelB.SetActive(false);

        Assert.That(UIBlurBackgroundManager.Instance.TopRequester, Is.SameAs(blurA));
        Assert.That(panelA.transform.parent, Is.EqualTo(root.transform));
        Assert.That(panelB.transform.parent, Is.EqualTo(root.transform));
        Assert.That(canvasA.enabled, Is.True);
    }

    [Test]
    public void SharedBlurBackground_DoesNotBlockRaycasts()
    {
        GameObject canvas = new("BlurStackTestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        GameObject panel = CreatePanel("BlurStackTestPanel", canvas.transform, out Canvas panelCanvas);

        AddBlurBackground(panel, panelCanvas);
        panel.SetActive(true);

        RawImage background = UIBlurBackgroundManager.Instance.GetComponentInChildren<RawImage>(true);

        Assert.That(background, Is.Not.Null);
        Assert.That(background.raycastTarget, Is.False);
    }

    [Test]
    public void FixedSettingUpper_IsNotMutatedByBlurManager()
    {
        GameObject canvas = new("BlurStackTestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        GameObject root = new("BlurStackTestRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        GameObject settingButton = new("BlurStackTestSettingButton", typeof(RectTransform));
        settingButton.transform.SetParent(root.transform, false);
        GameObject settingUpper = new("Setting_upper", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        Canvas settingCanvas = settingUpper.GetComponent<Canvas>();
        settingCanvas.overrideSorting = true;
        settingCanvas.sortingOrder = 10001;
        settingUpper.transform.SetParent(settingButton.transform, false);
        GameObject settingUnder = new("BlurStackTestSetting_under", typeof(RectTransform));
        settingUnder.transform.SetParent(settingButton.transform, false);
        GameObject panel = CreatePanel("BlurStackTestPanel", root.transform, out Canvas panelCanvas);
        Transform settingUpperParent = settingUpper.transform.parent;
        Transform settingButtonParent = settingButton.transform.parent;
        Transform settingUnderParent = settingUnder.transform.parent;

        AddBlurBackground(panel, panelCanvas);
        panel.SetActive(true);

        Assert.That(settingUpper.transform.parent, Is.EqualTo(settingUpperParent));
        Assert.That(settingButton.transform.parent, Is.EqualTo(settingButtonParent));
        Assert.That(settingUnder.transform.parent, Is.EqualTo(settingButton.transform));
        Assert.That(settingUnder.transform.parent, Is.EqualTo(settingUnderParent));
        Assert.That(settingUpper.GetComponent<Canvas>(), Is.SameAs(settingCanvas));
        Assert.That(settingUpper.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        Assert.That(settingUpper.GetComponent<Canvas>().sortingOrder, Is.EqualTo(10001));
    }

    [Test]
    public void SetRuntimeBlurredUiRoots_StoresValidUniqueRoots()
    {
        GameObject panel = CreatePanel("BlurStackTestPanel", null, out Canvas panelCanvas);
        GameObject rootA = new("BlurStackTestRootA");
        GameObject rootB = new("BlurStackTestRootB");
        UIBlurBackground blur = AddBlurBackground(panel, panelCanvas);

        blur.SetRuntimeBlurredUiRoots(new[] { rootA, null, rootB, rootA });

        Assert.That(blur.BlurredUiRoots, Is.EqualTo(new[] { rootA, rootB }));
    }

    [Test]
    public void ManagerSource_DoesNotContainAutomaticCanvasMutationApis()
    {
        string manager = System.IO.File.ReadAllText("Assets/Project/Scripts/UIBlurBackgroundManager.cs");

        Assert.That(manager, Does.Not.Contain("FindObjectsByType<Canvas>"));
        Assert.That(manager, Does.Not.Contain("AddComponent<Canvas>"));
        Assert.That(manager, Does.Not.Contain("AddComponent<GraphicRaycaster>"));
        Assert.That(manager, Does.Not.Contain("ApplyBackgroundCanvasStates"));
        Assert.That(manager, Does.Not.Contain("EnsureManagedCanvas"));
        Assert.That(manager, Does.Not.Contain("ApplySharpCanvas"));
        Assert.That(manager, Does.Not.Contain("ApplyBlurredCanvas"));
        Assert.That(manager, Does.Not.Contain("CaptureBackgroundNow"));
        Assert.That(manager, Does.Not.Contain("UIBlurBackgroundCaptureManager"));
        Assert.That(manager, Does.Not.Contain(".enabled = false"));
        Assert.That(manager, Does.Contain("UIBackgroundBlurRendererFeature.SourceTexture"));
    }

    private static GameObject CreatePanel(string name, Transform parent, out Canvas canvas)
    {
        GameObject panel = new(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
        canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10000;
        panel.SetActive(false);
        if (parent != null)
            panel.transform.SetParent(parent, false);
        return panel;
    }

    private static UIBlurBackground AddBlurBackground(GameObject panel, params Canvas[] canvases)
    {
        UIBlurBackground blur = panel.AddComponent<UIBlurBackground>();
        SerializedObject serialized = new(blur);
        SerializedProperty presentation = serialized.FindProperty("presentationCanvases");
        presentation.arraySize = canvases.Length;
        for (int i = 0; i < canvases.Length; i++)
            presentation.GetArrayElementAtIndex(i).objectReferenceValue = canvases[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return blur;
    }
}
