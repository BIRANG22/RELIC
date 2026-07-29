using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RestRoomShopPanel : MonoBehaviour
{
    private const string DefaultGoodsPrefabAssetPath = "Assets/Project/PrefabsR/RestRoom/Goods.prefab";

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GoodsIconItem goodsPrefab;

    [Header("Stock")]
    [SerializeField] private int totalGoodsCount = RestRoomShopService.DefaultTotalGoodsCount;
    [SerializeField] private int skillGoodsCount = RestRoomShopService.DefaultSkillGoodsCount;
    [SerializeField] private float commonSkillWeight = RestRoomShopService.DefaultCommonWeight;
    [SerializeField] private float rareSkillWeight = RestRoomShopService.DefaultRareWeight;
    [SerializeField] private float epicSkillWeight = RestRoomShopService.DefaultEpicWeight;

    [Header("Layout")]
    [SerializeField] private int columnCount = RestRoomShopService.DefaultColumnCount;
    [SerializeField] private Vector2 firstCellAnchoredPosition = new(-330f, 100f);
    [SerializeField] private Vector2 cellSpacing = new(220f, 300f);
    [SerializeField] private Vector2 fallbackItemSize = new(130f, 150f);

    private readonly List<GoodsIconItem> spawnedItems = new();
    private readonly ISkillRewardRandom random = new UnitySkillRewardRandom();

    private void Awake()
    {
        EnsureBindings();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDisable()
    {
        ClearSpawnedItems();
    }

    public void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        ClearSpawnedItems();

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void Refresh()
    {
        EnsureBindings();
        ClearSpawnedItems();

        if (goodsPrefab == null)
        {
            Debug.LogWarning("[RestRoomShopPanel] GoodsIconItem prefab/template not found.");
            return;
        }

        if (contentRoot == null)
            contentRoot = transform;

        goodsPrefab.gameObject.SetActive(false);

        if (DataManager.Instance == null ||
            DataManager.Instance.SkillDatabase == null ||
            DataManager.Instance.RelicDatabase == null)
        {
            return;
        }

        List<RestRoomShopGoods> stock = RestRoomShopService.CreateStock(
            DataManager.Instance.SkillDatabase.GetAll(),
            DataManager.Instance.RelicDatabase.GetAll(),
            GetUnavailableRelicIds(),
            random,
            totalGoodsCount,
            skillGoodsCount,
            commonSkillWeight,
            rareSkillWeight,
            epicSkillWeight);

        for (int i = 0; i < stock.Count; i++)
            SpawnGoods(stock[i], i);
    }

    private void SpawnGoods(RestRoomShopGoods goods, int index)
    {
        if (goods == null || goodsPrefab == null || contentRoot == null)
            return;

        goods.Icon = ResolveIcon(goods);

        GoodsIconItem item = Instantiate(goodsPrefab, contentRoot);
        item.gameObject.SetActive(true);

        PrepareSpawnedItemLayout(item, index);
        item.Initialize(goods, OnGoodsClicked);

        spawnedItems.Add(item);
    }

    private void OnGoodsClicked(GoodsIconItem item, RestRoomShopGoods goods)
    {
        if (item == null || goods == null)
            return;

        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        if (DataManager.Instance == null || DataManager.Instance.BattleRuntimeStore == null)
            return;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        if (runtime.Remnant < goods.Price)
        {
            ShowWarning("\uC7AC\uD654\uAC00 \uBD80\uC871\uD569\uB2C8\uB2E4.");
            return;
        }

        if (!CanPurchase(runtime, goods))
            return;

        if (!GrantGoods(runtime, goods))
            return;

        runtime.Remnant -= goods.Price;

        DataManager.Instance.BattleRuntimeStore.Set(runtime);
        BattleGoldHudUI.RefreshAll();
        item.MarkPurchased();
    }

    private bool CanPurchase(BattleRuntimeData runtime, RestRoomShopGoods goods)
    {
        if (goods.Kind == RestRoomShopGoodsKind.Relic && HasRelicAnywhere(goods.Id))
        {
            ShowWarning("\uC774\uBBF8 \uBCF4\uC720 \uC911\uC778 \uC720\uBB3C\uC785\uB2C8\uB2E4.");
            return false;
        }

        if (goods.Kind == RestRoomShopGoodsKind.Skill && HasSkillAnywhere(runtime, goods.Id))
        {
            ShowWarning("\uC774\uBBF8 \uBCF4\uC720 \uC911\uC778 \uC2A4\uD0AC\uC785\uB2C8\uB2E4.");
            return false;
        }

        return true;
    }

    private bool GrantGoods(BattleRuntimeData runtime, RestRoomShopGoods goods)
    {
        if (runtime == null || goods == null || string.IsNullOrWhiteSpace(goods.Id))
            return false;

        switch (goods.Kind)
        {
            case RestRoomShopGoodsKind.Skill:
                runtime.SkillInventoryIds ??= new List<string>();
                runtime.SkillInventoryIds.Add(goods.Id.Trim());
                SkillInventoryNotificationUI.ShowNewSkillNotice();
                SkillInventoryPanelUI.RefreshAll();
                return true;

            case RestRoomShopGoodsKind.Relic:
                runtime.OwnedRelicIds ??= new List<string>();
                runtime.OwnedRelicIds.Add(goods.Id.Trim());
                NormalizeOwnedRelics(runtime);
                RelicEquipPanelUI.RefreshAll();
                return true;

            default:
                return false;
        }
    }

    private void PrepareSpawnedItemLayout(GoodsIconItem item, int index)
    {
        if (item == null)
            return;

        Vector2 itemSize = ResolveItemSize();
        RectTransform rectTransform = item.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = itemSize;
            rectTransform.anchoredPosition = CalculateAnchoredPosition(index);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        LayoutElement layoutElement = item.GetComponent<LayoutElement>();

        if (layoutElement == null)
            layoutElement = item.gameObject.AddComponent<LayoutElement>();

        layoutElement.ignoreLayout = true;
        layoutElement.minWidth = itemSize.x;
        layoutElement.minHeight = itemSize.y;
        layoutElement.preferredWidth = itemSize.x;
        layoutElement.preferredHeight = itemSize.y;

        item.ApplyDefaultLayout(itemSize);
    }

    private Vector2 CalculateAnchoredPosition(int index)
    {
        int columns = Mathf.Max(1, columnCount);
        int row = index / columns;
        int column = index % columns;

        return new Vector2(
            firstCellAnchoredPosition.x + (cellSpacing.x * column),
            firstCellAnchoredPosition.y - (cellSpacing.y * row));
    }

    private Vector2 ResolveItemSize()
    {
        Vector2 size = fallbackItemSize;

        if (goodsPrefab == null)
            return ClampItemSize(size);

        RectTransform prefabRect = goodsPrefab.GetComponent<RectTransform>();

        if (prefabRect == null)
            return ClampItemSize(size);

        Vector2 rectSize = prefabRect.rect.size;

        if (rectSize.x >= 20f && rectSize.y >= 20f)
            return ClampItemSize(rectSize);

        if (prefabRect.sizeDelta.x >= 20f && prefabRect.sizeDelta.y >= 20f)
            return ClampItemSize(prefabRect.sizeDelta);

        return ClampItemSize(size);
    }

    private Vector2 ClampItemSize(Vector2 size)
    {
        return new Vector2(
            Mathf.Max(1f, size.x),
            Mathf.Max(1f, size.y));
    }

    private Sprite ResolveIcon(RestRoomShopGoods goods)
    {
        if (goods == null || DataManager.Instance == null)
            return null;

        if (goods.Kind == RestRoomShopGoodsKind.Skill)
        {
            if (goods.Skill?.Icon != null)
                return goods.Skill.Icon;

            if (DataManager.Instance.SkillIconDatabase != null &&
                DataManager.Instance.SkillIconDatabase.TryGetIcon(goods.Id, out Sprite skillIcon))
            {
                return skillIcon;
            }
        }

        if (goods.Kind == RestRoomShopGoodsKind.Relic &&
            DataManager.Instance.RelicIconDatabase != null &&
            DataManager.Instance.RelicIconDatabase.TryGetIcon(goods.Id, out Sprite relicIcon))
        {
            return relicIcon;
        }

        return null;
    }

    private HashSet<string> GetUnavailableRelicIds()
    {
        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);

        if (DataManager.Instance == null)
            return ids;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore?.GetOrCreate();

        if (runtime?.OwnedRelicIds != null)
        {
            for (int i = 0; i < runtime.OwnedRelicIds.Count; i++)
                AddId(ids, runtime.OwnedRelicIds[i]);
        }

        IReadOnlyDictionary<string, CharacterRuntimeData> characters =
            DataManager.Instance.CharacterRuntimeStore?.GetAll();

        if (characters == null)
            return ids;

        foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters)
        {
            CharacterRuntimeData character = pair.Value;

            if (character?.EquippedRelicIds == null)
                continue;

            for (int i = 0; i < character.EquippedRelicIds.Length; i++)
                AddId(ids, character.EquippedRelicIds[i]);
        }

        return ids;
    }

    private bool HasRelicAnywhere(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return false;

        return GetUnavailableRelicIds().Contains(relicId.Trim());
    }

    private bool HasSkillAnywhere(BattleRuntimeData runtime, string skillId)
    {
        if (runtime == null || string.IsNullOrWhiteSpace(skillId))
            return false;

        string targetId = skillId.Trim();

        if (runtime.SkillInventoryIds != null)
        {
            for (int i = 0; i < runtime.SkillInventoryIds.Count; i++)
            {
                if (IsSameSkillOrPairedVariant(runtime.SkillInventoryIds[i], targetId))
                    return true;
            }
        }

        IReadOnlyDictionary<string, CharacterRuntimeData> characters =
            DataManager.Instance?.CharacterRuntimeStore?.GetAll();

        if (characters == null)
            return false;

        foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters)
        {
            CharacterRuntimeData character = pair.Value;

            if (character?.EquippedSkillIds == null)
                continue;

            for (int i = 0; i < character.EquippedSkillIds.Length; i++)
            {
                if (IsSameSkillOrPairedVariant(character.EquippedSkillIds[i], targetId))
                    return true;
            }
        }

        return false;
    }

    private bool IsSameSkillOrPairedVariant(string ownedSkillId, string targetSkillId)
    {
        if (string.IsNullOrWhiteSpace(ownedSkillId) || string.IsNullOrWhiteSpace(targetSkillId))
            return false;

        string normalizedOwnedSkillId = ownedSkillId.Trim();
        string normalizedTargetSkillId = targetSkillId.Trim();

        if (string.Equals(normalizedOwnedSkillId, normalizedTargetSkillId, StringComparison.Ordinal))
            return true;

        return SkillRarityUtility.TryGetPairedVariantId(normalizedOwnedSkillId, out string pairedSkillId) &&
               string.Equals(pairedSkillId, normalizedTargetSkillId, StringComparison.Ordinal);
    }

    private void NormalizeOwnedRelics(BattleRuntimeData runtime)
    {
        if (runtime == null)
            return;

        runtime.OwnedRelicIds ??= new List<string>();
        HashSet<string> uniqueIds = new(StringComparer.OrdinalIgnoreCase);

        for (int i = runtime.OwnedRelicIds.Count - 1; i >= 0; i--)
        {
            string relicId = runtime.OwnedRelicIds[i];

            if (string.IsNullOrWhiteSpace(relicId))
            {
                runtime.OwnedRelicIds.RemoveAt(i);
                continue;
            }

            relicId = relicId.Trim();

            if (!uniqueIds.Add(relicId))
            {
                runtime.OwnedRelicIds.RemoveAt(i);
                continue;
            }

            runtime.OwnedRelicIds[i] = relicId;
        }
    }

    private void AddId(HashSet<string> ids, string id)
    {
        if (ids == null || string.IsNullOrWhiteSpace(id))
            return;

        ids.Add(id.Trim());
    }

    private void ShowWarning(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        BattleWarningUI.ShowMessage(message);
    }

    private void ClearSpawnedItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();
    }

    private void EnsureBindings()
    {
        bool inferContentRootFromTemplate = contentRoot == null;

        if (panelRoot == null)
            panelRoot = gameObject;

        if (goodsPrefab == null)
            goodsPrefab = LoadDefaultGoodsPrefabAsset();

        if (goodsPrefab == null)
            goodsPrefab = GetComponentInChildren<GoodsIconItem>(true);

        if (goodsPrefab == null)
            goodsPrefab = FindSceneGoodsTemplate();

        if (inferContentRootFromTemplate &&
            goodsPrefab != null &&
            goodsPrefab.transform.parent != null &&
            goodsPrefab.transform.IsChildOf(transform))
        {
            contentRoot = goodsPrefab.transform.parent;
        }

        if (contentRoot == null)
            contentRoot = transform;

        if (goodsPrefab == null)
            goodsPrefab = CreateFallbackGoodsTemplate();
    }

    private GoodsIconItem LoadDefaultGoodsPrefabAsset()
    {
#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultGoodsPrefabAssetPath);

        if (prefab == null)
            return null;

        GoodsIconItem item = prefab.GetComponent<GoodsIconItem>();

        if (item != null)
            return item;

        return prefab.GetComponentInChildren<GoodsIconItem>(true);
#else
        return null;
#endif
    }

    private GoodsIconItem FindSceneGoodsTemplate()
    {
        GoodsIconItem[] templates = Resources.FindObjectsOfTypeAll<GoodsIconItem>();

        for (int i = 0; i < templates.Length; i++)
        {
            GoodsIconItem template = templates[i];

            if (template == null || !IsSceneObject(template.gameObject))
                continue;

            return template;
        }

        return null;
    }

    private bool IsSceneObject(GameObject target)
    {
        return target != null &&
               target.scene.IsValid() &&
               target.scene.isLoaded;
    }

    private GoodsIconItem CreateFallbackGoodsTemplate()
    {
        if (contentRoot == null)
            contentRoot = transform;

        GameObject itemObject = new(
            "GoodsIconItem_Template",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(GoodsIconItem));

        itemObject.transform.SetParent(contentRoot, false);

        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        itemRect.sizeDelta = fallbackItemSize;

        Image background = itemObject.GetComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.08f);

        Button button = itemObject.GetComponent<Button>();
        button.targetGraphic = background;

        GameObject iconObject = new(
            "IconImage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        iconObject.transform.SetParent(itemObject.transform, false);

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.preserveAspect = true;

        GameObject priceObject = new(
            "price",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        priceObject.transform.SetParent(itemObject.transform, false);

        TextMeshProUGUI priceText = priceObject.GetComponent<TextMeshProUGUI>();
        priceText.alignment = TextAlignmentOptions.Center;
        priceText.fontSize = 28f;
        priceText.color = Color.white;
        priceText.raycastTarget = false;

        GoodsIconItem item = itemObject.GetComponent<GoodsIconItem>();
        item.ApplyDefaultLayout(fallbackItemSize);
        itemObject.SetActive(false);
        return item;
    }
}
