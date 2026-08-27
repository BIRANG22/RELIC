using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyRelicShopNpcInteraction : MonoBehaviour
{
    [Header("Presenter")]
    [SerializeField] private LobbyRelicShopPresenter presenter;

    [Header("Back Button")]
    [Tooltip("유물 상점 패널을 닫는 BackButton입니다.")]
    [SerializeField] private Button backButton;

    [Header("Availability Indicator")]
    [Tooltip("유물 상점을 이용할 수 없을 때도 항상 표시할 relic_stone (1)의 SpriteRenderer입니다.")]
    [SerializeField] private SpriteRenderer availabilityIndicatorRenderer;
    [Tooltip("이용 가능 상태에서 표시할 색상입니다.")]
    [SerializeField] private Color availabilityAvailableColor = new Color32(0, 177, 255, 255);
    [Tooltip("이용 제한 상태에서 깜박일 색상입니다.")]
    [SerializeField] private Color availabilityPulseColor = Color.white;
    [Tooltip("깜박임 보간 속도입니다. 값이 클수록 빠르게 변합니다.")]
    [SerializeField, Min(0.01f)] private float availabilityPulseSpeed = 1f;

    [Header("Purchase Cooldown")]
    [SerializeField] private SettingWarningUI warningUI;
    [SerializeField] private string purchaseCooldownWarningMessage = "다시 사용하려면 회복할 시간이 필요합니다.";

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;
    [SerializeField, Range(0f, 1f)] private float clickSfxVolume = 1f;

    private void Awake()
    {
        BindBackButton();
        RefreshAvailabilityIndicator(true);
    }

    private void OnEnable()
    {
        BindBackButton();
        RefreshAvailabilityIndicator(true);
    }

    private void LateUpdate()
    {
        // Purchase scripts can update runtime state after interaction.
        // Keep the shop availability and visual state synchronized each frame.
        RefreshAvailabilityIndicator(false);
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(CloseRelicShopPanel);
    }

    private void OnMouseUpAsButton()
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (presenter != null &&
            LobbyPositionModalInputBlocker.IsBlockedByAnother(presenter))
        {
            return;
        }

        if (presenter == null)
            return;

        LobbyRuntimeData runtime = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (LobbyRelicShopPurchaseLimit.HasPurchasedOffer(runtime))
        {
            PlayClickSfx();
            ShowWarning(purchaseCooldownWarningMessage);
            RefreshAvailabilityIndicator(true);
            return;
        }

        PlayClickSfx();
        presenter.Open();
    }

    private void BindBackButton()
    {
        if (backButton == null)
            return;

        backButton.onClick.RemoveListener(CloseRelicShopPanel);
        backButton.onClick.AddListener(CloseRelicShopPanel);
    }

    public void CloseRelicShopPanel()
    {
        if (presenter == null)
            return;

        PlayClickSfx();
        presenter.Close();
    }

    private void RefreshAvailabilityIndicator(bool forceColorReset)
    {
        if (availabilityIndicatorRenderer == null)
            return;

        LobbyRuntimeData runtime = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        bool locked = LobbyRelicShopPurchaseLimit.HasPurchasedOffer(runtime);

        GameObject indicatorObject = availabilityIndicatorRenderer.gameObject;
        if (indicatorObject.activeSelf == locked)
            indicatorObject.SetActive(!locked);

        if (locked)
            return;

        if (forceColorReset)
        {
            availabilityIndicatorRenderer.color = availabilityAvailableColor;
            return;
        }

        float pingPong = Mathf.PingPong(
            Time.unscaledTime * Mathf.Max(0.01f, availabilityPulseSpeed),
            1f);
        float eased = pingPong * pingPong * (3f - 2f * pingPong);
        availabilityIndicatorRenderer.color = Color.Lerp(
            availabilityAvailableColor,
            availabilityPulseColor,
            eased);
    }

    private void ShowWarning(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        if (warningUI != null)
        {
            warningUI.Show(message);
            return;
        }

        if (SettingWarningUI.Instance != null)
        {
            SettingWarningUI.Instance.Show(message);
            return;
        }

        Debug.LogWarning($"[LobbyRelicShopNpcInteraction] Warning UI is missing. Message: {message}", this);
    }

    private void PlayClickSfx()
    {
        if (!playClickSound || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx, clickSfxVolume);
    }
}
