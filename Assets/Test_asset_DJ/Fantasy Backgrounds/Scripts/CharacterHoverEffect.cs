using UnityEngine;

public class CharacterHoverEffect : MonoBehaviour
{
    [Header("Visual Target")]
    [SerializeField] private Transform visualTarget;

    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float scaleSpeed = 8f;

    [Header("Outline")]
    [SerializeField] private GameObject outlineObject;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        if (visualTarget == null)
        {
            visualTarget = transform;
        }

        originalScale = visualTarget.localScale;
        targetScale = originalScale;

        if (outlineObject != null)
        {
            outlineObject.SetActive(false);
        }
    }

    private void Update()
    {
        visualTarget.localScale = Vector3.Lerp(
            visualTarget.localScale,
            targetScale,
            Time.deltaTime * scaleSpeed
        );
    }

    private void OnMouseEnter()
    {
        targetScale = originalScale * hoverScale;

        if (outlineObject != null)
        {
            outlineObject.SetActive(true);
        }
    }

    private void OnMouseExit()
    {
        targetScale = originalScale;

        if (outlineObject != null)
        {
            outlineObject.SetActive(false);
        }
    }
}