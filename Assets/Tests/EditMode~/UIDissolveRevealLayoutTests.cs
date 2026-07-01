using System.Reflection;
using NUnit.Framework;
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
}
