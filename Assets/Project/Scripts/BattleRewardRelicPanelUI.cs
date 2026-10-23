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

    [Header("Relic Equip VFX")]
    [Tooltip("VFX appears at the center of the relic slot in content/Char1~3/Relic after equip or replacement.")]
    [SerializeField] private GameObject equippedSlotVfxPrefab;
    [SerializeField] private Vector3 equippedSlotVfxOffset = Vector3.zero;
    [SerializeField] private Vector3 equippedSlotVfxScale = Vector3.one;
    [SerializeField, Min(0f)] private float equipVfxDelay = 0f;
    [SerializeField, Min(0f)] private float slotVfxLifetime = 2f;
    private readonly System.Collections.Generic.List<GameObject> spawnedEquipVfx = new System.Collections.Generic.List<GameObject>();

    [Header("Relic Slot Hover Preview VFX")]
    [Tooltip("Separate VFX shown on the content slot displaying Equip or Replace on hover.")]
    [SerializeField] private GameObject slotPreviewVfxPrefab;
    [SerializeField] private Vector3 slotPreviewVfxOffset = Vector3.zero;
    [SerializeField] private Vector3 slotPreviewVfxScale = Vector3.one;
    private GameObject activeSlotPreviewVfx;
    private int previewCharacterIndex = -1;
    private int previewSlotIndex = -1;



    [Header("Item Intro Animation")]
    [Tooltip("Time item stays at the center of the screen before moving to its original position.")]
    [SerializeField, Min(0f)] private float itemCenterHoldDuration = 0.5f;
    [Tooltip("Time item takes to move from the screen center to its original position.")]
    [SerializeField, Min(0f)] private float itemReturnMoveDuration = 0.5f;
    [Header("Item Intro Fade In")]
    [Tooltip("Duration for the complete item (including its intro VFX) to fade in at screen center.")]
    [SerializeField, Min(0f)] private float itemIntroFadeInDuration = 0.35f;
    private CanvasGroup introItemCanvasGroup;
    private float introItemOriginalAlpha = 1f;
    private bool introItemOriginalInteractable = true;
    private bool introItemOriginalBlocksRaycasts = true;
    [Header("Item Image Intro VFX")]
    [Tooltip("VFX spawned when item/Itemimage/Icon appears at screen center.")]
    [SerializeField] private GameObject itemImageIntroVfxPrefab;
    [SerializeField] private Vector3 itemImageIntroVfxOffset = Vector3.zero;
    [SerializeField] private Vector3 itemImageIntroVfxScale = Vector3.one;
    [SerializeField, Min(0f)] private float itemImageIntroVfxLifetime = 2f;
    [Tooltip("Apply the relic rarity color to all ParticleSystems in the intro VFX prefab.")]
    [SerializeField] private bool tintItemIntroParticlesByRarity = true;
    [Header("Other Panels Fade In")]
    [Tooltip("Fade duration for other panels after the item reaches its original position.")]
    [SerializeField, Min(0f)] private float otherPanelsFadeInDuration = 0.35f;
    private readonly System.Collections.Generic.List<CanvasGroup> introOtherCanvasGroups =
        new System.Collections.Generic.List<CanvasGroup>();
    private readonly System.Collections.Generic.List<float> introOtherOriginalAlphas =
        new System.Collections.Generic.List<float>();
    private readonly System.Collections.Generic.List<bool> introOtherOriginalInteractables = new System.Collections.Generic.List<bool>();
    private readonly System.Collections.Generic.List<bool> introOtherOriginalRaycasts = new System.Collections.Generic.List<bool>();
    private GameObject activeItemImageIntroVfx;
    private Coroutine itemIntroCoroutine;
    private RectTransform introItemRect;
    private Vector3 introItemOriginalWorldPosition;
    private Vector2 introItemOriginalAnchoredPosition;
    private readonly System.Collections.Generic.List<GameObject> introHiddenObjects =
        new System.Collections.Generic.List<GameObject>();

    [Header("Equip Close Delay")]
    [SerializeField, Min(0f)] private float equipCloseDelay = 1f;
    [Header("Panel Close Fade")]
    [SerializeField, Min(0f)] private float panelFadeOutDuration = 0.25f;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [Header("Equipped Relic Icon Pop")]
    [SerializeField, Min(1f)] private float equippedIconStartScale = 1.25f;
    [SerializeField, Min(0f)] private float equippedIconScaleDuration = 0.2f;
    private Coroutine equippedIconScaleCoroutine;
    private Image animatedEquippedIcon;
    private Vector3 animatedIconOriginalScale;
    private Coroutine equipCloseCoroutine;
    private int lockedEquipCharacterIndex = -1;

    [Header("Reward Action Buttons")]
    [SerializeField] private Button extractButton;
    [SerializeField] private Button skipButton;
    [Header("Relic Extraction - Red Dustium")]
    [SerializeField, Min(0)] private int commonExtraction = 10;
    [SerializeField, Min(0)] private int rareExtraction = 20;
    [SerializeField, Min(0)] private int epicExtraction = 30;
    [SerializeField, Min(0)] private int uniqueExtraction = 50;
    private Coroutine rewardActionCloseCoroutine;

    private void BindRewardActionButtons()
    {
        if (extractButton == null) extractButton = transform.Find("ExtractButton")?.GetComponent<Button>();
        if (skipButton == null) skipButton = transform.Find("SkipButton")?.GetComponent<Button>();
        if (extractButton != null)
        {
            extractButton.onClick.RemoveListener(ExtractReward);
            extractButton.onClick.AddListener(ExtractReward);
        }
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(SkipReward);
            skipButton.onClick.AddListener(SkipReward);
        }
    }

    public void SkipReward()
    {
        if (current == null || resolving || itemIntroCoroutine != null) return;
        resolving = true;
        rewardActionCloseCoroutine = StartCoroutine(CloseWithoutEquip());
    }

    public void ExtractReward()
    {
        if (current == null || resolving || itemIntroCoroutine != null) return;
        DataManager dataManager = DataManager.Instance;
        if (dataManager == null ||
            !BattleRelicExtractionService.TryExtract(
                dataManager.BattleRuntimeStore,
                dataManager.RelicDatabase,
                current.RewardId,
                commonExtraction,
                rareExtraction,
                epicExtraction,
                uniqueExtraction,
                out int amount))
        {
            Debug.LogError($"[BattleRewardRelicPanelUI] Relic extraction failed. RelicId:{current.RewardId}", this);
            return;
        }

        resolving = true;
        BattleGoldHudUI.RefreshAll();
        Debug.Log($"[BattleRewardRelicPanelUI] Extracted relic. RelicId:{current.RewardId} Remnant:{amount}", this);
        rewardActionCloseCoroutine = StartCoroutine(CloseWithoutEquip());
    }

    private IEnumerator CloseWithoutEquip()
    {
        HideEquippedRelicTooltip();
        ClearSlotPreviewVfx();
        EnsurePanelCanvasGroup();
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
        yield return FadeOutPanel();
        rewardActionCloseCoroutine = null;
        Action callback = onResolved;
        onResolved = null;
        current = null;
        gameObject.SetActive(false);
        callback?.Invoke();
    }


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
        public int hoveredSlot = -1;
        public bool characterHovered;
    }

    private void Awake()
    {
        Bind();
        BindRewardActionButtons();
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
        ResetItemIntro();
        if (rewardActionCloseCoroutine != null)
        {
            StopCoroutine(rewardActionCloseCoroutine);
            rewardActionCloseCoroutine = null;
        }
        if (equipCloseCoroutine != null)
        {
            StopCoroutine(equipCloseCoroutine);
            equipCloseCoroutine = null;
        }
        lockedEquipCharacterIndex = -1;
        ClearEquipVfx();
        ClearSlotPreviewVfx();
        RestoreEquippedIconScale();
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
        ResetItemIntro();
        ClearEquipVfx();
        ClearSlotPreviewVfx();
        EnsurePanelCanvasGroup();
        panelCanvasGroup.alpha = 1f;
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
        BattleUIBlurRootCollector.ConfigureForPanel(gameObject);
        gameObject.SetActive(true);
        // 활성화 후에도 새 하이어라키의 텍스트를 다시 연결하여 갱신합니다.
        Bind();
        BindRewardActionButtons();
        Refresh();
        transform.SetAsLastSibling();
        BeginItemIntro();
    }

    // Preserve the original active state of the other direct children.
    // Their active states are restored after the item reaches its original position.
    private void BeginItemIntro()
    {
        introItemRect = transform.Find("item") as RectTransform;
        if (introItemRect == null)
        {
            Debug.LogWarning("[BattleRewardRelicPanelUI] item RectTransform not found; intro skipped.", this);
            return;
        }

        Canvas.ForceUpdateCanvases();
        introItemOriginalWorldPosition = introItemRect.position;
        introItemOriginalAnchoredPosition = introItemRect.anchoredPosition;

        RectTransform parentRect = introItemRect.parent as RectTransform;
        Canvas canvas = GetComponentInParent<Canvas>();
        Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;
        Camera camera = rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : rootCanvas.worldCamera;
        Vector3 centerWorld;
        if (parentRect == null || !RectTransformUtility.ScreenPointToWorldPointInRectangle(
                parentRect, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), camera, out centerWorld))
        {
            centerWorld = introItemOriginalWorldPosition;
        }

        introHiddenObjects.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child == introItemRect.gameObject || !child.activeSelf) continue;
            introHiddenObjects.Add(child);
            child.SetActive(false);
        }

        // Display the complete item hierarchy from the start of the intro.
        // Keep every other panel hidden until the item finishes returning.
        introItemRect.gameObject.SetActive(true);
        introItemCanvasGroup = introItemRect.GetComponent<CanvasGroup>();
        if (introItemCanvasGroup == null)
            introItemCanvasGroup = introItemRect.gameObject.AddComponent<CanvasGroup>();
        introItemOriginalAlpha = introItemCanvasGroup.alpha;
        introItemOriginalInteractable = introItemCanvasGroup.interactable;
        introItemOriginalBlocksRaycasts = introItemCanvasGroup.blocksRaycasts;
        introItemCanvasGroup.alpha = 0f;
        introItemCanvasGroup.interactable = false;
        introItemCanvasGroup.blocksRaycasts = false;
        introItemRect.position = centerWorld;
        SpawnItemImageIntroVfx();
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
        itemIntroCoroutine = StartCoroutine(AnimateItemIntro(centerWorld));
    }

    private IEnumerator AnimateItemIntro(Vector3 centerWorld)
    {
        // Fade in the whole item while it rests at screen center. If the configured
        // hold is shorter than the fade, finish the fade before moving.
        float fadeDuration = Mathf.Max(0f, itemIntroFadeInDuration);
        float centerDuration = Mathf.Max(0f, itemCenterHoldDuration, fadeDuration);
        float centerElapsed = 0f;
        while (centerElapsed < centerDuration)
        {
            centerElapsed += Time.unscaledDeltaTime;
            float progress = fadeDuration <= 0f ? 1f : Mathf.Clamp01(centerElapsed / fadeDuration);
            progress = progress * progress * (3f - 2f * progress);
            if (introItemCanvasGroup != null)
                introItemCanvasGroup.alpha = introItemOriginalAlpha * progress;
            yield return null;
        }
        if (introItemCanvasGroup != null)
            introItemCanvasGroup.alpha = introItemOriginalAlpha;

        // Keep the rest of the panel hidden throughout the movement.
        float duration = Mathf.Max(0f, itemReturnMoveDuration);
        if (introItemRect != null && duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                introItemRect.position = Vector3.LerpUnclamped(centerWorld, introItemOriginalWorldPosition, t);
                yield return null;
            }
        }
        if (introItemRect != null)
            introItemRect.anchoredPosition = introItemOriginalAnchoredPosition;
        // Reveal other UI only when the item has reached its final position.
        yield return FadeInOtherIntroObjects();
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
        RestoreIntroItemCanvasGroup();
        introItemRect = null;
        itemIntroCoroutine = null;
        RefreshSlotPreviewVfx();
    }

    private IEnumerator FadeInOtherIntroObjects()
    {
        RestoreOtherIntroAlpha();
        for (int i = 0; i < introHiddenObjects.Count; i++)
        {
            GameObject child = introHiddenObjects[i];
            if (child == null) continue;
            CanvasGroup group = child.GetComponent<CanvasGroup>();
            if (group == null) group = child.AddComponent<CanvasGroup>();
            introOtherCanvasGroups.Add(group);
            introOtherOriginalAlphas.Add(group.alpha);
            introOtherOriginalInteractables.Add(group.interactable);
            introOtherOriginalRaycasts.Add(group.blocksRaycasts);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            child.SetActive(true);
        }
        introHiddenObjects.Clear();

        float duration = Mathf.Max(0f, otherPanelsFadeInDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            for (int i = 0; i < introOtherCanvasGroups.Count; i++)
                if (introOtherCanvasGroups[i] != null)
                    introOtherCanvasGroups[i].alpha = introOtherOriginalAlphas[i] * t;
            yield return null;
        }
        RestoreOtherIntroAlpha();
    }

    private void RestoreOtherIntroAlpha()
    {
        for (int i = 0; i < introOtherCanvasGroups.Count; i++)
        {
            CanvasGroup group = introOtherCanvasGroups[i];
            if (group == null) continue;
            group.alpha = introOtherOriginalAlphas[i];
            group.interactable = introOtherOriginalInteractables[i];
            group.blocksRaycasts = introOtherOriginalRaycasts[i];
        }
        introOtherCanvasGroups.Clear();
        introOtherOriginalAlphas.Clear();
        introOtherOriginalInteractables.Clear();
        introOtherOriginalRaycasts.Clear();
    }

    private void RestoreOtherIntroObjects()
    {
        RestoreOtherIntroAlpha();
        foreach (GameObject child in introHiddenObjects)
            if (child != null) child.SetActive(true);
        introHiddenObjects.Clear();
    }

    private void SpawnItemImageIntroVfx()
    {
        if (itemImageIntroVfxPrefab == null || introItemRect == null) return;
        // Spawn at the complete item's visual center, outside any child Mask.
        Vector3 center = introItemRect.TransformPoint(introItemRect.rect.center);
        activeItemImageIntroVfx = Instantiate(itemImageIntroVfxPrefab, introItemRect, false);
        activeItemImageIntroVfx.name = itemImageIntroVfxPrefab.name + "_ItemIntroVFX";
        activeItemImageIntroVfx.transform.position = center + introItemRect.TransformVector(itemImageIntroVfxOffset);
        activeItemImageIntroVfx.transform.localRotation = Quaternion.identity;
        activeItemImageIntroVfx.transform.localScale = itemImageIntroVfxScale;
        activeItemImageIntroVfx.transform.SetAsLastSibling();
        if (tintItemIntroParticlesByRarity)
            ApplyIntroParticleRarityColor(activeItemImageIntroVfx);
        activeItemImageIntroVfx.SetActive(true);
        if (itemImageIntroVfxLifetime > 0f)
            Destroy(activeItemImageIntroVfx, itemImageIntroVfxLifetime);
    }

    private void ApplyIntroParticleRarityColor(GameObject vfx)
    {
        if (vfx == null || current == null || DataManager.Instance?.RelicDatabase == null)
            return;

        if (!DataManager.Instance.RelicDatabase.TryGet(current.RewardId, out RelicData relic) || relic == null)
            return;

        Color rarityColor;
        switch (RelicRarityUtility.GetRevealRank(ParseRarity(relic.Rarity)))
        {
            case 2: rarityColor = rareColor; break;
            case 3: rarityColor = epicColor; break;
            case 4: rarityColor = uniqueColor; break;
            default: rarityColor = commonColor; break;
        }

        // Change only the instantiated VFX. Keep the prefab asset unchanged.
        foreach (ParticleSystem particles in vfx.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(rarityColor);
        }
    }

    private void RestoreIntroItemCanvasGroup()
    {
        if (introItemCanvasGroup == null) return;
        introItemCanvasGroup.alpha = introItemOriginalAlpha;
        introItemCanvasGroup.interactable = introItemOriginalInteractable;
        introItemCanvasGroup.blocksRaycasts = introItemOriginalBlocksRaycasts;
        introItemCanvasGroup = null;
    }

    private void ResetItemIntro()
    {
        if (activeItemImageIntroVfx != null)
        {
            Destroy(activeItemImageIntroVfx);
            activeItemImageIntroVfx = null;
        }
        if (itemIntroCoroutine != null)
        {
            StopCoroutine(itemIntroCoroutine);
            itemIntroCoroutine = null;
        }
        RestoreIntroItemCanvasGroup();
        if (introItemRect != null)
        {
            introItemRect.anchoredPosition = introItemOriginalAnchoredPosition;
            introItemRect = null;
        }
        RestoreOtherIntroObjects();
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
                    // Hover label is no longer used; preview VFX is the only equip hint.
                    if (hoverRoot != null)
                    {
                        foreach (TMP_Text text in hoverRoot.GetComponentsInChildren<TMP_Text>(true))
                        {
                            text.text = string.Empty;
                            text.enabled = false;
                        }
                    }
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
        // Keep the preview VFX behavior; no Equip/Replace text is displayed.
        RefreshSlotPreviewVfx();
    }

    // Mirror the same slot that currently displays the Equip/Replace hover label.
    private void RefreshSlotPreviewVfx()
    {
        int selectedCharacter = -1;
        int selectedSlot = -1;
        if (current != null && !resolving && lockedEquipCharacterIndex < 0 &&
            itemIntroCoroutine == null && slotPreviewVfxPrefab != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                CharacterEntry entry = entries[i];
                if (entry == null || entry.root == null || !entry.root.activeInHierarchy)
                    continue;
                int firstEmpty = FindFirstEmptyRelicSlot(entry);
                int highlighted = -1;
                if (entry.characterHovered || entry.hoveredSlot >= 0)
                {
                    if (entry.hoveredSlot >= 0 && entry.hoveredSlot < 6 &&
                        entry.tooltipHovers[entry.hoveredSlot] != null &&
                        entry.tooltipHovers[entry.hoveredSlot].HasRelic)
                        highlighted = entry.hoveredSlot;
                    else
                        highlighted = firstEmpty;
                }
                if (highlighted < 0) continue;
                selectedCharacter = i;
                selectedSlot = highlighted;
                // An explicitly hovered slot takes priority over character hover.
                if (entry.hoveredSlot >= 0) break;
            }
        }

        if (selectedCharacter == previewCharacterIndex && selectedSlot == previewSlotIndex &&
            activeSlotPreviewVfx != null)
            return;

        ClearSlotPreviewVfx();
        if (selectedCharacter < 0 || selectedSlot < 0) return;
        Transform slot = FindEquippedContentSlot(selectedCharacter, selectedSlot);
        if (slot == null) return;
        Canvas.ForceUpdateCanvases();
        RectTransform rect = slot as RectTransform;
        Vector3 center = rect != null ? rect.TransformPoint(rect.rect.center) : slot.position;
        activeSlotPreviewVfx = Instantiate(slotPreviewVfxPrefab, transform, false);
        activeSlotPreviewVfx.name = slotPreviewVfxPrefab.name + "_RelicSlotPreviewVFX";
        activeSlotPreviewVfx.transform.position = center + slot.TransformVector(slotPreviewVfxOffset);
        activeSlotPreviewVfx.transform.localRotation = Quaternion.identity;
        activeSlotPreviewVfx.transform.localScale = slotPreviewVfxScale;
        activeSlotPreviewVfx.transform.SetAsLastSibling();
        activeSlotPreviewVfx.SetActive(true);
        previewCharacterIndex = selectedCharacter;
        previewSlotIndex = selectedSlot;
    }

    private void ClearSlotPreviewVfx()
    {
        if (activeSlotPreviewVfx != null)
        {
            activeSlotPreviewVfx.SetActive(false);
            Destroy(activeSlotPreviewVfx);
            activeSlotPreviewVfx = null;
        }
        previewCharacterIndex = -1;
        previewSlotIndex = -1;
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
        equipCloseCoroutine = StartCoroutine(PlayEquipVfxAndCommit(runtime, relic, destination, index, active));
    }

    private void LockEquippingCharacter(int selectedIndex)
    {
        lockedEquipCharacterIndex = selectedIndex;
        ClearSlotPreviewVfx();
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

    private IEnumerator PlayEquipVfxAndCommit(CharacterRuntimeData runtime, RelicData relic,
        int destination, int characterIndex, bool active)
    {
        HideEquippedRelicTooltip();

        if (equipVfxDelay > 0f)
            yield return new WaitForSecondsRealtime(equipVfxDelay);

        if (current == null || DataManager.Instance == null)
        {
            equipCloseCoroutine = null;
            yield break;
        }

        // Update the actual equipped slot before playing its VFX.
        runtime.EquippedRelicIds[destination] = current.RewardId.Trim();
        if (active) ActiveRelicRuntimeUtility.ResetUses(runtime, relic);
        DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(runtime);
        Refresh();

        // Animate only the newly equipped icon, not its slot or other content.
        int animatedVisualSlot = active ? 0 : destination - 1;
        CharacterEntry animatedEntry = entries[characterIndex];
        if (animatedEntry != null && animatedVisualSlot >= 0 && animatedVisualSlot < animatedEntry.relics.Length)
            PlayEquippedIconScale(animatedEntry.relics[animatedVisualSlot]);

        // Find the actual displayed slot after Refresh() has updated its icons.
        // Keep the original six-slot visual order for both equip and replacement.
        int visualSlot = active ? 0 : destination - 1;
        Transform slot = FindEquippedContentSlot(characterIndex, visualSlot);
        if (slot != null)
            SpawnEquipVfx(equippedSlotVfxPrefab, slot, equippedSlotVfxOffset,
                equippedSlotVfxScale, slotVfxLifetime);
        else if (equippedSlotVfxPrefab != null)
            Debug.LogWarning("[BattleRewardRelicPanelUI] Equipped VFX slot not found: Char" +
                (characterIndex + 1) + "/Relic/Relic" + (visualSlot + 1).ToString("00"), this);

        if (equipCloseDelay > 0f)
            yield return new WaitForSecondsRealtime(equipCloseDelay);
        EnsurePanelCanvasGroup();
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
        yield return FadeOutPanel();
        equipCloseCoroutine = null;
        ClearEquipVfx();
        Action callback = onResolved;
        onResolved = null;
        current = null;
        gameObject.SetActive(false);
        callback?.Invoke();
    }

    private void PlayEquippedIconScale(Image icon)
    {
        RestoreEquippedIconScale();
        if (icon == null || icon.sprite == null || !icon.enabled) return;
        animatedEquippedIcon = icon;
        animatedIconOriginalScale = icon.rectTransform.localScale;
        if (equippedIconScaleDuration <= 0f || equippedIconStartScale <= 1f) return;
        icon.rectTransform.localScale = animatedIconOriginalScale * equippedIconStartScale;
        equippedIconScaleCoroutine = StartCoroutine(AnimateEquippedIconScale(icon.rectTransform));
    }

    private IEnumerator AnimateEquippedIconScale(RectTransform icon)
    {
        Vector3 start = animatedIconOriginalScale * equippedIconStartScale;
        float elapsed = 0f;
        while (elapsed < equippedIconScaleDuration && icon != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / equippedIconScaleDuration);
            t = t * t * (3f - 2f * t);
            icon.localScale = Vector3.LerpUnclamped(start, animatedIconOriginalScale, t);
            yield return null;
        }
        if (icon != null) icon.localScale = animatedIconOriginalScale;
        equippedIconScaleCoroutine = null;
        animatedEquippedIcon = null;
    }

    private void RestoreEquippedIconScale()
    {
        if (equippedIconScaleCoroutine != null)
        {
            StopCoroutine(equippedIconScaleCoroutine);
            equippedIconScaleCoroutine = null;
        }
        if (animatedEquippedIcon != null)
            animatedEquippedIcon.rectTransform.localScale = animatedIconOriginalScale;
        animatedEquippedIcon = null;
    }

    private Transform FindEquippedContentSlot(int characterIndex, int slotIndex)
    {
        if (characterIndex < 0 || characterIndex >= entries.Length || slotIndex < 0 || slotIndex >= 6)
            return null;

        // Resolve the current hierarchy first, not potentially stale inspector references.
        Transform root = content != null ? content.Find("Char" + (characterIndex + 1)) : null;
        Transform slot = root != null ? root.Find("Relic/Relic" + (slotIndex + 1).ToString("00")) : null;
        if (slot != null) return slot;

        CharacterEntry entry = entries[characterIndex];
        Image icon = entry != null ? entry.relics[slotIndex] : null;
        return icon != null ? icon.transform.parent : null;
    }

    private void SpawnEquipVfx(GameObject prefab, Transform anchor, Vector3 offset,
        Vector3 size, float lifetime)
    {
        if (prefab == null || anchor == null) return;
        // Rebuild UI before sampling the equipped slot's final center.
        Canvas.ForceUpdateCanvases();
        // Spawn outside slot masks without moving the slot or changing LayoutGroup order.
        RectTransform anchorRect = anchor as RectTransform;
        Vector3 center = anchorRect != null ? anchorRect.TransformPoint(anchorRect.rect.center) : anchor.position;
        GameObject vfx = Instantiate(prefab, transform, false);
        vfx.name = prefab.name + "_RelicEquipVFX";
        vfx.transform.position = center + anchor.TransformVector(offset);
        vfx.transform.localRotation = Quaternion.identity;
        vfx.transform.localScale = size;
        vfx.transform.SetAsLastSibling();
        vfx.SetActive(true);
        spawnedEquipVfx.Add(vfx);
        if (lifetime > 0f)
            Destroy(vfx, lifetime);
    }

    private void ClearEquipVfx()
    {
        for (int i = 0; i < spawnedEquipVfx.Count; i++)
            if (spawnedEquipVfx[i] != null) Destroy(spawnedEquipVfx[i]);
        spawnedEquipVfx.Clear();
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

    public void Configure(BattleRewardRelicPanelUI tooltipPanel, RectTransform targetSlot, int character, int index)
    {
        panel = tooltipPanel;
        characterIndex = character;
        slotIndex = index;
        slot = targetSlot;
    }

    public void SetRelicId(string value)
    {
        if (string.Equals(relicId, value, StringComparison.Ordinal)) return;
        relicId = value;
        if (!isHovered) return;
        if (string.IsNullOrWhiteSpace(relicId))
        {
            panel?.HideEquippedRelicTooltip();
        }
        else
        {
            panel?.ShowEquippedRelicTooltip(slot, relicId);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        panel?.SetRelicSlotHover(characterIndex, slotIndex, true);
        if (string.IsNullOrWhiteSpace(relicId)) return;
        panel?.ShowEquippedRelicTooltip(slot, relicId);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        panel?.SetRelicSlotHover(characterIndex, slotIndex, false);
        panel?.HideEquippedRelicTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            panel?.EquipRelicSlot(characterIndex, slotIndex);
    }

    private void OnDisable()
    {
        isHovered = false;
        panel?.HideEquippedRelicTooltip();
    }
}
