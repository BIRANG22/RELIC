using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIBlurReplicaSourceColorTintTests
{
    private GameObject sourceObject;
    private GameObject replicaParentObject;
    private UIBlurReplicaSource replicaSource;

    [TearDown]
    public void TearDown()
    {
        replicaSource?.Destroy();
        if (sourceObject != null) Object.DestroyImmediate(sourceObject);
        if (replicaParentObject != null) Object.DestroyImmediate(replicaParentObject);
    }

    [Test]
    public void SyncNow_MultipliesGraphicColorByRuntimeCanvasRendererTint()
    {
        Image source = CreateSourceImage();
        source.color = Color.white;
        source.canvasRenderer.SetColor(Color.black);

        replicaSource = CreateReplica(source.rectTransform);

        Image replica = replicaSource.GameObject.GetComponent<Image>();
        Assert.That(replica.color, Is.EqualTo(Color.black));
    }

    [Test]
    public void SyncNow_ComposesGraphicRendererAndCanvasGroupColors()
    {
        Image source = CreateSourceImage();
        source.color = new Color(0.8f, 0.6f, 0.4f, 0.5f);
        source.canvasRenderer.SetColor(new Color(0.25f, 0.5f, 1f, 0.8f));
        sourceObject.AddComponent<CanvasGroup>().alpha = 0.5f;

        replicaSource = CreateReplica(source.rectTransform);

        Color actual = replicaSource.GameObject.GetComponent<Image>().color;
        Color expected = new Color(0.2f, 0.3f, 0.4f, 0.2f);
        Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
        Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
        Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
        Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.0001f));
    }

    private Image CreateSourceImage()
    {
        sourceObject = new GameObject("Source", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        return sourceObject.GetComponent<Image>();
    }

    private UIBlurReplicaSource CreateReplica(RectTransform source)
    {
        replicaParentObject = new GameObject("ReplicaParent", typeof(RectTransform));
        RectTransform parent = replicaParentObject.GetComponent<RectTransform>();
        parent.sizeDelta = new Vector2(1920f, 1080f);
        return new UIBlurReplicaSource(source, parent, null, 0);
    }
}
