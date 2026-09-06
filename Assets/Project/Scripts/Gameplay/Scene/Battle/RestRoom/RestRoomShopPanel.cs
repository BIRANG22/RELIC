using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class RestRoomShopPanel : MonoBehaviour
{
    public event Action Closed;

    [Header("Purchase Confirmation")]
    [SerializeField] private string purchaseConfirmMessage = "구매하시겠습니까?";

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GoodsIconItem[] goodsSlots = new GoodsIconItem[RestRoomShopService.DefaultTotalGoodsCount];

    [Header("Skill Resource Icons")]
    [SerializeField] private Sprite costResourceIcon;
    [SerializeField] private Sprite hpResourceIcon;
    [SerializeField] private Sprite uniqueResourceIcon;
    [SerializeField] private Sprite moveResourceIcon;

    [Header("Open Close")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField, Min(0.01f)] private float openCloseFadeDuration = 0.25f;
    [SerializeField] private bool deactivateOnClose;

    [Header("Stock Rarity Weight")]
    [SerializeField] private float commonRarityWeight = RestRoomShopService.DefaultCommonWeight;
    [SerializeField] private float rareRarityWeight = RestRoomShopService.DefaultRareWeight;
    [SerializeField] private float epicRarityWeight = RestRoomShopService.DefaultEpicWeight;
    [SerializeField] private float uniqueRarityWeight = RestRoomShopService.DefaultUniqueWeight;

    private readonly ISkillRewardRandom random = new UnitySkillRewardRandom();
    private readonly List<RestRoomShopGoods> currentStock = new();
    private Coroutine panelFadeRoutine;

    private void Awake()
    {
        EnsureBindings();
    }

    private void OnEnable()
    {
        EnsureBindings();
        StopPanelFade();
        SetPanelCanvasState(0f, false);
        ClearGoodsSlots();
    }

    private void OnDisable()
    {
        StopPanelFade();
        ClearGoodsSlots();
    }

    public void Open()
    {
        EnsureBindings();
        SetPanelRootActive(true);
        Refresh();

        StopPanelFade();
        SetPanelCanvasState(0f, false);
        panelFadeRoutine = StartCoroutine(FadePanel(1f, true, false));
    }

    public void Close()
    {
        EnsureBindings();
        StopPanelFade();
        SetPanelCanvasInteraction(false);
        panelFadeRoutine = StartCoroutine(FadePanel(0f, false, true));
    }

    public void Refresh()
    {
        EnsureBindings();
        ClearGoodsSlots();
        ResolveGoodsSlots();


        if (DataManager.Instance == null ||
            DataManager.Instance.SkillDatabase == null ||
            DataManager.Instance.RelicDatabase == null)
        {
            return;
        }

        List<RestRoomShopGoods> stock = RestRoomShopService.CreateStock(
            DataManager.Instance.SkillDatabase.GetAll(),
            DataManager.Instance.RelicDatabase.GetAll(),
            GetUnavailableSkillIds(),
            GetUnavailableRelicIds(),
            random,
            commonRarityWeight,
            rareRarityWeight,
            epicRarityWeight,
            uniqueRarityWeight);

        currentStock.Clear();
        currentStock.AddRange(stock);
        BindStock(currentStock);
    }

    public List<ResumeShopGoodsSaveData> CaptureResumeStock()
    {
        var saved = new List<ResumeShopGoodsSaveData>();
        for (int i = 0; i < currentStock.Count; i++)
        {
            RestRoomShopGoods goods = currentStock[i];
            if (goods != null)
                saved.Add(new ResumeShopGoodsSaveData { Kind = goods.Kind, Id = goods.Id, Price = goods.Price });
        }
        return saved;
    }

    public void OpenSavedStock(IReadOnlyList<ResumeShopGoodsSaveData> savedStock)
    {
        EnsureBindings();
        currentStock.Clear();
        if (savedStock != null)
        {
            for (int i = 0; i < savedStock.Count; i++)
            {
                ResumeShopGoodsSaveData saved = savedStock[i];
                if (saved == null || string.IsNullOrWhiteSpace(saved.Id)) continue;
                if (saved.Kind == RestRoomShopGoodsKind.Skill && DataManager.Instance.SkillDatabase.TryGet(saved.Id, out SkillMasterData skill))
                    currentStock.Add(new RestRoomShopGoods(saved.Kind, saved.Id, GameDataLocalization.SkillName(skill), GameDataLocalization.SkillDetails(skill), saved.Price, skill.Rarity, skill));
                else if (saved.Kind == RestRoomShopGoodsKind.Relic && DataManager.Instance.RelicDatabase.TryGet(saved.Id, out RelicData relic))
                    currentStock.Add(new RestRoomShopGoods(saved.Kind, saved.Id, GameDataLocalization.RelicName(relic), GameDataLocalization.RelicEffectDescription(relic), saved.Price, relic: relic));
            }
        }
        SetPanelRootActive(true);
        BindStock(currentStock);
        StopPanelFade();
        SetPanelCanvasState(1f, true);
    }

    private void BindStock(IReadOnlyList<RestRoomShopGoods> stock)
    {
        int bindCount = Mathf.Min(stock.Count, goodsSlots.Length);
        for (int i = 0; i < bindCount; i++)
            BindGoodsSlot(goodsSlots[i], stock[i]);
    }

    private void BindGoodsSlot(GoodsIconItem item, RestRoomShopGoods goods)
    {
        if (item == null || goods == null)
            return;

        goods.Icon = ResolveIcon(goods);
        item.ConfigureResourceIcons(costResourceIcon, hpResourceIcon, uniqueResourceIcon, moveResourceIcon);
        item.Initialize(goods, OnGoodsClicked);
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

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[RestRoomShopPanel] CHECK 프리팹을 표시할 UIManager를 찾을 수 없습니다.", this);
            return;
        }

        if (UIManager.Instance.IsConfirmDialogOpen)
            return;

        UIManager.Instance.ShowConfirmDialog(
            purchaseConfirmMessage,
            () =>
            {
                UIManager.Instance?.HideConfirmDialog();
                ConfirmGoodsPurchase(item, goods);
            },
            () => UIManager.Instance?.HideConfirmDialog());
    }

    private void ConfirmGoodsPurchase(GoodsIconItem item, RestRoomShopGoods goods)
    {
        if (item == null || goods == null)
            return;

        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        if (DataManager.Instance == null || DataManager.Instance.BattleRuntimeStore == null)
            return;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        // 확인창이 열린 사이에 재화/보유 상태가 바뀔 수 있으므로 구매 직전에 다시 검사합니다.
        if (runtime.Remnant < goods.Price)
        {
            ShowWarning("\uC7AC\uD654\uAC00 \uBD80\uC871\uD569\uB2C8\uB2E4.");
            return;
        }

        if (!CanPurchase(runtime, goods))
            return;

        if (!TryOpenEquipPanel(goods))
        {
            ShowWarning("장착 패널을 열 수 없습니다.");
            return;
        }

        runtime.Remnant -= goods.Price;
        DataManager.Instance.BattleRuntimeStore.Set(runtime);
        BattleGoldHudUI.RefreshAll();
        item.MarkPurchased();
    }

    private bool TryOpenEquipPanel(RestRoomShopGoods goods)
    {
        if (goods == null || string.IsNullOrWhiteSpace(goods.Id))
            return false;

        return goods.Kind switch
        {
            RestRoomShopGoodsKind.Skill => BattleRewardEquipPanelUI.TryOpenSkillReward(goods.Id),
            RestRoomShopGoodsKind.Relic => BattleRewardEquipPanelUI.TryOpenRelicReward(goods.Id),
            _ => false
        };
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

    private HashSet<string> GetUnavailableSkillIds()
    {
        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);

        if (DataManager.Instance == null)
            return ids;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore?.GetOrCreate();

        if (runtime?.SkillInventoryIds != null)
        {
            for (int i = 0; i < runtime.SkillInventoryIds.Count; i++)
                AddSkillAndPairedVariant(ids, runtime.SkillInventoryIds[i]);
        }

        IReadOnlyDictionary<string, CharacterRuntimeData> characters =
            DataManager.Instance.CharacterRuntimeStore?.GetAll();

        if (characters == null)
            return ids;

        foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters)
        {
            CharacterRuntimeData character = pair.Value;

            if (character?.EquippedSkillIds == null)
                continue;

            for (int i = 0; i < character.EquippedSkillIds.Length; i++)
                AddSkillAndPairedVariant(ids, character.EquippedSkillIds[i]);
        }

        return ids;
    }

    private void AddSkillAndPairedVariant(HashSet<string> ids, string skillId)
    {
        if (ids == null || string.IsNullOrWhiteSpace(skillId))
            return;

        string normalizedId = skillId.Trim();
        ids.Add(normalizedId);

        if (SkillRarityUtility.TryGetPairedVariantId(normalizedId, out string pairedSkillId) &&
            !string.IsNullOrWhiteSpace(pairedSkillId))
        {
            ids.Add(pairedSkillId.Trim());
        }
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

    private void ClearGoodsSlots()
    {
        ResolveGoodsSlots();

        for (int i = 0; i < goodsSlots.Length; i++)
        {
            GoodsIconItem slot = goodsSlots[i];
            if (slot == null)
                continue;

            slot.Clear();
            slot.gameObject.SetActive(false);
        }
    }

    private void EnsureBindings()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (panelCanvasGroup == null)
            panelCanvasGroup = ResolvePanelCanvasGroup();

        if (contentRoot == null)
        {
            Transform foundContent = FindChildRecursive(transform, "Content");
            contentRoot = foundContent != null ? foundContent : transform;
        }

        ResolveGoodsSlots();
    }

    private void ResolveGoodsSlots()
    {
        if (goodsSlots == null || goodsSlots.Length != RestRoomShopService.DefaultTotalGoodsCount)
            goodsSlots = new GoodsIconItem[RestRoomShopService.DefaultTotalGoodsCount];

        if (contentRoot == null)
            return;

        for (int i = 0; i < goodsSlots.Length; i++)
        {
            if (goodsSlots[i] != null)
                continue;

            string objectName = $"Goods{i + 1:00}";
            Transform child = FindChildRecursive(contentRoot, objectName);
            if (child == null)
                continue;

            GoodsIconItem item = child.GetComponent<GoodsIconItem>();
            if (item == null)
                item = child.gameObject.AddComponent<GoodsIconItem>();

            goodsSlots[i] = item;
        }
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

    private void SetPanelRootActive(bool active)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(active);
            return;
        }

        gameObject.SetActive(active);
    }

    private IEnumerator FadePanel(float targetAlpha, bool interactableAtEnd, bool notifyClosed)
    {
        EnsureBindings();

        if (panelCanvasGroup == null)
        {
            if (notifyClosed)
            {
                ClearGoodsSlots();
                if (deactivateOnClose)
                    SetPanelRootActive(false);
                Closed?.Invoke();
            }

            panelFadeRoutine = null;
            yield break;
        }

        float startAlpha = panelCanvasGroup.alpha;
        float duration = Mathf.Max(0.01f, openCloseFadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        SetPanelCanvasState(targetAlpha, interactableAtEnd);

        if (notifyClosed)
        {
            ClearGoodsSlots();
            if (deactivateOnClose)
                SetPanelRootActive(false);
            Closed?.Invoke();
        }

        panelFadeRoutine = null;
    }

    private void StopPanelFade()
    {
        if (panelFadeRoutine == null)
            return;

        StopCoroutine(panelFadeRoutine);
        panelFadeRoutine = null;
    }

    private void SetPanelCanvasState(float alpha, bool interactable)
    {
        if (panelCanvasGroup == null)
            panelCanvasGroup = ResolvePanelCanvasGroup();

        if (panelCanvasGroup == null)
            return;

        panelCanvasGroup.alpha = Mathf.Clamp01(alpha);
        panelCanvasGroup.interactable = interactable;
        panelCanvasGroup.blocksRaycasts = interactable;
    }

    private void SetPanelCanvasInteraction(bool interactable)
    {
        if (panelCanvasGroup == null)
            panelCanvasGroup = ResolvePanelCanvasGroup();

        if (panelCanvasGroup == null)
            return;

        panelCanvasGroup.interactable = interactable;
        panelCanvasGroup.blocksRaycasts = interactable;
    }

    private CanvasGroup ResolvePanelCanvasGroup()
    {
        GameObject target = panelRoot != null ? panelRoot : gameObject;
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.AddComponent<CanvasGroup>();
    }
}
