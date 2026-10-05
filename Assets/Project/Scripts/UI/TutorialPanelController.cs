using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public sealed class TutorialPanelController : MonoBehaviour
{
    public const int PageCount = 5;

    private static readonly string[] PageKeys =
    {
        LocalizationKeys.Tutorial.BattleGuide01,
        LocalizationKeys.Tutorial.BattleGuide02,
        LocalizationKeys.Tutorial.BattleGuide03,
        LocalizationKeys.Tutorial.BattleGuide04,
        LocalizationKeys.Tutorial.BattleGuide05
    };

    [Header("Page Content")]
    [SerializeField] private Image previewImage;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private Sprite[] pageImages = new Sprite[PageCount];

    [Header("Page Indicators")]
    [SerializeField] private GameObject[] pageIndicatorBacks = new GameObject[PageCount];

    [Header("Navigation")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button confirmButton;

    private int currentPageIndex;

    public static string GetPageKey(int pageIndex) => PageKeys[ClampPageIndex(pageIndex)];
    public static int ClampPageIndex(int pageIndex) => Mathf.Clamp(pageIndex, 0, PageCount - 1);

    private void Awake()
    {
        EnsurePageImageSlots();
        BindButtons();
        ShowPage(0);
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        RefreshPage();
    }

    private void OnDisable() => LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    private void OnDestroy() => UnbindButtons();

    public void ShowPreviousPage() => ShowPage(currentPageIndex - 1);
    public void ShowNextPage() => ShowPage(currentPageIndex + 1);
    public void Close() => Destroy(gameObject);

    public void ShowPage(int pageIndex)
    {
        currentPageIndex = ClampPageIndex(pageIndex);
        RefreshPage();
    }

    private void RefreshPage()
    {
        if (previewImage != null)
        {
            Sprite pageSprite = pageImages != null && currentPageIndex < pageImages.Length
                ? pageImages[currentPageIndex]
                : null;
            previewImage.sprite = pageSprite;
            previewImage.enabled = pageSprite != null;
        }

        if (descText != null)
            descText.text = GameLocalization.Get(GetPageKey(currentPageIndex));

        if (pageIndicatorBacks != null)
        {
            for (int i = 0; i < pageIndicatorBacks.Length; i++)
            {
                GameObject indicatorBack = pageIndicatorBacks[i];
                if (indicatorBack != null)
                    indicatorBack.SetActive(i == currentPageIndex);
            }
        }

        if (leftButton != null)
            leftButton.interactable = currentPageIndex > 0;
        if (rightButton != null)
            rightButton.interactable = currentPageIndex < PageCount - 1;
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(currentPageIndex == PageCount - 1);
    }

    private void BindButtons()
    {
        if (leftButton != null) leftButton.onClick.AddListener(ShowPreviousPage);
        if (rightButton != null) rightButton.onClick.AddListener(ShowNextPage);
        if (confirmButton != null) confirmButton.onClick.AddListener(Close);
    }

    private void UnbindButtons()
    {
        if (leftButton != null) leftButton.onClick.RemoveListener(ShowPreviousPage);
        if (rightButton != null) rightButton.onClick.RemoveListener(ShowNextPage);
        if (confirmButton != null) confirmButton.onClick.RemoveListener(Close);
    }

    private void HandleLocaleChanged(Locale _) => RefreshPage();

    private void EnsurePageImageSlots()
    {
        if (pageImages != null && pageImages.Length == PageCount)
            return;

        var normalized = new Sprite[PageCount];
        if (pageImages != null)
        {
            int copyCount = Mathf.Min(pageImages.Length, normalized.Length);
            for (int i = 0; i < copyCount; i++) normalized[i] = pageImages[i];
        }
        pageImages = normalized;
    }
}
