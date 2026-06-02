using UnityEngine;

public class StartRoomNpcClick : MonoBehaviour
{
    [SerializeField] private StartRoomController startRoomController;

    private void OnMouseDown()
    {
        if (startRoomController != null)
            startRoomController.OnNpcClicked();
    }
}