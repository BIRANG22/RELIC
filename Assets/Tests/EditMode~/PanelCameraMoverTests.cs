using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
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

    [UnityTest]
    public IEnumerator ExternalPanelActivation_MovesCameraToConfiguredTarget()
    {
        using CameraMoverFixture fixture = new("ExternalOpen");
        fixture.SetMoveDuration(0f);

        fixture.Panel.SetActive(true);
        yield return null;

        AssertPosition(fixture.CameraTransform.position, fixture.TargetPosition);
    }

    [Test]
    public void OpenPanel_CreatesFullParentRaycastShieldBehindPanel()
    {
        using CameraMoverFixture fixture = new("Shield");
        fixture.SetMoveDuration(0f);

        fixture.Mover.OpenPanel();

        Transform shield = fixture.Root.transform.Find("Shield Panel_InputShield");
        Assert.That(shield, Is.Not.Null);
        Assert.That(shield.GetComponent<Image>().raycastTarget, Is.True);

        RectTransform shieldRect = (RectTransform)shield;
        Assert.That(shieldRect.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(shieldRect.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(shieldRect.offsetMin, Is.EqualTo(Vector2.zero));
        Assert.That(shieldRect.offsetMax, Is.EqualTo(Vector2.zero));
        Assert.That(shield.GetSiblingIndex(), Is.EqualTo(fixture.Panel.transform.GetSiblingIndex() - 1));
    }

    [Test]
    public void ClosePanel_DisablesRaycastShield()
    {
        using CameraMoverFixture fixture = new("CloseShield");
        fixture.SetMoveDuration(0f);
        fixture.Mover.OpenPanel();

        fixture.Mover.ClosePanelAndReturnCamera();

        Transform shield = fixture.Root.transform.Find("CloseShield Panel_InputShield");
        Assert.That(shield, Is.Not.Null);
        Assert.That(shield.gameObject.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator OpenPanel_ContributesToGlobalModalBlockUntilPanelCloses()
    {
        using CameraMoverFixture fixture = new("WorldInput");
        fixture.SetMoveDuration(0f);

        fixture.Mover.OpenPanel();
        yield return null;

        Assert.That(LobbyPositionModalInputBlocker.IsBlocked, Is.True);

        fixture.Mover.ClosePanelAndReturnCamera();

        Assert.That(LobbyPositionModalInputBlocker.IsBlocked, Is.False);
    }

    [UnityTest]
    public IEnumerator ReopeningPanelDuringReturn_RestartsZoomAndRestoresShield()
    {
        using CameraMoverFixture fixture = new("Reopen");
        fixture.SetMoveDuration(10f);
        fixture.Mover.OpenPanel();
        fixture.CameraTransform.position = fixture.InterruptedPosition;
        fixture.Mover.ClosePanelAndReturnCamera();

        fixture.SetMoveDuration(0f);
        fixture.Panel.SetActive(true);
        yield return null;

        AssertPosition(fixture.CameraTransform.position, fixture.TargetPosition);
        Transform shield = fixture.Root.transform.Find("Reopen Panel_InputShield");
        Assert.That(shield.gameObject.activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator ReenablingMoverWhilePanelIsOpen_ReappliesZoomAndShield()
    {
        using CameraMoverFixture fixture = new("Reenable");
        fixture.SetMoveDuration(0f);
        fixture.Mover.OpenPanel();

        fixture.Mover.enabled = false;
        fixture.Mover.enabled = true;
        yield return null;

        AssertPosition(fixture.CameraTransform.position, fixture.TargetPosition);
        Transform shield = fixture.Root.transform.Find("Reenable Panel_InputShield");
        Assert.That(shield.gameObject.activeSelf, Is.True);
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
        public Vector3 TargetPosition { get; } = new(0f, 0f, -10f);
        public GameObject Root => root;
        public GameObject Panel => panelObject;
        public Transform CameraTransform => cameraObject.transform;
        public PanelCameraMover Mover { get; }

        public CameraMoverFixture(string name)
            : this(name, null)
        {
        }

        public CameraMoverFixture(string name, GameObject sharedCameraObject)
        {
            root = new GameObject(
                $"PanelCameraMover{name}Root",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster));

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
            targetObject.transform.position = TargetPosition;

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

            LobbyPositionModalInputBlocker.Unblock(null);

            if (ownsCamera)
                Object.DestroyImmediate(cameraObject);

            Object.DestroyImmediate(root);
        }
    }
}
