using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

    [Header("Stats Area Hover Tooltip - Inspector References")]
    [SerializeField] private RectTransform hpTooltipIcon;
    [SerializeField] private RectTransform karmaTooltipIcon;
    [SerializeField] private RectTransform costTooltipIcon;
    [SerializeField] private RectTransform recoveryTooltipIcon;
    [SerializeField] private GameObject statsTooltipPanel;
    [SerializeField] private TMP_Text statsTooltipText;


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
        EnsureStatsAreaNameTooltip();
    }

    private void OnEnable()
    {
        EnsureStatsAreaNameTooltip();
    }

    private void EnsureStatsAreaNameTooltip()
    {
        // 이 컴포넌트가 Stats_Area 자신에 붙어 있는 경우도 지원합니다.
        Transform stats = transform.name == "Stats_Area" ? transform : transform.Find("Stats_Area");
        if (stats == null) return;

        CharacterStatsAreaNameTooltip tooltip = stats.GetComponent<CharacterStatsAreaNameTooltip>();
        if (tooltip == null)
            tooltip = stats.gameObject.AddComponent<CharacterStatsAreaNameTooltip>();

        // 연결한 참조는 비활성화 상태의 Tooltip에도 적용됩니다.
        tooltip.Configure(hpTooltipIcon, karmaTooltipIcon, costTooltipIcon,
            recoveryTooltipIcon, statsTooltipPanel, statsTooltipText);
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
                GameLocalization.Get(LocalizationKeys.CharacterSetting.KarmaAcquisitionTitle)),
            NormalizeEditableText(GameDataLocalization.CharacterRegeneration(currentMasterData)),
            "");
    }

    public string GetStatTooltipTitle(CharacterStatTooltipTarget.StatType statType)
    {
        switch (statType)
        {
            case CharacterStatTooltipTarget.StatType.HP:
                return GameLocalization.Get("common.hp");
            case CharacterStatTooltipTarget.StatType.Cost:
                return GameLocalization.Get("common.cost");
            case CharacterStatTooltipTarget.StatType.CostRecovery:
                return GameLocalization.Get("common.recovery");
            case CharacterStatTooltipTarget.StatType.Karma:
                return GameLocalization.Get("resource.karma");
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
                    "lobby.stat.hp.description");
            case CharacterStatTooltipTarget.StatType.Cost:
                return GameLocalization.Get(
                    "lobby.stat.cost.description");
            case CharacterStatTooltipTarget.StatType.CostRecovery:
                return GameLocalization.Get(
                    "lobby.stat.recovery.description");
            case CharacterStatTooltipTarget.StatType.Karma:
                return GameLocalization.Get(
                    LocalizationKeys.CharacterSetting.KarmaDescription);
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
            hpLabelText.text = GameLocalization.Get("common.hp");

        if (costLabelText != null)
            costLabelText.text = GameLocalization.Get("common.cost");

        if (recoveryLabelText != null)
            recoveryLabelText.text = GameLocalization.Get("common.recovery");
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
        string title = string.IsNullOrWhiteSpace(statName) ? GameLocalization.Get("common.info") : statName.Trim();
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


/// <summary>
/// Stats_Area 아래의 네 아이콘에 포인터를 올리면 공용 Tooltip/Text에 이름을 표시합니다.
/// 기존 CharacterStatTooltipTarget의 설명 표시 이벤트와 독립적으로 작동합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CharacterStatsAreaNameTooltip : MonoBehaviour
{
    [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.05f;

    private GameObject tooltipObject;
    private TMP_Text tooltipText;
    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;
    private string activeKey;
    private RectTransform hoveredIcon;
    private RectTransform hpIconReference;
    private RectTransform karmaIconReference;
    private RectTransform costIconReference;
    private RectTransform recoveryIconReference;
    private GameObject panelReference;
    private TMP_Text textReference;

    public void Configure(RectTransform hp, RectTransform karma, RectTransform cost,
        RectTransform recovery, GameObject panel, TMP_Text text)
    {
        hpIconReference = hp;
        karmaIconReference = karma;
        costIconReference = cost;
        recoveryIconReference = recovery;
        panelReference = panel;
        textReference = text;
        Bind();
        ResetTooltip();
    }

    private void Awake()
    {
        Bind();
    }

    private void OnEnable()
    {
        Bind();
        ResetTooltip();
    }

    private void OnDisable()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        ResetTooltip();
    }

    private void Update()
    {
        // 설정 언어가 호버 중 변경되는 경우에도 현재 언어로 표시합니다.
        if (!string.IsNullOrEmpty(activeKey) && tooltipText != null)
        {
            string localized = GameLocalization.Get(activeKey);
            if (tooltipText.text != localized)
                tooltipText.text = localized;
        }
    }

    private void Bind()
    {
        Transform tooltip = panelReference != null ? panelReference.transform : transform.Find("Tooltip");
        Transform text = textReference != null ? textReference.transform :
            (tooltip != null ? tooltip.Find("Text") : null);
        if (tooltip == null || text == null)
            return;

        tooltipObject = tooltip.gameObject;
        tooltipText = text.GetComponent<TMP_Text>();
        if (tooltipText == null)
            return;

        canvasGroup = tooltip.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = tooltip.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        BindIcon(hpIconReference, "HP", "common.hp");
        BindIcon(karmaIconReference, "Karma", "resource.karma");
        BindIcon(costIconReference, "Cost", "common.mana");
        BindIcon(recoveryIconReference, "Recovery", "common.recovery");
    }

    private void BindIcon(RectTransform assignedIcon, string objectName, string localizationKey)
    {
        Transform icon = assignedIcon;
        if (icon == null)
        {
            Transform stat = transform.Find(objectName);
            if (stat == null) return;
            icon = stat.Find("Icon");
            if (icon == null) icon = stat;
        }

        CharacterStatsAreaNameHoverTarget target = icon.GetComponent<CharacterStatsAreaNameHoverTarget>();
        if (target == null)
            target = icon.gameObject.AddComponent<CharacterStatsAreaNameHoverTarget>();
        target.Initialize(this, localizationKey);

        Graphic raycastGraphic = icon.GetComponent<Graphic>();
        if (raycastGraphic != null)
            raycastGraphic.raycastTarget = true;
    }

    public void PointerEnter(string localizationKey, RectTransform iconRect)
    {
        if (tooltipObject == null || tooltipText == null || !isActiveAndEnabled)
            return;
        activeKey = localizationKey;
        hoveredIcon = iconRect;
        PositionBelowIcon();
        tooltipText.text = GameLocalization.Get(localizationKey);
        FadeTo(1f, fadeInDuration);
    }

    public void PointerExit(string localizationKey)
    {
        if (activeKey != localizationKey)
            return;
        activeKey = null;
        hoveredIcon = null;
        FadeTo(0f, fadeOutDuration);
    }

    private void PositionBelowIcon()
    {
        if (hoveredIcon == null || tooltipObject == null)
            return;

        RectTransform tooltipRect = tooltipObject.transform as RectTransform;
        RectTransform parentRect = tooltipRect != null ? tooltipRect.parent as RectTransform : null;
        if (tooltipRect == null || parentRect == null)
            return;

        // 아이콘 정중앙을 기준으로 툴팁의 정중앙 위치를 지정합니다.
        // 아이콘과 툴팁의 부모가 달라도 동일한 좌표계에서 계산합니다.
        Vector3 iconCenterWorld = hoveredIcon.TransformPoint(hoveredIcon.rect.center);
        Vector3 iconCenterLocal = parentRect.InverseTransformPoint(iconCenterWorld);

        tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRect.anchoredPosition = new Vector2(
            iconCenterLocal.x - parentRect.rect.center.x + (tooltipRect.pivot.x - 0.5f) * tooltipRect.rect.width,
            iconCenterLocal.y - parentRect.rect.center.y + (tooltipRect.pivot.y - 0.5f) * tooltipRect.rect.height + 30f);
    }

    private void FadeTo(float target, float duration)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = null;
        if (tooltipObject == null || canvasGroup == null)
            return;
        if (target > 0f)
            tooltipObject.SetActive(true);
        if (duration <= 0f || !isActiveAndEnabled)
        {
            canvasGroup.alpha = target;
            if (target <= 0f)
                tooltipObject.SetActive(false);
            return;
        }
        fadeRoutine = StartCoroutine(AnimateFade(target, duration));
    }

    private IEnumerator AnimateFade(float target, float duration)
    {
        float from = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        canvasGroup.alpha = target;
        if (target <= 0f)
            tooltipObject.SetActive(false);
        fadeRoutine = null;
    }

    private void ResetTooltip()
    {
        activeKey = null;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        if (tooltipObject != null)
            tooltipObject.SetActive(false);
    }
}

[DisallowMultipleComponent]
public sealed class CharacterStatsAreaNameHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CharacterStatsAreaNameTooltip owner;
    private string localizationKey;

    public void Initialize(CharacterStatsAreaNameTooltip tooltip, string key)
    {
        owner = tooltip;
        localizationKey = key;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null)
            owner.PointerEnter(localizationKey, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null)
            owner.PointerExit(localizationKey);
    }

    private void OnDisable()
    {
        if (owner != null)
            owner.PointerExit(localizationKey);
    }
}
