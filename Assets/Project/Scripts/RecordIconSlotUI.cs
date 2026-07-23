using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 도감에 표시되는 개별 아이콘 슬롯입니다.
/// 테두리 이미지를 기본, 마우스 오버, 선택 상태에 따라 색상으로 구분합니다.
/// </summary>
public class RecordIconSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image borderImage;
    [SerializeField] private Image iconImage;

    [Header("Border Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.gray;
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.55f, 1f, 1f);

    private string displayName;
    private bool isSelected;
    private bool isPointerInside;
    private Action<RecordIconSlotUI, string> clickedCallback;

    public string DisplayName => displayName;
    public bool IsSelected => isSelected;

    public void Initialize(Sprite icon, string itemName, Action<RecordIconSlotUI, string> onClicked)
    {
        displayName = itemName ?? string.Empty;
        clickedCallback = onClicked;
        isSelected = false;
        isPointerInside = false;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
        }

        RefreshBorderColor();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshBorderColor();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        RefreshBorderColor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        RefreshBorderColor();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        clickedCallback?.Invoke(this, displayName);
    }

    private void OnDisable()
    {
        isPointerInside = false;
        RefreshBorderColor();
    }

    private void RefreshBorderColor()
    {
        if (borderImage == null)
            return;

        if (isSelected)
        {
            borderImage.color = selectedColor;
            return;
        }

        borderImage.color = isPointerInside ? hoverColor : normalColor;
    }
}
