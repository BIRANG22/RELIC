using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class UIBlurBackgroundResolutionTests
{
    private GameObject managerRoot;

    [SetUp]
    public void SetUp()
    {
        managerRoot = UIBlurBackgroundManager.Instance.gameObject;
    }

    [TearDown]
    public void TearDown()
    {
        if (managerRoot != null)
            Object.DestroyImmediate(managerRoot);
    }

    [Test]
    public void SharedBlurCanvas_IsExcludedFromResolutionViewportFitting()
    {
        Canvas canvas = FindManagerCanvas("SharedBlurCanvas");

        Assert.That(canvas.GetComponent<ResolutionCanvasFitOptOut>(), Is.Not.Null);
        Assert.That(ResolutionManager.ShouldFitCanvasForResolution(canvas), Is.False);
    }

    [Test]
    public void BlurReplicaCanvas_IsExcludedFromResolutionViewportFitting()
    {
        InvokeManagerMethod("EnsureBlurReplicaUI");
        Canvas canvas = FindManagerCanvas("BlurReplicaRoot");

        Assert.That(canvas.GetComponent<ResolutionCanvasFitOptOut>(), Is.Not.Null);
        Assert.That(ResolutionManager.ShouldFitCanvasForResolution(canvas), Is.False);
    }

    private Canvas FindManagerCanvas(string objectName)
    {
        Transform target = managerRoot.transform.Find(objectName);
        Assert.That(target, Is.Not.Null, $"{objectName}을 찾을 수 없습니다.");

        Canvas canvas = target.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null, $"{objectName}에 Canvas가 없습니다.");
        return canvas;
    }

    private static void InvokeManagerMethod(string methodName)
    {
        MethodInfo method = typeof(UIBlurBackgroundManager).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"{methodName}을 찾을 수 없습니다.");
        method.Invoke(UIBlurBackgroundManager.Instance, null);
    }
}
