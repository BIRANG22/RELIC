using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 일반 클릭은 대상을 페이드인 후 유지합니다. 호버 중 클릭하면 페이드아웃합니다.
/// 호버로만 켜진 대상은 로컬 위치를 기준으로 이동+페이드인하고,
/// 호버 종료 시 원래 위치로 복귀+페이드아웃한 뒤 비활성화합니다.
/// </summary>
public class ActivateObjectsOnClick : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("활성화할 오브젝트")]
    [SerializeField] private GameObject[] targetObjects;

    [Header("활성화 조건")]
    [SerializeField] private bool activateOnClick = true;
    [SerializeField] private bool activateOnHover = true;

    [Header("페이드 설정")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.2f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.2f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("호버 이동 설정 (로컬 위치 기준)")]
    [SerializeField] private bool moveOnHover = true;
    [SerializeField] private Vector3 hoverMoveOffset = new Vector3(0f, 30f, 0f);
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Button button;
    private bool pointerInside;
    private readonly HashSet<GameObject> clickedTargets = new HashSet<GameObject>();
    private readonly Dictionary<GameObject, Coroutine> runningTransitions = new Dictionary<GameObject, Coroutine>();
    private readonly Dictionary<GameObject, Vector3> originalLocalPositions = new Dictionary<GameObject, Vector3>();

    private void Awake()
    {
        button = GetComponent<Button>();
        if (targetObjects == null) return;
        foreach (GameObject target in targetObjects)
            RememberOriginalPosition(target);
    }

    private void OnEnable()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(OnButtonClicked);
        pointerInside = false;
        foreach (var pair in runningTransitions)
            if (pair.Value != null) StopCoroutine(pair.Value);
        runningTransitions.Clear();
    }

    private void RememberOriginalPosition(GameObject target)
    {
        if (target != null && target != gameObject && !originalLocalPositions.ContainsKey(target))
            originalLocalPositions[target] = target.transform.localPosition;
    }

    private Vector3 OriginalPosition(GameObject target)
    {
        RememberOriginalPosition(target);
        return originalLocalPositions.TryGetValue(target, out Vector3 position)
            ? position : target.transform.localPosition;
    }

    private bool CanInteract()
    {
        return button == null || (button.IsActive() && button.IsInteractable());
    }

    private void OnButtonClicked()
    {
        if (!activateOnClick || !CanInteract() || targetObjects == null) return;

        // 호버 중 클릭하면 호버 표시를 닫습니다. 다음 포인터 진입까지 재표시하지 않습니다.
        if (activateOnHover && pointerInside)
        {
            foreach (GameObject target in targetObjects)
            {
                if (target == null || target == gameObject) continue;
                clickedTargets.Remove(target);
                TransitionTarget(target, false, true);
            }
            return;
        }

        foreach (GameObject target in targetObjects)
        {
            if (target == null || target == gameObject) continue;
            clickedTargets.Add(target);
            TransitionTarget(target, true, false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        if (activateOnHover && CanInteract()) ActivateObjects();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        if (!activateOnHover || targetObjects == null) return;
        foreach (GameObject target in targetObjects)
        {
            if (target != null && target != gameObject && !clickedTargets.Contains(target))
                TransitionTarget(target, false, true);
        }
    }

    // 기존 외부 호출 호환: 호버로 활성화하는 메서드입니다.
    public void ActivateObjects()
    {
        if (targetObjects == null) return;
        foreach (GameObject target in targetObjects)
        {
            if (target == null || target == gameObject || clickedTargets.Contains(target)) continue;
            TransitionTarget(target, true, true);
        }
    }

    private void TransitionTarget(GameObject target, bool show, bool hoverTransition)
    {
        if (target == null) return;
        Vector3 original = OriginalPosition(target);
        if (runningTransitions.TryGetValue(target, out Coroutine old) && old != null)
            StopCoroutine(old);
        runningTransitions.Remove(target);

        if (!show && !target.activeSelf) return;
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.AddComponent<CanvasGroup>();
        if (show && !target.activeSelf)
        {
            group.alpha = 0f;
            target.SetActive(true);
        }
        group.interactable = show;
        group.blocksRaycasts = show;

        Vector3 destination = hoverTransition && show && moveOnHover
            ? original + hoverMoveOffset : original;
        float duration = show ? fadeInDuration : fadeOutDuration;
        runningTransitions[target] = StartCoroutine(TransitionRoutine(target, group, show, destination, duration));
    }

    private IEnumerator TransitionRoutine(GameObject target, CanvasGroup group, bool show, Vector3 destination, float duration)
    {
        float fromAlpha = group.alpha;
        Vector3 fromPosition = target.transform.localPosition;
        float toAlpha = show ? 1f : 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (target == null || group == null) yield break;
            group.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            target.transform.localPosition = Vector3.LerpUnclamped(fromPosition, destination,
                moveCurve != null ? moveCurve.Evaluate(t) : t);
            yield return null;
        }
        if (target != null && group != null)
        {
            group.alpha = toAlpha;
            target.transform.localPosition = destination;
            if (!show) target.SetActive(false);
        }
        runningTransitions.Remove(target);
    }
}
