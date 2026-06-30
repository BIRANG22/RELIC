using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyMainPanelKeyboardInputController : MonoBehaviour
{
    [Header("Lobby Main Panel")]
    [SerializeField] private GameObject lobbyMainPanel;
    [SerializeField] private bool requireLobbyMainPanelActive = true;

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
    [SerializeField] private Button backButton;
    [SerializeField] private Button stageButton;
    [SerializeField] private Button playButton;

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
    [SerializeField] private bool closeStagePanelBySetActiveWhenCloseButtonMissing = true;

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

    [Header("Lobby Main Keys")]
    [SerializeField] private KeyCode backKey = KeyCode.Escape;
    [SerializeField] private KeyCode partySlot0Key = KeyCode.Alpha1;
    [SerializeField] private KeyCode partySlot1Key = KeyCode.Alpha2;
    [SerializeField] private KeyCode partySlot2Key = KeyCode.Alpha3;
    [SerializeField] private KeyCode stageKey = KeyCode.Tab;
    [SerializeField] private KeyCode playKey = KeyCode.Space;

    [Header("Stage Select Keys")]
    [SerializeField] private KeyCode stageMoveUpKey = KeyCode.W;
    [SerializeField] private KeyCode stageMoveDownKey = KeyCode.S;
    [SerializeField] private KeyCode stageSelectKey = KeyCode.Space;
    [SerializeField] private KeyCode stageCloseKey = KeyCode.Escape;
    [Tooltip("When enabled, the LobbyMain stage key also closes StageSelectPanel while the panel is open. Default stage key is Tab.")]
    [SerializeField] private bool useStageKeyToCloseStagePanel = true;

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
    [SerializeField] private float inputCooldown = 0.08f;

    private float nextInputAllowedTime;
    private int currentStageIndex = -1;
    private bool wasStagePanelActive;
    private GameObject currentHoveredStageObject;
    private int currentCharacterIndex;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private void Awake()
    {
        AutoBindCharacterReferencesIfNeeded();
    }

    private void OnEnable()
    {
        AutoBindCharacterReferencesIfNeeded();
        SyncCurrentCharacterIndex();
    }

    private void Update()
    {
        if (!isActiveAndEnabled)
            return;

        bool stagePanelActive = IsPanelActive(stageSelectPanel);

        if (stagePanelActive && !wasStagePanelActive)
            OnStagePanelOpened();
        else if (!stagePanelActive && wasStagePanelActive)
            ClearStageHover();

        wasStagePanelActive = stagePanelActive;

        if (CanHandleCharacterSettingInput())
        {
            HandleCharacterSettingInput();
            return;
        }

        if (stagePanelActive)
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

        if (Input.GetKeyDown(backKey))
        {
            InvokeButton(backButton);
            BlockInputForCooldown();
            return;
        }

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
            InvokeButton(stageButton);
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(playKey))
        {
            InvokeButton(playButton);
            BlockInputForCooldown();
        }
    }

    private void HandleStageSelectInput()
    {
        if (!CanHandleSharedInput())
            return;

        if (Input.GetKeyDown(stageCloseKey) || (useStageKeyToCloseStagePanel && Input.GetKeyDown(stageKey)))
        {
            CloseStagePanel();
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(stageMoveUpKey))
        {
            MoveStageSelection(1);
            BlockInputForCooldown();
            return;
        }

        if (Input.GetKeyDown(stageMoveDownKey))
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
            InvokeButton(characterBackButton);
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

        if (requireLobbyMainPanelActive && !IsPanelActive(lobbyMainPanel))
            return false;

        if (HasActivePanel(blockingPanels))
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

        if (selectFirstStageWhenPanelOpens)
            ApplyStageHover(currentStageIndex);
    }

    private void MoveStageSelection(int direction)
    {
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
        ClearStageHover();

        if (InvokeButton(stagePanelCloseButton))
            return;

        if (closeStagePanelBySetActiveWhenCloseButtonMissing && stageSelectPanel != null)
            stageSelectPanel.SetActive(false);
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
        bool skillOpen = IsPanelActive(skillArea);
        bool runeOpen = IsPanelActive(runeArea);

        if (skillOpen && !runeOpen)
        {
            OpenRuneArea();
            return;
        }

        OpenSkillArea();
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

    private bool HasActivePanel(GameObject[] panels)
    {
        if (panels == null)
            return false;

        for (int i = 0; i < panels.Length; i++)
        {
            if (IsPanelActive(panels[i]))
                return true;
        }

        return false;
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
