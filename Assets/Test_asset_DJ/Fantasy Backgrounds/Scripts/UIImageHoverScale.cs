using UnityEngine;
using UnityEngine.EventSystems;

public class UIImageHoverScale : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float scaleSpeed = 8f;

    [Header("Images To Toggle")]
    [SerializeField] private GameObject imageA;
    [SerializeField] private GameObject imageB;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private bool isHovering = false;
    private bool isFixed = false;
    private bool clickedThisFrame = false;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        SetImages(false);
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * scaleSpeed
        );

        if (clickedThisFrame)
        {
            clickedThisFrame = false;
            return;
        }

        if (!isFixed)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            ReleaseFixedState();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;

        if (!isFixed)
        {
            targetScale = originalScale;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        clickedThisFrame = true;

        isFixed = true;
        targetScale = originalScale * hoverScale;

        SetImages(true);
    }

    private void ReleaseFixedState()
    {
        isFixed = false;
        SetImages(false);

        targetScale = isHovering
            ? originalScale * hoverScale
            : originalScale;
    }

    private void SetImages(bool active)
    {
        if (imageA != null)
            imageA.SetActive(active);

        if (imageB != null)
            imageB.SetActive(active);
    }
}