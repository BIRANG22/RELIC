using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System;
using Object = UnityEngine.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleDeathService
{
    private const float DefaultMonsterDeathDestroyDelay = 0.6f;
    private const float SplitArcHeight = 0.85f;
    private const string SplitEffectId = "E_Split";
    private const string FirstStageId = "Stage1";
    private const string AwakeningCoreItemId = "Item_013";
    private const float AwakeningCoreRiseHeight = 0.85f;
    private const float AwakeningCoreRiseDuration = 0.3f;
    private const float AwakeningCoreDropDuration = 0.42f;
    private const float AwakeningCoreDropBounceHeight = 0.18f;
    private const float AwakeningCoreLandingRotationZ = -18f;

    private readonly GridManager gridManager;
    private readonly BattleMonsterSpawner monsterSpawner;
    private readonly BattleRoomLoader roomLoader;

    public BattleDeathService(
    GridManager gridManager,
    BattleMonsterSpawner monsterSpawner,
    BattleRoomLoader roomLoader)
    {
        this.gridManager = gridManager;
        this.monsterSpawner = monsterSpawner;
        this.roomLoader = roomLoader;
    }

    public void HandleMonsterDead(MonsterUnit monster)
    {
        HandleMonsterDead(monster, true);
    }

    /// <summary>
    /// 일반 처치 보상을 지급하지 않고 몬스터의 사망 처리를 진행합니다.
    /// 플레이어가 직접 처치한 것으로 처리하지 않아야 하는 경우에 사용합니다.
    /// </summary>
    public void HandleMonsterDeadWithoutReward(MonsterUnit monster)
    {
        HandleMonsterDead(monster, false);
    }

    /// <summary>
    /// 머크의 일반 사망 처리를 생략하고 블롭 2마리로 분열시킵니다.
    /// </summary>
    public void HandleMuckSplit(MonsterUnit muck)
    {
        if (muck == null || muck.RuntimeData == null || muck.RuntimeData.IsDeathHandled)
            return;

        // 분열은 처치가 아니라 몬스터의 형태가 바뀌는 과정입니다.
        ShowSplitEffectName(muck);

        // Split is not a normal kill: no kill rewards or death tracking.
        muck.RuntimeData.CurrentHP = 0;
        muck.RuntimeData.IsDeathHandled = true;

        BattleUnitAnimator animator = muck.GetComponent<BattleUnitAnimator>();
        MonsterDeathDissolve dissolve = muck.GetComponent<MonsterDeathDissolve>();
        if (animator != null)
            animator.PlayDead();
        float animationDuration = animator != null ? animator.DeadAnimationDuration : 0f;
        if (dissolve != null)
            dissolve.PlayAfter(animationDuration);

        if (roomLoader != null)
            roomLoader.UnregisterRuntimeMonster(muck);

        // 분열 직후 새 몬스터가 이동 연출 중이더라도 살아 있는 몬스터로 인식하도록
        // 먼저 생성 및 등록 처리를 완료한 뒤 연출을 진행합니다.
        bool animate = CoroutineHost.Instance != null && Application.isPlaying;
        List<MonsterUnit> spawned = SpawnBlobsFromMuck(muck);
        float deathDuration = GetMonsterDeathDestroyDelay(muck);
        if (animate)
        {
            CoroutineHost.Instance.StartCoroutine(AnimateMuckSplit(muck, spawned, deathDuration));
        }
        else
        {
            ApplySpawnGridEffects(spawned);
            RemoveDeadMonster(muck);
        }
    }

    private IEnumerator AnimateMuckSplit(MonsterUnit muck, List<MonsterUnit> spawned, float deathDuration)
    {
        if (muck == null)
            yield break;

        // 새로운 블롭은 이미 생성되었으며, 이 코루틴에서는 등장 연출만 처리합니다.
        float emergeDelay = Mathf.Min(0.18f, deathDuration * 0.25f);
        float travelDuration = Mathf.Max(0.18f, Mathf.Min(0.4f, deathDuration - emergeDelay));
        Vector3[] destinations = new Vector3[spawned.Count];
        Vector3[] origins = new Vector3[spawned.Count];
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] == null)
                continue;
            destinations[i] = spawned[i].transform.position;
            origins[i] = muck.transform.position;
            origins[i].z = destinations[i].z;
            SetSortingReferenceY(spawned[i], destinations[i].y);
            spawned[i].transform.position = origins[i];
        }

        if (emergeDelay > 0f)
            yield return new WaitForSeconds(emergeDelay);

        float elapsed = 0f;
        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / travelDuration));
            for (int i = 0; i < spawned.Count; i++)
                if (spawned[i] != null)
                {
                    Vector3 position = Vector3.LerpUnclamped(origins[i], destinations[i], t);
                    // 시작점과 도착점의 높이 차이에 더해 이동 중 포물선 효과를 적용합니다.
                    position.y += 4f * SplitArcHeight * t * (1f - t);
                    spawned[i].transform.position = position;
                }
            yield return null;
        }
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i] != null)
            {
                spawned[i].transform.position = destinations[i];
                ClearSortingReferenceY(spawned[i]);
            }

        // 분열한 블롭의 최종 위치를 확정한 뒤 해당 칸의 그리드 효과를 적용합니다.
        ApplySpawnGridEffects(spawned);

        float remaining = deathDuration - emergeDelay - elapsed;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);
        RemoveDeadMonster(muck);
    }

    private static void SetSortingReferenceY(MonsterUnit monster, float worldY)
    {
        if (monster == null)
            return;

        YSortSprite[] sorters = monster.GetComponentsInChildren<YSortSprite>(true);
        for (int i = 0; i < sorters.Length; i++)
            sorters[i].SetSortingReferenceY(worldY);
    }

    private static void ClearSortingReferenceY(MonsterUnit monster)
    {
        if (monster == null)
            return;

        YSortSprite[] sorters = monster.GetComponentsInChildren<YSortSprite>(true);
        for (int i = 0; i < sorters.Length; i++)
            sorters[i].ClearSortingReferenceY();
    }

    private static void ShowSplitEffectName(MonsterUnit muck)
    {
        if (muck == null)
            return;

        EffectMasterData effect = DataManager.Instance?.EffectDatabase?.Get(SplitEffectId);
        if (effect == null)
            return;

        string localizedName = GameDataLocalization.EffectName(effect);
        if (!string.IsNullOrWhiteSpace(localizedName))
            BattleSkillTextPopupUI.Show(muck.transform, localizedName);
    }

    private void HandleMonsterDead(MonsterUnit monster, bool collectReward)
    {
        if (monster == null || monster.RuntimeData == null)
            return;

        if (CoroutineHost.Instance != null && Application.isPlaying)
        {
            CoroutineHost.Instance.StartCoroutine(
                HandleMonsterDeadRoutine(monster, collectReward)
            );
            return;
        }

        if (!TryBeginMonsterDeath(monster, collectReward))
            return;

        FinishMonsterDeathAndPlaceBones(monster);
    }

    public IEnumerator HandleMonsterDeadRoutine(MonsterUnit monster)
    {
        yield return HandleMonsterDeadRoutine(monster, true);
    }

    private IEnumerator HandleMonsterDeadRoutine(MonsterUnit monster, bool collectReward)
    {
        if (!TryBeginMonsterDeath(monster, collectReward))
            yield break;

        bool showAwakeningCoreDrop = ShouldShowAwakeningCoreDrop(monster);

        float fullDeathDuration = GetMonsterDeathDestroyDelay(monster);
        BattleUnitAnimator deathAnimator = monster.GetComponent<BattleUnitAnimator>();
        float dissolveStartDelay = deathAnimator != null ? Mathf.Max(0f, deathAnimator.DeadAnimationDuration) : 0f;

        if (showAwakeningCoreDrop)
        {
            // Spawn the core when the final-phase death dissolve starts, not after it finishes.
            if (dissolveStartDelay > 0f)
                yield return new WaitForSeconds(dissolveStartDelay);
            if (monster != null && CoroutineHost.Instance != null)
                CoroutineHost.Instance.StartCoroutine(PlayAwakeningCoreDropPresentation(monster));
            float remaining = fullDeathDuration - dissolveStartDelay;
            if (remaining > 0f)
                yield return new WaitForSeconds(remaining);
        }
        else
        {
            yield return new WaitForSeconds(fullDeathDuration);
        }

        FinishMonsterDeathAndPlaceBones(monster);
    }

    private bool TryBeginMonsterDeath(MonsterUnit monster, bool collectReward)
    {
        if (monster == null || monster.RuntimeData == null)
            return false;

        if (!monster.RuntimeData.IsDead)
            return false;

        if (monster.RuntimeData.IsDeathHandled)
            return false;

        if (TryTransitionArabellaToPhaseTwo(monster))
            return false;

        monster.RuntimeData.IsDeathHandled = true;

        // 모르트와 함께 등장한 병사의 사망 정보를 네크로맨시 추적기에 기록합니다.
        MortNecromancyTracker.RecordSoldierDeath(monster, gridManager);

        if (monster.RuntimeData.MonsterId == "Mon_11")
            MortNecromancyTracker.RemoveMort(monster.RuntimeData.RuntimeId);

        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();
        MonsterDeathDissolve dissolve = monster.GetComponent<MonsterDeathDissolve>();

        if (animator != null)
            animator.PlayDead();

        if (dissolve != null)
            dissolve.PlayAfter(animator != null ? animator.DeadAnimationDuration : 0f);

        if (collectReward &&
            !monster.RuntimeData.SuppressDeathReward &&
            !MortNecromancyTracker.IsRevivedSoldier(monster.RuntimeData.RuntimeId))
        {
            CollectMonsterReward(monster);
        }

        if (roomLoader != null)
            roomLoader.UnregisterRuntimeMonster(monster);

        return true;
    }

    private static bool TryTransitionArabellaToPhaseTwo(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return false;

        // 전환 대기 중에는 HP 0 상태를 유지하여 추가 피해를 받지 않습니다.
        if (ArabellaPhaseTransition.IsTransitionPending(monster))
            return true;

        MonsterMasterData phaseTwoMaster =
            DataManager.Instance?.MonsterDatabase?.Get(ArabellaPhaseTransition.PhaseTwoMonsterId);

        if (!ArabellaPhaseTransition.TryCreatePhaseTwo(
                monster.RuntimeData,
                phaseTwoMaster,
                out MonsterRuntimeData phaseTwoRuntime))
        {
            return false;
        }

        ArabellaPhaseTransition.QueuePhaseTwo(monster, phaseTwoRuntime);
        return true;
    }

    private const string SkullBoneGridEffectId = "GR_Skull_bone";

    /// <summary>
    /// 몬스터의 사망 처리를 마무리하고 필요한 경우 뼈 오브젝트를 배치합니다.
    /// 머크의 분열 등 특수한 사망 처리와는 구분하여 실행합니다.
    /// </summary>
    private void FinishMonsterDeathAndPlaceBones(MonsterUnit monster)
    {
        if (monster == null)
            return;

        string monsterId = monster.RuntimeData != null ? monster.RuntimeData.MonsterId : null;
        bool leaveBones = monsterId == "Mon_07" || monsterId == "Mon_08";
        List<int> deathCells = leaveBones
            ? new List<int>(monster.OccupiedGridIndices)
            : null;

        RemoveDeadMonster(monster);

        if (!leaveBones || deathCells == null || deathCells.Count == 0)
            return;

        BattleGridEffectController controller =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);
        if (controller == null)
            return;

        for (int i = 0; i < deathCells.Count; i++)
        {
            int cell = deathCells[i];
            if (cell < 0 || controller.HasEffect(cell))
                continue;

            controller.TryPlaceEffect(cell, SkullBoneGridEffectId);
        }
    }

    private void RemoveDeadMonster(MonsterUnit monster)
    {
        if (monster == null)
            return;

        monster.DestroyHUD();

        if (Application.isPlaying)
            Object.Destroy(monster.gameObject);
        else
            Object.DestroyImmediate(monster.gameObject);
    }

    private float GetMonsterDeathDestroyDelay(MonsterUnit monster)
    {
        if (monster == null)
            return DefaultMonsterDeathDestroyDelay;

        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();
        MonsterDeathDissolve dissolve = monster.GetComponent<MonsterDeathDissolve>();
        float deadAnimationDuration = animator != null
            ? animator.DeadAnimationDuration
            : DefaultMonsterDeathDestroyDelay;

        return Mathf.Max(0f, deadAnimationDuration) +
               (dissolve != null ? dissolve.Duration : 0f);
    }

    private void CollectMonsterReward(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return;

        if (BattleRewardCollector.Instance == null)
            return;

        BattleRewardCollector.Instance.CollectMonsterReward(monster.RuntimeData);
    }

    private static bool ShouldShowAwakeningCoreDrop(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return false;

        if (!string.Equals(
                monster.RuntimeData.MonsterId,
                ArabellaPhaseTransition.PhaseTwoMonsterId,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (TrialUnlockProgress.Stage3ClearCount > 0)
            return false;

        MapRuntimeData runtime = DataManager.Instance?.MapRuntimeStore?.Get();
        if (runtime == null)
            return false;

        return string.Equals(runtime.CurrentStage, FirstStageId, StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerator PlayAwakeningCoreDropPresentation(MonsterUnit monster)
    {
        if (monster == null)
            yield break;

        GameObject visualRoot = CreateAwakeningCoreDropVisual(monster);
        if (visualRoot == null)
            yield break;

        Transform visualTransform = visualRoot.transform;
        Vector3 groundPosition = monster.transform.position;
        groundPosition.z -= 0.02f;
        Vector3 topPosition = groundPosition + Vector3.up * AwakeningCoreRiseHeight;
        visualTransform.position = groundPosition;
        visualTransform.localRotation = Quaternion.identity;

        float elapsed = 0f;
        while (elapsed < AwakeningCoreRiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / AwakeningCoreRiseDuration);
            float eased = 1f - (1f - t) * (1f - t);
            visualTransform.position = Vector3.LerpUnclamped(groundPosition, topPosition, eased);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < AwakeningCoreDropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / AwakeningCoreDropDuration);
            float eased = t * t * t;
            Vector3 position = Vector3.LerpUnclamped(topPosition, groundPosition, eased);
            position.y += 4f * AwakeningCoreDropBounceHeight * t * (1f - t);
            visualTransform.position = position;
            yield return null;
        }

        // A brief, uneven tumble after impact; no smooth rotation during the fall.
        visualTransform.position = groundPosition;
        visualTransform.localRotation = Quaternion.identity;
        Vector3 landedPosition = groundPosition;
        float tumbleTime = 0f;
        const float tumbleDuration = 0.22f;
        while (tumbleTime < tumbleDuration)
        {
            tumbleTime += Time.deltaTime;
            float t = Mathf.Clamp01(tumbleTime / tumbleDuration);
            // Two abrupt contacts: tilt, recoil slightly, then settle on one side.
            float rotation = t < 0.38f
                ? Mathf.Lerp(0f, AwakeningCoreLandingRotationZ * 1.45f, t / 0.38f)
                : t < 0.65f
                    ? Mathf.Lerp(AwakeningCoreLandingRotationZ * 1.45f,
                        AwakeningCoreLandingRotationZ * 0.7f, (t - 0.38f) / 0.27f)
                    : Mathf.Lerp(AwakeningCoreLandingRotationZ * 0.7f,
                        AwakeningCoreLandingRotationZ, (t - 0.65f) / 0.35f);
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            landedPosition.x = groundPosition.x + 0.12f * Mathf.Clamp01(t / 0.55f);
            landedPosition.y = groundPosition.y + (t < 0.6f ? 0.07f * Mathf.Sin(t / 0.6f * Mathf.PI) : 0f);
            visualTransform.position = landedPosition;
            yield return null;
        }
        visualTransform.position = new Vector3(groundPosition.x + 0.12f, groundPosition.y, groundPosition.z);
        visualTransform.localRotation = Quaternion.Euler(0f, 0f, AwakeningCoreLandingRotationZ);
        // Keep the item visible until the battle scene is unloaded.
    }

    private static GameObject CreateAwakeningCoreDropVisual(MonsterUnit monster)
    {
        if (monster == null)
            return null;

        if (DataManager.Instance?.ItemIconDatabase == null ||
            !DataManager.Instance.ItemIconDatabase.TryGetIcon(AwakeningCoreItemId, out Sprite iconSprite) ||
            iconSprite == null)
        {
            return null;
        }

        GameObject root = new GameObject("AwakeningCoreDropPresentation");
        SpriteRenderer referenceRenderer = monster.GetComponentInChildren<SpriteRenderer>();
        // Do not parent to the monster: the monster is destroyed after death.
        SpriteRenderer iconRenderer = root.AddComponent<SpriteRenderer>();
        iconRenderer.sprite = iconSprite;
        iconRenderer.drawMode = SpriteDrawMode.Simple;
        iconRenderer.maskInteraction = SpriteMaskInteraction.None;
        iconRenderer.sortingLayerID = referenceRenderer != null
            ? referenceRenderer.sortingLayerID
            : 0;
        iconRenderer.sortingOrder = referenceRenderer != null
            ? referenceRenderer.sortingOrder + 32
            : 32760;
        iconRenderer.color = Color.white;

        Vector3 baseScale = Vector3.one * 0.05f;
        root.transform.localScale = baseScale;
        return root;
    }

    private List<MonsterUnit> SpawnBlobsFromMuck(MonsterUnit muck)
    {
        List<MonsterUnit> spawned = new List<MonsterUnit>();
        if (muck == null || monsterSpawner == null || gridManager == null)
            return spawned;

        List<int> spawnCells = FindEmptyCellsAroundMonster(muck, 2);
        for (int i = 0; i < spawnCells.Count; i++)
        {
            SpawnedMonsterResult result = monsterSpawner.SpawnRuntimeMonster(
                "Mon_02", new List<int> { spawnCells[i] });
            if (result == null)
                continue;

            if (roomLoader != null)
                roomLoader.RegisterRuntimeMonster(result);
            if (result.MonsterTransform != null &&
                result.MonsterTransform.TryGetComponent(out MonsterUnit blob))
                spawned.Add(blob);
        }
        return spawned;
    }

    private void ApplySpawnGridEffects(List<MonsterUnit> spawned)
    {
        if (spawned == null || spawned.Count == 0)
            return;

        BattleGridEffectController controller =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);
        if (controller == null)
            return;

        for (int i = 0; i < spawned.Count; i++)
        {
            MonsterUnit blob = spawned[i];
            if (blob == null || blob.RuntimeData == null || blob.RuntimeData.IsDead)
                continue;

            // 새로 생성된 블롭이 점유한 모든 그리드에 효과를 적용합니다.
            for (int j = 0; j < blob.OccupiedGridIndices.Count; j++)
            {
                controller.ApplyToMonster(blob.OccupiedGridIndices[j], blob);
                if (blob.RuntimeData.IsDead)
                {
                    HandleMonsterDead(blob);
                    break;
                }
            }
        }
    }

    private List<int> FindEmptyCellsAroundMonster(MonsterUnit monster, int count)
    {
        List<int> result = new();

        if (monster == null || gridManager == null || count <= 0)
            return result;

        Vector2Int[] priorityOffsets =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
            new Vector2Int(-1, 1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1)
        };

        for (int cellIndex = 0; cellIndex < monster.OccupiedGridIndices.Count; cellIndex++)
        {
            int originIndex = monster.OccupiedGridIndices[cellIndex];

            if (originIndex < 0)
                continue;

            Vector2Int originCoord = gridManager.IndexToCoord(originIndex);

            for (int i = 0; i < priorityOffsets.Length; i++)
            {
                TryAddEmptyCell(originCoord + priorityOffsets[i], monster, result);

                if (result.Count >= count)
                    return result;
            }
        }

        // 주변 칸에 배치할 수 없으면 머크의 위치를 기준으로 다른 그리드를 탐색합니다.
        List<int> fallbackCandidates = new();

        for (int gridIndex = 0; gridIndex < gridManager.Width * gridManager.Height; gridIndex++)
        {
            if (result.Contains(gridIndex) || monster.ContainsGridIndex(gridIndex))
                continue;

            if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex, null, monster))
                continue;

            if (IsBlockedByGridEffect(gridIndex))
                continue;

            fallbackCandidates.Add(gridIndex);
        }

        fallbackCandidates.Sort((a, b) =>
        {
            int distanceA = GetNearestMonsterCellDistance(monster, a);
            int distanceB = GetNearestMonsterCellDistance(monster, b);
            int compare = distanceA.CompareTo(distanceB);
            return compare != 0 ? compare : a.CompareTo(b);
        });

        for (int i = 0; i < fallbackCandidates.Count && result.Count < count; i++)
            result.Add(fallbackCandidates[i]);

        return result;
    }

    private void TryAddEmptyCell(
        Vector2Int coord,
        MonsterUnit monster,
        List<int> result)
    {
        if (result == null || !gridManager.IsValidCoord(coord))
            return;

        int gridIndex = gridManager.CoordToIndex(coord);

        if (result.Contains(gridIndex) || monster.ContainsGridIndex(gridIndex))
            return;

        if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex, null, monster))
            return;

        if (IsBlockedByGridEffect(gridIndex))
            return;

        result.Add(gridIndex);
    }


    private bool IsBlockedByGridEffect(int gridIndex)
    {
        if (gridIndex < 0)
            return true;

        BattleGridEffectController gridEffectController =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        return gridEffectController != null && gridEffectController.IsBlocked(gridIndex);
    }

    private int GetNearestMonsterCellDistance(MonsterUnit monster, int gridIndex)
    {
        if (monster == null || gridIndex < 0)
            return int.MaxValue;

        Vector2Int targetCoord = gridManager.IndexToCoord(gridIndex);
        int nearestDistance = int.MaxValue;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            int originIndex = monster.OccupiedGridIndices[i];

            if (originIndex < 0)
                continue;

            Vector2Int originCoord = gridManager.IndexToCoord(originIndex);
            int distance = Mathf.Abs(targetCoord.x - originCoord.x) +
                           Mathf.Abs(targetCoord.y - originCoord.y);
            nearestDistance = Mathf.Min(nearestDistance, distance);
        }

        return nearestDistance;
    }
}
