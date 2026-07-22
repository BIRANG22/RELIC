using UnityEngine;

public sealed class LobbyRelicShopNpcInteraction : MonoBehaviour
{
    [SerializeField] private LobbyRelicShopPresenter presenter;

    private void OnMouseDown()
    {
        presenter?.Open();
    }
}
