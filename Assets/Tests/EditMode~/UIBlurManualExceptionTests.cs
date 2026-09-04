using System.IO;
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
    public void LobbyScene_DoesNotUseAutomaticBlurIncludeMarkers()
    {
        string sceneYaml = File.ReadAllText(LobbyScenePath);

        Assert.That(sceneYaml, Does.Not.Contain(UIBlurIncludeGuid));
    }

}
