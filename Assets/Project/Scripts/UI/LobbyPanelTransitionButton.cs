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
    [SerializeField]
    private PanelTransitionMode transitionMode =
        PanelTransitionMode.LobbyToCharacter;

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

    private LobbyBackgroundStateController backgroundStateController;
    private bool isProcessing;

    /// <summary>
    /// 이 버튼에 지정된 Panel To Open이 현재 열려 있는지 반환합니다.
    /// SpriteHoverScale에서 선택 표시를 유지할 때 사용합니다.
    /// </summary>
    public bool IsTargetPanelOpen =>
        panelToOpen != null && panelToOpen.activeInHierarchy;

    private void Awake()
    {
        ResolveLobbyBackgrounds();

        if (executeOnWorldClick && addColliderAutomatically)
            EnsureWorldCollider();
    }

    private void OnMouseUpAsButton()
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
        {
            AudioManager.Instance.PlaySfx(
                hoverSfx,
                hoverSfxVolumeMultiplier);
        }
    }

    public void Execute()
    {
        if (ShouldBlockInteractionByOpenMenuPanel())
            return;

        if (panelToOpen != null &&
            !panelToOpen.activeInHierarchy &&
            PanelCameraMover.IsAnotherTargetPanelOpen(panelToOpen))
        {
            return;
        }

        if (isProcessing)
            return;

        if (playClickSound && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(
                clickSfx,
                clickSfxVolumeMultiplier);
        }

        CloseOpenedPopupPanels();

        if (lobbyPanelTransition == null)
        {
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

        GameObject[] effectivePanelsToClose = GetEffectivePanelsToClose();
        GameObject[] effectiveWorldObjectsToClose = GetEffectiveWorldObjectsToClose();

        lobbyPanelTransition.PlayPanelChange(
            effectivePanelsToClose,
            panelToOpen,
            effectiveWorldObjectsToClose,
            worldObjectsToOpen,
            closeDirection,
            openDirection,
            clickActionDelay,
            InvokeBeforePanelChange,
            InvokeAfterPanelChange);

        Invoke(
            nameof(ClearProcessing),
            Mathf.Max(
                0.01f,
                clickActionDelay +
                lobbyPanelTransition.EstimatedTransitionTime +
                0.1f));
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

    private void GetDirections(
        out LobbyPanelTransition.TransitionDirection closeDirection,
        out LobbyPanelTransition.TransitionDirection openDirection)
    {
        closeDirection =
            LobbyPanelTransition.TransitionDirection.Horizontal;

        openDirection =
            LobbyPanelTransition.TransitionDirection.Horizontal;
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
        {
            viewStateController.ShowCharacterSelection();

            if (panelToOpen != null && panelToOpen.name == "CharacterSettingPanel")
                KeepSettingButtonActive();
        }
        else if (transitionMode == PanelTransitionMode.CharacterToLobby)
        {
            viewStateController.ShowPosition();
        }
    }

    private void ApplyWorldObjectChangeImmediately()
    {
        GameObject[] effectiveWorldObjectsToClose = GetEffectiveWorldObjectsToClose();

        if (effectiveWorldObjectsToClose != null)
        {
            for (int i = 0; i < effectiveWorldObjectsToClose.Length; i++)
            {
                if (effectiveWorldObjectsToClose[i] != null)
                    effectiveWorldObjectsToClose[i].SetActive(false);
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
        GameObject[] effectivePanelsToClose = GetEffectivePanelsToClose();

        if (effectivePanelsToClose != null)
        {
            for (int i = 0; i < effectivePanelsToClose.Length; i++)
            {
                if (effectivePanelsToClose[i] != null)
                    effectivePanelsToClose[i].SetActive(false);
            }
        }

        if (panelToOpen != null)
            panelToOpen.SetActive(true);
    }

    /// <summary>
    /// CharacterSettingPanel을 열 때는 로비의 SettingButton을 유지합니다.
    /// 기존 인스펙터의 Panels To Close에 SettingButton이 들어 있어도
    /// 캐릭터 설정 화면으로 전환할 때만 자동으로 제외합니다.
    /// </summary>
    private GameObject[] GetEffectivePanelsToClose()
    {
        if (panelsToClose == null || panelsToClose.Length == 0)
            return panelsToClose;

        if (panelToOpen == null || panelToOpen.name != "CharacterSettingPanel")
            return panelsToClose;

        int keepCount = 0;

        for (int i = 0; i < panelsToClose.Length; i++)
        {
            GameObject panel = panelsToClose[i];

            if (panel != null && panel.name != "SettingButton")
                keepCount++;
        }

        if (keepCount == panelsToClose.Length)
            return panelsToClose;

        GameObject[] filtered = new GameObject[keepCount];
        int index = 0;

        for (int i = 0; i < panelsToClose.Length; i++)
        {
            GameObject panel = panelsToClose[i];

            if (panel == null || panel.name == "SettingButton")
                continue;

            filtered[index++] = panel;
        }

        return filtered;
    }


    /// <summary>
    /// CharacterSettingPanel을 열 때는 World Objects To Close에 SettingButton이
    /// 등록되어 있어도 비활성화하지 않습니다.
    /// </summary>
    private GameObject[] GetEffectiveWorldObjectsToClose()
    {
        if (worldObjectsToClose == null || worldObjectsToClose.Length == 0)
            return worldObjectsToClose;

        if (panelToOpen == null || panelToOpen.name != "CharacterSettingPanel")
            return worldObjectsToClose;

        int keepCount = 0;

        for (int i = 0; i < worldObjectsToClose.Length; i++)
        {
            GameObject target = worldObjectsToClose[i];

            if (target != null && target.name != "SettingButton")
                keepCount++;
        }

        if (keepCount == worldObjectsToClose.Length)
            return worldObjectsToClose;

        GameObject[] filtered = new GameObject[keepCount];
        int index = 0;

        for (int i = 0; i < worldObjectsToClose.Length; i++)
        {
            GameObject target = worldObjectsToClose[i];

            if (target == null || target.name == "SettingButton")
                continue;

            filtered[index++] = target;
        }

        return filtered;
    }


    /// <summary>
    /// 캐릭터 설정 화면에서는 SettingButton을 항상 활성 상태로 유지합니다.
    /// 인스펙터의 닫기 목록에 잘못 포함되어 있어도 마지막 단계에서 복구합니다.
    /// </summary>
    private void KeepSettingButtonActive()
    {
        GameObject settingButton = FindSceneObject("SettingButton");

        if (settingButton != null)
            settingButton.SetActive(true);
    }

    private void ResolveLobbyBackgrounds()
    {
        if (positionBackground == null)
            positionBackground = FindSceneObject("Position_Back");

        if (characterSettingBackground == null)
        {
            characterSettingBackground =
                FindSceneObject("CharacterSetting_Back");
        }

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

        if (!TryGetTargetLobbyBackgroundState(
                out LobbyBackgroundState targetState))
        {
            return;
        }

        LobbyBackgroundStateController controller =
            ResolveBackgroundStateController();

        if (controller != null)
        {
            controller.ShowBackground(targetState);
            return;
        }

        ApplyLobbyBackgroundFallback(targetState);
    }

    private bool TryGetTargetLobbyBackgroundState(
        out LobbyBackgroundState state)
    {
        if (transitionMode == PanelTransitionMode.CharacterToLobby)
        {
            state = LobbyBackgroundState.Position;
            return true;
        }

        if (panelToOpen == null)
        {
            state = LobbyBackgroundState.Position;
            return true;
        }

        switch (panelToOpen.name)
        {
            case "PositionPanel":
                state = LobbyBackgroundState.Position;
                return true;

            case "CharacterSettingPanel":
                state = LobbyBackgroundState.CharacterSetting;
                return true;

            case "ErosionSelectPanel":
                state = LobbyBackgroundState.ErosionSelect;
                return true;

            case "RelicShopPanel":
                state = LobbyBackgroundState.RelicShop;
                return true;

            case "CultureTankPanel":
                state = LobbyBackgroundState.CultureTank;
                return true;

            default:
                state = LobbyBackgroundState.Position;
                return false;
        }
    }

    private LobbyBackgroundStateController ResolveBackgroundStateController()
    {
        if (backgroundStateController != null)
            return backgroundStateController;

        backgroundStateController =
            FindFirstObjectByType<LobbyBackgroundStateController>(
                FindObjectsInactive.Include);

        return backgroundStateController;
    }

    private void ApplyLobbyBackgroundFallback(
        LobbyBackgroundState targetState)
    {
        ResolveLobbyBackgrounds();

        GameObject targetBackground =
            GetBackgroundForState(targetState);

        if (targetBackground == null)
            return;

        SetBackgroundActive(
            positionBackground,
            targetBackground);

        SetBackgroundActive(
            characterSettingBackground,
            targetBackground);

        SetBackgroundActive(
            erosionSelectBackground,
            targetBackground);

        SetBackgroundActive(
            relicShopBackground,
            targetBackground);

        SetBackgroundActive(
            cultureTankBackground,
            targetBackground);
    }

    private GameObject GetBackgroundForState(
        LobbyBackgroundState state)
    {
        switch (state)
        {
            case LobbyBackgroundState.Position:
                return positionBackground;

            case LobbyBackgroundState.CharacterSetting:
                return characterSettingBackground;

            case LobbyBackgroundState.ErosionSelect:
                return erosionSelectBackground;

            case LobbyBackgroundState.RelicShop:
                return relicShopBackground;

            case LobbyBackgroundState.CultureTank:
                return cultureTankBackground;

            default:
                return null;
        }
    }

    private static void SetBackgroundActive(
        GameObject background,
        GameObject targetBackground)
    {
        if (background != null)
            background.SetActive(background == targetBackground);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        GameObject[] objects =
            FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];

            if (candidate != null &&
                candidate.name == objectName)
            {
                return candidate;
            }
        }

        return null;
    }

    private void EnsureWorldCollider()
    {
        if (GetComponent<Collider2D>() != null ||
            GetComponent<Collider>() != null)
        {
            return;
        }

        if (GetComponent<SpriteRenderer>() == null)
        {
            Debug.LogWarning(
                "[LobbyPanelTransitionButton] 월드 클릭을 사용하지만 " +
                "Collider와 SpriteRenderer가 없습니다. " +
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
