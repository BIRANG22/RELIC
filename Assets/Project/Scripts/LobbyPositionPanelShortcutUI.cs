using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BackgroundPanel/Lobby_Icon의 아이콘 버튼과 PositionPanel 패널을 Inspector에서 연결해
/// 월드 오브젝트를 다시 클릭하지 않고도 공용 모달 사이를 전환합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyPositionPanelShortcutUI : MonoBehaviour
{
    [Serializable]
    private sealed class ShortcutEntry
    {
        [Tooltip("클릭할 Lobby_Icon 버튼입니다. 예: Icon_01")]
        public Button iconButton;

        [Tooltip("이 아이콘으로 열 PositionPanel의 패널입니다. 예: ErosionSelectPanel")]
        public GameObject targetPanel;

        [Tooltip("Mainicon/Icon에 표시할 원본 이미지입니다. 예: Icon_01/Icon")]
        public Image iconImage;

        [Tooltip("Mainicon/MainText에 표시할 패널 이름입니다.")]
        public string displayName;
    }

    [Header("Lobby Icon Shortcuts")]
    [SerializeField] private ShortcutEntry[] shortcuts = Array.Empty<ShortcutEntry>();

    [Header("Current Selection Display")]
    [Tooltip("현재 선택된 Shortcut의 Sprite를 표시할 Mainicon/Icon 이미지입니다.")]
    [SerializeField] private Image mainIconImage;
    [Tooltip("현재 선택된 패널 이름을 표시할 Mainicon/MainText입니다.")]
    [SerializeField] private TMP_Text mainText;

    private readonly Dictionary<Button, UnityAction> boundListeners = new();

    private void OnEnable()
    {
        BindButtons();
        RefreshMainDisplayFromCurrentPanel();
    }

    private void OnDisable()
    {
        UnbindButtons();
    }

    private void BindButtons()
    {
        UnbindButtons();

        if (shortcuts == null)
            return;

        for (int i = 0; i < shortcuts.Length; i++)
        {
            ShortcutEntry entry = shortcuts[i];
            if (entry == null || entry.iconButton == null)
                continue;

            GameObject targetPanel = entry.targetPanel;
            UnityAction listener = () => OpenTargetPanel(targetPanel);
            entry.iconButton.onClick.AddListener(listener);
            boundListeners[entry.iconButton] = listener;
        }
    }

    private void UnbindButtons()
    {
        foreach (KeyValuePair<Button, UnityAction> pair in boundListeners)
        {
            if (pair.Key != null)
                pair.Key.onClick.RemoveListener(pair.Value);
        }

        boundListeners.Clear();
    }

    private void OpenTargetPanel(GameObject targetPanel)
    {
        if (targetPanel == null)
            return;

        if (LobbyPositionSharedModalBackground.IsPanelPresented(targetPanel))
            return;

        if (!LobbyPositionSharedModalBackground.PrepareForPanelSwitch(targetPanel))
            return;

        if (TryOpenErosionPanel(targetPanel))
        {
            ApplyMainDisplay(targetPanel);
            return;
        }

        if (TryOpenRelicShopPanel(targetPanel))
        {
            ApplyMainDisplay(targetPanel);
            return;
        }

        if (TryOpenCultureTankPanel(targetPanel))
        {
            ApplyMainDisplay(targetPanel);
            return;
        }

        OpenFallbackPanel(targetPanel);
        ApplyMainDisplay(targetPanel);
    }


    private void RefreshMainDisplayFromCurrentPanel()
    {
        LobbyPositionSharedModalBackground controller =
            FindFirstObjectByType<LobbyPositionSharedModalBackground>(FindObjectsInactive.Include);

        if (controller == null || controller.ActivePanel == null)
            return;

        ApplyMainDisplay(controller.ActivePanel);
    }

    /// <summary>
    /// 월드 오브젝트 또는 Lobby_Icon 어느 경로로 패널을 열어도
    /// 해당 Shortcut의 Inspector Display Name과 Icon을 공용 Mainicon에 반영합니다.
    /// </summary>
    public static void RefreshForPanel(GameObject targetPanel)
    {
        if (targetPanel == null)
            return;

        LobbyPositionPanelShortcutUI[] shortcutUis =
            FindObjectsByType<LobbyPositionPanelShortcutUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int i = 0; i < shortcutUis.Length; i++)
        {
            if (shortcutUis[i] != null)
                shortcutUis[i].ApplyMainDisplay(targetPanel);
        }
    }

    private void ApplyMainDisplay(GameObject targetPanel)
    {
        if (targetPanel == null || shortcuts == null)
            return;

        for (int i = 0; i < shortcuts.Length; i++)
        {
            ShortcutEntry entry = shortcuts[i];
            if (entry == null || entry.targetPanel != targetPanel)
                continue;

            if (mainIconImage != null)
            {
                Image sourceImage = entry.iconImage;
                if (sourceImage == null && entry.iconButton != null)
                {
                    Transform iconTransform = entry.iconButton.transform.Find("Icon");
                    if (iconTransform != null)
                        sourceImage = iconTransform.GetComponent<Image>();
                }

                if (sourceImage != null)
                    mainIconImage.sprite = sourceImage.sprite;
            }

            if (mainText != null)
                mainText.text = entry.displayName ?? string.Empty;

            return;
        }
    }

    private static bool TryOpenErosionPanel(GameObject targetPanel)
    {
        LobbyErosionMirrorButton[] controllers = FindObjectsByType<LobbyErosionMirrorButton>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < controllers.Length; i++)
        {
            LobbyErosionMirrorButton controller = controllers[i];
            if (controller == null || !controller.ControlsPanel(targetPanel))
                continue;

            controller.OpenErosionSelectPanel();
            return true;
        }

        return false;
    }

    private static bool TryOpenRelicShopPanel(GameObject targetPanel)
    {
        LobbyRelicShopPresenter[] presenters = FindObjectsByType<LobbyRelicShopPresenter>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < presenters.Length; i++)
        {
            LobbyRelicShopPresenter presenter = presenters[i];
            if (presenter == null || !presenter.ControlsPanel(targetPanel))
                continue;

            presenter.Open();
            return true;
        }

        return false;
    }

    private static bool TryOpenCultureTankPanel(GameObject targetPanel)
    {
        LobbyCultureTankPanelPresenter[] presenters = FindObjectsByType<LobbyCultureTankPanelPresenter>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < presenters.Length; i++)
        {
            LobbyCultureTankPanelPresenter presenter = presenters[i];
            if (presenter == null || !presenter.ControlsPanel(targetPanel))
                continue;

            presenter.Open();
            return true;
        }

        return false;
    }

    private void OpenFallbackPanel(GameObject targetPanel)
    {
        LobbyPositionModalInputBlocker.Block(this);
        LobbyPositionSharedModalBackground.ShowForPanel(
            targetPanel,
            this,
            () => CloseFallbackPanel(targetPanel));
        targetPanel.SetActive(true);
    }

    private void CloseFallbackPanel(GameObject targetPanel)
    {
        if (targetPanel != null)
            targetPanel.SetActive(false);

        LobbyPositionSharedModalBackground.HideForOwner(this);
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void OnDestroy()
    {
        LobbyPositionSharedModalBackground.HideForOwner(this);
        LobbyPositionModalInputBlocker.Unblock(this);
    }
}
