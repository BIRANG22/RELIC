using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

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
    [SerializeField] private string speakerNameKey = LocalizationKeys.Tutorial.SpeakerElric;

    [Header("Tutorial Input Blocker")]
    [Tooltip("튜토리얼 대화 중 뒤쪽 로비 UI/월드 오브젝트 클릭을 막는 전체 화면 레이캐스트 블로커입니다. 비어 있으면 런타임에 자동 생성합니다.")]
    [SerializeField] private GameObject tutorialInputBlocker;
    [Tooltip("DialoguePanel에 별도 Canvas가 없을 때 자동 생성 블로커가 사용할 Sorting Order입니다.")]
    [SerializeField] private int tutorialInputBlockerFallbackSortingOrder = 9049;

    [Header("Text Typewriter")]
    [Tooltip("1초에 표시할 글자 수입니다.")]
    [Min(1f)]
    [SerializeField] private float charactersPerSecond = 30f;

    [Header("문장 넘김 사운드")]
    [Tooltip("다음 문장으로 넘어갈 때 재생할 SFX입니다. AudioManager의 사운드 DB에서 선택합니다.")]
    [SerializeField, SoundId(SoundCategory.Sfx)]
    private string lineAdvanceSoundId = AudioIds.Sfx.NormalButtonClick;

    [Tooltip("문장 넘김 사운드의 볼륨입니다.")]
    [SerializeField, Range(0f, 1f)]
    private float lineAdvanceSoundVolume = 0.5f;

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

    [Header("Fragment Transfer Sound")]
    [SerializeField, SoundId(SoundCategory.Sfx)] private string fragmentTransferStartSoundId = "";
    [SerializeField, Range(0f, 1f)] private float fragmentTransferStartSoundVolume = 0.5f;

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
    [Tooltip("Initial Lobby dialogue localization keys, played in order.")]
    [TextArea(2, 4)]
    [SerializeField]
    private string[] introDialogue =
    {
        LocalizationKeys.Tutorial.Intro01, LocalizationKeys.Tutorial.Intro02, LocalizationKeys.Tutorial.Intro03,
        LocalizationKeys.Tutorial.Intro04, LocalizationKeys.Tutorial.Intro05
    };

    [Tooltip("FragmentGroup을 표시하기 시작할 최초 대사 번호입니다. 0부터 시작합니다.")]
    [Min(0)]
    [SerializeField] private int fragmentShowStartIndex = 2;

    [Header("First Expedition Dialogue Text")]
    [Tooltip("Post-intro dialogue localization keys, played in order.")]
    [TextArea(2, 4)]
    [SerializeField]
    private string[] firstExpeditionDialogue =
    {
        LocalizationKeys.Tutorial.FirstExpedition01, LocalizationKeys.Tutorial.FirstExpedition02
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

    private Coroutine deferredSpeakerNameRoutine;

    public bool IsDialogueOpen => dialogueMode != DialogueMode.None;

    private void ResetDialogueLocalizationKeys()
    {
        // 씬/프리팹에 예전 7개/3개 배열이 직렬화되어 있어도 현재 튜토리얼 구성(5개/2개)을 사용합니다.
        introDialogue = new[]
        {
            LocalizationKeys.Tutorial.Intro01,
            LocalizationKeys.Tutorial.Intro02,
            LocalizationKeys.Tutorial.Intro03,
            LocalizationKeys.Tutorial.Intro04,
            LocalizationKeys.Tutorial.Intro05
        };

        firstExpeditionDialogue = new[]
        {
            LocalizationKeys.Tutorial.FirstExpedition01,
            LocalizationKeys.Tutorial.FirstExpedition02
        };
    }

    private void Awake()
    {
        ResetDialogueLocalizationKeys();
        AutoBindHierarchy();
        BindNextButton();
        BindDialoguePanelClick();
        SetDialogueVisible(false);
        SetTutorialDisplay(false);
        CacheNextButtonIndicatorPosition();
        SetNextButtonReady(false);
    }

    private void BindDialoguePanelClick()
    {
        if (dialoguePanel == null)
            return;

        // DialoguePanel은 별도 Canvas(9050)를 사용하므로 자체 GraphicRaycaster가 있어야
        // 9049의 TutorialInputBlocker보다 위에서 실제 포인터 입력을 받을 수 있습니다.
        Canvas dialogueCanvas = dialoguePanel.GetComponent<Canvas>();
        if (dialogueCanvas != null)
        {
            dialogueCanvas.overrideSorting = true;
            dialogueCanvas.sortingOrder = 9050;

            if (dialoguePanel.GetComponent<GraphicRaycaster>() == null)
                dialoguePanel.AddComponent<GraphicRaycaster>();
        }

        // 패널의 빈 영역도 클릭 대상으로 만들기 위해 완전 투명 Image를 사용합니다.
        // 기존 자식 UI의 표시에는 영향을 주지 않으며 Raycast만 받습니다.
        Image clickSurface = dialoguePanel.GetComponent<Image>();
        if (clickSurface == null)
        {
            clickSurface = dialoguePanel.AddComponent<Image>();
            clickSurface.color = new Color(0f, 0f, 0f, 0f);
        }

        clickSurface.raycastTarget = true;

        LobbyTutorialDialogueClickRelay relay = dialoguePanel.GetComponent<LobbyTutorialDialogueClickRelay>();
        if (relay == null)
            relay = dialoguePanel.AddComponent<LobbyTutorialDialogueClickRelay>();

        relay.Setup(this);
    }

    internal void HandleDialoguePanelClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        AdvanceDialogue();
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
            // 최초 튜토리얼은 씬 전환이 화면을 가리고 있는 동안 미리 준비합니다.
            // 일반 로비 화면이 먼저 노출된 뒤 패널이 켜지는 깜빡임을 방지합니다.
            BeginIntroDialogue();
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
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

        if (deferredSpeakerNameRoutine != null)
        {
            StopCoroutine(deferredSpeakerNameRoutine);
            deferredSpeakerNameRoutine = null;
        }

        StopTypewriter();
        SetTutorialInputBlockerVisible(false);
        LobbyPositionModalInputBlocker.Unblock(this);
        ReleaseCameraPause();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _)
    {
        if (dialogueMode != DialogueMode.None)
            RefreshDialogueStep();
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(AdvanceDialogue);

        SetTutorialInputBlockerVisible(false);
        LobbyPositionModalInputBlocker.Unblock(this);
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

        // 첫 탐사 출발 대사는 이제 엘릭 월드 오브젝트 클릭으로 시작하지 않습니다.
        // 플레이 버튼으로 Ready_Panel이 실제 열린 뒤 자동으로 시작됩니다.
    }

    public void TryBeginFirstExpeditionDialogueFromReadyPanel()
    {
        if (IsDialogueOpen)
            return;

        if (DataManager.Instance == null || DataManager.Instance.LobbyRuntimeStore == null)
            return;

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();
        if (lobby.TutorialProgress != LobbyTutorialProgress.WaitingForSetup)
            return;

        BeginFirstExpeditionDialogue();
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

        PlayLineAdvanceSound();
        RefreshDialogueStep();
    }

    /// <summary>
    /// 다음 문장으로 넘어갈 때 AudioManager에 등록된 SFX를 재생합니다.
    /// </summary>
    private void PlayLineAdvanceSound()
    {
        if (string.IsNullOrWhiteSpace(lineAdvanceSoundId))
            return;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                $"[{nameof(LobbyTutorialController)}] AudioManager.Instance를 찾지 못했습니다. 문장 넘김 사운드를 재생할 수 없습니다.",
                this);
            return;
        }

        AudioManager.Instance.PlaySfx(lineAdvanceSoundId, Mathf.Clamp01(lineAdvanceSoundVolume));
    }

    private void RefreshDialogueStep()
    {
        ApplySpeakerName();
        ScheduleDeferredSpeakerNameRefresh();

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

        StartTypewriter(GameLocalization.Get(line));
    }


    private void ApplySpeakerName()
    {
        if (nameText != null)
            nameText.text = GameLocalization.Get(speakerNameKey);
    }

    private void ScheduleDeferredSpeakerNameRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (deferredSpeakerNameRoutine != null)
            StopCoroutine(deferredSpeakerNameRoutine);

        deferredSpeakerNameRoutine = StartCoroutine(ApplySpeakerNameNextFrame());
    }

    private IEnumerator ApplySpeakerNameNextFrame()
    {
        // DialoguePanel 최초 활성화 직후 로컬라이즈 컴포넌트가 기본 텍스트를 다시 적용할 수 있으므로
        // 한 프레임 뒤 현재 언어의 NPC 이름을 최종 적용합니다.
        yield return null;
        deferredSpeakerNameRoutine = null;
        ApplySpeakerName();
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
                Color = ResolveStarterRuneRarityColor(i, source.color),
                ScreenPosition = GetRectScreenCenter(sourceRect, sourceCamera)
            });
        }

        return snapshots;
    }

    private Color ResolveStarterRuneRarityColor(int index, Color fallbackColor)
    {
        if (starterRuneIds == null || index < 0 || index >= starterRuneIds.Length)
            return fallbackColor;

        string runeId = starterRuneIds[index];
        if (string.IsNullOrWhiteSpace(runeId) || DataManager.Instance?.RuneDatabase == null)
            return fallbackColor;

        if (!DataManager.Instance.RuneDatabase.TryGet(runeId.Trim(), out RuneData runeData) ||
            runeData == null || string.IsNullOrWhiteSpace(runeData.Rarity))
        {
            return fallbackColor;
        }

        Color rarityColor;
        if (!RecordPanelUI.TryGetCachedRarityDisplayColor(runeData.Rarity, out rarityColor))
        {
            RecordPanelUI[] panels = FindObjectsByType<RecordPanelUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            rarityColor = panels.Length > 0
                ? panels[0].GetRarityDisplayColor(runeData.Rarity)
                : fallbackColor;
        }

        rarityColor.a = fallbackColor.a;
        return rarityColor;
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

            PlayFragmentTransferStartSound();

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


    private void PlayFragmentTransferStartSound()
    {
        if (string.IsNullOrWhiteSpace(fragmentTransferStartSoundId) || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(
            fragmentTransferStartSoundId,
            Mathf.Clamp01(fragmentTransferStartSoundVolume));
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
        // DialoguePanel보다 바로 아래 Sorting Order의 투명 레이캐스트 블로커를 먼저 켜서
        // 튜토리얼 중 뒤쪽 로비 버튼과 월드 오브젝트가 클릭되지 않도록 합니다.
        SetTutorialInputBlockerVisible(visible);

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(visible);

            // Ready_Panel / Info_Panel이 열린 직후 첫 출발 대사가 시작될 때
            // 대화 패널이 준비 UI 뒤에 가려지지 않도록 같은 부모의 최상단으로 올립니다.
            if (visible)
                dialoguePanel.transform.SetAsLastSibling();
        }

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

    private void SetTutorialInputBlockerVisible(bool visible)
    {
        if (visible)
            EnsureTutorialInputBlocker();

        if (tutorialInputBlocker == null)
            return;

        tutorialInputBlocker.SetActive(visible);

        if (!visible)
            return;

        tutorialInputBlocker.transform.SetAsLastSibling();

        // 블로커를 앞으로 올린 뒤 DialoguePanel을 한 번 더 마지막 형제로 보내
        // Next 버튼을 포함한 대화 UI가 블로커 위에서 입력을 받도록 보장합니다.
        if (dialoguePanel != null)
            dialoguePanel.transform.SetAsLastSibling();
    }

    private void EnsureTutorialInputBlocker()
    {
        if (tutorialInputBlocker != null)
        {
            ConfigureTutorialInputBlocker(tutorialInputBlocker);
            return;
        }

        Transform parent = dialoguePanel != null && dialoguePanel.transform.parent != null
            ? dialoguePanel.transform.parent
            : transform;

        tutorialInputBlocker = new GameObject(
            "TutorialInputBlocker",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(GraphicRaycaster),
            typeof(Image));

        RectTransform rect = tutorialInputBlocker.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        ConfigureTutorialInputBlocker(tutorialInputBlocker);
    }

    private void ConfigureTutorialInputBlocker(GameObject blocker)
    {
        if (blocker == null)
            return;

        Image blockerImage = blocker.GetComponent<Image>();
        if (blockerImage == null)
            blockerImage = blocker.AddComponent<Image>();

        // 완전히 투명하지만 Raycast Target은 유지합니다.
        blockerImage.color = new Color(0f, 0f, 0f, 0f);
        blockerImage.raycastTarget = true;

        Canvas blockerCanvas = blocker.GetComponent<Canvas>();
        if (blockerCanvas == null)
            blockerCanvas = blocker.AddComponent<Canvas>();

        blockerCanvas.overrideSorting = true;

        int blockerSortingOrder = tutorialInputBlockerFallbackSortingOrder;
        if (dialoguePanel != null)
        {
            Canvas dialogueCanvas = dialoguePanel.GetComponent<Canvas>();
            if (dialogueCanvas != null)
                blockerSortingOrder = dialogueCanvas.sortingOrder - 1;
        }

        blockerCanvas.sortingOrder = blockerSortingOrder;

        if (blocker.GetComponent<GraphicRaycaster>() == null)
            blocker.AddComponent<GraphicRaycaster>();
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

/// <summary>
/// DialoguePanel의 빈 영역/텍스트 영역 클릭을 LobbyTutorialController의 다음 대사 입력으로 전달합니다.
/// NextButton처럼 자체 클릭 핸들러가 있는 자식은 기존 Button 처리를 그대로 사용합니다.
/// </summary>
public sealed class LobbyTutorialDialogueClickRelay : MonoBehaviour, IPointerClickHandler
{
    private LobbyTutorialController owner;

    public void Setup(LobbyTutorialController controller)
    {
        owner = controller;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        owner?.HandleDialoguePanelClick(eventData);
    }
}

