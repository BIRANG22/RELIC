using UnityEngine;
using UnityEngine.UI;

public class UI3DView : MonoBehaviour
{
    [Header("Render")]
    [SerializeField] private Camera uiCamera;
    [SerializeField] private RawImage targetRawImage;
    [SerializeField] private RenderTexture renderTexture;

    [Header("Raycast")]
    [SerializeField] private LayerMask interactableMask = -1;
    [SerializeField] private float rayDistance = 100f;

    public Camera UICamera => uiCamera;
    public RawImage TargetRawImage => targetRawImage;
    public LayerMask InteractableMask => interactableMask;
    public float RayDistance => rayDistance;

    private void Reset()
    {
        targetRawImage = GetComponent<RawImage>();
    }

    private void Awake()
    {
        if (uiCamera != null && renderTexture != null)
        {
            uiCamera.targetTexture = renderTexture;
        }

        if (targetRawImage != null && renderTexture != null)
        {
            targetRawImage.texture = renderTexture;
        }
    }
}