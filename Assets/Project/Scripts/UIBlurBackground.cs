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
    [SerializeField] private Canvas[] presentationCanvases = System.Array.Empty<Canvas>();
    [SerializeField] private List<GameObject> blurredUiRoots = new();
    private readonly List<GameObject> runtimeBlurredUiRoots = new();

    public float BlurRadius => blurRadius;
    public float Darken => darken;
    public float Saturation => saturation;
    public float Contrast => contrast;
    public GameObject PanelRoot => gameObject;
    public IReadOnlyList<Canvas> PresentationCanvases => presentationCanvases;
    public IEnumerable<GameObject> BlurredUiRoots => EnumerateBlurredUiRoots();

    public static UIBlurBackground EnsureForPanel(GameObject panelRoot)
    {
        if (panelRoot == null) return null;

        UIBlurBackground existing = panelRoot.GetComponent<UIBlurBackground>();
        if (existing != null) return existing;

        Debug.LogError(
            $"[UIBlurBackground] '{panelRoot.name}'에 UIBlurBackground가 미리 구성되어 있지 않습니다. " +
            "Blur 요청 Panel은 Scene/Prefab에서 Canvas Presentation Group과 함께 명시적으로 설정해야 합니다.",
            panelRoot);
        return null;
    }

    public void SetRuntimeBlurredUiRoots(IEnumerable<GameObject> roots)
    {
        runtimeBlurredUiRoots.Clear();
        if (roots == null)
            return;

        foreach (GameObject root in roots)
        {
            if (root == null || blurredUiRoots.Contains(root) || runtimeBlurredUiRoots.Contains(root))
                continue;

            runtimeBlurredUiRoots.Add(root);
        }

        if (UIBlurBackgroundManager.HasInstance)
            UIBlurBackgroundManager.Instance.RefreshPresentation();
    }

    private IEnumerable<GameObject> EnumerateBlurredUiRoots()
    {
        for (int i = 0; i < blurredUiRoots.Count; i++)
        {
            if (blurredUiRoots[i] != null)
                yield return blurredUiRoots[i];
        }

        for (int i = 0; i < runtimeBlurredUiRoots.Count; i++)
        {
            if (runtimeBlurredUiRoots[i] != null)
                yield return runtimeBlurredUiRoots[i];
        }
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
