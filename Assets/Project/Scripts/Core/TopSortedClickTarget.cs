using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class TopSortedClickTarget : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private UnityEvent onClicked;

    public int SortingOrder
    {
        get
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<SpriteRenderer>();

            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<SpriteRenderer>();

            return targetRenderer != null ? targetRenderer.sortingOrder : 0;
        }
    }

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Click()
    {
        onClicked?.Invoke();
    }
}