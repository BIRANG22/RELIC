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
        EnsureIndependentPanelButtonsAvailable();
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

        if (!IsAnimatorReady())
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

        while (isActiveAndEnabled && IsAnimatorReady() && targetAnimator.IsInTransition(0))
            yield return null;

        if (!isActiveAndEnabled || isClosing)
            yield break;

        if (!IsAnimatorReady())
        {
            ShowObjects();
            waitCoroutine = null;
            yield break;
        }

        AnimatorStateInfo stateInfo = targetAnimator.GetCurrentAnimatorStateInfo(0);
        int playingStateHash = stateInfo.fullPathHash;

        while (isActiveAndEnabled && !isClosing)
        {
            if (!IsAnimatorReady())
                break;

            if (!targetAnimator.IsInTransition(0))
            {
                if (!IsAnimatorReady())
                    break;

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
    /// 기존 UnityEvent 연결 호환용입니다.
    /// BackButton은 더 이상 book 역재생과 연동하지 않고 패널을 즉시 닫습니다.
    /// </summary>
    public void ClosePanelWithReverse()
    {
        if (isClosing)
            return;

        isClosing = true;
        StopRunningCoroutines();
        ClosePanelImmediately();
    }

    private IEnumerator ReverseAndClose()
    {
        if (targetAnimator == null)
            targetAnimator = GetComponent<Animator>();

        if (!IsAnimatorReady())
        {
            ClosePanelImmediately();
            yield break;
        }

        targetAnimator.enabled = true;
        targetAnimator.speed = 1f;
        targetAnimator.Update(0f);

        yield return null;

        while (IsAnimatorReady() && targetAnimator.IsInTransition(0))
            yield return null;

        if (!IsAnimatorReady())
        {
            ClosePanelImmediately();
            yield break;
        }

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


    private bool IsAnimatorReady()
    {
        return targetAnimator != null
            && targetAnimator.isActiveAndEnabled
            && targetAnimator.runtimeAnimatorController != null;
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
            if (target == null || IsIndependentPanelButton(target))
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
                if (target == null || IsIndependentPanelButton(target))
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
            if (target == null || IsIndependentPanelButton(target))
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

    private void EnsureIndependentPanelButtonsAvailable()
    {
        Transform root = erosionSelectPanel != null
            ? erosionSelectPanel.transform
            : transform.parent != null ? transform.parent : transform;

        SetIndependentButtonAvailable(FindChildRecursive(root, "PlayButton"));
        SetIndependentButtonAvailable(FindChildRecursive(root, "BackButton"));
    }

    private static void SetIndependentButtonAvailable(Transform buttonTransform)
    {
        if (buttonTransform == null)
            return;

        GameObject buttonObject = buttonTransform.gameObject;
        buttonObject.SetActive(true);

        CanvasGroup canvasGroup = buttonObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName))
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static bool IsIndependentPanelButton(GameObject target)
    {
        if (target == null)
            return false;

        return target.name == "PlayButton" || target.name == "BackButton";
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
