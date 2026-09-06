using Relic.Gameplay.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StatusEffectIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image typeIconImage;
    [SerializeField] private TMP_Text valueText;

    [Header("Tooltip")]
    [SerializeField] private bool showTooltipOnHover = true;
    [SerializeField] private UnitStatusEffectTooltipUI statusTooltipUI;

    [Header("Hover Raycast Area")]
    [Tooltip("StatusEffectIcon 루트 RectTransform 전체를 마우스 호버 영역으로 사용합니다.")]
    [SerializeField] private bool ensureFullRectHoverArea = true;

    private readonly List<StatusEffectRuntimeData> tooltipStatusEffects = new(1);
    private StatusEffectRuntimeData currentData;
    private bool pointerInside;
    private Graphic rootRaycastGraphic;

    private void Awake()
    {
        EnsureFullRectHoverRaycast();
    }

    private void OnEnable()
    {
        EnsureFullRectHoverRaycast();
    }

    public void SetTooltipEnabled(bool enabled)
    {
        showTooltipOnHover = enabled;

        if (!showTooltipOnHover)
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
            HideTooltip();
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (iconImage != null)
            ApplyImage(iconImage, GetIcon(data.EffectId));

        if (typeIconImage != null)
            ApplyImage(typeIconImage, GetTypeIcon(data.EffectId));

        if (valueText != null)
        {
            valueText.text = data.Stack > 0
                ? data.Stack.ToString()
                : "";
        }

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
        // 툴팁은 StatusEffectIcon의 위치를 기준으로 고정되므로
        // 마우스가 움직일 때마다 다시 생성할 필요가 없습니다.
        // OnPointerEnter에서 한 번 표시하고 OnPointerExit까지 유지합니다.
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

    private void EnsureFullRectHoverRaycast()
    {
        if (!ensureFullRectHoverArea)
            return;

        if (transform is not RectTransform)
            return;

        if (rootRaycastGraphic == null)
            rootRaycastGraphic = GetComponent<Graphic>();

        if (rootRaycastGraphic == null)
        {
            Image transparentImage = gameObject.AddComponent<Image>();
            transparentImage.sprite = null;
            transparentImage.color = new Color(1f, 1f, 1f, 0f);
            transparentImage.type = Image.Type.Simple;
            rootRaycastGraphic = transparentImage;
        }

        rootRaycastGraphic.raycastTarget = true;
    }

    private void ShowTooltip()
    {
        if (!showTooltipOnHover)
            return;

        if (currentData == null || !currentData.IsValid())
        {
            HideTooltip();
            return;
        }

        if (statusTooltipUI == null)
            statusTooltipUI = UnitStatusEffectTooltipUI.GetOrCreate();

        if (statusTooltipUI == null)
            return;

        tooltipStatusEffects.Clear();
        tooltipStatusEffects.Add(currentData);

        UnitStatusEffectTooltipSide side = GetTooltipSide();
        Vector2 screenPosition = GetTooltipScreenPosition(side);
        statusTooltipUI.Show(this, tooltipStatusEffects, screenPosition, side);
    }

    private void HideTooltip()
    {
        if (statusTooltipUI == null)
            return;

        statusTooltipUI.Hide(this);
    }

    private UnitStatusEffectTooltipSide GetTooltipSide()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null)
            return Input.mousePosition.x >= Screen.width * 0.5f
                ? UnitStatusEffectTooltipSide.Left
                : UnitStatusEffectTooltipSide.Right;

        Vector2 centerScreenPosition = RectTransformUtility.WorldToScreenPoint(GetRootCanvasCamera(), rect.position);
        return centerScreenPosition.x >= Screen.width * 0.5f
            ? UnitStatusEffectTooltipSide.Left
            : UnitStatusEffectTooltipSide.Right;
    }

    private Vector2 GetTooltipScreenPosition(UnitStatusEffectTooltipSide side)
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null)
            return Input.mousePosition;

        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        Camera camera = GetRootCanvasCamera();

        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        float centerY = (bottomLeft.y + topRight.y) * 0.5f;

        if (side == UnitStatusEffectTooltipSide.Left)
            return new Vector2(bottomLeft.x, centerY);

        return new Vector2(topRight.x, centerY);
    }

    private Camera GetRootCanvasCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
    }

    private Sprite GetIcon(string effectId)
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.StatusEffectIconDatabase == null)
            return null;

        if (DataManager.Instance.StatusEffectIconDatabase.TryGetIcon(effectId, out Sprite icon))
            return icon;

        return null;
    }

    private Sprite GetTypeIcon(string effectId)
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.StatusEffectIconDatabase == null)
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
        image.sprite = sprite;
        image.enabled = sprite != null;
    }
}
