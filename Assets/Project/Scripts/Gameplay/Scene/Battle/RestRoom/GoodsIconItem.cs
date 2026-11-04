using Relic.Gameplay.Data;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoodsIconItem : MonoBehaviour
{
    [Header("Common")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text plateText;
    [SerializeField] private GameObject skillRoot;
    [SerializeField] private GameObject relicRoot;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject soldoutRoot;

    [Header("Event Shop Goods Prefab")]
    [SerializeField] private Image line2;
    [Header("Rarity Color Image")]
    [Tooltip("Optional UI Image tinted with the same rarity color as the VFX. Its original alpha is preserved.")]
    [SerializeField] private Image rarityColorImage;
    [SerializeField] private Image goodsIcon;
    [SerializeField] private TMP_Text goodsName;
    [SerializeField] private TMP_Text goodsDetail;
    [SerializeField] private GameObject discountIcon;
    [SerializeField] private TMP_Text discountText;
    [SerializeField] private Color discountedOriginalPriceColor = new Color32(0x77, 0x77, 0x77, 0xFF);

    [Header("Rarity VFX")]
    [Tooltip("Shared VFX. Its particles and graphics are tinted according to the goods rarity.")]
    [SerializeField] private GameObject rarityVfxPrefab;
    [Tooltip("Optional parent for the VFX. Defaults to this GoodsIconItem transform.")]
    [SerializeField] private Transform rarityVfxRoot;
    [SerializeField] private Vector3 rarityVfxOffset = Vector3.zero;
    [SerializeField] private Vector3 rarityVfxScale = Vector3.one;
    [SerializeField] private Color commonVfxColor = new Color32(0xA9, 0xB1, 0xBE, 0xFF);
    [SerializeField] private Color rareVfxColor = new Color32(0x52, 0x86, 0xC7, 0xFF);
    [SerializeField] private Color epicVfxColor = new Color32(0xA0, 0x72, 0xC2, 0xFF);
    [SerializeField] private Color uniqueVfxColor = new Color32(0xCD, 0xA4, 0x52, 0xFF);
    [Header("Rarity VFX - After Back")]
    [Tooltip("Second rarity VFX spawned immediately after Back in the Goods hierarchy.")]
    [SerializeField] private GameObject backRarityVfxPrefab;
    [Tooltip("Optional Back reference. Defaults to the direct child named Back.")]
    [SerializeField] private Transform backRarityVfxAnchor;
    [SerializeField] private Vector3 backRarityVfxOffset = Vector3.zero;
    [SerializeField] private Vector3 backRarityVfxScale = Vector3.one;
    private GameObject activeRarityVfx;
    private GameObject activeBackRarityVfx;
    private string activeRarityVfxKey;

    [Header("Hover - Price Exclusion")]
    [Tooltip("Goods가 호버로 확대되어도 Price 영역의 크기와 상대 위치를 유지합니다.")]
    [SerializeField] private bool excludePriceFromHoverScale = true;

    private RectTransform priceRootTransform;
    private Vector3 baseGoodsScale;
    private Vector3 basePriceScale;
    private Vector2 basePriceAnchoredPosition;
    private bool priceHoverOriginCached;

    [Header("Skill")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private TMP_Text skillName;
    [SerializeField] private TMP_Text skillRarity;
    [SerializeField] private Image skillCostIcon;
    [SerializeField] private TMP_Text skillCostValue;
    [SerializeField] private TMP_Text skillDetail;
    [SerializeField] private Image skillRange;

    [Header("Relic")]
    [SerializeField] private Image relicIcon;
    [SerializeField] private TMP_Text relicName;
    [SerializeField] private TMP_Text relicRarity;
    [SerializeField] private TMP_Text relicDetail;

    [Header("Resource Icons")]
    [SerializeField] private Sprite costResourceIcon;
    [SerializeField] private Sprite hpResourceIcon;
    [SerializeField] private Sprite uniqueResourceIcon;
    [SerializeField] private Sprite moveResourceIcon;

    private RestRoomShopGoods goods;
    private Action<GoodsIconItem, RestRoomShopGoods> onClicked;
    private RectTransform rectTransform;
    private bool isPurchased;

    private const float SoldoutGray = 85f / 255f;
    private readonly Dictionary<Image, Color> originalImageColors = new();
    private readonly Dictionary<TMP_Text, Color> originalTextColors = new();
    private bool originalColorsCached;

    public RestRoomShopGoods Goods => goods;

    private void Awake()
    {
        AutoBind();
        ProtectDynamicLocalizedTexts();
        CacheOriginalVisualColors();
        CachePriceHoverOrigin();
    }

    private void OnEnable()
    {
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady -= OnLocaleTableReady;
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady += OnLocaleTableReady;
    }

    private void OnDisable()
    {
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady -= OnLocaleTableReady;
        ClearRarityVfx();
    }

    private void LateUpdate()
    {
        if (!excludePriceFromHoverScale)
            return;

        if (!priceHoverOriginCached)
            CachePriceHoverOrigin();

        if (!priceHoverOriginCached || priceRootTransform == null)
            return;

        // Goods 전체의 호버 확대를 Price에 대해서만 역보정합니다.
        // 확대/축소 중 매 프레임 적용해 Price의 크기와 기준 위치를 유지합니다.
        Vector3 currentScale = transform.localScale;
        float ratioX = Mathf.Abs(baseGoodsScale.x) > 0.0001f ? currentScale.x / baseGoodsScale.x : 1f;
        float ratioY = Mathf.Abs(baseGoodsScale.y) > 0.0001f ? currentScale.y / baseGoodsScale.y : 1f;
        float ratioZ = Mathf.Abs(baseGoodsScale.z) > 0.0001f ? currentScale.z / baseGoodsScale.z : 1f;
        if (Mathf.Abs(ratioX) < 0.0001f || Mathf.Abs(ratioY) < 0.0001f || Mathf.Abs(ratioZ) < 0.0001f)
            return;

        priceRootTransform.localScale = new Vector3(
            basePriceScale.x / ratioX,
            basePriceScale.y / ratioY,
            basePriceScale.z / ratioZ);
        priceRootTransform.anchoredPosition = new Vector2(
            basePriceAnchoredPosition.x / ratioX,
            basePriceAnchoredPosition.y / ratioY);
    }

    private void CachePriceHoverOrigin()
    {
        if (priceHoverOriginCached)
            return;

        Transform price = FindTransform("Price");
        if (price == null || price.parent != transform)
            return; // 기존 스킬/유물 프리팹에는 적용하지 않습니다.

        priceRootTransform = price as RectTransform;
        if (priceRootTransform == null)
            return;

        baseGoodsScale = transform.localScale;
        basePriceScale = priceRootTransform.localScale;
        basePriceAnchoredPosition = priceRootTransform.anchoredPosition;
        priceHoverOriginCached = true;
    }

    private void OnValidate()
    {
        AutoBind();
    }

    public void ConfigureResourceIcons(Sprite cost, Sprite hp, Sprite unique, Sprite move)
    {
        costResourceIcon = cost;
        hpResourceIcon = hp;
        uniqueResourceIcon = unique;
        moveResourceIcon = move;
    }

    public void Initialize(RestRoomShopGoods sourceGoods, Action<GoodsIconItem, RestRoomShopGoods> clicked)
    {
        AutoBind();

        goods = sourceGoods;
        onClicked = clicked;
        isPurchased = false;
        ClearRarityVfx();
        RestoreOriginalVisualColors();
        SetActive(soldoutRoot, false);

        if (goods == null)
        {
            Clear();
            return;
        }

        gameObject.SetActive(true);
        RefreshPrice();

        bool isSkill = goods.Kind == RestRoomShopGoodsKind.Skill && goods.Skill != null;
        bool isRelic = goods.Kind == RestRoomShopGoodsKind.Relic && goods.Relic != null;

        SetActive(skillRoot, isSkill);
        SetActive(relicRoot, isRelic);

        if (isSkill)
        {
            BindSkill(goods.Skill, goods.Icon);
        }
        else if (isRelic)
        {
            BindRelic(goods.Relic, goods.Icon);
        }
        else
        {
            SetText(plateText, string.Empty);
            SetImage(goodsIcon, null);
            SetText(goodsName, string.Empty);
            SetText(goodsDetail, string.Empty);
            ClearRarityVfx();
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
            button.interactable = true;
        }

        SetButtonAnimationInteraction(true);
    }

    public void Clear()
    {
        AutoBind();

        goods = null;
        onClicked = null;
        isPurchased = false;
        ClearRarityVfx();
        RestoreOriginalVisualColors();
        SetActive(soldoutRoot, false);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = false;
        }

        SetButtonAnimationInteraction(true);

        SetText(plateText, string.Empty);
        SetText(priceText, string.Empty);
        SetActive(discountIcon, false);
        SetText(discountText, string.Empty);
        SetImage(goodsIcon, null);
        SetText(goodsName, string.Empty);
        SetText(goodsDetail, string.Empty);
        SetActive(skillRoot, false);
        SetActive(relicRoot, false);
    }

    public void RefreshPrice()
    {
        if (goods == null || isPurchased)
            return;

        int originalPrice = GetOriginalPrice(goods);
        bool hasDiscount = originalPrice > goods.Price;
        SetText(priceText, (hasDiscount ? originalPrice : goods.Price).ToString());
        SetActive(discountIcon, hasDiscount);
        SetText(discountText, hasDiscount ? goods.Price.ToString() : string.Empty);

        if (priceText != null)
        {
            CacheOriginalVisualColors();
            priceText.color = hasDiscount
                ? discountedOriginalPriceColor
                : originalTextColors.TryGetValue(priceText, out Color original) ? original : Color.white;
        }
    }

    private static int GetOriginalPrice(RestRoomShopGoods item)
    {
        if (item.Kind == RestRoomShopGoodsKind.Relic && item.Relic != null)
            return Mathf.Max(0, item.Relic.RedDustiumCost);
        if (item.Kind == RestRoomShopGoodsKind.Skill && item.Skill != null)
            return Mathf.Max(0, item.Skill.RedDustiumCost);
        return Mathf.Max(0, item.Price);
    }

    public void MarkPurchased()
    {
        isPurchased = true;

        if (button != null)
            button.interactable = false;

        SetButtonAnimationInteraction(false);
        SetActive(soldoutRoot, true);
        ApplyPurchasedVisualState();
    }

    private void BindSkill(SkillMasterData skill, Sprite resolvedIcon)
    {
        if (skill == null)
            return;

        Sprite icon = resolvedIcon != null ? resolvedIcon : ResolveSkillIcon(skill);
        SetImage(skillIcon, icon);
        SkillUpgradeMarkStyle.ApplyShared(skillIcon, skill.SkillId);

        SetText(skillName, GameDataLocalization.SkillName(skill));
        SetImage(goodsIcon, icon);
        SetText(goodsName, GameDataLocalization.SkillName(skill));
        SkillEffectInlineIconUtility.SetText(goodsDetail, GameDataLocalization.SkillDetails(skill));
        ApplySkillRarityPresentation(skill.Rarity);
        SetText(skillCostValue, Mathf.Max(0, skill.ResourceCostValue).ToString());
        SkillEffectInlineIconUtility.SetText(skillDetail, GameDataLocalization.SkillDetails(skill));

        Sprite resourceIcon = ResolveResourceIcon(skill.ReferenceResource);
        SetImage(skillCostIcon, resourceIcon);

        Sprite rangeIcon = ResolveRangeIcon(skill.RangeId);
        SetImage(skillRange, rangeIcon);
    }

    private void BindRelic(RelicData relic, Sprite resolvedIcon)
    {
        if (relic == null)
            return;

        Sprite icon = resolvedIcon != null ? resolvedIcon : ResolveRelicIcon(relic.FragmentId);
        SetImage(relicIcon, icon);
        SetText(relicName, GameDataLocalization.RelicName(relic));
        SetImage(goodsIcon, icon);
        SetText(goodsName, GameDataLocalization.RelicName(relic));
        SetText(goodsDetail, GameDataLocalization.RelicEffectDescription(relic));
        ApplyRelicRarityPresentation(relic);
        SetText(relicDetail, GameDataLocalization.RelicEffectDescription(relic));
    }

    private void ApplySkillRarityPresentation(SkillRarity rarity)
    {
        string canonicalRarity = SkillRarityUtility.GetCanonicalName(rarity);
        string rarityText = GetSkillRarityText(rarity);

        // Plate_Text는 상품 종류만 표시하며 레어도 색상을 적용하지 않습니다.
        SetText(plateText, GameLocalization.Get("common.memory"));
        SetText(skillRarity, rarityText);
        ApplyRecordRarityColor(canonicalRarity, skillRarity);
        ApplyLineRarityColor(canonicalRarity);
        ApplyRarityImageColor(canonicalRarity);
        ShowRarityVfx(canonicalRarity);
    }

    private void ApplyRelicRarityPresentation(RelicData relic)
    {
        string canonicalRarity = GetRelicCanonicalRarity(relic);
        string rarityText = GetRelicRarityText(relic);
        string relicTypeText = GameLocalization.Get("common.relic");
        string rarityTypeText = string.IsNullOrWhiteSpace(rarityText)
            ? relicTypeText
            : $"{rarityText} {relicTypeText}";

        // Plate_Text는 상품 종류만 표시하며 레어도 색상을 적용하지 않습니다.
        SetText(plateText, GameLocalization.Get("common.relic"));
        SetText(relicRarity, rarityTypeText);
        ApplyRecordRarityColor(canonicalRarity, relicRarity);
        ApplyLineRarityColor(canonicalRarity);
        ApplyRarityImageColor(canonicalRarity);
        ShowRarityVfx(canonicalRarity);
    }

    private void ApplyRecordRarityColor(string rarity, params TMP_Text[] targets)
    {
        if (string.IsNullOrWhiteSpace(rarity) || targets == null || targets.Length == 0)
            return;

        Color rarityColor;
        bool foundColor = RecordPanelUI.TryGetCachedRarityDisplayColor(rarity, out rarityColor);

        if (!foundColor)
        {
            RecordPanelUI recordPanel = UnityEngine.Object.FindFirstObjectByType<RecordPanelUI>(FindObjectsInactive.Include);
            if (recordPanel == null)
                return;

            rarityColor = recordPanel.GetRarityDisplayColor(rarity);
        }

        for (int i = 0; i < targets.Length; i++)
        {
            TMP_Text target = targets[i];
            if (target != null)
                target.color = rarityColor;
        }
    }

    private void ApplyLineRarityColor(string rarity)
    {
        if (line2 == null || string.IsNullOrWhiteSpace(rarity))
            return;

        if (RecordPanelUI.TryGetCachedRarityDisplayColor(rarity, out Color color))
        {
            line2.color = color;
            return;
        }

        RecordPanelUI recordPanel = UnityEngine.Object.FindFirstObjectByType<RecordPanelUI>(FindObjectsInactive.Include);
        if (recordPanel != null)
            line2.color = recordPanel.GetRarityDisplayColor(rarity);
    }

    private void ApplyRarityImageColor(string rarity)
    {
        if (rarityColorImage == null || string.IsNullOrWhiteSpace(rarity))
            return;

        CacheOriginalVisualColors();
        Color tint = GetRarityVfxColor(rarity.Trim());
        Color original = originalImageColors.TryGetValue(rarityColorImage, out Color cached)
            ? cached : rarityColorImage.color;
        rarityColorImage.color = new Color(tint.r, tint.g, tint.b, original.a);
    }

    private void ShowRarityVfx(string rarity)
    {
        string key = string.IsNullOrWhiteSpace(rarity) ? string.Empty : rarity.Trim();
        if (string.Equals(activeRarityVfxKey, key, StringComparison.OrdinalIgnoreCase) &&
            (rarityVfxPrefab == null || activeRarityVfx != null) &&
            (backRarityVfxPrefab == null || activeBackRarityVfx != null))
            return;

        ClearRarityVfx();
        if (key.Length == 0)
            return;

        Color tint = GetRarityVfxColor(key);
        if (rarityVfxPrefab != null)
        {
            Transform parent = rarityVfxRoot != null &&
                (rarityVfxRoot == transform || rarityVfxRoot.IsChildOf(transform))
                ? rarityVfxRoot : transform;
            activeRarityVfx = CreateRarityVfx(
                rarityVfxPrefab, parent, rarityVfxOffset, rarityVfxScale,
                "_GoodsRarityVFX", tint);
            // Preserve the original VFX at the final sibling position.
            activeRarityVfx.transform.SetAsLastSibling();
        }

        if (backRarityVfxPrefab != null)
        {
            // Back's sibling is the intended render layer; do not parent under Back.
            Transform back = backRarityVfxAnchor != null &&
                backRarityVfxAnchor.parent == transform
                ? backRarityVfxAnchor : transform.Find("Back");
            if (back != null && back.parent == transform)
            {
                activeBackRarityVfx = CreateRarityVfx(
                    backRarityVfxPrefab, transform, backRarityVfxOffset,
                    backRarityVfxScale, "_BackRarityVFX", tint);
                activeBackRarityVfx.transform.SetSiblingIndex(back.GetSiblingIndex() + 1);
            }
            else
                Debug.LogWarning("[GoodsIconItem] Back child was not found; Back rarity VFX was not spawned.", this);
        }

        activeRarityVfxKey = key;
    }

    private GameObject CreateRarityVfx(GameObject prefab, Transform parent, Vector3 offset,
        Vector3 scale, string nameSuffix, Color tint)
    {
        GameObject instance = Instantiate(prefab, parent, false);
        instance.name = prefab.name + nameSuffix;
        instance.transform.localPosition = offset;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = scale;

        foreach (ParticleSystem particle in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particle.main;
            ParticleSystem.MinMaxGradient original = main.startColor;
            Color result = tint;
            if (original.mode == ParticleSystemGradientMode.Color)
                result.a *= original.color.a;
            main.startColor = result;
        }
        foreach (Graphic graphic in instance.GetComponentsInChildren<Graphic>(true))
        {
            Color original = graphic.color;
            graphic.color = new Color(tint.r, tint.g, tint.b, original.a);
            graphic.raycastTarget = false;
        }
        foreach (SpriteRenderer sprite in instance.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color original = sprite.color;
            sprite.color = new Color(tint.r, tint.g, tint.b, original.a);
        }

        CanvasGroup canvasGroup = instance.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = instance.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        instance.SetActive(true);
        return instance;
    }

    private Color GetRarityVfxColor(string rarity)
    {
        switch (rarity.ToUpperInvariant())
        {
            case "RARE": return rareVfxColor;
            case "EPIC": return epicVfxColor;
            case "UNIQUE": return uniqueVfxColor;
            default: return commonVfxColor;
        }
    }

    private void ClearRarityVfx()
    {
        activeRarityVfxKey = null;
        DestroyRarityVfx(ref activeRarityVfx);
        DestroyRarityVfx(ref activeBackRarityVfx);
    }

    private void DestroyRarityVfx(ref GameObject instance)
    {
        if (instance == null)
            return;
        instance.SetActive(false);
        if (Application.isPlaying)
            Destroy(instance);
        else
            DestroyImmediate(instance);
        instance = null;
    }

    private void OnLocaleTableReady(UnityEngine.Localization.Locale _)
    {
        if (goods == null)
            return;

        if (goods.Kind == RestRoomShopGoodsKind.Skill && goods.Skill != null)
            BindSkill(goods.Skill, goods.Icon);
        else if (goods.Kind == RestRoomShopGoodsKind.Relic && goods.Relic != null)
            BindRelic(goods.Relic, goods.Icon);

        if (isPurchased)
            ApplyPurchasedVisualState();
    }

    private void ProtectDynamicLocalizedTexts()
    {
        ProtectDynamicLocalizedText(plateText);
    }

    private static void ProtectDynamicLocalizedText(TMP_Text target)
    {
        if (target == null)
            return;

        if (target.GetComponent<LocalizationIgnore>() == null)
            target.gameObject.AddComponent<LocalizationIgnore>();
        if (target.GetComponent<LocalizationAutoBindingIgnore>() == null)
            target.gameObject.AddComponent<LocalizationAutoBindingIgnore>();

        LocalizedTMPText staticWriter = target.GetComponent<LocalizedTMPText>();
        if (staticWriter != null)
            staticWriter.enabled = false;

        DynamicLocalizedTMPText dynamicWriter = target.GetComponent<DynamicLocalizedTMPText>();
        if (dynamicWriter != null)
            dynamicWriter.enabled = false;
    }

    private string GetSkillRarityText(SkillRarity rarity)
    {
        return SkillRarityUtility.GetDisplayName(rarity);
    }

    private string GetRelicRarityText(RelicData relic)
    {
        if (relic == null || !RelicRarityUtility.TryParseChestRarity(relic.Rarity, out _))
            return string.IsNullOrWhiteSpace(relic?.Rarity) ? string.Empty : relic.Rarity;

        return RelicRarityUtility.GetDisplayName(relic.Rarity);
    }

    private string GetRelicCanonicalRarity(RelicData relic)
    {
        if (relic == null)
            return string.Empty;

        if (!RelicRarityUtility.TryParseChestRarity(relic.Rarity, out RelicRarity rarity))
            return relic.Rarity ?? string.Empty;

        return rarity switch
        {
            RelicRarity.Common => "Common",
            RelicRarity.Rare => "Rare",
            RelicRarity.Epic => "Epic",
            RelicRarity.Unique => "Unique",
            _ => relic.Rarity ?? string.Empty
        };
    }

    private Sprite ResolveSkillIcon(SkillMasterData skill)
    {
        if (skill?.Icon != null)
            return skill.Icon;

        if (DataManager.Instance?.SkillIconDatabase != null &&
            DataManager.Instance.SkillIconDatabase.TryGetIcon(skill?.SkillId, out Sprite icon))
        {
            return icon;
        }

        return null;
    }

    private Sprite ResolveRelicIcon(string relicId)
    {
        if (DataManager.Instance?.RelicIconDatabase != null &&
            DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out Sprite icon))
        {
            return icon;
        }

        return null;
    }

    private Sprite ResolveRangeIcon(string rangeId)
    {
        if (string.IsNullOrWhiteSpace(rangeId) || DataManager.Instance?.SkillRangeIconDatabase == null)
            return null;

        return DataManager.Instance.SkillRangeIconDatabase.TryGetIcon(rangeId, out Sprite icon)
            ? icon
            : null;
    }

    private Sprite ResolveResourceIcon(ReferenceResource resource)
    {
        return resource switch
        {
            ReferenceResource.HP => hpResourceIcon,
            ReferenceResource.UniqueResource => uniqueResourceIcon,
            ReferenceResource.MovePoint => moveResourceIcon != null ? moveResourceIcon : costResourceIcon,
            _ => costResourceIcon
        };
    }

    private void HandleClick()
    {
        if (isPurchased || goods == null)
            return;

        onClicked?.Invoke(this, goods);
    }

    private void AutoBind()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (button == null)
            button = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);

        if (plateText == null)
            plateText = FindComponent<TMP_Text>("Plate_Text");

        // The event shop prefab uses direct children rather than Skill/Relic roots.
        if (line2 == null) line2 = FindComponent<Image>("Line2");
        if (goodsIcon == null) goodsIcon = FindComponent<Image>("Icon");
        if (goodsName == null) goodsName = FindTextInBranch("Name");
        if (goodsDetail == null) goodsDetail = FindTextInBranch("Detail");
        if (discountIcon == null) discountIcon = FindTransform("DiscountIcon")?.gameObject;
        if (discountText == null) discountText = FindComponent<TMP_Text>("DiscountText");

        if (skillRoot == null)
            skillRoot = FindTransform("Skill")?.gameObject;

        if (relicRoot == null)
            relicRoot = FindTransform("Relic")?.gameObject;

        if (priceText == null)
            priceText = FindComponent<TMP_Text>("PriceText");

        if (soldoutRoot == null)
            soldoutRoot = FindTransform("Soldout")?.gameObject;

        if (skillRoot != null)
        {
            if (skillIcon == null) skillIcon = FindComponentUnder<Image>(skillRoot.transform, "Icon");
            if (skillName == null) skillName = FindComponentUnder<TMP_Text>(skillRoot.transform, "Name");
            if (skillRarity == null) skillRarity = FindComponentUnder<TMP_Text>(skillRoot.transform, "Rarity");
            if (skillCostIcon == null) skillCostIcon = FindComponentUnder<Image>(skillRoot.transform, "CostIcon");
            if (skillCostValue == null) skillCostValue = FindComponentUnder<TMP_Text>(skillRoot.transform, "CostValue");
            if (skillDetail == null) skillDetail = FindComponentUnder<TMP_Text>(skillRoot.transform, "Detail");
            if (skillRange == null) skillRange = FindComponentUnder<Image>(skillRoot.transform, "Range");
        }

        if (relicRoot != null)
        {
            if (relicIcon == null) relicIcon = FindComponentUnder<Image>(relicRoot.transform, "Icon");
            if (relicName == null) relicName = FindComponentUnder<TMP_Text>(relicRoot.transform, "Name");
            if (relicRarity == null) relicRarity = FindComponentUnder<TMP_Text>(relicRoot.transform, "Rarity");
            if (relicDetail == null) relicDetail = FindComponentUnder<TMP_Text>(relicRoot.transform, "Detail");
        }
    }

    private TMP_Text FindTextInBranch(string branchName)
    {
        Transform branch = FindTransform(branchName);
        return branch != null ? branch.GetComponent<TMP_Text>() ?? branch.GetComponentInChildren<TMP_Text>(true) : null;
    }

    private Transform FindTransform(string childName)
    {
        return FindChildRecursive(transform, childName);
    }

    private T FindComponent<T>(string childName) where T : Component
    {
        Transform child = FindTransform(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private T FindComponentUnder<T>(Transform root, string childName) where T : Component
    {
        Transform child = FindChildRecursive(root, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }


    private void CacheOriginalVisualColors()
    {
        if (originalColorsCached)
            return;

        originalImageColors.Clear();
        originalTextColors.Clear();

        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image != null)
                originalImageColors[image] = image.color;
        }

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text != null)
                originalTextColors[text] = text.color;
        }

        originalColorsCached = true;
    }

    private void RestoreOriginalVisualColors()
    {
        CacheOriginalVisualColors();

        foreach (KeyValuePair<Image, Color> pair in originalImageColors)
        {
            if (pair.Key != null)
                pair.Key.color = pair.Value;
        }

        foreach (KeyValuePair<TMP_Text, Color> pair in originalTextColors)
        {
            if (pair.Key != null)
                pair.Key.color = pair.Value;
        }
    }

    private void ApplyPurchasedVisualState()
    {
        CacheOriginalVisualColors();

        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || IsExcludedFromSoldoutTint(image.transform))
                continue;

            image.color = new Color(SoldoutGray, SoldoutGray, SoldoutGray, image.color.a);
        }

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || IsExcludedFromSoldoutTint(text.transform))
                continue;

            text.color = new Color(SoldoutGray, SoldoutGray, SoldoutGray, text.color.a);
        }
    }

    private bool IsExcludedFromSoldoutTint(Transform target)
    {
        if (target == null)
            return true;

        // Back, Plate 본체와 Plate_Text는 원래 색을 유지합니다.
        // Plate의 다른 자식 이미지는 회색 처리 대상입니다.
        if (string.Equals(target.name, "Back", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(target.name, "Plate", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(target.name, "Plate_Text", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Soldout과 그 자식은 구매 완료 표시이므로 원래 색을 유지합니다.
        return IsUnderNamedRoot(target, "Soldout");
    }

    private bool IsUnderNamedRoot(Transform target, string rootName)
    {
        Transform current = target;
        while (current != null && current != transform)
        {
            if (string.Equals(current.name, rootName, StringComparison.OrdinalIgnoreCase))
                return true;

            current = current.parent;
        }

        return false;
    }

    private void SetButtonAnimationInteraction(bool enabled)
    {
        ButtonAnimationCoroutine[] animations = GetComponentsInChildren<ButtonAnimationCoroutine>(true);
        for (int i = 0; i < animations.Length; i++)
        {
            ButtonAnimationCoroutine animation = animations[i];
            if (animation != null)
                animation.SetInteractionEnabled(enabled);
        }
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value ?? string.Empty;
    }

    private void SetImage(Image target, Sprite sprite)
    {
        if (target == null)
            return;

        target.sprite = sprite;
        target.enabled = sprite != null;
    }

    private void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
