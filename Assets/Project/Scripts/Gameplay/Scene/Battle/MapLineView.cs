using UnityEngine;
using UnityEngine.UI;

public class MapLineView : MonoBehaviour
{
    [SerializeField] private Image lineImage;

    public void Setup(Vector2 from, Vector2 to)
    {
        RectTransform rect = GetComponent<RectTransform>();

        if (rect == null)
            return;

        Vector2 direction = to - from;
        float distance = direction.magnitude;

        rect.anchoredPosition = (from + to) * 0.5f;
        rect.sizeDelta = new Vector2(8f, distance);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);

        if (lineImage != null)
            lineImage.raycastTarget = false;
    }
}