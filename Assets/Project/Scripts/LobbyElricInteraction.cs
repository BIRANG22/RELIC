using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class LobbyElricInteraction : MonoBehaviour
{
    [SerializeField] private LobbyTutorialController tutorialController;
    [SerializeField] private bool ignoreClickWhenPointerOverUi = true;

    private void Awake()
    {
        ResolveController();
        EnsureWorldCollider();
    }

    private void OnMouseUpAsButton()
    {
        if (ShouldBlockClick())
            return;

        ResolveController();
        tutorialController?.TryInteractWithElric();
    }

    public void Interact()
    {
        ResolveController();
        tutorialController?.TryInteractWithElric();
    }

    private bool ShouldBlockClick()
    {
        if (ignoreClickWhenPointerOverUi &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        if (UIPanelButton.IsMenuPanelOpen)
            return true;

        if (LobbyPositionModalInputBlocker.IsBlockedByAnother(this))
            return true;

        return tutorialController != null && tutorialController.IsDialogueOpen;
    }

    private void ResolveController()
    {
        if (tutorialController != null)
            return;

        tutorialController = FindFirstObjectByType<LobbyTutorialController>(FindObjectsInactive.Include);
    }

    private void EnsureWorldCollider()
    {
        if (GetComponent<Collider2D>() != null)
            return;

        if (GetComponent<SpriteRenderer>() != null)
        {
            gameObject.AddComponent<PolygonCollider2D>();
            return;
        }

        BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
        boxCollider.size = Vector2.one;
    }
}
