using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Relic.Gameplay.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Arranges SpawnGridCell UI objects into a perspective quad without world meshes.
/// Column-first, top-to-bottom order: Grid01..05, Grid06..10, Grid11..15.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SpawnGridUIWarpController : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField, Min(1)] private int width = 3;
    [SerializeField, Min(1)] private int height = 5;

    [Header("Board Corners - Grid Root Local UI Coordinates")]
    [SerializeField] private Vector2 bottomLeft = new Vector2(-300f, -250f);
    [SerializeField] private Vector2 bottomRight = new Vector2(300f, -250f);
    [SerializeField] private Vector2 topLeft = new Vector2(-300f, 250f);
    [SerializeField] private Vector2 topRight = new Vector2(300f, 250f);

    [Header("Cell Spacing (UI Units)")]
    [Tooltip("Horizontal distance between adjacent cell edges. The grid's four outer corners stay fixed.")]
    [SerializeField, Min(0f)] private float horizontalSpacing = 0f;
    [Tooltip("Vertical distance between adjacent cell edges. The grid's four outer corners stay fixed.")]
    [SerializeField, Min(0f)] private float verticalSpacing = 0f;

    [Header("SpawnGridCell Order (Optional)")]
    [Tooltip("Leave empty to find SpawnGridCell children in hierarchy order. Each column is top-to-bottom, starting with the left column.")]
    [SerializeField] private SpawnGridCell[] cells;

    [Header("Visuals")]
    [SerializeField] private bool warpBack = true;
    [SerializeField] private bool warpSelect = true;
    [SerializeField] private bool warpLine = true;
    [SerializeField, Range(0f, 0.35f)] private float visualInset = 0f;
    [Tooltip("Only arrange Char position at the cell center; never warp its image.")]
    [SerializeField] private bool centerCharacter = false;
    [SerializeField] private Vector2 characterOffset;

    [Header("Ready Party Portraits")]
    [Tooltip("SC01~SC03의 Mask/Char 이미지입니다. 비워두면 Ready_Panel/Party/Select_Char에서 자동 탐색합니다.")]
    [SerializeField] private Image[] partyPortraitImages = new Image[3];

    [Header("Ready Party Sort")]
    [Tooltip("Select_Char 아래 SC 오브젝트의 Number 이미지에 사용할 1, 2, 3번 스프라이트")]
    [SerializeField] private Sprite[] partyNumberSprites = new Sprite[3];

    private RectTransform partyListRoot;
    private readonly List<RectTransform> partyCards = new List<RectTransform>(3);
    private readonly List<string> placementOrder = new List<string>(3);
    private readonly List<string> currentPlaced = new List<string>(3);
    [SerializeField, Min(0.05f)] private float partyReorderDuration = 0.20f;
    private readonly Dictionary<RectTransform, Vector2> motionStarts = new Dictionary<RectTransform, Vector2>();
    private readonly Dictionary<RectTransform, Vector2> motionTargets = new Dictionary<RectTransform, Vector2>();
    private float motionStartTime;
    private bool motionActive;

    private void Start()
    {
        if (!Application.isPlaying) return;
        BindPartyPortraitImages();
        SetupPartyDrag();
        RefreshPartyPortraits();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        RefreshPartyPortraits();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || !motionActive) return;
        float t = Mathf.Clamp01((Time.unscaledTime - motionStartTime) / Mathf.Max(0.05f, partyReorderDuration));
        t = t * t * (3f - 2f * t);
        foreach (var pair in motionTargets)
        {
            if (pair.Key == null) continue;
            Vector2 start;
            if (motionStarts.TryGetValue(pair.Key, out start))
                pair.Key.anchoredPosition = Vector2.LerpUnclamped(start, pair.Value, t);
        }
        if (t >= 1f) motionActive = false;
    }

    // LayoutGroup의 목표 위치를 구한 뒤, 이전 화면 위치에서 부드럽게 이동합니다.
    internal void MovePartyCard(RectTransform card, int destination)
    {
        if (partyListRoot == null || card == null || partyCards.Count == 0) return;
        destination = Mathf.Clamp(destination, 0, partyCards.Count - 1);
        if (card.GetSiblingIndex() == destination) return;
        var previous = new Dictionary<RectTransform, Vector2>();
        foreach (RectTransform item in partyCards)
            if (item != null) previous[item] = item.anchoredPosition;
        card.SetSiblingIndex(destination);
        LayoutRebuilder.ForceRebuildLayoutImmediate(partyListRoot);
        motionStarts.Clear();
        motionTargets.Clear();
        foreach (RectTransform item in partyCards)
        {
            if (item == null) continue;
            motionStarts[item] = previous.ContainsKey(item) ? previous[item] : item.anchoredPosition;
            motionTargets[item] = item.anchoredPosition;
        }
        motionStartTime = Time.unscaledTime;
        motionActive = true;
        UpdatePartyNumbers();
    }

    private void BindPartyPortraitImages()
    {
        if (partyPortraitImages == null || partyPortraitImages.Length != 3)
            System.Array.Resize(ref partyPortraitImages, 3);

        Transform ready = transform;
        while (ready != null && ready.name != "Ready_Panel")
            ready = ready.parent;
        if (ready == null) return;

        Transform select = ready.Find("Party/Select_Char");
        if (select == null) return;
        partyListRoot = select as RectTransform;

        for (int i = 0; i < 3; i++)
        {
            if (partyPortraitImages[i] != null) continue;
            Transform character = select.Find($"SC{i + 1:00}/Mask/Char");
            if (character != null)
                partyPortraitImages[i] = character.GetComponent<Image>();
        }
    }

    private void RefreshPartyPortraits()
    {
        if (partyPortraitImages == null || partyPortraitImages.Length != 3 || partyListRoot == null)
            BindPartyPortraitImages();

        PartyRuntimeStore store = DataManager.Instance != null ? DataManager.Instance.PartyRuntimeStore : null;
        CharacterIconDatabase database = DataManager.Instance != null ? DataManager.Instance.CharacterIconDatabase : null;
        if (store == null) return;

        currentPlaced.Clear();
        for (int i = 0; i < store.MaxPartyCountValue; i++)
        {
            string id = store.GetCharacterId(i);
            if (string.IsNullOrWhiteSpace(id) || store.GetSpawnGridIndex(i) < 0) continue;
            if (!currentPlaced.Contains(id)) currentPlaced.Add(id);
        }

        for (int i = placementOrder.Count - 1; i >= 0; i--)
            if (!currentPlaced.Contains(placementOrder[i])) placementOrder.RemoveAt(i);
        foreach (string id in currentPlaced)
            if (!placementOrder.Contains(id)) placementOrder.Add(id);

        for (int i = 0; i < 3; i++)
        {
            Image target = partyPortraitImages[i];
            if (target == null) continue;
            Sprite portrait = null;
            if (i < placementOrder.Count && database != null)
                database.TryGetHUDSelectedPortraitImage(placementOrder[i], out portrait);
            if (target.sprite != portrait) target.sprite = portrait;
            bool visible = portrait != null;
            if (target.enabled != visible) target.enabled = visible;
            if (target.gameObject.activeSelf != visible) target.gameObject.SetActive(visible);
        }
    }

    private void SetupPartyDrag()
    {
        if (partyListRoot == null) BindPartyPortraitImages();
        if (partyListRoot == null) return;
        partyCards.Clear();
        for (int i = 0; i < 3; i++)
        {
            Transform card = partyListRoot.Find($"SC{i + 1:00}");
            if (card == null) continue;
            RectTransform rect = card as RectTransform;
            if (rect == null) continue;
            partyCards.Add(rect);
            Transform mask = card.Find("Mask");
            if (mask == null) continue;
            ReadyPartyCardDrag drag = mask.GetComponent<ReadyPartyCardDrag>();
            if (drag == null) drag = mask.gameObject.AddComponent<ReadyPartyCardDrag>();
            drag.Initialize(this, rect);
        }
        UpdatePartyNumbers();
    }

    private void UpdatePartyNumbers()
    {
        if (partyListRoot == null) return;
        for (int i = 0; i < partyListRoot.childCount; i++)
        {
            Transform card = partyListRoot.GetChild(i);
            if (!partyCards.Contains(card as RectTransform)) continue;
            Transform number = card.Find("Number");
            Image numberImage = number != null ? number.GetComponent<Image>() : null;
            if (numberImage != null && partyNumberSprites != null && i < partyNumberSprites.Length && partyNumberSprites[i] != null)
                numberImage.sprite = partyNumberSprites[i];
        }
    }

    // 드래그 완료 시 실제 SC 계층 순서와 초상화 표시 순서를 함께 바꿉니다.
    internal void CommitPartyCardOrder(RectTransform movedCard)
    {
        if (partyListRoot == null) return;
        List<RectTransform> ordered = new List<RectTransform>(partyCards);
        ordered.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));
        var oldCards = new List<RectTransform>(partyCards);
        var oldPortraits = (Image[])partyPortraitImages.Clone();
        var oldIds = new List<string>(placementOrder);
        for (int i = 0; i < ordered.Count; i++)
        {
            int oldIndex = oldCards.IndexOf(ordered[i]);
            if (oldIndex < 0) continue;
            if (i < partyPortraitImages.Length && oldIndex < oldPortraits.Length)
                partyPortraitImages[i] = oldPortraits[oldIndex];
        }
        placementOrder.Clear();
        for (int i = 0; i < ordered.Count; i++)
        {
            int oldIndex = oldCards.IndexOf(ordered[i]);
            if (oldIndex >= 0 && oldIndex < oldIds.Count) placementOrder.Add(oldIds[oldIndex]);
        }
        PartyRuntimeStore store = DataManager.Instance != null ? DataManager.Instance.PartyRuntimeStore : null;
        if (store == null || !store.ReorderPlacedCharacters(placementOrder))
        {
            // 실제 파티 순서를 갱신하지 못하면 UI만 변경된 상태로 확정하지 않습니다.
            placementOrder.Clear();
            placementOrder.AddRange(oldIds);
            foreach (RectTransform oldCard in oldCards)
                if (oldCard != null) oldCard.SetAsLastSibling();
            UpdatePartyNumbers();
            RefreshPartyPortraits();
            return;
        }
        partyCards.Clear();
        partyCards.AddRange(ordered);
        UpdatePartyNumbers();
        RefreshPartyPortraits();
    }

    // SC 카드 클릭으로 파티 등록을 해제합니다. 그리드와 HUD 슬롯은 남은 순서대로 압축됩니다.
    internal void UnregisterPartyCard(RectTransform clickedCard)
    {
        if (clickedCard == null || partyListRoot == null) return;
        int index = partyCards.IndexOf(clickedCard);
        if (index < 0 || index >= placementOrder.Count) return;
        string characterId = placementOrder[index];
        if (string.IsNullOrWhiteSpace(characterId)) return;

        SpawnGridPanel gridPanel = GetComponentInParent<SpawnGridPanel>();
        if (gridPanel == null)
            gridPanel = FindFirstObjectByType<SpawnGridPanel>(FindObjectsInactive.Include);
        if (gridPanel == null || !gridPanel.UnregisterReadyPartyCharacter(characterId)) return;

        int occupiedBeforeRemoval = placementOrder.Count;
        placementOrder.RemoveAt(index);
        // 마지막으로 등록된 캐릭터를 해제하면 다른 카드의 순서가 바뀌지 않습니다.
        // 뒤에 등록된 캐릭터가 있는 경우에만 빈 자리를 메우도록 카드를 이동합니다.
        if (index < occupiedBeforeRemoval - 1)
            MovePartyCard(clickedCard, occupiedBeforeRemoval - 1);
        partyCards.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));
        for (int i = 0; i < partyCards.Count && i < partyPortraitImages.Length; i++)
        {
            Transform portrait = partyCards[i].Find("Mask/Char");
            partyPortraitImages[i] = portrait != null ? portrait.GetComponent<Image>() : null;
        }
        UpdatePartyNumbers();
        RefreshPartyPortraits();
    }

    internal RectTransform PartyListRoot => partyListRoot;

    [Header("Editor")]
    [SerializeField] private bool previewInEditMode = true;

#if UNITY_EDITOR
    private bool editorApplyQueued;
    private void OnValidate()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        horizontalSpacing = Mathf.Max(0f, horizontalSpacing);
        verticalSpacing = Mathf.Max(0f, verticalSpacing);
        if (!previewInEditMode || Application.isPlaying || editorApplyQueued) return;
        editorApplyQueued = true;
        EditorApplication.delayCall += DelayedApply;
    }
    private void DelayedApply()
    {
        editorApplyQueued = false;
        if (this == null || Application.isPlaying || !previewInEditMode) return;
        ApplyLayout();
    }
#endif

    private void Awake()
    {
        if (Application.isPlaying) ApplyLayout();
    }

    [ContextMenu("Apply UI Grid Layout")]
    public void ApplyLayout()
    {
        RectTransform root = transform as RectTransform;
        if (root == null)
        {
            Debug.LogWarning("[SpawnGridUIWarpController] Attach to a UI RectTransform grid root.", this);
            return;
        }

        SpawnGridCell[] targets = cells;
        if (targets == null || targets.Length == 0)
            targets = GetComponentsInChildren<SpawnGridCell>(true);

        int count = Mathf.Min(width * height, targets.Length);
        for (int i = 0; i < count; i++)
        {
            SpawnGridCell cell = targets[i];
            if (cell == null) continue;
            RectTransform rect = cell.transform as RectTransform;
            if (rect == null || rect.parent != root) // coordinates must be relative to this grid root
            {
                Debug.LogWarning("[SpawnGridUIWarpController] Cells must be direct RectTransform children of the grid root: " + cell.name, cell);
                continue;
            }

            int column = i / height;
            int rowFromBottom = height - 1 - (i % height);
            Vector2 bl = Point(column, rowFromBottom);
            Vector2 br = Point(column + 1, rowFromBottom);
            Vector2 tl = Point(column, rowFromBottom + 1);
            Vector2 tr = Point(column + 1, rowFromBottom + 1);
            // Pull the four cell corners inward independently along each grid direction.
            // This produces a real gap between the RectTransforms, not just a visual inset.
            // Cap each gap against the shortest corresponding edge to avoid inverted cells.
            float minWidth = Mathf.Min((br - bl).magnitude, (tr - tl).magnitude);
            float minHeight = Mathf.Min((tl - bl).magnitude, (tr - br).magnitude);
            float halfGapX = Mathf.Min(horizontalSpacing, Mathf.Max(0f, minWidth - 1f)) * 0.5f;
            float halfGapY = Mathf.Min(verticalSpacing, Mathf.Max(0f, minHeight - 1f)) * 0.5f;
            Vector2 rightBottom = (br - bl).normalized * halfGapX;
            Vector2 rightTop = (tr - tl).normalized * halfGapX;
            Vector2 upLeft = (tl - bl).normalized * halfGapY;
            Vector2 upRight = (tr - br).normalized * halfGapY;
            bl += rightBottom + upLeft;
            br += -rightBottom + upRight;
            tl += rightTop - upLeft;
            tr += -rightTop - upRight;

            Vector2 center = (bl + br + tl + tr) * 0.25f;

            // Fixed anchors ensure anchoredPosition matches coordinates in the root space.
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.anchoredPosition = center - root.rect.center;
            rect.sizeDelta = new Vector2(
                Mathf.Max(1f, Mathf.Max(Mathf.Abs(bl.x - center.x), Mathf.Abs(br.x - center.x), Mathf.Abs(tl.x - center.x), Mathf.Abs(tr.x - center.x)) * 2f),
                Mathf.Max(1f, Mathf.Max(Mathf.Abs(bl.y - center.y), Mathf.Abs(br.y - center.y), Mathf.Abs(tl.y - center.y), Mathf.Abs(tr.y - center.y)) * 2f));

            if (warpBack) WarpVisual(rect, "Back", bl, br, tl, tr, center);
            if (warpSelect) WarpVisual(rect, "Select", bl, br, tl, tr, center);
            if (warpLine) WarpVisual(rect, "Line", bl, br, tl, tr, center);

            if (centerCharacter)
            {
                Transform charTransform = rect.Find("Char");
                RectTransform character = charTransform as RectTransform;
                if (character != null)
                {
                    character.anchorMin = character.anchorMax = new Vector2(0.5f, 0.5f);
                    character.anchoredPosition = characterOffset;
                }
            }
        }
    }

    private Vector2 Point(int x, int y)
    {
        float u = x / (float)width;
        float v = y / (float)height;
        return Vector2.LerpUnclamped(Vector2.LerpUnclamped(bottomLeft, topLeft, v),
                                      Vector2.LerpUnclamped(bottomRight, topRight, v), u);
    }

    private void WarpVisual(RectTransform cell, string childName, Vector2 bl, Vector2 br, Vector2 tl, Vector2 tr, Vector2 center)
    {
        Transform child = cell.Find(childName);
        if (child == null) return;
        Graphic graphic = child.GetComponent<Graphic>();
        if (graphic == null) return;
        UIGridQuadMeshEffect effect = child.GetComponent<UIGridQuadMeshEffect>();
        if (effect == null) effect = child.gameObject.AddComponent<UIGridQuadMeshEffect>();

        // The inset is measured toward each cell's center, preserving the perspective.
        bl = Vector2.Lerp(bl, center, visualInset);
        br = Vector2.Lerp(br, center, visualInset);
        tl = Vector2.Lerp(tl, center, visualInset);
        tr = Vector2.Lerp(tr, center, visualInset);
        Vector3 TransformCorner(Vector2 p) =>
            child.InverseTransformPoint(transform.TransformPoint(new Vector3(p.x, p.y, 0f)));
        effect.SetQuad(TransformCorner(bl), TransformCorner(br), TransformCorner(tl), TransformCorner(tr));
    }
}

/// <summary>Ready 파티 카드의 Mask에 자동으로 부착되는 드래그 핸들러입니다.</summary>
public sealed class ReadyPartyCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly Color HoverLineColor = new Color32(0xEE, 0xEE, 0xEE, 0xFF);
    private static readonly Color NormalLineColor = new Color32(0xA9, 0xB1, 0xBE, 0xFF);
    private Image hoverLine;
    private SpawnGridUIWarpController controller;
    private RectTransform card;
    private RectTransform root;
    private RectTransform preview;
    private CanvasGroup cardCanvas;
    private bool createdCanvas;
    private float originalAlpha;
    private int originalIndex;
    private readonly List<RectTransform> originalOrder = new List<RectTransform>();
    private Vector3 pointerOffset;
    private bool dragging;
    private bool suppressNextClick;

    public void Initialize(SpawnGridUIWarpController owner, RectTransform target)
    {
        controller = owner;
        card = target;
        Transform line = card != null ? card.Find("Line") : null;
        hoverLine = line != null ? line.GetComponent<Image>() : null;
        SetHoverLine(false);
    }

    public void OnPointerEnter(PointerEventData eventData) => SetHoverLine(true);
    public void OnPointerExit(PointerEventData eventData) => SetHoverLine(false);

    private void SetHoverLine(bool hovering)
    {
        if (hoverLine == null) return;
        Color color = hovering ? HoverLineColor : NormalLineColor;
        color.a = hoverLine.color.a;
        hoverLine.color = color;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (suppressNextClick) { suppressNextClick = false; return; }
        if (eventData.button != PointerEventData.InputButton.Left || dragging || controller == null) return;
        controller.UnregisterPartyCard(card);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (controller == null || card == null || !card.gameObject.activeInHierarchy) return;
        root = controller.PartyListRoot;
        if (root == null || root.parent == null) return;
        originalIndex = card.GetSiblingIndex();
        originalOrder.Clear();
        for (int i = 0; i < root.childCount; i++)
            if (root.GetChild(i) is RectTransform rect) originalOrder.Add(rect);

        // 원본 SC는 레이아웃에 남기고, 복사본만 마우스를 따라갑니다.
        preview = Instantiate(card, root.parent) as RectTransform;
        if (preview == null) return;
        preview.name = "PartyDragPreview";
        preview.SetAsLastSibling();
        preview.position = card.position;
        preview.rotation = card.rotation;
        preview.localScale = card.localScale;
        CanvasGroup proxyCanvas = preview.GetComponent<CanvasGroup>();
        if (proxyCanvas == null) proxyCanvas = preview.gameObject.AddComponent<CanvasGroup>();
        proxyCanvas.blocksRaycasts = false;
        proxyCanvas.interactable = false;
        // 복제된 Mask의 드래그 핸들러가 이벤트를 받지 않도록 합니다.
        foreach (ReadyPartyCardDrag drag in preview.GetComponentsInChildren<ReadyPartyCardDrag>(true))
            drag.enabled = false;

        cardCanvas = card.GetComponent<CanvasGroup>();
        createdCanvas = cardCanvas == null;
        if (createdCanvas) cardCanvas = card.gameObject.AddComponent<CanvasGroup>();
        originalAlpha = cardCanvas.alpha;
        cardCanvas.alpha = 0.18f;
        pointerOffset = card.position - ScreenWorldPosition(eventData);
        dragging = true;
        SetHoverLine(false);
        suppressNextClick = false;
        FollowPointer(eventData);
    }

    private Vector3 ScreenWorldPosition(PointerEventData data)
    {
        Vector2 local;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, data.position, data.pressEventCamera, out local))
            return root.TransformPoint(local);
        return card != null ? card.position : Vector3.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || root == null) return;
        FollowPointer(eventData);
        // 타깃의 중앙을 넘는 순간 순서를 바꿉니다. 마지막 슬롯 아래쪽 절반도 정상 판정됩니다.
        int destination = 0;
        for (int i = 0; i < root.childCount; i++)
        {
            RectTransform item = root.GetChild(i) as RectTransform;
            if (item == null || item == card) continue;
            float centerY = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera,
                item.TransformPoint(item.rect.center)).y;
            if (eventData.position.y < centerY) destination++;
        }
        controller.MovePartyCard(card, destination);
    }

    private void FollowPointer(PointerEventData eventData)
    {
        if (preview != null) preview.position = ScreenWorldPosition(eventData) + pointerOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        // 드롭 위치가 Select_Char 바깥이어도 세로 좌표로 최종 예정 순서를 갱신합니다.
        // 가로 좌표는 순서 판정에 사용하지 않습니다.
        OnDrag(eventData);
        EndDrag(true);
    }

    private void EndDrag(bool commit)
    {
        suppressNextClick = true;
        dragging = false;
        SetHoverLine(false);
        if (preview != null) Destroy(preview.gameObject);
        preview = null;
        if (cardCanvas != null)
        {
            cardCanvas.alpha = originalAlpha;
            if (createdCanvas) Destroy(cardCanvas);
        }
        cardCanvas = null;
        if (controller == null || card == null || root == null) return;
        if (!commit)
        {
            // 오브젝트가 비활성화되는 등 드래그가 중단된 경우에만 복구합니다.
            for (int i = 0; i < originalOrder.Count; i++)
            {
                RectTransform item = originalOrder[i];
                if (item != null && item.parent == root) controller.MovePartyCard(item, i);
            }
        }
        else if (card.GetSiblingIndex() != originalIndex)
        {
            controller.CommitPartyCardOrder(card);
        }
        originalOrder.Clear();
    }

    private void OnDisable()
    {
        if (dragging) EndDrag(false);
        SetHoverLine(false);
    }
}
