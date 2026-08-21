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
    [SerializeField] private TMP_Text hpLabelText;
    [SerializeField, FormerlySerializedAs("staminaLabelText")] private TMP_Text costLabelText;
    [SerializeField, FormerlySerializedAs("staminaRecoveryLabelText")] private TMP_Text recoveryLabelText;
    [SerializeField] private string hpLabel = "생명력";
    [SerializeField] private string costLabel = "마나";
    [SerializeField] private string recoveryLabel = "마나재생량";

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

    [Header("Karma Acquisition Text")]
    [SerializeField] private string karmaAcquisitionTitle = "카르마 획득 조건";

    [Header("Stat Tooltip Text")]
    [SerializeField] private string hpTooltipTitle = "생명력";
    [SerializeField, TextArea(2, 4)] private string hpTooltipDescription = "캐릭터의 생명력입니다.\n생명력이 0이 되면 전투불능 상태가 됩니다.";
    [SerializeField] private string costTooltipTitle = "마나";
    [SerializeField, TextArea(2, 4)] private string costTooltipDescription = "기억을 사용할 때 소모하는 자원입니다.\n현재 마나가 부족하면 기억을 사용할 수 없습니다.";
    [SerializeField] private string recoveryTooltipTitle = "마나재생량";
    [SerializeField, TextArea(2, 4)] private string recoveryTooltipDescription = "턴이 시작될 때 회복되는 마나 수치입니다.";

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
        AutoBindStatLabelTexts();
        ApplyCostLabels();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoBindCharacterMarkImageIfNeeded();
        AutoBindStatLabelTexts();
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
        // 캐릭터 데이터에는 기본 이동값이 없으며, 이동값은 장비/룬 효과로만 계산합니다.
        int baseMove = 0;

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
        if (currentMasterData == null)
        {
            currentStoryText = "";
            return;
        }

        currentStoryText = FormatStoryTooltip(
            NormalizeEditableText(karmaAcquisitionTitle),
            NormalizeEditableText(currentMasterData.Regeneration),
            "");
    }

    public string GetStatTooltipTitle(CharacterStatTooltipTarget.StatType statType)
    {
        switch (statType)
        {
            case CharacterStatTooltipTarget.StatType.HP:
                return NormalizeEditableText(hpTooltipTitle);
            case CharacterStatTooltipTarget.StatType.Cost:
                return NormalizeEditableText(costTooltipTitle);
            case CharacterStatTooltipTarget.StatType.CostRecovery:
                return NormalizeEditableText(recoveryTooltipTitle);
            default:
                return string.Empty;
        }
    }

    public string GetStatTooltipDescription(CharacterStatTooltipTarget.StatType statType)
    {
        switch (statType)
        {
            case CharacterStatTooltipTarget.StatType.HP:
                return NormalizeEditableText(hpTooltipDescription);
            case CharacterStatTooltipTarget.StatType.Cost:
                return NormalizeEditableText(costTooltipDescription);
            case CharacterStatTooltipTarget.StatType.CostRecovery:
                return NormalizeEditableText(recoveryTooltipDescription);
            default:
                return string.Empty;
        }
    }

    private static string NormalizeEditableText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\\r\\n", "\n")
            .Replace("\\n", "\n")
            .Replace("\\r", "")
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");
    }

    private void ApplyStoryText(string text)
    {
        if (storyText != null)
            storyText.text = text ?? "";
    }

    private void ApplyCostLabels()
    {
        AutoBindStatLabelTexts();

        if (hpLabelText != null)
            hpLabelText.text = hpLabel;

        if (costLabelText != null)
            costLabelText.text = costLabel;

        if (recoveryLabelText != null)
            recoveryLabelText.text = recoveryLabel;
    }

    private void AutoBindStatLabelTexts()
    {
        if (hpLabelText == null)
            hpLabelText = FindStatLabelText("HP", hpValueText);

        if (costLabelText == null)
            costLabelText = FindStatLabelText("Cost", costValueText);

        if (recoveryLabelText == null)
            recoveryLabelText = FindStatLabelText("Recovery", recoveryValueText);
    }

    private TMP_Text FindStatLabelText(string rootName, TMP_Text valueText)
    {
        Transform statRoot = FindChildByName(transform, rootName);
        if (statRoot == null)
            return null;

        TMP_Text[] texts = statRoot.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text candidate = texts[i];
            if (candidate != null && candidate != valueText)
                return candidate;
        }

        return null;
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
