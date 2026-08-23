using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bootstrap 씬에 배치되어 타이틀 -> 로비 사이의 게임 인트로를 재생합니다.
/// 최초 게임 시작 시에는 한 번만 재생하며, 설정창에서는 언제든 다시 볼 수 있습니다.
/// </summary>
public class IntroSequenceController : MonoBehaviour
{
    [Serializable]
    public class IntroLineAction
    {
        [Tooltip("이 연출을 실행할 문장 번호입니다. 0부터 시작합니다.")]
        [Min(0)]
        public int lineIndex;

        [Tooltip("해당 문장이 표시될 때 동시에 실행할 오브젝트 연출 목록입니다.")]
        public IntroObjectAction[] objectActions;
    }

    [Serializable]
    public class IntroObjectAction
    {
        [Tooltip("연출 대상 오브젝트입니다.")]
        public GameObject target;

        [Header("활성화 / 비활성화")]
        [Tooltip("체크하면 문장이 호출될 때 대상 오브젝트의 활성 상태를 변경합니다.")]
        public bool changeActiveState;

        [Tooltip("Change Active State가 켜져 있을 때 적용할 활성 상태입니다.")]
        public bool activeState = true;

        [Min(0f)]
        [Tooltip("문장이 호출된 뒤 활성화/비활성화 처리를 시작하기 전 대기 시간입니다. 페이드를 사용하는 경우 이 시간이 지난 뒤 페이드가 시작됩니다.")]
        public float activeStateDelay;

        [Header("활성화 / 비활성화 페이드")]
        [Tooltip("체크하면 활성화 시 0→1, 비활성화 시 1→0으로 알파값을 변화시킵니다. UI 오브젝트에 CanvasGroup이 없으면 자동으로 추가합니다.")]
        public bool fadeActiveState;

        [Min(0f)]
        [Tooltip("활성화/비활성화 페이드 시간입니다. 0이면 즉시 알파값을 적용합니다.")]
        public float fadeDuration = 0.5f;

        [Tooltip("0~1 페이드 진행도에 적용할 커브입니다.")]
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Transform 애니메이션")]
        [Tooltip("체크하면 Position을 목표값까지 변경합니다.")]
        public bool animatePosition;

        [Tooltip("체크하면 Rotation을 목표값까지 변경합니다.")]
        public bool animateRotation;

        [Tooltip("체크하면 Scale을 목표값까지 변경합니다.")]
        public bool animateScale;

        [Tooltip("체크하면 Local Position/Rotation을 사용하고, 끄면 World Position/Rotation을 사용합니다. Scale은 항상 Local Scale입니다.")]
        public bool useLocalTransform = true;

        public Vector3 targetPosition;
        public Vector3 targetEulerAngles;
        public Vector3 targetScale = Vector3.one;

        [Min(0f)]
        [Tooltip("애니메이션 시작 전 대기 시간입니다. Time Scale의 영향을 받지 않습니다.")]
        public float delay;

        [Min(0f)]
        [Tooltip("목표 Transform까지 이동하는 시간입니다. 0이면 즉시 적용됩니다.")]
        public float duration = 0.5f;

        [Tooltip("0~1 진행도에 적용할 애니메이션 커브입니다.")]
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

    [Header("인트로 UI")]
    [Tooltip("배경과 텍스트를 포함한 인트로 전체 루트입니다. 평소에는 자동으로 비활성화됩니다.")]
    [SerializeField] private GameObject introRoot;

    [Tooltip("타자 효과가 적용될 텍스트입니다.")]
    [SerializeField] private TMP_Text introText;

    [Header("Canvas Sorting")]
    [Tooltip("인트로 UI의 정렬 순서입니다. 일반 UI/옵션/일시정지보다 앞에, 씬 전환보다 뒤에 표시됩니다. 안전한 Canvas 정렬 범위 안에서 31000~31500으로 제한됩니다.")]
    [SerializeField] private int introSortingOrder = 31000;

    [Header("인트로 문장")]
    [Tooltip("순서대로 표시할 문장입니다. 한 항목이 한 화면에 표시됩니다.")]
    [TextArea(2, 6)]
    [SerializeField]
    private string[] introLines =
    {
        "원인 모를 재앙 이후, 잿가루는 세계를 뒤덮기 시작했다.",
        "그리고 잿가루에 침식된 구역 안에서 변이체들이 나타났다."
    };

    [Header("문장별 연출")]
    [Tooltip("문장이 표시되는 순간 실행할 오브젝트 연출입니다. Line Index는 Intro Lines의 0부터 시작하는 인덱스와 맞춥니다.")]
    [SerializeField] private IntroLineAction[] lineActions;

    [Header("타자 효과")]
    [Tooltip("1초에 표시되는 글자 수입니다.")]
    [Min(1f)]
    [SerializeField] private float charactersPerSecond = 30f;

    [Tooltip("인트로가 완전히 열린 직후 입력을 무시할 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float initialInputLockDuration = 0.15f;

    [Header("입력")]
    [Tooltip("마우스 왼쪽 클릭으로 문장 완성/다음 문장을 진행합니다.")]
    [SerializeField] private bool allowMouseClick = true;

    [Tooltip("Space 또는 Enter 키로 문장 완성/다음 문장을 진행합니다.")]
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

        // Bootstrap 씬에서 만들어진 인트로 오브젝트만 씬 전환 후에도 유지합니다.
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

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!isPlaying || isTransitioning || Time.unscaledTime < inputUnlockTime)
            return;

        if (WasAdvanceInputPressed())
            Advance();
    }

    /// <summary>
    /// 타이틀의 게임 시작 버튼에서 호출합니다.
    /// 타이틀 -> 전환 -> 인트로 -> 전환 -> 로비 순서로 진행합니다.
    /// </summary>
    public void PlayFirstTimeIntro()
    {
        BeginIntroWithTransition(true);
    }

    /// <summary>
    /// 설정창의 인트로 다시보기에서 호출합니다.
    /// 현재 화면 -> 전환 -> 인트로 -> 전환 -> 현재 화면 순서로 진행합니다.
    /// </summary>
    public void ReplayIntro()
    {
        BeginIntroWithTransition(false);
    }

    /// <summary>
    /// UI Button의 OnClick에 직접 연결할 수 있는 다음 진행 함수입니다.
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
            Debug.LogError("[IntroSequenceController] Intro Root 또는 Intro Text가 연결되지 않았습니다.", this);

            if (goToLobbyAfterFinish)
                await MoveToLobbyAsync();
            else
                IntroFinished?.Invoke();

            return;
        }

        if (GetValidLineCount() <= 0)
        {
            Debug.LogWarning("[IntroSequenceController] 표시할 인트로 문장이 없습니다.", this);

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
            IntroFinished?.Invoke();

            await MoveToLobbyAsync();
            return;
        }

        if (transition != null)
            await transition.PlayOpenAsync();

        isPlaying = false;
        isTransitioning = false;
        IntroFinished?.Invoke();
    }

    private async Task MoveToLobbyAsync()
    {
        if (GameManager.Instance == null || GameManager.Instance.StateMachine == null)
        {
            Debug.LogError("[IntroSequenceController] GameManager 또는 StateMachine이 준비되지 않았습니다.", this);

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
        if (visible)
            EnsureIntroCanvasSorting();

        if (introRoot != null)
            introRoot.SetActive(visible);

        if (!visible && introText != null)
        {
            introText.text = string.Empty;
            introText.maxVisibleCharacters = int.MaxValue;
        }
    }


    private void EnsureIntroCanvasSorting()
    {
        if (introRoot == null)
            return;

        Canvas introCanvas = introRoot.GetComponent<Canvas>();
        if (introCanvas == null)
            introCanvas = introRoot.AddComponent<Canvas>();

        introCanvas.overrideSorting = true;
        introCanvas.sortingOrder = Mathf.Clamp(introSortingOrder, IntroSortingOrderFloor, IntroSortingOrderCeiling);
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
