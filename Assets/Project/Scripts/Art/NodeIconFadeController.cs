using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NodeIconFadeController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float clickDelay = 0f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.5f;
    [SerializeField, Min(0f)] private float resetDelay = 1f;
    [SerializeField] private bool useUnscaledTime = true;

    private static readonly int FadeId = Shader.PropertyToID("_Fade");

    private Material runtimeMaterial;
    private Coroutine fadeCoroutine;
    private float nextClickableTime;

    private void Awake()
    {
        if (targetImage == null)
        {
            Transform nodeIcon = transform.Find("NodeIcon");
            if (nodeIcon != null)
                targetImage = nodeIcon.GetComponent<Image>();
        }

        if (targetImage == null)
        {
            Debug.LogWarning("[NodeIconFadeController] Target Image가 연결되지 않았습니다.", this);
            return;
        }

        if (targetImage.material == null)
        {
            Debug.LogWarning("[NodeIconFadeController] Target Image에 Material이 없습니다.", targetImage);
            return;
        }

        runtimeMaterial = new Material(targetImage.material);
        targetImage.material = runtimeMaterial;

        if (!runtimeMaterial.HasProperty(FadeId))
        {
            Debug.LogWarning("[NodeIconFadeController] Material에 _Fade 프로퍼티가 없습니다.", targetImage);
            return;
        }

        runtimeMaterial.SetFloat(FadeId, 1f);
    }

    private void OnEnable()
    {
        // 다음 노드 선택 화면이 열릴 때 항상 보이는 상태로 시작합니다.
        ResetFade();
    }

    private void OnDisable()
    {
        // Fade가 0인 상태에서 오브젝트가 비활성화되어도 다음 활성화에 영향이 없도록 즉시 복구합니다.
        ResetFade();
    }

    /// <summary>
    /// Button OnClick에서 호출합니다.
    /// 페이드는 즉시 시작하고, clickDelay 동안 추가 클릭은 무시합니다.
    /// Fade가 끝난 뒤 resetDelay만큼 기다렸다가 다시 1로 복구합니다.
    /// </summary>
    public void PlayFadeOut()
    {
        if (runtimeMaterial == null || !runtimeMaterial.HasProperty(FadeId))
            return;

        float currentTime = useUnscaledTime ? Time.unscaledTime : Time.time;
        if (currentTime < nextClickableTime)
            return;

        nextClickableTime = currentTime + clickDelay;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOutRoutine());
    }

    public void ResetFade()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        nextClickableTime = 0f;

        if (runtimeMaterial != null && runtimeMaterial.HasProperty(FadeId))
            runtimeMaterial.SetFloat(FadeId, 1f);
    }

    private IEnumerator FadeOutRoutine()
    {
        runtimeMaterial.SetFloat(FadeId, 1f);

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / fadeDuration);
            runtimeMaterial.SetFloat(FadeId, Mathf.Lerp(1f, 0f, t));

            yield return null;
        }

        runtimeMaterial.SetFloat(FadeId, 0f);

        if (resetDelay > 0f)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(resetDelay);
            else
                yield return new WaitForSeconds(resetDelay);
        }

        runtimeMaterial.SetFloat(FadeId, 1f);
        fadeCoroutine = null;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
