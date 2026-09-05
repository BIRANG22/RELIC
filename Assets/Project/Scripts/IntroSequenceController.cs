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
        [Tooltip("체크하면 활성화 시 0→1, 비활성화 시 1→0으로 알파값을 변화시킵니다. UI는 CanvasGroup, SpriteRenderer는 색상 알파를 사용합니다.")]
        public bool fadeActiveState;

        [Min(0f)]
        [Tooltip("활성화/비활성화 페이드 시간입니다. 0이면 즉시 알파값을 적용합니다.")]
        public float fadeDuration = 0.5f;

        [Tooltip("0~1 페이드 진행도에 적용할 커브입니다.")]
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("활성화 시 Animator 재생")]
        [Tooltip("체크하면 대상이 활성화되는 순간 지정한 Animator State를 처음부터 재생합니다.")]
        public bool playAnimatorOnActivate;

        [Tooltip("재생할 Animator입니다. 비워두면 Target 또는 자식에서 자동으로 찾습니다.")]
        public Animator animator;

        [Tooltip("재생할 Animator State 이름입니다.")]
        public string animatorStateName;

        [Header("색상 변경")]
        [Tooltip("체크하면 Target과 자식의 UI Graphic / SpriteRenderer 색상을 변경합니다.")]
        public bool changeColor;

        [Tooltip("변경할 목표 색상입니다.")]
        public Color targetColor = Color.white;

        [Min(0f)]
        [Tooltip("색상 변경을 시작하기 전 대기 시간입니다. Time Scale의 영향을 받지 않습니다.")]
        public float colorDelay;

        [Min(0f)]
        [Tooltip("목표 색상까지 변경하는 시간입니다. 0이면 즉시 변경됩니다.")]
        public float colorDuration = 0.5f;

        [Tooltip("0~1 색상 진행도에 적용할 커브입니다.")]
        public AnimationCurve colorCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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

        [Header("연출 강제 종료")]
        [Tooltip("체크하면 이 액션이 실행된 뒤 지정한 딜레이 후 인트로 연출을 강제로 종료합니다.")]
        public bool forceFinishSequence;

        [Min(0f)]
        [Tooltip("강제 종료까지 기다릴 시간입니다. Time Scale의 영향을 받지 않습니다.")]
        public float forceFinishDelay;
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
        public Graphic[] graphics;
        public Color[] graphicColors;
        public SpriteRenderer[] spriteRenderers;
        public Color[] spriteRendererColors;
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

    [Header("Intro Camera Rendering")]
    [SerializeField] private bool useScreenSpaceCamera = true;
    [SerializeField] private Camera introRenderCamera;
    [SerializeField] private bool createDedicatedIntroCamera;
    [Min(0.01f)]
    [SerializeField] private float introPlaneDistance = 25f;
    [SerializeField] private bool enableIntroCameraPostProcessing = true;
    [SerializeField] private bool autoHideOverlayCanvases = true;
    [SerializeField] private GameObject[] hideWhileIntroVisible;
    [SerializeField] private bool autoHideGameObjectsByName = true;
    [SerializeField] private string[] autoHideGameObjectNames = { "Background" };
    [SerializeField] private GameObject[] hideGameObjectsWhileIntroVisible;

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

    [Tooltip("입력 후 다음 진행 입력을 받을 때까지의 공통 딜레이입니다. 0이면 연속 입력을 제한하지 않습니다.")]
    [Min(0f)]
    [SerializeField] private float advanceInputDelay = 0.2f;

    [Header("자동 진행")]
    [Tooltip("현재 문장이 모두 표시된 뒤 다음 문장으로 자동 진행하기까지의 대기 시간입니다. 마지막 문장에서는 이 시간이 지난 뒤 자동으로 로비로 이동합니다.")]
    [Min(0f)]
    [SerializeField] private float autoAdvanceDelay = 2f;

    [Header("문장 넘김 사운드")]
    [Tooltip("다음 문장으로 넘어갈 때 재생할 SFX입니다. AudioManager의 사운드 DB에서 선택합니다.")]
    [SerializeField, SoundId(SoundCategory.Sfx)]
    private string lineAdvanceSoundId = AudioIds.Sfx.NormalButtonClick;

    [Tooltip("문장 넘김 사운드의 볼륨입니다.")]
    [SerializeField, Range(0f, 1f)]
    private float lineAdvanceSoundVolume = 1f;

    private Coroutine typewriterCoroutine;
    private Coroutine autoAdvanceCoroutine;
    private Coroutine forceFinishCoroutine;
    private int currentLineIndex;
    private int currentLineCharacterCount;
    private bool isTyping;
    private bool isPlaying;
    private bool isTransitioning;
    private bool moveToLobbyWhenFinished;
    private float inputUnlockTime;
    private float nextAdvanceInputTime;
    private readonly Dictionary<Transform, Coroutine> objectAnimationCoroutines = new Dictionary<Transform, Coroutine>();
    private readonly Dictionary<GameObject, Coroutine> objectFadeCoroutines = new Dictionary<GameObject, Coroutine>();
    private readonly Dictionary<Transform, int> objectAnimationVersions = new Dictionary<Transform, int>();
    private readonly Dictionary<GameObject, int> objectFadeVersions = new Dictionary<GameObject, int>();
    private readonly Dictionary<GameObject, Coroutine> objectColorCoroutines = new Dictionary<GameObject, Coroutine>();
    private readonly Dictionary<GameObject, int> objectColorVersions = new Dictionary<GameObject, int>();
    private readonly List<IntroObjectInitialState> objectInitialStates = new List<IntroObjectInitialState>();
    private readonly Dictionary<SpriteRenderer, float> spriteRendererBaseAlphas = new Dictionary<SpriteRenderer, float>();
    private readonly Dictionary<Canvas, bool> hiddenCanvasEnabledStates = new Dictionary<Canvas, bool>();
    private readonly Dictionary<GraphicRaycaster, bool> hiddenGraphicRaycasterEnabledStates = new Dictionary<GraphicRaycaster, bool>();
    private readonly Dictionary<GameObject, bool> hiddenGameObjectActiveStates = new Dictionary<GameObject, bool>();
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
        CancelForceFinishSequence();
        CancelAutoAdvance();
        HideOverlayCanvasesForIntro(false);
        HideGameObjectsForIntro(false);
        EndIntroParallaxPause();

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (isIntroVisible)
            HideOverlayCanvasesForIntro(true);

        if (!isPlaying)
            return;

        // 타자 출력 중에는 화면 전환/입력 잠금보다 문장 완성을 우선 처리합니다.
        // 따라서 인트로가 막 열린 직후에도 클릭/Space/Enter로 현재 문장을 즉시 끝까지 표시할 수 있습니다.
        if (WasAdvanceInputPressed())
            Advance();
    }

    private void LateUpdate()
    {
        if (isIntroVisible)
            HideOverlayCanvasesForIntro(true);
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
        if (!isPlaying)
            return;

        // 문장이 아직 타이핑 중이면 다른 입력 제한보다 먼저 현재 문장을 완성합니다.
        // 이 입력으로 다음 문장까지 넘어가지는 않습니다.
        if (isTyping)
        {
            CompleteCurrentLineImmediately();
            // 문장을 강제로 완성한 직후에는 다음 입력에 딜레이를 걸지 않습니다.
            // 따라서 바로 이어지는 클릭/Space 입력으로 다음 문장으로 진행할 수 있습니다.
            nextAdvanceInputTime = 0f;
            return;
        }

        if (isTransitioning || Time.unscaledTime < inputUnlockTime || Time.unscaledTime < nextAdvanceInputTime)
            return;

        nextAdvanceInputTime = Time.unscaledTime + Mathf.Max(0f, advanceInputDelay);
        CancelAutoAdvance();

        int nextIndex = currentLineIndex + 1;
        if (nextIndex < GetValidLineCount())
        {
            PlayLineAdvanceSound();
            ShowLine(nextIndex);
            return;
        }

        FinishIntroWithTransition();
    }

    /// <summary>
    /// 인트로 홀드 스킵 버튼에서 호출합니다.
    /// 현재 재생 중인 인트로를 기존 종료/씬 전환 흐름을 그대로 사용하여 종료합니다.
    /// </summary>
    public void SkipIntro()
    {
        if (!isPlaying || isTransitioning)
            return;

        FinishIntroWithTransition();
    }

    /// <summary>
    /// 다음 문장으로 넘어갈 때 AudioManager에 등록된 SFX를 재생합니다.
    /// DBAudioSource와 동일하게 사운드 ID와 볼륨을 사용합니다.
    /// </summary>
    private void PlayLineAdvanceSound()
    {
        if (string.IsNullOrWhiteSpace(lineAdvanceSoundId))
            return;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"[{nameof(IntroSequenceController)}] AudioManager.Instance를 찾지 못했습니다. 문장 넘김 사운드를 재생할 수 없습니다.", this);
            return;
        }

        AudioManager.Instance.PlaySfx(lineAdvanceSoundId, Mathf.Clamp01(lineAdvanceSoundVolume));
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
        CancelForceFinishSequence();
        CancelAutoAdvance();
        ResetActionObjectsToInitialState();

        moveToLobbyWhenFinished = goToLobbyAfterFinish;
        isPlaying = true;
        isTransitioning = true;
        currentLineIndex = 0;
        inputUnlockTime = float.PositiveInfinity;
        nextAdvanceInputTime = 0f;

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
        CancelAutoAdvance();

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
        if (action == null)
            return;

        if (action.forceFinishSequence)
            StartForceFinishSequence(action.forceFinishDelay);

        if (action.target == null)
            return;

        if (action.changeActiveState)
            StartActiveStateChange(action);

        if (action.changeColor)
            StartColorChange(action);

        if (!action.animatePosition && !action.animateRotation && !action.animateScale)
            return;

        Transform targetTransform = action.target.transform;
        StopObjectAnimation(targetTransform);

        int version = BeginObjectAnimation(targetTransform);
        Coroutine coroutine = StartCoroutine(AnimateObjectAction(action, targetTransform, version));
        if (IsCurrentObjectAnimation(targetTransform, version))
            objectAnimationCoroutines[targetTransform] = coroutine;
    }

    private void StartForceFinishSequence(float delay)
    {
        CancelForceFinishSequence();
        forceFinishCoroutine = StartCoroutine(ForceFinishSequenceAfterDelay(Mathf.Max(0f, delay)));
    }

    private IEnumerator ForceFinishSequenceAfterDelay(float delay)
    {
        float elapsed = 0f;
        while (elapsed < delay)
        {
            if (!isPlaying)
            {
                forceFinishCoroutine = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        while (isPlaying && isTransitioning)
            yield return null;

        forceFinishCoroutine = null;

        if (isPlaying)
            FinishIntroWithTransition();
    }

    private void CancelForceFinishSequence()
    {
        if (forceFinishCoroutine == null)
            return;

        StopCoroutine(forceFinishCoroutine);
        forceFinishCoroutine = null;
    }

    private void StartActiveStateChange(IntroObjectAction action)
    {
        GameObject target = action.target;
        StopObjectFade(target);

        int version = BeginObjectFade(target);
        Coroutine coroutine = StartCoroutine(AnimateActiveStateChange(action, target, version));
        if (IsCurrentObjectFade(target, version))
            objectFadeCoroutines[target] = coroutine;
    }

    private IEnumerator AnimateActiveStateChange(IntroObjectAction action, GameObject target, int version)
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
            if (action.activeState)
                PlayAnimatorOnActivate(action, target);

            CompleteObjectFade(target, version);
            yield break;
        }

        CanvasGroup canvasGroup = ResolveFadeCanvasGroup(target);
        SpriteRenderer[] spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);

        if (action.activeState)
        {
            target.SetActive(true);
            PlayAnimatorOnActivate(action, target);

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            SetSpriteRendererFadeAlpha(spriteRenderers, 0f);
        }
        else
        {
            if (!target.activeSelf)
            {
                if (canvasGroup != null)
                    canvasGroup.alpha = 0f;

                SetSpriteRendererFadeAlpha(spriteRenderers, 0f);
                CompleteObjectFade(target, version);
                yield break;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(canvasGroup.alpha);
        }

        float startCanvasAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
        float targetCanvasAlpha = action.activeState ? 1f : 0f;
        float startSpriteFactor = action.activeState ? 0f : 1f;
        float targetSpriteFactor = action.activeState ? 1f : 0f;

        if (action.fadeDuration <= 0f)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = targetCanvasAlpha;

            SetSpriteRendererFadeAlpha(spriteRenderers, targetSpriteFactor);

            if (!action.activeState)
                target.SetActive(false);

            CompleteObjectFade(target, version);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < action.fadeDuration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / action.fadeDuration);
            float curveTime = action.fadeCurve != null ? action.fadeCurve.Evaluate(normalizedTime) : normalizedTime;

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.LerpUnclamped(startCanvasAlpha, targetCanvasAlpha, curveTime);

            float spriteFactor = Mathf.LerpUnclamped(startSpriteFactor, targetSpriteFactor, curveTime);
            SetSpriteRendererFadeAlpha(spriteRenderers, spriteFactor);
            yield return null;
        }

        if (target != null)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = targetCanvasAlpha;

            SetSpriteRendererFadeAlpha(spriteRenderers, targetSpriteFactor);

            if (!action.activeState)
                target.SetActive(false);

            CompleteObjectFade(target, version);
        }
    }

    private void PlayAnimatorOnActivate(IntroObjectAction action, GameObject target)
    {
        if (action == null || target == null || !action.playAnimatorOnActivate || string.IsNullOrWhiteSpace(action.animatorStateName))
            return;

        Animator animator = action.animator;
        if (animator == null)
            animator = target.GetComponent<Animator>();
        if (animator == null)
            animator = target.GetComponentInChildren<Animator>(true);
        if (animator == null)
            return;

        animator.Play(action.animatorStateName, 0, 0f);
        animator.Update(0f);
    }

    private void StartColorChange(IntroObjectAction action)
    {
        GameObject target = action.target;
        if (target == null)
            return;

        StopObjectColor(target);
        int version = BeginObjectColor(target);
        Coroutine coroutine = StartCoroutine(AnimateColorChange(action, target, version));
        if (IsCurrentObjectColor(target, version))
            objectColorCoroutines[target] = coroutine;
    }

    private IEnumerator AnimateColorChange(IntroObjectAction action, GameObject target, int version)
    {
        if (target == null)
            yield break;

        if (action.colorDelay > 0f)
        {
            float delayElapsed = 0f;
            while (delayElapsed < action.colorDelay && target != null)
            {
                delayElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (target == null)
            yield break;

        Graphic[] graphics = target.GetComponentsInChildren<Graphic>(true);
        SpriteRenderer[] spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        Color[] startGraphicColors = new Color[graphics.Length];
        Color[] startSpriteColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < graphics.Length; i++)
            if (graphics[i] != null)
                startGraphicColors[i] = graphics[i].color;

        for (int i = 0; i < spriteRenderers.Length; i++)
            if (spriteRenderers[i] != null)
                startSpriteColors[i] = spriteRenderers[i].color;

        if (action.colorDuration <= 0f)
        {
            ApplyTargetColor(graphics, spriteRenderers, startGraphicColors, startSpriteColors, action.targetColor, 1f);
            CompleteObjectColor(target, version);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < action.colorDuration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / action.colorDuration);
            float curveTime = action.colorCurve != null ? action.colorCurve.Evaluate(normalizedTime) : normalizedTime;
            ApplyTargetColor(graphics, spriteRenderers, startGraphicColors, startSpriteColors, action.targetColor, curveTime);
            yield return null;
        }

        if (target != null)
        {
            ApplyTargetColor(graphics, spriteRenderers, startGraphicColors, startSpriteColors, action.targetColor, 1f);
            CompleteObjectColor(target, version);
        }
    }

    private void ApplyTargetColor(Graphic[] graphics, SpriteRenderer[] spriteRenderers, Color[] startGraphicColors, Color[] startSpriteColors, Color targetColor, float t)
    {
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic != null)
                graphic.color = Color.LerpUnclamped(startGraphicColors[i], targetColor, t);
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer renderer = spriteRenderers[i];
            if (renderer == null)
                continue;

            Color color = Color.LerpUnclamped(startSpriteColors[i], targetColor, t);
            renderer.color = color;
            spriteRendererBaseAlphas[renderer] = color.a;
        }
    }

    private static CanvasGroup ResolveFadeCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            return canvasGroup;

        // UI Graphic이 있는 경우에만 CanvasGroup을 자동 추가합니다.
        // SpriteRenderer 전용 오브젝트에는 불필요한 CanvasGroup을 추가하지 않습니다.
        if (target.GetComponent<Graphic>() != null || target.GetComponentInChildren<Graphic>(true) != null)
            return target.AddComponent<CanvasGroup>();

        return null;
    }

    private void SetSpriteRendererFadeAlpha(SpriteRenderer[] renderers, float factor)
    {
        if (renderers == null)
            return;

        factor = Mathf.Clamp01(factor);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!spriteRendererBaseAlphas.TryGetValue(renderer, out float baseAlpha))
            {
                baseAlpha = renderer.color.a;
                spriteRendererBaseAlphas[renderer] = baseAlpha;
            }

            Color color = renderer.color;
            color.a = baseAlpha * factor;
            renderer.color = color;
        }
    }

    private IEnumerator AnimateObjectAction(IntroObjectAction action, Transform targetTransform, int version)
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
            CompleteObjectAnimation(targetTransform, version);
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
            CompleteObjectAnimation(targetTransform, version);
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
                Graphic[] graphics = action.target.GetComponentsInChildren<Graphic>(true);
                SpriteRenderer[] spriteRenderers = action.target.GetComponentsInChildren<SpriteRenderer>(true);
                Color[] graphicColors = new Color[graphics.Length];
                Color[] spriteRendererColors = new Color[spriteRenderers.Length];

                for (int k = 0; k < graphics.Length; k++)
                    if (graphics[k] != null)
                        graphicColors[k] = graphics[k].color;

                for (int k = 0; k < spriteRenderers.Length; k++)
                {
                    if (spriteRenderers[k] != null)
                        spriteRendererColors[k] = spriteRenderers[k].color;
                }

                for (int k = 0; k < spriteRenderers.Length; k++)
                {
                    SpriteRenderer renderer = spriteRenderers[k];
                    if (renderer != null && !spriteRendererBaseAlphas.ContainsKey(renderer))
                        spriteRendererBaseAlphas.Add(renderer, renderer.color.a);
                }

                objectInitialStates.Add(new IntroObjectInitialState
                {
                    target = action.target,
                    activeSelf = action.target.activeSelf,
                    localPosition = targetTransform.localPosition,
                    localRotation = targetTransform.localRotation,
                    localScale = targetTransform.localScale,
                    worldPosition = targetTransform.position,
                    worldRotation = targetTransform.rotation,
                    canvasGroupAlpha = canvasGroup != null ? canvasGroup.alpha : 1f,
                    graphics = graphics,
                    graphicColors = graphicColors,
                    spriteRenderers = spriteRenderers,
                    spriteRendererColors = spriteRendererColors
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

            if (state.graphics != null && state.graphicColors != null)
            {
                int count = Mathf.Min(state.graphics.Length, state.graphicColors.Length);
                for (int j = 0; j < count; j++)
                    if (state.graphics[j] != null)
                        state.graphics[j].color = state.graphicColors[j];
            }

            if (state.spriteRenderers != null && state.spriteRendererColors != null)
            {
                int count = Mathf.Min(state.spriteRenderers.Length, state.spriteRendererColors.Length);
                for (int j = 0; j < count; j++)
                {
                    SpriteRenderer renderer = state.spriteRenderers[j];
                    if (renderer == null)
                        continue;

                    renderer.color = state.spriteRendererColors[j];
                    spriteRendererBaseAlphas[renderer] = state.spriteRendererColors[j].a;
                }
            }

            state.target.SetActive(state.activeSelf);
        }
    }

    private int BeginObjectAnimation(Transform targetTransform)
    {
        objectAnimationVersions.TryGetValue(targetTransform, out int version);
        version++;
        objectAnimationVersions[targetTransform] = version;
        return version;
    }

    private bool IsCurrentObjectAnimation(Transform targetTransform, int version)
    {
        return targetTransform != null &&
               objectAnimationVersions.TryGetValue(targetTransform, out int currentVersion) &&
               currentVersion == version;
    }

    private void CompleteObjectAnimation(Transform targetTransform, int version)
    {
        if (!IsCurrentObjectAnimation(targetTransform, version))
            return;

        objectAnimationCoroutines.Remove(targetTransform);
        objectAnimationVersions[targetTransform] = version + 1;
    }

    private int BeginObjectFade(GameObject target)
    {
        objectFadeVersions.TryGetValue(target, out int version);
        version++;
        objectFadeVersions[target] = version;
        return version;
    }

    private bool IsCurrentObjectFade(GameObject target, int version)
    {
        return target != null &&
               objectFadeVersions.TryGetValue(target, out int currentVersion) &&
               currentVersion == version;
    }

    private void CompleteObjectFade(GameObject target, int version)
    {
        if (!IsCurrentObjectFade(target, version))
            return;

        objectFadeCoroutines.Remove(target);
        objectFadeVersions[target] = version + 1;
    }

    private int BeginObjectColor(GameObject target)
    {
        objectColorVersions.TryGetValue(target, out int version);
        version++;
        objectColorVersions[target] = version;
        return version;
    }

    private bool IsCurrentObjectColor(GameObject target, int version)
    {
        return target != null &&
               objectColorVersions.TryGetValue(target, out int currentVersion) &&
               currentVersion == version;
    }

    private void CompleteObjectColor(GameObject target, int version)
    {
        if (!IsCurrentObjectColor(target, version))
            return;

        objectColorCoroutines.Remove(target);
        objectColorVersions[target] = version + 1;
    }

    private void StopObjectColor(GameObject target)
    {
        if (target == null)
            return;

        if (objectColorCoroutines.TryGetValue(target, out Coroutine coroutine) && coroutine != null)
            StopCoroutine(coroutine);

        objectColorCoroutines.Remove(target);
        objectColorVersions.TryGetValue(target, out int version);
        objectColorVersions[target] = version + 1;
    }

    private void StopObjectAnimation(Transform targetTransform)
    {
        if (targetTransform == null)
            return;

        if (objectAnimationCoroutines.TryGetValue(targetTransform, out Coroutine coroutine) && coroutine != null)
            StopCoroutine(coroutine);

        objectAnimationCoroutines.Remove(targetTransform);

        objectAnimationVersions.TryGetValue(targetTransform, out int version);
        objectAnimationVersions[targetTransform] = version + 1;
    }

    private void StopObjectFade(GameObject target)
    {
        if (target == null)
            return;

        if (objectFadeCoroutines.TryGetValue(target, out Coroutine coroutine) && coroutine != null)
            StopCoroutine(coroutine);

        objectFadeCoroutines.Remove(target);

        objectFadeVersions.TryGetValue(target, out int version);
        objectFadeVersions[target] = version + 1;
    }

    private void StopAllObjectAnimations()
    {
        foreach (Coroutine coroutine in objectAnimationCoroutines.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        foreach (Transform targetTransform in new List<Transform>(objectAnimationCoroutines.Keys))
        {
            if (targetTransform == null)
                continue;

            objectAnimationVersions.TryGetValue(targetTransform, out int version);
            objectAnimationVersions[targetTransform] = version + 1;
        }

        objectAnimationCoroutines.Clear();

        foreach (Coroutine coroutine in objectFadeCoroutines.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        foreach (GameObject target in new List<GameObject>(objectFadeCoroutines.Keys))
        {
            if (target == null)
                continue;

            objectFadeVersions.TryGetValue(target, out int version);
            objectFadeVersions[target] = version + 1;
        }

        objectFadeCoroutines.Clear();

        foreach (Coroutine coroutine in objectColorCoroutines.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        foreach (GameObject target in new List<GameObject>(objectColorCoroutines.Keys))
        {
            if (target == null)
                continue;

            objectColorVersions.TryGetValue(target, out int version);
            objectColorVersions[target] = version + 1;
        }

        objectColorCoroutines.Clear();
    }

    private IEnumerator TypeLine()
    {
        isTyping = true;

        if (currentLineCharacterCount <= 0)
        {
            isTyping = false;
            typewriterCoroutine = null;
            ScheduleAutoAdvance();
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
        ScheduleAutoAdvance();
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
        ScheduleAutoAdvance();
    }

    private void ScheduleAutoAdvance()
    {
        CancelAutoAdvance();

        if (!isPlaying)
            return;

        autoAdvanceCoroutine = StartCoroutine(AutoAdvanceAfterDelay(currentLineIndex));
    }

    private void CancelAutoAdvance()
    {
        if (autoAdvanceCoroutine == null)
            return;

        StopCoroutine(autoAdvanceCoroutine);
        autoAdvanceCoroutine = null;
    }

    private IEnumerator AutoAdvanceAfterDelay(int scheduledLineIndex)
    {
        float elapsed = 0f;
        float delay = Mathf.Max(0f, autoAdvanceDelay);

        while (isPlaying && isTransitioning)
            yield return null;

        while (elapsed < delay)
        {
            if (!isPlaying || isTyping || currentLineIndex != scheduledLineIndex)
            {
                autoAdvanceCoroutine = null;
                yield break;
            }

            if (isTransitioning)
            {
                yield return null;
                continue;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        autoAdvanceCoroutine = null;

        if (!isPlaying || isTransitioning || isTyping || currentLineIndex != scheduledLineIndex)
            yield break;

        int nextIndex = currentLineIndex + 1;
        if (nextIndex < GetValidLineCount())
        {
            PlayLineAdvanceSound();
            ShowLine(nextIndex);
            yield break;
        }

        FinishIntroWithTransition();
    }

    private async void FinishIntroWithTransition()
    {
        if (!isPlaying || isTransitioning)
            return;

        CancelForceFinishSequence();
        CancelAutoAdvance();
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
        isIntroVisible = visible;

        if (visible)
        {
            BeginIntroParallaxPause();
            EnsureIntroCanvasSorting();
            SetIntroInputBlockerVisible(true);
            HideGameObjectsForIntro(true);
            HideOverlayCanvasesForIntro(true);
        }

        if (introRoot != null)
            introRoot.SetActive(visible);

        if (!visible)
        {
            HideOverlayCanvasesForIntro(false);
            HideGameObjectsForIntro(false);
            SetIntroInputBlockerVisible(false);
            EndIntroParallaxPause();

            if (introText != null)
            {
                introText.text = string.Empty;
                introText.maxVisibleCharacters = int.MaxValue;
            }
        }
    }

    private void HideGameObjectsForIntro(bool hidden)
    {
        if (!hidden)
        {
            RestoreGameObjectsHiddenForIntro(hiddenGameObjectActiveStates);
            return;
        }

        if (autoHideGameObjectsByName)
            CaptureNamedGameObjectsForIntro();

        SetGameObjectsHiddenForIntro(hideGameObjectsWhileIntroVisible, hiddenGameObjectActiveStates, true);
    }

    private void CaptureNamedGameObjectsForIntro()
    {
        if (autoHideGameObjectNames == null || autoHideGameObjectNames.Length == 0)
            return;

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null || target.gameObject == null)
                continue;

            if (!ShouldHideGameObjectForIntro(target.gameObject))
                continue;

            HideGameObjectForIntro(target.gameObject, hiddenGameObjectActiveStates);
        }
    }

    private bool ShouldHideGameObjectForIntro(GameObject target)
    {
        if (target == null || target == introRoot || target.GetComponentInParent<CanvasMaterialSceneTransition>(true) != null)
            return false;

        if (target.transform.parent != null)
            return false;

        for (int i = 0; i < autoHideGameObjectNames.Length; i++)
        {
            string targetName = autoHideGameObjectNames[i];
            if (string.IsNullOrWhiteSpace(targetName))
                continue;

            if (target.name == targetName)
                return true;
        }

        return false;
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

    private static void SetGameObjectsHiddenForIntro(
        GameObject[] targets,
        Dictionary<GameObject, bool> activeStates,
        bool hidden)
    {
        if (activeStates == null)
            return;

        if (!hidden)
        {
            RestoreGameObjectsHiddenForIntro(activeStates);
            return;
        }

        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
            HideGameObjectForIntro(targets[i], activeStates);
    }

    private static void HideGameObjectForIntro(
        GameObject target,
        Dictionary<GameObject, bool> activeStates)
    {
        if (target == null || activeStates == null)
            return;

        if (!activeStates.ContainsKey(target))
            activeStates.Add(target, target.activeSelf);

        target.SetActive(false);
    }

    private static void RestoreGameObjectsHiddenForIntro(Dictionary<GameObject, bool> activeStates)
    {
        if (activeStates == null)
            return;

        foreach (KeyValuePair<GameObject, bool> state in activeStates)
        {
            if (state.Key != null)
                state.Key.SetActive(state.Value);
        }

        activeStates.Clear();
    }

    public static void SetGameObjectsHiddenForIntroForTest(
        GameObject[] targets,
        Dictionary<GameObject, bool> activeStates,
        bool hidden)
    {
        SetGameObjectsHiddenForIntro(targets, activeStates, hidden);
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
            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            if (IntroHoldSkipButton.IsPointerOverSkipButton(pointerPosition))
                return false;

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
