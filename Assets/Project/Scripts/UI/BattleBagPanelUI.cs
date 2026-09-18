using System.Collections;
using System.Collections.Generic;
using System;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleBagPanelUI : MonoBehaviour
{
    private enum StorageCategory
    {
        Item,
        Compound,
        Relic
    }

    private const int MaxBagItemCount = 8;
    private const int StorageMinimumSlotCount = 16;
    private const int CompoundMinimumSlotCount = 8;
    private const int RelicMinimumSlotCount = 3;

    [Header("Runtime")]
    [SerializeField] private InventoryRuntimeContextProvider runtimeContextProvider;
    [SerializeField] private bool allowDiscardInLobby = false;

    [Header("Battle Slot List")]
    [SerializeField] private Transform slotRoot;
    [SerializeField] private List<BattleBagItemSlotUI> slots = new();

    [Header("Dynamic Content Slots")]
    [SerializeField] private Transform storageContentRoot;
    [SerializeField] private BattleBagItemSlotUI storageSlotPrefab;
    [SerializeField] private ScrollRect storageScrollRect;
    [SerializeField] private Scrollbar storageVerticalScrollbar;

    [Header("Lobby Compound Storage")]
    [Tooltip("StoragePanel/Compound_ScrollView/Viewport/Content를 연결합니다. 비워두면 이름으로 자동 탐색합니다.")]
    [SerializeField] private Transform compoundContentRoot;
    [SerializeField] private ScrollRect compoundScrollRect;
    [SerializeField] private Scrollbar compoundVerticalScrollbar;

    [Header("Lobby Relic Storage")]
    [Tooltip("StoragePanel/Relic_Scroll View/Viewport/Content를 연결합니다. 비워두면 이름으로 자동 탐색합니다.")]
    [SerializeField] private Transform relicContentRoot;
    [SerializeField] private ScrollRect relicScrollRect;
    [SerializeField] private Scrollbar relicVerticalScrollbar;

    [Header("Legacy Category Buttons")]
    [SerializeField] private Button itemButton;
    [SerializeField] private Button compoundButton;

    private readonly List<BattleBagItemSlotUI> storageSlots = new();
    private readonly List<BattleBagItemSlotUI> compoundStorageSlots = new();
    private readonly List<BattleBagItemSlotUI> relicStorageSlots = new();
    private StorageCategory storageCategory = StorageCategory.Item;

    [Header("Discard")]
    [SerializeField] private Button discardButton;

    [Header("Detail Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailIconImage;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailDescriptionText;
    [SerializeField] private TMP_Text detailValueText;
    [SerializeField] private Vector2 detailPanelOffset = new Vector2(12f, 0f);

    [Header("Detail Tooltip Fade")]
    [Tooltip("StoragePanel TooltipPanel이 나타나는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float detailFadeInDuration = 0.12f;

    [Tooltip("StoragePanel TooltipPanel이 사라지는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float detailFadeOutDuration = 0.10f;

    [Tooltip("비워두면 TooltipPanel의 CanvasGroup을 자동으로 찾거나 추가합니다.")]
    [SerializeField] private CanvasGroup detailCanvasGroup;

    private BattleBagItemSlotUI selectedSlot;
    private BattleBagItemSlotUI hoveredSlot;
    private readonly List<RaycastResult> pointerRaycastResults = new();
    private bool isItemSelectionMode;
    private Action<string> itemSelectionCallback;
    private Action itemSelectionClosedCallback;
    private Coroutine detailFadeCoroutine;

    private void Awake()
    {
        AutoBind();
        EnsureDetailCanvasGroup();
        HideDetailImmediately();
        ProtectFixedStorageHeaders();
        BindDiscardButton();
        BindStorageCategoryButtons();
    }

    private void OnEnable()
    {
        ProtectFixedStorageHeaders();
        StartCoroutine(ProtectFixedStorageHeadersNextFrame());
        Refresh();
    }

    private void OnDisable()
    {
        EndItemSelectionMode(false, true);
        HideDetailImmediately();
    }

    private void Update()
    {
        if (selectedSlot == null)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        if (IsPointerOverSelectedBagSlotOrDiscardButton())
            return;

        ClearSelectedSlot();
    }

    private void LateUpdate()
    {
        // 외부 로컬라이즈/공용 패널 갱신이 같은 프레임 뒤쪽에서 제목을 덮어써도
        // StoragePanel 내부의 고정 섹션 제목은 항상 의도한 값으로 유지합니다.
        ProtectFixedStorageHeaders();
    }


    private IEnumerator ProtectFixedStorageHeadersNextFrame()
    {
        yield return null;
        ProtectFixedStorageHeaders();
    }

    private void ProtectFixedStorageHeaders()
    {
        ProtectFixedHeader(FindScrollViewHeader("Storage_Scroll View") ?? FindScrollViewHeader("Storage_ScrollView"), "재료");
        ProtectFixedHeader(FindScrollViewHeader("Compound_Scroll View") ?? FindScrollViewHeader("Compound_ScrollView"), "연성제");
        ProtectFixedHeader(FindScrollViewHeader("Relic_Scroll View") ?? FindScrollViewHeader("Relic_ScrollView"), "유물");
    }

    private TMP_Text FindScrollViewHeader(string scrollViewName)
    {
        Transform scrollView = FindDeepChild(transform, scrollViewName);
        if (scrollView == null)
            return null;

        Transform viewport = scrollView.Find("Viewport");
        Transform nameRoot = viewport != null ? viewport.Find("Name") : null;
        Transform nameText = nameRoot != null ? nameRoot.Find("NameText") : null;
        return nameText != null ? nameText.GetComponent<TMP_Text>() : null;
    }

    private static void ProtectFixedHeader(TMP_Text text, string fixedText)
    {
        if (text == null)
            return;

        if (text.GetComponent<LocalizationIgnore>() == null)
            text.gameObject.AddComponent<LocalizationIgnore>();

        // Name 루트 자체도 자동 로컬라이즈 대상에서 제외합니다.
        // 외부 자동 바인더가 자식 NameText를 다시 "보관함" 키로 연결하는 것을 방지합니다.
        Transform nameRoot = text.transform.parent;
        if (nameRoot != null && nameRoot.GetComponent<LocalizationIgnore>() == null)
            nameRoot.gameObject.AddComponent<LocalizationIgnore>();

        LocalizedTMPText localizer = text.GetComponent<LocalizedTMPText>();
        if (localizer != null)
            localizer.enabled = false;

        LocalizeStringEvent legacyLocalizer = text.GetComponent<LocalizeStringEvent>();
        if (legacyLocalizer != null)
            legacyLocalizer.enabled = false;

        text.text = fixedText;
    }

    private void AutoBind()
    {
        if (slotRoot == null)
        {
            Transform foundSlotRoot = FindDeepChild(transform, "SlotRoot");

            if (foundSlotRoot != null)
                slotRoot = foundSlotRoot;
        }

        Transform namedStorageScrollView = FindDeepChild(transform, "Storage_Scroll View") ?? FindDeepChild(transform, "Storage_ScrollView");
        Transform storageScrollView = namedStorageScrollView ?? FindDeepChild(transform, "Scroll View");
        if (storageScrollView != null)
        {
            Transform viewport = FindDeepChild(storageScrollView, "Viewport");

            if (storageContentRoot == null && viewport != null)
            {
                Transform foundContent = FindDeepChild(viewport, "Content");
                if (foundContent != null)
                    storageContentRoot = foundContent;
            }

            if (storageScrollRect == null)
                storageScrollRect = storageScrollView.GetComponent<ScrollRect>();

            if (storageVerticalScrollbar == null)
                storageVerticalScrollbar = FindDeepChild(storageScrollView, "Scrollbar Vertical")?.GetComponent<Scrollbar>();
        }

        Transform compoundScrollView = FindDeepChild(transform, "Compound_Scroll View") ?? FindDeepChild(transform, "Compound_ScrollView");
        if (compoundScrollView != null)
        {
            Transform viewport = FindDeepChild(compoundScrollView, "Viewport");

            if (compoundContentRoot == null && viewport != null)
            {
                Transform foundContent = FindDeepChild(viewport, "Content");
                if (foundContent != null)
                    compoundContentRoot = foundContent;
            }

            if (compoundScrollRect == null)
                compoundScrollRect = compoundScrollView.GetComponent<ScrollRect>();

            if (compoundVerticalScrollbar == null)
                compoundVerticalScrollbar = FindDeepChild(compoundScrollView, "Scrollbar Vertical")?.GetComponent<Scrollbar>();
        }

        Transform relicScrollView = FindDeepChild(transform, "Relic_Scroll View") ?? FindDeepChild(transform, "Relic_ScrollView");
        if (relicScrollView != null)
        {
            Transform viewport = FindDeepChild(relicScrollView, "Viewport");

            if (relicContentRoot == null && viewport != null)
            {
                Transform foundContent = FindDeepChild(viewport, "Content");
                if (foundContent != null)
                    relicContentRoot = foundContent;
            }

            if (relicScrollRect == null)
                relicScrollRect = relicScrollView.GetComponent<ScrollRect>();

            if (relicVerticalScrollbar == null)
                relicVerticalScrollbar = FindDeepChild(relicScrollView, "Scrollbar Vertical")?.GetComponent<Scrollbar>();
        }

        if (storageContentRoot == null)
        {
            Transform foundContent = FindDeepChild(transform, "Content");
            if (foundContent != null)
                storageContentRoot = foundContent;
        }

        BindStorageScrollView(storageScrollRect, storageContentRoot, storageVerticalScrollbar);
        BindStorageScrollView(compoundScrollRect, compoundContentRoot, compoundVerticalScrollbar);
        BindStorageScrollView(relicScrollRect, relicContentRoot, relicVerticalScrollbar);

        if (itemButton == null)
        {
            Transform itemButtonTransform = FindDeepChild(transform, "Item_Button");
            if (itemButtonTransform != null)
                itemButton = itemButtonTransform.GetComponent<Button>();
        }

        if (compoundButton == null)
        {
            Transform compoundButtonTransform = FindDeepChild(transform, "Compound_Button");
            if (compoundButtonTransform != null)
                compoundButton = compoundButtonTransform.GetComponent<Button>();
        }

        if (detailPanel == null)
        {
            Transform tooltip = transform.Find("TooltipPanel");

            if (tooltip != null)
                detailPanel = tooltip.gameObject;
        }

        if (detailPanel != null)
        {
            // 툴팁 패널 자체의 Image는 배경 이미지이므로 아이템 아이콘 출력용으로 사용하지 않습니다.
            if (detailIconImage != null && detailIconImage.transform == detailPanel.transform)
                detailIconImage = null;

            if (detailIconImage == null)
                detailIconImage = FindChildImage(detailPanel.transform, "DetailIconImage", "IconImage", "ItemIconImage", "Icon", "ItemIcon");

            if (detailNameText == null)
                detailNameText = FindChildText(detailPanel.transform, "DetailNameText", "Name", "ItemName", "Title", "Text", "Text (TMP)");

            if (detailDescriptionText == null)
                detailDescriptionText = FindChildText(detailPanel.transform, "Description", "Desc", "Details", "DetailText", "DetailDescriptionText");

            if (detailValueText == null)
                detailValueText = FindChildText(detailPanel.transform, "Value", "Price", "Gold", "ValueText");
        }

        if (discardButton == null)
        {
            Transform discard = FindDeepChild(transform, "DiscardButton")
                                ?? FindDeepChild(transform, "Discard_Button");

            if (discard != null)
                discardButton = discard.GetComponent<Button>();
        }

        BindDiscardButton();
        BindStorageCategoryButtons();
        BuildSlotsIfNeeded();
    }

    private void BindStorageScrollView(
        ScrollRect scrollRect,
        Transform contentRoot,
        Scrollbar verticalScrollbar)
    {
        if (scrollRect == null)
            return;

        if (scrollRect.content == null && contentRoot is RectTransform contentRect)
            scrollRect.content = contentRect;

        if (scrollRect.viewport == null)
        {
            Transform viewportTransform = FindDeepChild(scrollRect.transform, "Viewport");
            if (viewportTransform != null)
                scrollRect.viewport = viewportTransform as RectTransform;
        }

        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        if (verticalScrollbar != null)
        {
            verticalScrollbar.gameObject.SetActive(true);
            scrollRect.verticalScrollbar = verticalScrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }
    }

    private void BuildSlotsIfNeeded()
    {
        slots.RemoveAll(x => x == null);

        if (slots.Count > 0)
            return;

        // 로비 Storage는 Content + Prefab을 사용하는 경우에 슬롯을 생성합니다.
        // StoragePanel의 다른 자식 오브젝트에 슬롯 컴포넌트를 자동 추가하지 않습니다.
        if (storageContentRoot != null && storageSlotPrefab != null)
            return;

        Transform root = slotRoot != null ? slotRoot : transform;
        BattleBagItemSlotUI[] existingSlots = root.GetComponentsInChildren<BattleBagItemSlotUI>(true);

        if (existingSlots != null && existingSlots.Length > 0)
        {
            slots.AddRange(existingSlots);
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child == null)
                continue;

            BattleBagItemSlotUI slot = child.GetComponent<BattleBagItemSlotUI>();

            if (slot == null)
                slot = child.gameObject.AddComponent<BattleBagItemSlotUI>();

            slots.Add(slot);
        }
    }

    public void Refresh()
    {
        AutoBind();

        selectedSlot = null;
        hoveredSlot = null;

        IInventoryRuntimeContext context = ResolveRuntimeContext();

        if (context != null &&
            context.IsLobby &&
            !isItemSelectionMode &&
            storageContentRoot != null &&
            compoundContentRoot != null &&
            storageSlotPrefab != null)
        {
            RefreshLobbyDualStorage(context);
        }
        else
        {
            IReadOnlyList<string> displayedIds = GetDisplayedIds(context);
            List<BagItemStack> stacks = BagItemStackUtility.BuildStacks(displayedIds);

            if (storageContentRoot != null && storageSlotPrefab != null)
            {
                if (context != null && context.IsLobby)
                    RefreshLobbyStorageSlots(stacks);
                else
                    RefreshBattleDynamicSlots(stacks);
            }
            else
            {
                RefreshBattleSlots(stacks);
            }
        }

        HideDetail();
        RefreshDiscardButtonState();
    }

    private void RefreshLobbyDualStorage(IInventoryRuntimeContext context)
    {
        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();

        List<BagItemStack> itemStacks = BagItemStackUtility.BuildStacks(context?.BagItemIds);
        List<BagItemStack> compoundStacks = BagItemStackUtility.BuildStacks(lobby?.StoredCompoundIds);
        List<BagItemStack> relicStacks = BagItemStackUtility.BuildStacks(context?.OwnedRelicIds);

        RefreshLobbyStorageSlots(storageContentRoot, storageSlots, itemStacks, StorageMinimumSlotCount, 1.3f, true);
        RefreshLobbyStorageSlots(compoundContentRoot, compoundStorageSlots, compoundStacks, CompoundMinimumSlotCount, 1.3f, true);

        if (relicContentRoot != null)
            RefreshLobbyStorageSlots(relicContentRoot, relicStorageSlots, relicStacks, RelicMinimumSlotCount, 1.5f, false);
    }

    private void RefreshBattleSlots(List<BagItemStack> stacks)
    {
        int visibleStackCount = Mathf.Min(stacks != null ? stacks.Count : 0, MaxBagItemCount);

        for (int i = 0; i < slots.Count; i++)
        {
            BattleBagItemSlotUI slot = slots[i];

            if (slot == null)
                continue;

            slot.gameObject.SetActive(true);

            if (i < visibleStackCount)
            {
                BagItemStack stack = stacks[i];
                slot.Setup(stack.ItemId, stack.Count, OnFocusSlot, OnExitSlot, OnClickSlot);
            }
            else
            {
                slot.Clear(OnFocusSlot, OnExitSlot, OnClickSlot);
            }
        }
    }

    private void RefreshBattleDynamicSlots(List<BagItemStack> stacks)
    {
        ClearLobbyStorageSlots();

        // 배틀 가방은 서로 다른 재료 8종류까지 보관할 수 있습니다.
        // 보유 종류가 8개보다 적더라도 항상 8개의 슬롯 공간을 유지합니다.
        int stackCount = Mathf.Min(stacks != null ? stacks.Count : 0, MaxBagItemCount);
        int visibleSlotCount = MaxBagItemCount;

        for (int i = 0; i < visibleSlotCount; i++)
        {
            BattleBagItemSlotUI slot = Instantiate(storageSlotPrefab, storageContentRoot, false);
            slot.name = $"{storageSlotPrefab.name}_{i}";
            slot.gameObject.SetActive(true);

            if (i < stackCount)
            {
                BagItemStack stack = stacks[i];
                slot.Setup(stack.ItemId, stack.Count, OnFocusSlot, OnExitSlot, OnClickSlot);
            }
            else
            {
                slot.Clear(OnFocusSlot, OnExitSlot, OnClickSlot);
            }

            storageSlots.Add(slot);
        }

        if (storageContentRoot is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private void RefreshLobbyStorageSlots(List<BagItemStack> stacks)
    {
        RefreshLobbyStorageSlots(storageContentRoot, storageSlots, stacks, StorageMinimumSlotCount, 1.3f, true);
    }

    private void RefreshLobbyStorageSlots(
        Transform contentRoot,
        List<BattleBagItemSlotUI> targetSlots,
        List<BagItemStack> stacks,
        int minimumSlotCount,
        float slotScale,
        bool quantityVisible)
    {
        ClearDynamicStorageSlots(targetSlots);

        if (contentRoot == null || storageSlotPrefab == null)
            return;

        int stackCount = stacks != null ? stacks.Count : 0;
        int visibleSlotCount = Mathf.Max(minimumSlotCount, stackCount);

        for (int i = 0; i < visibleSlotCount; i++)
        {
            BattleBagItemSlotUI slot = Instantiate(storageSlotPrefab, contentRoot, false);
            slot.name = $"{storageSlotPrefab.name}_{i}";
            slot.gameObject.SetActive(true);
            slot.transform.localScale = Vector3.one * slotScale;
            slot.SetQuantityVisible(quantityVisible);

            if (i < stackCount)
            {
                BagItemStack stack = stacks[i];
                slot.Setup(stack.ItemId, stack.Count, OnFocusSlot, OnExitSlot, OnClickSlot);
            }
            else
            {
                slot.Clear(OnFocusSlot, OnExitSlot, OnClickSlot);
            }

            targetSlots.Add(slot);
        }

        if (contentRoot is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private void ClearLobbyStorageSlots()
    {
        ClearDynamicStorageSlots(storageSlots);
        ClearDynamicStorageSlots(compoundStorageSlots);
        ClearDynamicStorageSlots(relicStorageSlots);
    }

    private void ClearDynamicStorageSlots(List<BattleBagItemSlotUI> targetSlots)
    {
        if (targetSlots == null)
            return;

        for (int i = targetSlots.Count - 1; i >= 0; i--)
        {
            BattleBagItemSlotUI slot = targetSlots[i];

            if (slot != null)
            {
                slot.gameObject.SetActive(false);
                Destroy(slot.gameObject);
            }
        }

        targetSlots.Clear();
    }

    public void OpenForItemSelection(
        Action<string> onItemSelected,
        Action onSelectionClosed = null)
    {
        if (onItemSelected == null)
            return;

        isItemSelectionMode = true;
        storageCategory = StorageCategory.Item;
        itemSelectionCallback = onItemSelected;
        itemSelectionClosedCallback = onSelectionClosed;
        gameObject.SetActive(true);
        Refresh();
    }

    public void CancelItemSelection()
    {
        EndItemSelectionMode(false, true);
    }
    private IReadOnlyList<string> GetDisplayedIds(IInventoryRuntimeContext context)
    {
        if (context == null)
            return null;

        if (!context.IsLobby || storageCategory == StorageCategory.Item)
            return context.BagItemIds;

        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        return lobby?.StoredCompoundIds;
    }

    private List<string> GetMutableDisplayedIds(IInventoryRuntimeContext context)
    {
        return GetMutableDisplayedIds(context, selectedSlot != null ? selectedSlot.ItemId : null);
    }

    private List<string> GetMutableDisplayedIds(IInventoryRuntimeContext context, string itemId)
    {
        if (context == null)
            return null;

        if (!context.IsLobby)
            return context.BagItemIds;

        bool isCompound = !string.IsNullOrWhiteSpace(itemId) &&
                          DataManager.Instance?.CompoundDatabase != null &&
                          DataManager.Instance.CompoundDatabase.TryGet(itemId, out _);

        if (!isCompound && storageCategory == StorageCategory.Item)
            return context.BagItemIds;

        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        return isCompound ? lobby?.StoredCompoundIds : context.BagItemIds;
    }

    private IReadOnlyList<string> GetBagItemIds()
    {
        IInventoryRuntimeContext context = ResolveRuntimeContext();

        if (context == null)
            return null;

        return context.BagItemIds;
    }

    private IInventoryRuntimeContext ResolveRuntimeContext()
    {
        if (DataManager.Instance == null)
            return null;

        bool isLobbyScene = string.Equals(
            SceneManager.GetActiveScene().name,
            "Lobby",
            System.StringComparison.OrdinalIgnoreCase);

        // 로비 Storage의 부모에 어떤 RuntimeContextProvider가 있더라도
        // 반드시 LobbyRuntimeData를 표시해야 합니다. Battle 컨텍스트를 잘못 사용하면
        // 로비에 재료가 있어도 빈 슬롯만 보일 수 있습니다.
        if (isLobbyScene && DataManager.Instance.LobbyRuntimeStore != null)
            return InventoryRuntimeContext.ForLobby(DataManager.Instance.LobbyRuntimeStore.GetOrCreate());

        if (runtimeContextProvider == null)
            runtimeContextProvider = GetComponentInParent<InventoryRuntimeContextProvider>(true);

        IInventoryRuntimeContext context = runtimeContextProvider != null
            ? runtimeContextProvider.GetContext()
            : null;

        if (context != null)
            return context;

        if (DataManager.Instance.BattleRuntimeStore != null)
            return InventoryRuntimeContext.ForBattle(DataManager.Instance.BattleRuntimeStore.GetOrCreate());

        return null;
    }

    private void OnFocusSlot(BattleBagItemSlotUI slot)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (slot == null || !slot.HasItem)
            return;

        if (hoveredSlot != null && hoveredSlot != slot)
            hoveredSlot.SetHovered(false);

        hoveredSlot = slot;
        slot.SetHovered(true);
        ShowDetail(slot);
    }

    private void OnExitSlot(BattleBagItemSlotUI slot)
    {
        if (slot == null)
            return;

        if (hoveredSlot == slot)
            hoveredSlot = null;

        slot.SetHovered(false);
        HideDetail();
    }

    private void OnClickSlot(BattleBagItemSlotUI slot)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (slot == null || !slot.HasItem)
            return;

        if (isItemSelectionMode)
        {
            string selectedItemId = slot.ItemId;
            Action<string> callback = itemSelectionCallback;
            EndItemSelectionMode(false, true);
            callback?.Invoke(selectedItemId);
            return;
        }

        if (selectedSlot != null && selectedSlot != slot)
            selectedSlot.SetSelected(false);

        selectedSlot = slot;
        selectedSlot.SetSelected(true);

        // 클릭은 버리기 대상 선택만 처리합니다.
        // 툴팁은 마우스를 올렸을 때만 표시하고, 클릭으로 고정하지 않습니다.
        if (hoveredSlot != slot)
            HideDetail();

        RefreshDiscardButtonState();
    }

    private void ClearSelectedSlot()
    {
        if (selectedSlot != null)
            selectedSlot.SetSelected(false);

        selectedSlot = null;
        RefreshDiscardButtonState();
    }

    private void ClearAllSlotVisualStates()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
                slots[i].ResetVisualState();
        }

        for (int i = 0; i < storageSlots.Count; i++)
        {
            if (storageSlots[i] != null)
                storageSlots[i].ResetVisualState();
        }

        for (int i = 0; i < compoundStorageSlots.Count; i++)
        {
            if (compoundStorageSlots[i] != null)
                compoundStorageSlots[i].ResetVisualState();
        }

        for (int i = 0; i < relicStorageSlots.Count; i++)
        {
            if (relicStorageSlots[i] != null)
                relicStorageSlots[i].ResetVisualState();
        }
    }

    private bool IsPointerOverSelectedBagSlotOrDiscardButton()
    {
        if (EventSystem.current == null)
            return false;

        pointerRaycastResults.Clear();

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        EventSystem.current.RaycastAll(pointerEventData, pointerRaycastResults);

        for (int i = 0; i < pointerRaycastResults.Count; i++)
        {
            GameObject hitObject = pointerRaycastResults[i].gameObject;

            if (hitObject == null)
                continue;

            if (selectedSlot != null && hitObject.GetComponentInParent<BattleBagItemSlotUI>() == selectedSlot)
                return true;

            if (discardButton != null)
            {
                Transform hitTransform = hitObject.transform;
                Transform discardTransform = discardButton.transform;

                if (hitTransform == discardTransform || hitTransform.IsChildOf(discardTransform))
                    return true;
            }
        }

        return false;
    }

    private void ShowDetail(BattleBagItemSlotUI slot)
    {
        if (slot == null || !slot.HasItem)
        {
            HideDetail();
            return;
        }

        ShowDetail(slot.ItemId);
        MoveDetailPanelToRightOfSlot(slot);
    }

    private void ShowDetail(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            HideDetail();
            return;
        }

        ItemData item = null;
        CompoundData compound = null;
        RelicData relic = null;
        Sprite icon = null;

        if (DataManager.Instance != null)
        {
            bool isCompound = DataManager.Instance.CompoundDatabase != null &&
                              DataManager.Instance.CompoundDatabase.TryGet(itemId, out compound);
            bool isRelic = !isCompound &&
                           DataManager.Instance.RelicDatabase != null &&
                           DataManager.Instance.RelicDatabase.TryGet(itemId, out relic);

            if (isCompound || isRelic)
            {
                if (DataManager.Instance.RelicIconDatabase != null)
                    DataManager.Instance.RelicIconDatabase.TryGetIcon(itemId, out icon);
            }
            else
            {
                item = DataManager.Instance.ItemDatabase.Get(itemId);

                if (item != null && DataManager.Instance.ItemIconDatabase != null)
                    DataManager.Instance.ItemIconDatabase.TryGetIcon(itemId, out icon);
            }
        }

        ShowDetailWithFade();

        if (detailIconImage != null && (detailPanel == null || detailIconImage.transform != detailPanel.transform))
        {
            detailIconImage.sprite = icon;
            detailIconImage.enabled = icon != null;
        }

        if (detailNameText != null)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.Name))
                detailNameText.text = GameDataLocalization.ItemName(item);
            else if (compound != null && !string.IsNullOrWhiteSpace(compound.Name))
                detailNameText.text = GameDataLocalization.CompoundName(compound);
            else if (relic != null && !string.IsNullOrWhiteSpace(relic.Name))
                detailNameText.text = GameDataLocalization.RelicName(relic);
            else
                detailNameText.text = itemId;
        }

        if (detailDescriptionText != null)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.Desc))
                detailDescriptionText.text = GameDataLocalization.ItemDescription(item);
            else if (compound != null && !string.IsNullOrWhiteSpace(compound.EffectDesc))
                detailDescriptionText.text = GameDataLocalization.CompoundDescription(compound);
            else if (relic != null && !string.IsNullOrWhiteSpace(relic.EffectDesc))
                detailDescriptionText.text = GameDataLocalization.RelicEffectDescription(relic);
            else
                detailDescriptionText.text = GameLocalization.Get("battle.acquired_item", "획득한 아이템입니다.");
        }

        // 가방 툴팁은 아이템 이름과 설명만 표시합니다.
        // 판매 가격 문구는 GameData Item 시트의 설명(Desc)에 직접 작성해서 사용합니다.
        // DetailValueText가 이름 또는 설명 텍스트와 같은 오브젝트로 잘못 연결되어 있어도
        // 이미 출력된 아이템 이름과 설명을 빈 문자열로 덮어쓰지 않습니다.
        if (detailValueText != null &&
            detailValueText != detailNameText &&
            detailValueText != detailDescriptionText)
        {
            detailValueText.text = "";
        }
    }

    private void MoveDetailPanelToRightOfSlot(BattleBagItemSlotUI slot)
    {
        if (slot == null || detailPanel == null)
            return;

        RectTransform slotRect = slot.RectTransform;
        RectTransform detailRect = detailPanel.transform as RectTransform;

        if (slotRect == null || detailRect == null || detailRect.parent == null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera uiCamera = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);

        Vector3 rightCenterWorld = (corners[2] + corners[3]) * 0.5f;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, rightCenterWorld);

        RectTransform parentRect = detailRect.parent as RectTransform;

        if (parentRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCamera, out Vector2 localPoint))
            return;

        detailRect.pivot = new Vector2(0f, 0.5f);
        detailRect.anchoredPosition = localPoint + detailPanelOffset;
    }

    private void ShowDetailWithFade()
    {
        if (detailPanel == null)
            return;

        EnsureDetailCanvasGroup();

        if (!detailPanel.activeSelf)
            detailPanel.SetActive(true);

        if (detailCanvasGroup == null)
            return;

        detailCanvasGroup.interactable = false;
        detailCanvasGroup.blocksRaycasts = false;

        StartDetailFade(1f, detailFadeInDuration, false);
    }

    private void HideDetail()
    {
        if (detailPanel == null || !detailPanel.activeSelf)
            return;

        EnsureDetailCanvasGroup();

        if (detailCanvasGroup == null)
        {
            detailPanel.SetActive(false);
            return;
        }

        StartDetailFade(0f, detailFadeOutDuration, true);
    }

    private void EnsureDetailCanvasGroup()
    {
        if (detailPanel == null)
            return;

        if (detailCanvasGroup == null)
            detailCanvasGroup = detailPanel.GetComponent<CanvasGroup>();

        if (detailCanvasGroup == null)
            detailCanvasGroup = detailPanel.AddComponent<CanvasGroup>();

        detailCanvasGroup.interactable = false;
        detailCanvasGroup.blocksRaycasts = false;
    }

    private void StartDetailFade(float targetAlpha, float duration, bool deactivateAfterFade)
    {
        if (detailCanvasGroup == null)
            return;

        if (detailFadeCoroutine != null)
        {
            StopCoroutine(detailFadeCoroutine);
            detailFadeCoroutine = null;
        }

        if (duration <= 0f)
        {
            detailCanvasGroup.alpha = targetAlpha;

            if (deactivateAfterFade && detailPanel != null)
                detailPanel.SetActive(false);

            return;
        }

        detailFadeCoroutine = StartCoroutine(FadeDetailRoutine(targetAlpha, duration, deactivateAfterFade));
    }

    private IEnumerator FadeDetailRoutine(float targetAlpha, float duration, bool deactivateAfterFade)
    {
        float startAlpha = detailCanvasGroup != null ? detailCanvasGroup.alpha : targetAlpha;
        float elapsed = 0f;

        while (detailCanvasGroup != null && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            detailCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        if (detailCanvasGroup != null)
            detailCanvasGroup.alpha = targetAlpha;

        detailFadeCoroutine = null;

        if (deactivateAfterFade && detailPanel != null)
            detailPanel.SetActive(false);
    }

    private void HideDetailImmediately()
    {
        if (detailFadeCoroutine != null)
        {
            StopCoroutine(detailFadeCoroutine);
            detailFadeCoroutine = null;
        }

        EnsureDetailCanvasGroup();

        if (detailCanvasGroup != null)
            detailCanvasGroup.alpha = 0f;

        if (detailPanel != null && detailPanel.activeSelf)
            detailPanel.SetActive(false);
    }

    private void BindStorageCategoryButtons()
    {
        bool usesDualLobbyLayout = storageContentRoot != null && compoundContentRoot != null;
        if (usesDualLobbyLayout)
            return;

        if (itemButton != null)
        {
            itemButton.onClick.RemoveListener(ShowStoredItems);
            itemButton.onClick.AddListener(ShowStoredItems);
        }

        if (compoundButton != null)
        {
            compoundButton.onClick.RemoveListener(ShowStoredCompounds);
            compoundButton.onClick.AddListener(ShowStoredCompounds);
        }
    }

    private void ShowStoredItems()
    {
        storageCategory = StorageCategory.Item;
        Refresh();
        ResetStorageScrollToTop();
    }

    private void ShowStoredCompounds()
    {
        if (isItemSelectionMode)
            return;

        storageCategory = StorageCategory.Compound;
        Refresh();
        ResetStorageScrollToTop();
    }

    private void ResetStorageScrollToTop()
    {
        if (storageScrollRect == null)
            return;

        // 씬 전환으로 Content가 다시 생성된 직후 레이아웃을 먼저 확정한 다음
        // 스크롤을 항상 최상단에서 시작하도록 맞춥니다.
        Canvas.ForceUpdateCanvases();

        if (storageScrollRect.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(storageScrollRect.content);

        storageScrollRect.StopMovement();
        storageScrollRect.verticalNormalizedPosition = 1f;

        if (storageVerticalScrollbar != null)
            storageVerticalScrollbar.value = 1f;
    }

    private void BindDiscardButton()
    {
        if (discardButton == null)
            return;

        discardButton.onClick.RemoveListener(OnClickDiscardButton);
        discardButton.onClick.AddListener(OnClickDiscardButton);
        RefreshDiscardButtonState();
    }

    private void RefreshDiscardButtonState()
    {
        if (discardButton != null)
            discardButton.interactable = !isItemSelectionMode && IsDiscardAllowed() && selectedSlot != null && selectedSlot.HasItem;
    }

    private void EndItemSelectionMode(bool hidePanel, bool notifyClosed)
    {
        if (!isItemSelectionMode)
            return;

        isItemSelectionMode = false;
        itemSelectionCallback = null;
        Action closedCallback = itemSelectionClosedCallback;
        itemSelectionClosedCallback = null;

        ClearAllSlotVisualStates();
        selectedSlot = null;
        hoveredSlot = null;
        HideDetail();
        RefreshDiscardButtonState();

        if (notifyClosed)
            closedCallback?.Invoke();

        if (hidePanel && gameObject.activeSelf)
            gameObject.SetActive(false);
    }
    private void OnClickDiscardButton()
    {
        if (IsNetworkBattleClientReadOnly())
        {
            BattleWarningUI.ShowMessage(GameLocalization.Get("battle.host_only_bag_change", "멀티 배틀에서는 호스트만 가방을 변경할 수 있습니다."));
            return;
        }

        IInventoryRuntimeContext context = ResolveRuntimeContext();

        if (!IsDiscardAllowed(context))
            return;

        if (selectedSlot == null || !selectedSlot.HasItem)
        {
            BattleWarningUI.ShowMessage(GameLocalization.Get("battle.select_item_to_discard", "버릴 고유 아이템을 먼저 선택해 주세요."));
            return;
        }

        if (context == null)
            return;

        string removedItemId = selectedSlot.ItemId;

        List<string> displayedIds = GetMutableDisplayedIds(context, removedItemId);

        if (!BagItemStackUtility.RemoveOne(displayedIds, removedItemId))
        {
            BattleWarningUI.ShowMessage(GameLocalization.Get("battle.selected_item_not_found", "선택한 고유 아이템을 찾을 수 없습니다."));
            Refresh();
            return;
        }

        if (selectedSlot != null)
            selectedSlot.ResetVisualState();

        if (hoveredSlot != null && hoveredSlot != selectedSlot)
            hoveredSlot.ResetVisualState();

        ClearAllSlotVisualStates();

        SaveRuntimeContext(context);

        selectedSlot = null;
        hoveredSlot = null;
        HideDetail();
        Refresh();

        Debug.Log($"[BattleBagPanelUI] 고유 아이템을 버렸습니다. Item:{removedItemId}");
    }

    private bool IsDiscardAllowed()
    {
        return IsDiscardAllowed(ResolveRuntimeContext());
    }

    private bool IsDiscardAllowed(IInventoryRuntimeContext context)
    {
        if (IsNetworkBattleClientReadOnly())
            return false;

        return context == null || !context.IsLobby || allowDiscardInLobby;
    }

    private static bool IsNetworkBattleClientReadOnly()
    {
        SteamBattleStateSynchronizer synchronizer = SteamBattleStateSynchronizer.Instance;
        return synchronizer != null &&
               synchronizer.IsNetworkBattleActive &&
               !SteamLobbySessionState.IsLocalHost;
    }

    private void SaveRuntimeContext(IInventoryRuntimeContext context)
    {
        if (context == null || DataManager.Instance == null)
            return;

        if (context.IsLobby)
        {
            LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore?.GetOrCreate();
            if (lobby != null)
                DataManager.Instance.LobbyRuntimeStore.Set(lobby);
            return;
        }

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore?.GetOrCreate();
        if (runtime != null)
            DataManager.Instance.BattleRuntimeStore.Set(runtime);
    }

    private Image FindChildImage(Transform root, params string[] names)
    {
        if (root == null)
            return null;

        for (int i = 0; i < names.Length; i++)
        {
            Transform child = FindDeepChild(root, names[i]);

            if (child == null || child == root)
                continue;

            Image image = child.GetComponent<Image>();

            if (image != null)
                return image;
        }

        // 이름에 맞는 아이콘 자식을 찾지 못했다면 배경 이미지를 잘못 집지 않도록 null을 반환합니다.
        return null;
    }

    private TMP_Text FindChildText(Transform root, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = FindDeepChild(root, names[i]);

            if (child == null)
                continue;

            TMP_Text text = child.GetComponent<TMP_Text>();

            if (text != null)
                return text;
        }

        return root.GetComponentInChildren<TMP_Text>(true);
    }

    private Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), childName);

            if (found != null)
                return found;
        }

        return null;
    }

    public static void RefreshAll()
    {
        BattleBagPanelUI[] panels = UnityEngine.Object.FindObjectsByType<BattleBagPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
                panels[i].Refresh();
        }
    }
}
