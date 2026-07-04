using UnityEngine;

public class TopSortedClickManager : Singleton<TopSortedClickManager>
{
    [SerializeField] private LayerMask clickableLayerMask = ~0;

    protected override void Awake()
    {
        base.Awake();

        if (IsDuplicateInstance)
            return;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (UIPanelButton.IsMenuPanelOpen)
            return;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(
            ray,
            Mathf.Infinity,
            clickableLayerMask
        );

        TopSortedClickTarget bestTarget = null;
        int highestOrder = int.MinValue;

        for (int i = 0; i < hits.Length; i++)
        {
            TopSortedClickTarget target =
                hits[i].collider.GetComponentInParent<TopSortedClickTarget>();

            if (target == null)
                target = hits[i].collider.GetComponentInChildren<TopSortedClickTarget>();

            if (target == null)
                continue;

            int order = target.SortingOrder;

            if (order > highestOrder)
            {
                highestOrder = order;
                bestTarget = target;
            }
        }

        if (bestTarget != null)
            bestTarget.Click();
    }
}