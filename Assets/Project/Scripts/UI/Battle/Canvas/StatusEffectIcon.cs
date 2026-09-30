using Relic.Gameplay.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 상태효과 아이콘 표시와 툴팁 호버를 처리합니다.
/// 툴팁은 PointerEnter에서 한 번 표시하고 PointerMove에서는 위치만 갱신합니다.
/// </summary>
public class StatusEffectIcon : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerMoveHandler
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image typeIconImage;
    [SerializeField] private TMP_Text valueText;

    [Header("Tooltip")]
    [SerializeField] private bool showTooltipOnHover = true;
    [SerializeField] private UnitStatusEffectTooltipUI statusTooltipUI;

    private readonly List<StatusEffectRuntimeData> tooltipStatusEffects = new(1);
    private StatusEffectRuntimeData currentData;
    private bool pointerInside;

    public void SetTooltipEnabled(bool enabled)
    {
        showTooltipOnHover = enabled;

        if (!enabled)
        {
            pointerInside = false;
            HideTooltip();
        }
    }

    public void Set(StatusEffectRuntimeData data)
    {
        currentData = data;

        if (data == null)
        {
            pointerInside = false;
            HideTooltip();
            gameObject.SetActive(false);
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (iconImage != null)
            ApplyImage(iconImage, GetIcon(data.EffectId));

        if (typeIconImage != null)
            ApplyImage(typeIconImage, GetTypeIcon(data.EffectId));

        if (valueText != null)
            valueText.text = data.Stack > 0 ? data.Stack.ToString() : string.Empty;

        // 이미 마우스가 올라가 있는 상태에서 스택/턴만 갱신되면 내용만 새로 반영합니다.
        if (pointerInside)
            ShowTooltip();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        ShowTooltip();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!pointerInside || statusTooltipUI == null)
            return;

        Vector2 position = eventData != null
            ? eventData.position
            : (Vector2)Input.mousePosition;

        statusTooltipUI.UpdatePosition(position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        HideTooltip();
    }

    private void OnDisable()
    {
        pointerInside = false;
        HideTooltip();
    }

    private void OnDestroy()
    {
        HideTooltip();
    }

    private void ShowTooltip()
    {
        if (!showTooltipOnHover || currentData == null || !currentData.IsValid())
            return;

        if (statusTooltipUI == null)
            statusTooltipUI = UnitStatusEffectTooltipUI.GetOrCreate();

        if (statusTooltipUI == null)
            return;

        tooltipStatusEffects.Clear();
        tooltipStatusEffects.Add(currentData);

        Vector2 mousePosition = Input.mousePosition;
        UnitStatusEffectTooltipSide side = mousePosition.x >= Screen.width * 0.5f
            ? UnitStatusEffectTooltipSide.Left
            : UnitStatusEffectTooltipSide.Right;

        statusTooltipUI.Show(this, tooltipStatusEffects, mousePosition, side);
    }

    private void HideTooltip()
    {
        if (statusTooltipUI != null)
            statusTooltipUI.Hide(this);
    }

    private Sprite GetIcon(string effectId)
    {
        if (DataManager.Instance == null || DataManager.Instance.StatusEffectIconDatabase == null)
            return null;

        return DataManager.Instance.StatusEffectIconDatabase.TryGetIcon(effectId, out Sprite icon)
            ? icon
            : null;
    }

    private Sprite GetTypeIcon(string effectId)
    {
        if (DataManager.Instance == null || DataManager.Instance.StatusEffectIconDatabase == null)
            return null;

        if (DataManager.Instance.StatusEffectIconDatabase.TryGetTypeIcon(
                effectId,
                DataManager.Instance.EffectDatabase,
                out Sprite icon))
        {
            return icon;
        }

        return null;
    }

    private static void ApplyImage(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }
}
