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
    [Tooltip("Panel To Open�� �´� �κ� ������� �ڵ� ��ȯ�մϴ�.")]
    [SerializeField] private bool changeLobbyBackground = true;

    [Tooltip("��� ������ ������Ʈ �̸����� �ڵ� Ž���մϴ�.")]
    [SerializeField] private GameObject positionBackground;
    [SerializeField] private GameObject characterSettingBackground;
    [SerializeField] private GameObject erosionSelectBackground;
    [SerializeField] private GameObject relicShopBackground;
    [SerializeField] private GameObject cultureTankBackground;

    [Header("Opened Popup Close")]
    [SerializeField] private bool closeCurrentUIPanelButtonPanelOnExecute = true;
    [SerializeField] private GameObject[] extraPanelsToCloseOnExecute;

    [Header("World Object Click")]
    [Tooltip("üũ�ϸ� �� ��ũ��Ʈ�� ���� ���� ������Ʈ�� ���콺 ��Ŭ������ �� Execute�� �����մϴ�.")]
    [SerializeField] private bool executeOnWorldClick = true;

    [Tooltip("UI ������ Ŭ������ �� ���� ������Ʈ Ŭ���� �����ϴ�.")]
    [SerializeField] private bool blockWorldClickOverUI = true;

    [Tooltip("Collider2D�� ���� �� SpriteRenderer �������� PolygonCollider2D�� �ڵ� �߰��մϴ�.")]
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
    [SerializeField, SoundId(SoundCategory.Sfx)] private string hoverSfx = AudioIds.Sfx.NormalButtonHover;
    [SerializeField] private float hoverSfxVolumeMultiplier = 1f;

    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;
    [SerializeField] private float clickSfxVolumeMultiplier = 1f;

    private LobbyBackgroundStateController backgroundStateController;
    private bool isProcessing;

    /// <summary>
    /// �� ��ư�� ������ Panel To Open�� ���� ���� �ִ��� ��ȯ�մϴ�.
    /// SpriteHoverScale���� ���� ǥ�ø� ������ �� ����մϴ�.
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
    /// MenuPanel�� ���� ������ �޴� �ٱ��� �κ� ���� ������Ʈ �Է��� �����մϴ�.
    /// MenuPanel �ڽŰ� �� �ڽ� ��ư�� ��� ����� �� �ֽ��ϴ�.
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
    /// CharacterSettingPanel�� �� ���� �κ��� SettingButton�� �����մϴ�.
    /// ���� �ν������� Panels To Close�� SettingButton�� ��� �־
    /// ĳ���� ���� ȭ������ ��ȯ�� ���� �ڵ����� �����մϴ�.
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
    /// CharacterSettingPanel�� �� ���� World Objects To Close�� SettingButton��
    /// ��ϵǾ� �־ ��Ȱ��ȭ���� �ʽ��ϴ�.
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
    /// ĳ���� ���� ȭ�鿡���� SettingButton�� �׻� Ȱ�� ���·� �����մϴ�.
    /// �ν������� �ݱ� ��Ͽ� �߸� ���ԵǾ� �־ ������ �ܰ迡�� �����մϴ�.
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
                "[LobbyPanelTransitionButton] ���� Ŭ���� ��������� " +
                "Collider�� SpriteRenderer�� �����ϴ�. " +
                "Anchor ������Ʈ�� Collider2D�� ���� �߰��ϼ���.",
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
