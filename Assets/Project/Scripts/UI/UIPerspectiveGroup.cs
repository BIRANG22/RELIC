using System.Collections.Generic;
using UnityEngine;

public class UIPerspectiveGroup : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private List<RectTransform> items = new();

    [Header("Y Range")]
    [SerializeField] private float minY = -200f;
    [SerializeField] private float maxY = 200f;

    [Header("Scale By Y")]
    [SerializeField] private float nearScale = 1.15f; // 아래쪽
    [SerializeField] private float farScale = 0.8f;   // 위쪽

    [Header("Optional X Offset")]
    [SerializeField] private float perspectiveXOffset = 30f;

    [Header("Depth Sorting")]
    [SerializeField] private bool sortByY = true;

    private void Reset()
    {
        container = transform as RectTransform;
        CollectChildren();
    }

    [ContextMenu("Collect Children")]
    public void CollectChildren()
    {
        items.Clear();

        foreach (Transform child in transform)
        {
            if (child is RectTransform rt)
                items.Add(rt);
        }
    }

    [ContextMenu("Apply Perspective")]
    public void ApplyPerspective()
    {
        if (items.Count == 0)
            CollectChildren();

        foreach (var item in items)
        {
            if (item == null)
                continue;

            Vector2 pos = item.anchoredPosition;

            float t = Mathf.InverseLerp(maxY, minY, pos.y);
            // pos.y가 아래(minY)에 가까울수록 t=1, 위(maxY)에 가까울수록 t=0

            float scale = Mathf.Lerp(farScale, nearScale, t);
            item.localScale = new Vector3(scale, scale, 1f);

            // 원하면 y에 따라 x를 살짝 벌리거나 모을 수 있음
            float normalized = Mathf.Lerp(-1f, 1f, t);
            pos.x += normalized * perspectiveXOffset * 0f; // 필요하면 0f를 1f로
            item.anchoredPosition = pos;
        }

        if (sortByY)
        {
            items.Sort((a, b) => a.anchoredPosition.y.CompareTo(b.anchoredPosition.y));

            for (int i = 0; i < items.Count; i++)
            {
                items[i].SetSiblingIndex(i);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ApplyPerspective();
        }
    }
#endif
}