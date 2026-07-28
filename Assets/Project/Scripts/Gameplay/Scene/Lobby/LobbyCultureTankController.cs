using System;
using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum LobbyCultureTankPanelState
{
    MissingData,
    Empty,
    Running,
    Completed
}

[DisallowMultipleComponent]
public sealed class LobbyCultureTankController : MonoBehaviour
{
    private static readonly Color RunningLightStartColor = new(250f / 255f, 100f / 255f, 0f, 1f);
    private static readonly Color RunningLightEndColor = new(100f / 255f, 250f / 255f, 150f / 255f, 1f);

    [SerializeField] private string tankId;
    [SerializeField] private bool allowWorldInteraction;
    [SerializeField] private SpriteRenderer itemRenderer;
    [SerializeField] private Light2D tankLight;
    [SerializeField] private GameObject researchVfxRoot;
    [SerializeField] private TextMeshPro statusText;
    [SerializeField] private TextMeshPro claimFeedbackText;
    [SerializeField] private BattleBagPanelUI bagPanel;
    [SerializeField] private UIPanelButton bagPanelButton;
    [SerializeField] private Vector3 statusTextLocalPosition = new(0f, -0.9f, 0f);
    [SerializeField] private Vector3 claimFeedbackTextLocalPosition = new(0f, -0.45f, 0f);
    [SerializeField] private float statusTextFontSize = 3.2f;
    [SerializeField] private float claimFeedbackTextFontSize = 3.4f;
    [SerializeField] private int statusTextSortingOrder = 20;
    [SerializeField] private int claimFeedbackTextSortingOrder = 21;
    [SerializeField] private float claimFeedbackDurationSeconds = 1.1f;

    private int lastRemainingSeconds = -1;
    private bool closeBagPanelAfterSelection;
    private Coroutine claimFeedbackRoutine;

    private void Awake()
    {
        AutoBind();
        EnsureWorldSpriteCollider();
    }

    private void OnEnable()
    {
        RefreshVisuals(true);
    }

    private void Update()
    {
        RefreshVisuals(false);
    }

    private void OnDisable()
    {
        if (LobbyPositionModalInputBlocker.IsBlockedBy(this))
            LobbyPositionModalInputBlocker.Unblock(this);

        CloseBagPanelAfterSelectionIfNeeded();

        if (claimFeedbackRoutine != null)
        {
            StopCoroutine(claimFeedbackRoutine);
            claimFeedbackRoutine = null;
        }

        if (claimFeedbackText != null)
            claimFeedbackText.gameObject.SetActive(false);
    }

    public string TankId
    {
        get
        {
            AutoBind();
            return tankId;
        }
    }

    public void Interact()
    {
        if (ShouldBlockClick())
            return;

        if (!CanLocalPlayerMutateHostOnlyState())
            return;

        HandleInteraction();
    }

    public string GetPanelLabel()
    {
        AutoBind();

        string title = FormatPanelTankName(tankId);
        LobbyCultureTankPanelState state = GetPanelState();

        return state switch
        {
            LobbyCultureTankPanelState.Completed => $"{title}\n\uC644\uB8CC",
            LobbyCultureTankPanelState.Running => $"{title}\n\uBC30\uC591\uC911 {GetPanelRemainingSeconds()}s",
            LobbyCultureTankPanelState.MissingData => $"{title}\n\uB370\uC774\uD130 \uC5C6\uC74C",
            _ => $"{title}\n\uBE44\uC5B4 \uC788\uC74C"
        };
    }

    public LobbyCultureTankPanelState GetPanelState()
    {
        AutoBind();

        if (DataManager.Instance == null)
            return LobbyCultureTankPanelState.MissingData;

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore?.GetOrCreate();
        if (lobby == null)
            return LobbyCultureTankPanelState.MissingData;

        if (!CultureTankResearchService.TryGetTank(lobby, tankId, out CultureTankResearchRuntimeData tank))
            return LobbyCultureTankPanelState.Empty;

        if (tank.IsCompleted || CultureTankResearchService.GetRemainingSeconds(tank, DateTime.UtcNow.Ticks) <= 0)
            return LobbyCultureTankPanelState.Completed;

        return LobbyCultureTankPanelState.Running;
    }

    public void RefreshNow()
    {
        RefreshVisuals(true);
    }

    private int GetPanelRemainingSeconds()
    {
        LobbyRuntimeData lobby = DataManager.Instance != null
            ? DataManager.Instance.LobbyRuntimeStore?.GetOrCreate()
            : null;

        if (!CultureTankResearchService.TryGetTank(lobby, tankId, out CultureTankResearchRuntimeData tank))
            return 0;

        return CultureTankResearchService.GetRemainingSeconds(tank, DateTime.UtcNow.Ticks);
    }

    private static string FormatPanelTankName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "\uBC30\uC591\uC870";

        string trimmed = value.Trim();
        const string prefix = "CultureTank";
        if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            string suffix = trimmed[prefix.Length..].Trim();
            return string.IsNullOrWhiteSpace(suffix) ? "\uBC30\uC591\uC870" : $"\uBC30\uC591\uC870 {suffix}";
        }

        return trimmed;
    }

    private void OnMouseUpAsButton()
    {
        if (!allowWorldInteraction)
            return;

        Interact();
    }

    private void HandleInteraction()
    {
        if (ShouldBlockClick())
            return;

        AutoBind();

        if (DataManager.Instance == null)
            return;

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore?.GetOrCreate();
        if (lobby == null)
            return;

        long nowTicks = DateTime.UtcNow.Ticks;

        if (CultureTankResearchService.TryGetTank(lobby, tankId, out CultureTankResearchRuntimeData tank))
        {
            bool changed = CultureTankResearchService.RefreshCompletion(tank, nowTicks);
            if (changed && CanLocalPlayerMutateHostOnlyState())
            {
                SaveProgress();
                PublishHostSnapshotAfterLocalMutation();
            }

            RefreshVisuals(true);

            if (tank.IsCompleted)
                ClaimCompletedResearch(lobby, tank, nowTicks);
            else
                BattleWarningUI.ShowMessage("?�직 배양 중입?�다.");

            return;
        }

        OpenItemSelection(lobby);
    }

    private bool ShouldBlockClick()
    {
        if (LobbyPositionModalInputBlocker.IsBlockedByAnother(this))
            return true;

        if (SkillUpgradePanel.IsAnyPanelOpen)
            return true;

        return UIPanelButton.IsMenuPanelOpen;
    }

    private void OpenItemSelection(LobbyRuntimeData lobby)
    {
        if (lobby == null)
            return;

        if (lobby.BagItemIds == null || lobby.BagItemIds.Count <= 0)
        {
            BattleWarningUI.ShowMessage("배양??고유?�이?�이 ?�습?�다.");
            return;
        }

        if (bagPanel == null)
            bagPanel = FindFirstObjectByType<BattleBagPanelUI>(FindObjectsInactive.Include);

        if (bagPanel == null)
        {
            BattleWarningUI.ShowMessage("가�??�널??찾을 ???�습?�다.");
            return;
        }

        LobbyPositionModalInputBlocker.Block(this);
        bagPanel.OpenForItemSelection(StartResearchWithSelectedItem, ReleaseSelectionAndCloseBagPanel);
        OpenBagPanelForSelection();
    }

    private void StartResearchWithSelectedItem(string itemId)
    {
        ReleaseSelectionBlock();

        if (DataManager.Instance == null)
            return;

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore?.GetOrCreate();
        if (lobby == null)
            return;

        if (!CultureTankResearchService.TryStartResearch(
                lobby,
                tankId,
                itemId,
                DateTime.UtcNow.Ticks,
                out string error))
        {
            BattleWarningUI.ShowMessage("배양???�작?????�습?�다.");
            Debug.LogWarning($"[LobbyCultureTankController] Failed to start research. {error}");
            return;
        }

        SaveProgress();
        BattleBagPanelUI.RefreshAll();
        RefreshVisuals(true);
        PublishHostSnapshotAfterLocalMutation();
    }

    private void ClaimCompletedResearch(
        LobbyRuntimeData lobby,
        CultureTankResearchRuntimeData tank,
        long nowTicks)
    {
        if (lobby == null || tank == null || DataManager.Instance == null)
            return;

        ItemData item = DataManager.Instance.ItemDatabase.Get(tank.ItemId);

        if (!CultureTankResearchService.TryClaimCompletedResearch(
                lobby,
                item,
                tankId,
                nowTicks,
                out _,
                out string error))
        {
            BattleWarningUI.ShowMessage("배양 결과�?받을 ???�습?�다.");
            Debug.LogWarning($"[LobbyCultureTankController] Failed to claim research. {error}");
            return;
        }

        SaveProgress();
        ClearTankVisuals();
        ShowClaimFeedback();
        BattleWarningUI.ShowMessage("배양 결과 버프�??�보?�습?�다.");
        PublishHostSnapshotAfterLocalMutation();
    }

    private void RefreshVisuals(bool force)
    {
        AutoBind();

        LobbyRuntimeData lobby = DataManager.Instance != null
            ? DataManager.Instance.LobbyRuntimeStore?.GetOrCreate()
            : null;

        if (lobby == null ||
            !CultureTankResearchService.TryGetTank(lobby, tankId, out CultureTankResearchRuntimeData tank))
        {
            ClearTankVisuals();
            return;
        }

        long nowTicks = DateTime.UtcNow.Ticks;
        bool completedNow = false;
        if (CanLocalPlayerMutateHostOnlyState())
        {
            completedNow = CultureTankResearchService.RefreshCompletion(tank, nowTicks);

            if (completedNow)
            {
                SaveProgress();
                PublishHostSnapshotAfterLocalMutation();
            }
        }

        bool completedForDisplay =
            tank.IsCompleted ||
            CultureTankResearchService.GetRemainingSeconds(tank, nowTicks) <= 0;

        if (completedForDisplay)
        {
            SetCompletedVisual(tank.ItemId);
            SetResearchObjects(false);
            SetStatusText("?�료", true);
            lastRemainingSeconds = -1;
            return;
        }

        SetRunningVisual(tank.ItemId);
        SetResearchObjects(true);

        int remainingSeconds = CultureTankResearchService.GetRemainingSeconds(tank, nowTicks);
        if (force || remainingSeconds != lastRemainingSeconds)
        {
            SetStatusText($"배양�?{remainingSeconds}s", true);
            lastRemainingSeconds = remainingSeconds;
        }

        AnimateRunningLight();
    }

    private void ClearTankVisuals()
    {
        SetItemSprite(null);
        SetResearchObjects(false);
        SetStatusText(string.Empty, false);
        lastRemainingSeconds = -1;
    }

    private void SetRunningVisual(string itemId)
    {
        Sprite icon = null;

        if (DataManager.Instance?.ItemIconDatabase != null)
            DataManager.Instance.ItemIconDatabase.TryGetIcon(itemId, out icon);

        SetItemSprite(icon);
    }

    private void SetCompletedVisual(string itemId)
    {
        Sprite icon = null;

        if (DataManager.Instance?.ItemIconDatabase != null)
        {
            if (!DataManager.Instance.ItemIconDatabase.TryGetResearchResultIcon(itemId, out icon))
                DataManager.Instance.ItemIconDatabase.TryGetIcon(itemId, out icon);
        }

        SetItemSprite(icon);

        if (tankLight != null)
        {
            tankLight.enabled = true;
            tankLight.color = RunningLightEndColor;
        }
    }

    private void SetItemSprite(Sprite sprite)
    {
        if (itemRenderer == null)
            return;

        itemRenderer.sprite = sprite;
        itemRenderer.enabled = sprite != null;
    }

    private void SetResearchObjects(bool running)
    {
        if (researchVfxRoot != null)
            researchVfxRoot.SetActive(running);

        if (tankLight != null)
        {
            tankLight.enabled = running || HasCompletedResearch();

            if (!tankLight.enabled)
                tankLight.color = RunningLightStartColor;
        }
    }

    private void AnimateRunningLight()
    {
        if (tankLight == null)
            return;

        tankLight.enabled = true;
        float t = Mathf.PingPong(Time.time, 1f);
        tankLight.color = Color.Lerp(RunningLightStartColor, RunningLightEndColor, t);
    }

    private bool HasCompletedResearch()
    {
        LobbyRuntimeData lobby = DataManager.Instance != null
            ? DataManager.Instance.LobbyRuntimeStore?.GetOrCreate()
            : null;

        return CultureTankResearchService.TryGetTank(lobby, tankId, out CultureTankResearchRuntimeData tank) &&
               tank.IsCompleted;
    }

    private void SetStatusText(string text, bool visible)
    {
        if (statusText == null)
            return;

        statusText.text = text;
        statusText.gameObject.SetActive(visible && !string.IsNullOrWhiteSpace(text));
    }

    private void ShowClaimFeedback()
    {
        if (claimFeedbackText == null)
            claimFeedbackText = FindOrCreateClaimFeedbackText();

        if (claimFeedbackText == null)
            return;

        if (claimFeedbackRoutine != null)
            StopCoroutine(claimFeedbackRoutine);

        claimFeedbackRoutine = StartCoroutine(ShowClaimFeedbackRoutine());
    }

    private IEnumerator ShowClaimFeedbackRoutine()
    {
        claimFeedbackText.text = "버프 ?�보";
        claimFeedbackText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, claimFeedbackDurationSeconds));

        if (claimFeedbackText != null)
            claimFeedbackText.gameObject.SetActive(false);

        claimFeedbackRoutine = null;
    }

    private void AutoBind()
    {
        if (string.IsNullOrWhiteSpace(tankId))
            tankId = gameObject.name;

        if (itemRenderer == null)
        {
            Transform item = transform.Find("Item");
            if (item != null)
                itemRenderer = item.GetComponent<SpriteRenderer>();
        }

        if (itemRenderer == null)
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].transform != transform)
                {
                    itemRenderer = renderers[i];
                    break;
                }
            }
        }

        if (tankLight == null)
            tankLight = GetComponentInChildren<Light2D>(true);

        if (researchVfxRoot == null)
            researchVfxRoot = FindResearchVfxRoot();

        if (statusText == null)
            statusText = FindOrCreateStatusText();

        if (claimFeedbackText == null)
            claimFeedbackText = FindOrCreateClaimFeedbackText();

        if (bagPanel == null)
            bagPanel = FindFirstObjectByType<BattleBagPanelUI>(FindObjectsInactive.Include);

        if (bagPanelButton == null)
            bagPanelButton = FindBagPanelButton();
    }

    private GameObject FindResearchVfxRoot()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || child == transform)
                continue;

            if (child.name.Contains("VFX_BreedingTank_Bubbles", StringComparison.OrdinalIgnoreCase))
                return child.gameObject;
        }

        ParticleSystem particle = GetComponentInChildren<ParticleSystem>(true);
        return particle != null ? particle.gameObject : null;
    }

    private TextMeshPro FindOrCreateStatusText()
    {
        Transform existing = transform.Find("ResearchStatusText");
        TextMeshPro text = existing != null ? existing.GetComponent<TextMeshPro>() : null;

        if (text == null)
        {
            GameObject textObject = new("ResearchStatusText");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = statusTextLocalPosition;
            textObject.transform.localRotation = Quaternion.identity;
            textObject.transform.localScale = Vector3.one;
            text = textObject.AddComponent<TextMeshPro>();
        }

        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = statusTextFontSize;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(4f, 0.6f);

        MeshRenderer meshRenderer = text.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.sortingOrder = statusTextSortingOrder;

        return text;
    }

    private TextMeshPro FindOrCreateClaimFeedbackText()
    {
        Transform existing = transform.Find("ResearchClaimFeedbackText");
        TextMeshPro text = existing != null ? existing.GetComponent<TextMeshPro>() : null;

        if (text == null)
        {
            GameObject textObject = new("ResearchClaimFeedbackText");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = claimFeedbackTextLocalPosition;
            textObject.transform.localRotation = Quaternion.identity;
            textObject.transform.localScale = Vector3.one;
            text = textObject.AddComponent<TextMeshPro>();
        }

        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = claimFeedbackTextFontSize;
        text.color = new Color(0.65f, 1f, 0.75f, 1f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(4f, 0.6f);
        text.gameObject.SetActive(false);

        MeshRenderer meshRenderer = text.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.sortingOrder = claimFeedbackTextSortingOrder;

        return text;
    }

    private void EnsureWorldSpriteCollider()
    {
        if (GetComponent<Collider2D>() != null)
            return;

        if (GetComponent<SpriteRenderer>() == null)
            return;

        gameObject.AddComponent<PolygonCollider2D>();
    }

    private void OpenBagPanelForSelection()
    {
        closeBagPanelAfterSelection = false;

        if (bagPanel == null)
            return;

        if (bagPanelButton == null)
            bagPanelButton = FindBagPanelButton();

        bagPanel.gameObject.SetActive(true);

        if (bagPanelButton == null)
            return;

        bool wasOpen = bagPanelButton.IsMovePanelOpen();
        closeBagPanelAfterSelection = !wasOpen;
        bagPanelButton.SetMovePanelOpen(true, true);
    }

    private void ReleaseSelectionAndCloseBagPanel()
    {
        ReleaseSelectionBlock();
        CloseBagPanelAfterSelectionIfNeeded();
    }

    private void CloseBagPanelAfterSelectionIfNeeded()
    {
        if (!closeBagPanelAfterSelection)
            return;

        closeBagPanelAfterSelection = false;

        if (bagPanelButton == null)
            bagPanelButton = FindBagPanelButton();

        if (bagPanelButton != null)
            bagPanelButton.SetMovePanelOpen(false, true);
    }

    private UIPanelButton FindBagPanelButton()
    {
        if (bagPanel == null)
            return null;

        RectTransform bagRect = bagPanel.GetComponent<RectTransform>();
        UIPanelButton[] buttons = FindObjectsByType<UIPanelButton>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            UIPanelButton button = buttons[i];
            if (button != null && button.ControlsMovePanel(bagRect))
                return button;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            UIPanelButton button = buttons[i];
            if (button != null && string.Equals(button.name, "BagButton", StringComparison.Ordinal))
                return button;
        }

        return null;
    }

    private void ReleaseSelectionBlock()
    {
        if (LobbyPositionModalInputBlocker.IsBlockedBy(this))
            LobbyPositionModalInputBlocker.Unblock(this);
    }

    private static void SaveProgress()
    {
        SaveSystem.Instance?.SaveCurrentProgress();
    }

    private static bool CanLocalPlayerMutateHostOnlyState()
    {
        SteamLobbySharedStateSynchronizer synchronizer =
            SteamLobbySharedStateSynchronizer.Instance;
        return synchronizer == null ||
               synchronizer.CanLocalPlayerMutateHostOnlyState();
    }

    private static void PublishHostSnapshotAfterLocalMutation()
    {
        SteamLobbySharedStateSynchronizer.Instance
            ?.PublishHostSnapshotAfterLocalMutation();
    }
}
