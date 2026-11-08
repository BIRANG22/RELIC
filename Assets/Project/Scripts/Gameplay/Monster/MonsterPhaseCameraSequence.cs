using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Add this component only to monster prefabs that need an individual phase camera.
/// Call PlayPhaseCamera / ReturnPhaseCamera from animation events or monster phase logic.
/// Every monster instance owns its own editable camera steps.
/// </summary>
[DisallowMultipleComponent]
public class MonsterPhaseCameraSequence : MonoBehaviour
{
    [Serializable]
    public class CameraStep
    {
        public bool enabled = true;
        [Tooltip("Offset from this monster's world position (X/Y).")]
        public Vector2 offset = new Vector2(0f, 0.35f);
        public float zPosition = -13f;
        [Min(0.1f)] public float orthographicSize = 3.7f;
        [Min(0f)] public float duration = 0.3f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Shake after reaching this camera position.")]
        public bool shake;
        [Min(0f)] public float shakeDuration = 0.15f;
        [Min(0f)] public float shakeStrength = 0.1f;
        [Min(0f)] public float shakeFrequency = 20f;
        [Tooltip("How long to hold this framing after arriving, before the next step.")]
        [Min(0f)] public float holdDuration = 0.1f;
    }

    [Header("Monster-Specific Phase Camera")]
    [SerializeField] private bool enablePhaseCamera = true;
    [Tooltip("The transform to frame. Defaults to this monster's transform.")]
    [SerializeField] private Transform focusTarget;
    [Header("Camera Steps (each monster can use different zoom/shake)")]
    [SerializeField] private CameraStep[] steps = { new CameraStep() };
    [Header("Return")]
    [Tooltip("If disabled, call ReturnPhaseCamera when the phase animation finishes.")]
    [SerializeField] private bool autoReturnAfterSteps;
    [SerializeField, Min(0f)] private float returnDuration = 0.3f;
    [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Phase Animation Synchronization")]
    [Tooltip("Optional Animator that plays the phase transition. Assign it to wait for the clip as well as the camera steps.")]
    [SerializeField] private Animator phaseAnimator;
    [Tooltip("Animator STATE name (not necessarily the clip asset name). Must match exactly.")]
    [SerializeField] private string phaseStateName = "2Phase";
    [SerializeField, Min(0)] private int phaseAnimatorLayer = 0;
    [Tooltip("Maximum time to detect the phase state after the camera starts. Prevents indefinite waiting if no clip is played.")]
    [SerializeField, Min(0.05f)] private float phaseStateEnterTimeout = 1f;

    private Coroutine phaseAnimationMonitor;
    private bool phaseAnimationMonitoring;
    private bool phaseAnimationComplete = true;
    private bool phaseAnimationSeen;
    private bool phaseAnimationInterrupted;

    public bool IsPhaseAnimationComplete => phaseAnimationComplete;
    public bool WasPhaseAnimationInterrupted => phaseAnimationInterrupted;

    // Invoked by BattleCameraController for both legacy Arabella and per-monster entry points.
    public void NotifyPhaseCameraStarted()
    {
        if (phaseAnimationMonitor != null)
            StopCoroutine(phaseAnimationMonitor);
        phaseAnimationMonitor = null;
        phaseAnimationSeen = false;
        phaseAnimationInterrupted = false;
        phaseAnimationMonitoring = phaseAnimator != null && !string.IsNullOrEmpty(phaseStateName);
        phaseAnimationComplete = !phaseAnimationMonitoring;
        if (phaseAnimationMonitoring && isActiveAndEnabled)
            phaseAnimationMonitor = StartCoroutine(MonitorPhaseAnimation());
    }

    private IEnumerator MonitorPhaseAnimation()
    {
        float searchElapsed = 0f;
        int stateHash = Animator.StringToHash(phaseStateName);
        int layer = Mathf.Clamp(phaseAnimatorLayer, 0, Mathf.Max(0, phaseAnimator.layerCount - 1));
        while (phaseAnimator != null && phaseAnimator.isActiveAndEnabled && !phaseAnimationSeen && searchElapsed < phaseStateEnterTimeout)
        {
            AnimatorStateInfo current = phaseAnimator.GetCurrentAnimatorStateInfo(layer);
            AnimatorStateInfo next = phaseAnimator.GetNextAnimatorStateInfo(layer);
            if (current.shortNameHash == stateHash ||
                (phaseAnimator.IsInTransition(layer) && next.shortNameHash == stateHash))
            {
                phaseAnimationSeen = true;
                break;
            }
            searchElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!phaseAnimationSeen)
        {
            Debug.LogWarning($"[MonsterPhaseCameraSequence] State '{phaseStateName}' was not entered. Check Animator and state name.", this);
            phaseAnimationComplete = true;
            phaseAnimationMonitoring = false;
            phaseAnimationMonitor = null;
            yield break;
        }

        while (phaseAnimator != null && phaseAnimator.isActiveAndEnabled)
        {
            AnimatorStateInfo current = phaseAnimator.GetCurrentAnimatorStateInfo(layer);
            if (current.shortNameHash != stateHash)
            {
                // Entry may first be visible as NextAnimatorStateInfo during a transition.
                if (phaseAnimator.IsInTransition(layer) &&
                    phaseAnimator.GetNextAnimatorStateInfo(layer).shortNameHash == stateHash)
                {
                    yield return null;
                    continue;
                }
                // This is not a successful clip completion. Animator transitions can interrupt playback.
                phaseAnimationInterrupted = true;
                Debug.LogWarning($"[MonsterPhaseCameraSequence] State '{phaseStateName}' was interrupted before finishing. Check Animator transitions or external Play/CrossFade calls.", this);
                break;
            }
            if (current.normalizedTime >= 1f)
                break;
            yield return null;
        }
        phaseAnimationComplete = true;
        phaseAnimationMonitoring = false;
        phaseAnimationMonitor = null;
    }

    public IEnumerator WaitForPhaseAnimationCompletion()
    {
        while (phaseAnimationMonitoring && !phaseAnimationComplete)
            yield return null;
    }

    private void OnDisable()
    {
        if (phaseAnimationMonitor != null)
            StopCoroutine(phaseAnimationMonitor);
        phaseAnimationMonitor = null;
        phaseAnimationMonitoring = false;
        phaseAnimationComplete = true;
    }

    private Coroutine currentRequest;

    public Transform FocusTarget => focusTarget != null ? focusTarget : transform;
    public bool EnablePhaseCamera => enablePhaseCamera;
    public CameraStep[] Steps => steps;
    public bool AutoReturnAfterSteps => autoReturnAfterSteps;
    public float ReturnDuration => returnDuration;
    public AnimationCurve ReturnCurve => returnCurve;

    /// <summary>Animation Event: use at the desired frame in the phase clip.</summary>
    public void PlayPhaseCamera()
    {
        if (!enablePhaseCamera || !isActiveAndEnabled)
            return;
        BattleCameraController cameraController = BattleCameraController.Instance;
        if (cameraController == null || !cameraController.isActiveAndEnabled)
            return;
        if (currentRequest != null)
            StopCoroutine(currentRequest);
        currentRequest = StartCoroutine(PlayRequest(cameraController));
    }

    private IEnumerator PlayRequest(BattleCameraController cameraController)
    {
        yield return cameraController.PlayMonsterPhaseCamera(
            FocusTarget, this);
        currentRequest = null;
    }

    /// <summary>Animation Event: use at the end of the phase clip (unless auto return is enabled).</summary>
    public void ReturnPhaseCamera()
    {
        if (!isActiveAndEnabled)
            return;
        BattleCameraController cameraController = BattleCameraController.Instance;
        if (cameraController == null || !cameraController.isActiveAndEnabled)
            return;
        // Do not stop PlayRequest: it must finish every camera step.
        // The controller queues this return until the steps complete.
        StartCoroutine(ReturnRequest(cameraController));
    }

    private IEnumerator ReturnRequest(BattleCameraController cameraController)
    {
        yield return cameraController.ReturnFromMonsterPhaseCamera(this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        returnDuration = Mathf.Max(0f, returnDuration);
        if (steps == null)
            return;
        foreach (CameraStep step in steps)
        {
            if (step == null)
                continue;
            step.duration = Mathf.Max(0f, step.duration);
            step.holdDuration = Mathf.Max(0f, step.holdDuration);
            step.orthographicSize = Mathf.Max(0.1f, step.orthographicSize);
            step.shakeDuration = Mathf.Max(0f, step.shakeDuration);
            step.shakeStrength = Mathf.Max(0f, step.shakeStrength);
            step.shakeFrequency = Mathf.Max(0f, step.shakeFrequency);
        }
    }
#endif
}
