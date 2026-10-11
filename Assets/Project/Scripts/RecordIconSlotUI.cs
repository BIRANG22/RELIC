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
    [SerializeField] private Image backImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectObject;
    [SerializeField] private GameObject unconfirmedObject;

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

    private void Awake()
    {
        EnsureReferences();
        SetSelectVisible(false);
    }

    public void Initialize(Sprite icon, string itemName, Action<RecordIconSlotUI, string> onClicked, bool showIcon = true)
    {
        EnsureReferences();

        displayName = itemName ?? string.Empty;
        clickedCallback = onClicked;
        isSelected = false;
        isPointerInside = false;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.enabled = showIcon && icon != null;

            if (iconImage.gameObject != null)
                iconImage.gameObject.SetActive(showIcon && icon != null);
        }

        if (unconfirmedObject != null)
            unconfirmedObject.SetActive(!showIcon);

        SetSelectVisible(false);
        RefreshBorderColor();
    }

    public void SetBackColor(Color rarityColor)
    {
        EnsureReferences();
        if (backImage == null)
            return;

        // Preserve the original opacity set on the Back image.
        rarityColor.a = backImage.color.a;
        backImage.color = rarityColor;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        SetSelectVisible(selected);
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
        SetSelectVisible(false);
        RefreshBorderColor();
    }

    private void EnsureReferences()
    {
        if (backImage == null)
        {
            Transform backTransform = transform.Find("Back");
            if (backTransform != null)
                backImage = backTransform.GetComponent<Image>();
        }
        if (borderImage == null)
            borderImage = GetComponentInChildren<Image>(true);

        if (iconImage == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
                iconImage = iconTransform.GetComponent<Image>();
        }

        if (selectObject == null)
        {
            Transform selectTransform = transform.Find("Select");
            if (selectTransform != null)
                selectObject = selectTransform.gameObject;
        }

        if (unconfirmedObject == null)
        {
            Transform unconfirmedTransform = transform.Find("Unconfirmed");
            if (unconfirmedTransform != null)
                unconfirmedObject = unconfirmedTransform.gameObject;
        }
    }

    private void SetSelectVisible(bool visible)
    {
        if (selectObject != null && selectObject.activeSelf != visible)
            selectObject.SetActive(visible);
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
