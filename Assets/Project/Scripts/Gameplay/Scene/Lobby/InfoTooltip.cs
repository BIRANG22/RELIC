using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoTooltip : MonoBehaviour
{
    public static InfoTooltip Instance { get; private set; }

    [Header("Floating Tooltip References")]
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text effectText;

    [Header("Floating Tooltip Position")]
    [SerializeField] private Vector2 floatingOffset = new Vector2(8f, -8f);

    [Header("Fixed Info Area")]
    [SerializeField] private RectTransform fixedRoot;
    [SerializeField] private bool keepFixedRootAlwaysVisible = true;
    [SerializeField] private string emptyTitleText = "";
    [TextArea]
    [SerializeField] private string emptyEffectText = "";

    private Canvas canvas;
    private RectTransform canvasRect;
    private CanvasGroup canvasGroup;

    private TMP_Text fixedTitleText;
    private TMP_Text fixedEffectText;

    private void Awake()
    {
        Instance = this;

        canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        if (root != null)
        {
            canvasGroup = root.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        DisableRaycastTargets();
        ResolveFixedTexts();
        Hide();
    }

    private void DisableRaycastTargets()
    {
        if (root == null)
            return;

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }

    public void SetFixedRoot(RectTransform newFixedRoot)
    {
        fixedRoot = newFixedRoot;

        if (fixedRoot != null)
            fixedRoot.gameObject.SetActive(true);

        ResolveFixedTexts();
        ClearFixedText();
    }

    private void ResolveFixedTexts()
    {
        fixedTitleText = null;
        fixedEffectText = null;

        if (fixedRoot == null)
            return;

        TMP_Text[] texts = fixedRoot.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            string objectName = texts[i].gameObject.name.ToLower();

            if (fixedTitleText == null && objectName.Contains("title"))
            {
                fixedTitleText = texts[i];
                continue;
            }

            if (fixedEffectText == null &&
                (objectName.Contains("effect") ||
                 objectName.Contains("desc") ||
                 objectName.Contains("detail")))
            {
                fixedEffectText = texts[i];
                continue;
            }
        }

        if (fixedTitleText == null && texts.Length > 0)
            fixedTitleText = texts[0];

        if (fixedEffectText == null && texts.Length > 1)
            fixedEffectText = texts[1];
    }

    public void Show(string title, string effect, RectTransform targetRect)
    {
        if (root == null || targetRect == null)
            return;

        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0f, 1f);

        SetFloatingText(title, effect);
        SetPositionToTargetBottomRight(targetRect);

        root.gameObject.SetActive(true);
    }

    public void ShowFixed(string title, string effect)
    {
        if (fixedRoot != null)
            fixedRoot.gameObject.SetActive(true);

        SetFixedText(title, effect);
    }

    public void Hide()
    {
        if (fixedRoot != null && keepFixedRootAlwaysVisible)
        {
            fixedRoot.gameObject.SetActive(true);
            SetFixedText(emptyTitleText, emptyEffectText);
            return;
        }

        if (root != null)
            root.gameObject.SetActive(false);

        if (fixedRoot != null)
            fixedRoot.gameObject.SetActive(false);
    }

    public void ClearFixedText()
    {
        if (fixedRoot != null)
            fixedRoot.gameObject.SetActive(true);

        SetFixedText(emptyTitleText, emptyEffectText);
    }

    private void SetFloatingText(string title, string effect)
    {
        if (titleText != null)
            titleText.text = title;

        if (effectText != null)
            effectText.text = HighlightNumbers(effect);
    }

    private void SetFixedText(string title, string effect)
    {
        TMP_Text targetTitleText = fixedTitleText != null ? fixedTitleText : titleText;
        TMP_Text targetEffectText = fixedEffectText != null ? fixedEffectText : effectText;

        if (targetTitleText != null)
            targetTitleText.text = title;

        if (targetEffectText != null)
            targetEffectText.text = HighlightNumbers(effect);
    }

    private void SetPositionToTargetBottomRight(RectTransform targetRect)
    {
        if (root == null || canvasRect == null)
            return;

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);

        Vector3 targetBottomRightWorldPosition = corners[3];

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            targetBottomRightWorldPosition
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint
        );

        root.anchoredPosition = localPoint + floatingOffset;
    }

    public static string GetRuneEffectText(RuneData runeData)
    {
        if (runeData == null)
            return "";

        string text = "";

        if (!string.IsNullOrWhiteSpace(runeData.EffectIds))
            text += "Effect: " + runeData.EffectIds;

        if (!string.IsNullOrWhiteSpace(runeData.ValueRate))
            text += "\nValue: " + runeData.ValueRate;

        if (!string.IsNullOrWhiteSpace(runeData.CountRate))
            text += "\nCount: " + runeData.CountRate;

        if (runeData.BlueDustiumCost > 0)
            text += "\nBlue Dustium Cost: " + runeData.BlueDustiumCost;

        if (runeData.UnlockLevel > 0)
            text += "\nUnlock Lv. " + runeData.UnlockLevel;

        if (!string.IsNullOrWhiteSpace(runeData.Rarity))
            text += "\nRarity: " + runeData.Rarity;

        return string.IsNullOrWhiteSpace(text) ? GameLocalization.Get("common.no_effect", "효과 없음") : text;
    }

    public static string GetSkillEffectText(SkillMasterData skillData)
    {
        if (skillData == null)
            return "";

        string text = GameDataLocalization.SkillDetails(skillData);
        return string.IsNullOrWhiteSpace(text) ? GameLocalization.Get("common.no_effect", "효과 없음") : text;
    }
    private string HighlightNumbers(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        return System.Text.RegularExpressions.Regex.Replace(
            text,
            @"[+-]?\d+",
            "<color=#FFA500>$0</color>"
        );
    }
}
