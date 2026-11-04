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
    [SerializeField] private Image goodsIcon;
    [SerializeField] private TMP_Text goodsName;
    [SerializeField] private TMP_Text goodsDetail;
    [SerializeField] private GameObject discountIcon;
    [SerializeField] private TMP_Text discountText;
    [SerializeField] private Color discountedOriginalPriceColor = new Color32(0x77, 0x77, 0x77, 0xFF);

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
