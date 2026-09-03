using UnityEngine;

public class InputManager : Singleton<InputManager>
{
    public Vector3 MouseWorldPosition { get; private set; }
    private Camera cachedMainCamera;

    public void Initialize()
    {
    }
    private void Update()
    {
        HandleMouse();
    }

    private void HandleMouse()
    {
        Camera mainCamera = GetMainCamera();
        if (mainCamera == null) return;

        MouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        MouseWorldPosition = new Vector3(MouseWorldPosition.x, MouseWorldPosition.y, 0);

        if (Input.GetMouseButtonDown(0))
        {
            EventBus.Instance.Publish(new MouseLeftClickEvent(MouseWorldPosition));
        }
    }

    private Camera GetMainCamera()
    {
        if (cachedMainCamera == null)
            cachedMainCamera = Camera.main;

        return cachedMainCamera;
    }
}
