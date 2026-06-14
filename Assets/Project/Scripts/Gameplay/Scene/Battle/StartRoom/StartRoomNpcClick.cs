using UnityEngine;

public class StartRoomNpcClick : MonoBehaviour
{
    [Header("Click")]
    [SerializeField] private StartRoomController startRoomController;

    [Header("Hover Scale")]
    [SerializeField] private bool useHoverScale = true;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float scaleSpeed = 12f;

    private Vector3 defaultScale;
    private Vector3 targetScale;
    private bool initialized;

    private void Awake()
    {
        InitializeScale();
    }

    private void OnEnable()
    {
        InitializeScale();
        targetScale = defaultScale;
        transform.localScale = defaultScale;
    }

    private void Update()
    {
        if (!useHoverScale)
            return;

        InitializeScale();

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * scaleSpeed
        );
    }

    private void OnMouseEnter()
    {
        if (!useHoverScale)
            return;

        InitializeScale();
        targetScale = defaultScale * hoverScale;
    }

    private void OnMouseExit()
    {
        if (!useHoverScale)
            return;

        InitializeScale();
        targetScale = defaultScale;
    }

    private void OnMouseDown()
    {
        if (startRoomController != null)
            startRoomController.OnNpcClicked();
    }

    private void InitializeScale()
    {
        if (initialized)
            return;

        defaultScale = transform.localScale;
        targetScale = defaultScale;
        initialized = true;
    }
}
