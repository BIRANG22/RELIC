using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class CharacterStatTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
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
    [SerializeField] private CharacterStatTooltipUI tooltipUI;

    [Header("Position")]
    [SerializeField] private RectTransform positionAnchor;
    [SerializeField] private bool autoUseChildIconAsAnchor = true;
    [SerializeField] private Vector2 iconBottomRightOffset = new Vector2(16f, -16f);

    [Header("Text Override")]
    [SerializeField] private string customName;
    [SerializeField, TextArea] private string customDescription;

    [Header("Value Color")]
    [SerializeField] private string runeIncreaseColor = "#4E66DF";
    [SerializeField] private string runeDecreaseColor = "#D94B4B";
    [SerializeField] private string runeZeroColor = "#4E66DF";

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        Show(GetTooltipScreenPosition(eventData));
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!isPointerInside)
            return;

        if (tooltipUI != null)
            tooltipUI.SetPosition(GetTooltipScreenPosition(eventData));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;

        if (tooltipUI != null)
            tooltipUI.Hide();
    }

    private void Show(Vector2 screenPosition)
    {
        AutoBindIfNeeded();

        if (tooltipUI == null || characterInfoPanel == null)
            return;

        CharacterMasterData masterData = characterInfoPanel.CurrentMasterData;
        CharacterRuntimeData runtimeData = characterInfoPanel.CurrentRuntimeData;

        if (masterData == null)
            return;

        StatType resolvedStatType = GetResolvedStatType();
        int baseValue = GetBaseValue(masterData, resolvedStatType);
        int effectiveValue = GetEffectiveValue(runtimeData, masterData, resolvedStatType);
        int runeBonus = effectiveValue - baseValue;

        tooltipUI.SetFollowMouse(false);
        tooltipUI.Show(
            GetStatName(resolvedStatType),
            GetStatDescription(resolvedStatType),
            FormatValueLine(baseValue, runeBonus),
            screenPosition);
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
                return Mathf.Max(0, masterData.MoveValue);
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
                return "전투 중 이동할 수 있는 거리입니다.\n이동력이 높을수록 더 먼 위치로 이동할 수 있습니다.";
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
        string sign = runeBonus >= 0 ? "+" : "";
        string runeColor = runeBonus > 0
            ? runeIncreaseColor
            : runeBonus < 0 ? runeDecreaseColor : runeZeroColor;

        string runeText = sign + runeBonus + "(룬)";

        if (!string.IsNullOrWhiteSpace(runeColor))
            runeText = "<color=" + runeColor + ">" + runeText + "</color>";

        return baseValue + "(기본) " + runeText;
    }

    private Vector2 GetTooltipScreenPosition(PointerEventData eventData)
    {
        AutoBindIfNeeded();

        RectTransform anchor = GetPositionAnchor();

        if (anchor == null)
            return eventData != null ? eventData.position : (Vector2)Input.mousePosition;

        Vector3[] corners = new Vector3[4];
        anchor.GetWorldCorners(corners);

        Camera camera = GetCanvasCamera(anchor);
        Vector2 bottomRight = RectTransformUtility.WorldToScreenPoint(camera, corners[3]);
        return bottomRight + iconBottomRightOffset;
    }

    private RectTransform GetPositionAnchor()
    {
        if (positionAnchor != null)
            return positionAnchor;

        if (!autoUseChildIconAsAnchor)
            return transform as RectTransform;

        Transform icon = FindChildByName(transform, "Icon");

        if (icon != null && icon.TryGetComponent(out RectTransform iconRect))
            return iconRect;

        return transform as RectTransform;
    }

    private Camera GetCanvasCamera(RectTransform targetRect)
    {
        if (targetRect == null)
            return null;

        Canvas canvas = targetRect.GetComponentInParent<Canvas>();

        if (canvas == null)
            return null;

        Canvas rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;

        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
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

    private void AutoBindIfNeeded()
    {
        if (characterInfoPanel == null)
            characterInfoPanel = GetComponentInParent<CharacterInfoPanel>();

        if (characterInfoPanel == null)
            characterInfoPanel = FindFirstObjectByTypeSafe<CharacterInfoPanel>();

        if (tooltipUI == null)
            tooltipUI = FindTooltipUI();
    }


    private static T FindFirstObjectByTypeSafe<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<T>();
#else
        return FindObjectOfType<T>();
#endif
    }

    private CharacterStatTooltipUI FindTooltipUI()
    {
        CharacterStatTooltipUI activeTooltip = FindFirstObjectByTypeSafe<CharacterStatTooltipUI>();

        if (activeTooltip != null)
            return activeTooltip;

        CharacterStatTooltipUI[] allTooltips = Resources.FindObjectsOfTypeAll<CharacterStatTooltipUI>();

        for (int i = 0; i < allTooltips.Length; i++)
        {
            CharacterStatTooltipUI candidate = allTooltips[i];

            if (candidate == null)
                continue;

            if (!candidate.gameObject.scene.IsValid())
                continue;

            return candidate;
        }

        return null;
    }
}
