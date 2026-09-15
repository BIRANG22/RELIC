using UnityEngine;

public sealed class LobbyRelicShopNpcInteraction : MonoBehaviour
{
    [Header("Presenter")]
    [SerializeField] private LobbyRelicShopPresenter presenter;

    [Header("Availability Indicator")]
    [Tooltip("유물 구매 여부와 관계없이 계속 표시하고 숨쉬는 효과를 유지할 relic_rune SpriteRenderer입니다.")]
    [SerializeField] private SpriteRenderer availabilityIndicatorRenderer;
    [Tooltip("숨쉬는 효과의 기준 색상입니다.")]
    [SerializeField] private Color availabilityAvailableColor = new Color32(0, 177, 255, 255);
    [Tooltip("숨쉬는 효과에서 보간할 색상입니다.")]
    [SerializeField] private Color availabilityPulseColor = Color.white;
    [Tooltip("숨쉬는 효과의 보간 속도입니다. 값이 클수록 빠르게 변합니다.")]
    [SerializeField, Min(0.01f)] private float availabilityPulseSpeed = 1f;

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;
    [SerializeField, Range(0f, 1f)] private float clickSfxVolume = 1f;

    private void Awake()
    {
        RefreshAvailabilityIndicator(true);
    }

    private void OnEnable()
    {
        RefreshAvailabilityIndicator(true);
    }

    private void LateUpdate()
    {
        RefreshAvailabilityIndicator(false);
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

        PlayClickSfx();
        presenter.Open();
    }

    public void CloseRelicShopPanel()
    {
        if (presenter == null)
            return;

        presenter.Close();
    }

    private void RefreshAvailabilityIndicator(bool forceColorReset)
    {
        if (availabilityIndicatorRenderer == null)
            return;

        GameObject indicatorObject = availabilityIndicatorRenderer.gameObject;
        if (!indicatorObject.activeSelf)
            indicatorObject.SetActive(true);

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

    private void PlayClickSfx()
    {
        if (!playClickSound || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx, clickSfxVolume);
    }
}
