using Relic.Gameplay.Data;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class Setting : MonoBehaviour
{
    [Header("Character Info")]
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text characterInfoText;

    [Header("Character Info Panel")]
    [SerializeField] private CharacterInfoPanel characterInfoPanel;

    [Header("Setting Panel Scripts")]
    [SerializeField] private RuneSettingPanel runeSettingPanelScript;
    [SerializeField] private SkillSettingPanel skillSettingPanelScript;
    [SerializeField] private CharPick charPick;

    [Header("Character Level UI")]
    [SerializeField] private TMP_Text characterLevelText;
    [SerializeField] private TMP_Text characterExpText;

    [Header("Test Level Settings")]
    [SerializeField] private int testExpPerLevel = 1000;
    [SerializeField] private int maxTestLevel = 30;
    [SerializeField] private Button testLevelUpButton;
    [SerializeField] private Button testLevelDownButton;
    [SerializeField] private float testLevelHoldStartDelay = 0.35f;
    [SerializeField] private float testLevelHoldRepeatInterval = 0.08f;
    [SerializeField] private string maxLevelWarningMessage = "최대 레벨입니다.";

    [Header("Test Level Cheat Keys")]
    [SerializeField] private bool enableTestLevelCheatKeys = true;
    [SerializeField] private Key levelDownCheatKey = Key.O;
    [SerializeField] private Key levelUpCheatKey = Key.P;

    [Header("Preset UI")]
    [SerializeField] private Button[] presetButtons = new Button[4];
    [SerializeField] private bool enablePresetButtons = false;
    [SerializeField] private Color presetNormalColor = Color.white;
    [SerializeField] private Color presetSelectedColor = new Color(1f, 0.78f, 0.25f, 1f);

    [Header("Setting Area Tabs")]
    [SerializeField] private GameObject skillArea;
    [SerializeField] private GameObject runeArea;
    [SerializeField, HideInInspector] private Button previewButton;
    [SerializeField, HideInInspector] private Button skillButton;
    [SerializeField, HideInInspector] private Button runeButton;
    [SerializeField, HideInInspector] private Color tabNormalColor = Color.white;
    [SerializeField, HideInInspector] private Color tabSelectedColor = new Color(1f, 0.78f, 0.25f, 1f);

    [Header("Setting Area Slide Effect")]
    [Tooltip("스킬 영역에서 함께 이동할 BackGround입니다. 비어 있으면 자동으로 찾습니다.")]
    [SerializeField, HideInInspector] private RectTransform skillAreaBackGround;
    [Tooltip("스킬 영역에서 함께 이동할 SkillSettingPanel입니다. 비어 있으면 자동으로 찾습니다.")]
    [SerializeField, HideInInspector] private RectTransform skillSettingPanelRect;
    [Tooltip("룬 영역에서 함께 이동할 BackGround입니다. 비어 있으면 자동으로 찾습니다.")]
    [SerializeField, HideInInspector] private RectTransform runeAreaBackGround;
    [Tooltip("룬 영역에서 함께 이동할 RuneSettingPanel입니다. 비어 있으면 자동으로 찾습니다.")]
    [SerializeField, HideInInspector] private RectTransform runeSettingPanelRect;
    [Tooltip("영역이 목표 위치까지 이동하는 시간입니다.")]
    [SerializeField, HideInInspector] private float areaMoveDuration = 0.25f;

    [Header("Setting Area Tab Sound Effect")]
    [Range(0f, 1f)]
    [SerializeField, HideInInspector] private float tabTransitionSfxVolume = 1f;

    [Header("Setting Area Tab Scale Effect")]
    [SerializeField, HideInInspector] private float tabHoverScale = 1.08f;
    [SerializeField, HideInInspector] private float tabBreathMaxScale = 1.12f;
    [SerializeField, HideInInspector] private float tabSelectedScale = 1.2f;
    [SerializeField, HideInInspector] private float tabScaleInDuration = 0.12f;
    [SerializeField, HideInInspector] private float tabScaleOutDuration = 0.10f;
    [SerializeField, HideInInspector] private float tabBreathSpeed = 3.5f;

    [Header("Shared Info Area")]
    [SerializeField, HideInInspector] private RectTransform infoArea;

    [Header("Character Preview Canvas")]
    [Tooltip("CharacterSettingPanel과 함께 켜고 끌 CharacterPreviewCanvas입니다. 비어 있으면 이름으로 자동 탐색합니다.")]
    [SerializeField] private Canvas characterPreviewCanvas;

    [Header("Character Setting Fade")]
    [Tooltip("CharacterSettingPanel이 부드럽게 나타나고 사라지는 시간입니다.")]
    [SerializeField] private float characterSettingFadeDuration = 0.18f;

    [Header("Warning UI")]
    [SerializeField] private SettingWarningUI warningUI;

    private string currentCharacterId;
    private CharacterMasterData currentMasterData;
    private CharacterRuntimeData currentRuntimeData;

    private enum SettingTab
    {
        Preview,
        Skill,
        Rune
    }

    private int currentPartyIndex = -1;
    private int pendingPartyIndex = -1;
    private Coroutine pendingPartyOpenCoroutine;
    private SettingTab currentTab = SettingTab.Preview;

    private CharacterSettingTabButtonScaleEffect previewButtonScaleEffect;
    private CharacterSettingTabButtonScaleEffect skillButtonScaleEffect;
    private CharacterSettingTabButtonScaleEffect runeButtonScaleEffect;
    private Coroutine areaMoveCoroutine;
    private CanvasGroup characterSettingCanvasGroup;
    private Coroutine characterSettingFadeCoroutine;
    private bool characterSettingClosing;

    private bool characterPreviewCanvasStateCached;
    private bool characterPreviewOriginalOverrideSorting;
    private int characterPreviewOriginalSortingOrder;
    private int characterPreviewOriginalSortingLayerId;
    private RenderMode characterPreviewOriginalRenderMode;
    private Camera characterPreviewOriginalWorldCamera;
    private float characterPreviewOriginalPlaneDistance;
    private int characterPreviewOriginalTargetDisplay;

    private void Awake()
    {
        BindCharacterInfoTextIfNeeded();
        BindInfoAreaIfNeeded();
        BindSkillSettingPanelIfNeeded();

        // 탭 전환 중에도 오브젝트가 꺼지지 않도록 두 영역은 항상 활성화한다.
        if (skillArea != null)
            skillArea.SetActive(true);

        if (runeArea != null)
            runeArea.SetActive(true);

        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        if (charPick == null)
            charPick = FindFirstObjectByType<CharPick>(FindObjectsInactive.Include);

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.OnRuneChanged += RefreshCharacterInfo;

        if (skillSettingPanelScript != null)
            skillSettingPanelScript.SetSettingController(this);

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.SetSettingController(this);

        InitPresetButtons();
        InitTestLevelHoldButtons();
    }

    /// <summary>
    /// A/D 입력이 탭 버튼 사이의 Unity UI 자동 네비게이션으로 처리되지 않도록 한다.
    /// 탭 전환은 Tab 키와 직접 클릭으로만 처리한다.
    /// </summary>
    private void DisableTabButtonNavigation()
    {
        SetButtonNavigationNone(previewButton);
        SetButtonNavigationNone(skillButton);
        SetButtonNavigationNone(runeButton);
    }

    private static void SetButtonNavigationNone(Button button)
    {
        if (button == null)
            return;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        // 현재 탭 색상은 Setting에서 직접 관리한다.
        // EventSystem 선택 상태가 CharacterSelect로 이동해도 탭 색상이 깜빡이지 않도록
        // Button의 Color Tint 전환이 Target Graphic 색상을 덮어쓰지 않게 한다.
        button.transition = Selectable.Transition.None;
    }

    private void OnEnable()
    {
        BindSkillSettingPanelIfNeeded();
        PrepareCharacterSettingFade();

        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        // CharacterSettingPanel도 다른 PositionPanel 모달과 동일하게
        // 자신이 열려 있는 동안 로비 월드 오브젝트 입력을 차단합니다.
        LobbyPositionModalInputBlocker.Block(this);

        // Ready/Info 화면에서 바로 넘어오는 경우에도 BackgroundPanel을 끄지 않습니다.
        // 먼저 CharacterSettingPanel이 공용 BackgroundPanel의 소유권을 가져온 뒤
        // Ready 전용 표시 상태를 복구하고 Ready/Info만 퇴장시킵니다.
        LobbyPositionSharedModalBackground.ShowForPanel(
            gameObject,
            this,
            CloseFromSharedBackground);
        LobbyPositionSharedModalBackground.RestoreAfterReadyPanel();
        LobbyEquipPanelUI.TryCloseOpenReadyPanel();

        // CharacterPreviewCanvas는 CharacterSettingPanel과 별도 오브젝트이므로
        // BackgroundPanel 바로 위, CharacterSettingPanel 바로 아래에 배치합니다.
        ShowCharacterPreviewCanvas();
        PlayCharacterSettingFadeIn();

        if (pendingPartyIndex < 0)
            return;

        if (pendingPartyOpenCoroutine != null)
            StopCoroutine(pendingPartyOpenCoroutine);

        pendingPartyOpenCoroutine = StartCoroutine(OpenPendingPartySettingRoutine());
    }

    private IEnumerator OpenPendingPartySettingRoutine()
    {
        // 패널이 활성화된 첫 프레임의 Start/초기화가 끝난 뒤 선택 캐릭터를 적용합니다.
        yield return null;

        int partyIndex = pendingPartyIndex;
        pendingPartyIndex = -1;
        pendingPartyOpenCoroutine = null;

        if (partyIndex >= 0)
            OpenPartySetting(partyIndex);
    }

    private void Start()
    {
        ShowAllSettingAreasStatic();
    }

    private void Update()
    {
        HandleTestLevelCheatKeys();
    }

    /// <summary>
    /// 외부 키보드 입력 컨트롤러에서 호출한다.
    /// 프리뷰 → 룬 → 스킬 → 프리뷰 순서로 한 단계만 전환하며,
    /// 버튼 클릭과 같은 등장/퇴장 효과음을 재생한다.
    /// </summary>
    public void CycleTabByKeyboard()
    {
        // CharacterSettingPanel의 탭 전환 버튼은 제거되었습니다.
        // 스킬/파편 영역은 한 화면에서 항상 고정 표시합니다.
        ShowAllSettingAreasStatic();
    }

    private void OnDisable()
    {
        if (characterSettingFadeCoroutine != null)
        {
            StopCoroutine(characterSettingFadeCoroutine);
            characterSettingFadeCoroutine = null;
        }

        characterSettingClosing = false;
        ResetCharacterSettingFadeState();

        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LobbyPositionSharedModalBackground.HideForOwner(this);
        LobbyPositionModalInputBlocker.Unblock(this);

        // CharacterSettingPanel이 닫히면 별도 Canvas에 생성된 캐릭터 이미지도
        // 즉시 보이지 않도록 CharacterPreviewCanvas 전체를 함께 끕니다.
        HideCharacterPreviewCanvas();

        if (areaMoveCoroutine != null)
        {
            StopCoroutine(areaMoveCoroutine);
            areaMoveCoroutine = null;
        }

        ResetTabButtonScaleEffects();
    }

    private void CloseFromSharedBackground()
    {
        // BackgroundPanel과 시각적으로 자연스럽게 이어지도록 CharacterSettingPanel도
        // 페이드 아웃을 끝낸 뒤 비활성화합니다.
        if (characterSettingClosing)
            return;

        characterSettingClosing = true;

        if (characterSettingFadeCoroutine != null)
            StopCoroutine(characterSettingFadeCoroutine);

        characterSettingFadeCoroutine = StartCoroutine(FadeOutAndCloseCharacterSetting());
    }

    private void PrepareCharacterSettingFade()
    {
        if (characterSettingCanvasGroup == null)
        {
            characterSettingCanvasGroup = GetComponent<CanvasGroup>();
            if (characterSettingCanvasGroup == null)
                characterSettingCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (characterSettingFadeCoroutine != null)
        {
            StopCoroutine(characterSettingFadeCoroutine);
            characterSettingFadeCoroutine = null;
        }

        characterSettingClosing = false;
        characterSettingCanvasGroup.alpha = 0f;
        characterSettingCanvasGroup.interactable = false;
        characterSettingCanvasGroup.blocksRaycasts = false;
    }

    private void PlayCharacterSettingFadeIn()
    {
        if (characterSettingCanvasGroup == null)
            PrepareCharacterSettingFade();

        characterSettingFadeCoroutine = StartCoroutine(FadeCharacterSettingCanvasGroup(1f, true));
    }

    private IEnumerator FadeOutAndCloseCharacterSetting()
    {
        if (characterSettingCanvasGroup == null)
            PrepareCharacterSettingFade();

        characterSettingCanvasGroup.interactable = false;
        characterSettingCanvasGroup.blocksRaycasts = false;

        yield return FadeCharacterSettingCanvasGroup(0f, false);
        characterSettingFadeCoroutine = null;

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private IEnumerator FadeCharacterSettingCanvasGroup(float targetAlpha, bool enableInputOnComplete)
    {
        if (characterSettingCanvasGroup == null)
            yield break;

        float startAlpha = characterSettingCanvasGroup.alpha;
        float duration = Mathf.Max(0f, characterSettingFadeDuration);

        if (duration <= 0f)
        {
            characterSettingCanvasGroup.alpha = targetAlpha;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                characterSettingCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            characterSettingCanvasGroup.alpha = targetAlpha;
        }

        if (enableInputOnComplete && !characterSettingClosing)
        {
            characterSettingCanvasGroup.interactable = true;
            characterSettingCanvasGroup.blocksRaycasts = true;
            characterSettingFadeCoroutine = null;
        }
    }

    private void ResetCharacterSettingFadeState()
    {
        if (characterSettingCanvasGroup == null)
            return;

        characterSettingCanvasGroup.alpha = 1f;
        characterSettingCanvasGroup.interactable = true;
        characterSettingCanvasGroup.blocksRaycasts = true;
    }

    private void ShowCharacterPreviewCanvas()
    {
        ResolveCharacterPreviewCanvas();
        if (characterPreviewCanvas == null)
            return;

        if (!characterPreviewCanvasStateCached)
        {
            characterPreviewOriginalOverrideSorting = characterPreviewCanvas.overrideSorting;
            characterPreviewOriginalSortingOrder = characterPreviewCanvas.sortingOrder;
            characterPreviewOriginalSortingLayerId = characterPreviewCanvas.sortingLayerID;
            characterPreviewOriginalRenderMode = characterPreviewCanvas.renderMode;
            characterPreviewOriginalWorldCamera = characterPreviewCanvas.worldCamera;
            characterPreviewOriginalPlaneDistance = characterPreviewCanvas.planeDistance;
            characterPreviewOriginalTargetDisplay = characterPreviewCanvas.targetDisplay;
            characterPreviewCanvasStateCached = true;
        }

        Canvas settingCanvas = GetComponent<Canvas>();
        if (settingCanvas != null)
        {
            Canvas referenceCanvas = settingCanvas.rootCanvas != null
                ? settingCanvas.rootCanvas
                : settingCanvas;

            // CharacterSettingPanel이 속한 메인 UI Canvas와 같은 Render Mode를 사용해야
            // Sorting Order가 실제로 같은 렌더링 계층에서 비교됩니다.
            characterPreviewCanvas.renderMode = referenceCanvas.renderMode;
            characterPreviewCanvas.targetDisplay = referenceCanvas.targetDisplay;

            if (referenceCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                characterPreviewCanvas.worldCamera = referenceCanvas.worldCamera;
                characterPreviewCanvas.planeDistance = referenceCanvas.planeDistance;
            }
            else if (referenceCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                characterPreviewCanvas.worldCamera = null;
            }

            Canvas backgroundCanvas = ResolveSharedBackgroundCanvas();
            if (backgroundCanvas != null)
            {
                int previewOrder = backgroundCanvas.sortingOrder + 1;

                // CharacterPreviewCanvas만 블러 배경 위로 올립니다.
                // CharacterSettingPanel의 Canvas 정렬값은 절대 변경하지 않습니다.
                // CharacterSettingPanel을 별도 overrideSorting Canvas로 올리면
                // BackgroundPanel 자식인 Lobby_Icon보다 입력 우선순위가 높아져
                // Lobby_Icon_01~05가 보이면서도 클릭되지 않는 문제가 발생합니다.
                characterPreviewCanvas.sortingLayerID = backgroundCanvas.sortingLayerID;
                characterPreviewCanvas.overrideSorting = true;
                characterPreviewCanvas.sortingOrder = previewOrder;
            }
            else
            {
                // 공용 블러 Canvas를 찾지 못해도 CharacterSettingPanel 자체의 정렬값은 건드리지 않습니다.
                // Preview만 현재 UI Canvas보다 한 단계 아래에서 표시합니다.
                characterPreviewCanvas.sortingLayerID = settingCanvas.sortingLayerID;
                characterPreviewCanvas.overrideSorting = true;
                characterPreviewCanvas.sortingOrder = settingCanvas.sortingOrder - 1;
            }
        }

        if (!characterPreviewCanvas.gameObject.activeSelf)
            characterPreviewCanvas.gameObject.SetActive(true);

        // Canvas를 다시 켠 직후 현재 선택 캐릭터 표시 상태를 즉시 갱신합니다.
        LobbyCharacterPreviewController previewController = characterPreviewCanvas.GetComponent<LobbyCharacterPreviewController>();
        if (previewController == null)
            previewController = characterPreviewCanvas.GetComponentInChildren<LobbyCharacterPreviewController>(true);

        previewController?.Refresh();
    }

    private void HideCharacterPreviewCanvas()
    {
        ResolveCharacterPreviewCanvas();
        if (characterPreviewCanvas == null)
            return;

        if (characterPreviewCanvasStateCached)
        {
            characterPreviewCanvas.overrideSorting = characterPreviewOriginalOverrideSorting;
            characterPreviewCanvas.sortingOrder = characterPreviewOriginalSortingOrder;
            characterPreviewCanvas.sortingLayerID = characterPreviewOriginalSortingLayerId;
            characterPreviewCanvas.renderMode = characterPreviewOriginalRenderMode;
            characterPreviewCanvas.targetDisplay = characterPreviewOriginalTargetDisplay;
            characterPreviewCanvas.worldCamera = characterPreviewOriginalWorldCamera;
            characterPreviewCanvas.planeDistance = characterPreviewOriginalPlaneDistance;
        }

        if (characterPreviewCanvas.gameObject.activeSelf)
            characterPreviewCanvas.gameObject.SetActive(false);
    }


    private Canvas ResolveSharedBackgroundCanvas()
    {
        GameObject[] objects = FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        // BackgroundPanel 자체에 Canvas가 구성되어 있다면 그 정렬값을 최우선으로 사용합니다.
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];
            if (candidate == null || candidate.name != "BackgroundPanel")
                continue;

            Canvas canvas = candidate.GetComponent<Canvas>();
            if (canvas != null)
                return canvas;
        }

        // UIBlurBackgroundManager가 실제 블러 배경을 SharedBlurCanvas로 그리는 구조이므로
        // BackgroundPanel에 별도 Canvas가 없으면 이 Canvas를 기준으로 사용합니다.
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];
            if (candidate == null || candidate.name != "SharedBlurCanvas")
                continue;

            Canvas canvas = candidate.GetComponent<Canvas>();
            if (canvas != null)
                return canvas;
        }

        return null;
    }

    private void ResolveCharacterPreviewCanvas()
    {
        if (characterPreviewCanvas != null)
            return;

        GameObject[] objects = FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];
            if (candidate == null || candidate.name != "CharacterPreviewCanvas")
                continue;

            Canvas canvas = candidate.GetComponent<Canvas>();
            if (canvas == null)
                continue;

            characterPreviewCanvas = canvas;
            return;
        }
    }

    private void OnLocaleChanged(Locale _)
    {
        switch (currentTab)
        {
            case SettingTab.Preview:
                ApplyEmptyInfoText(string.Empty, GameLocalization.Get(LocalizationKeys.CharacterSetting.PreviewInfo));
                break;
            case SettingTab.Skill when skillSettingPanelScript == null || !skillSettingPanelScript.IsDisplayingSkillInfo:
                ApplyEmptyInfoText(
                    GameLocalization.Get(LocalizationKeys.CharacterSetting.SkillInfoTitle),
                    GameLocalization.Get(LocalizationKeys.CharacterSetting.SkillInfoEmpty));
                break;
            case SettingTab.Rune when runeSettingPanelScript == null || !runeSettingPanelScript.IsDisplayingRuneInfo:
                ApplyEmptyInfoText(
                    GameLocalization.Get(LocalizationKeys.CharacterSetting.RuneInfoTitle),
                    GameLocalization.Get(LocalizationKeys.CharacterSetting.RuneInfoEmpty));
                break;
        }
    }

    private void OnDestroy()
    {
        if (runeSettingPanelScript != null)
            runeSettingPanelScript.OnRuneChanged -= RefreshCharacterInfo;

        LobbyPositionSharedModalBackground.HideForOwner(this);
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void InitPresetButtons()
    {
        if (presetButtons == null)
            return;

        for (int i = 0; i < presetButtons.Length; i++)
        {
            if (presetButtons[i] == null)
                continue;

            int presetIndex = i;
            presetButtons[i].onClick.RemoveAllListeners();
            presetButtons[i].interactable = enablePresetButtons;
            presetButtons[i].navigation = new Navigation { mode = Navigation.Mode.None };

            if (enablePresetButtons)
                presetButtons[i].onClick.AddListener(() => SelectPreset(presetIndex));
        }
    }

    private void InitTabButtons()
    {
        // 기존 Button OnClick과 UIPanelButton의 클릭 효과음 연결은 건드리지 않는다.
        // 실제 마우스 클릭의 PointerDown 시점에서만 탭 전환 효과음을 먼저 재생한다.
        SetupTabPointerSoundRelay(previewButton, SettingTab.Preview);
        SetupTabPointerSoundRelay(skillButton, SettingTab.Skill);
        SetupTabPointerSoundRelay(runeButton, SettingTab.Rune);

        InitTabButtonScaleEffects();
    }

    private void SetupTabPointerSoundRelay(Button button, SettingTab targetTab)
    {
        if (button == null)
            return;

        SettingTabPointerSoundRelay relay = button.GetComponent<SettingTabPointerSoundRelay>();
        if (relay == null)
            relay = button.gameObject.AddComponent<SettingTabPointerSoundRelay>();

        relay.Setup(this, (int)targetTab);
    }

    internal void HandleTabButtonPointerDown(int targetTabValue)
    {
        if (!System.Enum.IsDefined(typeof(SettingTab), targetTabValue))
            return;

        SettingTab targetTab = (SettingTab)targetTabValue;
        if (currentTab == targetTab)
            return;

        PlayUserTabTransitionSound(targetTab);
    }

    private void InitTestLevelHoldButtons()
    {
        InitTestLevelHoldButton(testLevelUpButton, true);
        InitTestLevelHoldButton(testLevelDownButton, false);
    }

    private void InitTestLevelHoldButton(Button targetButton, bool isLevelUpButton)
    {
        if (targetButton == null)
            return;

        SettingTestLevelHoldButton holdButton = targetButton.GetComponent<SettingTestLevelHoldButton>();

        if (holdButton == null)
            holdButton = targetButton.gameObject.AddComponent<SettingTestLevelHoldButton>();

        holdButton.Setup(this, isLevelUpButton, testLevelHoldStartDelay, testLevelHoldRepeatInterval);
    }

    public void OpenCharacterSetting(string characterId)
    {
        // 캐릭터를 전환할 때 이전 캐릭터의 스킬/룬 상태를 다시 저장하지 않는다.
        // 각 패널에서 변경한 내용은 기존 저장 시점에 처리한다.

        // CharacterSettingPanel에 들어올 때마다 항상 프리뷰 탭부터 표시한다.
        if (currentTab == SettingTab.Preview)
            currentTab = SettingTab.Skill;

        ShowPreviewSetting();

        currentPartyIndex = -1;

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Clear();
            ShowWarning("선택된 캐릭터가 없다.");
            return;
        }

        if (DataManager.Instance == null)
        {
            Clear();
            ShowWarning("데이터를 불러올 수 없다.");
            return;
        }

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out currentMasterData))
        {
            Clear();
            ShowWarning("캐릭터 데이터를 찾을 수 없다.");
            return;
        }

        currentCharacterId = characterId;
        currentRuntimeData = DataManager.Instance.CharacterRuntimeStore.Get(characterId);

        if (currentRuntimeData == null)
        {
            Clear();
            ShowWarning("캐릭터 데이터를 찾을 수 없다.");
            return;
        }

        RefreshAllPanels();
    }

    /// <summary>
    /// CharacterSettingPanel이 아직 비활성 상태면 슬롯 선택을 예약하고,
    /// 패널 활성화/초기화가 끝난 다음 해당 캐릭터를 엽니다.
    /// </summary>
    public void OpenPartySettingWhenActive(int partyIndex)
    {
        if (isActiveAndEnabled && gameObject.activeInHierarchy)
        {
            pendingPartyIndex = -1;
            OpenPartySetting(partyIndex);
            return;
        }

        pendingPartyIndex = partyIndex;
    }

    public void OpenPartySetting(int partyIndex)
    {
        SaveBeforeBattle();

        currentPartyIndex = partyIndex;

        if (DataManager.Instance == null)
        {
            Clear();
            ShowWarning("데이터를 불러올 수 없다.");
            return;
        }

        string characterId = DataManager.Instance.PartyRuntimeStore.GetCharacterId(partyIndex);

        if (string.IsNullOrWhiteSpace(characterId))
        {
            // 빈 슬롯이면 현재 파티에 아직 편성되지 않은 캐릭터 중
            // CharacterSelect에서 가장 앞에 있는 사용 가능한 캐릭터를 보여준다.
            if (charPick == null ||
                !charPick.TrySelectFirstUnassignedCharacterForSetting(out characterId))
            {
                Clear();
                ShowWarning("선택할 수 있는 캐릭터가 없다.");
                return;
            }
        }
        else
        {
            // 외부의 Character1~3 슬롯에서 설정 화면을 연 경우에도
            // CharacterSelect의 CharBtn 선택 상태를 실제 파티 캐릭터와 맞춘다.
            if (charPick != null)
                charPick.SelectViewedCharacterForSetting(characterId);
        }

        OpenCharacterSetting(characterId);
        currentPartyIndex = partyIndex;
    }

    private void RefreshAllPanels()
    {
        BindSkillSettingPanelIfNeeded();
        RefreshCharacterInfo();
        RefreshPresetButtons();

        if (skillSettingPanelScript != null)
            skillSettingPanelScript.OpenCharacterSetting(currentCharacterId);

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.OpenCharacterSetting(currentCharacterId);

        ShowAllSettingAreasStatic();
    }

    public void SelectPreset(int presetIndex)
    {
        if (currentRuntimeData == null)
        {
            ShowWarning("캐릭터를 먼저 선택해야 한다.");
            return;
        }

        SaveBeforeBattle();

        /*
         * 여기서 실제 프리셋 변경 로직을 처리하면 됨.
         * 예:
         * currentRuntimeData.ActivePresetIndex = presetIndex;
         *
         * 단, CharacterRuntimeData에 ActivePresetIndex가 아직 없다면
         * 해당 필드를 추가해야 함.
         */

        RefreshAllPanels();
    }

    public void OnClickPresetA() => SelectPreset(0);
    public void OnClickPresetB() => SelectPreset(1);
    public void OnClickPresetC() => SelectPreset(2);
    public void OnClickPresetD() => SelectPreset(3);


    private void OnPreviewButtonClicked()
    {
        ShowPreviewSetting();
    }

    private void OnSkillButtonClicked()
    {
        ShowSkillSetting();
    }

    private void OnRuneButtonClicked()
    {
        ShowRuneSetting();
    }

    public void OpenSkillSettingForSlot(SkillSlotButton slotButton)
    {
        if (slotButton == null || skillSettingPanelScript == null)
            return;

        PlayUserTabTransitionSound(SettingTab.Skill);
        ShowSkillSetting(openDefaultSlot: false);
        skillSettingPanelScript.OpenSkillSelectPanel(slotButton);
    }

    public void OpenRuneSettingForSlot(RuneSlotButton slotButton)
    {
        if (slotButton == null || runeSettingPanelScript == null)
            return;

        if (slotButton.IsLocked)
        {
            runeSettingPanelScript.HandleRuneSlotClick(slotButton);
            return;
        }

        PlayUserTabTransitionSound(SettingTab.Rune);
        ShowRuneSetting();
        runeSettingPanelScript.SelectRuneSlotForSetting(slotButton);
    }

    /// <summary>
    /// 사용자가 프리뷰/스킬/룬 버튼을 직접 눌렀을 때만 탭 이동 효과음을 재생한다.
    /// 초기화나 다른 스크립트에서 Show...Setting()을 호출하는 경우에는 재생하지 않는다.
    /// </summary>
    private void PlayUserTabTransitionSound(SettingTab targetTab)
    {
        SettingTab previousTab = currentTab;

        if (previousTab == targetTab)
            return;

        if (targetTab == SettingTab.Preview)
        {
            // 숨겨져 있던 스킬 또는 룬 영역이 다시 등장한다.
            PlayTabTransitionSound(AudioIds.Sfx.CharacterSettingAreaAppear);
            return;
        }

        if (previousTab == SettingTab.Preview)
        {
            // 프리뷰에서 스킬 또는 룬 탭으로 이동하면 한 영역이 나간다.
            PlayTabTransitionSound(AudioIds.Sfx.CharacterSettingAreaExit);
            return;
        }

        // 스킬과 룬 사이를 전환하면 기존 영역은 나가고 새 영역은 등장한다.
        PlayTabTransitionSound(AudioIds.Sfx.CharacterSettingAreaExit);
        PlayTabTransitionSound(AudioIds.Sfx.CharacterSettingAreaAppear);
    }

    private void PlayTabTransitionSound(string sfxId)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(sfxId, tabTransitionSfxVolume);
    }

    public void ShowPreviewSetting()
    {
        ShowAllSettingAreasStatic();
        if (charPick != null)
            charPick.ShowCurrentPreviewNormal();
    }

    public void ShowSkillSetting()
    {
        ShowAllSettingAreasStatic();
    }

    private void ShowSkillSetting(bool openDefaultSlot)
    {
        ShowAllSettingAreasStatic();
    }

    public void ShowRuneSetting()
    {
        ShowAllSettingAreasStatic();
    }

    private void ShowAllSettingAreasStatic()
    {
        if (skillArea != null && !skillArea.activeSelf)
            skillArea.SetActive(true);

        if (runeArea != null && !runeArea.activeSelf)
            runeArea.SetActive(true);

        if (skillSettingPanelScript != null)
            skillSettingPanelScript.SetSkillSelectPanelEnabledForTab(true);

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.SetRuneSelectPanelEnabledForTab(true);
    }

    public void SaveBeforeBattle()
    {
        if (skillSettingPanelScript != null)
            skillSettingPanelScript.SaveBeforeBattle();

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.SaveBeforeBattle();
    }

    private void RefreshCharacterInfo()
    {
        BindCharacterInfoTextIfNeeded();

        if (currentMasterData == null || currentRuntimeData == null)
        {
            Clear();
            return;
        }

        if (characterNameText != null)
            characterNameText.text = GameDataLocalization.CharacterName(currentMasterData);

        if (characterInfoText != null)
            characterInfoText.text = FormatCharacterIntroduction(
                GameDataLocalization.CharacterIntroduction(currentMasterData));

        if (characterInfoPanel != null)
            characterInfoPanel.SetCharacter(currentMasterData, currentRuntimeData);

        RefreshCharacterLevelInfo();
    }

    public void Clear()
    {
        BindSkillSettingPanelIfNeeded();
        currentCharacterId = null;
        currentMasterData = null;
        currentRuntimeData = null;
        currentPartyIndex = -1;

        // 캐릭터 정보가 없는 잠금 버튼을 선택해도 빈칸으로 보이지 않도록
        // 기본 안내 문구와 0 수치를 표시한다.
        if (characterNameText != null)
            characterNameText.text = "잠김";

        if (characterInfoText != null)
            characterInfoText.text = "";

        if (characterInfoPanel != null)
            characterInfoPanel.Clear();

        if (characterLevelText != null)
            characterLevelText.text = "LV. 1";

        if (characterExpText != null)
            characterExpText.text = "EXP 0";

        if (skillSettingPanelScript != null)
            skillSettingPanelScript.ClearForEmptyCharacter();

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.ClearForEmptyCharacter();

        if (InfoTooltip.Instance != null)
            InfoTooltip.Instance.ClearFixedText();

        RefreshPresetButtons();
    }

    private void RefreshCharacterLevelInfo()
    {
        if (currentRuntimeData == null)
            return;

        if (characterLevelText != null)
            characterLevelText.text = "LV. " + currentRuntimeData.Level;

        if (characterExpText != null)
            characterExpText.text = "EXP " + GetDisplayedCharacterExperienceInCurrentLevel(
                currentRuntimeData.Level,
                currentRuntimeData.Exp);
    }

    public static int GetDisplayedCharacterExperienceInCurrentLevel(
        int level,
        int cumulativeExperience)
    {
        int safeLevel = Mathf.Max(1, level);
        int safeCumulativeExperience = Mathf.Max(0, cumulativeExperience);
        int levelStartExperience =
            BattleStageClearExperienceService.GetCumulativeExperienceForLevel(safeLevel);

        return Mathf.Max(0, safeCumulativeExperience - levelStartExperience);
    }

    private void HandleTestLevelCheatKeys()
    {
        if (!enableTestLevelCheatKeys)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (levelDownCheatKey != Key.None && keyboard[levelDownCheatKey].wasPressedThisFrame)
            OnClickTestLevelDown();

        if (levelUpCheatKey != Key.None && keyboard[levelUpCheatKey].wasPressedThisFrame)
            OnClickTestLevelUp();
    }

    public void OnClickTestLevelDown()
    {
        if (currentRuntimeData == null)
        {
            ShowWarning("캐릭터를 먼저 선택해야 한다.");
            return;
        }

        currentRuntimeData.Level = Mathf.Max(1, currentRuntimeData.Level - 1);
        ApplyTestExpByCurrentLevel();
        RefreshAfterLevelChanged();
    }

    public void OnClickTestLevelUp()
    {
        if (currentRuntimeData == null)
        {
            ShowWarning("캐릭터를 먼저 선택해야 한다.");
            return;
        }

        int safeMaxLevel = GetSafeMaxTestLevel();

        if (currentRuntimeData.Level >= safeMaxLevel)
        {
            currentRuntimeData.Level = safeMaxLevel;
            ApplyTestExpByCurrentLevel();
            RefreshAfterLevelChanged();
            ShowWarning(maxLevelWarningMessage);
            return;
        }

        currentRuntimeData.Level = Mathf.Min(safeMaxLevel, currentRuntimeData.Level + 1);
        ApplyTestExpByCurrentLevel();
        RefreshAfterLevelChanged();
    }

    public void SetTestLevelDirect(int level)
    {
        if (currentRuntimeData == null)
        {
            ShowWarning("캐릭터를 먼저 선택해야 한다.");
            return;
        }

        int safeMaxLevel = GetSafeMaxTestLevel();
        currentRuntimeData.Level = Mathf.Clamp(level, 1, safeMaxLevel);
        ApplyTestExpByCurrentLevel();
        RefreshAfterLevelChanged();

        if (level > safeMaxLevel)
            ShowWarning(maxLevelWarningMessage);
    }

    private void ApplyTestExpByCurrentLevel()
    {
        if (currentRuntimeData == null)
            return;

        int safeLevel = Mathf.Clamp(currentRuntimeData.Level, 1, GetSafeMaxTestLevel());
        int safeExpPerLevel = Mathf.Max(0, testExpPerLevel);
        currentRuntimeData.Level = safeLevel;
        currentRuntimeData.Exp = (safeLevel - 1) * safeExpPerLevel;
    }

    private int GetSafeMaxTestLevel()
    {
        return Mathf.Max(1, maxTestLevel);
    }

    private void RefreshAfterLevelChanged()
    {
        SaveBeforeBattle();

        RefreshCharacterInfo();
        RefreshPresetButtons();

        if (skillSettingPanelScript != null)
            skillSettingPanelScript.RefreshByCurrentLevel();

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.RefreshByCurrentLevel();

        if (characterInfoPanel != null)
            characterInfoPanel.Refresh();
    }

    private void RefreshPresetButtons()
    {
        if (presetButtons == null)
            return;

        /*
         * CharacterRuntimeData에 ActivePresetIndex 같은 값이 있다면 여기서 사용.
         * 지금은 임시로 선택 없음 처리.
         */
        int activePresetIndex = -1;

        for (int i = 0; i < presetButtons.Length; i++)
        {
            if (presetButtons[i] == null)
                continue;

            presetButtons[i].interactable = enablePresetButtons;
            presetButtons[i].navigation = new Navigation { mode = Navigation.Mode.None };

            Image image = presetButtons[i].GetComponent<Image>();

            if (image != null)
                image.color = i == activePresetIndex ? presetSelectedColor : presetNormalColor;
        }
    }

    private void RefreshTabButtons()
    {
        SetButtonColor(previewButton, currentTab == SettingTab.Preview ? tabSelectedColor : tabNormalColor);
        SetButtonColor(skillButton, currentTab == SettingTab.Skill ? tabSelectedColor : tabNormalColor);
        SetButtonColor(runeButton, currentTab == SettingTab.Rune ? tabSelectedColor : tabNormalColor);

        // EventSystem 포커스와 무관하게 실제 탭 상태를 기준으로 Glow를 동기화합니다.
        // CharBtn으로 캐릭터를 변경해 탭 UI가 다시 갱신되어도 현재 탭의 Glow가 유지됩니다.
        SetTabButtonGlowSelected(previewButton, currentTab == SettingTab.Preview);
        SetTabButtonGlowSelected(skillButton, currentTab == SettingTab.Skill);
        SetTabButtonGlowSelected(runeButton, currentTab == SettingTab.Rune);

        RefreshTabButtonScaleEffects();
    }

    private static void SetTabButtonGlowSelected(Button button, bool selected)
    {
        if (button == null)
            return;

        SelectedButtonBorderGlow glow = button.GetComponent<SelectedButtonBorderGlow>();
        if (glow == null)
            glow = button.GetComponentInChildren<SelectedButtonBorderGlow>(true);

        if (glow != null)
            glow.SetSelected(selected);
    }

    private void InitTabButtonScaleEffects()
    {
        previewButtonScaleEffect = InitTabButtonScaleEffect(previewButton);
        skillButtonScaleEffect = InitTabButtonScaleEffect(skillButton);
        runeButtonScaleEffect = InitTabButtonScaleEffect(runeButton);

        RefreshTabButtonScaleEffects();
    }

    private CharacterSettingTabButtonScaleEffect InitTabButtonScaleEffect(Button button)
    {
        if (button == null)
            return null;

        CharacterSettingTabButtonScaleEffect effect = button.GetComponent<CharacterSettingTabButtonScaleEffect>();

        if (effect == null)
            effect = button.gameObject.AddComponent<CharacterSettingTabButtonScaleEffect>();

        effect.Setup(
            tabHoverScale,
            tabBreathMaxScale,
            tabSelectedScale,
            tabScaleInDuration,
            tabScaleOutDuration,
            tabBreathSpeed);

        return effect;
    }

    private void RefreshTabButtonScaleEffects()
    {
        if (previewButtonScaleEffect == null && previewButton != null)
            previewButtonScaleEffect = InitTabButtonScaleEffect(previewButton);

        if (skillButtonScaleEffect == null && skillButton != null)
            skillButtonScaleEffect = InitTabButtonScaleEffect(skillButton);

        if (runeButtonScaleEffect == null && runeButton != null)
            runeButtonScaleEffect = InitTabButtonScaleEffect(runeButton);

        if (previewButtonScaleEffect != null)
            previewButtonScaleEffect.SetSelected(currentTab == SettingTab.Preview);

        if (skillButtonScaleEffect != null)
            skillButtonScaleEffect.SetSelected(currentTab == SettingTab.Skill);

        if (runeButtonScaleEffect != null)
            runeButtonScaleEffect.SetSelected(currentTab == SettingTab.Rune);
    }

    private void ResetTabButtonScaleEffects()
    {
        if (previewButtonScaleEffect != null)
            previewButtonScaleEffect.ResetScaleImmediate();

        if (skillButtonScaleEffect != null)
            skillButtonScaleEffect.ResetScaleImmediate();

        if (runeButtonScaleEffect != null)
            runeButtonScaleEffect.ResetScaleImmediate();
    }

    private void BindAreaSlideTargetsIfNeeded()
    {
        if (skillArea != null)
        {
            if (skillAreaBackGround == null)
                skillAreaBackGround = FindDirectChildRectTransform(skillArea.transform, "BackGround");

            if (skillSettingPanelRect == null)
            {
                if (skillSettingPanelScript != null)
                    skillSettingPanelRect = skillSettingPanelScript.transform as RectTransform;
                else
                    skillSettingPanelRect = FindDirectChildRectTransform(skillArea.transform, "SkillSettingPanel");
            }
        }

        if (runeArea != null)
        {
            if (runeAreaBackGround == null)
                runeAreaBackGround = FindDirectChildRectTransform(runeArea.transform, "BackGround");

            if (runeSettingPanelRect == null)
            {
                if (runeSettingPanelScript != null)
                    runeSettingPanelRect = runeSettingPanelScript.transform as RectTransform;
                else
                    runeSettingPanelRect = FindDirectChildRectTransform(runeArea.transform, "RuneSettingPanel");
            }
        }
    }

    private RectTransform FindDirectChildRectTransform(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        Transform child = parent.Find(childName);
        return child as RectTransform;
    }

    private void MoveSettingAreas(bool showSkillArea, bool showRuneArea)
    {
        ShowAllSettingAreasStatic();
    }

    private IEnumerator MoveSettingAreasRoutine(float skillTargetX, float runeTargetY)
    {
        Vector2 skillBackGroundStart = GetAnchoredPosition(skillAreaBackGround);
        Vector2 skillPanelStart = GetAnchoredPosition(skillSettingPanelRect);
        Vector2 runeBackGroundStart = GetAnchoredPosition(runeAreaBackGround);
        Vector2 runePanelStart = GetAnchoredPosition(runeSettingPanelRect);

        float duration = Mathf.Max(0.01f, areaMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            SetAnchoredPositionX(skillAreaBackGround, Mathf.Lerp(skillBackGroundStart.x, skillTargetX, easedT));
            SetAnchoredPositionX(skillSettingPanelRect, Mathf.Lerp(skillPanelStart.x, skillTargetX, easedT));
            SetAnchoredPositionY(runeAreaBackGround, Mathf.Lerp(runeBackGroundStart.y, runeTargetY, easedT));
            SetAnchoredPositionY(runeSettingPanelRect, Mathf.Lerp(runePanelStart.y, runeTargetY, easedT));

            yield return null;
        }

        SetAnchoredPositionX(skillAreaBackGround, skillTargetX);
        SetAnchoredPositionX(skillSettingPanelRect, skillTargetX);
        SetAnchoredPositionY(runeAreaBackGround, runeTargetY);
        SetAnchoredPositionY(runeSettingPanelRect, runeTargetY);

        areaMoveCoroutine = null;
    }

    private Vector2 GetAnchoredPosition(RectTransform target)
    {
        return target != null ? target.anchoredPosition : Vector2.zero;
    }

    private void SetAnchoredPositionX(RectTransform target, float x)
    {
        if (target == null)
            return;

        Vector2 position = target.anchoredPosition;
        position.x = x;
        target.anchoredPosition = position;
    }

    private void SetAnchoredPositionY(RectTransform target, float y)
    {
        if (target == null)
            return;

        Vector2 position = target.anchoredPosition;
        position.y = y;
        target.anchoredPosition = position;
    }

    private static string FormatCharacterIntroduction(string introduction)
    {
        if (string.IsNullOrEmpty(introduction))
            return string.Empty;

        // GameData 소개문은 일반 큰따옴표(" "), 스마트 따옴표(“ ”) 둘 다 사용할 수 있습니다.
        // 따옴표 안에 줄바꿈이 있어도 시작/종료 따옴표 사이 전체를 중앙 정렬합니다.
        int firstQuote = introduction.IndexOf('“');
        char closingQuote = '”';

        if (firstQuote < 0)
        {
            firstQuote = introduction.IndexOf('\"');
            closingQuote = '\"';
        }

        if (firstQuote < 0)
            return introduction;

        int secondQuote = introduction.IndexOf(closingQuote, firstQuote + 1);
        if (secondQuote < 0)
            return introduction;

        string beforeQuote = introduction.Substring(0, firstQuote);
        string quotedText = introduction.Substring(firstQuote, secondQuote - firstQuote + 1);
        string afterQuote = introduction.Substring(secondQuote + 1);

        // TMP 정렬 태그 안에서도 GameData 셀의 줄바꿈이 확실히 유지되도록
        // 인용문 내부의 실제 개행 문자를 <br> 태그로 변환합니다.
        quotedText = quotedText
            .Replace("\r\n", "<br>")
            .Replace("\r", "<br>")
            .Replace("\n", "<br>");

        return beforeQuote + "<align=\"center\">" + quotedText + "</align>" + afterQuote;
    }

    /// <summary>
    /// 새 CharacterSettingPanel 구조에서는 Skill_Area 자체에 SkillSettingPanel이 붙어 있습니다.
    /// 인스펙터에 예전 참조가 남아 있거나 비어 있어도 현재 Skill_Area의 컴포넌트를 우선 연결합니다.
    /// </summary>
    private void BindSkillSettingPanelIfNeeded()
    {
        if (skillArea == null)
        {
            Transform skillAreaTransform = transform.Find("Skill_Area");
            if (skillAreaTransform != null)
                skillArea = skillAreaTransform.gameObject;
        }

        SkillSettingPanel currentPanel = null;

        if (skillArea != null)
            currentPanel = skillArea.GetComponent<SkillSettingPanel>();

        if (currentPanel == null)
        {
            SkillSettingPanel[] panels = GetComponentsInChildren<SkillSettingPanel>(true);
            if (panels != null && panels.Length > 0)
                currentPanel = panels[0];
        }

        if (currentPanel == null)
            return;

        skillSettingPanelScript = currentPanel;
        skillSettingPanelScript.SetSettingController(this);
    }

    private void BindCharacterInfoTextIfNeeded()
    {
        if (characterInfoText != null)
            return;

        TMP_Text[] texts = transform.root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text candidate = texts[i];
            if (candidate != null && candidate.name == "CharacterInfoText")
            {
                characterInfoText = candidate;
                return;
            }
        }
    }


    private void BindInfoAreaIfNeeded()
    {
        if (infoArea != null)
            return;

        RectTransform[] rectTransforms = transform.root.GetComponentsInChildren<RectTransform>(true);

        for (int i = 0; i < rectTransforms.Length; i++)
        {
            if (rectTransforms[i] != null && rectTransforms[i].name == "InfoArea")
            {
                infoArea = rectTransforms[i];
                return;
            }
        }
    }

    private void SetSharedInfoArea(bool clearText)
    {
        BindInfoAreaIfNeeded();

        if (infoArea != null)
            infoArea.gameObject.SetActive(true);

        if (InfoTooltip.Instance == null)
            return;

        if (infoArea != null)
            InfoTooltip.Instance.SetFixedRoot(infoArea);

        if (clearText)
            InfoTooltip.Instance.ClearFixedText();
    }

    private void ApplyEmptyInfoText(string title, string effect)
    {
        if (skillSettingPanelScript != null)
            skillSettingPanelScript.SetEmptyInfoText(title, effect);

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.SetEmptyInfoText(title, effect);
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button == null)
            return;

        Graphic targetGraphic = button.targetGraphic;

        if (targetGraphic == null)
            targetGraphic = button.GetComponent<Graphic>();

        if (targetGraphic != null)
            targetGraphic.color = color;
    }

    private void ShowWarning(string message)
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        if (warningUI != null)
            warningUI.Show(message);
        else
            Debug.LogWarning("[Setting] " + message);
    }
}

/// <summary>
/// 프리뷰/스킬/룬 버튼의 실제 마우스 클릭 시작 시점만 Setting에 전달한다.
/// Button의 기존 OnClick 및 UIPanelButton 효과음 연결은 변경하지 않는다.
/// </summary>
public sealed class SettingTabPointerSoundRelay : MonoBehaviour, IPointerDownHandler
{
    private Setting owner;
    private int targetTabValue;

    public void Setup(Setting setting, int tabValue)
    {
        owner = setting;
        targetTabValue = tabValue;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        owner?.HandleTabButtonPointerDown(targetTabValue);
    }
}
