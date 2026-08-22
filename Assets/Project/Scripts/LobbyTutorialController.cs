using System.Collections;
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

    [Header("Starter Common Runes")]
    [Tooltip("첫 로비 튜토리얼에서 지급할 공용 룬 ID 3개입니다.")]
    [SerializeField] private string[] starterRuneIds = new string[3];

    private static readonly string[] IntroDialogue =
    {
        "드디어 모두 도착하셨군요. 기다리고 있었습니다.",
        "우선, 이것을 받아 주세요.",
        "이미 이 거점과 앵커링을 마쳐 두었습니다.",
        "탐사 중 모두가 쓰러지는 상황이 생기더라도, 앵커가 여러분을 이곳으로 이끌어 줄 겁니다.",
        "그리고 이것들도 함께 받아 주세요.",
        "이 파편들을 적절히 활용하신다면 여러분의 능력을 한층 끌어올릴 수 있을 겁니다.",
        "준비가 끝나면 다시 저에게 말을 걸어 주세요."
    };

    private static readonly string[] FirstExpeditionDialogue =
    {
        "준비를 마치셨군요.",
        "첫 탐사지는 로데른 폐허입니다.",
        "그곳에서 연구에 필요한 재료를 확보해 와 주세요."
    };

    private DialogueMode dialogueMode;
    private int dialogueIndex;
    private bool starterRunesGrantedThisDialogue;
    private Coroutine typewriterCoroutine;
    private bool isTyping;
    private int currentDialogueCharacterCount;
    private Vector2 nextButtonIndicatorBasePosition;
    private bool hasNextButtonIndicatorBasePosition;
    private bool cameraPauseActive;

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
        if (lobby.TutorialProgress == LobbyTutorialProgress.NotStarted)
            BeginIntroDialogue();
    }

    private void Update()
    {
        UpdateNextButtonIndicatorMotion();
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
        SetTutorialDisplay(false, false);
        RefreshDialogueStep();
    }

    private void AdvanceDialogue()
    {
        if (dialogueMode == DialogueMode.None || isTyping)
            return;

        dialogueIndex++;

        int count = dialogueMode == DialogueMode.Intro
            ? IntroDialogue.Length
            : FirstExpeditionDialogue.Length;

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
            line = IntroDialogue[Mathf.Clamp(dialogueIndex, 0, IntroDialogue.Length - 1)];
            ApplyIntroDisplay(dialogueIndex);
        }
        else if (dialogueMode == DialogueMode.FirstExpedition)
        {
            line = FirstExpeditionDialogue[Mathf.Clamp(dialogueIndex, 0, FirstExpeditionDialogue.Length - 1)];
            SetTutorialDisplay(false, false);
        }

        StartTypewriter(line);
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
        bool showAnchor = index >= 1 && index <= 3;
        bool showFragments = index >= 4 && index <= 5;

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

        }
        else if (finishedMode == DialogueMode.FirstExpedition &&
                 lobby.TutorialProgress == LobbyTutorialProgress.WaitingForSetup)
        {
            lobby.TutorialProgress = LobbyTutorialProgress.FirstExpeditionAssigned;
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

        if (anchorImage != null)
            anchorImage.SetActive(showAnchor);

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

            if (anchorImage == null)
                anchorImage = FindChildGameObject(displayRoot, "AnchorImage");

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
