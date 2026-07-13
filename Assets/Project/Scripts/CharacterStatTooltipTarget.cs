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
        Move
    }

    [Header("Target")]
    [SerializeField] private StatType statType = StatType.Auto;
    [SerializeField] private CharacterInfoPanel characterInfoPanel;

    [Header("Text Override")]
    [SerializeField] private string customName;
    [SerializeField, TextArea] private string customDescription;

    [Header("Value Color")]
    [SerializeField] private string runeIncreaseColor = "#4E66DF";
    [SerializeField] private string runeDecreaseColor = "#D94B4B";

    private bool isPointerInside;

    private void Awake()
    {
        AutoBindIfNeeded();
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

        isPointerInside = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        ShowInStory();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;

        if (characterInfoPanel != null)
            characterInfoPanel.HideStatTooltipInStory(this);
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

        characterInfoPanel.ShowStatTooltipInStory(
            this,
            GetStatName(resolvedStatType),
            GetStatDescription(resolvedStatType),
            FormatValueLine(baseValue, runeBonus));
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
            case StatType.Move:
                // 캐릭터 데이터에는 기본 이동값이 없습니다.
                return 0;
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
            case StatType.Move:
                return BattleEquipmentEffectService.GetEffectiveMoveValue(runtimeData, masterData);
            default:
                return GetBaseValue(masterData, resolvedStatType);
        }
    }

    private string GetStatName(StatType resolvedStatType)
    {
        if (!string.IsNullOrWhiteSpace(customName))
            return customName;

        switch (resolvedStatType)
        {
            case StatType.HP:
                return "체력";
            case StatType.Cost:
                return "코스트";
            case StatType.CostRecovery:
                return "코스트 회복량";
            case StatType.Move:
                return "이동력";
            default:
                return "정보";
        }
    }

    private string GetStatDescription(StatType resolvedStatType)
    {
        if (!string.IsNullOrWhiteSpace(customDescription))
            return customDescription;

        switch (resolvedStatType)
        {
            case StatType.HP:
                return "캐릭터의 생명력입니다.\n체력이 0이 되면 전투불능 상태가 됩니다.";
            case StatType.Cost:
                return "스킬을 사용할 때 소모하는 자원입니다.\n보유 코스트가 높을수록 한 턴에 더 많은 스킬을 사용할 수 있습니다.";
            case StatType.CostRecovery:
                return "턴이 시작될 때 회복되는 코스트 수치입니다.\n회복량이 높을수록 매 턴 사용할 수 있는 스킬 선택지가 늘어납니다.";
            case StatType.Move:
                return "전투 중 이동스킬 사용 시 1칸 이동에 1코스트를 사용합니다.\n이동력이 50 이상일 때는 2칸 이동에 1코스트를 사용합니다.";
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

            if (NameContains(objectName, "Stamina") ||
                NameContains(objectName, "Cost") ||
                NameContains(objectName, "코스트"))
                return StatType.Cost;

            if (NameContains(objectName, "Move") ||
                NameContains(objectName, "MoveValue") ||
                NameContains(objectName, "이동"))
                return StatType.Move;

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
        if (runeBonus == 0)
            return "기본 수치 " + baseValue;

        string sign = runeBonus > 0 ? "+" : "";
        string runeColor = runeBonus > 0 ? runeIncreaseColor : runeDecreaseColor;
        string runeText = sign + runeBonus;

        if (!string.IsNullOrWhiteSpace(runeColor))
            runeText = "<color=" + runeColor + ">" + runeText + "</color>";

        return "기본 수치 " + baseValue + "\n" +
               "룬 보정 " + runeText;
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
