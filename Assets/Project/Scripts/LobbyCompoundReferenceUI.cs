using System;
using System.Collections.Generic;
using System.Linq;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 CultureTankPanel/reference에서 플레이어가 발견한 재료를 기준으로
/// 관련 연성제 조합법을 생성합니다.
/// 재료 3개 중 하나라도 발견하면 슬롯이 표시되며,
/// 결과 연성제는 실제로 한 번 제작해 발견하기 전까지 ? 상태로 유지됩니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyCompoundReferenceUI : MonoBehaviour
{
    [Header("레시피 목록")]
    [Tooltip("reference/Scroll View/Viewport/Content 입니다. 비워두면 자동으로 찾습니다.")]
    [SerializeField] private RectTransform contentRoot;

    [Tooltip("CompoundRecipeSlot 프리팹입니다.")]
    [SerializeField] private CompoundRecipeSlotUI recipeSlotPrefab;

    [Header("스크롤")]
    [Tooltip("reference/Scroll View의 ScrollRect 입니다. 비워두면 자동으로 찾습니다.")]
    [SerializeField] private ScrollRect scrollRect;

    [Tooltip("레시피 목록은 끝에서 더 넘어가지 않도록 Clamped를 사용합니다.")]
    [SerializeField] private bool forceClampedMovement = true;

    [Header("공용 이름 표시")]
    [Tooltip("reference 아래의 Nameinfo를 제어하는 컴포넌트입니다. 비워두면 자동으로 찾습니다.")]
    [SerializeField] private CompoundReferenceNameTooltip nameTooltip;

    [Header("갱신")]
    [Tooltip("패널이 열린 동안 발견 상태 변화를 반영하는 간격입니다.")]
    [Min(0.05f)]
    [SerializeField] private float refreshInterval = 0.25f;

    private readonly List<CompoundRecipeSlotUI> spawnedSlots = new();
    private string lastStateKey = string.Empty;
    private float nextRefreshAt;

    private void Awake()
    {
        ResolveReferences();
        ConfigureScrollRect();
        HideSceneTemplateIfNeeded();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ConfigureScrollRect();
        RefreshNow(true);
        nextRefreshAt = Time.unscaledTime + refreshInterval;
    }

    private void OnDisable()
    {
        if (nameTooltip != null)
            nameTooltip.Hide();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshAt)
            return;

        nextRefreshAt = Time.unscaledTime + refreshInterval;
        RefreshNow(false);
    }

    /// <summary>
    /// 현재 발견 상태를 즉시 다시 읽어 목록을 갱신합니다.
    /// </summary>
    public void RefreshNow(bool forceRebuild = false)
    {
        ResolveReferences();
        ConfigureScrollRect();

        DataManager dataManager = GetDataManager();
        if (dataManager == null || dataManager.CompoundDatabase == null || contentRoot == null || recipeSlotPrefab == null)
            return;

        RecordDiscoveryService.BackfillFromCurrentState(dataManager);

        List<CompoundData> visibleRecipes = dataManager.CompoundDatabase.GetAll()
            .Where(compound => compound != null)
            .Where(compound => HasAnyDiscoveredMaterial(dataManager, compound))
            .OrderBy(compound => GetTrailingNumber(compound.CompoundId))
            .ThenBy(compound => compound.CompoundId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        string stateKey = BuildStateKey(dataManager, visibleRecipes);
        if (!forceRebuild && string.Equals(lastStateKey, stateKey, StringComparison.Ordinal))
            return;

        lastStateKey = stateKey;
        Rebuild(dataManager, visibleRecipes);
    }

    private void Rebuild(DataManager dataManager, List<CompoundData> visibleRecipes)
    {
        ClearSpawnedSlots();

        if (nameTooltip != null)
            nameTooltip.Hide();

        for (int i = 0; i < visibleRecipes.Count; i++)
        {
            CompoundData compound = visibleRecipes[i];
            CompoundRecipeSlotUI slot = Instantiate(recipeSlotPrefab, contentRoot, false);
            slot.gameObject.SetActive(true);
            slot.Bind(dataManager, compound, nameTooltip);
            spawnedSlots.Add(slot);
        }

        RefreshContentLayout();
    }

    /// <summary>
    /// 동적으로 생성된 슬롯의 Preferred Height를 즉시 Content에 반영합니다.
    /// ScrollRect의 Clamped는 Content Bounds가 Viewport보다 커야 움직일 수 있으므로,
    /// 레이아웃 갱신이 늦어져 Content가 작게 계산되는 문제를 여기서 바로 보정합니다.
    /// </summary>
    private void RefreshContentLayout()
    {
        if (contentRoot == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        float preferredHeight = LayoutUtility.GetPreferredHeight(contentRoot);
        if (preferredHeight > 0f)
            contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
        {
            scrollRect.content = contentRoot;
            scrollRect.StopMovement();
            scrollRect.SetLayoutVertical();
        }
    }

    private void ConfigureScrollRect()
    {
        if (scrollRect == null)
            return;

        if (contentRoot != null)
            scrollRect.content = contentRoot;

        scrollRect.vertical = true;

        if (forceClampedMovement)
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
    }

    private static bool HasAnyDiscoveredMaterial(DataManager dataManager, CompoundData compound)
    {
        if (compound == null)
            return false;

        return IsItemDiscovered(dataManager, compound.MaterialId1)
            || IsItemDiscovered(dataManager, compound.MaterialId2)
            || IsItemDiscovered(dataManager, compound.MaterialId3);
    }

    private static bool IsItemDiscovered(DataManager dataManager, string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId)
            && RecordDiscoveryService.IsItemDiscovered(dataManager, itemId.Trim());
    }

    private static string BuildStateKey(DataManager dataManager, List<CompoundData> recipes)
    {
        var parts = new List<string>(recipes.Count * 5);

        for (int i = 0; i < recipes.Count; i++)
        {
            CompoundData compound = recipes[i];
            parts.Add(compound.CompoundId ?? string.Empty);
            parts.Add(IsItemDiscovered(dataManager, compound.MaterialId1) ? "1" : "0");
            parts.Add(IsItemDiscovered(dataManager, compound.MaterialId2) ? "1" : "0");
            parts.Add(IsItemDiscovered(dataManager, compound.MaterialId3) ? "1" : "0");
            parts.Add(RecordDiscoveryService.IsCompoundDiscovered(dataManager, compound.CompoundId) ? "1" : "0");
        }

        return string.Join("|", parts);
    }

    private void ClearSpawnedSlots()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] != null)
            {
                spawnedSlots[i].gameObject.SetActive(false);
                Destroy(spawnedSlots[i].gameObject);
            }
        }

        spawnedSlots.Clear();
    }

    private void ResolveReferences()
    {
        if (contentRoot == null)
        {
            Transform content = transform.Find("Scroll View/Viewport/Content");
            if (content == null)
                content = FindDeepChild(transform, "Content");

            contentRoot = content as RectTransform;
        }

        if (scrollRect == null)
        {
            Transform scrollView = transform.Find("Scroll View");
            if (scrollView != null)
                scrollRect = scrollView.GetComponent<ScrollRect>();

            if (scrollRect == null && contentRoot != null)
                scrollRect = contentRoot.GetComponentInParent<ScrollRect>();
        }

        if (nameTooltip == null)
        {
            Transform nameInfo = FindDeepChild(transform, "Nameinfo");
            if (nameInfo != null)
            {
                nameTooltip = nameInfo.GetComponent<CompoundReferenceNameTooltip>();
                if (nameTooltip == null)
                    nameTooltip = nameInfo.gameObject.AddComponent<CompoundReferenceNameTooltip>();
            }
        }

        // Content 안에 미리 배치해 둔 CompoundRecipeSlot을 프리팹 템플릿처럼 사용할 수도 있습니다.
        if (recipeSlotPrefab == null && contentRoot != null)
        {
            CompoundRecipeSlotUI[] slots = contentRoot.GetComponentsInChildren<CompoundRecipeSlotUI>(true);
            if (slots.Length > 0)
                recipeSlotPrefab = slots[0];
        }
    }

    private void HideSceneTemplateIfNeeded()
    {
        if (recipeSlotPrefab == null || contentRoot == null)
            return;

        if (recipeSlotPrefab.transform.parent == contentRoot)
            recipeSlotPrefab.gameObject.SetActive(false);
    }

    private static DataManager GetDataManager()
    {
        if (DataManager.Instance != null)
            return DataManager.Instance;

        return FindFirstObjectByType<DataManager>(FindObjectsInactive.Include);
    }

    private static int GetTrailingNumber(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return int.MaxValue;

        int index = id.Length - 1;
        while (index >= 0 && char.IsDigit(id[index]))
            index--;

        string number = id.Substring(index + 1);
        return int.TryParse(number, out int parsed) ? parsed : int.MaxValue;
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (string.Equals(child.name, childName, StringComparison.Ordinal))
                return child;

            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
