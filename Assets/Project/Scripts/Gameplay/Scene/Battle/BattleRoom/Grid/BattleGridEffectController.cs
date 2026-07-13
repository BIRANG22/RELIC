using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleGridEffectController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform effectRoot;

    [Header("View")]
    [SerializeField] private Vector3 worldOffset = new(0f, 0.05f, 0f);
    [SerializeField] private float viewScale = 1.0f;
    [SerializeField] private bool overrideSortingLayer;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 1;

    [Header("World VFX Proxy")]
    [SerializeField] private bool playWorldVfxProxies = true;
    [SerializeField] private string worldVfxLayerName = "VFX";
    [SerializeField] private string worldVfxSortingLayerName = "Unit";
    [SerializeField] private int worldVfxSortingOrderOffset;
    [SerializeField] private float worldVfxYMultiplier = 100f;
    [SerializeField] private float worldVfxLifeTime = 9999f;

    private readonly BattleGridEffectState state = new();
    private readonly Dictionary<int, GameObject> viewsByGridIndex = new();
    private BattleGridEffectService service;
    private GridEffectSpriteDatabase prefabDatabase;

    public BattleGridEffectState State => state;

    public void SpawnInitialEffects(GridManager providedGridManager = null)
    {
        if (!EnsureDependencies(providedGridManager))
            return;

        ClearAll();

        IReadOnlyList<BattleGridEffectPlacement> placements =
            service.SpawnRandomEffects(
                state,
                gridManager.Width,
                gridManager.Height,
                CollectOccupiedGridIndices()
            );

        for (int i = 0; i < placements.Count; i++)
            SpawnView(placements[i]);
    }

    public void ClearAll()
    {
        state.Clear();
        ClearViews();
    }

    public bool IsBlocked(int gridIndex)
    {
        if (!EnsureDependencies())
            return false;

        return service.IsBlocked(state, gridIndex);
    }

    public bool HasEffect(int gridIndex)
    {
        return state.TryGetEffectId(gridIndex, out _);
    }

    public bool TryPlaceEffect(int gridIndex, string gridEffectId)
    {
        if (!EnsureDependencies())
            return false;

        if (gridManager.GetCellByIndex(gridIndex) == null)
            return false;

        if (DataManager.Instance.GridEffectDatabase == null ||
            !DataManager.Instance.GridEffectDatabase.TryGet(gridEffectId, out _))
        {
            return false;
        }

        if (!state.Place(gridIndex, gridEffectId))
            return false;

        SpawnView(new BattleGridEffectPlacement(gridIndex, gridEffectId));
        return true;
    }

    public BattleGridEffectApplyResult ApplyToPlayer(int gridIndex, BattleCharacter character)
    {
        if (character == null || character.RuntimeData == null)
            return BattleGridEffectApplyResult.None;

        if (!EnsureDependencies())
            return BattleGridEffectApplyResult.None;

        int hpBefore = character.RuntimeData.CurrentHP;
        BattleGridEffectApplyResult result =
            service.ApplyToPlayer(state, gridIndex, character.RuntimeData);

        PresentAppliedEffects(character, result, character.RuntimeData.CurrentHP > hpBefore);
        RemoveViewIfConsumed(result);
        return result;
    }

    public BattleGridEffectApplyResult ApplyToMonster(int gridIndex, MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return BattleGridEffectApplyResult.None;

        if (!EnsureDependencies())
            return BattleGridEffectApplyResult.None;

        int hpBefore = monster.RuntimeData.CurrentHP;
        BattleGridEffectApplyResult result =
            service.ApplyToMonster(state, gridIndex, monster.RuntimeData);

        PresentAppliedEffects(monster, result, monster.RuntimeData.CurrentHP > hpBefore);
        RemoveViewIfConsumed(result);

        if (result.Applied)
            monster.ShowAndRefreshHUD();

        return result;
    }

    private bool EnsureDependencies(GridManager providedGridManager = null)
    {
        if (gridManager == null)
            gridManager = providedGridManager;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>(FindObjectsInactive.Include);

        if (DataManager.Instance == null || DataManager.Instance.GridEffectDatabase == null)
            return false;

        service ??= new BattleGridEffectService(DataManager.Instance.GridEffectDatabase);
        prefabDatabase = DataManager.Instance.GridEffectSpriteDatabase;

        if (effectRoot == null)
            effectRoot = GetOrCreateEffectRoot();

        return gridManager != null;
    }

    private Transform GetOrCreateEffectRoot()
    {
        Transform existing = transform.Find("GridEffectRoot");

        if (existing != null)
            return existing;

        GameObject root = new("GridEffectRoot");
        root.transform.SetParent(transform, false);
        return root.transform;
    }

    private HashSet<int> CollectOccupiedGridIndices()
    {
        HashSet<int> occupied = new();

        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
                continue;

            if (character.CurrentGridIndex >= 0)
                occupied.Add(character.CurrentGridIndex);
        }

        MonsterUnit[] monsters = FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                continue;

            for (int cellIndex = 0; cellIndex < monster.OccupiedGridIndices.Count; cellIndex++)
            {
                int gridIndex = monster.OccupiedGridIndices[cellIndex];

                if (gridIndex >= 0)
                    occupied.Add(gridIndex);
            }
        }

        return occupied;
    }

    private void SpawnView(BattleGridEffectPlacement placement)
    {
        if (gridManager == null || prefabDatabase == null)
            return;

        if (!prefabDatabase.TryGetPrefab(placement.GridEffectId, out GameObject prefab) || prefab == null)
            return;

        RemoveView(placement.GridIndex);

        Transform parent = effectRoot != null ? effectRoot : transform;
        GameObject view = Instantiate(prefab, parent);
        view.name = $"GridEffect_{placement.GridEffectId}_{placement.GridIndex}";
        view.transform.position = gridManager.GetWorldPositionByIndex(placement.GridIndex) + worldOffset;
        view.transform.localRotation = prefab.transform.localRotation;
        view.transform.localScale = prefab.transform.localScale * Mathf.Max(0.01f, viewScale);

        ApplyRendererSorting(view);
        view.SetActive(true);
        PlayWorldVfxProxies(view, view.transform.position);

        viewsByGridIndex[placement.GridIndex] = view;
    }

    private void ApplyRendererSorting(GameObject view)
    {
        if (view == null)
            return;

        Renderer[] renderers = view.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            int prefabSortingOrder = renderer.sortingOrder;

            if (overrideSortingLayer && !string.IsNullOrWhiteSpace(sortingLayerName))
                renderer.sortingLayerName = sortingLayerName;

            renderer.sortingOrder = sortingOrder + prefabSortingOrder;
        }
    }

    private void PlayWorldVfxProxies(GameObject view, Vector3 worldPosition)
    {
        if (!playWorldVfxProxies || view == null)
            return;

        GridEffectWorldVfxPresenter[] presenters =
            view.GetComponentsInChildren<GridEffectWorldVfxPresenter>(true);

        if (presenters.Length == 0)
            return;

        int renderLayer = LayerMask.NameToLayer(worldVfxLayerName);

        if (renderLayer < 0)
        {
            Debug.LogWarning($"[BattleGridEffectController] Missing VFX layer: {worldVfxLayerName}");
            return;
        }

        GridEffectWorldVfxSpawnContext context = new(
            worldPosition,
            renderLayer,
            ResolveWorldVfxVisibleLayer(view, renderLayer),
            worldVfxSortingLayerName,
            worldVfxSortingOrderOffset,
            worldVfxYMultiplier,
            worldVfxLifeTime);

        for (int i = 0; i < presenters.Length; i++)
            presenters[i].Play(context);
    }

    private static int ResolveWorldVfxVisibleLayer(GameObject view, int renderLayer)
    {
        if (view == null)
            return 0;

        return view.layer == renderLayer ? 0 : view.layer;
    }

    private void PresentAppliedEffects(
        Component target,
        BattleGridEffectApplyResult result,
        bool hpRecovered)
    {
        if (target == null || result == null || !result.Applied)
            return;

        BattleUnitAnimator animator = ResolveUnitAnimator(target);

        if (animator == null)
            return;

        if (HasDamageEffect(result.AppliedEffectIds))
        {
            if (IsDeadTarget(target))
                animator.PlayDead();
            else
                animator.PlayHit();
        }

        if (hpRecovered && HasHealEffect(result.AppliedEffectIds))
            animator.PlayHeal();

        for (int i = 0; i < result.AppliedEffectIds.Count; i++)
        {
            string effectId = result.AppliedEffectIds[i];

            if (IsDamageEffect(effectId) || IsHealEffect(effectId))
                continue;

            animator.PlayStatusVfx(effectId);
        }
    }

    private static BattleUnitAnimator ResolveUnitAnimator(Component target)
    {
        if (target == null)
            return null;

        BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

        if (animator != null)
            return animator;

        animator = target.GetComponentInChildren<BattleUnitAnimator>(true);

        if (animator != null)
            return animator;

        return target.GetComponentInParent<BattleUnitAnimator>();
    }

    private static bool IsDeadTarget(Component target)
    {
        if (target is BattleCharacter character)
            return character.RuntimeData != null && character.RuntimeData.IsDead;

        if (target is MonsterUnit monster)
            return monster.RuntimeData != null && monster.RuntimeData.IsDead;

        return false;
    }

    private static bool HasDamageEffect(IReadOnlyList<string> effectIds)
    {
        if (effectIds == null)
            return false;

        for (int i = 0; i < effectIds.Count; i++)
        {
            if (IsDamageEffect(effectIds[i]))
                return true;
        }

        return false;
    }

    private static bool HasHealEffect(IReadOnlyList<string> effectIds)
    {
        if (effectIds == null)
            return false;

        for (int i = 0; i < effectIds.Count; i++)
        {
            if (IsHealEffect(effectIds[i]))
                return true;
        }

        return false;
    }

    private static bool IsDamageEffect(string effectId)
    {
        if (string.IsNullOrWhiteSpace(effectId))
            return false;

        string normalized = effectId.Trim();
        return normalized == "E_Damage" ||
               normalized == "E_Strike" ||
               normalized == "E_Pierce";
    }

    private static bool IsHealEffect(string effectId)
    {
        if (string.IsNullOrWhiteSpace(effectId))
            return false;

        string normalized = effectId.Trim();
        return normalized == "E_Focus" ||
               normalized.IndexOf("Heal", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               normalized.IndexOf("Recover", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void RemoveViewIfConsumed(BattleGridEffectApplyResult result)
    {
        if (result == null || !result.Consumed)
            return;

        RemoveView(result.GridIndex);
    }

    private void RemoveView(int gridIndex)
    {
        if (!viewsByGridIndex.TryGetValue(gridIndex, out GameObject view))
            return;

        viewsByGridIndex.Remove(gridIndex);

        if (view == null)
            return;

        CleanupWorldVfxProxies(view);
        view.SetActive(false);

        if (Application.isPlaying)
            Destroy(view);
        else
            DestroyImmediate(view);
    }

    private static void CleanupWorldVfxProxies(GameObject view)
    {
        if (view == null)
            return;

        GridEffectWorldVfxPresenter[] presenters =
            view.GetComponentsInChildren<GridEffectWorldVfxPresenter>(true);

        for (int i = 0; i < presenters.Length; i++)
            presenters[i].CleanupSpawnedVfx();
    }

    private void ClearViews()
    {
        List<int> gridIndices = new(viewsByGridIndex.Keys);

        for (int i = 0; i < gridIndices.Count; i++)
            RemoveView(gridIndices[i]);

        viewsByGridIndex.Clear();
    }
}
