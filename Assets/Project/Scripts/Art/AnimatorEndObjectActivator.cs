using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지정한 Animator의 스프라이트 애니메이션이 끝난 뒤 지정 오브젝트들을 페이드 인합니다.
/// ClosePanelWithReverse를 호출하면 같은 애니메이션을 역재생한 뒤 패널을 비활성화합니다.
/// </summary>
public class AnimatorEndObjectActivator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator targetAnimator;

    [Tooltip("이 오브젝트가 활성화될 때 애니메이션을 처음부터 다시 재생합니다.")]
    [SerializeField] private bool restartAnimationOnEnable = true;

    [Tooltip("애니메이션 종료 후 오브젝트가 나타나기 전 추가 대기 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float showDelay = 0f;

    [Header("Fade In")]
    [Tooltip("0이면 즉시 나타나고, 0보다 크면 지정 시간 동안 서서히 나타납니다.")]
    [Min(0f)]
    [SerializeField] private float fadeDuration = 0.35f;

    [Tooltip("페이드가 끝난 뒤 CanvasGroup의 Raycast 차단과 Interactable을 켭니다.")]
    [SerializeField] private bool enableInteractionAfterFade = true;

    [Header("Close")]
    [Tooltip("역재생이 끝난 뒤 비활성화할 ErosionSelectPanel을 지정합니다.")]
    [SerializeField] private GameObject erosionSelectPanel;

    [Header("Objects To Show")]
    [Tooltip("애니메이션이 끝난 뒤 활성화할 오브젝트들을 지정합니다.")]
    [SerializeField] private GameObject[] objectsToShow;

    private readonly Dictionary<GameObject, CanvasGroup> canvasGroupCache = new Dictionary<GameObject, CanvasGroup>();
    private Coroutine waitCoroutine;
    private Coroutine fadeCoroutine;
    private Coroutine closeCoroutine;
    private bool isClosing;

    private void Reset()
    {
        targetAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        isClosing = false;

        if (targetAnimator == null)
            targetAnimator = GetComponent<Animator>();

        if (targetAnimator != null)
            targetAnimator.speed = 1f;

        HideObjects();
        StopRunningCoroutines();
        waitCoroutine = StartCoroutine(WaitForAnimationEnd());
    }

    private void OnDisable()
    {
        if (targetAnimator != null)
            targetAnimator.speed = 1f;

        isClosing = false;
        StopRunningCoroutines();
    }

    private void StopRunningCoroutines()
    {
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }
    }

    private IEnumerator WaitForAnimationEnd()
    {
        if (targetAnimator == null)
            targetAnimator = GetComponent<Animator>();

        if (targetAnimator == null || targetAnimator.runtimeAnimatorController == null)
        {
            ShowObjects();
            waitCoroutine = null;
            yield break;
        }

        yield return null;

        if (restartAnimationOnEnable)
        {
            targetAnimator.speed = 1f;
            targetAnimator.Play(0, 0, 0f);
            targetAnimator.Update(0f);
            yield return null;
        }

        while (isActiveAndEnabled && targetAnimator.IsInTransition(0))
            yield return null;

        if (!isActiveAndEnabled || isClosing)
            yield break;

        AnimatorStateInfo stateInfo = targetAnimator.GetCurrentAnimatorStateInfo(0);
        int playingStateHash = stateInfo.fullPathHash;

        while (isActiveAndEnabled && !isClosing)
        {
            if (!targetAnimator.IsInTransition(0))
            {
                stateInfo = targetAnimator.GetCurrentAnimatorStateInfo(0);

                if (stateInfo.fullPathHash != playingStateHash)
                    break;

                if (stateInfo.normalizedTime >= 1f)
                    break;
            }

            yield return null;
        }

        if (!isActiveAndEnabled || isClosing)
            yield break;

        if (showDelay > 0f)
            yield return new WaitForSeconds(showDelay);

        if (!isClosing)
            ShowObjects();

        waitCoroutine = null;
    }

    /// <summary>
    /// BackButton의 OnClick에 연결합니다.
    /// 현재 book 애니메이션을 끝 프레임에서 처음 프레임까지 역재생한 뒤
    /// ErosionSelectPanel을 비활성화합니다.
    /// </summary>
    public void ClosePanelWithReverse()
    {
        if (isClosing)
            return;

        isClosing = true;

        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        HideObjects();

        if (closeCoroutine != null)
            StopCoroutine(closeCoroutine);

        closeCoroutine = StartCoroutine(ReverseAndClose());
    }

    private IEnumerator ReverseAndClose()
    {
        if (targetAnimator == null)
            targetAnimator = GetComponent<Animator>();

        if (targetAnimator == null || targetAnimator.runtimeAnimatorController == null)
        {
            ClosePanelImmediately();
            yield break;
        }

        targetAnimator.enabled = true;
        targetAnimator.speed = 1f;
        targetAnimator.Update(0f);

        yield return null;

        while (targetAnimator.IsInTransition(0))
            yield return null;

        AnimatorStateInfo stateInfo = targetAnimator.GetCurrentAnimatorStateInfo(0);
        int stateHash = stateInfo.fullPathHash;

        // Animator의 음수 speed에 의존하지 않고 normalizedTime을 직접 1 -> 0으로 이동합니다.
        // 이렇게 하면 역재생이 지원되지 않는 Animator 설정에서도 닫기 애니메이션이 보장됩니다.
        float reverseDuration = Mathf.Max(0.01f, stateInfo.length);
        float reverseNormalizedTime = 1f;

        targetAnimator.speed = 0f;
        targetAnimator.Play(stateHash, 0, reverseNormalizedTime);
        targetAnimator.Update(0f);

        while (isActiveAndEnabled && reverseNormalizedTime > 0f)
        {
            reverseNormalizedTime -= Time.unscaledDeltaTime / reverseDuration;
            reverseNormalizedTime = Mathf.Clamp01(reverseNormalizedTime);

            targetAnimator.Play(stateHash, 0, reverseNormalizedTime);
            targetAnimator.Update(0f);

            yield return null;
        }

        targetAnimator.Play(stateHash, 0, 0f);
        targetAnimator.Update(0f);
        targetAnimator.speed = 1f;

        closeCoroutine = null;
        ClosePanelImmediately();
    }

    private void ClosePanelImmediately()
    {
        if (targetAnimator != null)
            targetAnimator.speed = 1f;

        if (erosionSelectPanel != null)
            erosionSelectPanel.SetActive(false);
        else if (transform.parent != null)
            transform.parent.gameObject.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void HideObjects()
    {
        if (objectsToShow == null)
            return;

        foreach (GameObject target in objectsToShow)
        {
            if (target == null)
                continue;

            CanvasGroup canvasGroup = GetOrAddCanvasGroup(target);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            target.SetActive(false);
        }
    }

    private void ShowObjects()
    {
        if (objectsToShow == null)
            return;

        if (fadeDuration <= 0f)
        {
            foreach (GameObject target in objectsToShow)
            {
                if (target == null)
                    continue;

                target.SetActive(true);

                CanvasGroup canvasGroup = GetOrAddCanvasGroup(target);
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = enableInteractionAfterFade;
                canvasGroup.blocksRaycasts = enableInteractionAfterFade;
            }

            return;
        }

        fadeCoroutine = StartCoroutine(FadeInObjects());
    }

    private IEnumerator FadeInObjects()
    {
        List<CanvasGroup> groups = new List<CanvasGroup>();

        foreach (GameObject target in objectsToShow)
        {
            if (target == null)
                continue;

            target.SetActive(true);

            CanvasGroup canvasGroup = GetOrAddCanvasGroup(target);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            groups.Add(canvasGroup);
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration && !isClosing)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i] != null)
                    groups[i].alpha = t;
            }

            yield return null;
        }

        if (!isClosing)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i] == null)
                    continue;

                groups[i].alpha = 1f;
                groups[i].interactable = enableInteractionAfterFade;
                groups[i].blocksRaycasts = enableInteractionAfterFade;
            }
        }

        fadeCoroutine = null;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        if (canvasGroupCache.TryGetValue(target, out CanvasGroup cachedGroup) && cachedGroup != null)
            return cachedGroup;

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = target.AddComponent<CanvasGroup>();

        canvasGroupCache[target] = canvasGroup;
        return canvasGroup;
    }
}
