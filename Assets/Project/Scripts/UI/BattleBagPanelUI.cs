using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleBagPanelUI : MonoBehaviour
{
    private const int MaxBagItemCount = 8;

    [Header("Slot List")]
    [SerializeField] private Transform slotRoot;
    [SerializeField] private List<BattleBagItemSlotUI> slots = new();

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

    private void Awake()
    {
        AutoBind();
        BindDiscardButton();
    }

    private void OnEnable()
    {
        Refresh();
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
            Transform foundSlotRoot = transform.Find("SlotRoot");

            if (foundSlotRoot != null)
                slotRoot = foundSlotRoot;
        }

        if (detailPanel == null)
        {
            Transform tooltip = transform.Find("TooltipPanel");

            if (tooltip != null)
                detailPanel = tooltip.gameObject;
        }

        if (detailPanel != null)
        {
            // 툴팁패널 자기 자신의 Image는 배경 이미지이므로 아이템 아이콘 출력용으로 사용하지 않습니다.
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
            Transform discard = FindDeepChild(transform, "DiscardButton");

            if (discard != null)
                discardButton = discard.GetComponent<Button>();
        }

        BindDiscardButton();
        BuildSlotsIfNeeded();
    }

    private void BuildSlotsIfNeeded()
    {
        slots.RemoveAll(x => x == null);

        if (slots.Count > 0)
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

        IReadOnlyList<string> itemIds = GetBagItemIds();

        for (int i = 0; i < slots.Count; i++)
        {
            BattleBagItemSlotUI slot = slots[i];

            if (slot == null)
                continue;

            if (itemIds != null && i < itemIds.Count && i < MaxBagItemCount)
                slot.Setup(itemIds[i], OnFocusSlot, OnExitSlot, OnClickSlot);
            else
                slot.Clear(OnFocusSlot, OnExitSlot, OnClickSlot);
        }

        HideDetail();
        RefreshDiscardButtonState();
    }

    private IReadOnlyList<string> GetBagItemIds()
    {
        if (DataManager.Instance == null || DataManager.Instance.BattleRuntimeStore == null)
            return null;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.Get();

        if (runtime == null)
            return null;

        runtime.BagItemIds ??= new List<string>();
        return runtime.BagItemIds;
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
        Sprite icon = null;

        if (DataManager.Instance != null)
        {
            item = DataManager.Instance.ItemDatabase.Get(itemId);

            if (DataManager.Instance.ItemIconDatabase != null)
                DataManager.Instance.ItemIconDatabase.TryGetIcon(itemId, out icon);
        }

        if (detailPanel != null)
            detailPanel.SetActive(true);

        if (detailIconImage != null && (detailPanel == null || detailIconImage.transform != detailPanel.transform))
        {
            detailIconImage.sprite = icon;
            detailIconImage.enabled = icon != null;
        }

        if (detailNameText != null)
            detailNameText.text = item != null && !string.IsNullOrWhiteSpace(item.Name) ? item.Name : itemId;

        if (detailDescriptionText != null)
            detailDescriptionText.text = item != null && !string.IsNullOrWhiteSpace(item.Desc) ? item.Desc : "획득한 아이템입니다.";

        // 가방 툴팁은 아이템 이름과 설명만 표시합니다.
        // 판매 가격 문구는 GameData Item 시트의 설명(Desc)에 직접 작성해서 사용합니다.
        // DetailValueText가 이름 또는 설명 텍스트와 같은 오브젝트로 잘못 연결되어 있어도
        // 이미 출력한 아이템 이름/설명을 빈 문자열로 덮어쓰지 않습니다.
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
            discardButton.interactable = selectedSlot != null && selectedSlot.HasItem;
    }

    private void OnClickDiscardButton()
    {
        if (selectedSlot == null || !selectedSlot.HasItem)
        {
            BattleWarningUI.ShowMessage("버릴 고유아이템을 먼저 선택해주세요.");
            return;
        }

        int slotIndex = slots.IndexOf(selectedSlot);

        if (slotIndex < 0)
        {
            BattleWarningUI.ShowMessage("선택한 고유아이템을 찾을 수 없습니다.");
            Refresh();
            return;
        }

        if (DataManager.Instance == null || DataManager.Instance.BattleRuntimeStore == null)
            return;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();
        runtime.BagItemIds ??= new List<string>();

        if (slotIndex >= runtime.BagItemIds.Count)
        {
            Refresh();
            return;
        }

        string removedItemId = runtime.BagItemIds[slotIndex];

        if (selectedSlot != null)
            selectedSlot.ResetVisualState();

        if (hoveredSlot != null && hoveredSlot != selectedSlot)
            hoveredSlot.ResetVisualState();

        ClearAllSlotVisualStates();

        runtime.BagItemIds.RemoveAt(slotIndex);
        DataManager.Instance.BattleRuntimeStore.Set(runtime);

        selectedSlot = null;
        hoveredSlot = null;
        HideDetail();
        Refresh();

        Debug.Log($"[BattleBagPanelUI] 고유아이템을 버렸습니다. Item:{removedItemId}");
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

        // 이름이 맞는 아이콘 자식을 찾지 못했다면 배경 이미지를 잘못 잡지 않도록 null을 반환합니다.
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
        BattleBagPanelUI[] panels = Object.FindObjectsByType<BattleBagPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
                panels[i].Refresh();
        }
    }
}
