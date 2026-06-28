using UnityEngine;

public class UIRotateZLoop : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotateSpeed = 5f;
    [SerializeField] private bool clockwise = true;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        float direction = clockwise ? -1f : 1f;
        rectTransform.Rotate(0f, 0f, rotateSpeed * direction * Time.deltaTime);
    }
}