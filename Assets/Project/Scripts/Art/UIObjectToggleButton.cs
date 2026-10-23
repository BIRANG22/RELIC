using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggles Inspector-assigned objects. Showing moves each target from its
/// original local position by movementOffset; hiding returns it to the origin.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UIObjectToggleButton : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private GameObject[] targetObjects = System.Array.Empty<GameObject>();

    [Header("Initial State")]
    [SerializeField] private bool applyInitialState = false;
    [SerializeField] private bool initialActiveState = false;

    [Header("Movement (Local Position)")]
    [SerializeField] private bool animatePosition = true;
    [Tooltip("How far to move from the original local position when turned ON. Turning OFF reverses this movement.")]
    [SerializeField] private Vector3 movementOffset = new Vector3(0f, 100f, 0f);
    [Min(0f)] [SerializeField] private float showDuration = 0.25f;
    [Min(0f)] [SerializeField] private float hideDuration = 0.25f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useUnscaledTime = true;

    private Button button;
    private bool isActive;
    private readonly Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private readonly Dictionary<GameObject, Coroutine> runningTransitions = new Dictionary<GameObject, Coroutine>();

    private void Awake()
    {
        button = GetComponent<Button>();
        foreach (GameObject target in targetObjects)
        {
            if (target != null && target != gameObject && !originalPositions.ContainsKey(target))
                originalPositions.Add(target, target.transform.localPosition);
        }

        isActive = GetFirstTargetState();
        if (applyInitialState)
        {
            isActive = initialActiveState;
            ApplyTargetState(false);
        }
        else if (isActive)
        {
            // A target already visible in the scene is considered to be at
            // its ON position; the saved local position remains the OFF origin.
            ApplyTargetState(false);
        }
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();
        button.onClick.AddListener(Toggle);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(Toggle);

        foreach (KeyValuePair<GameObject, Coroutine> entry in runningTransitions)
        {
            if (entry.Value != null)
                StopCoroutine(entry.Value);
            if (entry.Key != null && originalPositions.TryGetValue(entry.Key, out Vector3 origin))
            {
                entry.Key.transform.localPosition = isActive ? origin + movementOffset : origin;
                entry.Key.SetActive(isActive);
            }
        }
        runningTransitions.Clear();
    }

    public void Toggle()
    {
        SetActive(!isActive);
    }

    public void SetActive(bool active)
    {
        isActive = active;
        ApplyTargetState(true);
    }

    private bool GetFirstTargetState()
    {
        foreach (GameObject target in targetObjects)
        {
            if (target != null && target != gameObject)
                return target.activeSelf;
        }
        return false;
    }

    private void ApplyTargetState(bool animated)
    {
        foreach (GameObject target in targetObjects)
        {
            if (target == null || target == gameObject)
                continue;

            if (!originalPositions.TryGetValue(target, out Vector3 origin))
            {
                origin = target.transform.localPosition;
                originalPositions[target] = origin;
            }

            bool interrupted = runningTransitions.TryGetValue(target, out Coroutine current);
            if (interrupted)
            {
                if (current != null)
                    StopCoroutine(current);
                runningTransitions.Remove(target);
            }

            Vector3 destination = isActive ? origin + movementOffset : origin;
            float duration = isActive ? showDuration : hideDuration;

            if (!animated || !animatePosition || duration <= 0f)
            {
                target.transform.localPosition = destination;
                target.SetActive(isActive);
                continue;
            }

            if (isActive)
            {
                Vector3 start = target.activeSelf ? target.transform.localPosition : origin;
                target.SetActive(true);
                target.transform.localPosition = start;
                runningTransitions[target] = StartCoroutine(MoveTarget(target, start, destination, duration, false));
            }
            else
            {
                if (!target.activeSelf)
                {
                    target.transform.localPosition = origin;
                    continue;
                }
                Vector3 start = target.transform.localPosition;
                runningTransitions[target] = StartCoroutine(MoveTarget(target, start, destination, duration, true));
            }
        }
    }

    private IEnumerator MoveTarget(GameObject target, Vector3 start, Vector3 end, float duration, bool deactivateAtEnd)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (target == null)
                yield break;

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.transform.localPosition = Vector3.LerpUnclamped(start, end, movementCurve.Evaluate(t));
            yield return null;
        }

        if (target != null)
        {
            target.transform.localPosition = end;
            if (deactivateAtEnd)
                target.SetActive(false);
        }
        runningTransitions.Remove(target);
    }
}
