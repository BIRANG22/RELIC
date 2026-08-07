using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyRelicShopNpcInteraction : MonoBehaviour
{
    [Header("Presenter")]
    [SerializeField] private LobbyRelicShopPresenter presenter;

    [Header("Back Button")]
    [Tooltip("유물 상점 패널을 닫는 BackButton입니다.")]
    [SerializeField] private Button backButton;

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private SfxType clickSfx = SfxType.NormalButtonClick;
    [SerializeField, Range(0f, 1f)] private float clickSfxVolume = 1f;

    private void Awake()
    {
        BindBackButton();
    }

    private void OnEnable()
    {
        BindBackButton();
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(CloseRelicShopPanel);
    }

    private void OnMouseUpAsButton()
    {
        // 메뉴 패널이 열려 있는 동안에는 월드 NPC 클릭을 받지 않는다.
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        // 침식도 선택창이나 배양조처럼 다른 위치 모달이 열려 있으면
        // 유물 상점을 중복으로 열지 않는다.
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

    private void PlayClickSfx()
    {
        if (!playClickSound || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx, clickSfxVolume);
    }
}