using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyTutorialController : MonoBehaviour
{
    private const string AnchorVfxRootObjectName = "Vfx_root_anchor";
    private const string AnchorVfxProxyObjectName = "AnchorVfxProxy";
    private const string AnchorVfxRendererRootName = "__LobbyTutorialAnchorVfxRenderer";
    private const int AnchorVfxRenderLayer = 9;

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
    [SerializeField] private GameObject anchorImage;
    [SerializeField] private GameObject fragmentGroup;
    [SerializeField] private Image[] fragmentImages = new Image[3];

    [Header("Anchor VFX RenderTexture")]
    [SerializeField] private GameObject anchorVfxRoot;

    [Tooltip("RenderTexture width for the tutorial anchor VFX.")]
    [SerializeField, Min(1)] private int anchorVfxRenderTextureWidth = 512;

    [Tooltip("RenderTexture height for the tutorial anchor VFX.")]
    [SerializeField, Min(1)] private int anchorVfxRenderTextureHeight = 512;

    [Tooltip("Orthographic size of the private tutorial anchor VFX camera.")]
    [SerializeField, Min(0.01f)] private float anchorVfxRenderCameraOrthographicSize = 3f;

    [Tooltip("UI size of the tutorial anchor VFX RawImage proxy.")]
    [SerializeField] private Vector2 anchorVfxProxySize = new(400f, 400f);

    [Tooltip("Anchored position offset of the tutorial anchor VFX RawImage proxy.")]
    [SerializeField] private Vector2 anchorVfxProxyAnchoredPosition = Vector2.zero;

    [Tooltip("Local position of the cloned VFX inside the private render space.")]
    [SerializeField] private Vector3 anchorVfxRenderVfxLocalPosition = Vector3.zero;

    [Tooltip("Optional RawImage material template. Empty uses the scene VFXImage material when present.")]
    [SerializeField] private Material anchorVfxProxyMaterialTemplate;

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

    [Tooltip("AnchorImage를 표시하기 시작할 최초 대사 번호입니다. 0부터 시작합니다.")]
    [Min(0)]
    [SerializeField] private int anchorShowStartIndex = 1;

    [Tooltip("AnchorImage를 마지막으로 표시할 대사 번호입니다. 0부터 시작합니다.")]
    [Min(0)]
    [SerializeField] private int anchorShowEndIndex = 3;

    [Tooltip("FragmentGroup을 표시하기 시작할 최초 대사 번호입니다. 0부터 시작합니다.")]
    [Min(0)]
    [SerializeField] private int fragmentShowStartIndex = 4;

    [Tooltip("FragmentGroup을 마지막으로 표시할 대사 번호입니다. 0부터 시작합니다.")]
    [Min(0)]
    [SerializeField] private int fragmentShowEndIndex = 5;

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
    private LobbyUiVfxRenderTextureProxy anchorVfxProxy;

    public bool IsDialogueOpen => dialogueMode != DialogueMode.None;

    private void Awake()
    {
        AutoBindHierarchy();
        BindNextButton();
        SetDialogueVisible(false);
        SetTutorialDisplay(false, false);
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
    }

    private void OnDisable()
    {
        StopTypewriter();
        HideAnchorVfxProxy();
        ReleaseCameraPause();
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(AdvanceDialogue);

        HideAnchorVfxProxy();
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
        SetTutorialDisplay(false, false);
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
            SetTutorialDisplay(false, false);
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

    private static bool IsIndexInRange(int index, int startIndex, int endIndex)
    {
        int min = Mathf.Min(startIndex, endIndex);
        int max = Mathf.Max(startIndex, endIndex);
        return index >= min && index <= max;
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
        bool showAnchor = IsIndexInRange(index, anchorShowStartIndex, anchorShowEndIndex);
        bool showFragments = IsIndexInRange(index, fragmentShowStartIndex, fragmentShowEndIndex);

        SetTutorialDisplay(showAnchor, showFragments);

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
        dialogueMode = DialogueMode.None;
        dialogueIndex = 0;

        SetDialogueVisible(false);
        SetTutorialDisplay(false, false);

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

    private void SetTutorialDisplay(bool showAnchor, bool showFragments)
    {
        bool showRoot = showAnchor || showFragments;

        if (tutorialDisplay != null)
            tutorialDisplay.SetActive(showRoot);

        if (showAnchor)
        {
            if (anchorImage != null)
                anchorImage.SetActive(true);

            ShowAnchorVfxProxy();
        }
        else
        {
            HideAnchorVfxProxy();

            if (anchorImage != null)
                anchorImage.SetActive(false);
        }

        if (fragmentGroup != null)
            fragmentGroup.SetActive(showFragments);
    }

    private void ShowAnchorVfxProxy()
    {
        LobbyUiVfxRenderTextureProxy proxy = EnsureAnchorVfxProxy();
        if (proxy != null)
            proxy.Show();
    }

    private void HideAnchorVfxProxy()
    {
        if (anchorVfxProxy == null && anchorImage != null)
            anchorVfxProxy = anchorImage.GetComponent<LobbyUiVfxRenderTextureProxy>();

        if (anchorVfxProxy != null)
            anchorVfxProxy.Hide();

        if (anchorVfxRoot != null)
            anchorVfxRoot.SetActive(false);
    }

    private LobbyUiVfxRenderTextureProxy EnsureAnchorVfxProxy()
    {
        if (anchorImage == null)
            return null;

        if (anchorVfxRoot == null)
            anchorVfxRoot = FindChildGameObject(anchorImage.transform, AnchorVfxRootObjectName);

        if (anchorVfxRoot == null)
            return null;

        if (anchorVfxProxy == null)
            anchorVfxProxy = anchorImage.GetComponent<LobbyUiVfxRenderTextureProxy>();

        if (anchorVfxProxy == null)
            anchorVfxProxy = anchorImage.AddComponent<LobbyUiVfxRenderTextureProxy>();

        anchorVfxProxy.Configure(
            anchorVfxRoot,
            AnchorVfxProxyObjectName,
            AnchorVfxRendererRootName,
            anchorVfxRenderTextureWidth,
            anchorVfxRenderTextureHeight,
            anchorVfxRenderCameraOrthographicSize,
            anchorVfxProxySize,
            anchorVfxProxyAnchoredPosition,
            anchorVfxRenderVfxLocalPosition,
            anchorVfxProxyMaterialTemplate,
            AnchorVfxRenderLayer);
        return anchorVfxProxy;
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

            if (anchorImage == null)
                anchorImage = FindChildGameObject(displayRoot, "AnchorImage");

            if (anchorVfxRoot == null && anchorImage != null)
                anchorVfxRoot = FindChildGameObject(anchorImage.transform, AnchorVfxRootObjectName);

            if (fragmentGroup == null)
                fragmentGroup = FindChildGameObject(displayRoot, "FragmentGroup");
        }

        AutoBindFragmentImages();
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
