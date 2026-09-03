using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleGridEffectController : MonoBehaviour
{
    private const string SpiderEggGridEffectId = "GR_spider_egg";
    private const string ExplosiveDollGridEffectId = "GR_explosive_doll";
    private const string ResidueGridEffectId = "GR_Residue";
    private const string CharacterTargetType = "Character";
    private const string CinderMonsterId = "Mon_06";

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform effectRoot;

    [Header("Grid Effect HP UI")]
    [Tooltip("체력이 있는 그리드 효과 위에 표시할 공용 HP UI 프리팹입니다.")]
    [SerializeField] private GridEffectHpUI gridEffectHpUiPrefab;

    [Tooltip("GridEffect HP UI가 생성될 Screen Space Canvas입니다.")]
    [SerializeField] private Canvas gridEffectHpCanvas;

    [Tooltip("BoxCollider2D의 위쪽 끝에서 HP UI를 추가로 띄울 월드 Y 오프셋입니다.")]
    [SerializeField] private float gridEffectHpUiWorldYOffset = 0.12f;

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
    private readonly Dictionary<int, GridEffectHpUI> hpUiByGridIndex = new();
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
            !DataManager.Instance.GridEffectDatabase.TryGet(gridEffectId, out GridEffectData data) ||
            data == null)
        {
            return false;
        }

        if (state.TryGetEffectId(gridIndex, out string existingGridEffectId))
        {
            bool isSameResidue =
                string.Equals(existingGridEffectId, ResidueGridEffectId, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(gridEffectId, ResidueGridEffectId, System.StringComparison.OrdinalIgnoreCase);

            if (!isSameResidue)
                return false;

            // 같은 칸에 점액이 다시 생성되면 중첩하지 않고 지속시간만 최대치로 갱신합니다.
            return state.RefreshDuration(gridIndex, data.Duration);
        }

        if (!state.Place(gridIndex, gridEffectId, data.Duration, GetInitialHitPoints(data)))
            return false;

        SpawnView(new BattleGridEffectPlacement(gridIndex, gridEffectId));
        return true;
    }


    /// <summary>
    /// 실제 상태에는 등록하지 않고 지정한 칸에 GridEffect의 반투명 배치 미리보기를 생성합니다.
    /// </summary>
    public bool TryCreatePlacementPreview(
        int gridIndex,
        string gridEffectId,
        float alpha,
        out GameObject preview)
    {
        preview = null;

        if (!EnsureDependencies() ||
            gridManager.GetCellByIndex(gridIndex) == null ||
            string.IsNullOrWhiteSpace(gridEffectId) ||
            prefabDatabase == null ||
            !prefabDatabase.TryGetPrefab(gridEffectId, out GameObject prefab) ||
            prefab == null)
        {
            return false;
        }

        Transform parent = effectRoot != null ? effectRoot : transform;
        preview = Instantiate(prefab, parent);
        preview.name = $"GridEffectPreview_{gridEffectId}_{gridIndex}";
        preview.transform.position = gridManager.GetWorldPositionByIndex(gridIndex) + worldOffset;
        preview.transform.localRotation = prefab.transform.localRotation;
        preview.transform.localScale = prefab.transform.localScale * Mathf.Max(0.01f, viewScale);

        DisablePreviewInteraction(preview);
        ApplyRendererSorting(preview);
        ApplyPreviewAlpha(preview, Mathf.Clamp01(alpha));
        preview.SetActive(true);
        return true;
    }

    private static void DisablePreviewInteraction(GameObject preview)
    {
        if (preview == null)
            return;

        Collider[] colliders = preview.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        Collider2D[] colliders2D = preview.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders2D.Length; i++)
        {
            if (colliders2D[i] != null)
                colliders2D[i].enabled = false;
        }
    }

    private static void ApplyPreviewAlpha(GameObject preview, float alpha)
    {
        if (preview == null)
            return;

        SpriteRenderer[] spriteRenderers = preview.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer renderer = spriteRenderers[i];
            if (renderer == null)
                continue;

            Color color = renderer.color;
            color.a *= alpha;
            renderer.color = color;
        }

        CanvasGroup[] canvasGroups = preview.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            CanvasGroup group = canvasGroups[i];
            if (group == null)
                continue;

            group.alpha *= alpha;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        ParticleSystem[] particleSystems = preview.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
                continue;

            ParticleSystem.MainModule main = particleSystem.main;
            ParticleSystem.MinMaxGradient startColor = main.startColor;

            switch (startColor.mode)
            {
                case ParticleSystemGradientMode.Color:
                    {
                        Color color = startColor.color;
                        color.a *= alpha;
                        main.startColor = color;
                        break;
                    }
                case ParticleSystemGradientMode.TwoColors:
                    {
                        Color minColor = startColor.colorMin;
                        Color maxColor = startColor.colorMax;
                        minColor.a *= alpha;
                        maxColor.a *= alpha;
                        main.startColor = new ParticleSystem.MinMaxGradient(minColor, maxColor);
                        break;
                    }
            }
        }
    }

    public bool TryRemoveEffect(int gridIndex)
    {
        if (!EnsureDependencies())
            return false;

        if (!state.Remove(gridIndex))
            return false;

        RemoveView(gridIndex);
        return true;
    }

    public bool HasDamageableEffect(int gridIndex)
    {
        return state.TryGetHitPoints(gridIndex, out int hitPoints) && hitPoints > 0;
    }

    /// <summary>
    /// 체력을 가진 그리드 효과의 현재 체력과 최대 체력을 반환합니다.
    /// </summary>
    public bool TryGetEffectHitPoints(int gridIndex, out int currentHp, out int maxHp)
    {
        currentHp = 0;
        maxHp = 0;

        if (!state.TryGetHitPoints(gridIndex, out currentHp) ||
            !TryGetEffectData(gridIndex, out GridEffectData data) ||
            data == null ||
            data.HP <= 0)
        {
            currentHp = 0;
            return false;
        }

        maxHp = Mathf.Max(0, data.HP);
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        return true;
    }

    public bool IsCharacterTargetEffect(int gridIndex)
    {
        if (!HasDamageableEffect(gridIndex) || !TryGetEffectData(gridIndex, out GridEffectData data))
            return false;

        return string.Equals(
            data.TargetType?.Trim(),
            CharacterTargetType,
            System.StringComparison.OrdinalIgnoreCase);
    }

    public IReadOnlyList<int> GetCharacterTargetGridIndices()
    {
        List<int> result = new();
        IReadOnlyList<BattleGridEffectPlacement> placements = state.GetPlacements();

        for (int i = 0; i < placements.Count; i++)
        {
            int gridIndex = placements[i].GridIndex;

            if (IsCharacterTargetEffect(gridIndex))
                result.Add(gridIndex);
        }

        return result;
    }

    public bool TryDamageEffect(int gridIndex, int damage, out bool destroyed)
    {
        destroyed = false;

        if (!EnsureDependencies())
            return false;

        if (damage <= 0)
            return false;

        if (!state.DamageHitPoints(gridIndex, damage, out destroyed))
            return false;

        if (destroyed)
            RemoveView(gridIndex);

        return true;
    }

    public void ResolveTurnEndGridEffects()
    {
        if (!EnsureDependencies())
            return;

        IReadOnlyList<BattleGridEffectPlacement> placements = state.GetPlacements();
        List<int> explosiveDollGridIndices = new();

        for (int i = 0; i < placements.Count; i++)
        {
            BattleGridEffectPlacement placement = placements[i];

            if (string.Equals(
                    placement.GridEffectId,
                    ExplosiveDollGridEffectId,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                explosiveDollGridIndices.Add(placement.GridIndex);
            }
        }

        for (int i = 0; i < explosiveDollGridIndices.Count; i++)
            ExplodeDoll(explosiveDollGridIndices[i]);
    }

    private void ExplodeDoll(int gridIndex)
    {
        if (!TryGetEffectData(gridIndex, out GridEffectData data) || gridManager == null)
            return;

        int damage = Mathf.Max(0, data.ValueRate);
        Vector2Int origin = gridManager.IndexToCoord(gridIndex);
        HashSet<MonsterUnit> damagedMonsters = new();

        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0)
                    continue;

                Vector2Int coord = origin + new Vector2Int(x, y);

                if (!gridManager.IsValidCoord(coord))
                    continue;

                int targetGridIndex = gridManager.CoordToIndex(coord);

                if (!BattleOccupancyService.TryGetMonsterAtGrid(targetGridIndex, out MonsterUnit monster) ||
                    monster == null ||
                    monster.RuntimeData == null ||
                    monster.RuntimeData.IsDead ||
                    !damagedMonsters.Add(monster))
                {
                    continue;
                }

                BattleEffectUtility.DamageMonster(monster, damage);
            }
        }

        TryRemoveEffect(gridIndex);
    }

    public void AdvanceTurnDurations()
    {
        if (!EnsureDependencies())
            return;

        IReadOnlyList<BattleGridEffectPlacement> expiredPlacements = state.AdvanceDurationsDetailed();

        for (int i = 0; i < expiredPlacements.Count; i++)
        {
            BattleGridEffectPlacement placement = expiredPlacements[i];
            RemoveView(placement.GridIndex);

            if (placement.GridEffectId == SpiderEggGridEffectId)
                SpawnCinderFromExpiredEgg(placement.GridIndex);
        }
    }

    public void ApplyStandingResidueToPlayers()
    {
        if (!EnsureDependencies())
            return;

        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
                continue;

            int gridIndex = character.CurrentGridIndex;

            if (gridIndex < 0 ||
                !state.TryGetEffectId(gridIndex, out string gridEffectId) ||
                !string.Equals(gridEffectId, "GR_Residue", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // 잔여물 위에서 새 턴을 시작하면 이동하지 않았더라도 다시 피해를 받습니다.
            ApplyToPlayer(gridIndex, character);
        }
    }

    public BattleGridEffectApplyResult ApplyToPlayer(int gridIndex, BattleCharacter character)
    {
        if (character == null || character.RuntimeData == null)
            return BattleGridEffectApplyResult.None;

        if (BattleEquipmentEffectService.IgnoresGridEffects(character.RuntimeData))
            return BattleGridEffectApplyResult.None;

        if (!EnsureDependencies())
            return BattleGridEffectApplyResult.None;

        int hpBefore = character.RuntimeData.CurrentHP;
        int shieldBefore = character.RuntimeData.CurrentShield;
        BattleGridEffectApplyResult result =
            service.ApplyToPlayer(state, gridIndex, character.RuntimeData);

        int hpDamage = Mathf.Max(0, hpBefore - character.RuntimeData.CurrentHP);
        int shieldDamage = Mathf.Max(0, shieldBefore - character.RuntimeData.CurrentShield);
        int shownDamage = hpDamage + shieldDamage;

        // 그리드 효과 피해는 스킬 피해와 합산하지 않고 별도의 피해 숫자로 표시합니다.
        if (shownDamage > 0)
            BattleDamageTextPopupUI.Show(character.transform, shownDamage);

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

    private static int GetInitialHitPoints(GridEffectData data)
    {
        return data != null ? Mathf.Max(0, data.HP) : 0;
    }

    private bool TryGetEffectData(int gridIndex, out GridEffectData data)
    {
        data = null;

        if (!state.TryGetEffectId(gridIndex, out string gridEffectId) ||
            DataManager.Instance == null ||
            DataManager.Instance.GridEffectDatabase == null)
        {
            return false;
        }

        return DataManager.Instance.GridEffectDatabase.TryGet(gridEffectId, out data) && data != null;
    }

    private void SpawnCinderFromExpiredEgg(int eggGridIndex)
    {
        int spawnGridIndex = FindAvailableCinderSpawnGrid(eggGridIndex);

        if (spawnGridIndex < 0)
            return;

        BattleMonsterSpawner spawner =
            FindFirstObjectByType<BattleMonsterSpawner>(FindObjectsInactive.Include);

        if (spawner == null)
            return;

        SpawnedMonsterResult result =
            spawner.SpawnRuntimeMonster(CinderMonsterId, new List<int> { spawnGridIndex });

        if (result == null || result.RuntimeData == null || result.MonsterTransform == null)
            return;

        // Cinder hatched from an egg starts without innate armor and never grants a kill reward.
        result.RuntimeData.ClearAllShield();
        result.RuntimeData.SuppressDeathReward = true;

        BattleRoomLoader roomLoader =
            FindFirstObjectByType<BattleRoomLoader>(FindObjectsInactive.Include);

        if (roomLoader != null)
            roomLoader.RegisterRuntimeMonster(result);
    }

    private int FindAvailableCinderSpawnGrid(int preferredGridIndex)
    {
        if (IsSpawnGridAvailable(preferredGridIndex))
            return preferredGridIndex;

        if (gridManager == null)
            return -1;

        Vector2Int originCoord = gridManager.IndexToCoord(preferredGridIndex);
        Vector2Int[] offsets =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector2Int coord = originCoord + offsets[i];

            if (!gridManager.IsValidCoord(coord))
                continue;

            int gridIndex = gridManager.CoordToIndex(coord);

            if (IsSpawnGridAvailable(gridIndex))
                return gridIndex;
        }

        int cellCount = gridManager.Width * gridManager.Height;
        int bestGridIndex = -1;
        int bestDistance = int.MaxValue;

        for (int gridIndex = 0; gridIndex < cellCount; gridIndex++)
        {
            if (!IsSpawnGridAvailable(gridIndex))
                continue;

            Vector2Int coord = gridManager.IndexToCoord(gridIndex);
            int distance = Mathf.Abs(coord.x - originCoord.x) + Mathf.Abs(coord.y - originCoord.y);

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            bestGridIndex = gridIndex;
        }

        return bestGridIndex;
    }

    private bool IsSpawnGridAvailable(int gridIndex)
    {
        if (gridIndex < 0 || gridManager == null)
            return false;

        if (gridManager.GetCellByIndex(gridIndex) == null)
            return false;

        if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex))
            return false;

        if (HasEffect(gridIndex) || IsBlocked(gridIndex))
            return false;

        return true;
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

        // 그리드 효과의 실제 오브젝트 또는 VFX 위에 마우스를 올리면
        // GameData의 Name / ToolTip을 표시할 수 있도록 공통 호버 대상을 자동으로 붙입니다.
        GridEffectHoverTarget.Attach(
            view,
            placement.GridEffectId,
            null,
            this,
            placement.GridIndex);

        PlayWorldVfxProxies(view, view.transform.position);

        viewsByGridIndex[placement.GridIndex] = view;
        SpawnHpUi(placement.GridIndex, placement.GridEffectId, view);
    }

    private void SpawnHpUi(int gridIndex, string gridEffectId, GameObject view)
    {
        RemoveHpUi(gridIndex);

        if (view == null ||
            gridEffectHpUiPrefab == null ||
            gridEffectHpCanvas == null ||
            DataManager.Instance == null ||
            DataManager.Instance.GridEffectDatabase == null ||
            !DataManager.Instance.GridEffectDatabase.TryGet(gridEffectId, out GridEffectData data) ||
            data == null ||
            data.HP <= 0)
        {
            return;
        }

        // 호버 판정과 동일하게 프리팹에 직접 설정한 BoxCollider2D를 기준으로 사용합니다.
        BoxCollider2D targetCollider = view.GetComponentInChildren<BoxCollider2D>(true);

        if (targetCollider == null)
        {
            Debug.LogWarning(
                $"[BattleGridEffectController] HP UI를 표시할 BoxCollider2D가 없습니다. GridEffect={gridEffectId}",
                view);
            return;
        }

        GridEffectHpUI hpUi = Instantiate(gridEffectHpUiPrefab, gridEffectHpCanvas.transform);
        hpUi.name = $"GridEffectHpUI_{gridEffectId}_{gridIndex}";
        hpUi.gameObject.SetActive(true);
        hpUi.Bind(
            this,
            gridIndex,
            targetCollider,
            gridEffectHpCanvas,
            gridEffectHpUiWorldYOffset);

        hpUiByGridIndex[gridIndex] = hpUi;
    }

    private void RemoveHpUi(int gridIndex)
    {
        if (!hpUiByGridIndex.TryGetValue(gridIndex, out GridEffectHpUI hpUi))
            return;

        hpUiByGridIndex.Remove(gridIndex);

        if (hpUi == null)
            return;

        hpUi.gameObject.SetActive(false);

        if (Application.isPlaying)
            Destroy(hpUi.gameObject);
        else
            DestroyImmediate(hpUi.gameObject);
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
        RemoveHpUi(gridIndex);

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
        hpUiByGridIndex.Clear();
    }
}
