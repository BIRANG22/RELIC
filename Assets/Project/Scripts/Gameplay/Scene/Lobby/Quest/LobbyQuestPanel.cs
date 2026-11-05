using TMPro;
using UnityEngine.Localization.Settings;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LobbyQuestPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text questText;

    [Header("Automatic Size")]
    [SerializeField, Min(0f)] private float horizontalPadding = 28f;
    [SerializeField, Min(0f)] private float screenEdgePadding = 16f;
    [SerializeField, Min(1f)] private float minimumPanelWidth = 100f;

    private RectTransform panelRect;
    private RectTransform textRect;
    private RectTransform canvasRect;
    private bool initialized;

    private void Awake()
    {
        ConfigureDynamicQuestText();
        InitializeSizing();
    }

    private void ConfigureDynamicQuestText()
    {
        if (questText == null)
            return;

        LocalizedTMPText.ProtectDynamicOwnership(questText);
        DynamicLocalizedTMPText redundantWriter = questText.GetComponent<DynamicLocalizedTMPText>();
        if (redundantWriter != null)
            redundantWriter.enabled = false;
    }

    private void InitializeSizing()
    {
        if (initialized || questText == null)
            return;

        panelRect = transform as RectTransform;
        textRect = questText.rectTransform;
        if (panelRect == null || textRect == null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.rootCanvas.transform as RectTransform;

        initialized = true;
    }

    // Quest text is localized at runtime, so its font must be resolved before
    // TextMeshPro measures glyph widths in GetPreferredValues(). The global
    // font applicator may otherwise run later in the same frame.
    private void ApplyQuestLocaleFont()
    {
        if (questText == null)
            return;

        var locale = LocalizationSettings.SelectedLocale;
        if (locale == null)
            return;

        LocaleFontDatabase database = LocaleFontDatabaseProvider.LoadDefault();
        if (database == null ||
            !database.TryResolve(locale.Identifier.Code, LocaleFontDatabase.BodyRoleId, out TMP_FontAsset localeFont) ||
            localeFont == null || questText.font == localeFont)
        {
            return;
        }

        questText.font = localeFont;
    }

    private void ResizeToText()
    {
        InitializeSizing();
        if (!initialized)
            return;

        // Read the actual right screen boundary in local panel units.
        // Do not change the anchors, pivot, anchored positions or scale.
        float availableWidth = float.PositiveInfinity;
        if (canvasRect != null)
        {
            Vector3[] corners = new Vector3[4];
            canvasRect.GetWorldCorners(corners);
            Vector3 rightTopLocal = panelRect.InverseTransformPoint(corners[2]);
            Vector3 rightBottomLocal = panelRect.InverseTransformPoint(corners[3]);
            float rightEdge = Mathf.Min(rightTopLocal.x, rightBottomLocal.x);
            float scale = Mathf.Max(0.0001f, Mathf.Abs(panelRect.lossyScale.x));
            availableWidth = Mathf.Max(1f, rightEdge - screenEdgePadding / scale);
        }

        // Keep the original heights. Text is a single line, not wrapped vertically.
        questText.textWrappingMode = TextWrappingModes.NoWrap;
        questText.overflowMode = TextOverflowModes.Ellipsis;
        float preferredWidth = questText.GetPreferredValues(questText.text, Mathf.Infinity, Mathf.Infinity).x;
        float requestedWidth = Mathf.Max(minimumPanelWidth, Mathf.Ceil(preferredWidth + horizontalPadding + 2f));
        float targetWidth = Mathf.Min(availableWidth, requestedWidth);
        float targetTextWidth = Mathf.Max(1f, targetWidth - horizontalPadding);
        // Existing top-left anchors and pivots expand only to the right.
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetTextWidth);
    }

    public void Apply(LobbyQuestState state)
    {
        ConfigureDynamicQuestText();
        if (gameObject.activeSelf != state.IsVisible)
            gameObject.SetActive(state.IsVisible);

        if (questText != null && state.IsVisible)
        {
            // Apply the language font before assigning localized characters or
            // measuring them; this also handles locale changes while visible.
            ApplyQuestLocaleFont();
            if (questText.text != state.Text)
                questText.text = state.Text;
            ResizeToText();
        }
    }
}
