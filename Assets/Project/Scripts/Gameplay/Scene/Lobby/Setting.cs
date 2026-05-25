using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;

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

    private string currentCharacterId;
    private CharacterMasterData currentMasterData;
    private CharacterRuntimeData currentRuntimeData;

    private void Awake()
    {
        if (runeSettingPanelScript != null)
            runeSettingPanelScript.OnRuneChanged += RefreshCharacterInfo;
    }

    private void OnDestroy()
    {
        if (runeSettingPanelScript != null)
            runeSettingPanelScript.OnRuneChanged -= RefreshCharacterInfo;
    }

    public void OpenCharacterSetting(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            Clear();
            return;
        }

        if (DataManager.Instance == null)
        {
            Clear();
            return;
        }

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out currentMasterData))
        {
            Clear();
            return;
        }

        currentCharacterId = characterId;
        currentRuntimeData = DataManager.Instance.CharacterRuntimeStore.Get(characterId);

        if (currentRuntimeData == null)
        {
            Clear();
            return;
        }

        RefreshCharacterInfo();

        if (skillSettingPanelScript != null)
            skillSettingPanelScript.OpenCharacterSetting(characterId);

        if (runeSettingPanelScript != null)
            runeSettingPanelScript.OpenCharacterSetting(characterId);
    }

    public void OpenPartySetting(int partyIndex)
    {
        if (DataManager.Instance == null)
        {
            Clear();
            return;
        }

        string characterId = DataManager.Instance.PartyRuntimeStore.GetCharacterId(partyIndex);

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Clear();
            return;
        }

        OpenCharacterSetting(characterId);
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
}