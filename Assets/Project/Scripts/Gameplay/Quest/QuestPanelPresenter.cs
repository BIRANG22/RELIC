using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class QuestPanelPresenter : MonoBehaviour
{
    [SerializeField] private TMP_Text questText;

    private void Awake()
    {
        AutoBind();
    }

    public void Bind(TMP_Text text)
    {
        questText = text;
    }

    public void Show(string text, bool visible)
    {
        if (questText != null)
            questText.text = text ?? string.Empty;

        gameObject.SetActive(visible);
    }

    public static QuestPanelPresenter CreateDefault(Transform parent)
    {
        GameObject panelObject = new("QuestPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(QuestPanelPresenter));
        panelObject.layer = 5;

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(parent, false);
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -32f);
        panelRect.sizeDelta = new Vector2(720f, 64f);

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject textObject = new("QuestText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = 5;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(panelRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 8f);
        textRect.offsetMax = new Vector2(-24f, -8f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.fontSize = 24f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = Color.white;

        QuestPanelPresenter presenter = panelObject.GetComponent<QuestPanelPresenter>();
        presenter.Bind(text);
        presenter.Show(string.Empty, false);
        return presenter;
    }

    private void AutoBind()
    {
        if (questText != null)
            return;

        questText = GetComponentInChildren<TMP_Text>(true);
    }
}
