using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
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

    [Header("Preset UI")]
    [SerializeField] private Button[] presetButtons = new Button[4];
    [SerializeField] private bool enablePresetButtons = false;
    [SerializeField] private Color presetNormalColor = Color.white;
    [SerializeField] private Color presetSelectedColor = new Color(1f, 0.78f, 0.25f, 1f);

    [Header("Setting Area Tabs")]
    [SerializeField] private GameObject skillArea;
    [SerializeField] private GameObject runeArea;
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

    private int currentPartyIndex = -1;
    private bool isSkillTabOpen = true;

    private CharacterSettingTabButtonScaleEffect skillButtonScaleEffect;
    private CharacterSettingTabButtonScaleEffect runeButtonScaleEffect;

    private void Awake()
    {
        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.OnRuneChanged += RefreshCharacterInfo;

        InitPresetButtons();
        InitTabButtons();
        InitTestLevelHoldButtons();
    }

    private void Start()
    {
        ShowSkillSetting();
    }

    private void OnDisable()
    {
        ResetTabButtonScaleEffects();
    }

    private void OnDestroy()
    {
        if (runeSettingPanelScript != null)
            runeSettingPanelScript.OnRuneChanged -= RefreshCharacterInfo;

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
        SaveBeforeBattle();

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

        if (isSkillTabOpen)
            ShowSkillSetting();
        else
            ShowRuneSetting();
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

    public void ShowSkillSetting()
    {
        isSkillTabOpen = true;

        if (skillArea != null)
            skillArea.SetActive(true);

        if (runeArea != null)
            runeArea.SetActive(false);

        if (skillSettingPanelScript != null)
            skillSettingPanelScript.SetSkillSelectPanelVisible(true);

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
        isSkillTabOpen = false;

        if (skillArea != null)
            skillArea.SetActive(false);

        if (runeArea != null)
            runeArea.SetActive(true);

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
        SetButtonColor(skillButton, isSkillTabOpen ? tabSelectedColor : tabNormalColor);
        SetButtonColor(runeButton, isSkillTabOpen ? tabNormalColor : tabSelectedColor);

        RefreshTabButtonScaleEffects();
    }

    private void InitTabButtonScaleEffects()
    {
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
        if (skillButtonScaleEffect == null && skillButton != null)
            skillButtonScaleEffect = InitTabButtonScaleEffect(skillButton);

        if (runeButtonScaleEffect == null && runeButton != null)
            runeButtonScaleEffect = InitTabButtonScaleEffect(runeButton);

        if (skillButtonScaleEffect != null)
            skillButtonScaleEffect.SetSelected(isSkillTabOpen);

        if (runeButtonScaleEffect != null)
            runeButtonScaleEffect.SetSelected(!isSkillTabOpen);
    }

    private void ResetTabButtonScaleEffects()
    {
        if (skillButtonScaleEffect != null)
            skillButtonScaleEffect.ResetScaleImmediate();

        if (runeButtonScaleEffect != null)
            runeButtonScaleEffect.ResetScaleImmediate();
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