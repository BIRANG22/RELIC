using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CharacterInfoPanel : MonoBehaviour
{
    [Header("Value Texts")]
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField, FormerlySerializedAs("staminaValueText")] private TMP_Text costValueText;
    [SerializeField, FormerlySerializedAs("staminaRecoveryValueText")] private TMP_Text recoveryValueText;
    [SerializeField] private TMP_Text moveValueText;

    [Header("Label Texts")]
    [SerializeField, FormerlySerializedAs("staminaLabelText")] private TMP_Text costLabelText;
    [SerializeField, FormerlySerializedAs("staminaRecoveryLabelText")] private TMP_Text recoveryLabelText;
    [SerializeField] private string costLabel = "코스트";
    [SerializeField] private string recoveryLabel = "코스트 회복량";

    [Header("Rune Modified Stat Display")]
    [SerializeField] private bool showModifiedStatDelta = true;
    [SerializeField] private string statIncreaseColor = "#4E66DF";
    [SerializeField] private string statDecreaseColor = "#D94B4B";

    [Header("Character Mark")]
    [SerializeField] private Image characterMarkImage;
    [SerializeField] private bool autoBindCharacterMarkImage = true;
    [SerializeField] private bool hideMarkWhenMissing = true;

    [Header("Story")]
    [SerializeField] private TMP_Text storyText;

    private CharacterMasterData currentMasterData;
    private CharacterRuntimeData currentRuntimeData;

    public CharacterMasterData CurrentMasterData => currentMasterData;
    public CharacterRuntimeData CurrentRuntimeData => currentRuntimeData;

    private void Awake()
    {
        AutoBindCharacterMarkImageIfNeeded();
        ApplyCostLabels();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoBindCharacterMarkImageIfNeeded();
        ApplyCostLabels();
    }
#endif

    public void SetCharacter(CharacterMasterData masterData, CharacterRuntimeData runtimeData)
    {
        currentMasterData = masterData;
        currentRuntimeData = runtimeData;

        Refresh();
    }

    public void Refresh()
    {
        ApplyCostLabels();

        if (currentMasterData == null)
        {
            Clear();
            return;
        }

        int baseHP = Mathf.Max(1, currentMasterData.MaxHP);
        int baseCost = Mathf.Max(0, currentMasterData.MaxCost);
        int baseRecovery = Mathf.Max(0, currentMasterData.CostRecovery);
        int baseMove = Mathf.Max(0, currentMasterData.MoveValue);

        int effectiveHP = BattleEquipmentEffectService.GetEffectiveMaxHP(currentRuntimeData, currentMasterData);
        int effectiveCost = BattleEquipmentEffectService.GetEffectiveMaxCost(currentRuntimeData, currentMasterData);
        int effectiveRecovery = BattleEquipmentEffectService.GetEffectiveCostRecovery(currentRuntimeData, currentMasterData);
        int effectiveMove = BattleEquipmentEffectService.GetEffectiveMoveValue(currentRuntimeData, currentMasterData);

        if (hpValueText != null)
            hpValueText.text = FormatStatValue(baseHP, effectiveHP);

        if (costValueText != null)
            costValueText.text = FormatStatValue(baseCost, effectiveCost);

        if (recoveryValueText != null)
            recoveryValueText.text = FormatStatValue(baseRecovery, effectiveRecovery);

        if (moveValueText != null)
            moveValueText.text = FormatStatValue(baseMove, effectiveMove);

        RefreshCharacterMark();

        if (storyText != null)
            storyText.text = FormatIntroduction(currentMasterData.Introduction);
    }

    public void Clear()
    {
        currentMasterData = null;
        currentRuntimeData = null;

        ApplyCostLabels();

        if (hpValueText != null)
            hpValueText.text = "";

        if (costValueText != null)
            costValueText.text = "";

        if (recoveryValueText != null)
            recoveryValueText.text = "";

        if (moveValueText != null)
            moveValueText.text = "";

        ClearCharacterMark();

        if (storyText != null)
            storyText.text = "";
    }

    private void ApplyCostLabels()
    {
        if (costLabelText != null)
            costLabelText.text = costLabel;

        if (recoveryLabelText != null)
            recoveryLabelText.text = recoveryLabel;
    }

    private string FormatStatValue(int baseValue, int effectiveValue)
    {
        if (!showModifiedStatDelta || baseValue == effectiveValue)
            return effectiveValue.ToString();

        int delta = effectiveValue - baseValue;
        string sign = delta > 0 ? "+" : "";
        string color = delta > 0 ? statIncreaseColor : statDecreaseColor;

        if (string.IsNullOrWhiteSpace(color))
            return effectiveValue + " (" + sign + delta + ")";

        return effectiveValue + " <color=" + color + ">(" + sign + delta + ")</color>";
    }

    private void RefreshCharacterMark()
    {
        AutoBindCharacterMarkImageIfNeeded();

        if (characterMarkImage == null)
            return;

        Sprite markSprite = GetCharacterMarkSprite();
        bool hasMark = markSprite != null;

        characterMarkImage.sprite = markSprite;
        characterMarkImage.enabled = hasMark || !hideMarkWhenMissing;
        characterMarkImage.gameObject.SetActive(hasMark || !hideMarkWhenMissing);
    }

    private void ClearCharacterMark()
    {
        if (characterMarkImage == null)
            return;

        characterMarkImage.sprite = null;

        if (hideMarkWhenMissing)
        {
            characterMarkImage.enabled = false;
            characterMarkImage.gameObject.SetActive(false);
        }
    }

    private Sprite GetCharacterMarkSprite()
    {
        if (currentMasterData == null)
            return null;

        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase.TryGetMark(currentMasterData.CharacterId, out Sprite mark))
            return mark;

        return null;
    }

    private void AutoBindCharacterMarkImageIfNeeded()
    {
        if (!autoBindCharacterMarkImage)
            return;

        if (characterMarkImage != null)
            return;

        Transform markTransform = FindChildByName(transform, "CharacterMark");

        if (markTransform == null)
            markTransform = FindChildByName(transform, "Mark");

        if (markTransform == null)
            markTransform = FindChildByName(transform, "MarkImage");

        if (markTransform == null)
            return;

        characterMarkImage = markTransform.GetComponent<Image>();

        if (characterMarkImage == null)
            characterMarkImage = markTransform.GetComponentInChildren<Image>(true);
    }

    private Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildByName(root.GetChild(i), targetName);

            if (result != null)
                return result;
        }

        return null;
    }

    private string FormatIntroduction(string introduction)
    {
        if (string.IsNullOrWhiteSpace(introduction))
            return "";

        string formattedIntroduction = introduction
            .Replace("\\r\\n", "\n")
            .Replace("\\n", "\n")
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        string[] lines = formattedIntroduction.Split('\n');

        if (lines.Length == 0)
            return "";

        lines[0] = $"<color=#4E66DF>{lines[0]}</color>";

        return string.Join("\n", lines);
    }
}
