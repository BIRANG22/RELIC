using Relic.Gameplay.Data;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    [SerializeField] private Button previewButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button runeButton;
    [SerializeField] private Color tabNormalColor = Color.white;
    [SerializeField] private Color tabSelectedColor = new Color(1f, 0.78f, 0.25f, 1f);

    [Header("Setting Area Slide Effect")]
    [Tooltip("스킬 영역에서 함께 이동할 BackGround입니다. 비어 있으면 자동으로 찾습니다.")]
    [SerializeField] private RectTransform skillAreaBackGround;
    [Tooltip("스킬 영역에서 함께 이동할 SkillSettingPanel입니다. 비어 있으면 자동으로 찾습니다.")]
    [SerializeField] private RectTransform skillSettingPanelRect;
    [Tooltip("룬 영역에서 함께 이동할 BackGround입니다. 비어 있으면 자동으로 찾습니다.")]
    [SerializeField] private RectTransform runeAreaBackGround;
    [Tooltip("룬 영역에서 함께 이동할 RuneSettingPanel입니다. 비어 있으면 자동으로 찾습니다.")]
    [SerializeField] private RectTransform runeSettingPanelRect;
    [Tooltip("화면에 표시될 때의 Y 좌표입니다.")]
    [SerializeField] private float areaShownY = 0f;
    [Tooltip("화면 위로 숨겨질 때의 Y 좌표입니다.")]
    [SerializeField] private float areaHiddenY = 800f;
    [Tooltip("영역이 목표 위치까지 이동하는 시간입니다.")]
    [SerializeField] private float areaMoveDuration = 0.25f;

    [Header("Setting Area Tab Sound Effect")]
    [Range(0f, 1f)]
    [SerializeField] private float tabTransitionSfxVolume = 1f;

    [Header("Setting Area Tab Scale Effect")]
    [SerializeField] private float tabHoverScale = 1.08f;
    [SerializeField] private float tabBreathMaxScale = 1.12f;
    [SerializeField] private float tabSelectedScale = 1.2f;
    [SerializeField] private float tabScaleInDuration = 0.12f;
    [SerializeField] private float tabScaleOutDuration = 0.10f;
    [SerializeField] private float tabBreathSpeed = 3.5f;

    [Header("Shared Info Area")]
    [SerializeField] private RectTransform infoArea;

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
    private SettingTab currentTab = SettingTab.Preview;

    private CharacterSettingTabButtonScaleEffect previewButtonScaleEffect;
    private CharacterSettingTabButtonScaleEffect skillButtonScaleEffect;
    private CharacterSettingTabButtonScaleEffect runeButtonScaleEffect;
    private Coroutine areaMoveCoroutine;

    private void Awake()
    {
        BindInfoAreaIfNeeded();
        BindAreaSlideTargetsIfNeeded();

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

        InitPresetButtons();
        InitTabButtons();
        DisableTabButtonNavigation();
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

    private void Start()
    {
        // 첫 화면을 구성할 때는 버튼을 누른 것이 아니므로 전환 효과음을 재생하지 않는다.
        SettingTab initialTab = currentTab;
        currentTab = initialTab == SettingTab.Preview ? SettingTab.Skill : SettingTab.Preview;
        ShowPreviewSetting();
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
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        SettingTab targetTab;

        switch (currentTab)
        {
            case SettingTab.Preview:
                targetTab = SettingTab.Rune;
                break;

            case SettingTab.Rune:
                targetTab = SettingTab.Skill;
                break;

            default:
                targetTab = SettingTab.Preview;
                break;
        }

        PlayUserTabTransitionSound(targetTab);

        switch (targetTab)
        {
            case SettingTab.Rune:
                ShowRuneSetting();
                break;

            case SettingTab.Skill:
                ShowSkillSetting();
                break;

            default:
                ShowPreviewSetting();
                break;
        }

    }

    private void OnDisable()
    {
        if (areaMoveCoroutine != null)
        {
            StopCoroutine(areaMoveCoroutine);
            areaMoveCoroutine = null;
        }

        ResetTabButtonScaleEffects();
    }

    private void OnDestroy()
    {
        if (runeSettingPanelScript != null)
            runeSettingPanelScript.OnRuneChanged -= RefreshCharacterInfo;

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
            ShowWarning("선택된 캐릭터가 없습니다.");
            return;
        }

        if (DataManager.Instance == null)
        {
            Clear();
            ShowWarning("DataManager가 없습니다.");
            return;
        }

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out currentMasterData))
        {
            Clear();
            ShowWarning("캐릭터 데이터를 찾을 수 없습니다.");
            return;
        }

        currentCharacterId = characterId;
        currentRuntimeData = DataManager.Instance.CharacterRuntimeStore.Get(characterId);

        if (currentRuntimeData == null)
        {
            Clear();
            ShowWarning("캐릭터 런타임 데이터를 찾을 수 없습니다.");
            return;
        }

        RefreshAllPanels();
    }

    public void OpenPartySetting(int partyIndex)
    {
        SaveBeforeBattle();

        currentPartyIndex = partyIndex;

        if (DataManager.Instance == null)
        {
            Clear();
            ShowWarning("DataManager가 없습니다.");
            return;
        }

        string characterId = DataManager.Instance.PartyRuntimeStore.GetCharacterId(partyIndex);

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Clear();
            ShowWarning("해당 파티 슬롯에 캐릭터가 없습니다.");
            return;
        }

        OpenCharacterSetting(characterId);
        currentPartyIndex = partyIndex;
    }

    private void RefreshAllPanels()
    {
        RefreshCharacterInfo();
        RefreshPresetButtons();

        if (skillSettingPanelScript != null)
            skillSettingPanelScript.OpenCharacterSetting(currentCharacterId);

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.OpenCharacterSetting(currentCharacterId);

        switch (currentTab)
        {
            case SettingTab.Preview:
                ShowPreviewSetting();
                break;
            case SettingTab.Rune:
                ShowRuneSetting();
                break;
            default:
                ShowSkillSetting();
                break;
        }
    }

    public void SelectPreset(int presetIndex)
    {
        if (currentRuntimeData == null)
        {
            ShowWarning("캐릭터를 먼저 선택해야 합니다.");
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
            PlayTabTransitionSound(SfxType.CharacterSettingAreaAppear);
            return;
        }

        if (previousTab == SettingTab.Preview)
        {
            // 프리뷰에서 스킬 또는 룬 탭으로 이동하면 한 영역이 나간다.
            PlayTabTransitionSound(SfxType.CharacterSettingAreaExit);
            return;
        }

        // 스킬과 룬 사이를 전환하면 기존 영역은 나가고 새 영역은 등장한다.
        PlayTabTransitionSound(SfxType.CharacterSettingAreaExit);
        PlayTabTransitionSound(SfxType.CharacterSettingAreaAppear);
    }

    private void PlayTabTransitionSound(SfxType sfxType)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(sfxType, tabTransitionSfxVolume);
    }

    public void ShowPreviewSetting()
    {
        if (currentTab == SettingTab.Preview)
        {
            RefreshTabButtons();
            return;
        }

        currentTab = SettingTab.Preview;

        // 프리뷰 탭에서는 선택용 패널을 먼저 잠근 뒤,
        // 장착 중인 스킬 세팅 패널과 룬 세팅 패널을 한 화면에 함께 표시한다.
        if (skillSettingPanelScript != null)
            skillSettingPanelScript.SetSkillSelectPanelEnabledForTab(false);

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.SetRuneSelectPanelEnabledForTab(false);

        MoveSettingAreas(showSkillArea: true, showRuneArea: true);

        if (charPick != null)
            charPick.ShowCurrentPreviewNormal();

        SetSharedInfoArea(false);
        ApplyEmptyInfoText(string.Empty, "룬/스킬의 정보가 표시됩니다.");
        RefreshTabButtons();
    }

    public void ShowSkillSetting()
    {
        if (currentTab == SettingTab.Skill)
            return;

        currentTab = SettingTab.Skill;

        // 스킬 탭에서는 스킬 영역만 표시한다.
        if (skillSettingPanelScript != null)
            skillSettingPanelScript.SetSkillSelectPanelEnabledForTab(true);

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.SetRuneSelectPanelEnabledForTab(false);

        MoveSettingAreas(showSkillArea: true, showRuneArea: false);

        if (charPick != null)
            charPick.ShowCurrentPreviewSkill();

        SetSharedInfoArea(false);
        ApplyEmptyInfoText("스킬정보", "스킬의 정보가 표시됩니다.");
        RefreshTabButtons();
    }

    public void ShowRuneSetting()
    {
        if (currentTab == SettingTab.Rune)
            return;

        currentTab = SettingTab.Rune;

        // 룬 탭에서는 룬 영역만 표시한다.
        if (skillSettingPanelScript != null)
            skillSettingPanelScript.SetSkillSelectPanelEnabledForTab(false);

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.SetRuneSelectPanelEnabledForTab(true);

        MoveSettingAreas(showSkillArea: false, showRuneArea: true);

        if (charPick != null)
            charPick.ShowCurrentPreviewRune();

        SetSharedInfoArea(false);
        ApplyEmptyInfoText("룬정보", "룬의 정보가 표시됩니다.");
        RefreshTabButtons();
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
        if (currentMasterData == null || currentRuntimeData == null)
        {
            Clear();
            return;
        }

        if (characterNameText != null)
            characterNameText.text = currentMasterData.Name;

        if (characterInfoText != null)
            characterInfoText.text = "";

        if (characterInfoPanel != null)
            characterInfoPanel.SetCharacter(currentMasterData, currentRuntimeData);

        RefreshCharacterLevelInfo();
    }

    public void Clear()
    {
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
            characterExpText.text = "EXP " + currentRuntimeData.Exp;
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
            ShowWarning("캐릭터를 먼저 선택해야 합니다.");
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
            ShowWarning("캐릭터를 먼저 선택해야 합니다.");
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
            ShowWarning("캐릭터를 먼저 선택해야 합니다.");
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

        RefreshTabButtonScaleEffects();
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
        BindAreaSlideTargetsIfNeeded();

        if (skillArea != null && !skillArea.activeSelf)
            skillArea.SetActive(true);

        if (runeArea != null && !runeArea.activeSelf)
            runeArea.SetActive(true);

        if (areaMoveCoroutine != null)
            StopCoroutine(areaMoveCoroutine);

        float skillTargetY = showSkillArea ? areaShownY : areaHiddenY;
        float runeTargetY = showRuneArea ? areaShownY : areaHiddenY;

        areaMoveCoroutine = StartCoroutine(MoveSettingAreasRoutine(skillTargetY, runeTargetY));
    }

    private IEnumerator MoveSettingAreasRoutine(float skillTargetY, float runeTargetY)
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

            SetAnchoredPositionY(skillAreaBackGround, Mathf.Lerp(skillBackGroundStart.y, skillTargetY, easedT));
            SetAnchoredPositionY(skillSettingPanelRect, Mathf.Lerp(skillPanelStart.y, skillTargetY, easedT));
            SetAnchoredPositionY(runeAreaBackGround, Mathf.Lerp(runeBackGroundStart.y, runeTargetY, easedT));
            SetAnchoredPositionY(runeSettingPanelRect, Mathf.Lerp(runePanelStart.y, runeTargetY, easedT));

            yield return null;
        }

        SetAnchoredPositionY(skillAreaBackGround, skillTargetY);
        SetAnchoredPositionY(skillSettingPanelRect, skillTargetY);
        SetAnchoredPositionY(runeAreaBackGround, runeTargetY);
        SetAnchoredPositionY(runeSettingPanelRect, runeTargetY);

        areaMoveCoroutine = null;
    }

    private Vector2 GetAnchoredPosition(RectTransform target)
    {
        return target != null ? target.anchoredPosition : Vector2.zero;
    }

    private void SetAnchoredPositionY(RectTransform target, float y)
    {
        if (target == null)
            return;

        Vector2 position = target.anchoredPosition;
        position.y = y;
        target.anchoredPosition = position;
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
