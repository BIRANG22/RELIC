using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyRelicShopNpcInteraction : MonoBehaviour
{
    [Header("Presenter")]
    [SerializeField] private LobbyRelicShopPresenter presenter;

    [Header("Back Button")]
    [Tooltip("���� ���� �г��� �ݴ� BackButton�Դϴ�.")]
    [SerializeField] private Button backButton;

    [Header("Availability Indicator")]
    [Tooltip("���� ������ �̿��� �� ���� �� �׻� ǥ���� relic_stone (1)�� SpriteRenderer�Դϴ�.")]
    [SerializeField] private SpriteRenderer availabilityIndicatorRenderer;
    [Tooltip("�̿� ���� ���¿��� ������ ȿ���� ���� �����Դϴ�.")]
    [SerializeField] private Color availabilityAvailableColor = new Color32(0, 177, 255, 255);
    [Tooltip("�̿� ���� ���¿��� ������ ȿ���� ���� �����Դϴ�.")]
    [SerializeField] private Color availabilityPulseColor = Color.white;
    [Tooltip("������ �պ��ϴ� �ӵ��Դϴ�. ���� Ŭ���� ������ �����ϴ�.")]
    [SerializeField, Min(0.01f)] private float availabilityPulseSpeed = 1f;

    [Header("Purchase Cooldown")]
    [SerializeField] private SettingWarningUI warningUI;
    [SerializeField] private string purchaseCooldownWarningMessage = "�ٽ� �̿��Ϸ��� ��� �ð��� �ʿ��մϴ�.";

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
        // ���� ȣ�� ��ũ��Ʈ�� ���� ������Ʈ�� �Ѱ� ������
        // ���� ������ ���� �̿� ���� ���°� ���� ǥ�� ���¸� �����ϵ��� LateUpdate���� �����մϴ�.
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
