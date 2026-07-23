using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class LobbyResearcherCultureTankInteraction : MonoBehaviour
{
    [SerializeField] private LobbyCultureTankPanelPresenter presenter;
    [SerializeField] private bool ignoreClickWhenPointerOverUi = true;

    private void Awake()
    {
        AutoBind();
        EnsureWorldCollider();
    }

    private void OnMouseDown()
    {
        if (ShouldBlockClick())
            return;

        OpenPanel();
    }

    public void OpenPanel()
    {
        AutoBind();
        presenter?.Open();
    }

    private bool ShouldBlockClick()
    {
        if (ignoreClickWhenPointerOverUi &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        if (LobbyPositionModalInputBlocker.IsBlockedByAnother(this))
            return true;

        if (SkillUpgradePanel.IsAnyPanelOpen)
            return true;

        return UIPanelButton.IsMenuPanelOpen;
    }

    private void AutoBind()
    {
        if (presenter == null)
            presenter = GetComponent<LobbyCultureTankPanelPresenter>();

        if (presenter == null)
            presenter = gameObject.AddComponent<LobbyCultureTankPanelPresenter>();
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
