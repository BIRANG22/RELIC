using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class PanelCameraMoverTests
{
    private const string SourcePath = "Assets/Project/Scripts/Art/PanelCameraMover.cs";

    [Test]
    public void ColliderClick_OpensOnMouseReleaseInsteadOfMousePress()
    {
        string source = File.ReadAllText(SourcePath);

        Assert.That(source, Does.Contain("private void OnMouseUpAsButton()"));
        Assert.That(source, Does.Not.Contain("private void OnMouseDown()"));
    }

    [UnityTest]
    public IEnumerator DisableWhileReturning_RestoresOriginalCameraTransform()
    {
        using CameraMoverFixture fixture = new("Single");
        fixture.SetMoveDuration(10f);

        fixture.Mover.OpenPanel();
        fixture.CameraTransform.position = fixture.InterruptedPosition;
        fixture.Mover.ClosePanelAndReturnCamera();

        fixture.Mover.enabled = false;

        yield return null;

        AssertPosition(fixture.CameraTransform.position, fixture.OriginalPosition);
    }

    [UnityTest]
    public IEnumerator OpeningAnotherMoverDuringReturn_KeepsFirstOriginalCameraTransform()
    {
        using SharedCameraMoverFixture fixture = new();
        fixture.First.SetMoveDuration(10f);
        fixture.Second.SetMoveDuration(0f);

        fixture.First.Mover.OpenPanel();
        fixture.CameraTransform.position = fixture.InterruptedPosition;
        fixture.First.Mover.ClosePanelAndReturnCamera();
        fixture.CameraTransform.position = fixture.InterruptedPosition;

        fixture.Second.Mover.OpenPanel();
        yield return null;

        fixture.Second.Mover.ClosePanelAndReturnCamera();
        yield return null;

        AssertPosition(fixture.CameraTransform.position, fixture.OriginalPosition);
    }

    private static void AssertPosition(Vector3 actual, Vector3 expected)
    {
        Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.001f),
            $"Expected {expected}, but was {actual}.");
    }

    private sealed class SharedCameraMoverFixture : System.IDisposable
    {
        private readonly GameObject root = new("PanelCameraMoverSharedRoot");
        private readonly GameObject cameraObject;

        public Vector3 OriginalPosition { get; } = new(0f, 0f, -20f);
        public Vector3 InterruptedPosition { get; } = new(0f, 0f, -15f);
        public Transform CameraTransform => cameraObject.transform;
        public CameraMoverFixture First { get; }
        public CameraMoverFixture Second { get; }

        public SharedCameraMoverFixture()
        {
            cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(HorizontalHubCameraDrag));
            cameraObject.transform.SetParent(root.transform);
            cameraObject.transform.position = OriginalPosition;

            First = new CameraMoverFixture("First", cameraObject);
            Second = new CameraMoverFixture("Second", cameraObject);
        }

        public void Dispose()
        {
            First.Dispose();
            Second.Dispose();
            Object.DestroyImmediate(root);
        }
    }

    private sealed class CameraMoverFixture : System.IDisposable
    {
        private readonly GameObject root;
        private readonly bool ownsCamera;
        private readonly GameObject cameraObject;
        private readonly GameObject panelObject;
        private readonly GameObject targetObject;
        private readonly GameObject moverObject;

        public Vector3 OriginalPosition { get; } = new(0f, 0f, -20f);
        public Vector3 InterruptedPosition { get; } = new(0f, 0f, -15f);
        public Transform CameraTransform => cameraObject.transform;
        public PanelCameraMover Mover { get; }

        public CameraMoverFixture(string name)
            : this(name, null)
        {
        }

        public CameraMoverFixture(string name, GameObject sharedCameraObject)
        {
            root = new GameObject($"PanelCameraMover{name}Root");

            if (sharedCameraObject == null)
            {
                ownsCamera = true;
                cameraObject = new GameObject($"{name} Camera", typeof(Camera), typeof(HorizontalHubCameraDrag));
                cameraObject.transform.position = OriginalPosition;
            }
            else
            {
                cameraObject = sharedCameraObject;
            }

            cameraObject.transform.SetParent(root.transform);

            panelObject = new GameObject($"{name} Panel");
            panelObject.transform.SetParent(root.transform);
            panelObject.SetActive(false);

            targetObject = new GameObject($"{name} Target");
            targetObject.transform.SetParent(root.transform);
            targetObject.transform.position = new Vector3(0f, 0f, -10f);

            moverObject = new GameObject($"{name} Mover");
            moverObject.transform.SetParent(root.transform);
            Mover = moverObject.AddComponent<PanelCameraMover>();

            SerializedObject serializedMover = new(Mover);
            serializedMover.FindProperty("targetPanel").objectReferenceValue = panelObject;
            serializedMover.FindProperty("targetCamera").objectReferenceValue = cameraObject.GetComponent<Camera>();
            serializedMover.FindProperty("cameraMoveTarget").objectReferenceValue = targetObject.transform;
            serializedMover.ApplyModifiedPropertiesWithoutUndo();
        }

        public void SetMoveDuration(float duration)
        {
            SerializedObject serializedMover = new(Mover);
            serializedMover.FindProperty("moveDuration").floatValue = duration;
            serializedMover.ApplyModifiedPropertiesWithoutUndo();
        }

        public void Dispose()
        {
            if (Mover != null)
                Mover.enabled = false;

            if (ownsCamera)
                Object.DestroyImmediate(cameraObject);

            Object.DestroyImmediate(root);
        }
    }
}
