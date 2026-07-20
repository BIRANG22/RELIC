using UnityEngine;

[DisallowMultipleComponent]
public sealed class PositionCharacterSettingButton : MonoBehaviour
{
    [SerializeField] private LobbyViewStateController viewStateController;

    private void Awake()
    {
        EnsureWorldSpriteCollider();
    }

    private void OnMouseUpAsButton()
    {
        if (LobbyPositionModalInputBlocker.IsBlocked)
            return;

        Execute();
    }

    public void Execute()
    {
        if (viewStateController == null)
            viewStateController = FindFirstObjectByType<LobbyViewStateController>();

        if (viewStateController == null)
        {
            Debug.LogWarning(
                "[PositionCharacterSettingButton] LobbyViewStateController is missing.",
                this);
            return;
        }

        viewStateController.ShowCharacterSelection();
    }

    private void EnsureWorldSpriteCollider()
    {
        if (GetComponent<Collider2D>() != null)
            return;

        if (GetComponent<SpriteRenderer>() == null)
        {
            Debug.LogWarning(
                "[PositionCharacterSettingButton] SpriteRenderer is missing.",
                this);
            return;
        }

        gameObject.AddComponent<PolygonCollider2D>();
    }
}
