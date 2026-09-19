using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class CharacterInfoPanel : MonoBehaviour
{
    [Header("Value Texts")]
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField, FormerlySerializedAs("staminaValueText")] private TMP_Text costValueText;
    [SerializeField, FormerlySerializedAs("staminaRecoveryValueText")] private TMP_Text recoveryValueText;
    [SerializeField] private TMP_Text karmaValueText;

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

    [Header("Number Change Effect")]
    [Tooltip("스탯 수치가 변할 때 이전 값에서 새 값까지 숫자가 변화하는 시간입니다.")]
    [SerializeField, Min(0f)] private float numberChangeDuration = 0.2f;

    [Header("Story")]
    [SerializeField] private TMP_Text storyText;
    [SerializeField] private string storyTooltipTitleColor = "#4E66DF";

    [Header("Karma Acquisition Text")]
    [SerializeField] private string karmaAcquisitionTitle = "카르마 획득 조건";

    [Header("Stat Tooltip Text")]
    [SerializeField] private string hpTooltipTitle = "생명력";
    [SerializeField] private string costTooltipTitle = "마나";
    [SerializeField] private string recoveryTooltipTitle = "마나재생량";
    [SerializeField] private string karmaTooltipTitle = "카르마";

    [Header("Story Tooltip Timing")]
    [SerializeField, Min(0f)] private float storyTooltipRestoreDelay = 0.15f;

    private CharacterMasterData currentMasterData;
    private CharacterRuntimeData currentRuntimeData;
    private string currentStoryText = "";
    private Component temporaryStoryOwner;
    private Coroutine restoreStoryCoroutine;
    private Coroutine statNumberChangeCoroutine;
    private bool hasDisplayedStatValues;
    private int displayedBaseHP;
    private int displayedEffectiveHP;
    private int displayedBaseCost;
    private int displayedEffectiveCost;
    private int displayedBaseRecovery;
    private int displayedEffectiveRecovery;
    private int displayedKarma;

    public CharacterMasterData CurrentMasterData => currentMasterData;
    public CharacterRuntimeData CurrentRuntimeData => currentRuntimeData;

    private void Awake()
    {
        AutoBindValueTexts();
        AutoBindValueTexts();
        AutoBindStatLabelTexts();
        ApplyCostLabels();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoBindStatLabelTexts();
        ApplyCostLabels();
    }
#endif

    public void SetCharacter(CharacterMasterData masterData, CharacterRuntimeData runtimeData)
    {
        bool characterChanged = currentMasterData != masterData || currentRuntimeData != runtimeData;

        currentMasterData = masterData;
        currentRuntimeData = runtimeData;
        temporaryStoryOwner = null;
        CancelRestoreStoryCoroutine();

        if (characterChanged)
        {
            StopStatNumberChangeCoroutine();
            hasDisplayedStatValues = false;
        }

        Refresh();
    }

    public void Refresh()
    {
        AutoBindValueTexts();
        ApplyCostLabels();

        if (currentMasterData == null)
        {
            Clear();
            return;
        }

        int baseHP = Mathf.Max(1, currentMasterData.MaxHP);
        int baseCost = Mathf.Max(0, currentMasterData.MaxCost);
        int baseRecovery = Mathf.Max(0, currentMasterData.CostRecovery);
        int maxKarma = Mathf.Max(0, currentMasterData.MaxResource);

        int effectiveHP = BattleEquipmentEffectService.GetEffectiveMaxHP(currentRuntimeData, currentMasterData);
        int effectiveCost = BattleEquipmentEffectService.GetEffectiveMaxCost(currentRuntimeData, currentMasterData);
        int effectiveRecovery = BattleEquipmentEffectService.GetEffectiveCostRecovery(currentRuntimeData, currentMasterData);

        AutoBindKarmaValueText();
        RefreshStatNumberValues(
            baseHP,
            effectiveHP,
            baseCost,
            effectiveCost,
            baseRecovery,
            effectiveRecovery,
            maxKarma);

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
        StopStatNumberChangeCoroutine();
        hasDisplayedStatValues = false;

        ApplyCostLabels();

        // 캐릭터 정보가 없을 때도 스탯 영역이 빈칸으로 남지 않도록
        // 모든 수치를 0으로 표시한다.
        if (hpValueText != null)
            hpValueText.text = "0";

        if (costValueText != null)
            costValueText.text = "0";

        if (recoveryValueText != null)
            recoveryValueText.text = "0";

        AutoBindKarmaValueText();
        if (karmaValueText != null)
            karmaValueText.text = "0";

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

    private void RefreshStatNumberValues(
        int baseHP,
        int effectiveHP,
        int baseCost,
        int effectiveCost,
        int baseRecovery,
        int effectiveRecovery,
        int maxKarma)
    {
        if (!Application.isPlaying || !hasDisplayedStatValues || numberChangeDuration <= 0f)
        {
            SetStatValuesImmediate(
                baseHP,
                effectiveHP,
                baseCost,
                effectiveCost,
                baseRecovery,
                effectiveRecovery,
                maxKarma);
            return;
        }

        if (displayedBaseHP == baseHP &&
            displayedEffectiveHP == effectiveHP &&
            displayedBaseCost == baseCost &&
            displayedEffectiveCost == effectiveCost &&
            displayedBaseRecovery == baseRecovery &&
            displayedEffectiveRecovery == effectiveRecovery &&
            displayedKarma == maxKarma)
        {
            ApplyDisplayedStatValues();
            return;
        }

        StopStatNumberChangeCoroutine();
        statNumberChangeCoroutine = StartCoroutine(AnimateStatNumberValues(
            baseHP,
            effectiveHP,
            baseCost,
            effectiveCost,
            baseRecovery,
            effectiveRecovery,
            maxKarma));
    }

    private void SetStatValuesImmediate(
        int baseHP,
        int effectiveHP,
        int baseCost,
        int effectiveCost,
        int baseRecovery,
        int effectiveRecovery,
        int maxKarma)
    {
        StopStatNumberChangeCoroutine();

        displayedBaseHP = baseHP;
        displayedEffectiveHP = effectiveHP;
        displayedBaseCost = baseCost;
        displayedEffectiveCost = effectiveCost;
        displayedBaseRecovery = baseRecovery;
        displayedEffectiveRecovery = effectiveRecovery;
        displayedKarma = maxKarma;
        hasDisplayedStatValues = true;

        ApplyDisplayedStatValues();
    }

    private IEnumerator AnimateStatNumberValues(
        int targetBaseHP,
        int targetEffectiveHP,
        int targetBaseCost,
        int targetEffectiveCost,
        int targetBaseRecovery,
        int targetEffectiveRecovery,
        int targetKarma)
    {
        int startBaseHP = displayedBaseHP;
        int startEffectiveHP = displayedEffectiveHP;
        int startBaseCost = displayedBaseCost;
        int startEffectiveCost = displayedEffectiveCost;
        int startBaseRecovery = displayedBaseRecovery;
        int startEffectiveRecovery = displayedEffectiveRecovery;
        int startKarma = displayedKarma;

        float elapsed = 0f;
        while (elapsed < numberChangeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / numberChangeDuration);

            displayedBaseHP = Mathf.RoundToInt(Mathf.Lerp(startBaseHP, targetBaseHP, progress));
            displayedEffectiveHP = Mathf.RoundToInt(Mathf.Lerp(startEffectiveHP, targetEffectiveHP, progress));
            displayedBaseCost = Mathf.RoundToInt(Mathf.Lerp(startBaseCost, targetBaseCost, progress));
            displayedEffectiveCost = Mathf.RoundToInt(Mathf.Lerp(startEffectiveCost, targetEffectiveCost, progress));
            displayedBaseRecovery = Mathf.RoundToInt(Mathf.Lerp(startBaseRecovery, targetBaseRecovery, progress));
            displayedEffectiveRecovery = Mathf.RoundToInt(Mathf.Lerp(startEffectiveRecovery, targetEffectiveRecovery, progress));
            displayedKarma = Mathf.RoundToInt(Mathf.Lerp(startKarma, targetKarma, progress));

            ApplyDisplayedStatValues();
            yield return null;
        }

        displayedBaseHP = targetBaseHP;
        displayedEffectiveHP = targetEffectiveHP;
        displayedBaseCost = targetBaseCost;
        displayedEffectiveCost = targetEffectiveCost;
        displayedBaseRecovery = targetBaseRecovery;
        displayedEffectiveRecovery = targetEffectiveRecovery;
        displayedKarma = targetKarma;

        ApplyDisplayedStatValues();
        statNumberChangeCoroutine = null;
    }

    private void ApplyDisplayedStatValues()
    {
        if (hpValueText != null)
            hpValueText.text = FormatStatValue(displayedBaseHP, displayedEffectiveHP);

        if (costValueText != null)
            costValueText.text = FormatStatValue(displayedBaseCost, displayedEffectiveCost);

        if (recoveryValueText != null)
            recoveryValueText.text = FormatStatValue(displayedBaseRecovery, displayedEffectiveRecovery);

        if (karmaValueText != null)
            karmaValueText.text = Mathf.Max(0, displayedKarma).ToString();
    }

    private void StopStatNumberChangeCoroutine()
    {
        if (statNumberChangeCoroutine == null)
            return;

        StopCoroutine(statNumberChangeCoroutine);
        statNumberChangeCoroutine = null;
    }

    private void RefreshStoryTextCache()
    {
        if (currentMasterData == null)
        {
            currentStoryText = "";
            return;
        }

        currentStoryText = FormatStoryTooltip(
            GameLocalization.Get(
                LocalizationKeys.CharacterSetting.KarmaAcquisitionTitle,
                NormalizeEditableText(karmaAcquisitionTitle)),
            NormalizeEditableText(GameDataLocalization.CharacterRegeneration(currentMasterData)),
            "");
    }

    public string GetStatTooltipTitle(CharacterStatTooltipTarget.StatType statType)
    {
        switch (statType)
        {
            case CharacterStatTooltipTarget.StatType.HP:
                return GameLocalization.Get("common.hp", NormalizeEditableText(hpTooltipTitle));
            case CharacterStatTooltipTarget.StatType.Cost:
                return GameLocalization.Get("common.cost", NormalizeEditableText(costTooltipTitle));
            case CharacterStatTooltipTarget.StatType.CostRecovery:
                return GameLocalization.Get("common.recovery", NormalizeEditableText(recoveryTooltipTitle));
            case CharacterStatTooltipTarget.StatType.Karma:
                return GameLocalization.Get("resource.karma", NormalizeEditableText(karmaTooltipTitle));
            default:
                return string.Empty;
        }
    }

    public string GetStatTooltipDescription(CharacterStatTooltipTarget.StatType statType)
    {
        switch (statType)
        {
            case CharacterStatTooltipTarget.StatType.HP:
                return GameLocalization.Get(
                    "lobby.stat.hp.description",
                    "생명력이 0이 되면 전투불능 상태가 된다.");
            case CharacterStatTooltipTarget.StatType.Cost:
                return GameLocalization.Get(
                    "lobby.stat.cost.description",
                    "보유 마나가 부족하면 행동을 등록할 수 없다.");
            case CharacterStatTooltipTarget.StatType.CostRecovery:
                return GameLocalization.Get(
                    "lobby.stat.recovery.description",
                    "턴이 시작될 때 자동으로 회복되는 마나 수치이다.");
            case CharacterStatTooltipTarget.StatType.Karma:
                return GameLocalization.Get(
                    LocalizationKeys.CharacterSetting.KarmaDescription,
                    "각자의 전투 방식에 따라 축적되며, 기억을 발현하는 힘이 된다.");
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


    private void AutoBindValueTexts()
    {
        if (hpValueText == null)
            hpValueText = FindValueText("HP");
        if (costValueText == null)
            costValueText = FindValueText("Cost");
        if (recoveryValueText == null)
            recoveryValueText = FindValueText("Recovery");
        if (karmaValueText == null)
            karmaValueText = FindValueText("Karma");
    }

    private TMP_Text FindValueText(string rootName)
    {
        Transform root = FindChildByName(transform, rootName);
        if (root == null)
            return null;

        Transform value = FindChildByName(root, "ValueText");
        return value != null ? value.GetComponent<TMP_Text>() : null;
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

    private void AutoBindKarmaValueText()
    {
        if (karmaValueText != null)
            return;

        Transform karmaRoot = FindChildByName(transform, "Karma");
        if (karmaRoot == null)
            return;

        Transform valueTransform = FindChildByName(karmaRoot, "ValueText");
        if (valueTransform != null)
            karmaValueText = valueTransform.GetComponent<TMP_Text>();
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
            return effectiveValue + "(" + sign + delta + ")";

        return effectiveValue + "<color=" + color + ">(" + sign + delta + ")</color>";
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
