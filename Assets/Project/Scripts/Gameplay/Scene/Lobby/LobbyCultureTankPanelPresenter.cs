using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyCultureTankPanelPresenter : MonoBehaviour
{
    private const int StorageMinimumSlotCount = 12;
    private const float PassiveRefreshInterval = 0.25f;

    [SerializeField] private GameObject panelRoot;
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

    [Header("Ingredient Register Sound")]
    [Tooltip("Storage의 재료가 CultureTankRow에 실제로 등록되었을 때 재생할 SFX입니다.")]
    [SerializeField, SoundId(SoundCategory.Sfx)]
    private string ingredientRegisterSoundId = AudioIds.Sfx.NormalButtonClick;

    [Tooltip("CultureTankRow 등록 사운드의 볼륨입니다.")]
    [SerializeField, Range(0f, 1f)]
    private float ingredientRegisterSoundVolume = 1f;

    [Header("Compound Transfer Sound")]
    [Tooltip("완성된 연성제 획득 이펙트가 타겟으로 이동하기 시작할 때 재생할 SFX입니다.")]
    [SerializeField, SoundId(SoundCategory.Sfx)]
    private string compoundTransferStartSoundId = string.Empty;

    [Tooltip("연성제 획득 이동 시작 사운드의 볼륨입니다.")]
    [SerializeField, Range(0f, 1f)]
    private float compoundTransferStartSoundVolume = 0.5f;

    [Header("Compound Transfer Animation")]
    [Tooltip("조합 완료 후 completion 아이콘을 잠깐 보여준 뒤 이동 연출을 시작하기까지의 시간입니다.")]
    [SerializeField, Min(0f)] private float compoundTransferStartDelay = 0.5f;
    [Tooltip("로비 유물 구매 때 사용하는 것과 같은 ScreenSpaceTransferOrbEffect 프리팹입니다.")]
    [SerializeField] private ScreenSpaceTransferOrbEffect screenSpaceTransferEffectPrefab;
    [Tooltip("완성된 연성제가 최종적으로 들어갈 Lobby_Icon/Icon_04(Storage) UI입니다. 비어 있으면 씬에서 자동 탐색합니다.")]
    [SerializeField] private RectTransform compoundTransferUiTarget;

    private readonly List<BattleBagItemSlotUI> storageSlots = new();
    private readonly List<string> storageItemOrder = new();
    private int selectedSlotIndex = -1;
    private float nextPassiveRefreshTime;
    private Coroutine automaticClaimCoroutine;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    private void Awake() { BindSceneObjects(); BindCultureStorageHeader(); BindButtons(); EnsureStorageSlots(); }
    private void OnEnable() { BindSceneObjects(); BindCultureStorageHeader(); BindButtons(); EnsureStorageSlots(); RefreshAll(); RefreshPanelText(); }

    private void BindCultureStorageHeader()
    {
        Transform root = panelRoot != null ? panelRoot.transform : transform;
        Transform storage = Find(root, "Storage");
        Transform nameRoot = storage != null ? storage.Find("Name") : null;
        Transform nameTextTransform = nameRoot != null ? nameRoot.Find("NameText") : null;
        TMP_Text nameText = nameTextTransform != null ? nameTextTransform.GetComponent<TMP_Text>() : null;
        if (nameText == null)
            return;

        LocalizedTMPText localizer = nameText.GetComponent<LocalizedTMPText>();
        if (localizer == null)
            localizer = nameText.gameObject.AddComponent<LocalizedTMPText>();

        localizer.enabled = true;
        localizer.Configure(LocalizationKeys.Lobby.CultureMaterial, string.Empty, false);
    }

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
        LobbyPositionSharedModalBackground.ShowForPanel(panelRoot, this, Close);
        panelRoot.SetActive(true); RefreshAll(); RefreshPanelText();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        LobbyPositionSharedModalBackground.HideForOwner(this);
        selectedSlotIndex = -1;
        ResetStorageSlotVisualStates();
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    public bool ControlsPanel(GameObject panel)
    {
        BindSceneObjects();
        return panel != null && panelRoot == panel;
    }

    private void OnDisable()
    {
        if (automaticClaimCoroutine != null)
        {
            StopCoroutine(automaticClaimCoroutine);
            automaticClaimCoroutine = null;
        }

        LobbyPositionSharedModalBackground.HideForOwner(this);
        LobbyPositionModalInputBlocker.Unblock(this);
    }
    private void OnDestroy()
    {
        LobbyPositionSharedModalBackground.HideForOwner(this);
        LobbyPositionModalInputBlocker.Unblock(this);
    }

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
                    GameLocalization.Get("lobby.culture_tank_number"),
                    i + 1);
            if (row.StateLabel != null)
                row.StateLabel.text = filled
                    ? GameLocalization.Get("lobby.material_inserted")
                    : GameLocalization.Get("lobby.empty");
            Sprite icon = null;
            if (filled) DataManager.Instance?.ItemIconDatabase?.TryGetIcon(slot.ItemId, out icon);
            row.SetIcon(icon);
            row.ApplyVisualState(filled);
            if (row.Button != null) row.Button.interactable = filled && CanMutate();
        }
        if (emptyText != null) emptyText.gameObject.SetActive(false);
        if (combineButton != null)
            combineButton.interactable = CanMutate() && lobby != null && lobby.CultureTankResearches.Count == 3 &&
                                         string.IsNullOrEmpty(lobby.CompletedCultureTankCombinationId);
    }

    private void SelectRow(int index)
    {
        if (!CanMutate())
            return;

        LobbyRuntimeData lobby = GetLobby();
        if (!CultureTankResearchService.TryGetTank(lobby, GetSlotId(index), out _))
            return;

        if (CultureTankResearchService.TryRemoveIngredient(lobby, GetSlotId(index), out _))
            SaveAndPublish();

        selectedSlotIndex = -1;
        RefreshAll();
    }

    public bool TryRegisterRecipeMaterial(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        LobbyRuntimeData lobby = GetLobby();
        if (lobby == null || !CanMutate())
            return false;

        string normalizedItemId = itemId.Trim();

        // 조합식 재료도 Storage 슬롯과 동일하게 토글합니다.
        // 이미 배양조에 들어 있는 재료라면 다시 클릭했을 때 제거합니다.
        if (TryRemoveStorageItemFromTank(lobby, normalizedItemId))
        {
            selectedSlotIndex = -1;
            SaveAndPublish();
            RefreshAll();
            return true;
        }

        return SelectInventoryItem(normalizedItemId);
    }

    public bool TryRegisterRecipeMaterials(string materialId1, string materialId2, string materialId3)
    {
        if (!CanMutate())
            return false;

        LobbyRuntimeData lobby = GetLobby();
        if (lobby == null || !string.IsNullOrWhiteSpace(lobby.CompletedCultureTankCombinationId))
            return false;

        string[] materialIds =
        {
            NormalizeRecipeMaterialId(materialId1),
            NormalizeRecipeMaterialId(materialId2),
            NormalizeRecipeMaterialId(materialId3)
        };

        if (Array.Exists(materialIds, string.IsNullOrEmpty))
            return false;

        // 현재 배양조에 들어 있는 재료까지 포함해, 레시피 3종을 모두 보유했을 때만 한 번에 교체합니다.
        // 재료가 부족하면 기존 배양조 상태는 건드리지 않습니다.
        var availableCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        if (lobby.BagItemIds != null)
        {
            for (int i = 0; i < lobby.BagItemIds.Count; i++)
                AddRecipeMaterialCount(availableCounts, lobby.BagItemIds[i]);
        }

        if (lobby.CultureTankResearches != null)
        {
            for (int i = 0; i < lobby.CultureTankResearches.Count; i++)
                AddRecipeMaterialCount(availableCounts, lobby.CultureTankResearches[i]?.ItemId);
        }

        var requiredCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < materialIds.Length; i++)
            AddRecipeMaterialCount(requiredCounts, materialIds[i]);

        foreach (KeyValuePair<string, int> required in requiredCounts)
        {
            availableCounts.TryGetValue(required.Key, out int available);
            if (available < required.Value)
                return false;
        }

        // 검증이 끝난 뒤에만 기존 재료를 Storage로 돌려놓고 레시피 순서대로 1~3번 배양조에 등록합니다.
        if (lobby.CultureTankResearches != null && lobby.CultureTankResearches.Count > 0)
        {
            for (int i = lobby.CultureTankResearches.Count - 1; i >= 0; i--)
            {
                CultureTankResearchRuntimeData research = lobby.CultureTankResearches[i];
                if (research != null && !string.IsNullOrWhiteSpace(research.ItemId))
                    lobby.BagItemIds.Add(research.ItemId.Trim());
            }
            lobby.CultureTankResearches.Clear();
        }

        for (int i = 0; i < materialIds.Length; i++)
        {
            if (!CultureTankResearchService.TryPlaceIngredient(lobby, GetSlotId(i), materialIds[i], out string error))
            {
                Debug.LogWarning($"[LobbyCultureTankPanelPresenter] 레시피 재료 자동 등록 실패: {error}");
                return false;
            }
        }

        PlayIngredientRegisterSound();
        selectedSlotIndex = -1;
        SaveAndPublish();
        RefreshAll();
        return true;
    }

    private static string NormalizeRecipeMaterialId(string itemId)
    {
        return string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
    }

    private static void AddRecipeMaterialCount(Dictionary<string, int> counts, string itemId)
    {
        if (counts == null)
            return;

        string normalizedId = NormalizeRecipeMaterialId(itemId);
        if (string.IsNullOrEmpty(normalizedId))
            return;

        counts.TryGetValue(normalizedId, out int count);
        counts[normalizedId] = count + 1;
    }

    private bool SelectInventoryItem(string itemId)
    {
        LobbyRuntimeData lobby = GetLobby();
        bool hasCompletedCombination = !string.IsNullOrWhiteSpace(lobby?.CompletedCultureTankCombinationId);

        if (!CanMutate() || hasCompletedCombination || string.IsNullOrWhiteSpace(itemId))
            return false;

        // Storage의 재료를 바로 클릭하면 선택된 행이 있을 때는 그 행에,
        // 선택된 행이 없을 때는 CultureTankRow_1~3 중 첫 번째 빈 행에 자동 투입합니다.
        int targetSlotIndex = selectedSlotIndex >= 0
            ? selectedSlotIndex
            : FindFirstEmptyTankIndex(lobby);

        if (targetSlotIndex < 0)
        {
            Debug.LogWarning("[LobbyCultureTankPanelPresenter] 비어 있는 배양조가 없습니다.");
            return false;
        }

        if (!CultureTankResearchService.TryPlaceIngredient(lobby, GetSlotId(targetSlotIndex), itemId, out string error))
        {
            Debug.LogWarning($"[LobbyCultureTankPanelPresenter] {error}");
            return false;
        }

        PlayIngredientRegisterSound();
        selectedSlotIndex = -1;
        SaveAndPublish();
        RefreshAll();
        return true;
    }

    private void PlayIngredientRegisterSound()
    {
        if (string.IsNullOrWhiteSpace(ingredientRegisterSoundId))
            return;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
            return;

        audioManager.PlaySfx(
            ingredientRegisterSoundId,
            Mathf.Clamp01(ingredientRegisterSoundVolume));
    }

    private void Combine()
    {
        DataManager data = DataManager.Instance;
        if (!CultureTankResearchService.TryCombine(GetLobby(), data?.ItemDatabase, data?.CompoundDatabase, out string compoundId, out string error))
        {
            bool invalidRecipe = string.Equals(
                error,
                "No compound recipe matches these ingredients.",
                StringComparison.Ordinal);

            string warningMessage = invalidRecipe
                ? GameLocalization.Get(
                    LocalizationKeys.Warning.CompoundInvalidRecipe,
                    "조합식이 올바르지 않습니다.")
                : GameLocalization.Get("lobby.cannot_combine");

            SettingWarningUI.ShowMessage(warningMessage);
            Debug.LogWarning($"[LobbyCultureTankPanelPresenter] {error}");
            return;
        }

        // 조합이 성공한 순간 도감 발견 상태와 Reference 프리팹을 즉시 갱신합니다.
        // completion -> Storage 이동 연출은 별도로 0.5초 뒤 시작됩니다.
        if (!string.IsNullOrWhiteSpace(compoundId) && data != null)
            RecordDiscoveryService.RegisterCompound(data, compoundId);

        selectedSlotIndex = -1;
        SaveAndPublish();
        RefreshAll();
        RefreshCompoundReferenceNow();
    }

    private void ClaimCompletion()
    {
        if (!CultureTankResearchService.TryClaimCompletedCombination(GetLobby(), DataManager.Instance?.CompoundDatabase, out string compoundId, out string error))
        { Debug.LogWarning($"[LobbyCultureTankPanelPresenter] {error}"); return; }

        RecordDiscoveryService.RegisterCompound(DataManager.Instance, compoundId);
        SaveAndPublish();
        RefreshAll();
        RefreshCompoundReferenceNow();
    }

    private static void RefreshCompoundReferenceNow()
    {
        LobbyCompoundReferenceUI[] references = FindObjectsByType<LobbyCompoundReferenceUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < references.Length; i++)
        {
            if (references[i] != null)
                references[i].RefreshNow(true);
        }
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
            completionIcon.enabled = completed && icon != null;
            completionIcon.preserveAspect = true;
        }
        // 조합 직후 completion에 완성 아이콘을 표시한 뒤 다음 프레임에 자동으로 보관함 수령 연출을 시작합니다.
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
        // 조합 직후 RefreshCompletion()에서 completion 아이콘을 먼저 갱신합니다.
        // 아이콘이 눈에 들어올 정도의 짧은 시간만 보여준 뒤 Storage 이동 연출을 시작합니다.
        float startDelay = Mathf.Max(0f, compoundTransferStartDelay);
        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);
        else
            yield return null;

        LobbyRuntimeData lobby = GetLobby();
        if (lobby == null || string.IsNullOrWhiteSpace(lobby.CompletedCultureTankCombinationId))
        {
            automaticClaimCoroutine = null;
            yield break;
        }

        // 유물 구매와 동일하게 결과 데이터는 즉시 보관함에 반영하고,
        // completion 위치에서 Storage 아이콘으로 이동하는 연출은 독립적으로 끝까지 재생합니다.
        PlayCompoundTransferEffect();
        automaticClaimCoroutine = null;
        ClaimCompletion();
    }

    private void PlayCompoundTransferEffect()
    {
        if (completionIcon == null ||
            compoundTransferUiTarget == null ||
            screenSpaceTransferEffectPrefab == null)
        {
            return;
        }

        RectTransform sourceRect = completionIcon.rectTransform;
        Camera sourceCamera = ScreenSpaceTransferOrbEffect.ResolveUiCamera(sourceRect, Camera.main);
        Camera targetCamera = ScreenSpaceTransferOrbEffect.ResolveUiCamera(compoundTransferUiTarget, Camera.main);
        Vector2 startScreenPosition = ScreenSpaceTransferOrbEffect.GetRectScreenCenter(sourceRect, sourceCamera);
        Vector2 endScreenPosition = ScreenSpaceTransferOrbEffect.GetRectScreenCenter(compoundTransferUiTarget, targetCamera);
        Color rarityColor = ResolveCompletedCompoundRarityColor();

        PlayCompoundTransferStartSound();

        // LobbyRelicShopPresenter와 동일하게 Effect 자신이 코루틴을 실행합니다.
        // 패널 갱신/닫힘과 무관하게 Storage까지 연출이 끝까지 재생됩니다.
        ScreenSpaceTransferOrbEffect effect = Instantiate(screenSpaceTransferEffectPrefab);
        effect.PlayDetached(startScreenPosition, endScreenPosition, rarityColor);
    }

    private void PlayCompoundTransferStartSound()
    {
        if (string.IsNullOrWhiteSpace(compoundTransferStartSoundId))
            return;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
            return;

        audioManager.PlaySfx(
            compoundTransferStartSoundId,
            Mathf.Clamp01(compoundTransferStartSoundVolume));
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

    private static ScreenSpaceTransferOrbEffect FindRelicShopTransferEffectPrefab()
    {
        LobbyRelicShopPresenter[] presenters = FindObjectsByType<LobbyRelicShopPresenter>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < presenters.Length; i++)
        {
            LobbyRelicShopPresenter presenter = presenters[i];
            if (presenter != null && presenter.ScreenSpaceTransferEffectPrefab != null)
                return presenter.ScreenSpaceTransferEffectPrefab;
        }

        return null;
    }

    private static RectTransform FindStorageTransferTarget()
    {
        RectTransform[] rects = FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == null || !string.Equals(rect.name, "Icon_04", StringComparison.Ordinal))
                continue;

            Transform parent = rect.parent;
            if (parent != null && string.Equals(parent.name, "Lobby_Icon", StringComparison.Ordinal))
                return rect;
        }

        return null;
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
                selectedItemId => OnStorageItemClicked(capturedSlot, selectedItemId),
                (draggedItemId, eventData) => TryDropStorageItem(draggedItemId, eventData));

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

    private bool TryDropStorageItem(string itemId, PointerEventData eventData)
    {
        if (string.IsNullOrWhiteSpace(itemId) || eventData == null)
            return false;

        LobbyRuntimeData lobby = GetLobby();
        if (lobby == null || !CanMutate())
            return false;

        string normalizedItemId = itemId.Trim();

        // 같은 재료를 여러 번 사용하는 조합식은 없으므로 배양조 중복 등록을 막습니다.
        if (IsStorageItemSelectedInTank(lobby, normalizedItemId))
            return false;

        for (int i = 0; i < rows.Length; i++)
        {
            TankRow row = rows[i];
            if (row?.Root == null || !row.Root.activeInHierarchy)
                continue;

            RectTransform rowRect = row.Root.transform as RectTransform;
            if (rowRect == null)
                continue;

            Camera uiCamera = ScreenSpaceTransferOrbEffect.ResolveUiCamera(rowRect, Camera.main);
            if (!RectTransformUtility.RectangleContainsScreenPoint(rowRect, eventData.position, uiCamera))
                continue;

            // 이미 재료가 있는 Row는 드롭으로 덮어쓰지 않습니다.
            if (CultureTankResearchService.TryGetTank(lobby, GetSlotId(i), out _))
                return false;

            if (!CultureTankResearchService.TryPlaceIngredient(
                    lobby,
                    GetSlotId(i),
                    normalizedItemId,
                    out string error))
            {
                Debug.LogWarning($"[LobbyCultureTankPanelPresenter] {error}");
                return false;
            }

            PlayIngredientRegisterSound();
            selectedSlotIndex = -1;
            SaveAndPublish();
            RefreshAll();
            return true;
        }

        return false;
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
        if (compoundTransferUiTarget == null)
            compoundTransferUiTarget = FindStorageTransferTarget();

        if (screenSpaceTransferEffectPrefab == null)
            screenSpaceTransferEffectPrefab = FindRelicShopTransferEffectPrefab();
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

        // Content 아래에 미리 배치한 StorageSlotUI를 우선 재사용합니다.
        // 기본 표시 수는 12칸이며, 실제 재료가 12종류를 초과할 때만 필요한 만큼 추가로 활성화/생성합니다.
        RegisterExistingStorageSlots();
        EnsureStorageSlotCount(StorageMinimumSlotCount);
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
        private static readonly Color EmptyBacklineColor = new Color32(0x77, 0x77, 0x77, 0xFF);
        private static readonly Color FilledBacklineColor = new Color32(0xA9, 0xB1, 0xBE, 0xFF);
        private static readonly Color HoverBacklineColor = Color.white;

        [SerializeField] private GameObject root;
        [SerializeField] private Image background;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text stateLabel;
        [SerializeField] private Image itemIcon;
        [SerializeField] private GameObject cultureText;

        private LobbyCultureTankRowHoverRelay hoverRelay;
        private bool filled;

        public GameObject Root { get => root; set => root = value; }
        public Image Background => background;
        public Button Button => button;
        public TMP_Text Label => label;
        public TMP_Text StateLabel => stateLabel;

        public void Bind()
        {
            if (root == null) return;

            Image hierarchyBackline = Find(root.transform, "backline")?.GetComponent<Image>() ??
                                      Find(root.transform, "Backline")?.GetComponent<Image>();
            if (hierarchyBackline != null) background = hierarchyBackline;
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

            if (label == null) label = Find(root.transform, "Label")?.GetComponent<TMP_Text>();
            if (stateLabel == null) stateLabel = Find(root.transform, "StateLabel")?.GetComponent<TMP_Text>();

            if (itemIcon == null) itemIcon = Find(root.transform, "Icon")?.GetComponent<Image>();
            if (itemIcon == null) itemIcon = Find(root.transform, "icon")?.GetComponent<Image>();
            if (itemIcon != null) itemIcon.raycastTarget = false;

            GameObject hierarchyCultureText = Find(root.transform, "Culture_Text")?.gameObject;
            if (hierarchyCultureText != null)
                cultureText = hierarchyCultureText;

            hoverRelay = root.GetComponent<LobbyCultureTankRowHoverRelay>() ??
                         root.AddComponent<LobbyCultureTankRowHoverRelay>();
            hoverRelay.Configure(SetHovered);
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

        public void ApplyVisualState(bool hasIngredient)
        {
            filled = hasIngredient;

            if (cultureText != null)
                cultureText.SetActive(!hasIngredient);

            if (!hasIngredient && hoverRelay != null)
                hoverRelay.ResetHover();

            bool hovered = hasIngredient && hoverRelay != null && hoverRelay.IsHovered;
            SetBacklineRgb(hasIngredient
                ? (hovered ? HoverBacklineColor : FilledBacklineColor)
                : EmptyBacklineColor);
        }

        private void SetHovered(bool isHovered)
        {
            if (!filled)
            {
                SetBacklineRgb(EmptyBacklineColor);
                return;
            }

            SetBacklineRgb(isHovered ? HoverBacklineColor : FilledBacklineColor);
        }

        private void SetBacklineRgb(Color rgb)
        {
            if (background == null)
                return;

            Color current = background.color;
            background.color = new Color(rgb.r, rgb.g, rgb.b, current.a);
        }
    }

}

[DisallowMultipleComponent]
public sealed class LobbyCultureTankRowHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Action<bool> onHoverChanged;
    private bool hovered;

    public bool IsHovered => hovered;

    public void Configure(Action<bool> callback)
    {
        onHoverChanged = callback;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        onHoverChanged?.Invoke(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        onHoverChanged?.Invoke(false);
    }

    public void ResetHover()
    {
        if (!hovered)
            return;

        hovered = false;
        onHoverChanged?.Invoke(false);
    }
}
