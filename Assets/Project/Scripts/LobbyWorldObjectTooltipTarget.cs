using UnityEngine;

/// <summary>
/// 로비의 월드 오브젝트에 마우스를 올렸을 때 공용 툴팁을 표시합니다.
/// Anchor, relic_stone, Researcher, statue에 각각 부착합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class LobbyWorldObjectTooltipTarget : MonoBehaviour
{
    [Header("표시 문구")]
    [Tooltip("마우스를 올렸을 때 툴팁에 표시할 문구입니다.")]
    [SerializeField] private string tooltipText;

    private bool isMouseOver;

    private void OnMouseEnter()
    {
        isMouseOver = true;
        ShowTooltip();
    }

    private void OnMouseOver()
    {
        // 패널을 닫은 직후 마우스가 움직이지 않아 OnMouseEnter가 다시 오지 않는 경우를 보완합니다.
        if (!isMouseOver)
        {
            isMouseOver = true;
        }

        ShowTooltip();
    }

    private void OnMouseExit()
    {
        isMouseOver = false;
        HideTooltip();
    }

    private void OnDisable()
    {
        isMouseOver = false;
        HideTooltip();
    }

    private void OnDestroy()
    {
        HideTooltip();
    }

    private void ShowTooltip()
    {
        LobbyMouseOverTooltipUI tooltip = LobbyMouseOverTooltipUI.Instance;
        if (tooltip == null)
        {
            return;
        }

        tooltip.Show(this, tooltipText);
    }

    private void HideTooltip()
    {
        LobbyMouseOverTooltipUI tooltip = LobbyMouseOverTooltipUI.Instance;
        if (tooltip == null)
        {
            return;
        }

        tooltip.Hide(this);
    }
}
