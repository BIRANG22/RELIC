using NUnit.Framework;
using UnityEngine;

public class HUDSlotVisibilitySafetyTests
{
    [Test]
    public void NewlyCreatedSlot_StartsHidden()
    {
        GameObject hudObject = new GameObject("HUDSlot-Test", typeof(RectTransform));

        try
        {
            hudObject.AddComponent<HUDSlot>();

            CanvasGroup canvasGroup = hudObject.GetComponent<CanvasGroup>();
            Assert.That(canvasGroup, Is.Not.Null);
            Assert.That(canvasGroup.alpha, Is.Zero);
            Assert.That(canvasGroup.interactable, Is.False);
            Assert.That(canvasGroup.blocksRaycasts, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(hudObject);
        }
    }

    [Test]
    public void Show_WithoutReadyFollowPosition_RemainsHidden()
    {
        GameObject hudObject = new GameObject("HUDSlot-Test", typeof(RectTransform));

        try
        {
            HUDSlot hud = hudObject.AddComponent<HUDSlot>();
            hud.Show();

            CanvasGroup canvasGroup = hudObject.GetComponent<CanvasGroup>();
            Assert.That(canvasGroup.alpha, Is.Zero);
            Assert.That(canvasGroup.interactable, Is.False);
            Assert.That(canvasGroup.blocksRaycasts, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(hudObject);
        }
    }

    [Test]
    public void DisablingSlot_ForcesCanvasHidden()
    {
        GameObject hudObject = new GameObject("HUDSlot-Test", typeof(RectTransform));

        try
        {
            HUDSlot hud = hudObject.AddComponent<HUDSlot>();
            hud.SetUseFollowPosition(false);
            hud.Show();

            CanvasGroup canvasGroup = hudObject.GetComponent<CanvasGroup>();
            Assert.That(canvasGroup.alpha, Is.EqualTo(1f));

            hudObject.SetActive(false);

            Assert.That(canvasGroup.alpha, Is.Zero);
            Assert.That(canvasGroup.interactable, Is.False);
            Assert.That(canvasGroup.blocksRaycasts, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(hudObject);
        }
    }

    [Test]
    public void RebindingVisibleSlot_ToMissingTarget_HidesImmediately()
    {
        GameObject hudObject = new GameObject("HUDSlot-Test", typeof(RectTransform));

        try
        {
            HUDSlot hud = hudObject.AddComponent<HUDSlot>();
            hud.SetUseFollowPosition(false);
            hud.Show();

            CanvasGroup canvasGroup = hudObject.GetComponent<CanvasGroup>();
            Assert.That(canvasGroup.alpha, Is.EqualTo(1f));

            hud.SetUseFollowPosition(true);
            hud.SetFollowTarget(null);

            Assert.That(canvasGroup.alpha, Is.Zero);
            Assert.That(canvasGroup.interactable, Is.False);
            Assert.That(canvasGroup.blocksRaycasts, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(hudObject);
        }
    }

    [Test]
    public void Show_AfterInactiveChildColliderActivates_UsesColliderTopPosition()
    {
        GameObject cameraObject = new GameObject("HUDSlot-Camera", typeof(Camera));
        GameObject targetObject = new GameObject("HUDSlot-Target");
        GameObject colliderObject = new GameObject("HUDSlot-Collider", typeof(BoxCollider2D));
        GameObject hudObject = new GameObject("HUDSlot-Test", typeof(RectTransform));

        try
        {
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;

            colliderObject.transform.SetParent(targetObject.transform, false);
            colliderObject.transform.localPosition = new Vector3(0f, 3f, 0f);
            colliderObject.SetActive(false);

            HUDSlot hud = hudObject.AddComponent<HUDSlot>();
            hud.SetFollowTarget(targetObject.transform, null);
            hud.Show();
            float fallbackY = hudObject.transform.position.y;

            colliderObject.SetActive(true);
            hud.Show();

            Assert.That(hudObject.transform.position.y, Is.GreaterThan(fallbackY));
        }
        finally
        {
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(colliderObject);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(cameraObject);
        }
    }
}
