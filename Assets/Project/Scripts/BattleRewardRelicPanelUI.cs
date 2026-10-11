using System;
using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

// 전투 및 이벤트에서 획득한 유물을 캐릭터에게 직접 장착하는 패널입니다.
public sealed class BattleRewardRelicPanelUI : MonoBehaviour
{
    [SerializeField] private Image rarityLine;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TMP_Text rewardName;
    [SerializeField] private TMP_Text rewardRarity;
    [SerializeField] private TMP_Text rewardEffect;
    [SerializeField] private Transform content;
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color rareColor = new Color32(81, 157, 228, 255);
    [SerializeField] private Color epicColor = new Color32(165, 107, 221, 255);
    [SerializeField] private Color uniqueColor = new Color32(233, 177, 69, 255);

    [Header("Equipped Relic Tooltip")]
    [SerializeField] private RectTransform tooltipUI;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(170f, 0f);
    [SerializeField] private bool tooltipUseSlotRightEdge = true;
    [SerializeField, Min(0f)] private float tooltipScreenEdgePadding = 12f;
    [Header("Tooltip Fade")]
    [SerializeField, Min(0f)] private float tooltipFadeInDuration = 0.2f;
    [SerializeField, Min(0f)] private float tooltipFadeOutDuration = 0.12f;
    private RectTransform activeTooltipSlot;
    private CanvasGroup tooltipCanvasGroup;
    private Coroutine tooltipFadeCoroutine;
    private Vector2Int lastTooltipScreenSize;

    [Header("Equipped Relic Slot Hover")]
    [SerializeField, Min(1f)] private float relicSlotHoverScale = 1.1f;
    [SerializeField, Min(0f)] private float relicSlotHoverDuration = 0.2f;

    [Header("Equipped Relic Slot Lines")]
    [SerializeField] private Color equippedRelicLineColor = new Color32(0xA9, 0xB1, 0xBE, 0xFF);
    [SerializeField] private Color emptyRelicLineColor = new Color32(0x77, 0x77, 0x77, 0xFF);

    [Serializable]
    private sealed class CharacterReferences
    {
        public Transform root;
        public Image characterIcon;
        public TMP_Text characterName;
        public GameObject select;
        public Image relic01Icon;
        public Image relic02Icon;
        public Image relic03Icon;
        public Image relic04Icon;
        public Image relic05Icon;
        public Image relic06Icon;

        public Image GetRelicIcon(int index)
        {
            switch (index)
            {
                case 0: return relic01Icon;
                case 1: return relic02Icon;
                case 2: return relic03Icon;
                case 3: return relic04Icon;
                case 4: return relic05Icon;
                case 5: return relic06Icon;
                default: return null;
            }
        }
    }

    [Header("content / Char1~3 - inspector references")]
    [SerializeField] private CharacterReferences[] characters = new CharacterReferences[3];

    private readonly CharacterEntry[] entries = new CharacterEntry[3];
    private BattleRewardData current;
    private Action onResolved;
    private bool resolving;

    [Header("Equip Flight Animation")]
    [SerializeField] private RectTransform flyingItem;
    [SerializeField, Min(0f)] private float gatherDuration = 0.18f;
    [SerializeField, Min(0f)] private float riseDuration = 0.13f;
    [SerializeField, Min(0f)] private float dropDuration = 0.19f;
    [Tooltip("Extra world-space distance above the Canvas top edge before the item returns.")]
    [SerializeField, Min(0f)] private float riseHeight = 80f;
    [SerializeField, Range(0.01f, 1f)] private float condensedItemWidth = 0.008f;
    [SerializeField, Range(0.01f, 1f)] private float condensedItemHeight = 0.9f;
    private Vector3 itemOriginalPosition;
    private Vector3 itemOriginalScale;
    private CanvasGroup itemCanvasGroup;
    private float itemOriginalAlpha = 1f;
    private bool itemFlightActive;

    [Header("Equip Movement Line")]
    [SerializeField] private GameObject equipMovement;
    [SerializeField, Min(0f)] private float movementFadeInDuration = 0.14f;
    private CanvasGroup movementCanvasGroup;

    [Header("Equip Close Delay")]
    [SerializeField, Min(0f)] private float equipCloseDelay = 1f;
    [Header("Panel Close Fade")]
    [SerializeField, Min(0f)] private float panelFadeOutDuration = 0.25f;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    private Coroutine equipCloseCoroutine;
    private int lockedEquipCharacterIndex = -1;

    private sealed class CharacterEntry
    {
        public GameObject root;
        public string id;
        public Image icon;
        public TMP_Text name;
        public GameObject select;
        public Button button;
        public Image[] relics = new Image[6];
        public Image[] relicLines = new Image[6];
        public BattleRelicSlotTooltipHover[] tooltipHovers = new BattleRelicSlotTooltipHover[6];
        public TMP_Text[] slotHoverLabels = new TMP_Text[6];
        public int hoveredSlot = -1;
        public bool characterHovered;
    }

    private void Awake()
    {
        Bind();
        EnsurePanelCanvasGroup();
    }

    private void EnsurePanelCanvasGroup()
    {
        if (panelCanvasGroup == null)
            panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private IEnumerator FadeOutPanel()
    {
        EnsurePanelCanvasGroup();
        float initialAlpha = panelCanvasGroup.alpha;
        float duration = Mathf.Max(0f, panelFadeOutDuration);
        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                panelCanvasGroup.alpha = Mathf.Lerp(initialAlpha, 0f, t);
                yield return null;
            }
        }
        panelCanvasGroup.alpha = 0f;
    }

    private void Start()
    {
        if (current == null && gameObject.activeSelf) gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (equipCloseCoroutine != null)
        {
            StopCoroutine(equipCloseCoroutine);
            equipCloseCoroutine = null;
        }
        lockedEquipCharacterIndex = -1;
        RestoreFlyingItem();
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }
    }

    public static bool TryOpenRelicReward(string relicId, Action callback = null)
    {
        if (string.IsNullOrWhiteSpace(relicId) || DataManager.Instance?.RelicDatabase == null) return false;
        if (!DataManager.Instance.RelicDatabase.TryGet(relicId.Trim(), out RelicData relic) || relic == null) return false;
        BattleRewardRelicPanelUI panel = FindFirstObjectByType<BattleRewardRelicPanelUI>(FindObjectsInactive.Include);
        if (panel == null || panel.current != null) return false;
        Sprite icon = null;
        DataManager.Instance.RelicIconDatabase?.TryGetIcon(relicId.Trim(), out icon);
        panel.Open(new BattleRewardData { Type = BattleRewardType.Relic, RewardId = relicId.Trim(), Icon = icon }, callback);
        return true;
    }

    public void Open(BattleRewardData reward, Action callback)
    {
        if (reward == null || reward.Type != BattleRewardType.Relic || current != null) return;
        Bind();
        current = reward;
        onResolved = callback;
        resolving = false;
        lockedEquipCharacterIndex = -1;
        if (equipCloseCoroutine != null)
        {
            StopCoroutine(equipCloseCoroutine);
            equipCloseCoroutine = null;
        }
        RestoreFlyingItem();
        EnsurePanelCanvasGroup();
        panelCanvasGroup.alpha = 1f;
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
        BattleUIBlurRootCollector.ConfigureForPanel(gameObject);
        gameObject.SetActive(true);
        // 활성화 후에도 새 하이어라키의 텍스트를 다시 연결하여 갱신합니다.
        Bind();
        Refresh();
        transform.SetAsLastSibling();
    }

    private static TMP_Text FindText(Transform target)
    {
        if (target == null) return null;
        TMP_Text text = target.GetComponent<TMP_Text>();
        // TMP SubMeshUI is not a primary text label. Do not bind a submesh as Name.
        return text != null ? text : target.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void Bind()
    {
        Transform item = transform.Find("item");
        if (flyingItem == null)
            flyingItem = item as RectTransform;
        if (equipMovement == null)
            equipMovement = item?.Find("Movement")?.gameObject;
        rarityLine = rarityLine != null ? rarityLine : item?.Find("Background/Line2")?.GetComponent<Image>();
        rewardIcon = rewardIcon != null ? rewardIcon : item?.Find("Itemimage/Icon")?.GetComponent<Image>();
        rewardName = rewardName != null ? rewardName : FindText(item?.Find("Name"));
        rewardRarity = rewardRarity != null ? rewardRarity : FindText(item?.Find("Rarity"));
        rewardEffect = rewardEffect != null ? rewardEffect : FindText(item?.Find("Effect"));
        // Support both the old Contant hierarchy and the renamed content hierarchy.
        // 이전 프리팹에서 직렬화된 참조가 남아 있을 수 있어 현재 하이어라키를 우선합니다.
        Transform currentContent = transform.Find("content") ?? transform.Find("Contant") ?? transform.Find("Content");
        if (currentContent != null) content = currentContent;
        if (tooltipUI == null)
            tooltipUI = transform.Find("TooltipUI") as RectTransform;
        if (tooltipUI != null)
        {
            tooltipCanvasGroup = tooltipUI.GetComponent<CanvasGroup>();
            if (tooltipCanvasGroup == null)
                tooltipCanvasGroup = tooltipUI.gameObject.AddComponent<CanvasGroup>();
            tooltipCanvasGroup.interactable = false;
            tooltipCanvasGroup.blocksRaycasts = false;
            if (!tooltipUI.gameObject.activeSelf)
                tooltipCanvasGroup.alpha = 0f;
            foreach (Graphic graphic in tooltipUI.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            tooltipUI.gameObject.SetActive(false);
        }
        if (content == null) return;
        for (int i = 0; i < 3; i++)
        {
            CharacterReferences refs = characters != null && i < characters.Length ? characters[i] : null;
            Transform root = refs?.root != null ? refs.root : content.Find("Char" + (i + 1));
            if (root == null) continue;
            CharacterEntry e = entries[i] ?? new CharacterEntry();
            entries[i] = e;
            e.root = root.gameObject;
            e.icon = refs?.characterIcon != null ? refs.characterIcon : root.Find("Icon/Mask/Image")?.GetComponent<Image>();
            e.name = refs?.characterName != null ? refs.characterName : FindText(root.Find("Name"));
            e.select = refs?.select != null ? refs.select : root.Find("Select")?.gameObject;
            e.button = root.GetComponent<Button>();
            if (e.button == null) e.button = root.gameObject.AddComponent<Button>();
            e.button.transition = Selectable.Transition.None;
            int index = i;
            e.button.onClick.RemoveAllListeners();
            e.button.onClick.AddListener(() => EquipTo(index));
            RelicHoverRelay relay = root.GetComponent<RelicHoverRelay>();
            if (relay == null) relay = root.gameObject.AddComponent<RelicHoverRelay>();
            relay.Configure(e.select);
            relay.SetSelectionLocked(lockedEquipCharacterIndex >= 0, lockedEquipCharacterIndex == index);
            relay.SetHoverCallback(hovering => SetCharacterHover(index, hovering));
            if (e.select != null)
                foreach (Graphic graphic in e.select.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            Transform relicRoot = root.Find("Relic");
            for (int j = 0; j < 6; j++)
            {
                Transform relicSlot = relicRoot?.Find("Relic" + (j + 1).ToString("00"));
                e.relics[j] = refs?.GetRelicIcon(j) != null ? refs.GetRelicIcon(j) : relicSlot?.Find("Icon")?.GetComponent<Image>();
                e.relicLines[j] = relicSlot?.Find("Line")?.GetComponent<Image>();
                if (relicSlot != null)
                {
                    BattleRelicSlotTooltipHover hover = relicSlot.GetComponent<BattleRelicSlotTooltipHover>();
                    if (hover == null) hover = relicSlot.gameObject.AddComponent<BattleRelicSlotTooltipHover>();
                    hover.Configure(this, relicSlot as RectTransform, index, j);
                    Transform hoverRoot = relicSlot.Find("Hover");
                    e.slotHoverLabels[j] = FindText(hoverRoot);
                    if (hoverRoot != null)
                    {
                        hoverRoot.gameObject.SetActive(true);
                        CanvasGroup hoverCanvas = hoverRoot.GetComponent<CanvasGroup>();
                        if (hoverCanvas == null) hoverCanvas = hoverRoot.gameObject.AddComponent<CanvasGroup>();
                        hoverCanvas.interactable = false;
                        hoverCanvas.blocksRaycasts = false;
                        foreach (Graphic graphic in hoverRoot.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                    }
                    e.tooltipHovers[j] = hover;
                    Image hitImage = relicSlot.GetComponent<Image>();
                    if (hitImage == null)
                    {
                        hitImage = relicSlot.gameObject.AddComponent<Image>();
                        hitImage.color = new Color(1f, 1f, 1f, 0f);
                    }
                    hitImage.raycastTarget = true;
                }
            }
        }
    }

    private void Refresh()
    {
        if (current == null || DataManager.Instance == null) return;
        RelicData relic = null;
        DataManager.Instance.RelicDatabase?.TryGet(current.RewardId, out relic);
        Sprite icon = current.Icon;
        if (DataManager.Instance.RelicIconDatabase != null) DataManager.Instance.RelicIconDatabase.TryGetIcon(current.RewardId, out icon);
        SetIcon(rewardIcon, icon);
        if (rewardName != null) rewardName.text = relic != null ? GameDataLocalization.RelicName(relic) : current.RewardId;
        if (rewardRarity != null) rewardRarity.text = relic != null ? GameDataLocalization.RelicRarity(relic) : string.Empty;
        if (rewardEffect != null) rewardEffect.text = relic != null ? GameDataLocalization.RelicEffectDescription(relic) : string.Empty;
        if (rarityLine != null && relic != null)
        {
            Color color = commonColor;
            if (!RecordPanelUI.TryGetCachedRarityDisplayColor(relic.Rarity, out color))
            {
                switch (RelicRarityUtility.GetRevealRank(ParseRarity(relic.Rarity)))
                {
                    case 2: color = rareColor; break;
                    case 3: color = epicColor; break;
                    case 4: color = uniqueColor; break;
                    default: color = commonColor; break;
                }
            }
            rarityLine.color = color;
        }
        for (int i = 0; i < entries.Length; i++)
        {
            CharacterEntry e = entries[i];
            if (e == null) continue;
            e.id = DataManager.Instance.PartyRuntimeStore?.GetCharacterId(i);
            bool present = !string.IsNullOrWhiteSpace(e.id);
            e.root.SetActive(present);
            if (!present) continue;
            if (e.select != null) e.select.SetActive(true);
            CharacterMasterData master = DataManager.Instance.CharacterDatabase?.Get(e.id);
            // TMP SubMesh가 아니라 Name의 원본 TMP에 직접 이름을 기록합니다.
            Transform nameRoot = content.Find("Char" + (i + 1) + "/Name");
            e.name = FindText(nameRoot);
            string displayName = master != null ? GameDataLocalization.CharacterName(master) : null;
            if (string.IsNullOrWhiteSpace(displayName)) displayName = master != null ? master.Name : null;
            if (string.IsNullOrWhiteSpace(displayName)) displayName = e.id;
            if (e.name != null) { e.name.text = displayName; e.name.SetAllDirty(); }
            else Debug.LogWarning("[BattleRewardRelicPanelUI] Char" + (i + 1) + "/Name TMP_Text를 찾지 못했습니다.", e.root);
            Sprite portrait = null;
            DataManager.Instance.CharacterIconDatabase?.TryGetIcon(e.id, out portrait);
            SetIcon(e.icon, portrait);
            DataManager.Instance.CharacterRuntimeStore.TryGet(e.id, out CharacterRuntimeData runtime);
            int visible = 0;
            if (runtime != null)
            {
                ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
                // 0번은 활성 유물 슬롯이므로 일반 유물 표시에서는 제외합니다.
                for (int j = 1; j < runtime.EquippedRelicIds.Length && visible < 6; j++)
                {
                    string id = runtime.EquippedRelicIds[j];
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    Sprite equippedIcon = null;
                    DataManager.Instance.RelicIconDatabase?.TryGetIcon(id, out equippedIcon);
                    SetIcon(e.relics[visible], equippedIcon);
                    SetRelicLine(e, visible, true);
                    e.tooltipHovers[visible]?.SetRelicId(id);
                    visible++;
                }
            }
            while (visible < 6)
            {
                SetIcon(e.relics[visible], null);
                SetRelicLine(e, visible, false);
                e.tooltipHovers[visible]?.SetRelicId(null);
                visible++;
            }
            RefreshSlotHoverLabels(i);
        }
    }

    private void SetCharacterHover(int characterIndex, bool hovering)
    {
        if (lockedEquipCharacterIndex >= 0) return;
        if (characterIndex < 0 || characterIndex >= entries.Length) return;
        CharacterEntry e = entries[characterIndex];
        if (e == null) return;
        e.characterHovered = hovering;
        if (!hovering) e.hoveredSlot = -1;
        RefreshSlotHoverLabels(characterIndex);
    }

    internal void SetRelicSlotHover(int characterIndex, int slotIndex, bool hovering)
    {
        if (lockedEquipCharacterIndex >= 0) return;
        if (characterIndex < 0 || characterIndex >= entries.Length) return;
        CharacterEntry e = entries[characterIndex];
        if (e == null) return;
        e.hoveredSlot = hovering ? slotIndex : -1;
        RefreshSlotHoverLabels(characterIndex);
    }

    private int FindFirstEmptyRelicSlot(CharacterEntry e)
    {
        if (e == null || string.IsNullOrWhiteSpace(e.id) || DataManager.Instance == null) return -1;
        if (!DataManager.Instance.CharacterRuntimeStore.TryGet(e.id, out CharacterRuntimeData runtime) || runtime == null) return -1;
        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        for (int i = 1; i < runtime.EquippedRelicIds.Length && i <= 6; i++)
            if (string.IsNullOrWhiteSpace(runtime.EquippedRelicIds[i])) return i - 1;
        return -1;
    }

    private void RefreshSlotHoverLabels(int characterIndex)
    {
        CharacterEntry e = entries[characterIndex];
        if (e == null) return;
        int firstEmpty = FindFirstEmptyRelicSlot(e);
        int highlighted = -1;
        if (lockedEquipCharacterIndex < 0 && (e.characterHovered || e.hoveredSlot >= 0))
        {
            if (e.hoveredSlot >= 0 && e.hoveredSlot < 6 &&
                e.tooltipHovers[e.hoveredSlot] != null && e.tooltipHovers[e.hoveredSlot].HasRelic)
                highlighted = e.hoveredSlot;
            else
                highlighted = firstEmpty;
        }
        for (int i = 0; i < 6; i++)
        {
            TMP_Text label = e.slotHoverLabels[i];
            if (label == null) continue;
            label.text = i == highlighted ? (i == firstEmpty ? "장착" : "교체") : string.Empty;
            label.SetAllDirty();
            Transform hoverRoot = label.transform.parent;
            if (hoverRoot != null)
            {
                CanvasGroup group = hoverRoot.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = i == highlighted ? 1f : 0f;
            }
        }
    }

    internal void EquipRelicSlot(int characterIndex, int slotIndex)
    {
        EquipTo(characterIndex, slotIndex);
    }

    private void SetRelicLine(CharacterEntry entry, int slotIndex, bool occupied)
    {
        if (entry == null || slotIndex < 0 || slotIndex >= entry.relicLines.Length)
            return;

        Image line = entry.relicLines[slotIndex];
        if (line != null)
            line.color = occupied ? equippedRelicLineColor : emptyRelicLineColor;
    }

    internal void ShowEquippedRelicTooltip(RectTransform slot, string relicId)
    {
        if (tooltipUI == null || slot == null || string.IsNullOrWhiteSpace(relicId) ||
            DataManager.Instance?.RelicDatabase == null ||
            !DataManager.Instance.RelicDatabase.TryGet(relicId, out RelicData relic) || relic == null)
            return;

        string relicName = RecordDisplayNameResolver.RelicName(relic);
        string relicEffect = GameDataLocalization.RelicEffectDescription(relic);
        if (string.IsNullOrWhiteSpace(relicEffect))
            relicEffect = relic.EffectDesc ?? string.Empty;

        SetTooltipText("Name", relicName);
        SetTooltipText("Rarity", GameDataLocalization.RelicRarity(relic));
        SetTooltipText("Detail", relicEffect);
        Transform rarityLineRoot = FindTooltipChild(tooltipUI, "Line");
        Image tooltipRarityLine = rarityLineRoot != null ? rarityLineRoot.GetComponent<Image>() : null;
        if (tooltipRarityLine != null)
        {
            Color color = commonColor;
            if (!RecordPanelUI.TryGetCachedRarityDisplayColor(relic.Rarity, out color))
            {
                switch (RelicRarityUtility.GetRevealRank(ParseRarity(relic.Rarity)))
                {
                    case 2: color = rareColor; break;
                    case 3: color = epicColor; break;
                    case 4: color = uniqueColor; break;
                    default: color = commonColor; break;
                }
            }
            tooltipRarityLine.color = color;
        }
        SetTooltipText("Effect", relicEffect);
        Sprite sprite = null;
        DataManager.Instance.RelicIconDatabase?.TryGetIcon(relicId, out sprite);
        Transform iconRoot = FindTooltipChild(tooltipUI, "Icon");
        if (iconRoot != null)
        {
            Image icon = iconRoot.GetComponent<Image>();
            if (icon != null && sprite != null) icon.sprite = sprite;
        }

        if (tooltipCanvasGroup == null)
            tooltipCanvasGroup = tooltipUI.GetComponent<CanvasGroup>();
        if (tooltipCanvasGroup == null)
            tooltipCanvasGroup = tooltipUI.gameObject.AddComponent<CanvasGroup>();

        if (tooltipFadeCoroutine != null)
        {
            StopCoroutine(tooltipFadeCoroutine);
            tooltipFadeCoroutine = null;
        }

        // Place the tooltip while invisible; do not animate its X/Y position.
        bool wasVisible = tooltipUI.gameObject.activeSelf;
        activeTooltipSlot = slot;
        tooltipCanvasGroup.alpha = wasVisible ? tooltipCanvasGroup.alpha : 0f;
        tooltipUI.gameObject.SetActive(true);
        tooltipUI.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        UpdateTooltipPosition();
        lastTooltipScreenSize = new Vector2Int(Screen.width, Screen.height);
        BeginTooltipFade(1f, tooltipFadeInDuration);
    }

    private void LateUpdate()
    {
        // Recalculate only on resolution changes. Following the animated slot
        // scale every frame makes the tooltip visibly slide on hover.
        if (tooltipUI != null && tooltipUI.gameObject.activeInHierarchy && activeTooltipSlot != null &&
            (Screen.width != lastTooltipScreenSize.x || Screen.height != lastTooltipScreenSize.y))
        {
            Canvas.ForceUpdateCanvases();
            UpdateTooltipPosition();
            lastTooltipScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }

    private void UpdateTooltipPosition()
    {
        if (tooltipUI == null || activeTooltipSlot == null)
            return;

        RectTransform parentRect = tooltipUI.parent as RectTransform;
        Canvas canvas = tooltipUI.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : parentRect;
        if (parentRect == null || canvasRect == null)
            return;

        Vector3[] corners = new Vector3[4];
        activeTooltipSlot.GetWorldCorners(corners);
        Vector3 worldAnchor = tooltipUseSlotRightEdge
            ? (corners[2] + corners[3]) * 0.5f
            : (corners[0] + corners[1]) * 0.5f;
        Vector2 localAnchor = parentRect.InverseTransformPoint(worldAnchor);
        Vector2 anchorReference = new Vector2(
            Mathf.Lerp(parentRect.rect.xMin, parentRect.rect.xMax, tooltipUI.anchorMin.x),
            Mathf.Lerp(parentRect.rect.yMin, parentRect.rect.yMax, tooltipUI.anchorMin.y));
        tooltipUI.anchoredPosition = localAnchor - anchorReference + tooltipOffset;

        // Calculate actual visual bounds, including pivot, scale and descendants.
        // Clamp in root Canvas units so CanvasScaler / resolution changes are safe.
        tooltipUI.GetWorldCorners(corners);
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 point = canvasRect.InverseTransformPoint(corners[i]);
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
        }

        Rect bounds = canvasRect.rect;
        float padding = Mathf.Max(0f, tooltipScreenEdgePadding);
        float left = bounds.xMin + padding, right = bounds.xMax - padding;
        float bottom = bounds.yMin + padding, top = bounds.yMax - padding;
        float deltaX = minX < left ? left - minX : maxX > right ? right - maxX : 0f;
        float deltaY = minY < bottom ? bottom - minY : maxY > top ? top - maxY : 0f;
        // If the tooltip is larger than the available area, anchor its top-left.
        if (maxX - minX > right - left) deltaX = left - minX;
        if (maxY - minY > top - bottom) deltaY = top - maxY;

        Vector3 worldDelta = canvasRect.TransformVector(new Vector3(deltaX, deltaY, 0f));
        Vector3 parentDelta = parentRect.InverseTransformVector(worldDelta);
        tooltipUI.anchoredPosition += new Vector2(parentDelta.x, parentDelta.y);
    }

    internal float RelicSlotHoverScale => relicSlotHoverScale;
    internal float RelicSlotHoverDuration => relicSlotHoverDuration;

    internal void HideEquippedRelicTooltip()
    {
        activeTooltipSlot = null;
        if (tooltipUI == null || !tooltipUI.gameObject.activeSelf)
            return;
        BeginTooltipFade(0f, tooltipFadeOutDuration);
    }

    private void BeginTooltipFade(float targetAlpha, float duration)
    {
        if (tooltipCanvasGroup == null || tooltipUI == null)
            return;

        if (tooltipFadeCoroutine != null)
        {
            StopCoroutine(tooltipFadeCoroutine);
            tooltipFadeCoroutine = null;
        }

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy || !tooltipUI.gameObject.activeInHierarchy || duration <= 0f)
        {
            tooltipCanvasGroup.alpha = targetAlpha;
            if (targetAlpha <= 0f)
                tooltipUI.gameObject.SetActive(false);
            return;
        }

        tooltipFadeCoroutine = StartCoroutine(AnimateTooltipAlpha(targetAlpha, duration));
    }

    private IEnumerator AnimateTooltipAlpha(float targetAlpha, float duration)
    {
        float startingAlpha = tooltipCanvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            tooltipCanvasGroup.alpha = Mathf.Lerp(startingAlpha, targetAlpha, t);
            yield return null;
        }

        tooltipCanvasGroup.alpha = targetAlpha;
        if (targetAlpha <= 0f && tooltipUI != null)
            tooltipUI.gameObject.SetActive(false);
        tooltipFadeCoroutine = null;
    }

    private void SetTooltipText(string childName, string value)
    {
        Transform child = FindTooltipChild(tooltipUI, childName);
        TMP_Text label = child != null ? FindText(child) : null;
        if (label == null) return;

        // Tooltip values are already localized from the relic database.
        // Prevent static localization writers from restoring prefab text.
        if (label.GetComponent<LocalizationAutoBindingIgnore>() == null)
            label.gameObject.AddComponent<LocalizationAutoBindingIgnore>();
        LocalizedTMPText staticLocalizer = label.GetComponent<LocalizedTMPText>();
        if (staticLocalizer != null) staticLocalizer.enabled = false;
        DynamicLocalizedTMPText dynamicLocalizer = label.GetComponent<DynamicLocalizedTMPText>();
        if (dynamicLocalizer != null) dynamicLocalizer.enabled = false;
        LocalizeStringEvent stringEvent = label.GetComponent<LocalizeStringEvent>();
        if (stringEvent != null) stringEvent.enabled = false;

        label.text = value ?? string.Empty;
        label.SetAllDirty();
    }

    private static Transform FindTooltipChild(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase)) return child;
            Transform nested = FindTooltipChild(child, name);
            if (nested != null) return nested;
        }
        return null;
    }

    private void EquipTo(int index, int selectedSlot = -1)
    {
        if (resolving || current == null || index < 0 || index >= entries.Length) return;
        CharacterEntry e = entries[index];
        if (e == null || string.IsNullOrWhiteSpace(e.id) || DataManager.Instance == null) return;
        if (!DataManager.Instance.CharacterRuntimeStore.TryGet(e.id, out CharacterRuntimeData runtime) || runtime == null) return;
        if (!DataManager.Instance.RelicDatabase.TryGet(current.RewardId, out RelicData relic) || relic == null) return;
        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        bool active = ActiveRelicEffectResolver.IsActiveRelic(relic);
        int start = active ? 0 : 1;
        int end = active ? 1 : Math.Min(7, runtime.EquippedRelicIds.Length);
        int empty = -1;
        for (int i = start; i < end; i++)
            if (string.IsNullOrWhiteSpace(runtime.EquippedRelicIds[i])) { empty = i; break; }
        int destination = empty;
        if (!active && selectedSlot >= 0 && selectedSlot < 6 && selectedSlot + 1 < end)
        {
            int requested = selectedSlot + 1;
            // Empty slots always fill from the front. Occupied slots can be replaced directly.
            if (!string.IsNullOrWhiteSpace(runtime.EquippedRelicIds[requested]))
                destination = requested;
        }
        if (destination < 0) return;
        resolving = true;
        LockEquippingCharacter(index);
        // Keep the destination empty visually until the item has landed.
        // Also defer the actual replacement until then.
        equipCloseCoroutine = StartCoroutine(PlayEquipFlight(runtime, relic, destination, index, active));
    }

    private void LockEquippingCharacter(int selectedIndex)
    {
        lockedEquipCharacterIndex = selectedIndex;
        for (int i = 0; i < entries.Length; i++)
        {
            CharacterEntry entry = entries[i];
            if (entry == null) continue;
            entry.characterHovered = false;
            entry.hoveredSlot = -1;
            RelicHoverRelay relay = entry.root != null ? entry.root.GetComponent<RelicHoverRelay>() : null;
            if (relay != null) relay.SetSelectionLocked(true, i == selectedIndex);
            RefreshSlotHoverLabels(i);
        }
    }

    private IEnumerator PlayEquipFlight(CharacterRuntimeData runtime, RelicData relic,
        int destination, int characterIndex, bool active)
    {
        HideEquippedRelicTooltip();
        RectTransform target = null;
        CharacterEntry entry = entries[characterIndex];
        if (entry != null)
        {
            int visualSlot = active ? 0 : destination - 1;
            if (visualSlot >= 0 && visualSlot < entry.relics.Length && entry.relics[visualSlot] != null)
                target = entry.relics[visualSlot].transform.parent as RectTransform;
        }

        if (flyingItem != null && target != null)
        {
            itemFlightActive = true;
            itemOriginalPosition = flyingItem.position;
            itemOriginalScale = flyingItem.localScale;
            itemCanvasGroup = flyingItem.GetComponent<CanvasGroup>();
            if (itemCanvasGroup == null)
                itemCanvasGroup = flyingItem.gameObject.AddComponent<CanvasGroup>();
            itemOriginalAlpha = itemCanvasGroup.alpha;
            itemCanvasGroup.blocksRaycasts = false;
            PrepareMovementLine();

            // Use world positions of UI rects so nested canvases and canvas scaling work.
            // Collapse around the item's own position, not the screen center.
            Vector3 start = itemOriginalPosition;
            Vector3 landing = target.TransformPoint(target.rect.center);
            Vector3 thin = Vector3.Scale(itemOriginalScale,
                new Vector3(condensedItemWidth, condensedItemHeight, 1f));

            // Keep the full thin item outside the visible root Canvas before
            // changing its X coordinate. It then drops from offscreen at the
            // destination slot's X position (no visible sideways travel).
            float offscreenY = GetFlyingItemOffscreenTop(thin, start.y, landing.y);
            Vector3 riseTop = new Vector3(start.x, offscreenY, start.z);
            Vector3 targetAbove = new Vector3(landing.x, offscreenY, landing.z);

            // Stay in place while narrowing into a very thin vertical streak.
            yield return AnimateFlyingItem(start, start, itemOriginalScale, thin, gatherDuration, false, true);
            // Rush upward with a fast start, then travel horizontally only offscreen.
            yield return AnimateFlyingItem(start, riseTop, thin, thin, riseDuration, false);
            flyingItem.position = targetAbove;
            // Drop hard into the destination, accelerating toward the slot.
            yield return AnimateFlyingItem(targetAbove, landing, thin, thin, dropDuration, true);
            HideMovementLine();
            itemCanvasGroup.alpha = 0f;
        }

        // Replace only after the streak reaches the slot.
        runtime.EquippedRelicIds[destination] = current.RewardId.Trim();
        if (active) ActiveRelicRuntimeUtility.ResetUses(runtime, relic);
        DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(runtime);
        Refresh();
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, equipCloseDelay));
        EnsurePanelCanvasGroup();
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
        yield return FadeOutPanel();
        equipCloseCoroutine = null;
        RestoreFlyingItem();
        Action callback = onResolved;
        onResolved = null;
        current = null;
        gameObject.SetActive(false);
        callback?.Invoke();
    }

    private float GetFlyingItemOffscreenTop(Vector3 thinScale, float centerY, float landingY)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null && canvas.rootCanvas != null
            ? canvas.rootCanvas.transform as RectTransform
            : null;

        float canvasTopY = Mathf.Max(centerY, landingY);
        if (canvasRect != null)
        {
            Vector3[] corners = new Vector3[4];
            canvasRect.GetWorldCorners(corners);
            canvasTopY = Mathf.Max(corners[1].y, corners[2].y);
        }

        // UI scale is in local space; account for parent Canvas scaling as well.
        float currentScaleY = Mathf.Abs(flyingItem.localScale.y);
        float scaleRatio = currentScaleY > 0.0001f
            ? Mathf.Abs(thinScale.y / currentScaleY)
            : 1f;
        float halfItemHeight = flyingItem.rect.height *
                               Mathf.Abs(flyingItem.lossyScale.y) * scaleRatio * 0.5f;

        return canvasTopY + halfItemHeight + riseHeight;
    }

    private IEnumerator AnimateFlyingItem(Vector3 from, Vector3 to,
        Vector3 fromScale, Vector3 toScale, float duration, bool accelerate = false,
        bool revealMovement = false)
    {
        if (flyingItem == null) yield break;
        float elapsed = 0f;
        duration = Mathf.Max(0.001f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = accelerate ? t * t * t : 1f - Mathf.Pow(1f - t, 3f);
            flyingItem.position = Vector3.LerpUnclamped(from, to, t);
            flyingItem.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
            if (revealMovement && movementCanvasGroup != null)
                movementCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f,
                    movementFadeInDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / movementFadeInDuration));
            yield return null;
        }
        flyingItem.position = to;
        flyingItem.localScale = toScale;
        if (revealMovement && movementCanvasGroup != null)
            movementCanvasGroup.alpha = 1f;
    }

    private void PrepareMovementLine()
    {
        if (equipMovement == null && flyingItem != null)
            equipMovement = flyingItem.Find("Movement")?.gameObject;
        if (equipMovement == null)
            return;

        movementCanvasGroup = equipMovement.GetComponent<CanvasGroup>();
        if (movementCanvasGroup == null)
            movementCanvasGroup = equipMovement.AddComponent<CanvasGroup>();
        movementCanvasGroup.alpha = 0f;
        movementCanvasGroup.interactable = false;
        movementCanvasGroup.blocksRaycasts = false;
        equipMovement.SetActive(true);
    }

    private void HideMovementLine()
    {
        if (movementCanvasGroup != null)
            movementCanvasGroup.alpha = 0f;
        if (equipMovement != null)
            equipMovement.SetActive(false);
    }

    private void RestoreFlyingItem()
    {
        HideMovementLine();
        if (!itemFlightActive) return;
        itemFlightActive = false;
        if (flyingItem != null)
        {
            flyingItem.position = itemOriginalPosition;
            flyingItem.localScale = itemOriginalScale;
            if (itemCanvasGroup != null)
            {
                itemCanvasGroup.alpha = itemOriginalAlpha;
                itemCanvasGroup.blocksRaycasts = false;
            }
        }
    }

    private static RelicRarity ParseRarity(string raw)
    {
        return RelicRarityUtility.TryParseChestRarity(raw, out RelicRarity value) ? value : RelicRarity.Common;
    }

    private static void SetIcon(Image target, Sprite sprite)
    {
        if (target == null) return;
        target.sprite = sprite;
        target.enabled = sprite != null;
    }
}

// 호버 효과는 장착 입력과 분리합니다.
public sealed class RelicHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Select Slide / Fade")]
    [SerializeField, Min(0f)] private float slideDistance = 24f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.2f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.12f;
    [SerializeField, Range(0f, 1f)] private float idleAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float hoverAlpha = 1f;

    private GameObject selection;
    private RectTransform selectionRect;
    private CanvasGroup selectionCanvasGroup;
    private Vector2 originalPosition;
    private Coroutine selectionAnimationCoroutine;
    private bool hovered;
    private Action<bool> hoverCallback;
    private bool selectionLocked;

    public void SetHoverCallback(Action<bool> callback) => hoverCallback = callback;

    public void Configure(GameObject target)
    {
        if (selection != target)
        {
            StopAnimation();
            selection = target;
            selectionRect = selection != null ? selection.GetComponent<RectTransform>() : null;
            selectionCanvasGroup = selection != null ? selection.GetComponent<CanvasGroup>() : null;
            if (selection != null && selectionCanvasGroup == null)
                selectionCanvasGroup = selection.AddComponent<CanvasGroup>();
            if (selectionRect != null)
                originalPosition = selectionRect.anchoredPosition;
        }

        if (selection == null)
            return;

        selection.SetActive(true);
        selectionCanvasGroup.interactable = false;
        selectionCanvasGroup.blocksRaycasts = false;
        hovered = false;
        StopAnimation();
        Apply(0f);
    }

    public void SetSelectionLocked(bool locked, bool selected)
    {
        selectionLocked = locked;
        StopAnimation();
        if (locked) Apply(selected ? 1f : 0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectionLocked) return;
        hovered = true;
        hoverCallback?.Invoke(true);
        Animate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selectionLocked) return;
        hovered = false;
        hoverCallback?.Invoke(false);
        Animate(false);
    }

    private void Animate(bool show)
    {
        if (selection == null)
            return;

        StopAnimation();
        if (!isActiveAndEnabled)
        {
            Apply(show ? 1f : 0f);
            return;
        }

        selectionAnimationCoroutine = StartCoroutine(AnimateRoutine(show));
    }

    private IEnumerator AnimateRoutine(bool show)
    {
        float duration = show ? fadeInDuration : fadeOutDuration;
        float from = CurrentProgress();
        float to = show ? 1f : 0f;
        if (duration <= 0f)
        {
            Apply(to);
            selectionAnimationCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            Apply(Mathf.Lerp(from, to, t));
            yield return null;
        }

        Apply(to);
        selectionAnimationCoroutine = null;
    }

    private float CurrentProgress()
    {
        if (selectionCanvasGroup == null)
            return 0f;
        float difference = hoverAlpha - idleAlpha;
        return Mathf.Abs(difference) < 0.0001f
            ? (hovered ? 1f : 0f)
            : Mathf.Clamp01((selectionCanvasGroup.alpha - idleAlpha) / difference);
    }

    private void Apply(float progress)
    {
        if (selection == null)
            return;

        if (!selection.activeSelf)
            selection.SetActive(true);

        if (selectionCanvasGroup != null)
            selectionCanvasGroup.alpha = Mathf.Lerp(idleAlpha, hoverAlpha, progress);
        if (selectionRect != null)
            selectionRect.anchoredPosition = originalPosition + Vector2.left * (slideDistance * (1f - progress));
    }

    private void StopAnimation()
    {
        if (selectionAnimationCoroutine == null)
            return;
        StopCoroutine(selectionAnimationCoroutine);
        selectionAnimationCoroutine = null;
    }

    private void OnDisable()
    {
        StopAnimation();
        hovered = false;
        Apply(0f);
    }
}

// Only occupied relic slots display their details on hover.
public sealed class BattleRelicSlotTooltipHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private BattleRewardRelicPanelUI panel;
    private RectTransform slot;
    private string relicId;
    private bool isHovered;
    private int characterIndex;
    private int slotIndex;
    public bool HasRelic => !string.IsNullOrWhiteSpace(relicId);
    private Coroutine scaleCoroutine;
    private Vector3 originalScale = Vector3.one;
    private bool hasOriginalScale;

    public void Configure(BattleRewardRelicPanelUI tooltipPanel, RectTransform targetSlot, int character, int index)
    {
        panel = tooltipPanel;
        characterIndex = character;
        slotIndex = index;
        if (slot != targetSlot || !hasOriginalScale)
        {
            slot = targetSlot;
            if (slot != null)
            {
                originalScale = slot.localScale;
                hasOriginalScale = true;
            }
        }
    }

    public void SetRelicId(string value)
    {
        if (string.Equals(relicId, value, StringComparison.Ordinal)) return;
        relicId = value;
        if (!isHovered) return;
        if (string.IsNullOrWhiteSpace(relicId))
        {
            panel?.HideEquippedRelicTooltip();
            AnimateScale(false);
        }
        else
        {
            panel?.ShowEquippedRelicTooltip(slot, relicId);
            AnimateScale(true);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        panel?.SetRelicSlotHover(characterIndex, slotIndex, true);
        if (string.IsNullOrWhiteSpace(relicId)) return;
        panel?.ShowEquippedRelicTooltip(slot, relicId);
        AnimateScale(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        panel?.SetRelicSlotHover(characterIndex, slotIndex, false);
        panel?.HideEquippedRelicTooltip();
        AnimateScale(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            panel?.EquipRelicSlot(characterIndex, slotIndex);
    }

    private void AnimateScale(bool enlarged)
    {
        if (slot == null || !hasOriginalScale) return;
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        float multiplier = enlarged && !string.IsNullOrWhiteSpace(relicId)
            ? (panel != null ? panel.RelicSlotHoverScale : 1.1f)
            : 1f;
        Vector3 destination = originalScale * multiplier;
        float duration = panel != null ? panel.RelicSlotHoverDuration : 0.2f;
        if (!isActiveAndEnabled || duration <= 0f)
        {
            slot.localScale = destination;
            return;
        }
        scaleCoroutine = StartCoroutine(AnimateScaleRoutine(destination, duration));
    }

    private IEnumerator AnimateScaleRoutine(Vector3 destination, float duration)
    {
        Vector3 start = slot.localScale;
        float elapsed = 0f;
        while (elapsed < duration && slot != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            slot.localScale = Vector3.LerpUnclamped(start, destination, t);
            yield return null;
        }
        if (slot != null) slot.localScale = destination;
        scaleCoroutine = null;
    }

    private void OnDisable()
    {
        isHovered = false;
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        if (slot != null && hasOriginalScale) slot.localScale = originalScale;
        panel?.HideEquippedRelicTooltip();
    }
}
