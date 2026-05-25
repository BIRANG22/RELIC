using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;

public class CharacterInfoPanel : MonoBehaviour
{
    [Header("Value Texts")]
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private TMP_Text staminaValueText;
    [SerializeField] private TMP_Text recoveryValueText;
    [SerializeField] private TMP_Text moveValueText;

    [Header("Story")]
    [SerializeField] private TMP_Text storyText;

    private CharacterMasterData currentMasterData;
    private CharacterRuntimeData currentRuntimeData;

    public void SetCharacter(CharacterMasterData masterData, CharacterRuntimeData runtimeData)
    {
        currentMasterData = masterData;
        currentRuntimeData = runtimeData;

        Refresh();
    }

    public void Refresh()
    {
        if (currentMasterData == null)
        {
            Clear();
            return;
        }

        if (hpValueText != null)
            hpValueText.text = currentMasterData.MaxHealth.ToString();

        if (staminaValueText != null)
            staminaValueText.text = currentMasterData.MaxStamina.ToString();

        if (recoveryValueText != null)
            recoveryValueText.text = currentMasterData.StaminaRecovery.ToString();

        if (moveValueText != null)
            moveValueText.text = currentMasterData.MoveValue.ToString();

        if (storyText != null)
            storyText.text = "";
    }

    public void Clear()
    {
        currentMasterData = null;
        currentRuntimeData = null;

        if (hpValueText != null)
            hpValueText.text = "";

        if (staminaValueText != null)
            staminaValueText.text = "";

        if (recoveryValueText != null)
            recoveryValueText.text = "";

        if (moveValueText != null)
            moveValueText.text = "";

        if (storyText != null)
            storyText.text = "";
    }
}