using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class CharacterStatTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum StatType
    {
        Auto,
        HP,
        Cost,
        CostRecovery,
        Karma
    }

    [Header("Target")]
    [SerializeField] private StatType statType = StatType.Auto;
    [SerializeField] private CharacterInfoPanel characterInfoPanel;

    [Header("Text Override")]
    [SerializeField] private string customName;

    [Header("Value Color")]
    [SerializeField] private string runeIncreaseColor = "#4E66DF";
    [SerializeField] private string runeDecreaseColor = "#D94B4B";

    [Header("Hover Scale")]
    [Tooltip("생명력, 카르마, 마나, 마나재생량 아이콘에 마우스를 올렸을 때 적용할 크기 배율입니다.")]
    [Min(1f)]
    [SerializeField] private float hoverScaleMultiplier = 1.1f;

    private bool isPointerInside;
    private RectTransform hoverScaleTarget;
    private Vector3 originalHoverScale;
    private bool scaleInitialized;

    private void Awake()
    {
        AutoBindIfNeeded();
        InitializeScale();
    }

    private void OnEnable()
    {
        InitializeScale();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoBindIfNeeded();
    }
#endif

    private void OnDisable()
    {
        if (isPointerInside && characterInfoPanel != null)
            characterInfoPanel.HideStatTooltipInStory(this);

        ApplyHoverScale(false);
        isPointerInside = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHoverState(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHoverState(false);
    }

    private void SetHoverState(bool hovered)
    {
        if (isPointerInside == hovered)
            return;

        isPointerInside = hovered;
        ApplyHoverScale(hovered);

        if (hovered)
        {
            ShowInStory();
        }
        else if (characterInfoPanel != null)
        {
            characterInfoPanel.HideStatTooltipInStory(this);
        }
    }

    private RectTransform FindHoverScaleTarget()
    {
        Transform directIcon = transform.Find("Icon");
        if (directIcon is RectTransform directRect)
            return directRect;

        RectTransform[] children = GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            RectTransform child = children[i];
            if (child != null && child != transform &&
                string.Equals(child.name, "Icon", System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return transform as RectTransform;
    }

    private void InitializeScale()
    {
        if (hoverScaleTarget == null)
            hoverScaleTarget = FindHoverScaleTarget();

        if (hoverScaleTarget == null)
        {
            scaleInitialized = false;
            return;
        }

        originalHoverScale = hoverScaleTarget.localScale;
        scaleInitialized = true;
    }

    private void ApplyHoverScale(bool hovered)
    {
        if (!scaleInitialized || hoverScaleTarget == null)
            InitializeScale();

        if (hoverScaleTarget == null)
            return;

        if (!IsHoverScaleEnabled())
        {
            hoverScaleTarget.localScale = originalHoverScale;
            return;
        }

        hoverScaleTarget.localScale = hovered
            ? originalHoverScale * hoverScaleMultiplier
            : originalHoverScale;
    }

    private bool IsHoverScaleEnabled()
    {
        StatType resolvedStatType = GetResolvedStatType();

        return resolvedStatType == StatType.HP ||
               resolvedStatType == StatType.Cost ||
               resolvedStatType == StatType.CostRecovery ||
               resolvedStatType == StatType.Karma;
    }

    private void ShowInStory()
    {
        AutoBindIfNeeded();

        if (characterInfoPanel == null)
            return;

        CharacterMasterData masterData = characterInfoPanel.CurrentMasterData;
        CharacterRuntimeData runtimeData = characterInfoPanel.CurrentRuntimeData;

        if (masterData == null)
            return;

        StatType resolvedStatType = GetResolvedStatType();
        int baseValue = GetBaseValue(masterData, resolvedStatType);
        int effectiveValue = GetEffectiveValue(runtimeData, masterData, resolvedStatType);
        int runeBonus = effectiveValue - baseValue;
        string valueLine = resolvedStatType == StatType.Karma
            ? "최대보유량 " + effectiveValue
            : FormatValueLine(baseValue, runeBonus);

        characterInfoPanel.ShowStatTooltipInStory(
            this,
            GetStatName(resolvedStatType),
            GetStatDescription(resolvedStatType),
            valueLine);
    }

    private int GetBaseValue(CharacterMasterData masterData, StatType resolvedStatType)
    {
        if (masterData == null)
            return 0;

        switch (resolvedStatType)
        {
            case StatType.HP:
                return Mathf.Max(1, masterData.MaxHP);
            case StatType.Cost:
                return Mathf.Max(0, masterData.MaxCost);
            case StatType.CostRecovery:
                return Mathf.Max(0, masterData.CostRecovery);
            case StatType.Karma:
                return Mathf.Max(0, masterData.MaxResource);
            default:
                return 0;
        }
    }

    private int GetEffectiveValue(CharacterRuntimeData runtimeData, CharacterMasterData masterData, StatType resolvedStatType)
    {
        switch (resolvedStatType)
        {
            case StatType.HP:
                return BattleEquipmentEffectService.GetEffectiveMaxHP(runtimeData, masterData);
            case StatType.Cost:
                return BattleEquipmentEffectService.GetEffectiveMaxCost(runtimeData, masterData);
            case StatType.CostRecovery:
                return BattleEquipmentEffectService.GetEffectiveCostRecovery(runtimeData, masterData);
            case StatType.Karma:
                return GetBaseValue(masterData, resolvedStatType);
            default:
                return GetBaseValue(masterData, resolvedStatType);
        }
    }

    private string GetStatName(StatType resolvedStatType)
    {
        if (!string.IsNullOrWhiteSpace(customName))
            return customName;

        if (characterInfoPanel != null)
        {
            string panelTitle = characterInfoPanel.GetStatTooltipTitle(resolvedStatType);
            if (!string.IsNullOrWhiteSpace(panelTitle))
                return panelTitle;
        }

        switch (resolvedStatType)
        {
            case StatType.HP:
                return "생명력";
            case StatType.Cost:
                return "마나";
            case StatType.CostRecovery:
                return "마나재생량";
            case StatType.Karma:
                return "카르마";
            default:
                return "정보";
        }
    }

    private string GetStatDescription(StatType resolvedStatType)
    {
        if (characterInfoPanel != null)
        {
            string panelDescription = characterInfoPanel.GetStatTooltipDescription(resolvedStatType);
            if (!string.IsNullOrWhiteSpace(panelDescription))
                return panelDescription;
        }

        switch (resolvedStatType)
        {
            case StatType.HP:
                return "캐릭터의 생명력이다.\n생명력이 0이 되면 전투불능 상태가 된다.";
            case StatType.Cost:
                return "기억을 사용할 때 소모하는 자원이다.\n현재 마나가 부족하면 기억을 사용할 수 없다.";
            case StatType.CostRecovery:
                return "턴이 시작될 때 회복되는 마나 수치이다.\n회복량이 높을수록 한 턴에 사용할 수 있는 기억 선택지가 늘어난다.";
            case StatType.Karma:
                int maxKarma = characterInfoPanel != null && characterInfoPanel.CurrentMasterData != null
                    ? Mathf.Max(0, characterInfoPanel.CurrentMasterData.MaxResource)
                    : 0;
                return string.Format("발현기억에 사용하는 자원이다.\n최대 보유량은 {0}이다.", maxKarma);
            default:
                return "";
        }
    }

    private StatType GetResolvedStatType()
    {
        if (statType != StatType.Auto)
            return statType;

        Transform current = transform;

        while (current != null)
        {
            string objectName = current.name;

            if (NameContains(objectName, "HP") ||
                NameContains(objectName, "Health") ||
                NameContains(objectName, "체력"))
                return StatType.HP;

            if (NameContains(objectName, "Recovery") ||
                NameContains(objectName, "Recover") ||
                NameContains(objectName, "CostRecovery") ||
                NameContains(objectName, "회복"))
                return StatType.CostRecovery;

            if (NameContains(objectName, "Karma") ||
                NameContains(objectName, "카르마"))
                return StatType.Karma;

            if (NameContains(objectName, "Stamina") ||
                NameContains(objectName, "Cost") ||
                NameContains(objectName, "코스트"))
                return StatType.Cost;

            current = current.parent;
        }

        return StatType.HP;
    }

    private bool NameContains(string source, string keyword)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(keyword))
            return false;

        return source.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private string FormatValueLine(int baseValue, int runeBonus)
    {
        string baseLine = string.Format("기본 수치 {0}", baseValue);

        if (runeBonus == 0)
            return baseLine;

        string sign = runeBonus > 0 ? "+" : "";
        string runeColor = runeBonus > 0 ? runeIncreaseColor : runeDecreaseColor;
        string runeText = sign + runeBonus;

        if (!string.IsNullOrWhiteSpace(runeColor))
            runeText = "<color=" + runeColor + ">" + runeText + "</color>";

        string runeLine = "파편 보정 " + runeText;

        return baseLine + "\n" + runeLine;
    }


    private string NormalizeTooltipText(string text)
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

    private void AutoBindIfNeeded()
    {
        if (characterInfoPanel == null)
            characterInfoPanel = GetComponentInParent<CharacterInfoPanel>();

        if (characterInfoPanel == null)
            characterInfoPanel = FindFirstObjectByTypeSafe<CharacterInfoPanel>();
    }

    private static T FindFirstObjectByTypeSafe<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<T>();
#else
        return FindObjectOfType<T>();
#endif
    }
}
