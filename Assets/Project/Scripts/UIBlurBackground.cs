using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class UIBlurBackground : MonoBehaviour
{
    [SerializeField, Range(0f, 8f)] private float blurRadius = 8f;
    [SerializeField, Range(0f, 1f)] private float darken = .75f;
    [SerializeField, Range(0f, 1.5f)] private float saturation = .4f;
    [SerializeField, Range(0f, 2f)] private float contrast = .8f;

    public float BlurRadius => blurRadius;
    public float Darken => darken;
    public float Saturation => saturation;
    public float Contrast => contrast;

    public static UIBlurBackground EnsureForPanel(GameObject panelRoot)
    {
        if (panelRoot == null) return null;
        UIBlurBackground existing = panelRoot.GetComponentInChildren<UIBlurBackground>(true);
        if (existing != null) return existing;

        GameObject background = new("__AutoBlurBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UIBlurBackground));
        background.transform.SetParent(panelRoot.transform, false);
        background.transform.SetAsFirstSibling();
        RectTransform rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = background.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = false;
        return background.GetComponent<UIBlurBackground>();
    }

    // 기존 호출부 호환용입니다. Shared Blur는 UI 캡처 대상 목록을 사용하지 않습니다.
    public void SetRuntimeBlurredUiRoots(IEnumerable<GameObject> roots)
    {
    }

    private void OnEnable() => UIBlurBackgroundManager.Instance.Request(this);

    private void OnDisable()
    {
        if (UIBlurBackgroundManager.HasInstance)
            UIBlurBackgroundManager.Instance.Release(this);
    }

    private void OnDestroy()
    {
        if (UIBlurBackgroundManager.HasInstance)
            UIBlurBackgroundManager.Instance.Release(this);
    }
}
