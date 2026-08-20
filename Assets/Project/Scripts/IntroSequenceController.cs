using System;
using System.Collections;
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

        EnsureIntroCanvasSorting();
        SetIntroVisible(false);
    }

    private void OnDestroy()
    {
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
        string line = GetLine(lineIndex);

        introText.text = line;
        introText.maxVisibleCharacters = 0;
        introText.ForceMeshUpdate();

        currentLineCharacterCount = introText.textInfo.characterCount;
        typewriterCoroutine = StartCoroutine(TypeLine());
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
