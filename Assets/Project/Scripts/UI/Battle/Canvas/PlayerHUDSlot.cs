using Relic.Gameplay.Data;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Minimal HUD slot used to switch the active battle character.
/// </summary>
public class PlayerHUDSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Character Portraits")]
    [Tooltip("Normal portrait. If empty, PortraitImage is found automatically.")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private bool autoFindPortraitImages = true;
    [SerializeField] private string portraitObjectName = "PortraitImage";

    [Tooltip("Selected portrait. If empty, Select_PortraitImage is found automatically.")]
    [SerializeField] private Image selectPortraitImage;
    [SerializeField] private string selectPortraitObjectName = "Select_PortraitImage";

    [Header("Edge Selection Color")]
    [Tooltip("Edge image. If empty, Edje is found first and Edge is used as a fallback.")]
    [SerializeField] private Image edgeImage;
    [SerializeField] private bool autoFindEdgeImage = true;
    [SerializeField] private Color normalEdgeColor = new Color32(0x77, 0x77, 0x77, 0xFF);
    [SerializeField] private Color selectedEdgeColor = new Color32(0xEE, 0xEE, 0xEE, 0xFF);

    [Header("Hover / Selection Scale")]
    [SerializeField] private float hoverScale = 1.1f;

    [Header("Click Selection")]
    [SerializeField] private bool enableHudClickSelect = true;
    [SerializeField] private bool ensureClickableRaycastGraphic = true;

    private CharacterRuntimeData boundRuntime;
    private bool isCommandSelected;
    private bool isPointerHovering;
    private Vector3 baseScale;

    public CharacterRuntimeData BoundRuntime => boundRuntime;

    public event Action<CharacterRuntimeData, RectTransform> OnClicked;

    private void Awake()
    {
        baseScale = transform.localScale;
        ResolveReferences();
        EnsureClickableGraphic();
        ApplySelectionVisual();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (boundRuntime != null)
            Refresh();
        else
            ApplySelectionVisual();
    }

    public void Bind(CharacterRuntimeData runtimeData)
    {
        boundRuntime = runtimeData;
        ResolveReferences();

        if (boundRuntime == null)
        {
            Clear();
            return;
        }

        Refresh();
    }

    /// <summary>
    /// Kept for compatibility with existing battle/rest room setup code.
    /// </summary>
    public void SetKeyboardNumber(int number)
    {
    }

    /// <summary>
    /// Kept for compatibility with BattleRoomLoader and RestRoomController.
    /// The supplied scale becomes the base scale used by hover/selection enlargement.
    /// </summary>
    public void SetBaseScale(Vector3 baseScale)
    {
        this.baseScale = baseScale;
        ApplyScaleVisual();
    }

    public void Refresh()
    {
        if (boundRuntime == null)
        {
            Clear();
            return;
        }

        ResolveReferences();

        Sprite normalPortrait = GetCharacterHUDPortraitImage(boundRuntime.CharacterId);
        Sprite selectedPortrait = GetCharacterHUDSelectedPortraitImage(boundRuntime.CharacterId);

        if (portraitImage != null)
            portraitImage.sprite = normalPortrait;

        if (selectPortraitImage != null)
            selectPortraitImage.sprite = selectedPortrait;

        ApplySelectionVisual();
    }

    public void SetCommandSelected(bool selected)
    {
        isCommandSelected = selected;
        ApplySelectionVisual();
        ApplyScaleVisual();
    }

    /// <summary>
    /// Kept for compatibility with existing BattleCharacter calls.
    /// Linked world-character hover uses the same HUD scale feedback.
    /// </summary>
    public void SetLinkedCharacterHover(bool active)
    {
        isPointerHovering = active;
        ApplyScaleVisual();
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsMenuPanelOpen() || boundRuntime == null)
            return;

        isPointerHovering = true;
        ApplyScaleVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPointerHovering)
            return;

        isPointerHovering = false;
        ApplyScaleVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!enableHudClickSelect)
            return;

        if (IsMenuPanelOpen())
            return;

        if (boundRuntime == null)
            return;

        OnClicked?.Invoke(boundRuntime, GetComponent<RectTransform>());
    }

    private void ResolveReferences()
    {
        if (autoFindPortraitImages)
        {
            if (portraitImage == null)
                portraitImage = FindChildImageByName(portraitObjectName);

            if (selectPortraitImage == null)
                selectPortraitImage = FindChildImageByName(selectPortraitObjectName);
        }

        if (autoFindEdgeImage && edgeImage == null)
        {
            edgeImage = FindChildImageByName("Edje");

            if (edgeImage == null)
                edgeImage = FindChildImageByName("Edge");
        }
    }

    private void ApplySelectionVisual()
    {
        if (portraitImage != null)
            portraitImage.gameObject.SetActive(!isCommandSelected);

        if (selectPortraitImage != null)
            selectPortraitImage.gameObject.SetActive(isCommandSelected);

        if (edgeImage != null)
        {
            edgeImage.color = isCommandSelected
                ? selectedEdgeColor
                : normalEdgeColor;
        }
    }


    private void ApplyScaleVisual()
    {
        bool enlarged = isCommandSelected || isPointerHovering;
        float multiplier = enlarged ? Mathf.Max(1f, hoverScale) : 1f;
        transform.localScale = baseScale * multiplier;
    }

    private Image FindChildImageByName(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == null || child == transform)
                continue;

            if (!string.Equals(child.name, targetName, StringComparison.OrdinalIgnoreCase))
                continue;

            Image image = child.GetComponent<Image>();
            if (image != null)
                return image;
        }

        return null;
    }

    private Sprite GetCharacterHUDPortraitImage(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        if (DataManager.Instance == null || DataManager.Instance.CharacterIconDatabase == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase.TryGetHUDPortraitImage(characterId, out Sprite portrait))
            return portrait;

        return null;
    }

    private Sprite GetCharacterHUDSelectedPortraitImage(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        if (DataManager.Instance == null || DataManager.Instance.CharacterIconDatabase == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase.TryGetHUDSelectedPortraitImage(characterId, out Sprite portrait))
            return portrait;

        return null;
    }

    private void EnsureClickableGraphic()
    {
        if (!ensureClickableRaycastGraphic)
            return;

        Graphic graphic = GetComponent<Graphic>();

        if (graphic == null)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            return;
        }

        graphic.raycastTarget = true;
    }

    private void Clear()
    {
        if (portraitImage != null)
            portraitImage.sprite = null;

        if (selectPortraitImage != null)
            selectPortraitImage.sprite = null;

        isCommandSelected = false;
        isPointerHovering = false;
        ApplySelectionVisual();
        ApplyScaleVisual();
    }

    private static bool IsMenuPanelOpen()
    {
        GameObject menuPanel = GameObject.Find("MenuPanel");
        return menuPanel != null && menuPanel.activeInHierarchy;
    }
}
