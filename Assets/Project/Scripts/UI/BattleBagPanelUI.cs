using System.Collections.Generic;
using System;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleBagPanelUI : MonoBehaviour
{
    private enum StorageCategory
    {
        Item,
        Compound
    }

    private const int MaxBagItemCount = 8;
    private const int StorageMinimumSlotCount = 36;

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

    [Header("Legacy Category Buttons")]
    [SerializeField] private Button itemButton;
    [SerializeField] private Button compoundButton;

    private readonly List<BattleBagItemSlotUI> storageSlots = new();
    private readonly List<BattleBagItemSlotUI> compoundStorageSlots = new();
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

    private BattleBagItemSlotUI selectedSlot;
    private BattleBagItemSlotUI hoveredSlot;
    private readonly List<RaycastResult> pointerRaycastResults = new();
    private bool isItemSelectionMode;
    private Action<string> itemSelectionCallback;
    private Action itemSelectionClosedCallback;

    private void Awake()
    {
        AutoBind();
        BindDiscardButton();
        BindStorageCategoryButtons();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDisable()
    {
        EndItemSelectionMode(false, true);
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

    private void AutoBind()
    {
        if (slotRoot == null)
        {
            Transform foundSlotRoot = FindDeepChild(transform, "SlotRoot");

            if (foundSlotRoot != null)
                slotRoot = foundSlotRoot;
        }

        Transform namedStorageScrollView = FindDeepChild(transform, "Storage_ScrollView");
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

        Transform compoundScrollView = FindDeepChild(transform, "Compound_ScrollView");
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

        if (storageContentRoot == null)
        {
            Transform foundContent = FindDeepChild(transform, "Content");
            if (foundContent != null)
                storageContentRoot = foundContent;
        }

        BindStorageScrollView(storageScrollRect, storageContentRoot, storageVerticalScrollbar);
        BindStorageScrollView(compoundScrollRect, compoundContentRoot, compoundVerticalScrollbar);

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
            // ?댄똻?⑤꼸 ?먭린 ?먯떊??Image??諛곌꼍 ?대?吏?대?濡??꾩씠???꾩씠肄?異쒕젰?⑹쑝濡??ъ슜?섏? ?딆뒿?덈떎.
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

        // 濡쒕퉬 Storage??Content + Prefab???ъ슜???고??꾩뿉 ?щ’???앹꽦?⑸땲??
        // StoragePanel???ㅻⅨ ?먯떇 ?ㅻ툕?앺듃???щ’ 而댄룷?뚰듃瑜??먮룞 異붽??섏? ?딆뒿?덈떎.
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

        RefreshLobbyStorageSlots(storageContentRoot, storageSlots, itemStacks);
        RefreshLobbyStorageSlots(compoundContentRoot, compoundStorageSlots, compoundStacks);
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

        // 諛고? 媛諛⑹? ?쒕줈 ?ㅻⅨ ?щ즺 8醫낅쪟源뚯? 蹂닿??????덉뒿?덈떎.
        // 蹂댁쑀 醫낅쪟媛 8媛쒕낫???곷뜑?쇰룄 ??긽 8媛쒖쓽 ?щ’ 怨듦컙???좎??⑸땲??
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
        RefreshLobbyStorageSlots(storageContentRoot, storageSlots, stacks);
    }

    private void RefreshLobbyStorageSlots(
        Transform contentRoot,
        List<BattleBagItemSlotUI> targetSlots,
        List<BagItemStack> stacks)
    {
        ClearDynamicStorageSlots(targetSlots);

        if (contentRoot == null || storageSlotPrefab == null)
            return;

        int stackCount = stacks != null ? stacks.Count : 0;
        int visibleSlotCount = Mathf.Max(StorageMinimumSlotCount, stackCount);

        for (int i = 0; i < visibleSlotCount; i++)
        {
            BattleBagItemSlotUI slot = Instantiate(storageSlotPrefab, contentRoot, false);
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

            targetSlots.Add(slot);
        }

        if (contentRoot is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private void ClearLobbyStorageSlots()
    {
        ClearDynamicStorageSlots(storageSlots);
        ClearDynamicStorageSlots(compoundStorageSlots);
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

        // 濡쒕퉬 Storage??遺紐⑥뿉 ?대뼡 RuntimeContextProvider媛 ?덈뜑?쇰룄
        // 諛섎뱶??LobbyRuntimeData瑜??쒖떆?댁빞 ?⑸땲?? Battle ?뚯뒪瑜??섎せ 臾쇰㈃
        // 濡쒕퉬???щ즺媛 ?덉뼱??36媛쒖쓽 鍮??щ’留?蹂댁씪 ???덉뒿?덈떎.
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

        // ?대┃? 踰꾨━湲?????좏깮留?泥섎━?⑸땲??
        // ?댄똻? 留덉슦?ㅻ? ?щ졇???뚮쭔 ?쒖떆?섍퀬, ?대┃?쇰줈 怨좎젙?섏? ?딆뒿?덈떎.
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
        Sprite icon = null;

        if (DataManager.Instance != null)
        {
            bool isCompound = DataManager.Instance.CompoundDatabase != null &&
                              DataManager.Instance.CompoundDatabase.TryGet(itemId, out compound);

            if (isCompound)
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

        if (detailPanel != null)
            detailPanel.SetActive(true);

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
            else
                detailNameText.text = itemId;
        }

        if (detailDescriptionText != null)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.Desc))
                detailDescriptionText.text = GameDataLocalization.ItemDescription(item);
            else if (compound != null && !string.IsNullOrWhiteSpace(compound.EffectDesc))
                detailDescriptionText.text = GameDataLocalization.CompoundDescription(compound);
            else
                detailDescriptionText.text = GameLocalization.Get("battle.acquired_item", "?띾뱷???꾩씠?쒖엯?덈떎.");
        }

        // 媛諛??댄똻? ?꾩씠???대쫫怨??ㅻ챸留??쒖떆?⑸땲??
        // ?먮ℓ 媛寃?臾멸뎄??GameData Item ?쒗듃???ㅻ챸(Desc)??吏곸젒 ?묒꽦?댁꽌 ?ъ슜?⑸땲??
        // DetailValueText媛 ?대쫫 ?먮뒗 ?ㅻ챸 ?띿뒪?몄? 媛숈? ?ㅻ툕?앺듃濡??섎せ ?곌껐?섏뼱 ?덉뼱??
        // ?대? 異쒕젰???꾩씠???대쫫/?ㅻ챸??鍮?臾몄옄?대줈 ??뼱?곗? ?딆뒿?덈떎.
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

    private void HideDetail()
    {
        if (detailPanel != null)
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

        // ???꾪솚?쇰줈 Content媛 ?ㅼ떆 ?앹꽦??吏곹썑 ?덉씠?꾩썐??癒쇱? ?뺤젙???ㅼ쓬
        // ?ㅽ겕濡ㅼ쓣 ??긽 理쒖긽?⑥뿉???쒖옉?섎룄濡?留욎땅?덈떎.
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
            BattleWarningUI.ShowMessage(GameLocalization.Get("battle.host_only_bag_change", "硫??諛고??먯꽌???몄뒪?몃쭔 媛諛⑹쓣 蹂寃쏀븷 ???덉뒿?덈떎."));
            return;
        }

        IInventoryRuntimeContext context = ResolveRuntimeContext();

        if (!IsDiscardAllowed(context))
            return;

        if (selectedSlot == null || !selectedSlot.HasItem)
        {
            BattleWarningUI.ShowMessage(GameLocalization.Get("battle.select_item_to_discard", "踰꾨┫ 怨좎쑀?꾩씠?쒖쓣 癒쇱? ?좏깮?댁＜?몄슂."));
            return;
        }

        if (context == null)
            return;

        string removedItemId = selectedSlot.ItemId;

        List<string> displayedIds = GetMutableDisplayedIds(context, removedItemId);

        if (!BagItemStackUtility.RemoveOne(displayedIds, removedItemId))
        {
            BattleWarningUI.ShowMessage(GameLocalization.Get("battle.selected_item_not_found", "?좏깮??怨좎쑀?꾩씠?쒖쓣 李얠쓣 ???놁뒿?덈떎."));
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

        Debug.Log($"[BattleBagPanelUI] 怨좎쑀?꾩씠?쒖쓣 踰꾨졇?듬땲?? Item:{removedItemId}");
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

        // ?대쫫??留욌뒗 ?꾩씠肄??먯떇??李얠? 紐삵뻽?ㅻ㈃ 諛곌꼍 ?대?吏瑜??섎せ ?≪? ?딅룄濡?null??諛섑솚?⑸땲??
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
