using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Bootstrap ���� ��ġ�Ǿ� Ÿ��Ʋ -> �κ� ������ ���� ��Ʈ�θ� ����մϴ�.
/// ���� ���� ���� �ÿ��� �� ���� ����ϸ�, ����â������ ������ �ٽ� �� �� �ֽ��ϴ�.
/// </summary>
public class IntroSequenceController : MonoBehaviour
{
    [Serializable]
    public class IntroLineAction
    {
        [Tooltip("�� ������ ������ ���� ��ȣ�Դϴ�. 0���� �����մϴ�.")]
        [Min(0)]
        public int lineIndex;

        [Tooltip("�ش� ������ ǥ�õ� �� ���ÿ� ������ ������Ʈ ���� ����Դϴ�.")]
        public IntroObjectAction[] objectActions;
    }

    [Serializable]
    public class IntroObjectAction
    {
        [Tooltip("���� ��� ������Ʈ�Դϴ�.")]
        public GameObject target;

        [Header("Ȱ��ȭ / ��Ȱ��ȭ")]
        [Tooltip("üũ�ϸ� ������ ȣ��� �� ��� ������Ʈ�� Ȱ�� ���¸� �����մϴ�.")]
        public bool changeActiveState;

        [Tooltip("Change Active State�� ���� ���� �� ������ Ȱ�� �����Դϴ�.")]
        public bool activeState = true;

        [Min(0f)]
        [Tooltip("������ ȣ��� �� Ȱ��ȭ/��Ȱ��ȭ ó���� �����ϱ� �� ��� �ð��Դϴ�. ���̵带 ����ϴ� ��� �� �ð��� ���� �� ���̵尡 ���۵˴ϴ�.")]
        public float activeStateDelay;

        [Header("Ȱ��ȭ / ��Ȱ��ȭ ���̵�")]
        [Tooltip("üũ�ϸ� Ȱ��ȭ �� 0��1, ��Ȱ��ȭ �� 1��0���� ���İ��� ��ȭ��ŵ�ϴ�. UI ������Ʈ�� CanvasGroup�� ������ �ڵ����� �߰��մϴ�.")]
        public bool fadeActiveState;

        [Min(0f)]
        [Tooltip("Ȱ��ȭ/��Ȱ��ȭ ���̵� �ð��Դϴ�. 0�̸� ��� ���İ��� �����մϴ�.")]
        public float fadeDuration = 0.5f;

        [Tooltip("0~1 ���̵� ���൵�� ������ Ŀ���Դϴ�.")]
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Transform �ִϸ��̼�")]
        [Tooltip("üũ�ϸ� Position�� ��ǥ������ �����մϴ�.")]
        public bool animatePosition;

        [Tooltip("üũ�ϸ� Rotation�� ��ǥ������ �����մϴ�.")]
        public bool animateRotation;

        [Tooltip("üũ�ϸ� Scale�� ��ǥ������ �����մϴ�.")]
        public bool animateScale;

        [Tooltip("üũ�ϸ� Local Position/Rotation�� ����ϰ�, ���� World Position/Rotation�� ����մϴ�. Scale�� �׻� Local Scale�Դϴ�.")]
        public bool useLocalTransform = true;

        public Vector3 targetPosition;
        public Vector3 targetEulerAngles;
        public Vector3 targetScale = Vector3.one;

        [Min(0f)]
        [Tooltip("�ִϸ��̼� ���� �� ��� �ð��Դϴ�. Time Scale�� ������ ���� �ʽ��ϴ�.")]
        public float delay;

        [Min(0f)]
        [Tooltip("��ǥ Transform���� �̵��ϴ� �ð��Դϴ�. 0�̸� ��� ����˴ϴ�.")]
        public float duration = 0.5f;

        [Tooltip("0~1 ���൵�� ������ �ִϸ��̼� Ŀ���Դϴ�.")]
        public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    private class IntroObjectInitialState
    {
        public GameObject target;
        public bool activeSelf;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public Vector3 worldPosition;
        public Quaternion worldRotation;
        public float canvasGroupAlpha;
    }

    private const int IntroSortingOrderFloor = 31000;
    private const int IntroSortingOrderCeiling = 31500;

    public static IntroSequenceController Instance { get; private set; }

    [Header("��Ʈ�� UI")]
    [Tooltip("���� �ؽ�Ʈ�� ������ ��Ʈ�� ��ü ��Ʈ�Դϴ�. ��ҿ��� �ڵ����� ��Ȱ��ȭ�˴ϴ�.")]
    [SerializeField] private GameObject introRoot;

    [Tooltip("Ÿ�� ȿ���� ����� �ؽ�Ʈ�Դϴ�.")]
    [SerializeField] private TMP_Text introText;

    [Header("Canvas Sorting")]
    [Tooltip("��Ʈ�� UI�� ���� �����Դϴ�. �Ϲ� UI/�ɼ�/�Ͻ��������� �տ�, �� ��ȯ���� �ڿ� ǥ�õ˴ϴ�. ������ Canvas ���� ���� �ȿ��� 31000~31500���� ���ѵ˴ϴ�.")]
    [SerializeField] private int introSortingOrder = 31000;

    [Header("Intro Camera Rendering")]
    [SerializeField] private bool useScreenSpaceCamera = true;
    [SerializeField] private Camera introRenderCamera;
    [SerializeField] private bool createDedicatedIntroCamera;
    [Min(0.01f)]
    [SerializeField] private float introPlaneDistance = 25f;
    [SerializeField] private bool enableIntroCameraPostProcessing = true;
    [SerializeField] private bool autoHideOverlayCanvases = true;
    [SerializeField] private GameObject[] hideWhileIntroVisible;

    [Header("��Ʈ�� ����")]
    [Tooltip("������� ǥ���� �����Դϴ�. �� �׸��� �� ȭ�鿡 ǥ�õ˴ϴ�.")]
    [TextArea(2, 6)]
    [SerializeField]
    private string[] introLines =
    {
        "���� �� ��� ����, ������� ���踦 �ڵ��� �����ߴ�.",
        "�׸��� �����翡 ħ�ĵ� ���� �ȿ��� ����ü���� ��Ÿ����."
    };

    [Header("���庰 ����")]
    [Tooltip("������ ǥ�õǴ� ���� ������ ������Ʈ �����Դϴ�. Line Index�� Intro Lines�� 0���� �����ϴ� �ε����� ����ϴ�.")]
    [SerializeField] private IntroLineAction[] lineActions;

    [Header("Ÿ�� ȿ��")]
    [Tooltip("1�ʿ� ǥ�õǴ� ���� ���Դϴ�.")]
    [Min(1f)]
    [SerializeField] private float charactersPerSecond = 30f;

    [Tooltip("��Ʈ�ΰ� ������ ���� ���� �Է��� ������ �ð��Դϴ�.")]
    [Min(0f)]
    [SerializeField] private float initialInputLockDuration = 0.15f;

    [Header("�Է�")]
    [Tooltip("���콺 ���� Ŭ������ ���� �ϼ�/���� ������ �����մϴ�.")]
    [SerializeField] private bool allowMouseClick = true;

    [Tooltip("Space �Ǵ� Enter Ű�� ���� �ϼ�/���� ������ �����մϴ�.")]
    [SerializeField] private bool allowKeyboardInput = true;

    private Coroutine typewriterCoroutine;
    private int currentLineIndex;
    private int currentLineCharacterCount;
    private bool isTyping;
    private bool isPlaying;
    private bool isTransitioning;
    private bool moveToLobbyWhenFinished;
    private float inputUnlockTime;
    private readonly Dictionary<Transform, Coroutine> objectAnimationCoroutines = new Dictionary<Transform, Coroutine>();
    private readonly Dictionary<GameObject, Coroutine> objectFadeCoroutines = new Dictionary<GameObject, Coroutine>();
    private readonly List<IntroObjectInitialState> objectInitialStates = new List<IntroObjectInitialState>();
    private readonly Dictionary<Canvas, bool> hiddenCanvasEnabledStates = new Dictionary<Canvas, bool>();
    private readonly Dictionary<GraphicRaycaster, bool> hiddenGraphicRaycasterEnabledStates = new Dictionary<GraphicRaycaster, bool>();
    private bool isIntroVisible;
    private Camera runtimeIntroCamera;
    private Image introInputBlocker;
    private bool hasIntroParallaxPause;

    public bool IsPlaying => isPlaying;
    public event Action IntroFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Bootstrap ������ ������� ��Ʈ�� ������Ʈ�� �� ��ȯ �Ŀ��� �����մϴ�.
        if (transform.parent != null)
            transform.SetParent(null, false);

        DontDestroyOnLoad(gameObject);

        CaptureActionObjectInitialStates();
        EnsureIntroCanvasSorting();
        SetIntroVisible(false);
    }

    private void OnDestroy()
    {
        StopAllObjectAnimations();
        HideOverlayCanvasesForIntro(false);
        EndIntroParallaxPause();

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (isIntroVisible)
            HideOverlayCanvasesForIntro(true);

        if (!isPlaying || isTransitioning || Time.unscaledTime < inputUnlockTime)
            return;

        if (WasAdvanceInputPressed())
            Advance();
    }

    private void LateUpdate()
    {
        if (isIntroVisible)
            HideOverlayCanvasesForIntro(true);
    }

    /// <summary>
    /// Ÿ��Ʋ�� ���� ���� ��ư���� ȣ���մϴ�.
    /// Ÿ��Ʋ -> ��ȯ -> ��Ʈ�� -> ��ȯ -> �κ� ������ �����մϴ�.
    /// </summary>
    public void PlayFirstTimeIntro()
    {
        BeginIntroWithTransition(true);
    }

    /// <summary>
    /// ����â�� ��Ʈ�� �ٽú��⿡�� ȣ���մϴ�.
    /// ���� ȭ�� -> ��ȯ -> ��Ʈ�� -> ��ȯ -> ���� ȭ�� ������ �����մϴ�.
    /// </summary>
    public void ReplayIntro()
    {
        BeginIntroWithTransition(false);
    }

    /// <summary>
    /// UI Button�� OnClick�� ���� ������ �� �ִ� ���� ���� �Լ��Դϴ�.
    /// </summary>
    public void Advance()
    {
        if (!isPlaying || isTransitioning || Time.unscaledTime < inputUnlockTime)
            return;

        if (isTyping)
        {
            CompleteCurrentLineImmediately();
            return;
        }

        int nextIndex = currentLineIndex + 1;
        if (nextIndex < GetValidLineCount())
        {
            ShowLine(nextIndex);
            return;
        }

        FinishIntroWithTransition();
    }

    private async void BeginIntroWithTransition(bool goToLobbyAfterFinish)
    {
        if (isPlaying || isTransitioning)
            return;

        if (introRoot == null || introText == null)
        {
            Debug.LogError("[IntroSequenceController] Intro Root �Ǵ� Intro Text�� ������� �ʾҽ��ϴ�.", this);

            if (goToLobbyAfterFinish)
                await MoveToLobbyAsync();
            else
                IntroFinished?.Invoke();

            return;
        }

        if (GetValidLineCount() <= 0)
        {
            Debug.LogWarning("[IntroSequenceController] ǥ���� ��Ʈ�� ������ �����ϴ�.", this);

            if (goToLobbyAfterFinish)
            {
                IntroSettings.MarkIntroSeen();
                await MoveToLobbyAsync();
            }
            else
            {
                IntroFinished?.Invoke();
            }

            return;
        }

        StopAllObjectAnimations();
        ResetActionObjectsToInitialState();

        moveToLobbyWhenFinished = goToLobbyAfterFinish;
        isPlaying = true;
        isTransitioning = true;
        currentLineIndex = 0;
        inputUnlockTime = float.PositiveInfinity;

        CanvasMaterialSceneTransition transition = GetSceneTransition();

        if (transition != null)
            await transition.PlayCloseAsync();

        SetIntroVisible(true);
        ShowLine(0);

        if (transition != null)
            await transition.PlayOpenAsync();

        inputUnlockTime = Time.unscaledTime + initialInputLockDuration;
        isTransitioning = false;
    }

    private void ShowLine(int lineIndex)
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        currentLineIndex = lineIndex;
        ApplyLineActions(lineIndex);
        string line = GetLine(lineIndex);

        introText.text = line;
        introText.maxVisibleCharacters = 0;
        introText.ForceMeshUpdate();

        currentLineCharacterCount = introText.textInfo.characterCount;
        typewriterCoroutine = StartCoroutine(TypeLine());
    }

    private void ApplyLineActions(int lineIndex)
    {
        if (lineActions == null || lineActions.Length == 0)
            return;

        for (int i = 0; i < lineActions.Length; i++)
        {
            IntroLineAction lineAction = lineActions[i];
            if (lineAction == null || lineAction.lineIndex != lineIndex || lineAction.objectActions == null)
                continue;

            for (int j = 0; j < lineAction.objectActions.Length; j++)
                ExecuteObjectAction(lineAction.objectActions[j]);
        }
    }

    private void ExecuteObjectAction(IntroObjectAction action)
    {
        if (action == null || action.target == null)
            return;

        if (action.changeActiveState)
            StartActiveStateChange(action);

        if (!action.animatePosition && !action.animateRotation && !action.animateScale)
            return;

        Transform targetTransform = action.target.transform;
        StopObjectAnimation(targetTransform);

        Coroutine coroutine = StartCoroutine(AnimateObjectAction(action, targetTransform));
        objectAnimationCoroutines[targetTransform] = coroutine;
    }

    private void StartActiveStateChange(IntroObjectAction action)
    {
        GameObject target = action.target;
        StopObjectFade(target);

        Coroutine coroutine = StartCoroutine(AnimateActiveStateChange(action, target));
        objectFadeCoroutines[target] = coroutine;
    }

    private IEnumerator AnimateActiveStateChange(IntroObjectAction action, GameObject target)
    {
        if (target == null)
            yield break;

        if (action.activeStateDelay > 0f)
        {
            float delayElapsed = 0f;
            while (delayElapsed < action.activeStateDelay && target != null)
            {
                delayElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (target == null)
            yield break;

        if (!action.fadeActiveState)
        {
            target.SetActive(action.activeState);
            objectFadeCoroutines.Remove(target);
            yield break;
        }

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = target.AddComponent<CanvasGroup>();

        if (action.activeState)
        {
            target.SetActive(true);
            canvasGroup.alpha = 0f;
        }
        else
        {
            if (!target.activeSelf)
            {
                canvasGroup.alpha = 0f;
                objectFadeCoroutines.Remove(target);
                yield break;
            }

            canvasGroup.alpha = Mathf.Clamp01(canvasGroup.alpha);
        }

        float startAlpha = canvasGroup.alpha;
        float targetAlpha = action.activeState ? 1f : 0f;

        if (action.fadeDuration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;

            if (!action.activeState)
                target.SetActive(false);

            objectFadeCoroutines.Remove(target);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < action.fadeDuration && target != null && canvasGroup != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / action.fadeDuration);
            float curveTime = action.fadeCurve != null ? action.fadeCurve.Evaluate(normalizedTime) : normalizedTime;
            canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, curveTime);
            yield return null;
        }

        if (target != null && canvasGroup != null)
            canvasGroup.alpha = targetAlpha;

        if (target != null && !action.activeState)
            target.SetActive(false);

        if (target != null)
            objectFadeCoroutines.Remove(target);
    }

    private IEnumerator AnimateObjectAction(IntroObjectAction action, Transform targetTransform)
    {
        if (action.delay > 0f)
        {
            float delayElapsed = 0f;
            while (delayElapsed < action.delay)
            {
                delayElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (targetTransform == null)
            yield break;

        Vector3 startPosition = action.useLocalTransform ? targetTransform.localPosition : targetTransform.position;
        Quaternion startRotation = action.useLocalTransform ? targetTransform.localRotation : targetTransform.rotation;
        Vector3 startScale = targetTransform.localScale;
        Quaternion targetRotation = Quaternion.Euler(action.targetEulerAngles);

        if (action.duration <= 0f)
        {
            ApplyTransformValues(action, targetTransform, action.targetPosition, targetRotation, action.targetScale);
            objectAnimationCoroutines.Remove(targetTransform);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < action.duration && targetTransform != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / action.duration);
            float curveTime = action.animationCurve != null ? action.animationCurve.Evaluate(normalizedTime) : normalizedTime;

            Vector3 position = Vector3.LerpUnclamped(startPosition, action.targetPosition, curveTime);
            Quaternion rotation = Quaternion.SlerpUnclamped(startRotation, targetRotation, curveTime);
            Vector3 scale = Vector3.LerpUnclamped(startScale, action.targetScale, curveTime);

            ApplyTransformValues(action, targetTransform, position, rotation, scale);
            yield return null;
        }

        if (targetTransform != null)
        {
            ApplyTransformValues(action, targetTransform, action.targetPosition, targetRotation, action.targetScale);
            objectAnimationCoroutines.Remove(targetTransform);
        }
    }

    private void ApplyTransformValues(IntroObjectAction action, Transform targetTransform, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (action.animatePosition)
        {
            if (action.useLocalTransform)
                targetTransform.localPosition = position;
            else
                targetTransform.position = position;
        }

        if (action.animateRotation)
        {
            if (action.useLocalTransform)
                targetTransform.localRotation = rotation;
            else
                targetTransform.rotation = rotation;
        }

        if (action.animateScale)
            targetTransform.localScale = scale;
    }

    private void CaptureActionObjectInitialStates()
    {
        objectInitialStates.Clear();
        HashSet<GameObject> capturedTargets = new HashSet<GameObject>();

        if (lineActions == null)
            return;

        for (int i = 0; i < lineActions.Length; i++)
        {
            IntroLineAction lineAction = lineActions[i];
            if (lineAction == null || lineAction.objectActions == null)
                continue;

            for (int j = 0; j < lineAction.objectActions.Length; j++)
            {
                IntroObjectAction action = lineAction.objectActions[j];
                if (action == null || action.target == null || !capturedTargets.Add(action.target))
                    continue;

                Transform targetTransform = action.target.transform;
                CanvasGroup canvasGroup = action.target.GetComponent<CanvasGroup>();

                objectInitialStates.Add(new IntroObjectInitialState
                {
                    target = action.target,
                    activeSelf = action.target.activeSelf,
                    localPosition = targetTransform.localPosition,
                    localRotation = targetTransform.localRotation,
                    localScale = targetTransform.localScale,
                    worldPosition = targetTransform.position,
                    worldRotation = targetTransform.rotation,
                    canvasGroupAlpha = canvasGroup != null ? canvasGroup.alpha : 1f
                });
            }
        }
    }

    private void ResetActionObjectsToInitialState()
    {
        for (int i = 0; i < objectInitialStates.Count; i++)
        {
            IntroObjectInitialState state = objectInitialStates[i];
            if (state == null || state.target == null)
                continue;

            Transform targetTransform = state.target.transform;
            targetTransform.localPosition = state.localPosition;
            targetTransform.localRotation = state.localRotation;
            targetTransform.localScale = state.localScale;

            CanvasGroup canvasGroup = state.target.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = state.canvasGroupAlpha;

            state.target.SetActive(state.activeSelf);
        }
    }

    private void StopObjectAnimation(Transform targetTransform)
    {
        if (targetTransform == null || !objectAnimationCoroutines.TryGetValue(targetTransform, out Coroutine coroutine))
            return;

        if (coroutine != null)
            StopCoroutine(coroutine);

        objectAnimationCoroutines.Remove(targetTransform);
    }

    private void StopObjectFade(GameObject target)
    {
        if (target == null || !objectFadeCoroutines.TryGetValue(target, out Coroutine coroutine))
            return;

        if (coroutine != null)
            StopCoroutine(coroutine);

        objectFadeCoroutines.Remove(target);
    }

    private void StopAllObjectAnimations()
    {
        foreach (Coroutine coroutine in objectAnimationCoroutines.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        objectAnimationCoroutines.Clear();

        foreach (Coroutine coroutine in objectFadeCoroutines.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        objectFadeCoroutines.Clear();
    }

    private IEnumerator TypeLine()
    {
        isTyping = true;

        if (currentLineCharacterCount <= 0)
        {
            isTyping = false;
            typewriterCoroutine = null;
            yield break;
        }

        float secondsPerCharacter = 1f / Mathf.Max(1f, charactersPerSecond);
        float accumulatedTime = 0f;
        int visibleCharacters = 0;

        while (visibleCharacters < currentLineCharacterCount)
        {
            accumulatedTime += Time.unscaledDeltaTime;

            while (accumulatedTime >= secondsPerCharacter &&
                   visibleCharacters < currentLineCharacterCount)
            {
                accumulatedTime -= secondsPerCharacter;
                visibleCharacters++;
                introText.maxVisibleCharacters = visibleCharacters;
            }

            yield return null;
        }

        introText.maxVisibleCharacters = currentLineCharacterCount;
        isTyping = false;
        typewriterCoroutine = null;
    }

    private void CompleteCurrentLineImmediately()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        introText.maxVisibleCharacters = currentLineCharacterCount;
        isTyping = false;
    }

    private async void FinishIntroWithTransition()
    {
        if (!isPlaying || isTransitioning)
            return;

        isTransitioning = true;

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        isTyping = false;

        CanvasMaterialSceneTransition transition = GetSceneTransition();
        if (transition != null)
            await transition.PlayCloseAsync();

        SetIntroVisible(false);

        bool shouldMoveToLobby = moveToLobbyWhenFinished;
        moveToLobbyWhenFinished = false;

        if (shouldMoveToLobby)
        {
            IntroSettings.MarkIntroSeen();

            SceneFlowManager sceneFlow = SceneFlowManager.Instance;
            if (transition != null && sceneFlow != null)
                sceneFlow.UseAlreadyClosedTransitionForNextLoad();

            isPlaying = false;
            isTransitioning = false;
            HideOverlayCanvasesForIntro(false);
            IntroFinished?.Invoke();

            await MoveToLobbyAsync();
            return;
        }

        if (transition != null)
            await transition.PlayOpenAsync();

        isPlaying = false;
        isTransitioning = false;
        HideOverlayCanvasesForIntro(false);
        IntroFinished?.Invoke();
    }

    private async Task MoveToLobbyAsync()
    {
        if (GameManager.Instance == null || GameManager.Instance.StateMachine == null)
        {
            Debug.LogError("[IntroSequenceController] GameManager �Ǵ� StateMachine�� �غ���� �ʾҽ��ϴ�.", this);

            CanvasMaterialSceneTransition transition = GetSceneTransition();
            if (transition != null && transition.IsPlaying)
                await transition.PlayOpenAsync();

            return;
        }

        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Lobby);
    }

    private CanvasMaterialSceneTransition GetSceneTransition()
    {
        if (CanvasMaterialSceneTransition.Instance != null)
            return CanvasMaterialSceneTransition.Instance;

        return FindFirstObjectByType<CanvasMaterialSceneTransition>(FindObjectsInactive.Include);
    }

    private void SetIntroVisible(bool visible)
    {
        isIntroVisible = visible;

        if (visible)
        {
            BeginIntroParallaxPause();
            EnsureIntroCanvasSorting();
            SetIntroInputBlockerVisible(true);
            HideOverlayCanvasesForIntro(true);
        }

        if (introRoot != null)
            introRoot.SetActive(visible);

        if (!visible)
        {
            HideOverlayCanvasesForIntro(false);
            SetIntroInputBlockerVisible(false);
            EndIntroParallaxPause();

            if (introText != null)
            {
                introText.text = string.Empty;
                introText.maxVisibleCharacters = int.MaxValue;
            }
        }
    }

    private void HideOverlayCanvasesForIntro(bool hidden)
    {
        if (!hidden)
        {
            RestoreCanvasesHiddenForIntro(hiddenCanvasEnabledStates, hiddenGraphicRaycasterEnabledStates);
            return;
        }

        if (autoHideOverlayCanvases)
            CaptureActiveOverlayCanvasesForIntro();

        SetCanvasesHiddenForIntro(hideWhileIntroVisible, hiddenCanvasEnabledStates, hiddenGraphicRaycasterEnabledStates, true);
    }

    private void CaptureActiveOverlayCanvasesForIntro()
    {
        Canvas introCanvas = introRoot != null ? introRoot.GetComponent<Canvas>() : null;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (!ShouldHideCanvasForIntro(canvas, introCanvas))
                continue;

            HideCanvasForIntro(canvas, hiddenCanvasEnabledStates, hiddenGraphicRaycasterEnabledStates);
        }
    }

    private static bool ShouldHideCanvasForIntro(Canvas canvas, Canvas introCanvas)
    {
        if (canvas == null || !canvas.isRootCanvas || canvas == introCanvas || IsSceneTransitionCanvas(canvas))
            return false;

        return canvas.renderMode == RenderMode.ScreenSpaceOverlay;
    }

    private static bool IsSceneTransitionCanvas(Canvas canvas)
    {
        if (canvas == null)
            return false;

        return canvas.GetComponentInParent<CanvasMaterialSceneTransition>(true) != null ||
               canvas.GetComponentInChildren<CanvasMaterialSceneTransition>(true) != null;
    }

    private static void SetCanvasesHiddenForIntro(
        GameObject[] targets,
        Dictionary<Canvas, bool> enabledStates,
        Dictionary<GraphicRaycaster, bool> raycasterEnabledStates,
        bool hidden)
    {
        if (enabledStates == null)
            return;

        if (!hidden)
        {
            RestoreCanvasesHiddenForIntro(enabledStates, raycasterEnabledStates);
            return;
        }

        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            GameObject target = targets[i];
            if (target == null)
                continue;

            Canvas canvas = target.GetComponent<Canvas>();
            if (canvas != null)
                HideCanvasForIntro(canvas, enabledStates, raycasterEnabledStates);
        }
    }

    private static void HideCanvasForIntro(
        Canvas canvas,
        Dictionary<Canvas, bool> enabledStates,
        Dictionary<GraphicRaycaster, bool> raycasterEnabledStates)
    {
        if (canvas == null || enabledStates == null)
            return;

        if (!enabledStates.ContainsKey(canvas))
            enabledStates.Add(canvas, canvas.enabled);

        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster != null && raycasterEnabledStates != null)
        {
            if (!raycasterEnabledStates.ContainsKey(raycaster))
                raycasterEnabledStates.Add(raycaster, raycaster.enabled);

            raycaster.enabled = false;
        }

        canvas.enabled = false;
    }

    private static void RestoreCanvasesHiddenForIntro(
        Dictionary<Canvas, bool> enabledStates,
        Dictionary<GraphicRaycaster, bool> raycasterEnabledStates)
    {
        if (enabledStates != null)
        {
            foreach (KeyValuePair<Canvas, bool> state in enabledStates)
            {
                if (state.Key != null)
                    state.Key.enabled = state.Value;
            }

            enabledStates.Clear();
        }

        if (raycasterEnabledStates == null)
            return;

        foreach (KeyValuePair<GraphicRaycaster, bool> state in raycasterEnabledStates)
        {
            if (state.Key != null)
                state.Key.enabled = state.Value;
        }

        raycasterEnabledStates.Clear();
    }

    public static void SetCanvasesHiddenForIntroForTest(
        GameObject[] targets,
        Dictionary<Canvas, bool> enabledStates,
        bool hidden)
    {
        SetCanvasesHiddenForIntro(targets, enabledStates, new Dictionary<GraphicRaycaster, bool>(), hidden);
    }

    public static void SetCanvasesHiddenForIntroForTest(
        GameObject[] targets,
        Dictionary<Canvas, bool> enabledStates,
        Dictionary<GraphicRaycaster, bool> raycasterEnabledStates,
        bool hidden)
    {
        SetCanvasesHiddenForIntro(targets, enabledStates, raycasterEnabledStates, hidden);
    }
    private void BeginIntroParallaxPause()
    {
        if (hasIntroParallaxPause)
            return;

        CameraMouseParallaxController.BeginIntroPause();
        hasIntroParallaxPause = true;
    }

    private void EndIntroParallaxPause()
    {
        if (!hasIntroParallaxPause)
            return;

        hasIntroParallaxPause = false;
        CameraMouseParallaxController.EndIntroPause();
    }
    private void SetIntroInputBlockerVisible(bool visible)
    {
        if (introRoot == null)
            return;

        if (introInputBlocker == null)
            introInputBlocker = CreateIntroInputBlocker(introRoot.transform);

        if (introInputBlocker == null)
            return;

        introInputBlocker.gameObject.SetActive(visible);
        introInputBlocker.raycastTarget = visible;

        if (visible)
            introInputBlocker.transform.SetAsFirstSibling();
    }

    private static Image CreateIntroInputBlocker(Transform parent)
    {
        if (parent == null)
            return null;

        GameObject blockerObject = new GameObject("IntroInputBlocker");
        blockerObject.layer = parent.gameObject.layer;
        blockerObject.transform.SetParent(parent, false);

        RectTransform rectTransform = blockerObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        Image blocker = blockerObject.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0f);
        blocker.raycastTarget = false;
        blockerObject.SetActive(false);
        return blocker;
    }
    private void EnsureIntroCanvasSorting()
    {
        if (introRoot == null)
            return;

        Canvas introCanvas = introRoot.GetComponent<Canvas>();
        if (introCanvas == null)
            introCanvas = introRoot.AddComponent<Canvas>();

        ConfigureIntroCanvas(
            introCanvas,
            ResolveIntroRenderCamera(),
            useScreenSpaceCamera,
            enableIntroCameraPostProcessing,
            introPlaneDistance,
            Mathf.Clamp(introSortingOrder, IntroSortingOrderFloor, IntroSortingOrderCeiling));
    }

    private Camera ResolveIntroRenderCamera()
    {
        if (createDedicatedIntroCamera)
        {
            if (runtimeIntroCamera != null)
                return runtimeIntroCamera;

            GameObject cameraObject = new GameObject("Intro UI Camera");
            DontDestroyOnLoad(cameraObject);

            runtimeIntroCamera = cameraObject.AddComponent<Camera>();
            runtimeIntroCamera.clearFlags = CameraClearFlags.Depth;
            runtimeIntroCamera.orthographic = true;
            runtimeIntroCamera.orthographicSize = 5f;
            runtimeIntroCamera.nearClipPlane = 0.01f;
            runtimeIntroCamera.farClipPlane = 100f;
            runtimeIntroCamera.depth = 100f;
            int uiLayer = LayerMask.NameToLayer("UI");
            runtimeIntroCamera.cullingMask = uiLayer >= 0 ? 1 << uiLayer : 0;

            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = enableIntroCameraPostProcessing;
            return runtimeIntroCamera;
        }

        if (introRenderCamera != null)
            return introRenderCamera;

        introRenderCamera = Camera.main;
        return introRenderCamera;
    }

    private static void ConfigureIntroCanvas(
        Canvas introCanvas,
        Camera renderCamera,
        bool useScreenSpaceCamera,
        bool enablePostProcessing,
        float planeDistance,
        int sortingOrder)
    {
        if (introCanvas == null)
            return;

        introCanvas.overrideSorting = true;
        introCanvas.sortingOrder = sortingOrder;

        if (!useScreenSpaceCamera)
        {
            introCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            introCanvas.worldCamera = null;
            return;
        }

        introCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        introCanvas.worldCamera = renderCamera;
        introCanvas.planeDistance = Mathf.Max(0.01f, planeDistance);

        if (renderCamera == null || !enablePostProcessing)
            return;

        UniversalAdditionalCameraData cameraData = renderCamera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData != null)
            cameraData.renderPostProcessing = true;
    }

    public static void ConfigureIntroCanvasForTest(
        Canvas introCanvas,
        Camera renderCamera,
        bool useScreenSpaceCamera,
        bool enablePostProcessing,
        float planeDistance,
        int sortingOrder)
    {
        ConfigureIntroCanvas(
            introCanvas,
            renderCamera,
            useScreenSpaceCamera,
            enablePostProcessing,
            planeDistance,
            sortingOrder);
    }

    private bool WasAdvanceInputPressed()
    {
        if (allowMouseClick &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        if (!allowKeyboardInput || Keyboard.current == null)
            return false;

        return Keyboard.current.spaceKey.wasPressedThisFrame ||
               Keyboard.current.enterKey.wasPressedThisFrame ||
               Keyboard.current.numpadEnterKey.wasPressedThisFrame;
    }

    private int GetValidLineCount()
    {
        return introLines != null ? introLines.Length : 0;
    }

    private string GetLine(int index)
    {
        if (introLines == null || index < 0 || index >= introLines.Length)
            return string.Empty;

        return introLines[index] ?? string.Empty;
    }
}
