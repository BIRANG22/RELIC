using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Relic.Gameplay.Data;

/// <summary>
/// 지도/이벤트/휴식방의 월드 캐릭터에 마우스를 올렸을 때 해당 캐릭터의 HP를 표시합니다.
/// Character Resources 아래 Char1~3은 파티 슬롯 0~2에 대응합니다.
/// </summary>
public sealed class BattleCharacterResourcesUI : MonoBehaviour
{
    public enum AnchorMode
    {
        MapEvent,
        Rest
    }

    private const int PartySlotCount = 3;

    [Header("Character Panels")]
    [Tooltip("Char1, Char2, Char3. 비워두면 자식 이름으로 자동 연결합니다.")]
    [SerializeField] private GameObject[] characterPanels = new GameObject[PartySlotCount];

    [Header("World Anchor Offset")]
    [Tooltip("지도/이벤트에서 EventAllyPoint0~2 기준으로 더할 UI 위치 오프셋입니다.")]
    [SerializeField] private Vector2[] mapEventOffsets = new Vector2[PartySlotCount];
    [Tooltip("휴식방에서 AllySpawnPoint_01~03 기준으로 더할 UI 위치 오프셋입니다.")]
    [SerializeField] private Vector2[] restOffsets = new Vector2[PartySlotCount];

    [Header("World Camera")]
    [Tooltip("비워두면 Main Camera를 사용합니다.")]
    [SerializeField] private Camera worldCamera;

    private readonly SlotView[] slotViews = new SlotView[PartySlotCount];
    private int visibleSlot = -1;
    private Transform visibleWorldAnchor;
    private AnchorMode visibleAnchorMode;
    private Canvas parentCanvas;

    private sealed class SlotView
    {
        public GameObject Root;
        public RectTransform Rect;
        public Image HpFill;
        public TMP_Text HpValue;
    }

    private void Awake()
    {
        ResolveReferences();
        HideAll();
    }

    private void OnEnable()
    {
        ResolveReferences();
        HideAll();
    }

    private void LateUpdate()
    {
        if (visibleSlot < 0)
            return;

        if (IsCharacterResourceHoverBlocked())
        {
            HideAll();
            return;
        }

        RefreshSlot(visibleSlot);
        UpdateVisiblePanelPosition();
    }

    public void ShowForPartySlot(int slotIndex, Transform worldAnchor, AnchorMode anchorMode)
    {
        if (slotIndex < 0 || slotIndex >= PartySlotCount || worldAnchor == null)
            return;

        ResolveReferences();

        if (IsCharacterResourceHoverBlocked())
        {
            HideAll();
            return;
        }

        if (!TryGetCharacterRuntime(slotIndex, out CharacterRuntimeData runtime, out CharacterMasterData master))
        {
            HideAll();
            return;
        }

        for (int i = 0; i < slotViews.Length; i++)
        {
            SlotView view = slotViews[i];
            if (view?.Root != null)
                view.Root.SetActive(i == slotIndex);
        }

        visibleSlot = slotIndex;
        visibleWorldAnchor = worldAnchor;
        visibleAnchorMode = anchorMode;

        Bind(slotViews[slotIndex], runtime, master);
        UpdateVisiblePanelPosition();
    }

    public void HideForPartySlot(int slotIndex)
    {
        if (visibleSlot != slotIndex)
            return;

        HideAll();
    }

    public void HideAll()
    {
        visibleSlot = -1;
        visibleWorldAnchor = null;

        ResolveReferences();
        for (int i = 0; i < slotViews.Length; i++)
        {
            SlotView view = slotViews[i];
            if (view?.Root != null && view.Root.activeSelf)
                view.Root.SetActive(false);
        }
    }

    private void RefreshSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotViews.Length)
            return;

        if (!TryGetCharacterRuntime(slotIndex, out CharacterRuntimeData runtime, out CharacterMasterData master))
        {
            HideAll();
            return;
        }

        Bind(slotViews[slotIndex], runtime, master);
    }

    private static bool TryGetCharacterRuntime(
        int slotIndex,
        out CharacterRuntimeData runtime,
        out CharacterMasterData master)
    {
        runtime = null;
        master = null;

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null || dataManager.PartyRuntimeStore == null || dataManager.CharacterRuntimeStore == null)
            return false;

        string characterId = dataManager.PartyRuntimeStore.GetCharacterId(slotIndex);
        if (string.IsNullOrWhiteSpace(characterId))
            return false;

        runtime = dataManager.CharacterRuntimeStore.Get(characterId);
        if (runtime == null)
            return false;

        dataManager.CharacterDatabase?.TryGet(characterId, out master);
        return true;
    }

    private static void Bind(SlotView view, CharacterRuntimeData runtime, CharacterMasterData master)
    {
        if (view == null || runtime == null)
            return;

        int maxHp = Mathf.Max(1, BattleEquipmentEffectService.GetEffectiveMaxHP(runtime, master));
        int currentHp = Mathf.Clamp(runtime.CurrentHP, 0, maxHp);

        if (view.HpFill != null)
            view.HpFill.fillAmount = Mathf.Clamp01(currentHp / (float)maxHp);

        if (view.HpValue != null)
            view.HpValue.text = $"{currentHp}/{maxHp}";
    }

    private void UpdateVisiblePanelPosition()
    {
        if (visibleSlot < 0 || visibleSlot >= slotViews.Length || visibleWorldAnchor == null)
            return;

        SlotView view = slotViews[visibleSlot];
        if (view?.Rect == null)
            return;

        RectTransform parentRect = view.Rect.parent as RectTransform;
        if (parentRect == null)
            return;

        Camera camera = worldCamera != null ? worldCamera : Camera.main;
        if (camera == null)
            return;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, visibleWorldAnchor.position);

        Camera uiCamera = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = parentCanvas.worldCamera != null ? parentCanvas.worldCamera : camera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCamera, out Vector2 localPoint))
            return;

        Vector2 offset = GetOffset(visibleSlot, visibleAnchorMode);
        Vector3 currentLocal = view.Rect.localPosition;
        view.Rect.localPosition = new Vector3(localPoint.x + offset.x, localPoint.y + offset.y, currentLocal.z);
    }

    private Vector2 GetOffset(int slotIndex, AnchorMode mode)
    {
        Vector2[] offsets = mode == AnchorMode.Rest ? restOffsets : mapEventOffsets;
        if (offsets == null || slotIndex < 0 || slotIndex >= offsets.Length)
            return Vector2.zero;

        return offsets[slotIndex];
    }

    private bool IsCharacterResourceHoverBlocked()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            RaycastResult result = raycastResults[i];
            if (result.gameObject == null)
                continue;

            // 월드 PhysicsRaycaster 결과는 제외하고 실제 UI GraphicRaycaster 결과만 확인합니다.
            if (!(result.module is GraphicRaycaster))
                continue;

            Transform hitTransform = result.gameObject.transform;

            // Character Resources 자신의 Back/Fill/Value는 자기 자신을 차단하지 않습니다.
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            // 마우스 위치에 다른 UI가 하나라도 있으면 월드 캐릭터 호버 UI를 표시하지 않습니다.
            return true;
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        if (characterPanels == null || characterPanels.Length != PartySlotCount)
            characterPanels = new GameObject[PartySlotCount];

        EnsureOffsetArray(ref mapEventOffsets);
        EnsureOffsetArray(ref restOffsets);

        for (int i = 0; i < PartySlotCount; i++)
        {
            if (characterPanels[i] == null)
            {
                Transform panel = FindChildRecursive(transform, $"Char{i + 1}");
                if (panel != null)
                    characterPanels[i] = panel.gameObject;
            }

            GameObject root = characterPanels[i];
            if (root == null)
                continue;

            SlotView view = slotViews[i] ??= new SlotView();
            view.Root = root;
            view.Rect = root.GetComponent<RectTransform>();

            Transform hp = FindChildRecursive(root.transform, "Hp");
            view.HpFill = GetImage(hp, "Fill");
            view.HpValue = GetText(hp, "Value");
        }
    }

    private static void EnsureOffsetArray(ref Vector2[] offsets)
    {
        if (offsets != null && offsets.Length == PartySlotCount)
            return;

        Vector2[] resized = new Vector2[PartySlotCount];
        if (offsets != null)
        {
            int copyCount = Mathf.Min(offsets.Length, resized.Length);
            for (int i = 0; i < copyCount; i++)
                resized[i] = offsets[i];
        }

        offsets = resized;
    }

    private static Image GetImage(Transform root, string childName)
    {
        Transform child = FindChildRecursive(root, childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private static TMP_Text GetText(Transform root, string childName)
    {
        Transform child = FindChildRecursive(root, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
