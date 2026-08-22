using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class LobbyMainPanelKeyboardInputController : MonoBehaviour
{
    [Header("Position Panel")]
    [FormerlySerializedAs("lobbyMainPanel")]
    [Tooltip("다른 팝업이 열려 있지 않을 때 ESC로 MenuPanel을 열 수 있는 기본 로비 패널입니다.")]
    [SerializeField] private GameObject positionPanel;
    [SerializeField] private bool requirePositionPanelActive = true;

    [Header("Character Setting Panel")]
    [SerializeField] private GameObject characterSettingPanel;
    [SerializeField] private bool requireCharacterSettingPanelActive = true;

    [Header("Block Main Input When Active")]
    [Tooltip("Register panels that should stop only LobbyMainPanel keyboard input, such as popup panels. StageSelectPanel can be registered here because stage input is handled first.")]
    [SerializeField] private GameObject[] blockingPanels;

    [Header("Block Character Setting Input When Active")]
    [Tooltip("Register popup panels that should stop CharacterSettingPanel keyboard input. Do not register CharacterSettingPanel itself.")]
    [SerializeField] private GameObject[] characterBlockingPanels;

    [Header("Lobby Main Buttons")]
    [SerializeField] private Button stageButton;
    [SerializeField] private Button playButton;

    [Header("Lobby Menu Panel")]
    [Tooltip("로비 메뉴를 열고 닫는 전용 컨트롤러입니다. MenuButton, ContinueButton, ESC가 같은 방식으로 동작하게 합니다.")]
    [SerializeField] private LobbyMenuController lobbyMenuController;
    [Tooltip("로비 메뉴 패널입니다. 컨트롤러가 비어 있을 때 자동 연결과 입력 차단 확인에 사용합니다.")]
    [SerializeField] private GameObject menuPanel;
    [Tooltip("MenuPanel이 열려 있으면 로비 메인 키보드 입력을 막습니다.")]
    [SerializeField] private bool blockMainInputWhenMenuPanelOpen = true;

    [Header("ESC 우선 닫기 패널")]
    [Tooltip("열려 있으면 ESC 입력 시 메뉴보다 먼저 닫히는 유물 상점 패널입니다.")]
    [SerializeField] private GameObject relicShopPanel;
    [Tooltip("유물 상점 패널을 닫을 때 사용할 프레젠터입니다. 비워두면 자동으로 찾습니다.")]
    [SerializeField] private LobbyRelicShopPresenter relicShopPresenter;
    [Tooltip("열려 있으면 ESC 입력 시 메뉴보다 먼저 닫히는 배양조 패널입니다.")]
    [SerializeField] private GameObject cultureTankPanel;
    [Tooltip("배양조 패널을 닫고 내부 선택 상태를 정리하는 프리젠터입니다.")]
    [SerializeField] private LobbyCultureTankPanelPresenter cultureTankPanelPresenter;
    [Tooltip("열려 있으면 ESC 입력 시 메뉴보다 먼저 닫히는 침식도 선택 패널입니다.")]
    [SerializeField] private GameObject erosionSelectPanel;
    [Tooltip("침식도 선택창을 정상적으로 닫아 월드 입력 차단까지 해제하는 버튼 컨트롤러입니다. 비워두면 자동으로 찾습니다.")]
    [SerializeField] private LobbyErosionMirrorButton erosionMirrorButton;

    [Header("Lobby Main Party Slots")]
    [SerializeField] private GameObject partySlot0;
    [SerializeField] private GameObject partySlot1;
    [SerializeField] private GameObject partySlot2;

    [Header("Stage Select Panel")]
    [SerializeField] private GameObject stageSelectPanel;
    [SerializeField] private Button stagePanelCloseButton;
    [SerializeField] private Button[] stageSelectButtons = new Button[4];
    [SerializeField] private bool selectFirstStageWhenPanelOpens = true;
    [SerializeField] private bool wrapStageSelection = true;
    [Tooltip("스테이지 패널을 로비 메인에서 항상 켜둡니다. StageButton을 제거하거나 비활성화한 구조에서 사용합니다.")]
    [SerializeField] private bool keepStagePanelAlwaysOpen = true;
    [Tooltip("항상 켜둔 스테이지 패널을 별도 입력 모드로 처리할지 설정합니다. 기본값은 false이며, Space는 PlayButton 입력으로 유지됩니다.")]
    [SerializeField] private bool handleAlwaysOpenStagePanelAsModal = false;
    [SerializeField] private bool closeStagePanelBySetActiveWhenCloseButtonMissing = true;
    [Tooltip("스테이지 버튼을 중앙 선택 방식으로 회전시키는 컨트롤러입니다.")]
    [SerializeField] private LobbyStageButtonCarousel stageButtonCarousel;

    [Header("Character Setting Buttons")]
    [SerializeField] private Button characterBackButton;
    [SerializeField] private Button selectButton;
    [SerializeField] private CharacterConfirmButton characterConfirmButton;

    [Header("Character Select")]
    [SerializeField] private CharPick charPick;
    [SerializeField] private Transform charButtonRoot;
    [SerializeField] private List<CharBtn> characterButtons = new List<CharBtn>();
    [SerializeField] private bool autoBindCharacterButtons = true;
    [SerializeField] private bool wrapCharacterSelection = true;

    [Header("Character Party Slots")]
    [SerializeField] private GameObject characterPartySlot0;
    [SerializeField] private GameObject characterPartySlot1;
    [SerializeField] private GameObject characterPartySlot2;

    [Header("Skill Rune Toggle")]
    [SerializeField] private Button skillButton;
    [SerializeField] private Button runeButton;
    [SerializeField] private CharacterSubPanelOpenButton skillOpenButton;
    [SerializeField] private CharacterSubPanelOpenButton runeOpenButton;
    [SerializeField] private GameObject skillArea;
    [SerializeField] private GameObject runeArea;
    [Tooltip("CharacterSettingPanel의 프리뷰/룬/스킬 탭 전환을 담당하는 Setting 컴포넌트입니다.")]
    [SerializeField] private Setting characterSettingController;

    [Header("Skill Rune Toggle Keyboard SFX")]
    [Tooltip("CharacterSettingPanel에서 Tab 키로 SkillArea/RuneArea를 전환할 때 마우스 호버와 같은 효과음을 재생합니다.")]
    [SerializeField] private bool playSkillRuneKeyboardHoverSfx = true;
    [SerializeField] private SfxType skillRuneKeyboardHoverSfx = SfxType.NormalButtonHover;
    [SerializeField, Range(0f, 2f)] private float skillRuneKeyboardHoverSfxVolume = 1f;

    [Header("Lobby Main Keys")]
    [SerializeField] private KeyCode backKey = KeyCode.Escape;
    [SerializeField] private KeyCode partySlot0Key = KeyCode.Alpha1;
    [SerializeField] private KeyCode partySlot1Key = KeyCode.Alpha2;
    [SerializeField] private KeyCode partySlot2Key = KeyCode.Alpha3;
    [SerializeField] private KeyCode stageKey = KeyCode.Tab;
    [SerializeField] private KeyCode playKey = KeyCode.Space;

    [Header("Stage Select Keys")]
    [SerializeField] private KeyCode stageSelectKey = KeyCode.Space;
    [SerializeField] private KeyCode stageCloseKey = KeyCode.Escape;

    [Header("Character Setting Keys")]
    [SerializeField] private KeyCode characterBackKey = KeyCode.Escape;
    [SerializeField] private KeyCode characterMoveLeftKey = KeyCode.A;
    [SerializeField] private KeyCode characterMoveRightKey = KeyCode.D;
    [SerializeField] private KeyCode characterTabKey = KeyCode.Tab;
    [SerializeField] private KeyCode characterSlot0Key = KeyCode.Alpha1;
    [SerializeField] private KeyCode characterSlot1Key = KeyCode.Alpha2;
    [SerializeField] private KeyCode characterSlot2Key = KeyCode.Alpha3;
    [SerializeField] private KeyCode characterCurrentButtonClickKey = KeyCode.F;
    [SerializeField] private KeyCode characterSelectKey = KeyCode.Space;

    [Header("Option")]
    [SerializeField] private bool ignoreWhenInputFieldSelected = true;
    [SerializeField] private bool ignoreWhenPointerDragging = true;
    [SerializeField] private bool requireButtonInteractable = true;
    [Tooltip("SettingWarningUI처럼 잠깐 표시되는 경고 UI는 켜져 있어도 로비 키보드 조작을 막지 않습니다.")]
    [SerializeField] private bool ignoreWarningUIBlockingPanel = true;
    [SerializeField] private float inputCooldown = 0.08f;

    private float nextInputAllowedTime;
    private int currentStageIndex = -1;
    private bool wasStagePanelActive;
    private GameObject currentHoveredStageObject;
    private int currentCharacterIndex;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private void Awake()
    {
        AutoBindPositionPanelIfNeeded();
        AutoBindCharacterReferencesIfNeeded();
        AutoBindStageCarouselIfNeeded();
        AutoBindLobbyMenuControllerIfNeeded();
        AutoBindEscapePriorityPanelsIfNeeded();
        OpenStagePanelIfNeeded();
    }

    private void OnEnable()
    {
        AutoBindPositionPanelIfNeeded();
        AutoBindCharacterReferencesIfNeeded();
        AutoBindStageCarouselIfNeeded();
        AutoBindLobbyMenuControllerIfNeeded();
        AutoBindEscapePriorityPanelsIfNeeded();
        SyncCurrentCharacterIndex();
        OpenStagePanelIfNeeded();
    }

    private void Update()
    {
        if (!isActiveAndEnabled)
            return;

        OpenStagePanelIfNeeded();

        bool stagePanelActive = IsPanelActive(stageSelectPanel);

        if (stagePanelActive && !wasStagePanelActive)
            OnStagePanelOpened();
        else if (!stagePanelActive && wasStagePanelActive)
            ClearStageHover();

        wasStagePanelActive = stagePanelActive;

        if (IsLobbyMenuOpen())
        {
            HandleMenuPanelInput();
            return;
        }

        if (Input.GetKeyDown(backKey) && CanHandleSharedInput())
        {
            // ESC 우선순위:
            // 1. 유물 상점 닫기
            // 2. 침식도 선택 패널 닫기
            // 3. CharacterSettingPanel 닫기
            // 4. PositionPanel만 열려 있을 때 메뉴 열기
            if (TryCloseEscapePriorityPanel())
            {
                BlockInputForCooldown();
                return;
            }

            if (CanOpenMenuFromPositionPanel())
            {
                ToggleMenuPanel();
                BlockInputForCooldown();
            }

            return;
        }

        if (CanHandleCharacterSettingInput())
        {
            HandleCharacterSettingInput();
            return;
        }

        if (stagePanelActive && (!keepStagePanelAlwaysOpen || handleAlwaysOpenStagePanelAsModal))
        {
            HandleStageSelectInput();
            return;
        }

        HandleLobbyMainInput();
    }

    private void HandleLobbyMainInput()
    {
        if (!CanHandleMainInput())
            return;

        if (Input.GetKeyDown(partySlot0Key) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            InvokePartySlot(partySlot0);
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(partySlot1Key) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            InvokePartySlot(partySlot1);
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(partySlot2Key) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            InvokePartySlot(partySlot2);
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(stageKey))
        {
            if (keepStagePanelAlwaysOpen && IsPanelActive(stageSelectPanel))
            {
                // 스테이지 패널이 항상 켜져 있는 구조에서는 Tab으로 다음 스테이지를 중앙으로 불러옵니다.
                // A/D는 침식 난이도 선택에 사용하므로 여기서는 처리하지 않습니다.
                MoveStageSelection(1);
                BlockInputForCooldown();
                return;
            }

            InvokeButton(stageButton);
            BlockInputForCooldown();
            return;
        }

        if (keepStagePanelAlwaysOpen && IsPanelActive(stageSelectPanel))
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                MoveStageSelection(1);
                BlockInputForCooldown();
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                MoveStageSelection(-1);
                BlockInputForCooldown();
                return;
            }
        }

        if (Input.GetKeyDown(playKey))
        {
            InvokeButton(playButton);
            BlockInputForCooldown();
        }
    }


    private void AutoBindPositionPanelIfNeeded()
    {
        if (positionPanel != null)
            return;

        positionPanel = FindSceneObjectByName("PositionPanel");
    }

    private void AutoBindEscapePriorityPanelsIfNeeded()
    {
        if (relicShopPresenter == null)
            relicShopPresenter = FindFirstObjectByType<LobbyRelicShopPresenter>(FindObjectsInactive.Include);

        if (relicShopPanel == null)
            relicShopPanel = FindSceneObjectByName("RelicShopPanel");

        if (cultureTankPanelPresenter == null)
            cultureTankPanelPresenter = FindFirstObjectByType<LobbyCultureTankPanelPresenter>(FindObjectsInactive.Include);

        if (cultureTankPanel == null)
            cultureTankPanel = FindSceneObjectByName("CultureTankPanel");

        if (erosionSelectPanel == null)
            erosionSelectPanel = FindSceneObjectByName("ErosionSelectPanel");

        if (erosionMirrorButton == null ||
            !erosionMirrorButton.ControlsPanel(erosionSelectPanel))
        {
            erosionMirrorButton = FindErosionMirrorButtonForPanel(erosionSelectPanel);
        }
    }

    private bool TryCloseEscapePriorityPanel()
    {
        AutoBindEscapePriorityPanelsIfNeeded();

        if (IsPanelActive(relicShopPanel))
        {
            if (relicShopPresenter != null)
                relicShopPresenter.Close();
            else
                relicShopPanel.SetActive(false);

            return true;
        }

        if (IsPanelActive(cultureTankPanel))
        {
            if (cultureTankPanelPresenter != null)
                cultureTankPanelPresenter.Close();
            else
                cultureTankPanel.SetActive(false);

            return true;
        }

        if (IsPanelActive(erosionSelectPanel))
        {
            // 씬에 같은 컴포넌트가 여러 개 있을 수 있으므로,
            // 현재 열린 패널을 실제로 관리하는 인스턴스를 다시 찾습니다.
            LobbyErosionMirrorButton controller =
                FindErosionMirrorButtonForPanel(erosionSelectPanel);

            if (controller != null)
            {
                erosionMirrorButton = controller;
                controller.CloseErosionSelectPanel();
            }
            else
            {
                erosionSelectPanel.SetActive(false);
                LobbyPositionModalInputBlocker.Unblock(null);
                Debug.LogWarning(
                    "[LobbyMainPanelKeyboardInputController] ErosionSelectPanel을 관리하는 " +
                    "LobbyErosionMirrorButton을 찾지 못해 입력 차단을 강제로 해제했습니다.",
                    this);
            }

            return true;
        }

        if (IsPanelActive(characterSettingPanel))
        {
            AutoBindCharacterReferencesIfNeeded();

            if (InvokeButton(characterBackButton))
                return true;

            Debug.LogWarning(
                "[LobbyMainPanelKeyboardInputController] CharacterSettingPanel의 BackButton을 찾지 못했거나 버튼을 실행할 수 없습니다.",
                this);
            return false;
        }

        return false;
    }

    private bool CanOpenMenuFromPositionPanel()
    {
        AutoBindPositionPanelIfNeeded();

        if (!IsPanelActive(positionPanel))
            return false;

        if (IsPanelActive(characterSettingPanel))
            return false;

        if (HasActiveBlockingPanel(blockingPanels))
            return false;

        return true;
    }


    private static LobbyErosionMirrorButton FindErosionMirrorButtonForPanel(
        GameObject targetPanel)
    {
        if (targetPanel == null)
            return null;

        LobbyErosionMirrorButton[] buttons =
            FindObjectsByType<LobbyErosionMirrorButton>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            LobbyErosionMirrorButton button = buttons[i];

            if (button != null && button.ControlsPanel(targetPanel))
                return button;
        }

        return null;
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform item = transforms[i];
            if (item != null && item.gameObject.scene.IsValid() && item.name == objectName)
                return item.gameObject;
        }

        return null;
    }

    private void AutoBindLobbyMenuControllerIfNeeded()
    {
        if (lobbyMenuController != null)
            return;

        if (menuPanel != null)
        {
            lobbyMenuController = menuPanel.GetComponent<LobbyMenuController>();

            if (lobbyMenuController == null)
                lobbyMenuController = menuPanel.GetComponentInParent<LobbyMenuController>(true);

            if (lobbyMenuController == null)
                lobbyMenuController = menuPanel.GetComponentInChildren<LobbyMenuController>(true);
        }

        if (lobbyMenuController == null)
            lobbyMenuController = GetComponent<LobbyMenuController>();

        if (lobbyMenuController != null && menuPanel == null)
            menuPanel = lobbyMenuController.MenuPanel;
    }

    private bool IsLobbyMenuOpen()
    {
        if (lobbyMenuController != null)
            return lobbyMenuController.IsMenuOpen;

        return IsPanelActive(menuPanel);
    }

    private void ToggleMenuPanel()
    {
        AutoBindLobbyMenuControllerIfNeeded();

        if (lobbyMenuController != null)
        {
            lobbyMenuController.ToggleMenu();
            return;
        }

        if (menuPanel == null)
            return;

        menuPanel.SetActive(!menuPanel.activeSelf);
    }

    private void HandleMenuPanelInput()
    {
        if (!CanHandleSharedInput())
            return;

        if (Input.GetKeyDown(backKey))
        {
            CloseMenuPanel();
            BlockInputForCooldown();
        }
    }

    private void CloseMenuPanel()
    {
        AutoBindLobbyMenuControllerIfNeeded();

        if (lobbyMenuController != null)
        {
            lobbyMenuController.CloseMenu();
            return;
        }

        if (menuPanel == null)
            return;

        menuPanel.SetActive(false);

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            if (EventSystem.current.currentSelectedGameObject.transform.IsChildOf(menuPanel.transform))
                EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void HandleStageSelectInput()
    {
        if (!CanHandleSharedInput())
            return;

        if (Input.GetKeyDown(stageCloseKey))
        {
            CloseStagePanel();
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(stageKey))
        {
            // StageSelectPanel 안에서도 Tab은 패널 닫기가 아니라 다음 스테이지 이동으로 사용합니다.
            MoveStageSelection(1);
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveStageSelection(1);
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveStageSelection(-1);
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(stageSelectKey))
        {
            InvokeCurrentStageButton();
            BlockInputForCooldown();
        }
    }

    private void HandleCharacterSettingInput()
    {
        if (!CanHandleSharedInput())
            return;

        if (Input.GetKeyDown(characterBackKey))
        {
            AutoBindCharacterReferencesIfNeeded();

            if (InvokeButton(characterBackButton))
                BlockInputForCooldown();

            return;
        }

        if (Input.GetKeyDown(characterTabKey))
        {
            HandleSkillRuneTabInput();
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(characterMoveLeftKey) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveCharacterSelection(-1);
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(characterMoveRightKey) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveCharacterSelection(1);
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(characterSlot0Key) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            SelectCharacterPartySlot(0, characterPartySlot0 != null ? characterPartySlot0 : partySlot0);
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(characterSlot1Key) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            SelectCharacterPartySlot(1, characterPartySlot1 != null ? characterPartySlot1 : partySlot1);
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(characterSlot2Key) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            SelectCharacterPartySlot(2, characterPartySlot2 != null ? characterPartySlot2 : partySlot2);
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(characterCurrentButtonClickKey))
        {
            ClickCurrentCharacterButton();
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(characterSelectKey))
        {
            InvokeCharacterSelectButton();
            BlockInputForCooldown();
        }
    }

    private bool CanHandleMainInput()
    {
        if (!CanHandleSharedInput())
            return false;

        AutoBindPositionPanelIfNeeded();

        if (requirePositionPanelActive && !IsPanelActive(positionPanel))
            return false;

        if (blockMainInputWhenMenuPanelOpen && IsLobbyMenuOpen())
            return false;

        if (HasActiveBlockingPanel(blockingPanels))
            return false;

        return true;
    }

    private bool CanHandleCharacterSettingInput()
    {
        if (requireCharacterSettingPanelActive && !IsPanelActive(characterSettingPanel))
            return false;

        if (HasActivePanel(characterBlockingPanels))
            return false;

        return true;
    }

    private bool CanHandleSharedInput()
    {
        if (Time.unscaledTime < nextInputAllowedTime)
            return false;

        if (ignoreWhenInputFieldSelected && IsInputFieldSelected())
            return false;

        if (ignoreWhenPointerDragging && IsPointerDragging())
            return false;

        return true;
    }

    private void OnStagePanelOpened()
    {
        currentStageIndex = GetFirstAvailableStageIndex();

        if (stageButtonCarousel != null)
        {
            stageButtonCarousel.SetSelection(currentStageIndex, true);
            currentStageIndex = stageButtonCarousel.CurrentIndex;
        }

        if (selectFirstStageWhenPanelOpens)
            ApplyStageHover(currentStageIndex);
    }

    private void MoveStageSelection(int direction)
    {
        if (stageButtonCarousel != null)
        {
            int movedIndex = stageButtonCarousel.MoveSelection(direction);

            if (movedIndex >= 0)
            {
                currentStageIndex = movedIndex;
                ApplyStageHover(currentStageIndex);
            }

            return;
        }

        if (stageSelectButtons == null || stageSelectButtons.Length <= 0)
            return;

        int nextIndex = FindNextAvailableStageIndex(currentStageIndex, direction);

        if (nextIndex < 0)
            return;

        currentStageIndex = nextIndex;
        ApplyStageHover(currentStageIndex);
    }

    private int FindNextAvailableStageIndex(int startIndex, int direction)
    {
        if (stageSelectButtons == null || stageSelectButtons.Length <= 0)
            return -1;

        int count = stageSelectButtons.Length;
        int normalizedDirection = direction >= 0 ? 1 : -1;
        int index = startIndex;

        if (index < 0 || index >= count)
            index = normalizedDirection > 0 ? -1 : count;

        for (int i = 0; i < count; i++)
        {
            index += normalizedDirection;

            if (wrapStageSelection)
            {
                if (index < 0)
                    index = count - 1;
                else if (index >= count)
                    index = 0;
            }
            else if (index < 0 || index >= count)
            {
                return -1;
            }

            if (IsStageButtonAvailable(index))
                return index;
        }

        return -1;
    }

    private int GetFirstAvailableStageIndex()
    {
        if (stageSelectButtons == null)
            return -1;

        for (int i = 0; i < stageSelectButtons.Length; i++)
        {
            if (IsStageButtonAvailable(i))
                return i;
        }

        return -1;
    }

    private bool IsStageButtonAvailable(int index)
    {
        if (stageSelectButtons == null)
            return false;

        if (index < 0 || index >= stageSelectButtons.Length)
            return false;

        Button button = stageSelectButtons[index];

        if (button == null)
            return false;

        if (!button.gameObject.activeInHierarchy)
            return false;

        if (requireButtonInteractable && !button.interactable)
            return false;

        return true;
    }

    private void ApplyStageHover(int index)
    {
        if (!IsStageButtonAvailable(index))
            return;

        Button button = stageSelectButtons[index];
        GameObject targetObject = button.gameObject;

        if (currentHoveredStageObject == targetObject)
            return;

        SendPointerExit(currentHoveredStageObject);

        currentHoveredStageObject = targetObject;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(targetObject);

        SendPointerEnter(currentHoveredStageObject);
    }

    private void ClearStageHover()
    {
        SendPointerExit(currentHoveredStageObject);

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == currentHoveredStageObject)
            EventSystem.current.SetSelectedGameObject(null);

        currentHoveredStageObject = null;
        currentStageIndex = -1;
    }

    private void InvokeCurrentStageButton()
    {
        if (!IsStageButtonAvailable(currentStageIndex))
        {
            currentStageIndex = GetFirstAvailableStageIndex();
            ApplyStageHover(currentStageIndex);
        }

        if (IsStageButtonAvailable(currentStageIndex))
            InvokeButton(stageSelectButtons[currentStageIndex]);
    }

    private void CloseStagePanel()
    {
        if (keepStagePanelAlwaysOpen)
        {
            OpenStagePanelIfNeeded();
            return;
        }

        ClearStageHover();

        if (InvokeButton(stagePanelCloseButton))
            return;

        if (closeStagePanelBySetActiveWhenCloseButtonMissing && stageSelectPanel != null)
            stageSelectPanel.SetActive(false);
    }

    private void AutoBindStageCarouselIfNeeded()
    {
        if (stageButtonCarousel != null)
            return;

        if (stageSelectPanel == null)
            return;

        stageButtonCarousel = stageSelectPanel.GetComponentInChildren<LobbyStageButtonCarousel>(true);
    }

    private void OpenStagePanelIfNeeded()
    {
        if (!keepStagePanelAlwaysOpen)
            return;

        if (stageSelectPanel != null && !stageSelectPanel.activeSelf)
            stageSelectPanel.SetActive(true);
    }

    private void MoveCharacterSelection(int direction)
    {
        AutoBindCharacterButtonsIfNeeded();

        if (characterButtons == null || characterButtons.Count <= 0)
            return;

        SyncCurrentCharacterIndex();

        int count = characterButtons.Count;
        int nextIndex = currentCharacterIndex + direction;

        if (wrapCharacterSelection)
        {
            if (nextIndex < 0)
                nextIndex = count - 1;
            else if (nextIndex >= count)
                nextIndex = 0;
        }
        else if (nextIndex < 0 || nextIndex >= count)
        {
            return;
        }

        CharBtn targetButton = characterButtons[nextIndex];

        if (targetButton == null || !targetButton.gameObject.activeInHierarchy)
            return;

        currentCharacterIndex = nextIndex;
        targetButton.Execute(true);
    }

    private void HandleSkillRuneTabInput()
    {
        if (characterSettingController == null)
            characterSettingController = FindFirstObjectByType<Setting>(FindObjectsInactive.Include);

        if (characterSettingController == null)
            return;

        // 탭 전환은 Setting 한 곳에서만 처리한다.
        // 영역을 비활성화하지 않으므로 이동 코루틴도 안전하게 실행된다.
        characterSettingController.CycleTabByKeyboard();
    }

    private void OpenSkillArea()
    {
        if (runeArea != null)
            runeArea.SetActive(false);

        if (skillOpenButton != null)
        {
            skillOpenButton.Execute();
            return;
        }

        if (InvokeButton(skillButton))
            return;

        if (skillArea != null)
            skillArea.SetActive(true);
    }

    private void OpenRuneArea()
    {
        if (skillArea != null)
            skillArea.SetActive(false);

        if (runeOpenButton != null)
        {
            runeOpenButton.Execute();
            return;
        }

        if (InvokeButton(runeButton))
            return;

        if (runeArea != null)
            runeArea.SetActive(true);
    }

    private void PlaySkillRuneKeyboardHoverSfx()
    {
        if (!playSkillRuneKeyboardHoverSfx)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(skillRuneKeyboardHoverSfx, skillRuneKeyboardHoverSfxVolume);
    }

    private void SelectCharacterPartySlot(int slotIndex, GameObject slotObject)
    {
        bool selectedSlot = InvokePartySlot(slotObject, true);

        if (!selectedSlot && CharacterSelectionState.Instance != null)
        {
            CharacterSelectionState.Instance.SelectPartySlot(slotIndex);
            selectedSlot = true;
        }

        if (!selectedSlot)
            return;
    }

    private void ClickCurrentCharacterButton()
    {
        AutoBindCharacterButtonsIfNeeded();
        SyncCurrentCharacterIndex();

        CharBtn currentButton = GetCurrentCharacterButton();

        if (currentButton == null)
            return;

        currentButton.Execute(true);
    }

    private CharBtn GetCurrentCharacterButton()
    {
        if (charPick != null && charPick.CurrentButton != null)
            return charPick.CurrentButton;

        if (characterButtons == null || characterButtons.Count <= 0)
            return null;

        currentCharacterIndex = Mathf.Clamp(currentCharacterIndex, 0, characterButtons.Count - 1);
        return characterButtons[currentCharacterIndex];
    }

    private void InvokeCharacterSelectButton()
    {
        if (InvokeButton(selectButton))
            return;

        if (characterConfirmButton != null)
            characterConfirmButton.Execute();
    }

    private void AutoBindCharacterReferencesIfNeeded()
    {
        if (characterSettingPanel == null)
            characterSettingPanel = FindSceneObjectByName("CharacterSettingPanel");

        if (characterBackButton == null && characterSettingPanel != null)
        {
            Transform backButtonTransform = characterSettingPanel.transform.Find("BackButton");

            if (backButtonTransform == null)
            {
                Transform[] children = characterSettingPanel.GetComponentsInChildren<Transform>(true);

                for (int i = 0; i < children.Length; i++)
                {
                    Transform child = children[i];

                    if (child != null && child.name == "BackButton")
                    {
                        backButtonTransform = child;
                        break;
                    }
                }
            }

            if (backButtonTransform != null)
                characterBackButton = backButtonTransform.GetComponent<Button>();
        }

        if (charPick == null && characterSettingPanel != null)
            charPick = characterSettingPanel.GetComponentInChildren<CharPick>(true);

        AutoBindCharacterButtonsIfNeeded();
    }

    private void AutoBindCharacterButtonsIfNeeded()
    {
        if (!autoBindCharacterButtons)
            return;

        Transform root = charButtonRoot;

        if (root == null && charPick != null)
            root = charPick.transform;

        if (root == null && characterSettingPanel != null)
            root = characterSettingPanel.transform;

        if (root == null)
            return;

        List<CharBtn> foundButtons = new List<CharBtn>();
        CharBtn[] buttons = root.GetComponentsInChildren<CharBtn>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            CharBtn button = buttons[i];

            if (button == null)
                continue;

            if (foundButtons.Contains(button))
                continue;

            foundButtons.Add(button);
        }

        foundButtons.Sort(CompareCharacterButtons);

        if (foundButtons.Count <= 0)
            return;

        bool shouldReplace = characterButtons == null || characterButtons.Count != foundButtons.Count;

        if (!shouldReplace)
        {
            for (int i = 0; i < foundButtons.Count; i++)
            {
                if (characterButtons[i] != foundButtons[i])
                {
                    shouldReplace = true;
                    break;
                }
            }
        }

        if (shouldReplace)
            characterButtons = foundButtons;
    }

    private static int CompareCharacterButtons(CharBtn a, CharBtn b)
    {
        int aIndex = ExtractTrailingNumber(a != null ? a.name : string.Empty);
        int bIndex = ExtractTrailingNumber(b != null ? b.name : string.Empty);

        if (aIndex != bIndex)
            return aIndex.CompareTo(bIndex);

        int aSibling = a != null ? a.transform.GetSiblingIndex() : 0;
        int bSibling = b != null ? b.transform.GetSiblingIndex() : 0;

        return aSibling.CompareTo(bSibling);
    }

    private static int ExtractTrailingNumber(string value)
    {
        if (string.IsNullOrEmpty(value))
            return int.MaxValue;

        int end = value.Length - 1;

        while (end >= 0 && char.IsDigit(value[end]))
            end--;

        if (end >= value.Length - 1)
            return int.MaxValue;

        string numberText = value.Substring(end + 1);

        int number;
        if (int.TryParse(numberText, out number))
            return number;

        return int.MaxValue;
    }

    private void SyncCurrentCharacterIndex()
    {
        AutoBindCharacterButtonsIfNeeded();

        if (characterButtons == null || characterButtons.Count <= 0)
        {
            currentCharacterIndex = 0;
            return;
        }

        if (charPick != null && charPick.CurrentButton != null)
        {
            int index = characterButtons.IndexOf(charPick.CurrentButton);

            if (index >= 0)
            {
                currentCharacterIndex = index;
                return;
            }
        }

        currentCharacterIndex = Mathf.Clamp(currentCharacterIndex, 0, characterButtons.Count - 1);
    }

    private void SendPointerEnter(GameObject targetObject)
    {
        if (targetObject == null || EventSystem.current == null)
            return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = Input.mousePosition
        };

        ExecuteEvents.Execute(targetObject, pointerData, ExecuteEvents.pointerEnterHandler);
    }

    private void SendPointerExit(GameObject targetObject)
    {
        if (targetObject == null || EventSystem.current == null)
            return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = Input.mousePosition
        };

        ExecuteEvents.Execute(targetObject, pointerData, ExecuteEvents.pointerExitHandler);
    }

    private void BlockInputForCooldown()
    {
        nextInputAllowedTime = Time.unscaledTime + Mathf.Max(0f, inputCooldown);
    }

    private bool InvokeButton(Button button)
    {
        if (button == null)
            return false;

        if (!button.gameObject.activeInHierarchy)
            return false;

        if (requireButtonInteractable && !button.interactable)
            return false;

        button.onClick.Invoke();
        return true;
    }

    private bool InvokePartySlot(GameObject slotObject)
    {
        return InvokePartySlot(slotObject, false);
    }

    private bool InvokePartySlot(GameObject slotObject, bool allowInactiveSlotObject)
    {
        if (slotObject == null)
            return false;

        if (!allowInactiveSlotObject && !slotObject.activeInHierarchy)
            return false;

        Button button = slotObject.GetComponent<Button>();

        if (button == null)
            button = slotObject.GetComponentInChildren<Button>(true);

        if (button != null && (!allowInactiveSlotObject || button.gameObject.activeInHierarchy))
            return InvokeButton(button);

        PartySlotButton partySlotButton = slotObject.GetComponent<PartySlotButton>();

        if (partySlotButton == null)
            partySlotButton = slotObject.GetComponentInChildren<PartySlotButton>(true);

        if (partySlotButton != null && (allowInactiveSlotObject || partySlotButton.gameObject.activeInHierarchy))
        {
            partySlotButton.Execute();
            return true;
        }

        if (allowInactiveSlotObject)
            return false;

        return ExecutePointerClick(slotObject);
    }

    private bool ExecutePointerClick(GameObject targetObject)
    {
        if (targetObject == null || EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = Input.mousePosition
        };

        GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(targetObject);

        if (handler == null)
            handler = targetObject;

        ExecuteEvents.Execute(handler, pointerData, ExecuteEvents.pointerClickHandler);
        return true;
    }

    private bool HasActiveBlockingPanel(GameObject[] panels)
    {
        if (panels == null)
            return false;

        for (int i = 0; i < panels.Length; i++)
        {
            GameObject panel = panels[i];

            if (keepStagePanelAlwaysOpen && stageSelectPanel != null && panel == stageSelectPanel)
                continue;

            if (IsNonBlockingWarningPanel(panel))
                continue;

            if (IsPanelActive(panel))
                return true;
        }

        return false;
    }

    private bool HasActivePanel(GameObject[] panels)
    {
        if (panels == null)
            return false;

        for (int i = 0; i < panels.Length; i++)
        {
            GameObject panel = panels[i];

            if (IsNonBlockingWarningPanel(panel))
                continue;

            if (IsPanelActive(panel))
                return true;
        }

        return false;
    }

    private bool IsNonBlockingWarningPanel(GameObject panel)
    {
        if (!ignoreWarningUIBlockingPanel || panel == null)
            return false;

        return panel.GetComponent<SettingWarningUI>() != null ||
               panel.GetComponentInParent<SettingWarningUI>(true) != null ||
               panel.GetComponentInChildren<SettingWarningUI>(true) != null;
    }

    private bool IsPanelActive(GameObject panel)
    {
        return panel != null && panel.activeInHierarchy;
    }

    private bool IsInputFieldSelected()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected == null)
            return false;

        if (selected.GetComponent<InputField>() != null)
            return true;

        if (selected.GetComponent<TMP_InputField>() != null)
            return true;

        return false;
    }

    private bool IsPointerDragging()
    {
        if (EventSystem.current == null)
            return false;

        if (!Input.GetMouseButton(0))
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            if (raycastResults[i].gameObject == null)
                continue;

            if (raycastResults[i].gameObject.GetComponentInParent<ScrollRect>() != null)
                return true;
        }

        return false;
    }
}
