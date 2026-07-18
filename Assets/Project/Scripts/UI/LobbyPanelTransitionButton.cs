using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class LobbyPanelTransitionButton : MonoBehaviour, IPointerEnterHandler
{
    public enum PanelTransitionMode
    {
        LobbyToCharacter,
        CharacterToLobby,
        Custom
    }

    [Header("Panel Change")]
    [SerializeField] private GameObject[] panelsToClose;
    [SerializeField] private GameObject panelToOpen;

    [Header("Opened Popup Close")]
    [SerializeField] private bool closeCurrentUIPanelButtonPanelOnExecute = true;
    [SerializeField] private GameObject[] extraPanelsToCloseOnExecute;

    [Header("World Object Change")]
    [SerializeField] private GameObject[] worldObjectsToClose;
    [SerializeField] private GameObject[] worldObjectsToOpen;

    [Header("Transition")]
    [SerializeField] private LobbyPanelTransition lobbyPanelTransition;
    [SerializeField] private PanelTransitionMode transitionMode = PanelTransitionMode.LobbyToCharacter;
    [SerializeField] private LobbyPanelTransition.TransitionDirection customCloseDirection = LobbyPanelTransition.TransitionDirection.Vertical;
    [SerializeField] private LobbyPanelTransition.TransitionDirection customOpenDirection = LobbyPanelTransition.TransitionDirection.Horizontal;
    [SerializeField] private float clickActionDelay = 0f;

    [Header("Middle Actions")]
    [SerializeField] private UnityEvent beforePanelChange;
    [SerializeField] private UnityEvent afterPanelChange;

    [Header("Button Sound")]
    [SerializeField] private bool playHoverSound = true;
    [SerializeField] private SfxType hoverSfx = SfxType.NormalButtonHover;
    [SerializeField] private float hoverSfxVolumeMultiplier = 1f;
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private SfxType clickSfx = SfxType.NormalButtonClick;
    [SerializeField] private float clickSfxVolumeMultiplier = 1f;

    private bool isProcessing;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHoverSound)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(hoverSfx, hoverSfxVolumeMultiplier);
    }

    public void Execute()
    {
        if (isProcessing)
            return;

        if (playClickSound && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(clickSfx, clickSfxVolumeMultiplier);

        CloseOpenedPopupPanels();

        if (lobbyPanelTransition == null)
        {
            Debug.LogWarning("[LobbyPanelTransitionButton] Lobby Panel Transition is not assigned.");
            InvokeBeforePanelChange();
            ApplyPanelChangeImmediately();
            InvokeAfterPanelChange();
            return;
        }

        if (lobbyPanelTransition.IsPlaying)
            return;

        GetDirections(out LobbyPanelTransition.TransitionDirection closeDirection, out LobbyPanelTransition.TransitionDirection openDirection);

        isProcessing = true;

        lobbyPanelTransition.PlayPanelChange(
            panelsToClose,
            panelToOpen,
            worldObjectsToClose,
            worldObjectsToOpen,
            closeDirection,
            openDirection,
            clickActionDelay,
            InvokeBeforePanelChange,
            InvokeAfterPanelChange);

        Invoke(nameof(ClearProcessing), Mathf.Max(0.01f, clickActionDelay + lobbyPanelTransition.EstimatedTransitionTime + 0.1f));
    }

    private void GetDirections(out LobbyPanelTransition.TransitionDirection closeDirection, out LobbyPanelTransition.TransitionDirection openDirection)
    {
        if (transitionMode == PanelTransitionMode.LobbyToCharacter)
        {
            closeDirection = LobbyPanelTransition.TransitionDirection.Vertical;
            openDirection = LobbyPanelTransition.TransitionDirection.Horizontal;
            return;
        }

        if (transitionMode == PanelTransitionMode.CharacterToLobby)
        {
            closeDirection = LobbyPanelTransition.TransitionDirection.Horizontal;
            openDirection = LobbyPanelTransition.TransitionDirection.Vertical;
            return;
        }

        closeDirection = customCloseDirection;
        openDirection = customOpenDirection;
    }

    private void CloseOpenedPopupPanels()
    {
        if (closeCurrentUIPanelButtonPanelOnExecute)
            UIPanelButton.CloseCurrentOpenedPanel();

        if (extraPanelsToCloseOnExecute == null)
            return;

        for (int i = 0; i < extraPanelsToCloseOnExecute.Length; i++)
        {
            if (extraPanelsToCloseOnExecute[i] != null)
                extraPanelsToCloseOnExecute[i].SetActive(false);
        }
    }

    private void InvokeBeforePanelChange()
    {
        if (beforePanelChange != null)
            beforePanelChange.Invoke();
    }

    private void InvokeAfterPanelChange()
    {
        if (afterPanelChange != null)
            afterPanelChange.Invoke();

        LobbyViewStateController viewStateController =
            FindFirstObjectByType<LobbyViewStateController>();

        if (viewStateController == null)
            return;

        if (transitionMode == PanelTransitionMode.LobbyToCharacter)
            viewStateController.ShowCharacterSelection();
        else if (transitionMode == PanelTransitionMode.CharacterToLobby)
            viewStateController.ShowLobby();
    }


    private void ApplyWorldObjectChangeImmediately()
    {
        if (worldObjectsToClose != null)
        {
            for (int i = 0; i < worldObjectsToClose.Length; i++)
            {
                if (worldObjectsToClose[i] != null)
                    worldObjectsToClose[i].SetActive(false);
            }
        }

        if (worldObjectsToOpen != null)
        {
            for (int i = 0; i < worldObjectsToOpen.Length; i++)
            {
                if (worldObjectsToOpen[i] != null)
                    worldObjectsToOpen[i].SetActive(true);
            }
        }
    }

    private void ApplyPanelChangeImmediately()
    {
        if (panelsToClose != null)
        {
            for (int i = 0; i < panelsToClose.Length; i++)
            {
                if (panelsToClose[i] != null)
                    panelsToClose[i].SetActive(false);
            }
        }

        if (panelToOpen != null)
            panelToOpen.SetActive(true);
    }

    private void ClearProcessing()
    {
        isProcessing = false;
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ClearProcessing));
        isProcessing = false;
    }
}
