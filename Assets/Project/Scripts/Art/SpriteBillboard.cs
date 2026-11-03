using UnityEngine;

/// <summary>
/// Keeps a sprite facing the target camera.
/// Attach this component to the sprite object.
/// </summary>
public class SpriteBillboard : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    [Header("Billboard Settings")]
    [SerializeField] private bool lockYAxis = false;

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        if (lockYAxis)
        {
            Vector3 direction = targetCamera.transform.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(-direction);
        }
        else
        {
            transform.rotation = targetCamera.transform.rotation;
        }
    }
}
