using System.Collections;
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
    [SerializeField] private Image characterMark2Image;
    [SerializeField] private bool autoBindCharacterMarkImage = true;
    [SerializeField] private bool hideMarkWhenMissing = true;

    [Header("Story")]
    [SerializeField] private TMP_Text storyText;
    [SerializeField] private string storyTooltipTitleColor = "#4E66DF";

    [Header("Story Tooltip Timing")]
    [SerializeField, Min(0f)] private float storyTooltipRestoreDelay = 0.15f;

    private CharacterMasterData currentMasterData;
    private CharacterRuntimeData currentRuntimeData;
    private string currentStoryText = "";
    private Component temporaryStoryOwner;
    private Coroutine restoreStoryCoroutine;

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
        temporaryStoryOwner = null;
        CancelRestoreStoryCoroutine();

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
        RefreshStoryTextCache();

        if (temporaryStoryOwner == null)
            ApplyStoryText(currentStoryText);
    }

    public void Clear()
    {
        currentMasterData = null;
        currentRuntimeData = null;
        currentStoryText = "";
        temporaryStoryOwner = null;
        CancelRestoreStoryCoroutine();

        ApplyCostLabels();

        // 캐릭터 정보가 없을 때도 스탯 영역이 빈칸으로 남지 않도록
        // 모든 수치를 0으로 표시한다.
        if (hpValueText != null)
            hpValueText.text = "0";

        if (costValueText != null)
            costValueText.text = "0";

        if (recoveryValueText != null)
            recoveryValueText.text = "0";

        if (moveValueText != null)
            moveValueText.text = "0";

        ClearCharacterMark();
        ApplyStoryText("설명 없음");
    }

    public void ShowStatTooltipInStory(Component owner, string statName, string description, string valueLine)
    {
        if (storyText == null)
            return;

        CancelRestoreStoryCoroutine();
        temporaryStoryOwner = owner;
        ApplyStoryText(FormatStoryTooltip(statName, description, valueLine));
    }

    public void HideStatTooltipInStory(Component owner)
    {
        if (temporaryStoryOwner != null && owner != null && temporaryStoryOwner != owner)
            return;

        CancelRestoreStoryCoroutine();

        if (storyTooltipRestoreDelay <= 0f || !gameObject.activeInHierarchy)
        {
            RestoreStoryText(owner);
            return;
        }

        restoreStoryCoroutine = StartCoroutine(RestoreStoryTextAfterDelay(owner));
    }

    private IEnumerator RestoreStoryTextAfterDelay(Component owner)
    {
        yield return new WaitForSecondsRealtime(storyTooltipRestoreDelay);

        restoreStoryCoroutine = null;
        RestoreStoryText(owner);
    }

    private void RestoreStoryText(Component owner)
    {
        if (temporaryStoryOwner != null && owner != null && temporaryStoryOwner != owner)
            return;

        temporaryStoryOwner = null;
        ApplyStoryText(currentStoryText);
    }

    private void CancelRestoreStoryCoroutine()
    {
        if (restoreStoryCoroutine == null)
            return;

        StopCoroutine(restoreStoryCoroutine);
        restoreStoryCoroutine = null;
    }

    private void RefreshStoryTextCache()
    {
        currentStoryText = currentMasterData != null
            ? FormatIntroduction(currentMasterData.Introduction)
            : "";
    }

    private void ApplyStoryText(string text)
    {
        if (storyText != null)
            storyText.text = text ?? "";
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

    private string FormatStoryTooltip(string statName, string description, string valueLine)
    {
        string title = string.IsNullOrWhiteSpace(statName) ? "정보" : statName.Trim();
        string body = string.IsNullOrWhiteSpace(description) ? "" : description.Trim();
        string value = string.IsNullOrWhiteSpace(valueLine) ? "" : valueLine.Trim();

        if (!string.IsNullOrWhiteSpace(storyTooltipTitleColor))
            title = "<color=" + storyTooltipTitleColor + ">" + title + "</color>";

        if (!string.IsNullOrWhiteSpace(body) && !string.IsNullOrWhiteSpace(value))
            return title + "\n\n" + body + "\n\n" + value;

        if (!string.IsNullOrWhiteSpace(body))
            return title + "\n\n" + body;

        if (!string.IsNullOrWhiteSpace(value))
            return title + "\n\n" + value;

        return title;
    }

    private void RefreshCharacterMark()
    {
        AutoBindCharacterMarkImageIfNeeded();

        ApplyCharacterMarkImage(characterMarkImage, GetCharacterMarkSprite());
        ApplyCharacterMarkImage(characterMark2Image, GetCharacterMark2Sprite());
    }

    private void ClearCharacterMark()
    {
        ClearCharacterMarkImage(characterMarkImage);
        ClearCharacterMarkImage(characterMark2Image);
    }

    private void ApplyCharacterMarkImage(Image targetImage, Sprite sprite)
    {
        if (targetImage == null)
            return;

        bool hasSprite = sprite != null;

        targetImage.sprite = sprite;
        targetImage.enabled = hasSprite || !hideMarkWhenMissing;
        targetImage.gameObject.SetActive(hasSprite || !hideMarkWhenMissing);
    }

    private void ClearCharacterMarkImage(Image targetImage)
    {
        if (targetImage == null)
            return;

        targetImage.sprite = null;

        if (hideMarkWhenMissing)
        {
            targetImage.enabled = false;
            targetImage.gameObject.SetActive(false);
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

    private Sprite GetCharacterMark2Sprite()
    {
        if (currentMasterData == null)
            return null;

        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase.TryGetMark2(currentMasterData.CharacterId, out Sprite mark2))
            return mark2;

        return null;
    }

    private void AutoBindCharacterMarkImageIfNeeded()
    {
        if (!autoBindCharacterMarkImage)
            return;

        if (characterMarkImage == null)
        {
            characterMarkImage = FindImageByNames(
                "Character_mark",
                "CharacterMark",
                "Mark",
                "MarkImage");
        }

        if (characterMark2Image == null)
        {
            characterMark2Image = FindImageByNames(
                "Character_mark2",
                "CharacterMark2",
                "Mark2",
                "Mark2Image");
        }
    }

    private Image FindImageByNames(params string[] names)
    {
        if (names == null)
            return null;

        for (int i = 0; i < names.Length; i++)
        {
            Transform target = FindChildByName(transform, names[i]);

            if (target == null)
                continue;

            Image image = target.GetComponent<Image>();

            if (image == null)
                image = target.GetComponentInChildren<Image>(true);

            if (image != null)
                return image;
        }

        return null;
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
