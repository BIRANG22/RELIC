using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

    private const float LobbyChildIconHoverSelectedScale = 1.1f;

    [Header("Lobby Icon Selection Scale")]
    [Tooltip("선택된 Icon_01~05 버튼 자체의 확대 배율입니다.")]
    [SerializeField] private float selectedButtonScale = 1.2f;
    [Tooltip("Lobby_Icon의 선택/호버 스케일 전환에 걸리는 시간(초)입니다.")]
    [SerializeField] private float iconScaleDuration = 0.15f;

    private readonly Dictionary<Button, UnityAction> boundListeners = new();
    private readonly Dictionary<Transform, Vector3> buttonBaseScales = new();
    private readonly Dictionary<Transform, Vector3> iconBaseScales = new();
    private readonly Dictionary<Button, LobbyPositionShortcutHoverRelay> hoverRelays = new();
    private readonly HashSet<Button> hoveredButtons = new();
    private readonly Dictionary<Transform, Coroutine> iconScaleRoutines = new();
    private Coroutine deferredMainDisplayRoutine;
    private GameObject selectedTargetPanel;

    private void OnEnable()
    {
        CacheIconBaseScales();
        BindButtons();
        BindHoverRelays();
        RefreshMainDisplayFromCurrentPanel();
    }

    private void OnDisable()
    {
        if (deferredMainDisplayRoutine != null)
        {
            StopCoroutine(deferredMainDisplayRoutine);
            deferredMainDisplayRoutine = null;
        }

        StopAllIconScaleRoutines();
        hoveredButtons.Clear();
        ResetAllIconScalesImmediate();
        UnbindHoverRelays();
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

        if (TryOpenCharacterSettingPanel(targetPanel))
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
            LobbyPositionPanelShortcutUI shortcutUi = shortcutUis[i];
            if (shortcutUi == null)
                continue;

            shortcutUi.ApplyMainDisplay(targetPanel);
            shortcutUi.ScheduleDeferredMainDisplay(targetPanel);
        }
    }

    private void ScheduleDeferredMainDisplay(GameObject targetPanel)
    {
        if (!isActiveAndEnabled || targetPanel == null)
            return;

        if (deferredMainDisplayRoutine != null)
            StopCoroutine(deferredMainDisplayRoutine);

        deferredMainDisplayRoutine = StartCoroutine(ApplyMainDisplayNextFrame(targetPanel));
    }

    private IEnumerator ApplyMainDisplayNextFrame(GameObject targetPanel)
    {
        yield return null;
        deferredMainDisplayRoutine = null;

        if (targetPanel != null)
            ApplyMainDisplay(targetPanel);
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
                {
                    mainIconImage.sprite = sourceImage.sprite;
                    mainIconImage.color = sourceImage.color;
                }
            }

            if (mainText != null)
                mainText.text = entry.displayName ?? string.Empty;

            selectedTargetPanel = targetPanel;
            AnimateIconSelection(targetPanel);
            return;
        }
    }

    private void CacheIconBaseScales()
    {
        if (shortcuts == null)
            return;

        for (int i = 0; i < shortcuts.Length; i++)
        {
            ShortcutEntry entry = shortcuts[i];
            if (entry == null)
                continue;

            Transform buttonTransform = entry.iconButton != null
                ? entry.iconButton.transform
                : null;
            if (buttonTransform != null && !buttonBaseScales.ContainsKey(buttonTransform))
                buttonBaseScales.Add(buttonTransform, buttonTransform.localScale);

            Transform iconTransform = GetShortcutChildIconTransform(entry);
            if (iconTransform != null && !iconBaseScales.ContainsKey(iconTransform))
                iconBaseScales.Add(iconTransform, iconTransform.localScale);
        }
    }

    private void AnimateIconSelection(GameObject targetPanel)
    {
        selectedTargetPanel = targetPanel;
        RefreshAllIconScales(true);
    }

    private void RefreshAllIconScales(bool animate)
    {
        CacheIconBaseScales();

        if (shortcuts == null)
            return;

        for (int i = 0; i < shortcuts.Length; i++)
        {
            ShortcutEntry entry = shortcuts[i];
            if (entry == null)
                continue;

            bool selected = entry.targetPanel == selectedTargetPanel;
            bool hovered = entry.iconButton != null && hoveredButtons.Contains(entry.iconButton);

            // 기존 효과: Icon_01~05 버튼 자체는 선택된 동안 1.2배를 유지합니다.
            Transform buttonTransform = entry.iconButton != null
                ? entry.iconButton.transform
                : null;
            if (buttonTransform != null)
            {
                if (!buttonBaseScales.TryGetValue(buttonTransform, out Vector3 buttonBaseScale))
                {
                    buttonBaseScale = buttonTransform.localScale;
                    buttonBaseScales[buttonTransform] = buttonBaseScale;
                }

                Vector3 buttonTargetScale = selected
                    ? buttonBaseScale * selectedButtonScale
                    : buttonBaseScale;
                StartIconScaleTransition(buttonTransform, buttonTargetScale, animate);
            }

            // 추가 효과: 자식 Icon은 호버 또는 선택 상태에서 1.1배를 유지합니다.
            Transform iconTransform = GetShortcutChildIconTransform(entry);
            if (iconTransform == null)
                continue;

            if (!iconBaseScales.TryGetValue(iconTransform, out Vector3 iconBaseScale))
            {
                iconBaseScale = iconTransform.localScale;
                iconBaseScales[iconTransform] = iconBaseScale;
            }

            Vector3 iconTargetScale = (selected || hovered)
                ? iconBaseScale * LobbyChildIconHoverSelectedScale
                : iconBaseScale;
            StartIconScaleTransition(iconTransform, iconTargetScale, animate);
        }
    }

    private void StartIconScaleTransition(Transform iconTransform, Vector3 targetScale, bool animate)
    {
        if (iconTransform == null)
            return;

        if (iconScaleRoutines.TryGetValue(iconTransform, out Coroutine running) && running != null)
            StopCoroutine(running);

        iconScaleRoutines.Remove(iconTransform);

        if (!animate || !isActiveAndEnabled || !gameObject.activeInHierarchy || iconScaleDuration <= 0f)
        {
            iconTransform.localScale = targetScale;
            return;
        }

        Coroutine routine = StartCoroutine(AnimateIconScaleRoutine(iconTransform, targetScale));
        iconScaleRoutines[iconTransform] = routine;
    }

    private IEnumerator AnimateIconScaleRoutine(Transform iconTransform, Vector3 targetScale)
    {
        Vector3 startScale = iconTransform != null ? iconTransform.localScale : targetScale;
        float duration = Mathf.Max(0.0001f, iconScaleDuration);
        float elapsed = 0f;

        while (iconTransform != null && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            iconTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
            yield return null;
        }

        if (iconTransform != null)
            iconTransform.localScale = targetScale;

        if (iconTransform != null)
            iconScaleRoutines.Remove(iconTransform);
    }

    private void BindHoverRelays()
    {
        UnbindHoverRelays();

        if (shortcuts == null)
            return;

        for (int i = 0; i < shortcuts.Length; i++)
        {
            ShortcutEntry entry = shortcuts[i];
            if (entry == null || entry.iconButton == null)
                continue;

            Button button = entry.iconButton;
            LobbyPositionShortcutHoverRelay relay = button.GetComponent<LobbyPositionShortcutHoverRelay>();
            if (relay == null)
                relay = button.gameObject.AddComponent<LobbyPositionShortcutHoverRelay>();

            relay.Configure(
                () => HandleShortcutHoverChanged(button, true),
                () => HandleShortcutHoverChanged(button, false));
            hoverRelays[button] = relay;
        }
    }

    private void UnbindHoverRelays()
    {
        foreach (KeyValuePair<Button, LobbyPositionShortcutHoverRelay> pair in hoverRelays)
        {
            if (pair.Value != null)
                pair.Value.Configure(null, null);
        }

        hoverRelays.Clear();
    }

    private void HandleShortcutHoverChanged(Button button, bool hovering)
    {
        if (button == null)
            return;

        if (hovering)
            hoveredButtons.Add(button);
        else
            hoveredButtons.Remove(button);

        // Lobby_Icon 비활성화 과정에서는 자식 HoverRelay.OnDisable이 먼저 호출될 수 있습니다.
        // 이 시점에는 코루틴을 시작하지 않고 현재 선택 상태에 맞춰 즉시 정리합니다.
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            RefreshAllIconScales(false);
            return;
        }

        RefreshAllIconScales(true);
    }

    private void StopAllIconScaleRoutines()
    {
        foreach (Coroutine routine in iconScaleRoutines.Values)
        {
            if (routine != null)
                StopCoroutine(routine);
        }

        iconScaleRoutines.Clear();
    }

    private void ResetAllIconScalesImmediate()
    {
        StopAllIconScaleRoutines();

        foreach (KeyValuePair<Transform, Vector3> pair in buttonBaseScales)
        {
            if (pair.Key != null)
                pair.Key.localScale = pair.Value;
        }

        foreach (KeyValuePair<Transform, Vector3> pair in iconBaseScales)
        {
            if (pair.Key != null)
                pair.Key.localScale = pair.Value;
        }
    }

    private static Transform GetShortcutChildIconTransform(ShortcutEntry entry)
    {
        if (entry == null)
            return null;

        if (entry.iconImage != null)
            return entry.iconImage.transform;

        if (entry.iconButton != null)
            return entry.iconButton.transform.Find("Icon");

        return null;
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

    /// <summary>
    /// CharacterSettingPanel은 Setting 컴포넌트가 공용 BackgroundPanel과 입력 차단의
    /// 소유자가 되도록 직접 활성화합니다. ShortcutUI가 임시 소유자가 되면
    /// Setting.OnEnable에서 소유권이 덮어써져 닫을 때 입력 차단이 남을 수 있습니다.
    /// </summary>
    private static bool TryOpenCharacterSettingPanel(GameObject targetPanel)
    {
        if (targetPanel == null)
            return false;

        Setting setting = targetPanel.GetComponent<Setting>();
        if (setting == null)
            return false;

        TitleManager.CloseTitleModePanelsExceptInScene(targetPanel);

        if (!targetPanel.activeSelf)
            targetPanel.SetActive(true);

        return true;
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


public sealed class LobbyPositionShortcutHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Action onEnter;
    private Action onExit;

    public void Configure(Action pointerEnter, Action pointerExit)
    {
        onEnter = pointerEnter;
        onExit = pointerExit;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onExit?.Invoke();
    }

    private void OnDisable()
    {
        onExit?.Invoke();
    }
}
