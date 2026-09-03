using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyTutorialController : MonoBehaviour
{

    private enum DialogueMode
    {
        None,
        Intro,
        FirstExpedition
    }

    [Header("Dialogue")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image npcImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button nextButton;
    [SerializeField] private RectTransform nextButtonIndicator;
    [SerializeField] private string speakerName = "엘릭";

    [Header("Scene Transition Timing")]
    [Tooltip("로비 열림 전환이 끝나기 몇 초 전에 최초 튜토리얼 대화를 시작할지 설정합니다.")]
    [Min(0f)]
    [SerializeField] private float tutorialStartBeforeTransitionEnd = 0.15f;

    [Header("Text Typewriter")]
    [Tooltip("1초에 표시할 글자 수입니다.")]
    [Min(1f)]
    [SerializeField] private float charactersPerSecond = 30f;

    [Header("Next Button Indicator")]
    [Tooltip("NextButton의 자식 Image가 위아래로 움직이는 거리입니다.")]
    [Min(0f)]
    [SerializeField] private float indicatorMoveDistance = 6f;

    [Tooltip("NextButton의 자식 Image가 위아래로 움직이는 속도입니다.")]
    [Min(0f)]
    [SerializeField] private float indicatorMoveSpeed = 2.5f;

    [Header("Tutorial Display")]
    [SerializeField] private GameObject tutorialDisplay;
    [SerializeField] private GameObject fragmentGroup;
    [SerializeField] private Image[] fragmentImages = new Image[3];

    [Header("Fragment Transfer Animation")]
    [Tooltip("튜토리얼 종료 후 Fragment01~03이 날아갈 SettingButton 위치입니다. 비워두면 이름이 SettingButton인 오브젝트를 자동으로 찾습니다.")]
    [SerializeField] private RectTransform fragmentTransferTarget;
    [Tooltip("유물 구매 이동 연출과 동일하게 사용할 RelicPurchaseTransferEffect Texture2D입니다. Texture Type은 Default를 사용할 수 있습니다.")]
    [SerializeField] private Texture2D fragmentTransferEffectTexture;
    [Tooltip("생성되는 RelicPurchaseTransferEffect의 크기입니다.")]
    [SerializeField] private Vector2 fragmentTransferEffectSize = new Vector2(96f, 96f);
    [Tooltip("파편이 처음 오른쪽 위로 튀어 오르는 UI 이동량입니다.")]
    [SerializeField] private Vector2 fragmentTransferBounceOffset = new Vector2(180f, 120f);
    [Tooltip("파편이 처음 튀어 오르는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float fragmentTransferBounceDuration = 0.18f;
    [Tooltip("튀어 오른 뒤 SettingButton까지 이동하는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float fragmentTransferFlyDuration = 0.32f;
    [Tooltip("Fragment01~03이 RelicPurchaseTransferEffect로 촤라락 교체되는 간격입니다.")]
    [SerializeField, Min(0f)] private float fragmentTransferSwapInterval = 0.08f;
    [Tooltip("각 RelicPurchaseTransferEffect가 SettingButton으로 출발하는 간격입니다. 앞 효과가 이동 중이어도 다음 효과가 출발합니다.")]
    [SerializeField, Min(0f)] private float fragmentTransferLaunchInterval = 0.12f;
    [Tooltip("RelicPurchaseTransferEffect가 생성될 때의 크기 배율입니다.")]
    [SerializeField, Min(0.05f)] private float fragmentTransferStartScale = 1f;
    [Tooltip("SettingButton에 도착할 때 RelicPurchaseTransferEffect의 최종 크기 배율입니다.")]
    [SerializeField, Min(0.05f)] private float fragmentTransferEndScale = 0.35f;

    [Header("Fragment Transfer Trail")]
    [SerializeField, Min(0.005f)] private float fragmentTrailSpawnInterval = 0.025f;
    [SerializeField, Min(0.01f)] private float fragmentTrailLifetime = 0.18f;
    [SerializeField, Range(0.05f, 1f)] private float fragmentTrailStartScale = 0.78f;
    [SerializeField, Range(0f, 1f)] private float fragmentTrailEndScale = 0.2f;
    [SerializeField, Range(0f, 1f)] private float fragmentTrailStartAlpha = 0.48f;

    [Header("Tutorial Dialogue Text")]
    [Tooltip("최초 로비 진입 시 엘릭이 말하는 대사입니다. 위에서부터 순서대로 재생됩니다.")]
    [TextArea(2, 4)]
    [SerializeField]
    private string[] introDialogue =
    {
        "드디어 모두 도착하셨군요. 기다리고 있었습니다.",
        "우선, 이것을 받아 주세요.",
        "이미 이 거점과 앵커링을 마쳐 두었습니다.",
        "탐사 중 모두가 쓰러지는 상황이 생기더라도, 앵커가 여러분을 이곳으로 이끌어 줄 겁니다.",
        "그리고 이것들도 함께 받아 주세요.",
        "이 파편들을 적절히 활용하신다면 여러분의 능력을 한층 끌어올릴 수 있을 겁니다.",
        "준비가 끝나면 다시 저에게 말을 걸어 주세요."
    };

    [Tooltip("FragmentGroup을 표시하기 시작할 최초 대사 번호입니다. 0부터 시작합니다.")]
    [Min(0)]
    [SerializeField] private int fragmentShowStartIndex = 4;

    [Header("First Expedition Dialogue Text")]
    [Tooltip("최초 튜토리얼 이후 엘릭에게 다시 말을 걸었을 때 나오는 대사입니다.")]
    [TextArea(2, 4)]
    [SerializeField]
    private string[] firstExpeditionDialogue =
    {
        "준비를 마치셨군요.",
        "첫 탐사지는 로데른 폐허입니다.",
        "그곳에서 연구에 필요한 재료를 확보해 와 주세요."
    };

    [Header("Starter Common Runes")]
    [Tooltip("첫 로비 튜토리얼에서 지급할 공용 룬 ID 3개입니다.")]
    [SerializeField] private string[] starterRuneIds = new string[3];

    private DialogueMode dialogueMode;
    private int dialogueIndex;
    private bool starterRunesGrantedThisDialogue;
    private Coroutine typewriterCoroutine;
    private bool isTyping;
    private int currentDialogueCharacterCount;
    private Vector2 nextButtonIndicatorBasePosition;
    private bool hasNextButtonIndicatorBasePosition;
    private bool cameraPauseActive;
    private Coroutine fragmentTransferCoroutine;

    private sealed class FragmentTransferSnapshot
    {
        public Image SourceImage;
        public Color Color;
        public Vector2 ScreenPosition;
    }

    private sealed class FragmentTransferTrailGhost
    {
        public RectTransform Rect;
        public RawImage Image;
        public Vector3 StartScale;
        public Color StartColor;
        public float Age;
    }

    public bool IsDialogueOpen => dialogueMode != DialogueMode.None;

    private void Awake()
    {
        AutoBindHierarchy();
        BindNextButton();
        SetDialogueVisible(false);
        SetTutorialDisplay(false);
        CacheNextButtonIndicatorPosition();
        SetNextButtonReady(false);
    }

    private IEnumerator Start()
    {
        // DataManager와 로비 런타임 데이터가 준비된 다음 최초 진입 여부를 확인합니다.
        while (DataManager.Instance == null || DataManager.Instance.LobbyRuntimeStore == null)
            yield return null;

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();
        LobbyQuestManager.Instance?.Refresh();

        if (lobby.TutorialProgress == LobbyTutorialProgress.NotStarted)
        {
            // 로비 씬 전환의 열림 연출이 완전히 끝난 뒤 최초 튜토리얼 대화를 시작합니다.
            yield return WaitForLobbySceneTransitionComplete();
            BeginIntroDialogue();
        }
    }

    private IEnumerator WaitForLobbySceneTransitionComplete()
    {
        // 로비 씬이 활성화된 직후에는 열림 전환이 아직 시작되지 않았을 수 있으므로
        // 한 프레임 기다린 뒤 전환의 마지막 구간을 감시합니다.
        yield return null;

        float safeLeadTime = Mathf.Max(0f, tutorialStartBeforeTransitionEnd);

        while (true)
        {
            SceneFlowManager sceneFlow = SceneFlowManager.Instance;
            CanvasMaterialSceneTransition transition = CanvasMaterialSceneTransition.Instance;

            if (transition != null && transition.IsOpening)
            {
                // 전환이 완전히 사라진 뒤가 아니라 마지막 구간에 대화 UI를 미리 준비합니다.
                // 전환 그래픽이 위를 덮고 있으므로 플레이어에게는 자연스럽게 함께 드러납니다.
                if (transition.OpenRemainingTime <= safeLeadTime)
                    break;
            }
            else
            {
                bool sceneFlowLoading = sceneFlow != null && sceneFlow.IsLoading;
                bool transitionPlaying = transition != null && transition.IsPlaying;

                // 전환을 사용하지 않는 경로에서는 씬 로드가 끝나는 즉시 시작합니다.
                if (!sceneFlowLoading && !transitionPlaying)
                    break;
            }

            yield return null;
        }
    }

    private void Update()
    {
        UpdateNextButtonIndicatorMotion();

        // 튜토리얼 대화가 진행 중일 때는 Space 키도 마우스 클릭과 동일하게 처리합니다.
        if (dialogueMode != DialogueMode.None && Input.GetKeyDown(KeyCode.Space))
            AdvanceDialogue();
    }

    private void OnDisable()
    {
        StopTypewriter();
        ReleaseCameraPause();
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(AdvanceDialogue);
        ReleaseCameraPause();

    }

    public void TryInteractWithElric()
    {
        if (IsDialogueOpen)
            return;

        if (DataManager.Instance == null || DataManager.Instance.LobbyRuntimeStore == null)
            return;

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();

        if (lobby.TutorialProgress == LobbyTutorialProgress.NotStarted)
        {
            BeginIntroDialogue();
            return;
        }

        if (lobby.TutorialProgress == LobbyTutorialProgress.WaitingForSetup ||
            lobby.TutorialProgress == LobbyTutorialProgress.FirstExpeditionAssigned)
        {
            BeginFirstExpeditionDialogue();
        }
    }

    private void BeginIntroDialogue()
    {
        dialogueMode = DialogueMode.Intro;
        dialogueIndex = 0;
        starterRunesGrantedThisDialogue = false;
        SetDialogueVisible(true);
        RefreshDialogueStep();
    }

    private void BeginFirstExpeditionDialogue()
    {
        dialogueMode = DialogueMode.FirstExpedition;
        dialogueIndex = 0;
        SetDialogueVisible(true);
        SetTutorialDisplay(false);
        RefreshDialogueStep();
    }

    private void AdvanceDialogue()
    {
        if (dialogueMode == DialogueMode.None)
            return;

        // 인트로와 동일하게 타이핑 중 클릭하면 현재 문장을 즉시 전부 표시합니다.
        // 문장이 모두 표시된 상태에서 다시 클릭해야 다음 대사로 넘어갑니다.
        if (isTyping)
        {
            CompleteTypewriterImmediately();
            return;
        }

        dialogueIndex++;

        int count = dialogueMode == DialogueMode.Intro
            ? GetDialogueCount(introDialogue)
            : GetDialogueCount(firstExpeditionDialogue);

        if (dialogueIndex >= count)
        {
            FinishDialogue();
            return;
        }

        RefreshDialogueStep();
    }

    private void RefreshDialogueStep()
    {
        if (nameText != null)
            nameText.text = speakerName;

        string line = string.Empty;

        if (dialogueMode == DialogueMode.Intro)
        {
            line = GetDialogueLine(introDialogue, dialogueIndex);
            ApplyIntroDisplay(dialogueIndex);
        }
        else if (dialogueMode == DialogueMode.FirstExpedition)
        {
            line = GetDialogueLine(firstExpeditionDialogue, dialogueIndex);
            SetTutorialDisplay(false);
        }

        StartTypewriter(line);
    }


    private static int GetDialogueCount(string[] lines)
    {
        return lines != null ? lines.Length : 0;
    }

    private static string GetDialogueLine(string[] lines, int index)
    {
        if (lines == null || lines.Length == 0)
            return string.Empty;

        return lines[Mathf.Clamp(index, 0, lines.Length - 1)] ?? string.Empty;
    }

    private void StartTypewriter(string line)
    {
        StopTypewriter();
        SetNextButtonReady(false);

        if (dialogueText == null)
        {
            SetNextButtonReady(true);
            return;
        }

        dialogueText.text = line ?? string.Empty;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();
        currentDialogueCharacterCount = dialogueText.textInfo.characterCount;

        typewriterCoroutine = StartCoroutine(TypeDialogueLine());

        // 타이핑 중에도 클릭을 받아 현재 문장을 즉시 완성할 수 있게 합니다.
        if (nextButton != null)
            nextButton.interactable = true;
    }

    private IEnumerator TypeDialogueLine()
    {
        isTyping = true;

        if (currentDialogueCharacterCount <= 0)
        {
            CompleteTypewriter();
            yield break;
        }

        float secondsPerCharacter = 1f / Mathf.Max(1f, charactersPerSecond);
        float accumulatedTime = 0f;
        int visibleCharacters = 0;

        while (visibleCharacters < currentDialogueCharacterCount)
        {
            accumulatedTime += Time.unscaledDeltaTime;

            while (accumulatedTime >= secondsPerCharacter &&
                   visibleCharacters < currentDialogueCharacterCount)
            {
                accumulatedTime -= secondsPerCharacter;
                visibleCharacters++;
                dialogueText.maxVisibleCharacters = visibleCharacters;
            }

            yield return null;
        }

        CompleteTypewriter();
    }

    private void CompleteTypewriter()
    {
        if (dialogueText != null)
            dialogueText.maxVisibleCharacters = currentDialogueCharacterCount;

        isTyping = false;
        typewriterCoroutine = null;
        SetNextButtonReady(true);
    }

    private void CompleteTypewriterImmediately()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (dialogueText != null)
            dialogueText.maxVisibleCharacters = currentDialogueCharacterCount;

        isTyping = false;
        SetNextButtonReady(true);
    }

    private void StopTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        isTyping = false;
    }

    private void ApplyIntroDisplay(int index)
    {
        // 파편이 처음 표시된 뒤에는 마지막 대사가 끝나 패널이 닫힐 때까지 계속 유지합니다.
        bool showFragments = index >= fragmentShowStartIndex;

        SetTutorialDisplay(showFragments);

        if (showFragments)
        {
            ApplyStarterRuneIcons();

            if (!starterRunesGrantedThisDialogue)
            {
                GrantStarterRunes();
                starterRunesGrantedThisDialogue = true;
            }
        }
    }

    private void FinishDialogue()
    {
        StopTypewriter();
        SetNextButtonReady(false);

        DialogueMode finishedMode = dialogueMode;
        List<FragmentTransferSnapshot> fragmentTransferSnapshots = finishedMode == DialogueMode.Intro
            ? CaptureFragmentTransferSnapshots()
            : null;

        dialogueMode = DialogueMode.None;
        dialogueIndex = 0;

        // 마지막 문장까지 원본 Fragment를 유지한 뒤, 패널이 닫힌 다음 순차 이동 연출을 시작합니다.
        SetDialogueVisible(false);

        if (finishedMode == DialogueMode.Intro)
            StartFragmentTransfer(fragmentTransferSnapshots);
        else
            SetTutorialDisplay(false);

        if (DataManager.Instance == null || DataManager.Instance.LobbyRuntimeStore == null)
            return;

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();

        if (finishedMode == DialogueMode.Intro)
        {
            // 대화를 끝내기 전에 Next를 연속 입력해도 지급이 누락되지 않도록 한 번 더 보장합니다.
            GrantStarterRunes();
            lobby.TutorialProgress = LobbyTutorialProgress.WaitingForSetup;
            SaveTutorialProgressImmediately();
        }
        else if (finishedMode == DialogueMode.FirstExpedition &&
                 lobby.TutorialProgress == LobbyTutorialProgress.WaitingForSetup)
        {
            lobby.TutorialProgress = LobbyTutorialProgress.FirstExpeditionAssigned;
            SaveTutorialProgressImmediately();
        }

        LobbyQuestManager.Instance?.Refresh();
    }

    private List<FragmentTransferSnapshot> CaptureFragmentTransferSnapshots()
    {
        var snapshots = new List<FragmentTransferSnapshot>();
        if (fragmentImages == null)
            return snapshots;

        for (int i = 0; i < fragmentImages.Length; i++)
        {
            Image source = fragmentImages[i];
            if (source == null || source.sprite == null)
                continue;

            RectTransform sourceRect = source.rectTransform;
            Camera sourceCamera = ResolveUiCamera(sourceRect);
            snapshots.Add(new FragmentTransferSnapshot
            {
                SourceImage = source,
                Color = source.color,
                ScreenPosition = GetRectScreenCenter(sourceRect, sourceCamera)
            });
        }

        return snapshots;
    }

    private void StartFragmentTransfer(List<FragmentTransferSnapshot> snapshots)
    {
        if (snapshots == null || snapshots.Count == 0)
        {
            SetTutorialDisplay(false);
            return;
        }

        if (fragmentTransferCoroutine != null)
            StopCoroutine(fragmentTransferCoroutine);

        fragmentTransferCoroutine = StartCoroutine(PlayFragmentTransferRoutine(snapshots));
    }

    private IEnumerator PlayFragmentTransferRoutine(List<FragmentTransferSnapshot> snapshots)
    {
        ResolveFragmentTransferTarget();
        Canvas transferCanvas = ResolveFragmentTransferCanvas();

        if (fragmentTransferTarget == null || transferCanvas == null || fragmentTransferEffectTexture == null)
        {
            if (fragmentTransferTarget == null)
                Debug.LogWarning("[LobbyTutorialController] Fragment 이동 효과의 SettingButton 목표를 찾지 못했습니다.", this);

            if (fragmentTransferEffectTexture == null)
                Debug.LogWarning("[LobbyTutorialController] Fragment Transfer Effect Texture가 지정되지 않았습니다.", this);

            SetTutorialDisplay(false);
            fragmentTransferCoroutine = null;
            yield break;
        }

        RectTransform transferParent = ResolveTransferEffectParent(transferCanvas);
        Camera targetCamera = ResolveUiCamera(fragmentTransferTarget);
        Vector2 targetScreenPosition = GetRectScreenCenter(fragmentTransferTarget, targetCamera);

        var preparedEffects = new List<(RawImage EffectImage, FragmentTransferSnapshot Snapshot)>();

        // 먼저 Fragment01 -> 02 -> 03 순서로 촤라락 Effect로 교체합니다.
        // 이 단계에서는 아직 SettingButton으로 출발하지 않습니다.
        for (int i = 0; i < snapshots.Count; i++)
        {
            FragmentTransferSnapshot snapshot = snapshots[i];
            if (snapshot == null || snapshot.SourceImage == null)
                continue;

            RawImage effectImage = CreateFragmentTransferEffect(transferCanvas, transferParent, snapshot);
            if (effectImage == null)
                continue;

            snapshot.SourceImage.gameObject.SetActive(false);
            preparedEffects.Add((effectImage, snapshot));

            if (i < snapshots.Count - 1 && fragmentTransferSwapInterval > 0f)
                yield return new WaitForSecondsRealtime(fragmentTransferSwapInterval);
        }

        // 원본 Fragment는 모두 Effect로 교체되었으므로 TutorialDisplay는 정리합니다.
        // Effect는 별도의 Canvas에 생성되어 있으므로 계속 화면에 남아 이동합니다.
        SetTutorialDisplay(false);

        // Effect01이 이동 중일 때 Effect02, Effect03도 순차적으로 출발하도록 겹쳐 재생합니다.
        var runningTransfers = new List<Coroutine>();
        for (int i = 0; i < preparedEffects.Count; i++)
        {
            RawImage effectImage = preparedEffects[i].EffectImage;
            FragmentTransferSnapshot snapshot = preparedEffects[i].Snapshot;
            if (effectImage == null || snapshot == null)
                continue;

            Coroutine transfer = StartCoroutine(AnimateAndDestroyFragmentTransfer(
                effectImage,
                transferCanvas,
                transferParent,
                snapshot,
                targetScreenPosition));
            runningTransfers.Add(transfer);

            if (i < preparedEffects.Count - 1 && fragmentTransferLaunchInterval > 0f)
                yield return new WaitForSecondsRealtime(fragmentTransferLaunchInterval);
        }

        // 이미 동시에 진행 중인 이동들이 모두 끝날 때까지만 기다립니다.
        for (int i = 0; i < runningTransfers.Count; i++)
        {
            if (runningTransfers[i] != null)
                yield return runningTransfers[i];
        }

        fragmentTransferCoroutine = null;
    }


    private IEnumerator AnimateAndDestroyFragmentTransfer(
        RawImage effectImage,
        Canvas transferCanvas,
        RectTransform transferParent,
        FragmentTransferSnapshot snapshot,
        Vector2 targetScreenPosition)
    {
        if (effectImage == null)
            yield break;

        yield return AnimateSingleFragmentTransfer(
            effectImage.rectTransform,
            transferCanvas,
            transferParent,
            snapshot,
            targetScreenPosition);

        if (effectImage != null)
            Destroy(effectImage.gameObject);
    }

    private RawImage CreateFragmentTransferEffect(
        Canvas transferCanvas,
        RectTransform transferParent,
        FragmentTransferSnapshot snapshot)
    {
        if (transferCanvas == null || snapshot == null || fragmentTransferEffectTexture == null)
            return null;

        GameObject effectObject = new GameObject(
            "RelicPurchaseTransferEffect",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

        RectTransform rect = effectObject.GetComponent<RectTransform>();
        rect.SetParent(transferParent != null ? transferParent : transferCanvas.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = fragmentTransferEffectSize;
        rect.localScale = Vector3.one * fragmentTransferStartScale;
        rect.anchoredPosition = ScreenToUiLocalPosition(
            transferCanvas,
            transferParent,
            snapshot.ScreenPosition);
        rect.SetAsLastSibling();

        RawImage image = effectObject.GetComponent<RawImage>();
        image.texture = fragmentTransferEffectTexture;
        image.color = snapshot.Color;
        image.raycastTarget = false;
        return image;
    }

    private IEnumerator AnimateSingleFragmentTransfer(
        RectTransform effect,
        Canvas transferCanvas,
        RectTransform transferParent,
        FragmentTransferSnapshot snapshot,
        Vector2 targetScreenPosition)
    {
        if (effect == null || snapshot == null)
            yield break;

        Vector2 targetPosition = ScreenToUiLocalPosition(transferCanvas, transferParent, targetScreenPosition);
        Vector2 startPosition = ScreenToUiLocalPosition(transferCanvas, transferParent, snapshot.ScreenPosition);
        Vector2 bouncePosition = startPosition + fragmentTransferBounceOffset;
        Vector3 startScale = effect.localScale;
        Vector3 endScale = Vector3.one * fragmentTransferEndScale;
        float totalDuration = Mathf.Max(0.02f, fragmentTransferBounceDuration + fragmentTransferFlyDuration);
        float bounceRatio = Mathf.Clamp01(fragmentTransferBounceDuration / totalDuration);
        Vector3 bounceScale = Vector3.LerpUnclamped(startScale, endScale, bounceRatio);
        RawImage sourceImage = effect.GetComponent<RawImage>();
        var trailGhosts = new List<FragmentTransferTrailGhost>();
        float trailTimer = 0f;

        float elapsed = 0f;
        float safeBounceDuration = Mathf.Max(0.01f, fragmentTransferBounceDuration);
        while (elapsed < safeBounceDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsed += deltaTime;
            float eased = EaseInCubic(Mathf.Clamp01(elapsed / safeBounceDuration));

            effect.anchoredPosition = Vector2.LerpUnclamped(startPosition, bouncePosition, eased);
            effect.localScale = Vector3.LerpUnclamped(startScale, bounceScale, eased);
            trailTimer += deltaTime;
            SpawnFragmentTrailGhostsIfNeeded(
                effect, sourceImage, transferCanvas, transferParent, trailGhosts, ref trailTimer);
            UpdateFragmentTrailGhosts(trailGhosts, deltaTime);

            yield return null;
        }

        elapsed = 0f;
        float safeFlyDuration = Mathf.Max(0.01f, fragmentTransferFlyDuration);
        while (elapsed < safeFlyDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsed += deltaTime;
            float eased = EaseInQuint(Mathf.Clamp01(elapsed / safeFlyDuration));

            effect.anchoredPosition = Vector2.LerpUnclamped(bouncePosition, targetPosition, eased);
            effect.localScale = Vector3.LerpUnclamped(bounceScale, endScale, eased);
            trailTimer += deltaTime;
            SpawnFragmentTrailGhostsIfNeeded(
                effect, sourceImage, transferCanvas, transferParent, trailGhosts, ref trailTimer);
            UpdateFragmentTrailGhosts(trailGhosts, deltaTime);

            yield return null;
        }

        effect.anchoredPosition = targetPosition;
        effect.localScale = endScale;

        while (trailGhosts.Count > 0)
        {
            UpdateFragmentTrailGhosts(trailGhosts, Time.unscaledDeltaTime);
            yield return null;
        }
    }

    private void SpawnFragmentTrailGhostsIfNeeded(
        RectTransform sourceRect,
        RawImage sourceImage,
        Canvas transferCanvas,
        RectTransform transferParent,
        List<FragmentTransferTrailGhost> trailGhosts,
        ref float trailTimer)
    {
        if (sourceRect == null || sourceImage == null || sourceImage.texture == null ||
            transferCanvas == null || trailGhosts == null)
        {
            return;
        }

        float safeInterval = Mathf.Max(0.005f, fragmentTrailSpawnInterval);
        while (trailTimer >= safeInterval)
        {
            trailTimer -= safeInterval;
            FragmentTransferTrailGhost ghost = CreateFragmentTrailGhost(
                sourceRect, sourceImage, transferCanvas, transferParent);
            if (ghost != null)
                trailGhosts.Add(ghost);
        }
    }

    private FragmentTransferTrailGhost CreateFragmentTrailGhost(
        RectTransform sourceRect,
        RawImage sourceImage,
        Canvas transferCanvas,
        RectTransform transferParent)
    {
        GameObject ghostObject = new GameObject(
            "FragmentTransferTrail",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

        RectTransform ghostRect = ghostObject.GetComponent<RectTransform>();
        ghostRect.SetParent(transferParent != null ? transferParent : transferCanvas.transform, false);
        ghostRect.anchorMin = sourceRect.anchorMin;
        ghostRect.anchorMax = sourceRect.anchorMax;
        ghostRect.pivot = sourceRect.pivot;
        ghostRect.sizeDelta = sourceRect.sizeDelta;
        ghostRect.anchoredPosition = sourceRect.anchoredPosition;
        ghostRect.localRotation = sourceRect.localRotation;
        ghostRect.localScale = sourceRect.localScale * fragmentTrailStartScale;

        int sourceSiblingIndex = sourceRect.GetSiblingIndex();
        ghostRect.SetSiblingIndex(Mathf.Max(0, sourceSiblingIndex));
        sourceRect.SetAsLastSibling();

        RawImage ghostImage = ghostObject.GetComponent<RawImage>();
        ghostImage.texture = sourceImage.texture;
        ghostImage.uvRect = sourceImage.uvRect;
        Color ghostColor = sourceImage.color;
        ghostColor.a *= fragmentTrailStartAlpha;
        ghostImage.color = ghostColor;
        ghostImage.raycastTarget = false;

        return new FragmentTransferTrailGhost
        {
            Rect = ghostRect,
            Image = ghostImage,
            StartScale = ghostRect.localScale,
            StartColor = ghostColor,
            Age = 0f
        };
    }

    private void UpdateFragmentTrailGhosts(List<FragmentTransferTrailGhost> trailGhosts, float deltaTime)
    {
        if (trailGhosts == null)
            return;

        float safeLifetime = Mathf.Max(0.01f, fragmentTrailLifetime);
        for (int i = trailGhosts.Count - 1; i >= 0; i--)
        {
            FragmentTransferTrailGhost ghost = trailGhosts[i];
            if (ghost == null || ghost.Rect == null || ghost.Image == null)
            {
                trailGhosts.RemoveAt(i);
                continue;
            }

            ghost.Age += deltaTime;
            float t = Mathf.Clamp01(ghost.Age / safeLifetime);
            Color color = ghost.StartColor;
            color.a = ghost.StartColor.a * (1f - t);
            ghost.Image.color = color;

            float scaleMultiplier = Mathf.Lerp(fragmentTrailStartScale, fragmentTrailEndScale, t) /
                                    Mathf.Max(0.0001f, fragmentTrailStartScale);
            ghost.Rect.localScale = ghost.StartScale * scaleMultiplier;

            if (t < 1f)
                continue;

            Destroy(ghost.Rect.gameObject);
            trailGhosts.RemoveAt(i);
        }
    }

    private void ResolveFragmentTransferTarget()
    {
        if (fragmentTransferTarget != null)
            return;

        GameObject target = FindSceneObject("SettingButton");
        if (target != null)
            fragmentTransferTarget = target.transform as RectTransform;
    }

    private Canvas ResolveFragmentTransferCanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            return canvas;

        if (fragmentTransferTarget != null)
            return fragmentTransferTarget.GetComponentInParent<Canvas>();

        return null;
    }

    private static RectTransform ResolveTransferEffectParent(Canvas transferCanvas)
    {
        if (transferCanvas == null)
            return null;

        RectTransform contentRoot =
            ResolutionCanvasViewportFitter.ResolveContentRoot(transferCanvas.transform);
        return contentRoot != null ? contentRoot : transferCanvas.transform as RectTransform;
    }

    private static Camera ResolveUiCamera(RectTransform targetRect)
    {
        if (targetRect == null)
            return null;

        Canvas targetCanvas = targetRect.GetComponentInParent<Canvas>();
        if (targetCanvas == null || targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return targetCanvas.worldCamera != null ? targetCanvas.worldCamera : Camera.main;
    }

    private static Vector2 GetRectScreenCenter(RectTransform targetRect, Camera fallbackCamera)
    {
        if (targetRect == null)
            return Vector2.zero;

        Canvas targetCanvas = targetRect.GetComponentInParent<Canvas>();
        Camera uiCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? (targetCanvas.worldCamera != null ? targetCanvas.worldCamera : fallbackCamera)
            : null;

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        return RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);
    }

    private static Vector2 ScreenToUiLocalPosition(
        Canvas canvas,
        RectTransform coordinateRoot,
        Vector2 screenPosition)
    {
        if (canvas == null)
            return Vector2.zero;

        RectTransform targetRect = coordinateRoot != null
            ? coordinateRoot
            : canvas.transform as RectTransform;
        if (targetRect == null)
            return Vector2.zero;

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect, screenPosition, uiCamera, out Vector2 localPoint)
            ? localPoint
            : Vector2.zero;
    }

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    private static float EaseInQuint(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * t * t;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            Transform candidate = all[i];
            if (candidate == null || candidate.name != objectName)
                continue;

            GameObject gameObject = candidate.gameObject;
            if (!gameObject.scene.IsValid())
                continue;

            return gameObject;
        }

        return null;
    }

    private void SaveTutorialProgressImmediately()
    {
        if (SaveSystem.Instance == null)
        {
            Debug.LogWarning(
                "[LobbyTutorialController] SaveSystem.Instance를 찾지 못해 튜토리얼 진행 상태를 즉시 저장하지 못했습니다.",
                this);
            return;
        }

        if (!SaveSystem.Instance.SaveCurrentProgress())
        {
            Debug.LogWarning(
                "[LobbyTutorialController] 튜토리얼 진행 상태 즉시 저장에 실패했습니다.",
                this);
        }
    }

    private void GrantStarterRunes()
    {
        if (DataManager.Instance == null || starterRuneIds == null)
            return;

        for (int i = 0; i < starterRuneIds.Length; i++)
        {
            string runeId = starterRuneIds[i];
            if (string.IsNullOrWhiteSpace(runeId))
                continue;

            RecordDiscoveryService.RegisterRune(DataManager.Instance, runeId.Trim());
        }
    }

    private void ApplyStarterRuneIcons()
    {
        if (fragmentImages == null || starterRuneIds == null)
            return;

        RuneIconDatabase iconDatabase = DataManager.Instance != null
            ? DataManager.Instance.RuneIconDatabase
            : null;

        int count = Mathf.Min(fragmentImages.Length, starterRuneIds.Length);
        for (int i = 0; i < count; i++)
        {
            Image image = fragmentImages[i];
            if (image == null)
                continue;

            string runeId = starterRuneIds[i];
            if (iconDatabase != null &&
                !string.IsNullOrWhiteSpace(runeId) &&
                iconDatabase.TryGetIcon(runeId.Trim(), out Sprite icon))
            {
                image.sprite = icon;
                image.enabled = true;
            }
        }
    }

    private void BindNextButton()
    {
        if (nextButton == null)
            return;

        nextButton.onClick.RemoveListener(AdvanceDialogue);
        nextButton.onClick.AddListener(AdvanceDialogue);
    }

    private void SetDialogueVisible(bool visible)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(visible);

        SetNpcImageVisible(visible);

        if (visible)
        {
            LobbyPositionModalInputBlocker.Block(this);
            AcquireCameraPause();
        }
        else
        {
            LobbyPositionModalInputBlocker.Unblock(this);
            ReleaseCameraPause();
        }
    }

    private void AcquireCameraPause()
    {
        if (cameraPauseActive)
            return;

        CameraMouseParallaxController.BeginUiPanelPause();
        cameraPauseActive = true;
    }

    private void ReleaseCameraPause()
    {
        if (!cameraPauseActive)
            return;

        CameraMouseParallaxController.EndUiPanelPause();
        cameraPauseActive = false;
    }


    private void SetNextButtonReady(bool ready)
    {
        if (nextButton != null)
            nextButton.interactable = ready;

        if (nextButtonIndicator != null)
        {
            if (ready)
            {
                CacheNextButtonIndicatorPosition();
                nextButtonIndicator.anchoredPosition = nextButtonIndicatorBasePosition;
            }

            nextButtonIndicator.gameObject.SetActive(ready);
        }
    }

    private void CacheNextButtonIndicatorPosition()
    {
        if (nextButtonIndicator == null || hasNextButtonIndicatorBasePosition)
            return;

        nextButtonIndicatorBasePosition = nextButtonIndicator.anchoredPosition;
        hasNextButtonIndicatorBasePosition = true;
    }

    private void UpdateNextButtonIndicatorMotion()
    {
        if (nextButtonIndicator == null ||
            !nextButtonIndicator.gameObject.activeInHierarchy ||
            !hasNextButtonIndicatorBasePosition)
        {
            return;
        }

        float offsetY = Mathf.Sin(Time.unscaledTime * indicatorMoveSpeed * Mathf.PI * 2f) *
                        indicatorMoveDistance;
        nextButtonIndicator.anchoredPosition =
            nextButtonIndicatorBasePosition + new Vector2(0f, offsetY);
    }


    /// <summary>
    /// 대화 NPC 이미지를 표시하거나 숨깁니다.
    /// 이후 페이드, 스케일 등의 초상화 연출은 이 메서드를 확장해서 적용할 수 있습니다.
    /// </summary>
    private void SetNpcImageVisible(bool visible)
    {
        if (npcImage != null)
            npcImage.gameObject.SetActive(visible);
    }

    private void SetTutorialDisplay(bool showFragments)
    {
        if (tutorialDisplay != null)
            tutorialDisplay.SetActive(showFragments);

        if (fragmentGroup != null)
            fragmentGroup.SetActive(showFragments);
    }

    private void AutoBindHierarchy()
    {
        if (dialoguePanel == null)
            dialoguePanel = FindChildGameObject(transform, "DialoguePanel");

        if (tutorialDisplay == null)
            tutorialDisplay = FindChildGameObject(transform, "TutorialDisplay");

        if (dialoguePanel != null)
        {
            Transform dialogueRoot = dialoguePanel.transform;

            if (npcImage == null)
                npcImage = FindChildComponent<Image>(dialogueRoot, "NpcImage");

            if (nameText == null)
                nameText = FindChildComponent<TMP_Text>(dialogueRoot, "NameText");

            if (dialogueText == null)
                dialogueText = FindChildComponent<TMP_Text>(dialogueRoot, "DialogueText");

            if (nextButton == null)
                nextButton = FindChildComponent<Button>(dialogueRoot, "NextButton");

            if (nextButtonIndicator == null && nextButton != null)
            {
                GameObject indicatorObject = FindChildGameObject(nextButton.transform, "Image");
                if (indicatorObject != null)
                    nextButtonIndicator = indicatorObject.GetComponent<RectTransform>();
            }
        }

        if (tutorialDisplay != null)
        {
            Transform displayRoot = tutorialDisplay.transform;

            if (fragmentGroup == null)
                fragmentGroup = FindChildGameObject(displayRoot, "FragmentGroup");
        }

        AutoBindFragmentImages();
        ResolveFragmentTransferTarget();
    }

    private void AutoBindFragmentImages()
    {
        if (fragmentGroup == null)
            return;

        if (fragmentImages == null || fragmentImages.Length != 3)
            fragmentImages = new Image[3];

        for (int i = 0; i < fragmentImages.Length; i++)
        {
            if (fragmentImages[i] != null)
                continue;

            string objectName = "Fragment0" + (i + 1);
            fragmentImages[i] = FindChildComponent<Image>(fragmentGroup.transform, objectName);
        }
    }

    private static GameObject FindChildGameObject(Transform root, string objectName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == objectName)
                return child.gameObject;
        }

        return null;
    }

    private static T FindChildComponent<T>(Transform root, string objectName) where T : Component
    {
        GameObject target = FindChildGameObject(root, objectName);
        return target != null ? target.GetComponent<T>() : null;
    }
}
