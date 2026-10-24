using NUnit.Framework;
using UnityEngine;

public sealed class UIBlurReplicaSourceScaleTests
{
    private GameObject sourceRootObject;
    private GameObject replicaParentObject;
    private UIBlurReplicaSource replicaSource;

    [TearDown]
    public void TearDown()
    {
        replicaSource?.Destroy();

        if (sourceRootObject != null)
            Object.DestroyImmediate(sourceRootObject);

        if (replicaParentObject != null)
            Object.DestroyImmediate(replicaParentObject);
    }

    [Test]
    public void RootUsesUnitScaleWhileChildPreservesLocalScale()
    {
        replicaParentObject = new GameObject("ReplicaParent", typeof(RectTransform));
        RectTransform replicaParent = replicaParentObject.GetComponent<RectTransform>();
        replicaParent.sizeDelta = new Vector2(1920f, 1080f);

        sourceRootObject = new GameObject("SourceRoot", typeof(RectTransform));
        RectTransform sourceRoot = sourceRootObject.GetComponent<RectTransform>();
        sourceRoot.sizeDelta = new Vector2(800f, 400f);
        sourceRoot.localScale = new Vector3(1.5f, 1.5f, 1f);

        GameObject sourceChildObject = new("SourceChild", typeof(RectTransform));
        RectTransform sourceChild = sourceChildObject.GetComponent<RectTransform>();
        sourceChild.SetParent(sourceRoot, false);
        sourceChild.localScale = new Vector3(0.75f, 0.5f, 1f);

        replicaSource = new UIBlurReplicaSource(sourceRoot, replicaParent, null, 0);

        Transform replicaRoot = replicaSource.GameObject.transform;
        Transform replicaChild = replicaRoot.Find("SourceChild_BlurReplica");

        Assert.That(replicaRoot.localScale, Is.EqualTo(Vector3.one));
        Assert.That(replicaChild, Is.Not.Null);
        Assert.That(replicaChild.localScale, Is.EqualTo(sourceChild.localScale));
    }
}
