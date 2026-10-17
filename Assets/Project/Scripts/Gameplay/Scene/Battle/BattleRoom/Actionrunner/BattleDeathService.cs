using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleDeathService
{
    private const float DefaultMonsterDeathDestroyDelay = 0.6f;
    private const float SplitArcHeight = 0.85f;
    private const string SplitEffectId = "E_Split";

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
    /// 일반 처치 보상을 지급하지 않고 몬스터의 사망 연출과 제거만 처리합니다.
    /// 신더의 자폭처럼 플레이어가 처치한 것으로 취급하지 않는 경우에 사용합니다.
    /// </summary>
    public void HandleMonsterDeadWithoutReward(MonsterUnit monster)
    {
        HandleMonsterDead(monster, false);
    }

    /// <summary>
    /// 머크를 일반 사망 보상 없이 제거하고 블롭 2마리로 분열시킵니다.
    /// </summary>
    public void HandleMuckSplit(MonsterUnit muck)
    {
        if (muck == null || muck.RuntimeData == null || muck.RuntimeData.IsDeathHandled)
            return;

        // 분열은 처치가 아니라 몬스터 교체 연출입니다.
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

        // 승리 판정이 분열 연출 도중 실행되어도 블롭을 살아 있는 몬스터로 인식하도록
        // 연출 대기 전에 먼저 생성하고 전투에 등록합니다.
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

        // 생성 및 등록은 이미 끝났고, 이 코루틴에서는 화면 연출만 처리합니다.
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
                    // 시작점과 도착점에서는 0, 이동 중간에는 최고점인 포물선.
                    position.y += 4f * SplitArcHeight * t * (1f - t);
                    spawned[i].transform.position = position;
                }
            yield return null;
        }
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i] != null)
                spawned[i].transform.position = destinations[i];

        // 분열된 블롭이 목적지 칸에 도착하면 이동 가능한 지형 효과를 적용합니다.
        ApplySpawnGridEffects(spawned);

        float remaining = deathDuration - emergeDelay - elapsed;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);
        RemoveDeadMonster(muck);
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

        RemoveDeadMonster(monster);
    }

    public IEnumerator HandleMonsterDeadRoutine(MonsterUnit monster)
    {
        yield return HandleMonsterDeadRoutine(monster, true);
    }

    private IEnumerator HandleMonsterDeadRoutine(MonsterUnit monster, bool collectReward)
    {
        if (!TryBeginMonsterDeath(monster, collectReward))
            yield break;

        yield return new WaitForSeconds(GetMonsterDeathDestroyDelay(monster));

        RemoveDeadMonster(monster);
    }

    private bool TryBeginMonsterDeath(MonsterUnit monster, bool collectReward)
    {
        if (monster == null || monster.RuntimeData == null)
            return false;

        if (!monster.RuntimeData.IsDead)
            return false;

        if (monster.RuntimeData.IsDeathHandled)
            return false;

        monster.RuntimeData.IsDeathHandled = true;

        // 모르트와 함께 전투 중인 드라우그/바로우의 사망 위치와 사망 턴을 기록합니다.
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

            // 생성 과정에서 기록된 실제 점유 그리드를 사용합니다.
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

        // 인접 칸이 모두 막혔으면 머크에서 가까운 빈 그리드부터 찾습니다.
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
