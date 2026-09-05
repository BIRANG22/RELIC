using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyCultureTankPanelPresenter : MonoBehaviour
{
    private const int StorageMinimumSlotCount = 36;
    private const float PassiveRefreshInterval = 0.25f;

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button backButton;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private TMP_Text emptyText;
    [SerializeField] private Transform storageContentRoot;
    [SerializeField] private BattleBagItemSlotUI storageSlotPrefab;
    [SerializeField] private ScrollRect storageScrollRect;
    [SerializeField] private Scrollbar storageVerticalScrollbar;
    [SerializeField] private TankRow[] rows = new TankRow[3];
    [SerializeField] private Button combineButton;
    [SerializeField] private GameObject completionRoot;
    [SerializeField] private Button completionButton;
    [SerializeField] private Image completionIcon;

    [Header("Automatic Claim")]
    [Min(0f)][SerializeField] private float automaticClaimDelay = 1f;

    [Header("Compound Transfer Animation")]
    [Tooltip("딜레이가 끝난 뒤 연성제 획득 이펙트가 도착할 UI 타겟입니다.")]
    [SerializeField] private RectTransform compoundTransferTarget;
    [Tooltip("유물/튜토리얼 파편 획득과 같은 방식으로 RawImage에 표시할 Texture2D입니다.")]
    [SerializeField] private Texture2D compoundTransferEffectTexture;
    [SerializeField] private Vector2 compoundTransferEffectSize = new Vector2(96f, 96f);
    [SerializeField] private Vector2 compoundTransferBounceOffset = new Vector2(180f, 120f);
    [SerializeField, Min(0.01f)] private float compoundTransferBounceDuration = 0.18f;
    [SerializeField, Min(0.01f)] private float compoundTransferFlyDuration = 0.32f;
    [SerializeField, Min(0.05f)] private float compoundTransferEndScale = 0.35f;

    [Header("Compound Transfer Trail")]
    [SerializeField, Min(0.005f)] private float compoundTrailSpawnInterval = 0.025f;
    [SerializeField, Min(0.01f)] private float compoundTrailLifetime = 0.18f;
    [SerializeField, Range(0.05f, 1f)] private float compoundTrailStartScale = 0.78f;
    [SerializeField, Range(0f, 1f)] private float compoundTrailEndScale = 0.2f;
    [SerializeField, Range(0f, 1f)] private float compoundTrailStartAlpha = 0.48f;

    private readonly List<BattleBagItemSlotUI> storageSlots = new();
    private readonly List<string> storageItemOrder = new();
    private int selectedSlotIndex = -1;
    private float nextPassiveRefreshTime;
    private Coroutine automaticClaimCoroutine;
    private bool compoundTransferInProgress;
    private GameObject activeCompoundTransferEffect;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    private void Awake() { BindSceneObjects(); BindButtons(); EnsureStorageSlots(); }
    private void OnEnable() { BindSceneObjects(); BindButtons(); EnsureStorageSlots(); RefreshAll(); RefreshPanelText(); }
    private void Update()
    {
        if (!IsOpen || Time.unscaledTime < nextPassiveRefreshTime)
            return;

        RefreshAll();
    }

    public void Open()
    {
        if (LobbyPositionModalInputBlocker.IsBlockedByAnother(this)) return;
        BindSceneObjects(); BindButtons();
        if (panelRoot == null) return;
        LobbyPositionModalInputBlocker.Block(this);
        UIBlurBackground blurBackground = UIBlurBackground.EnsureForPanel(panelRoot);
        LobbyQuestManager.Instance?.ConfigureQuestPanelBlur(blurBackground);
        panelRoot.SetActive(true); RefreshAll(); RefreshPanelText();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        selectedSlotIndex = -1;
        ResetStorageSlotVisualStates();
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void OnDisable()
    {
        if (automaticClaimCoroutine != null)
        {
            StopCoroutine(automaticClaimCoroutine);
            automaticClaimCoroutine = null;
        }

        compoundTransferInProgress = false;
        if (activeCompoundTransferEffect != null)
        {
            Destroy(activeCompoundTransferEffect);
            activeCompoundTransferEffect = null;
        }

        LobbyPositionModalInputBlocker.Unblock(this);
    }
    private void OnDestroy() => LobbyPositionModalInputBlocker.Unblock(this);

    private void RefreshAll()
    {
        nextPassiveRefreshTime = Time.unscaledTime + PassiveRefreshInterval;
        RefreshRows();
        RefreshInventory();
        RefreshCompletion();
    }

    private void RefreshRows()
    {
        LobbyRuntimeData lobby = GetLobby();
        for (int i = 0; i < rows.Length; i++)
        {
            TankRow row = rows[i];
            if (row?.Root == null) continue;
            string slotId = GetSlotId(i);
            bool filled = CultureTankResearchService.TryGetTank(lobby, slotId, out CultureTankResearchRuntimeData slot);
            if (row.Label != null)
                row.Label.text = string.Format(
                    GameLocalization.Get("lobby.culture_tank_number", "배양조 {0}"),
                    i + 1);
            if (row.StateLabel != null)
                row.StateLabel.text = filled
                    ? GameLocalization.Get("lobby.material_inserted", "재료 투입됨")
                    : GameLocalization.Get("lobby.empty", "비어 있음");
            Sprite icon = null;
            if (filled) DataManager.Instance?.ItemIconDatabase?.TryGetIcon(slot.ItemId, out icon);
            row.SetIcon(icon);
            if (row.Button != null) row.Button.interactable = CanMutate();
        }
        if (emptyText != null) emptyText.gameObject.SetActive(false);
        if (combineButton != null)
            combineButton.interactable = CanMutate() && lobby != null && lobby.CultureTankResearches.Count == 3 &&
                                         string.IsNullOrEmpty(lobby.CompletedCultureTankCombinationId);
    }

    private void SelectRow(int index)
    {
        LobbyRuntimeData lobby = GetLobby();
        if (CultureTankResearchService.TryGetTank(lobby, GetSlotId(index), out _))
        {
            if (CultureTankResearchService.TryRemoveIngredient(lobby, GetSlotId(index), out _)) SaveAndPublish();
            selectedSlotIndex = -1;
        }
        else selectedSlotIndex = index;
        RefreshAll();
    }

    private void SelectInventoryItem(string itemId)
    {
        LobbyRuntimeData lobby = GetLobby();
        bool hasCompletedCombination = !string.IsNullOrWhiteSpace(lobby?.CompletedCultureTankCombinationId);

        if (!CanMutate() || hasCompletedCombination || string.IsNullOrWhiteSpace(itemId))
            return;

        // Storage의 재료를 바로 클릭하면 선택된 행이 있을 때는 그 행에,
        // 선택된 행이 없을 때는 CultureTankRow_1~3 중 첫 번째 빈 행에 자동 투입합니다.
        int targetSlotIndex = selectedSlotIndex >= 0
            ? selectedSlotIndex
            : FindFirstEmptyTankIndex(lobby);

        if (targetSlotIndex < 0)
        {
            Debug.LogWarning("[LobbyCultureTankPanelPresenter] 비어 있는 배양조가 없습니다.");
            return;
        }

        if (!CultureTankResearchService.TryPlaceIngredient(lobby, GetSlotId(targetSlotIndex), itemId, out string error))
        {
            Debug.LogWarning($"[LobbyCultureTankPanelPresenter] {error}");
            return;
        }

        selectedSlotIndex = -1;
        SaveAndPublish();
        RefreshAll();
    }

    private void Combine()
    {
        DataManager data = DataManager.Instance;
        if (!CultureTankResearchService.TryCombine(GetLobby(), data?.ItemDatabase, data?.CompoundDatabase, out _, out string error))
        { BattleWarningUI.ShowMessage(GameLocalization.Get("lobby.cannot_combine", "조합할 수 없습니다.")); Debug.LogWarning($"[LobbyCultureTankPanelPresenter] {error}"); return; }
        selectedSlotIndex = -1; SaveAndPublish(); RefreshAll();
    }

    private void ClaimCompletion()
    {
        if (!CultureTankResearchService.TryClaimCompletedCombination(GetLobby(), DataManager.Instance?.CompoundDatabase, out string compoundId, out string error))
        { Debug.LogWarning($"[LobbyCultureTankPanelPresenter] {error}"); return; }

        RecordDiscoveryService.RegisterCompound(DataManager.Instance, compoundId);
        SaveAndPublish();
        RefreshAll();
    }

    private void RefreshCompletion()
    {
        LobbyRuntimeData lobby = GetLobby();
        string id = lobby?.CompletedCultureTankCombinationId;
        CompoundData recipe = null;
        bool completed = !string.IsNullOrWhiteSpace(id) &&
                         DataManager.Instance?.CompoundDatabase != null &&
                         DataManager.Instance.CompoundDatabase.TryGet(id, out recipe);
        if (completionRoot != null) completionRoot.SetActive(ShouldShowCompletionRoot(completed));
        if (completionIcon != null)
        {
            Sprite icon = null;
            if (completed && DataManager.Instance?.RelicIconDatabase != null)
                DataManager.Instance.RelicIconDatabase.TryGetIcon(recipe.CompoundId, out icon);
            completionIcon.sprite = icon;
            completionIcon.enabled = completed && icon != null && !compoundTransferInProgress;
            completionIcon.preserveAspect = true;
        }
        // 완성된 연성제는 클릭으로 즉시 획득하지 않고, 아래 자동 획득 딜레이가 끝난 뒤 수령합니다.
        if (completionButton != null) completionButton.interactable = false;

        if (completed && CanMutate())
            StartAutomaticClaimIfNeeded();
    }

    private void StartAutomaticClaimIfNeeded()
    {
        if (automaticClaimCoroutine != null)
            return;

        LobbyRuntimeData lobby = GetLobby();
        if (lobby == null || string.IsNullOrWhiteSpace(lobby.CompletedCultureTankCombinationId))
            return;

        automaticClaimCoroutine = StartCoroutine(AutomaticClaimRoutine());
    }

    private IEnumerator AutomaticClaimRoutine()
    {
        float delay = Mathf.Max(0f, automaticClaimDelay);
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);
        else
            yield return null;

        LobbyRuntimeData lobby = GetLobby();
        if (lobby == null || string.IsNullOrWhiteSpace(lobby.CompletedCultureTankCombinationId))
        {
            automaticClaimCoroutine = null;
            yield break;
        }

        // 딜레이가 끝나면 completion 위치에서 지정한 타겟으로 획득 이펙트를 이동시킵니다.
        // 이펙트가 도착한 뒤 실제 연성제를 보관함에 넣습니다.
        yield return PlayCompoundTransferEffectRoutine();

        automaticClaimCoroutine = null;

        lobby = GetLobby();
        if (lobby == null || string.IsNullOrWhiteSpace(lobby.CompletedCultureTankCombinationId))
            yield break;

        ClaimCompletion();
    }

    private IEnumerator PlayCompoundTransferEffectRoutine()
    {
        if (completionIcon == null || compoundTransferTarget == null || compoundTransferEffectTexture == null)
            yield break;

        Canvas transferCanvas = completionIcon.GetComponentInParent<Canvas>();
        if (transferCanvas == null)
            yield break;

        RectTransform transferParent = ResolveTransferEffectParent(transferCanvas);
        RectTransform sourceRect = completionIcon.rectTransform;
        Camera sourceCamera = ResolveUiCamera(sourceRect);
        Camera targetCamera = ResolveUiCamera(compoundTransferTarget);
        Vector2 startScreenPosition = GetRectScreenCenter(sourceRect, sourceCamera);
        Vector2 targetScreenPosition = GetRectScreenCenter(compoundTransferTarget, targetCamera);

        Color rarityColor = ResolveCompletedCompoundRarityColor();
        RawImage effectImage = CreateCompoundTransferEffect(
            transferCanvas,
            transferParent,
            startScreenPosition,
            rarityColor);
        if (effectImage == null)
            yield break;

        activeCompoundTransferEffect = effectImage.gameObject;
        compoundTransferInProgress = true;
        completionIcon.enabled = false;

        yield return AnimateCompoundTransferEffect(
            effectImage.rectTransform,
            transferCanvas,
            transferParent,
            startScreenPosition,
            targetScreenPosition);

        if (effectImage != null)
            Destroy(effectImage.gameObject);

        activeCompoundTransferEffect = null;
        compoundTransferInProgress = false;
    }

    private Color ResolveCompletedCompoundRarityColor()
    {
        Color fallbackColor = completionIcon != null ? completionIcon.color : Color.white;
        LobbyRuntimeData lobby = GetLobby();
        string compoundId = lobby?.CompletedCultureTankCombinationId;

        if (string.IsNullOrWhiteSpace(compoundId) ||
            DataManager.Instance?.CompoundDatabase == null ||
            !DataManager.Instance.CompoundDatabase.TryGet(compoundId, out CompoundData compoundData) ||
            compoundData == null || string.IsNullOrWhiteSpace(compoundData.Rarity))
        {
            return fallbackColor;
        }

        Color rarityColor;
        if (!RecordPanelUI.TryGetCachedRarityDisplayColor(compoundData.Rarity, out rarityColor))
        {
            RecordPanelUI[] panels = FindObjectsByType<RecordPanelUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            rarityColor = panels.Length > 0
                ? panels[0].GetRarityDisplayColor(compoundData.Rarity)
                : fallbackColor;
        }

        rarityColor.a = fallbackColor.a;
        return rarityColor;
    }

    private RawImage CreateCompoundTransferEffect(
        Canvas transferCanvas,
        RectTransform transferParent,
        Vector2 screenPosition,
        Color rarityColor)
    {
        if (transferCanvas == null || compoundTransferEffectTexture == null)
            return null;

        GameObject effectObject = new GameObject(
            "CompoundTransferEffect",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

        RectTransform rect = effectObject.GetComponent<RectTransform>();
        rect.SetParent(transferParent != null ? transferParent : transferCanvas.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = compoundTransferEffectSize;
        rect.localScale = Vector3.one;
        rect.anchoredPosition = ScreenToUiLocalPosition(transferCanvas, transferParent, screenPosition);
        rect.SetAsLastSibling();

        RawImage image = effectObject.GetComponent<RawImage>();
        image.texture = compoundTransferEffectTexture;
        image.color = rarityColor;
        image.raycastTarget = false;
        return image;
    }

    private sealed class CompoundTrailGhost
    {
        public RectTransform Rect;
        public RawImage Image;
        public Vector3 StartScale;
        public Color StartColor;
        public float Age;
    }

    private IEnumerator AnimateCompoundTransferEffect(
        RectTransform effect,
        Canvas transferCanvas,
        RectTransform transferParent,
        Vector2 fromScreenPosition,
        Vector2 toScreenPosition)
    {
        if (effect == null || transferCanvas == null)
            yield break;

        RawImage sourceImage = effect.GetComponent<RawImage>();
        Vector2 fromPosition = ScreenToUiLocalPosition(transferCanvas, transferParent, fromScreenPosition);
        Vector2 toPosition = ScreenToUiLocalPosition(transferCanvas, transferParent, toScreenPosition);
        Vector2 bouncePosition = fromPosition + compoundTransferBounceOffset;
        Vector3 startScale = effect.localScale;
        Vector3 bounceScale = startScale * 1.08f;
        Vector3 endScale = Vector3.one * compoundTransferEndScale;
        var trailGhosts = new List<CompoundTrailGhost>();
        float trailTimer = 0f;

        effect.SetAsLastSibling();

        float elapsed = 0f;
        float safeBounceDuration = Mathf.Max(0.01f, compoundTransferBounceDuration);
        while (elapsed < safeBounceDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsed += deltaTime;
            float t = Mathf.Clamp01(elapsed / safeBounceDuration);
            float eased = EaseInCubic(t);
            effect.anchoredPosition = Vector2.LerpUnclamped(fromPosition, bouncePosition, eased);
            effect.localScale = Vector3.LerpUnclamped(startScale, bounceScale, eased);
            trailTimer += deltaTime;
            SpawnCompoundTrailGhosts(effect, sourceImage, transferCanvas, transferParent, trailGhosts, ref trailTimer);
            UpdateCompoundTrailGhosts(trailGhosts, deltaTime);
            yield return null;
        }

        elapsed = 0f;
        float safeFlyDuration = Mathf.Max(0.01f, compoundTransferFlyDuration);
        while (elapsed < safeFlyDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsed += deltaTime;
            float t = Mathf.Clamp01(elapsed / safeFlyDuration);
            float eased = EaseInQuint(t);
            effect.anchoredPosition = Vector2.LerpUnclamped(bouncePosition, toPosition, eased);
            effect.localScale = Vector3.LerpUnclamped(bounceScale, endScale, eased);
            trailTimer += deltaTime;
            SpawnCompoundTrailGhosts(effect, sourceImage, transferCanvas, transferParent, trailGhosts, ref trailTimer);
            UpdateCompoundTrailGhosts(trailGhosts, deltaTime);
            yield return null;
        }

        effect.anchoredPosition = toPosition;
        effect.localScale = endScale;

        while (trailGhosts.Count > 0)
        {
            UpdateCompoundTrailGhosts(trailGhosts, Time.unscaledDeltaTime);
            yield return null;
        }
    }

    private void SpawnCompoundTrailGhosts(
        RectTransform sourceRect,
        RawImage sourceImage,
        Canvas transferCanvas,
        RectTransform transferParent,
        List<CompoundTrailGhost> trailGhosts,
        ref float trailTimer)
    {
        if (sourceRect == null || sourceImage == null || sourceImage.texture == null ||
            transferCanvas == null || trailGhosts == null)
            return;

        float interval = Mathf.Max(0.005f, compoundTrailSpawnInterval);
        while (trailTimer >= interval)
        {
            trailTimer -= interval;
            GameObject ghostObject = new GameObject(
                "CompoundTransferTrail",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));

            RectTransform ghostRect = ghostObject.GetComponent<RectTransform>();
            ghostRect.SetParent(transferParent != null ? transferParent : transferCanvas.transform, false);
            ghostRect.anchorMin = sourceRect.anchorMin;
            ghostRect.anchorMax = sourceRect.anchorMax;
            ghostRect.pivot = sourceRect.pivot;
            ghostRect.sizeDelta = sourceRect.sizeDelta;
            ghostRect.anchoredPosition = sourceRect.anchoredPosition;
            ghostRect.localRotation = sourceRect.localRotation;
            ghostRect.localScale = sourceRect.localScale * compoundTrailStartScale;
            ghostRect.SetSiblingIndex(Mathf.Max(0, sourceRect.GetSiblingIndex()));
            sourceRect.SetAsLastSibling();

            RawImage ghostImage = ghostObject.GetComponent<RawImage>();
            ghostImage.texture = sourceImage.texture;
            ghostImage.uvRect = sourceImage.uvRect;
            Color ghostColor = sourceImage.color;
            ghostColor.a *= compoundTrailStartAlpha;
            ghostImage.color = ghostColor;
            ghostImage.raycastTarget = false;

            trailGhosts.Add(new CompoundTrailGhost
            {
                Rect = ghostRect,
                Image = ghostImage,
                StartScale = ghostRect.localScale,
                StartColor = ghostColor,
                Age = 0f
            });
        }
    }

    private void UpdateCompoundTrailGhosts(List<CompoundTrailGhost> trailGhosts, float deltaTime)
    {
        float lifetime = Mathf.Max(0.01f, compoundTrailLifetime);
        for (int i = trailGhosts.Count - 1; i >= 0; i--)
        {
            CompoundTrailGhost ghost = trailGhosts[i];
            if (ghost == null || ghost.Rect == null || ghost.Image == null)
            {
                trailGhosts.RemoveAt(i);
                continue;
            }

            ghost.Age += deltaTime;
            float t = Mathf.Clamp01(ghost.Age / lifetime);
            ghost.Rect.localScale = Vector3.LerpUnclamped(
                ghost.StartScale,
                ghost.StartScale * compoundTrailEndScale,
                t);
            Color color = ghost.StartColor;
            color.a = Mathf.Lerp(ghost.StartColor.a, 0f, t);
            ghost.Image.color = color;

            if (t < 1f)
                continue;

            Destroy(ghost.Rect.gameObject);
            trailGhosts.RemoveAt(i);
        }
    }

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    private static float EaseInQuint(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * t * t;
    }

    private static Vector2 ScreenToUiLocalPosition(Canvas canvas, RectTransform coordinateRoot, Vector2 screenPosition)
    {
        if (canvas == null)
            return Vector2.zero;

        RectTransform targetRect = coordinateRoot != null ? coordinateRoot : canvas.transform as RectTransform;
        if (targetRect == null)
            return Vector2.zero;

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect, screenPosition, uiCamera, out Vector2 localPoint)
            ? localPoint
            : Vector2.zero;
    }

    private static RectTransform ResolveTransferEffectParent(Canvas transferCanvas)
    {
        if (transferCanvas == null)
            return null;

        RectTransform resolved = ResolutionCanvasViewportFitter.ResolveContentRoot(transferCanvas.transform);
        return resolved != null ? resolved : transferCanvas.transform as RectTransform;
    }

    private static Camera ResolveUiCamera(RectTransform rect)
    {
        Canvas canvas = rect != null ? rect.GetComponentInParent<Canvas>() : null;
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
    }

    private static Vector2 GetRectScreenCenter(RectTransform targetRect, Camera fallbackCamera)
    {
        if (targetRect == null)
            return Vector2.zero;

        Canvas targetCanvas = targetRect.GetComponentInParent<Canvas>();
        Camera uiCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? (targetCanvas.worldCamera != null ? targetCanvas.worldCamera : fallbackCamera)
            : null;

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        return RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);
    }

    private void RefreshInventory()
    {
        EnsureStorageSlots();

        LobbyRuntimeData lobby = GetLobby();
        List<BagItemStack> stacks = BuildStorageStacksIncludingReserved(lobby);
        int stackCount = stacks != null ? stacks.Count : 0;
        int visibleSlotCount = Mathf.Max(StorageMinimumSlotCount, stackCount);
        EnsureStorageSlotCount(visibleSlotCount);

        bool hasCompletedCombination = !string.IsNullOrWhiteSpace(lobby?.CompletedCultureTankCombinationId);
        bool canSelect = CanMutate() && !hasCompletedCombination && HasEmptyTankSlot(lobby);

        for (int i = 0; i < storageSlots.Count; i++)
        {
            BattleBagItemSlotUI slot = storageSlots[i];
            if (slot == null)
                continue;

            string itemId = string.Empty;
            int itemCount = 0;
            if (i < stackCount)
            {
                BagItemStack stack = stacks[i];
                itemId = stack.ItemId;
                itemCount = stack.Count;

                bool needsSetup = !string.Equals(slot.ItemId, stack.ItemId, StringComparison.Ordinal) ||
                                  slot.Quantity != stack.Count;
                if (needsSetup)
                {
                    slot.SetupAllowZeroQuantity(
                        stack.ItemId,
                        stack.Count,
                        OnStorageSlotFocus,
                        OnStorageSlotExit,
                        null);
                }
            }
            else if (slot.HasItem)
            {
                slot.Clear(OnStorageSlotFocus, OnStorageSlotExit, null);
            }

            Button button = slot.GetComponent<Button>();
            CultureTankInventorySlotClickRelay relay =
                slot.GetComponent<CultureTankInventorySlotClickRelay>() ??
                slot.gameObject.AddComponent<CultureTankInventorySlotClickRelay>();

            BattleBagItemSlotUI capturedSlot = slot;
            bool selected = slot.HasItem &&
                            !string.IsNullOrWhiteSpace(itemId) &&
                            IsStorageItemSelectedInTank(lobby, itemId);

            relay.Configure(
                button,
                itemId,
                selected || (canSelect && slot.HasItem && itemCount > 0),
                selectedItemId => OnStorageItemClicked(capturedSlot, selectedItemId));

            slot.SetSelected(selected);

            // Refresh 도중 PointerExit 이벤트가 끊겨도 이전 Hover 상태가 남지 않도록
            // 현재 실제 마우스 위치를 기준으로 매번 true/false를 모두 동기화합니다.
            slot.SetHovered(slot.HasItem && IsPointerOverSlot(slot));

            slot.RefreshQuantityVisual();
        }

        if (storageContentRoot is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private void OnStorageSlotFocus(BattleBagItemSlotUI slot)
    {
        if (slot == null || !slot.HasItem)
            return;

        slot.SetHovered(true);
    }

    private void OnStorageSlotExit(BattleBagItemSlotUI slot)
    {
        if (slot == null)
            return;

        slot.SetHovered(false);
    }

    private void OnStorageItemClicked(BattleBagItemSlotUI slot, string itemId)
    {
        if (slot == null || !slot.HasItem || string.IsNullOrWhiteSpace(itemId))
            return;

        LobbyRuntimeData lobby = GetLobby();
        if (lobby == null || !CanMutate())
            return;

        string normalizedItemId = itemId.Trim();

        // 이미 CultureTankRow에 등록된 재료를 다시 클릭하면 해당 Row에서 제거합니다.
        // 제거 후 RefreshAll()에서 Storage 슬롯 선택 효과도 함께 해제됩니다.
        if (TryRemoveStorageItemFromTank(lobby, normalizedItemId))
        {
            selectedSlotIndex = -1;
            SaveAndPublish();
            RefreshAll();
            return;
        }

        if (!HasEmptyTankSlot(lobby))
            return;

        SelectInventoryItem(normalizedItemId);
    }

    private static bool TryRemoveStorageItemFromTank(LobbyRuntimeData lobby, string itemId)
    {
        if (lobby?.CultureTankResearches == null || string.IsNullOrWhiteSpace(itemId))
            return false;

        string normalizedItemId = itemId.Trim();
        for (int i = 0; i < lobby.CultureTankResearches.Count; i++)
        {
            CultureTankResearchRuntimeData research = lobby.CultureTankResearches[i];
            if (research == null || string.IsNullOrWhiteSpace(research.ItemId))
                continue;

            if (!string.Equals(research.ItemId.Trim(), normalizedItemId, StringComparison.Ordinal))
                continue;

            if (string.IsNullOrWhiteSpace(research.TankId))
                return false;

            return CultureTankResearchService.TryRemoveIngredient(lobby, research.TankId, out _);
        }

        return false;
    }

    private void ResetStorageSlotVisualStates()
    {
        for (int i = 0; i < storageSlots.Count; i++)
        {
            BattleBagItemSlotUI slot = storageSlots[i];
            if (slot == null)
                continue;

            slot.SetSelected(false);
            slot.SetHovered(false);
        }
    }


    private static bool IsStorageItemSelectedInTank(LobbyRuntimeData lobby, string itemId)
    {
        if (lobby?.CultureTankResearches == null || string.IsNullOrWhiteSpace(itemId))
            return false;

        string normalizedItemId = itemId.Trim();
        for (int i = 0; i < lobby.CultureTankResearches.Count; i++)
        {
            CultureTankResearchRuntimeData research = lobby.CultureTankResearches[i];
            if (research == null || string.IsNullOrWhiteSpace(research.ItemId))
                continue;

            if (string.Equals(research.ItemId.Trim(), normalizedItemId, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool IsPointerOverSlot(BattleBagItemSlotUI slot)
    {
        RectTransform rect = slot != null ? slot.RectTransform : null;
        if (rect == null || !rect.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, eventCamera);
    }

    private void RefreshPanelText()
    {
        MenuPanelTextRefresher refresher = EnsurePanelTextRefresher(panelRoot);
        if (refresher == null)
            return;

        refresher.RefreshNow();
        refresher.RefreshNextFrame();
    }

    private static MenuPanelTextRefresher EnsurePanelTextRefresher(GameObject panel)
    {
        if (panel == null)
            return null;

        MenuPanelTextRefresher refresher = panel.GetComponent<MenuPanelTextRefresher>();
        return refresher != null ? refresher : panel.AddComponent<MenuPanelTextRefresher>();
    }

    private void BindSceneObjects()
    {
        if (panelRoot == null) panelRoot = gameObject;
        Transform root = panelRoot.transform;
        if (backButton == null) backButton = Find(root, "BackButton")?.GetComponent<Button>();
        // 새 하이어라키에서는 CultureTankPanel 바로 아래에 MixButton, completion, CultureTankRow_1~3, Storage가 위치합니다.
        // contentRoot는 구형 하이어라키 호환용으로만 유지하며, 새 구조 바인딩에는 사용하지 않습니다.
        if (contentRoot == null) contentRoot = Find(root, "Content") as RectTransform;
        Transform storage = Find(root, "Storage");
        if (storage != null)
        {
            Transform scrollView = Find(storage, "Scroll View");
            Transform viewport = Find(scrollView, "Viewport");

            if (storageContentRoot == null)
            {
                storageContentRoot = Find(viewport, "Content");
                if (storageContentRoot == null)
                    storageContentRoot = Find(storage, "Content");
            }

            if (storageScrollRect == null && scrollView != null)
                storageScrollRect = scrollView.GetComponent<ScrollRect>();

            if (storageVerticalScrollbar == null && scrollView != null)
                storageVerticalScrollbar = Find(scrollView, "Scrollbar Vertical")?.GetComponent<Scrollbar>();
        }

        BindStorageScrollView();
        if (combineButton == null)
            combineButton = Find(root, "MixButton")?.GetComponent<Button>();
        if (combineButton == null)
            Debug.LogError("[LobbyCultureTankPanelPresenter] MixButton 오브젝트에 Button 컴포넌트가 필요합니다.", this);

        if (completionRoot == null) completionRoot = Find(root, "completion")?.gameObject;
        if (completionRoot != null)
        {
            // completion에는 씬/프리팹에 미리 설정한 Button을 그대로 사용합니다.
            // 런타임에 Button/Image를 추가하면 기존 클릭 영역과 Graphic 설정이 바뀔 수 있습니다.
            completionButton = completionRoot.GetComponent<Button>();
            if (completionButton == null)
            {
                Debug.LogError("[LobbyCultureTankPanelPresenter] completion 오브젝트에 Button 컴포넌트가 필요합니다.", completionRoot);
            }
            else
            {
                Graphic targetGraphic = completionButton.targetGraphic;
                if (targetGraphic == null)
                    targetGraphic = completionRoot.GetComponent<Graphic>();
                if (targetGraphic == null)
                    targetGraphic = completionRoot.GetComponentInChildren<Graphic>(true);

                if (targetGraphic != null)
                {
                    targetGraphic.raycastTarget = true;
                    completionButton.targetGraphic = targetGraphic;
                }
            }
        }
        if (completionIcon == null && completionRoot != null)
            completionIcon = Find(completionRoot.transform, "icon")?.GetComponent<Image>();
        if (completionIcon == null && completionRoot != null)
            completionIcon = Find(completionRoot.transform, "Image")?.GetComponent<Image>();
        if (completionIcon == null && completionButton != null)
            completionIcon = completionButton.GetComponent<Image>();
        if (rows == null || rows.Length != 3) rows = new TankRow[3];
        for (int i = 0; i < 3; i++)
        {
            rows[i] ??= new TankRow();
            if (rows[i].Root == null) rows[i].Root = Find(root, $"CultureTankRow_{i + 1}")?.gameObject;
            rows[i].Bind();
        }
    }

    private void BindStorageScrollView()
    {
        if (storageScrollRect == null)
            return;

        // 씬/프리팹에서 설정한 Viewport/Content RectTransform 값은 절대 변경하지 않습니다.
        // ScrollRect 참조가 비어 있을 때만 연결하고, 스크롤 기능만 활성화합니다.
        if (storageScrollRect.content == null && storageContentRoot is RectTransform contentRect)
            storageScrollRect.content = contentRect;

        if (storageScrollRect.viewport == null)
        {
            Transform viewportTransform = Find(storageScrollRect.transform, "Viewport");
            if (viewportTransform != null)
                storageScrollRect.viewport = viewportTransform as RectTransform;
        }

        storageScrollRect.horizontal = false;
        storageScrollRect.vertical = true;

        if (storageVerticalScrollbar != null)
        {
            storageVerticalScrollbar.gameObject.SetActive(true);
            storageScrollRect.verticalScrollbar = storageVerticalScrollbar;
            storageScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }
    }

    private void BindButtons()
    {
        if (backButton != null) { backButton.onClick.RemoveListener(Close); backButton.onClick.AddListener(Close); }
        if (combineButton != null) { combineButton.onClick.RemoveListener(Combine); combineButton.onClick.AddListener(Combine); }
        if (completionButton != null) completionButton.onClick.RemoveListener(ClaimCompletion);
        for (int i = 0; i < rows.Length; i++)
        {
            int index = i;
            if (rows[i]?.Button == null) continue;
            rows[i].Button.onClick.RemoveAllListeners(); rows[i].Button.onClick.AddListener(() => SelectRow(index));
        }
    }

    private void EnsureStorageSlots()
    {
        if (storageContentRoot == null || storageSlotPrefab == null)
            return;

        // Content 아래에 미리 배치한 StorageSlotUI_0~35를 우선 재사용합니다.
        // 기본 36칸이 이미 존재하면 새 슬롯을 만들지 않고, 36종류를 초과할 때만 추가 생성합니다.
        RegisterExistingStorageSlots();

        int targetCount = Mathf.Max(StorageMinimumSlotCount, storageSlots.Count);
        EnsureStorageSlotCount(targetCount);
    }

    private void RegisterExistingStorageSlots()
    {
        if (storageContentRoot == null)
            return;

        storageSlots.RemoveAll(slot => slot == null);
        if (storageSlots.Count > 0)
            return;

        for (int i = 0; i < storageContentRoot.childCount; i++)
        {
            Transform child = storageContentRoot.GetChild(i);
            BattleBagItemSlotUI slot = child.GetComponent<BattleBagItemSlotUI>();
            if (slot == null)
                continue;

            storageSlots.Add(slot);
        }
    }

    private void EnsureStorageSlotCount(int targetCount)
    {
        if (storageContentRoot == null || storageSlotPrefab == null)
            return;

        storageSlots.RemoveAll(slot => slot == null);

        while (storageSlots.Count < targetCount)
        {
            int index = storageSlots.Count;
            BattleBagItemSlotUI slot = Instantiate(storageSlotPrefab, storageContentRoot, false);
            slot.name = $"{storageSlotPrefab.name}_{index}";
            slot.gameObject.SetActive(true);
            slot.Clear(null, null, null);
            storageSlots.Add(slot);
        }

        for (int i = 0; i < storageSlots.Count; i++)
            storageSlots[i].gameObject.SetActive(i < targetCount);
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    private List<BagItemStack> BuildStorageStacksIncludingReserved(LobbyRuntimeData lobby)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var activeIds = new HashSet<string>(StringComparer.Ordinal);
        var discoveryOrder = new List<string>();

        if (lobby?.BagItemIds != null)
        {
            for (int i = 0; i < lobby.BagItemIds.Count; i++)
            {
                string rawId = lobby.BagItemIds[i];
                if (string.IsNullOrWhiteSpace(rawId))
                    continue;

                string itemId = rawId.Trim();
                if (activeIds.Add(itemId))
                    discoveryOrder.Add(itemId);

                counts.TryGetValue(itemId, out int count);
                counts[itemId] = count + 1;
            }
        }

        if (lobby?.CultureTankResearches != null)
        {
            for (int i = 0; i < lobby.CultureTankResearches.Count; i++)
            {
                CultureTankResearchRuntimeData research = lobby.CultureTankResearches[i];
                if (research == null || string.IsNullOrWhiteSpace(research.ItemId))
                    continue;

                string itemId = research.ItemId.Trim();
                if (activeIds.Add(itemId))
                    discoveryOrder.Add(itemId);

                if (!counts.ContainsKey(itemId))
                    counts[itemId] = 0;
            }
        }

        // 작업대에 임시 등록한 재료는 실제 확정 소비 전까지 Storage의 원래 슬롯 위치를 유지합니다.
        // 취소 시에도 같은 슬롯에서 0 -> 1로 돌아오도록, 현재 존재하는 ID의 표시 순서를 캐시합니다.
        storageItemOrder.RemoveAll(itemId => !activeIds.Contains(itemId));

        var orderedIds = new HashSet<string>(storageItemOrder, StringComparer.Ordinal);
        for (int i = 0; i < discoveryOrder.Count; i++)
        {
            string itemId = discoveryOrder[i];
            if (orderedIds.Add(itemId))
                storageItemOrder.Add(itemId);
        }

        var stacks = new List<BagItemStack>(storageItemOrder.Count);
        for (int i = 0; i < storageItemOrder.Count; i++)
        {
            string itemId = storageItemOrder[i];
            if (!activeIds.Contains(itemId))
                continue;

            counts.TryGetValue(itemId, out int count);
            stacks.Add(new BagItemStack(itemId, count));
        }

        return stacks;
    }

    private static int FindFirstEmptyTankIndex(LobbyRuntimeData lobby)
    {
        if (lobby == null)
            return -1;

        for (int i = 0; i < 3; i++)
        {
            if (!CultureTankResearchService.TryGetTank(lobby, GetSlotId(i), out _))
                return i;
        }

        return -1;
    }

    private static bool HasEmptyTankSlot(LobbyRuntimeData lobby) => FindFirstEmptyTankIndex(lobby) >= 0;

    private static string GetSlotId(int index) => $"CultureTank{index + 1}";
    public static bool ShouldShowCompletionRoot(bool hasCompletedCombination) => true;
    public static bool CanSelectInventoryItem(bool hasSelectedRow, bool canMutate, bool hasCompletedCombination) =>
        hasSelectedRow && canMutate && !hasCompletedCombination;
    private static LobbyRuntimeData GetLobby() => DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
    private static bool CanMutate() => SteamLobbySharedStateSynchronizer.Instance == null || SteamLobbySharedStateSynchronizer.Instance.CanLocalPlayerMutateHostOnlyState();
    private static void SaveAndPublish() { SaveSystem.Instance?.SaveCurrentProgress(); BattleBagPanelUI.RefreshAll(); SteamLobbySharedStateSynchronizer.Instance?.PublishHostSnapshotAfterLocalMutation(); }

    [Serializable]
    private sealed class TankRow
    {
        [SerializeField] private GameObject root; [SerializeField] private Image background; [SerializeField] private Button button;
        [SerializeField] private TMP_Text label; [SerializeField] private TMP_Text stateLabel; [SerializeField] private Image itemIcon;
        public GameObject Root { get => root; set => root = value; }
        public Image Background => background; public Button Button => button;
        public TMP_Text Label => label; public TMP_Text StateLabel => stateLabel;
        public void Bind()
        {
            if (root == null) return;
            if (background == null) background = Find(root.transform, "back")?.GetComponent<Image>();
            if (background == null) background = root.GetComponent<Image>();
            if (button == null) button = root.GetComponent<Button>() ?? root.AddComponent<Button>();
            Image clickSurface = root.GetComponent<Image>();
            if (clickSurface == null)
            {
                clickSurface = root.AddComponent<Image>();
                clickSurface.color = Color.clear;
            }
            clickSurface.raycastTarget = true;
            button.targetGraphic = clickSurface;
            if (label == null) label = Find(root.transform, "Label")?.GetComponent<TMP_Text>() ?? root.GetComponentInChildren<TMP_Text>(true);
            if (stateLabel == null) stateLabel = Find(root.transform, "StateLabel")?.GetComponent<TMP_Text>();
            // CultureTankRow 안에 이미 배치된 Icon 오브젝트를 재료 아이콘 표시용으로 사용합니다.
            // 런타임에 ItemIcon 오브젝트를 새로 만들지 않습니다.
            if (itemIcon == null) itemIcon = Find(root.transform, "Icon")?.GetComponent<Image>();
            if (itemIcon == null) itemIcon = Find(root.transform, "icon")?.GetComponent<Image>();
            if (itemIcon != null) itemIcon.raycastTarget = false;
        }
        public void SetIcon(Sprite icon)
        {
            if (itemIcon == null) return;

            bool hasIcon = icon != null;
            itemIcon.sprite = icon;
            itemIcon.preserveAspect = true;
            itemIcon.enabled = hasIcon;
            itemIcon.gameObject.SetActive(hasIcon);
        }
    }

}
