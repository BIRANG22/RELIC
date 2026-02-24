using UnityEngine;

public class InputManager : Singleton<InputManager>
{
    public Vector3 MouseWorldPosition { get; private set; }

    public void Initialize()
    {
        Debug.Log("InputManager Initialized");
    }
    private void Update()
    {
        HandleMouse();
    }

    private void HandleMouse()
    {
        if (Camera.main == null) return;

        MouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        MouseWorldPosition = new Vector3(MouseWorldPosition.x, MouseWorldPosition.y, 0);

        if (Input.GetMouseButtonDown(0))
        {
            EventBus.Instance.Publish(new MouseLeftClickEvent(MouseWorldPosition));
        }
    }
}