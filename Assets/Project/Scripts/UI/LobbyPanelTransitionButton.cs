using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
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


    [Header("Lobby Background Change")]
    [Tooltip("Panel To Open에 맞는 로비 배경으로 자동 전환합니다.")]
    [SerializeField] private bool changeLobbyBackground = true;

    [Tooltip("비어 있으면 오브젝트 이름으로 자동 탐색합니다.")]
    [SerializeField] private GameObject positionBackground;
    [SerializeField] private GameObject characterSettingBackground;
    [SerializeField] private GameObject erosionSelectBackground;
    [SerializeField] private GameObject relicShopBackground;
    [SerializeField] private GameObject cultureTankBackground;

    [Header("Opened Popup Close")]
    [SerializeField] private bool closeCurrentUIPanelButtonPanelOnExecute = true;
    [SerializeField] private GameObject[] extraPanelsToCloseOnExecute;

    [Header("World Object Click")]
    [Tooltip("체크하면 이 스크립트가 붙은 월드 오브젝트를 마우스 좌클릭했을 때 Execute를 실행합니다.")]
    [SerializeField] private bool executeOnWorldClick = true;

    [Tooltip("UI 위에서 클릭했을 때 월드 오브젝트 클릭을 막습니다.")]
    [SerializeField] private bool blockWorldClickOverUI = true;

    [Tooltip("Collider2D가 없을 때 SpriteRenderer 기준으로 PolygonCollider2D를 자동 추가합니다.")]
    [SerializeField] private bool addColliderAutomatically = true;

    [Header("World Object Change")]
    [SerializeField] private GameObject[] worldObjectsToClose;
    [SerializeField] private GameObject[] worldObjectsToOpen;

    [Header("Transition")]
    [SerializeField] private LobbyPanelTransition lobbyPanelTransition;

    [Header("Camera Reset")]
    [Tooltip("전환 화면이 완전히 닫힌 뒤, 패널이 교체되기 직전에 메인 카메라를 기본 위치로 되돌립니다.")]
    [SerializeField] private bool resetCameraBeforeTransition = true;

    [Tooltip("위치를 되돌릴 메인 카메라의 HorizontalHubCameraDrag입니다. 비어 있으면 자동으로 찾습니다.")]
    [SerializeField] private HorizontalHubCameraDrag hubCameraDrag;
    [SerializeField] private PanelTransitionMode transitionMode = PanelTransitionMode.LobbyToCharacter;
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

    private void Awake()
    {
        ResolveLobbyBackgrounds();

        if (executeOnWorldClick && addColliderAutomatically)
            EnsureWorldCollider();
    }

    private void OnMouseDown()
    {
        if (!executeOnWorldClick)
            return;

        if (ShouldBlockInteractionByOpenMenuPanel())
            return;

        if (LobbyPositionModalInputBlocker.IsBlocked)
            return;

        if (blockWorldClickOverUI &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Execute();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ShouldBlockInteractionByOpenMenuPanel())
            return;

        if (!playHoverSound)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(hoverSfx, hoverSfxVolumeMultiplier);
    }

    public void Execute()
    {
        if (ShouldBlockInteractionByOpenMenuPanel())
            return;

        if (isProcessing)
            return;

        if (playClickSound && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(clickSfx, clickSfxVolumeMultiplier);

        CloseOpenedPopupPanels();

        if (lobbyPanelTransition == null)
        {
            Debug.LogWarning("[LobbyPanelTransitionButton] Lobby Panel Transition is not assigned.", this);
            InvokeBeforePanelChange();
            ApplyWorldObjectChangeImmediately();
            ApplyPanelChangeImmediately();
            InvokeAfterPanelChange();
            return;
        }

        if (lobbyPanelTransition.IsPlaying)
            return;

        GetDirections(
            out LobbyPanelTransition.TransitionDirection closeDirection,
            out LobbyPanelTransition.TransitionDirection openDirection);

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

        Invoke(
            nameof(ClearProcessing),
            Mathf.Max(0.01f, clickActionDelay + lobbyPanelTransition.EstimatedTransitionTime + 0.1f));
    }


    /// <summary>
    /// MenuPanel이 열려 있으면 메뉴 바깥의 로비 월드 오브젝트 입력을 차단합니다.
    /// MenuPanel 자신과 그 자식 버튼은 계속 사용할 수 있습니다.
    /// </summary>
    private bool ShouldBlockInteractionByOpenMenuPanel()
    {
        if (!UIPanelButton.IsMenuPanelOpen)
            return false;

        GameObject menuPanel = UIPanelButton.FindMenuPanelInScene();
        if (menuPanel == null)
            return true;

        Transform currentTransform = transform;
        return currentTransform != menuPanel.transform &&
               !currentTransform.IsChildOf(menuPanel.transform);
    }

    private void ResetCameraBeforeTransition()
    {
        if (!resetCameraBeforeTransition)
            return;

        if (hubCameraDrag == null)
        {
            hubCameraDrag = FindFirstObjectByType<HorizontalHubCameraDrag>(
                FindObjectsInactive.Include);
        }

        if (hubCameraDrag != null)
            hubCameraDrag.ResetToDefaultPositionImmediate();
    }

    private void GetDirections(
        out LobbyPanelTransition.TransitionDirection closeDirection,
        out LobbyPanelTransition.TransitionDirection openDirection)
    {
        // 현재 로비 전환은 HorizontalTransition만 사용합니다.
        closeDirection = LobbyPanelTransition.TransitionDirection.Horizontal;
        openDirection = LobbyPanelTransition.TransitionDirection.Horizontal;
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
        // 전환 화면이 완전히 닫힌 상태에서 카메라를 초기화합니다.
        // 따라서 카메라 이동 과정은 플레이어에게 보이지 않습니다.
        if (transitionMode == PanelTransitionMode.LobbyToCharacter)
            ResetCameraBeforeTransition();

        ApplyLobbyBackgroundForTargetPanel();
        beforePanelChange?.Invoke();
    }

    private void InvokeAfterPanelChange()
    {
        afterPanelChange?.Invoke();

        LobbyViewStateController viewStateController =
            FindFirstObjectByType<LobbyViewStateController>();

        if (viewStateController == null)
            return;

        if (transitionMode == PanelTransitionMode.LobbyToCharacter)
            viewStateController.ShowCharacterSelection();
        else if (transitionMode == PanelTransitionMode.CharacterToLobby)
            viewStateController.ShowPosition();
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


    private void ResolveLobbyBackgrounds()
    {
        if (positionBackground == null)
            positionBackground = FindSceneObject("Position_Back");

        if (characterSettingBackground == null)
            characterSettingBackground = FindSceneObject("CharacterSetting_Back");

        if (erosionSelectBackground == null)
            erosionSelectBackground = FindSceneObject("ErosionSelect_Back");

        if (relicShopBackground == null)
            relicShopBackground = FindSceneObject("RelicShop_Back");

        if (cultureTankBackground == null)
            cultureTankBackground = FindSceneObject("CultureTank_Back");
    }

    private void ApplyLobbyBackgroundForTargetPanel()
    {
        if (!changeLobbyBackground)
            return;

        ResolveLobbyBackgrounds();

        GameObject targetBackground = GetTargetLobbyBackground();
        if (targetBackground == null)
            return;

        SetBackgroundActive(positionBackground, targetBackground);
        SetBackgroundActive(characterSettingBackground, targetBackground);
        SetBackgroundActive(erosionSelectBackground, targetBackground);
        SetBackgroundActive(relicShopBackground, targetBackground);
        SetBackgroundActive(cultureTankBackground, targetBackground);
    }

    private GameObject GetTargetLobbyBackground()
    {
        if (transitionMode == PanelTransitionMode.CharacterToLobby)
            return positionBackground;

        if (panelToOpen == null)
            return null;

        switch (panelToOpen.name)
        {
            case "PositionPanel":
                return positionBackground;

            case "CharacterSettingPanel":
                return characterSettingBackground;

            case "ErosionSelectPanel":
                return erosionSelectBackground;

            case "RelicShopPanel":
                return relicShopBackground;

            case "CultureTankPanel":
                return cultureTankBackground;

            default:
                return null;
        }
    }

    private static void SetBackgroundActive(GameObject background, GameObject targetBackground)
    {
        if (background != null)
            background.SetActive(background == targetBackground);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        GameObject[] objects = FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];

            if (candidate != null && candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    private void EnsureWorldCollider()
    {
        if (GetComponent<Collider2D>() != null || GetComponent<Collider>() != null)
            return;

        if (GetComponent<SpriteRenderer>() == null)
        {
            Debug.LogWarning(
                "[LobbyPanelTransitionButton] 월드 클릭을 사용하지만 Collider와 SpriteRenderer가 없습니다. " +
                "Anchor 오브젝트에 Collider2D를 직접 추가하세요.",
                this);
            return;
        }

        gameObject.AddComponent<PolygonCollider2D>();
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
