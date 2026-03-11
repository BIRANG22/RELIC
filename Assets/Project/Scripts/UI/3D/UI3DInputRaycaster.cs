using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(UI3DView))]
public class UI3DInputRaycaster : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private UI3DView view;

    private void Reset()
    {
        view = GetComponent<UI3DView>();
    }

    private void Awake()
    {
        if (view == null)
            view = GetComponent<UI3DView>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (view == null || view.UICamera == null || view.TargetRawImage == null)
            return;

        if (!TryBuildRay(eventData.position, eventData.pressEventCamera, out Ray ray))
            return;

        if (Physics.Raycast(ray, out RaycastHit hit, view.RayDistance, view.InteractableMask))
        {
            var clickable = hit.collider.GetComponentInParent<UI3DClickable>();
            if (clickable != null)
            {
                clickable.OnClick(hit);
            }

            Debug.Log($"UI3D Click Hit: {hit.collider.name}");
        }
    }

    public bool TryBuildRay(Vector2 screenPosition, Camera eventCamera, out Ray ray)
    {
        ray = default;

        RectTransform rectTransform = view.TargetRawImage.rectTransform;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                screenPosition,
                eventCamera,
                out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = rectTransform.rect;

        if (!rect.Contains(localPoint))
            return false;

        float u = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float v = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

        Rect uvRect = view.TargetRawImage.uvRect;
        u = uvRect.x + u * uvRect.width;
        v = uvRect.y + v * uvRect.height;

        ray = view.UICamera.ViewportPointToRay(new Vector3(u, v, 0f));
        return true;
    }
}