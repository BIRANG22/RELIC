using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UIPerspectiveItem : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private UITrapezoidWarp trapezoidWarp;

    [Header("Y Range")]
    [SerializeField] private float minY = -200f;
    [SerializeField] private float maxY = 200f;

    [Header("Scale")]
    [SerializeField] private float nearScale = 1.15f;
    [SerializeField] private float farScale = 0.8f;

    [Header("Warp")]
    [SerializeField] private float nearTopInset = 0.05f;
    [SerializeField] private float farTopInset = 0.22f;

    [Header("Shear")]
    [SerializeField] private float shear = 0f;

    private void Reset()
    {
        rectTransform = GetComponent<RectTransform>();
        trapezoidWarp = GetComponent<UITrapezoidWarp>();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            Apply();
#endif
    }

    [ContextMenu("Apply")]
    public void Apply()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (trapezoidWarp == null)
            trapezoidWarp = GetComponent<UITrapezoidWarp>();

        float y = rectTransform.anchoredPosition.y;
        float t = Mathf.InverseLerp(maxY, minY, y);

        float scale = Mathf.Lerp(farScale, nearScale, t);
        rectTransform.localScale = new Vector3(scale, scale, 1f);

        if (trapezoidWarp != null)
        {
            trapezoidWarp.TopInsetNormalized = Mathf.Lerp(farTopInset, nearTopInset, t);
        }
    }
}