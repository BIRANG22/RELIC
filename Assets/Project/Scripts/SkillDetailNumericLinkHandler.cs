using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Skill_Details 전체 영역을 마우스로 감지하고,
/// 클릭 시 모든 스킬에 공통으로 적용되는 상세 수치 보기 모드를 토글합니다.
/// </summary>
public sealed class SkillDetailNumericLinkHandler : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public const int HoverScalePercent = 120; // 1.2배
    public const string AllLinksHoverId = "__all__";

    public static bool DetailedMode { get; private set; }
    public static event Action<bool> DetailedModeChanged;

    private TMP_Text targetText;
    private Action<string> hoveredLinkChanged;
    private bool isHovered;

    public void Configure(TMP_Text text, Action<string> onHoveredLinkChanged)
    {
        targetText = text != null ? text : GetComponent<TMP_Text>();
        hoveredLinkChanged = onHoveredLinkChanged;

        if (targetText != null)
        {
            targetText.richText = true;
            targetText.raycastTarget = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHovered)
            return;

        isHovered = true;
        hoveredLinkChanged?.Invoke(AllLinksHoverId);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovered)
            return;

        isHovered = false;
        hoveredLinkChanged?.Invoke(string.Empty);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 숫자 글자 자체가 아니라 Skill_Details 전체 영역을 클릭 범위로 사용합니다.
        DetailedMode = !DetailedMode;
        DetailedModeChanged?.Invoke(DetailedMode);
    }
}
