using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BootstrapToTitleTransitionLoader : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string titleSceneName = "Title";

    [Header("Transition Canvas")]
    [SerializeField] private Canvas transitionCanvas;

    [Header("Transition Images")]
    [Tooltip("계속 보이는 검정 배경 이미지입니다.")]
    [SerializeField] private Image blackBackgroundImage;

    [Tooltip("알파값이 1에서 0으로 줄어든 뒤 타이틀 씬으로 이동할 이미지입니다.")]
    [SerializeField] private Image fadeImage;

    [Header("Fade Settings")]
    [SerializeField] private float startDelay = 0.1f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Sorting")]
    [SerializeField] private int sortingOrder = 30000;

    [Header("Cleanup")]
    [SerializeField] private bool disableCanvasBeforeLoad = true;

    private bool isLoading;

    private void Awake()
    {
        SetupTransitionCanvas();

        SetImageActive(blackBackgroundImage, true);
        SetImageActive(fadeImage, true);

        SetImageAlpha(blackBackgroundImage, 1f);
        SetImageAlpha(fadeImage, 1f);
    }

    private void Start()
    {
        if (isLoading)
        {
            return;
        }

        StartCoroutine(FadeImageThenLoadTitleRoutine());
    }

    private void SetupTransitionCanvas()
    {
        if (transitionCanvas == null)
        {
            transitionCanvas = GetComponentInChildren<Canvas>(true);
        }

        if (transitionCanvas == null)
        {
            return;
        }

        transitionCanvas.gameObject.SetActive(true);
        transitionCanvas.overrideSorting = true;
        transitionCanvas.sortingOrder = sortingOrder;
    }

    private IEnumerator FadeImageThenLoadTitleRoutine()
    {
        isLoading = true;

        SetImageActive(blackBackgroundImage, true);
        SetImageActive(fadeImage, true);

        SetImageAlpha(blackBackgroundImage, 1f);
        SetImageAlpha(fadeImage, 1f);

        if (startDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(startDelay);
        }

        if (fadeImage == null)
        {
            Debug.LogError("[BootstrapToTitleTransitionLoader] Fade Image가 연결되지 않았습니다.");
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = fadeDuration > 0f ? Mathf.Clamp01(elapsed / fadeDuration) : 1f;
            float curveValue = fadeCurve != null ? fadeCurve.Evaluate(t) : t;

            SetImageAlpha(fadeImage, 1f - curveValue);

            yield return null;
        }

        SetImageAlpha(fadeImage, 0f);

        yield return null;

        if (disableCanvasBeforeLoad && transitionCanvas != null)
        {
            transitionCanvas.gameObject.SetActive(false);
        }

        if (string.IsNullOrWhiteSpace(titleSceneName))
        {
            Debug.LogError("[BootstrapToTitleTransitionLoader] Title Scene Name이 비어 있습니다.");
            yield break;
        }

        SceneManager.LoadScene(titleSceneName, LoadSceneMode.Single);
    }

    private void SetImageActive(Image targetImage, bool active)
    {
        if (targetImage == null)
        {
            return;
        }

        targetImage.gameObject.SetActive(active);
    }

    private void SetImageAlpha(Image targetImage, float alpha)
    {
        if (targetImage == null)
        {
            return;
        }

        Color color = targetImage.color;
        color.a = Mathf.Clamp01(alpha);
        targetImage.color = color;
    }
}