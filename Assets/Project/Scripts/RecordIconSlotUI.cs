using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 도감에 표시되는 개별 아이콘 슬롯입니다.
/// 테두리 이미지는 기본, 마우스 오버, 선택 상태에 따라 색상으로 구분하며,
/// 선택된 슬롯은 자식 Select 오브젝트를 활성화합니다.
/// </summary>
public class RecordIconSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image borderImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectObject;

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
        ResolveSelectObject();
        RefreshVisualState();
    }

    public void Initialize(Sprite icon, string itemName, Action<RecordIconSlotUI, string> onClicked)
    {
        displayName = itemName ?? string.Empty;
        clickedCallback = onClicked;
        isSelected = false;
        isPointerInside = false;

        ResolveSelectObject();

        if (iconImage != null)
        {
            bool hasIcon = icon != null;
            iconImage.sprite = icon;
            iconImage.enabled = hasIcon;
            iconImage.preserveAspect = true;
            iconImage.gameObject.SetActive(hasIcon);
        }

        RefreshVisualState();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        RefreshVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        RefreshVisualState();
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
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        RefreshBorderColor();
        RefreshSelectObject();
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

    private void RefreshSelectObject()
    {
        if (selectObject == null)
            return;

        selectObject.SetActive(isSelected);
    }

    private void ResolveSelectObject()
    {
        if (selectObject != null)
            return;

        selectObject = FindChildByName("Select");
    }

    private GameObject FindChildByName(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
            return null;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == null || child == transform)
                continue;

            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }
}
