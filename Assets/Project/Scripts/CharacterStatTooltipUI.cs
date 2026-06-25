using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterStatTooltipUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Position")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private Vector2 screenOffset = new Vector2(24f, -24f);
    [SerializeField] private bool followMouse = false;
    [SerializeField] private bool keepInsideScreen = true;
    [SerializeField] private Vector2 screenPadding = new Vector2(16f, 16f);

    private bool isShowing;
    private Camera canvasCamera;

    private void Awake()
    {
        AutoBindIfNeeded();
        Hide();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoBindIfNeeded();
    }
#endif

    private void Update()
    {
        if (!isShowing || !followMouse)
            return;

        SetPosition(Input.mousePosition);
    }

    public void Show(string statName, string description, string valueLine, Vector2 screenPosition)
    {
        AutoBindIfNeeded();

        if (nameText != null)
            nameText.text = statName ?? "";

        if (descriptionText != null)
        {
            if (string.IsNullOrWhiteSpace(valueLine))
                descriptionText.text = description ?? "";
            else if (string.IsNullOrWhiteSpace(description))
                descriptionText.text = valueLine;
            else
                descriptionText.text = description.TrimEnd() + "\n\n" + valueLine;
        }

        gameObject.SetActive(true);
        isShowing = true;
        SetPosition(screenPosition);
    }

    public void Hide()
    {
        isShowing = false;
        gameObject.SetActive(false);
    }

    public void SetFollowMouse(bool value)
    {
        followMouse = value;
    }

    public void SetPosition(Vector2 screenPosition)
    {
        AutoBindIfNeeded();

        if (tooltipRect == null)
            return;

        Vector2 targetScreenPosition = screenPosition + screenOffset;

        if (keepInsideScreen)
            targetScreenPosition = ClampToScreen(targetScreenPosition);

        Canvas canvas = GetTargetCanvas();

        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            tooltipRect.position = targetScreenPosition;
            return;
        }

        RectTransform parentRect = tooltipRect.parent as RectTransform;

        if (parentRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                targetScreenPosition,
                canvasCamera,
                out Vector2 localPoint))
        {
            tooltipRect.localPosition = localPoint;
        }
    }

    private Vector2 ClampToScreen(Vector2 screenPosition)
    {
        if (tooltipRect == null)
            return screenPosition;

        Vector2 size = tooltipRect.rect.size;
        Vector3 scale = tooltipRect.lossyScale;
        size.x *= Mathf.Abs(scale.x);
        size.y *= Mathf.Abs(scale.y);

        float minX = screenPadding.x;
        float maxX = Screen.width - screenPadding.x;
        float minY = screenPadding.y;
        float maxY = Screen.height - screenPadding.y;

        if (screenPosition.x + size.x > maxX)
            screenPosition.x = Mathf.Max(minX, maxX - size.x);

        if (screenPosition.y - size.y < minY)
            screenPosition.y = Mathf.Min(maxY, minY + size.y);

        screenPosition.x = Mathf.Clamp(screenPosition.x, minX, maxX);
        screenPosition.y = Mathf.Clamp(screenPosition.y, minY, maxY);

        return screenPosition;
    }

    private Canvas GetTargetCanvas()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        if (targetCanvas != null)
        {
            Canvas rootCanvas = targetCanvas.rootCanvas;
            canvasCamera = rootCanvas != null ? rootCanvas.worldCamera : targetCanvas.worldCamera;
            return rootCanvas != null ? rootCanvas : targetCanvas;
        }

        canvasCamera = null;
        return null;
    }

    private void AutoBindIfNeeded()
    {
        if (tooltipRect == null)
            tooltipRect = transform as RectTransform;

        if (nameText == null)
            nameText = FindChildText("NameText", "Name", "TitleText", "Title");

        if (descriptionText == null)
            descriptionText = FindChildText("DescriptionText", "Description", "DescText", "Desc");

        GetTargetCanvas();
    }

    private TMP_Text FindChildText(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = FindChildByName(transform, names[i]);

            if (child == null)
                continue;

            TMP_Text text = child.GetComponent<TMP_Text>();

            if (text != null)
                return text;
        }

        return null;
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
}
