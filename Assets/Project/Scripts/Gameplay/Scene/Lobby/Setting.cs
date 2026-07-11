using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
    [SerializeField] private GameObject previewArea;
    [SerializeField] private GameObject skillArea;
    [SerializeField] private GameObject runeArea;
    [SerializeField] private Button previewButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button runeButton;
    [SerializeField] private Color tabNormalColor = Color.white;
    [SerializeField] private Color tabSelectedColor = new Color(1f, 0.78f, 0.25f, 1f);

    [Header("Setting Area Tab Scale Effect")]
    [SerializeField] private float tabHoverScale = 1.08f;
    [SerializeField] private float tabBreathMaxScale = 1.12f;
    [SerializeField] private float tabSelectedScale = 1.2f;
    [SerializeField] private float tabScaleInDuration = 0.12f;
    [SerializeField] private float tabScaleOutDuration = 0.10f;
    [SerializeField] private float tabBreathSpeed = 3.5f;

    [Header("Fixed Info Areas")]
    [SerializeField] private RectTransform skillInfoArea;
    [SerializeField] private RectTransform runeInfoArea;

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

    private void Awake()
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        if (charPick == null)
            charPick = FindFirstObjectByType<CharPick>(FindObjectsInactive.Include);

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.OnRuneChanged += RefreshCharacterInfo;

        InitPresetButtons();
        InitTabButtons();
        InitTestLevelHoldButtons();
    }

    private void Start()
    {
        ShowPreviewSetting();
    }

    private void Update()
    {
        HandleTestLevelCheatKeys();
    }

    private void OnDisable()
    {
        ResetTabButtonScaleEffects();
    }

    private void OnDestroy()
    {
        if (runeSettingPanelScript != null)
            runeSettingPanelScript.OnRuneChanged -= RefreshCharacterInfo;

        if (previewButton != null)
            previewButton.onClick.RemoveListener(ShowPreviewSetting);

        if (skillButton != null)
            skillButton.onClick.RemoveListener(ShowSkillSetting);

        if (runeButton != null)
            runeButton.onClick.RemoveListener(ShowRuneSetting);
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
        if (previewButton != null)
        {
            previewButton.onClick.RemoveListener(ShowPreviewSetting);
            previewButton.onClick.AddListener(ShowPreviewSetting);
        }

        if (skillButton != null)
        {
            skillButton.onClick.RemoveListener(ShowSkillSetting);
            skillButton.onClick.AddListener(ShowSkillSetting);
        }

        if (runeButton != null)
        {
            runeButton.onClick.RemoveListener(ShowRuneSetting);
            runeButton.onClick.AddListener(ShowRuneSetting);
        }

        InitTabButtonScaleEffects();
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
        currentTab = SettingTab.Preview;
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

    public void ShowPreviewSetting()
    {
        currentTab = SettingTab.Preview;

        SetAreaActive(previewArea, true);
        SetAreaActive(skillArea, false);
        SetAreaActive(runeArea, false);

        if (skillSettingPanelScript != null)
            skillSettingPanelScript.SetSkillSelectPanelVisible(false);

        if (charPick != null)
            charPick.ShowCurrentPreviewNormal();

        SetFixedInfoArea(null);
        RefreshTabButtons();
    }

    public void ShowSkillSetting()
    {
        currentTab = SettingTab.Skill;

        SetAreaActive(previewArea, false);
        SetAreaActive(skillArea, true);
        SetAreaActive(runeArea, false);

        if (skillSettingPanelScript != null)
            skillSettingPanelScript.SetSkillSelectPanelVisible(true);

        if (charPick != null)
            charPick.ShowCurrentPreviewSkill();

        if (skillInfoArea != null)
            skillInfoArea.gameObject.SetActive(true);

        if (runeInfoArea != null)
            runeInfoArea.gameObject.SetActive(false);

        if (InfoTooltip.Instance != null)
        {
            InfoTooltip.Instance.SetFixedRoot(skillInfoArea);
            InfoTooltip.Instance.ClearFixedText();
        }

        RefreshTabButtons();
    }

    public void ShowRuneSetting()
    {
        currentTab = SettingTab.Rune;

        SetAreaActive(previewArea, false);
        SetAreaActive(skillArea, false);
        SetAreaActive(runeArea, true);

        if (skillSettingPanelScript != null)
            skillSettingPanelScript.SetSkillSelectPanelVisible(false);

        if (charPick != null)
            charPick.ShowCurrentPreviewRune();

        if (skillInfoArea != null)
            skillInfoArea.gameObject.SetActive(false);

        if (runeInfoArea != null)
            runeInfoArea.gameObject.SetActive(true);

        if (InfoTooltip.Instance != null)
        {
            InfoTooltip.Instance.SetFixedRoot(runeInfoArea);
            InfoTooltip.Instance.ClearFixedText();
        }

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

        if (characterNameText != null)
            characterNameText.text = "";

        if (characterInfoText != null)
            characterInfoText.text = "";

        if (characterInfoPanel != null)
            characterInfoPanel.Clear();

        if (characterLevelText != null)
            characterLevelText.text = "";

        if (characterExpText != null)
            characterExpText.text = "";

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

    private void SetAreaActive(GameObject area, bool isActive)
    {
        if (area != null)
            area.SetActive(isActive);
    }

    private void SetFixedInfoArea(RectTransform activeArea)
    {
        if (skillInfoArea != null)
            skillInfoArea.gameObject.SetActive(activeArea == skillInfoArea);

        if (runeInfoArea != null)
            runeInfoArea.gameObject.SetActive(activeArea == runeInfoArea);

        if (InfoTooltip.Instance != null)
        {
            if (activeArea != null)
                InfoTooltip.Instance.SetFixedRoot(activeArea);

            InfoTooltip.Instance.ClearFixedText();
        }
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();

        if (image != null)
            image.color = color;
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