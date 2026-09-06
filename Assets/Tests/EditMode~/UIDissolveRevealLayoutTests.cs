using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIDissolveRevealLayoutTests
{
    [Test]
    public void ShowFromLeft_AlignsChildrenByLeftBoundsWithPanelPadding()
    {
        LayoutFixture fixture = CreateFixture();

        try
        {
            fixture.Reveal.ShowFromLeft();

            float expectedLeft = GetLocalBounds(fixture.Panel, fixture.Panel).min.x + 80f;

            Assert.That(GetLocalBounds(fixture.Panel, fixture.Children[0]).min.x, Is.EqualTo(expectedLeft).Within(0.01f));
            Assert.That(GetLocalBounds(fixture.Panel, fixture.Children[1]).min.x, Is.EqualTo(expectedLeft).Within(0.01f));
            Assert.That(GetLocalBounds(fixture.Panel, fixture.Children[2]).min.x, Is.EqualTo(expectedLeft).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(fixture.Root);
        }
    }

    [Test]
    public void ShowFromRight_AlignsChildrenByRightBoundsWithPanelPadding()
    {
        LayoutFixture fixture = CreateFixture();

        try
        {
            fixture.Reveal.ShowFromRight();

            float expectedRight = GetLocalBounds(fixture.Panel, fixture.Panel).max.x - 80f;

            Assert.That(GetLocalBounds(fixture.Panel, fixture.Children[0]).max.x, Is.EqualTo(expectedRight).Within(0.01f));
            Assert.That(GetLocalBounds(fixture.Panel, fixture.Children[1]).max.x, Is.EqualTo(expectedRight).Within(0.01f));
            Assert.That(GetLocalBounds(fixture.Panel, fixture.Children[2]).max.x, Is.EqualTo(expectedRight).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(fixture.Root);
        }
    }

    [Test]
    public void ShowFromLeft_KeepsVerticalGapBetweenActualScaledBounds()
    {
        LayoutFixture fixture = CreateFixture();

        try
        {
            fixture.Reveal.ShowFromLeft();

            Bounds first = GetLocalBounds(fixture.Panel, fixture.Children[0]);
            Bounds second = GetLocalBounds(fixture.Panel, fixture.Children[1]);
            Bounds third = GetLocalBounds(fixture.Panel, fixture.Children[2]);

            Assert.That(first.min.y - second.max.y, Is.EqualTo(24f).Within(0.01f));
            Assert.That(second.min.y - third.max.y, Is.EqualTo(24f).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(fixture.Root);
        }
    }

    [Test]
    public void ShowFromRight_KeepsVerticalGapBetweenActualScaledBounds()
    {
        LayoutFixture fixture = CreateFixture();

        try
        {
            fixture.Reveal.ShowFromRight();

            Bounds first = GetLocalBounds(fixture.Panel, fixture.Children[0]);
            Bounds second = GetLocalBounds(fixture.Panel, fixture.Children[1]);
            Bounds third = GetLocalBounds(fixture.Panel, fixture.Children[2]);

            Assert.That(first.min.y - second.max.y, Is.EqualTo(24f).Within(0.01f));
            Assert.That(second.min.y - third.max.y, Is.EqualTo(24f).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(fixture.Root);
        }
    }

    [Test]
    public void ShouldRevealFromLeft_UsesCenterColumnAsLeftRevealThreshold()
    {
        Assert.That(UIDissolveReveal.ShouldRevealFromLeft(14, 7, 5), Is.False);
        Assert.That(UIDissolveReveal.ShouldRevealFromLeft(15, 7, 5), Is.True);
        Assert.That(UIDissolveReveal.ShouldRevealFromLeft(19, 7, 5), Is.True);
        Assert.That(UIDissolveReveal.ShouldRevealFromLeft(34, 7, 5), Is.True);
    }

    [Test]
    public void ShowForGridIndex_CenterOrRightMonsterShowsFromLeft()
    {
        LayoutFixture fixture = CreateFixture();

        try
        {
            fixture.Reveal.ShowForGridIndex(GridIndex(3, 2));

            float expectedLeft = GetLocalBounds(fixture.Panel, fixture.Panel).min.x + 80f;

            Assert.That(GetLocalBounds(fixture.Panel, fixture.Children[0]).min.x, Is.EqualTo(expectedLeft).Within(0.01f));
            Assert.That(GetLocalBounds(fixture.Panel, fixture.Children[1]).min.x, Is.EqualTo(expectedLeft).Within(0.01f));
            Assert.That(GetLocalBounds(fixture.Panel, fixture.Children[2]).min.x, Is.EqualTo(expectedLeft).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(fixture.Root);
        }
    }

    [Test]
    public void ShowForGridIndex_LeftMonsterShowsFromRight()
    {
        LayoutFixture fixture = CreateFixture();

        try
        {
            fixture.Reveal.ShowForGridIndex(GridIndex(2, 2));

            float expectedRight = GetLocalBounds(fixture.Panel, fixture.Panel).max.x - 80f;

            Assert.That(GetLocalBounds(fixture.Panel, fixture.Children[0]).max.x, Is.EqualTo(expectedRight).Within(0.01f));
            Assert.That(GetLocalBounds(fixture.Panel, fixture.Children[1]).max.x, Is.EqualTo(expectedRight).Within(0.01f));
            Assert.That(GetLocalBounds(fixture.Panel, fixture.Children[2]).max.x, Is.EqualTo(expectedRight).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(fixture.Root);
        }
    }

    [Test]
    public void ShowFromLeft_AutoResolvesSceneMonsterInfoPanelWhenFieldsAreEmpty()
    {
        AutoPanelFixture fixture = CreateAutoPanelFixture();

        try
        {
            fixture.Reveal.ShowFromLeft();

            float expectedLeft = GetLocalBounds(fixture.Panel, fixture.Panel).min.x + 80f;

            Assert.That(GetLocalBounds(fixture.Panel, fixture.Children[0]).min.x, Is.EqualTo(expectedLeft).Within(0.01f));
            Assert.That(GetLocalBounds(fixture.Panel, fixture.Children[1]).min.x, Is.EqualTo(expectedLeft).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(fixture.Root);
        }
    }

    [Test]
    public void ShowFromLeft_ActivatesInactiveRenderParentWithoutAwakeHideCancellingShow()
    {
        GameObject parent = new("RawImage(RT)");
        parent.SetActive(false);

        GameObject child = new("DissolveImage", typeof(RectTransform));
        child.transform.SetParent(parent.transform, false);
        child.AddComponent<RawImage>();
        child.AddComponent<UIDissolveReveal>();

        try
        {
            UIDissolveReveal reveal = child.GetComponent<UIDissolveReveal>();

            reveal.ShowFromLeft();

            Assert.That(parent.activeSelf, Is.True);
            Assert.That(child.activeSelf, Is.True);
            Assert.That(child.activeInHierarchy, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(parent);
        }
    }

    [Test]
    public void AwakeHide_DoesNotCreateRenderOutputCanvas()
    {
        GameObject root = new("UIDissolveRevealLayoutTests_AwakeHideRoot");
        GameObject renderObject = new("RawImage(RT)", typeof(RectTransform));
        renderObject.transform.SetParent(root.transform, false);

        GameObject revealObject = new("DissolveImage", typeof(RectTransform), typeof(RawImage));
        revealObject.transform.SetParent(renderObject.transform, false);

        try
        {
            UIDissolveReveal reveal = revealObject.AddComponent<UIDissolveReveal>();
            reveal.HideImmediate();

            Assert.That(renderObject.GetComponent<Canvas>(), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void HideToLeft_UsesLeftRevealDirection()
    {
        DirectionFixture fixture = CreateDirectionFixture();

        try
        {
            fixture.Reveal.ShowFromLeft();
            fixture.Reveal.HideToLeft();

            Assert.That(fixture.Image.material.GetFloat("_Direction"), Is.EqualTo(0f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(fixture.Root);
            Object.DestroyImmediate(fixture.BaseMaterial);
        }
    }

    [Test]
    public void HideToRight_UsesRightRevealDirection()
    {
        DirectionFixture fixture = CreateDirectionFixture();

        try
        {
            fixture.Reveal.ShowFromRight();
            fixture.Reveal.HideToRight();

            Assert.That(fixture.Image.material.GetFloat("_Direction"), Is.EqualTo(1f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(fixture.Root);
            Object.DestroyImmediate(fixture.BaseMaterial);
        }
    }

    [Test]
    public void ShowFromLeft_AlignsAfterActivatingInactiveInfoCanvas()
    {
        InactiveInfoCanvasFixture fixture = CreateInactiveInfoCanvasFixture();

        try
        {
            fixture.Reveal.ShowFromLeft();

            float expectedLeft = GetLocalBounds(fixture.Panel, fixture.Panel).min.x + 80f;

            Assert.That(fixture.CanvasObject.activeSelf, Is.True);
            Assert.That(GetLocalBounds(fixture.Panel, fixture.ResizingChild).min.x, Is.EqualTo(expectedLeft).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(fixture.Root);
        }
    }

    [Test]
    public void Show_AlignsAfterActivatingInactiveInfoCanvasUsingLastRevealSide()
    {
        InactiveInfoCanvasFixture fixture = CreateInactiveInfoCanvasFixture();

        try
        {
            fixture.Reveal.Show();

            float expectedLeft = GetLocalBounds(fixture.Panel, fixture.Panel).min.x + 80f;

            Assert.That(fixture.CanvasObject.activeSelf, Is.True);
            Assert.That(GetLocalBounds(fixture.Panel, fixture.ResizingChild).min.x, Is.EqualTo(expectedLeft).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(fixture.Root);
        }
    }

    [Test]
    public void ShowFromLeft_ForcesRenderOutputRawImageCanvasToFrontSorting()
    {
        InactiveInfoCanvasFixture fixture = CreateInactiveInfoCanvasFixture();

        try
        {
            Canvas infoCanvas = fixture.CanvasObject.GetComponent<Canvas>();
            Assert.That(fixture.RenderObject.GetComponent<Canvas>(), Is.Null);

            fixture.Reveal.ShowFromLeft();

            Canvas renderCanvas = fixture.RenderObject.GetComponent<Canvas>();

            Assert.That(renderCanvas, Is.Not.Null);
            Assert.That(renderCanvas.overrideSorting, Is.True);
            Assert.That(renderCanvas.sortingOrder, Is.EqualTo(30000));
            Assert.That(infoCanvas.overrideSorting, Is.False);
            Assert.That(infoCanvas.sortingOrder, Is.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(fixture.Root);
        }
    }

    [Test]
    public void HideImmediate_ResetsRenderOutputCanvasSorting()
    {
        InactiveInfoCanvasFixture fixture = CreateInactiveInfoCanvasFixture();

        try
        {
            fixture.Reveal.ShowFromLeft();

            Canvas renderCanvas = fixture.RenderObject.GetComponent<Canvas>();
            Assert.That(renderCanvas, Is.Not.Null);
            Assert.That(renderCanvas.overrideSorting, Is.True);
            Assert.That(renderCanvas.sortingOrder, Is.EqualTo(30000));

            fixture.Reveal.HideImmediate();

            Assert.That(renderCanvas.overrideSorting, Is.False);
            Assert.That(renderCanvas.sortingOrder, Is.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(fixture.Root);
        }
    }

    [Test]
    public void ShowFromLeft_InfoPanelDoesNotBlockHudRaycasts()
    {
        GameObject root = new("UIDissolveRevealLayoutTests_ClickPolicyRoot");
        GameObject revealObject = new("DissolveImage", typeof(RectTransform), typeof(RawImage), typeof(UIDissolveReveal));
        revealObject.transform.SetParent(root.transform, false);

        GameObject panelObject = new("MonsterInfoPanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(root.transform, false);

        try
        {
            UIDissolveReveal reveal = revealObject.GetComponent<UIDissolveReveal>();
            SetPrivateField(reveal, "monsterInfoPanel", panelObject.GetComponent<RectTransform>());

            reveal.ShowFromLeft();

            CanvasGroup canvasGroup = panelObject.GetComponent<CanvasGroup>();

            Assert.That(canvasGroup, Is.Not.Null);
            Assert.That(canvasGroup.interactable, Is.False);
            Assert.That(canvasGroup.blocksRaycasts, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static LayoutFixture CreateFixture()
    {
        GameObject root = new("UIDissolveRevealLayoutTests_Root", typeof(RectTransform), typeof(UIDissolveReveal));
        RectTransform panel = root.GetComponent<RectTransform>();
        panel.sizeDelta = new Vector2(1000f, 800f);

        VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 0f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childScaleWidth = false;
        layout.childScaleHeight = false;

        UIDissolveReveal reveal = root.GetComponent<UIDissolveReveal>();
        SetPrivateField(reveal, "infoContentLayout", layout);

        RectTransform[] children =
        {
            CreateChild(panel, "Wide_Short", new Vector2(160f, 40f), new Vector3(1.25f, 0.5f, 1f)),
            CreateChild(panel, "Narrow_Tall", new Vector2(90f, 70f), new Vector3(0.8f, 1.7f, 1f)),
            CreateChild(panel, "Normal", new Vector2(130f, 55f), new Vector3(1f, 1f, 1f))
        };

        return new LayoutFixture(root, reveal, panel, children);
    }

    private static AutoPanelFixture CreateAutoPanelFixture()
    {
        GameObject root = new("UIDissolveRevealLayoutTests_AutoRoot");
        GameObject revealObject = new("DissolveImage", typeof(RectTransform), typeof(RawImage), typeof(UIDissolveReveal));
        revealObject.transform.SetParent(root.transform, false);

        GameObject canvasObject = new("DissolvePanelCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(root.transform, false);

        GameObject panelObject = new("UIDissolveRevealLayoutTests_AutoPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.sizeDelta = new Vector2(1000f, 800f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childScaleWidth = false;
        layout.childScaleHeight = false;

        RectTransform[] children =
        {
            CreateChild(panel, "Auto_Wide", new Vector2(160f, 40f), Vector3.one),
            CreateChild(panel, "Auto_Narrow", new Vector2(90f, 70f), Vector3.one)
        };

        UIDissolveReveal reveal = revealObject.GetComponent<UIDissolveReveal>();
        SetPrivateField(reveal, "autoMonsterInfoPanelName", panelObject.name);
        return new AutoPanelFixture(root, reveal, panel, children);
    }

    private static DirectionFixture CreateDirectionFixture()
    {
        GameObject root = new("UIDissolveRevealLayoutTests_DirectionRoot", typeof(RectTransform));
        root.SetActive(false);

        RawImage image = root.AddComponent<RawImage>();
        Material sourceMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Project/Art/Materials/Dissolve/M_UI_RT_Dissolve.mat");
        Assert.That(sourceMaterial, Is.Not.Null);

        Material baseMaterial = Object.Instantiate(sourceMaterial);
        image.material = baseMaterial;

        UIDissolveReveal reveal = root.AddComponent<UIDissolveReveal>();
        SetPrivateField(reveal, "targetRawImage", image);

        return new DirectionFixture(root, reveal, image, baseMaterial);
    }

    private static InactiveInfoCanvasFixture CreateInactiveInfoCanvasFixture()
    {
        GameObject root = new("UIDissolveRevealLayoutTests_InactiveInfoRoot");

        GameObject renderObject = new("RawImage(RT)", typeof(RectTransform));
        renderObject.transform.SetParent(root.transform, false);
        renderObject.SetActive(false);

        GameObject revealObject = new("DissolveImage", typeof(RectTransform), typeof(RawImage));
        revealObject.transform.SetParent(renderObject.transform, false);
        revealObject.SetActive(false);

        GameObject canvasObject = new("DissolvePanelCanvas", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(root.transform, false);
        canvasObject.SetActive(false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.overrideSorting = false;
        canvas.sortingOrder = 0;

        GameObject panelObject = new("MonsterInfoPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.sizeDelta = new Vector2(1000f, 800f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childScaleWidth = false;
        layout.childScaleHeight = false;

        RectTransform child = CreateChild(panel, "ResizeOnEnable", new Vector2(40f, 40f), Vector3.one);
        ResizeOnEnableRectTransform resizeOnEnable = child.gameObject.AddComponent<ResizeOnEnableRectTransform>();
        resizeOnEnable.EnabledSize = new Vector2(200f, 40f);

        RawImage renderImage = revealObject.GetComponent<RawImage>();
        UIDissolveReveal reveal = revealObject.AddComponent<UIDissolveReveal>();
        SetPrivateField(reveal, "targetRawImage", renderImage);
        SetPrivateField(reveal, "infoContentLayout", layout);
        SetPrivateField(reveal, "monsterInfoPanel", panel);
        SetPrivateField(reveal, "objectsEnabledWhileVisible", new[] { canvasObject, renderObject });

        return new InactiveInfoCanvasFixture(root, reveal, canvasObject, renderObject, renderImage, panel, child);
    }

    private static RectTransform CreateChild(
        RectTransform parent,
        string name,
        Vector2 size,
        Vector3 scale)
    {
        GameObject child = new(name, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.localScale = scale;
        return rect;
    }

    private static Bounds GetLocalBounds(RectTransform root, RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        Vector3 min = root.InverseTransformPoint(corners[0]);
        Vector3 max = min;

        for (int i = 1; i < corners.Length; i++)
        {
            Vector3 point = root.InverseTransformPoint(corners[i]);
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        Bounds bounds = new();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    private static int GridIndex(int x, int y)
    {
        const int GridHeight = 5;
        return x * GridHeight + y;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private readonly struct LayoutFixture
    {
        public LayoutFixture(
            GameObject root,
            UIDissolveReveal reveal,
            RectTransform panel,
            RectTransform[] children)
        {
            Root = root;
            Reveal = reveal;
            Panel = panel;
            Children = children;
        }

        public readonly GameObject Root;
        public readonly UIDissolveReveal Reveal;
        public readonly RectTransform Panel;
        public readonly RectTransform[] Children;
    }

    private readonly struct AutoPanelFixture
    {
        public AutoPanelFixture(
            GameObject root,
            UIDissolveReveal reveal,
            RectTransform panel,
            RectTransform[] children)
        {
            Root = root;
            Reveal = reveal;
            Panel = panel;
            Children = children;
        }

        public readonly GameObject Root;
        public readonly UIDissolveReveal Reveal;
        public readonly RectTransform Panel;
        public readonly RectTransform[] Children;
    }

    private readonly struct DirectionFixture
    {
        public DirectionFixture(
            GameObject root,
            UIDissolveReveal reveal,
            RawImage image,
            Material baseMaterial)
        {
            Root = root;
            Reveal = reveal;
            Image = image;
            BaseMaterial = baseMaterial;
        }

        public readonly GameObject Root;
        public readonly UIDissolveReveal Reveal;
        public readonly RawImage Image;
        public readonly Material BaseMaterial;
    }

    private readonly struct InactiveInfoCanvasFixture
    {
        public InactiveInfoCanvasFixture(
            GameObject root,
            UIDissolveReveal reveal,
            GameObject canvasObject,
            GameObject renderObject,
            RawImage renderImage,
            RectTransform panel,
            RectTransform resizingChild)
        {
            Root = root;
            Reveal = reveal;
            CanvasObject = canvasObject;
            RenderObject = renderObject;
            RenderImage = renderImage;
            Panel = panel;
            ResizingChild = resizingChild;
        }

        public readonly GameObject Root;
        public readonly UIDissolveReveal Reveal;
        public readonly GameObject CanvasObject;
        public readonly GameObject RenderObject;
        public readonly RawImage RenderImage;
        public readonly RectTransform Panel;
        public readonly RectTransform ResizingChild;
    }

    private sealed class ResizeOnEnableRectTransform : MonoBehaviour
    {
        public Vector2 EnabledSize { get; set; }

        private void OnEnable()
        {
            GetComponent<RectTransform>().sizeDelta = EnabledSize;
        }
    }
}
