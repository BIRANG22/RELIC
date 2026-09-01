using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.VFX;

public enum VfxFlipType
{
    RotationY180,
    ParticleRendererFlipY,
    None
}

public class BattleUnitAnimator : MonoBehaviour
{
    private const string DefaultVfxSortingReferenceName = "SpriteRoot";

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("State Names")]
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string moveStateName = "Move";
    [SerializeField] private string guardStateName = "Guard";
    [SerializeField] private string hitStateName = "Hit";
    [SerializeField] private string healStateName = "";
    [SerializeField] private string deadStateName = "Dead";

    [Header("Move VFX")]
    [SerializeField] private BattleVfxEntry moveVfx;

    [Header("Guard VFX")]
    [SerializeField] private BattleVfxEntry guardVfx;

    [Header("Hit VFX")]
    [SerializeField] private BattleVfxEntry hitVfx;

    [Header("Heal VFX")]
    [SerializeField] private BattleVfxEntry healVfx;

    [Header("Status VFX")]
    [SerializeField] private BattleStatusVfxSet statusVfx = new();

    [Header("Player Skill Presentations")]
    [SerializeField] private BattleUnitPlayerSkillPresentations playerSkillPresentations = new();

    [Header("Skill Attack Overrides")]
    [SerializeField] private SkillAttackOverrideDatabase skillAttackOverrideDatabase;

    [Header("Skill VFX")]
    [SerializeField] private SkillVfxDatabase skillVfxDatabase;
    [SerializeField] private GridManager gridManager;

    [Header("Monster Action Presentations")]
    [SerializeField]
    private BattleUnitActionPresentation[] monsterActionPresentations =
        BattleUnitActionPresentation.CreateArray(10);

    [Header("VFX Spawn")]
    [SerializeField] private Transform vfxSpawnPoint;

    [Header("VFX Sorting")]
    [SerializeField] private string vfxSortingReferenceName = DefaultVfxSortingReferenceName;
    [SerializeField] private float vfxSortingReferenceYOffset = -0.1f;

    [Header("VFX Facing")]
    [SerializeField] private BattleUnitFacing unitFacing;
    [SerializeField] private bool autoFindFacing = true;
    [SerializeField] private bool flipVfxLocalPositionX = true;
    [SerializeField] private bool flipVfxScaleXWhenFlipTypeNone = true;

    [SerializeField] private float vfxLifeTime = 2f;

    [Header("Setting")]
    [SerializeField] private int animatorLayer = 0;
    [SerializeField] private float crossFadeDuration = 0f;
    [SerializeField] private float deadAnimationDuration = 0.6f;
    [SerializeField] private bool forceAnimatorUpdate = true;
    [SerializeField] private bool autoFindAnimatorInChildren = true;

    [SerializeField] private string vfxLayerName = "VFX";
    private int vfxLayer = -1;

    private int currentAttackIndex;
    private Transform vfxSortingReference;

    public float DeadAnimationDuration => Mathf.Max(0f, deadAnimationDuration);

    private void Awake()
    {
        EnsurePlayerSkillPresentations();
        EnsureMonsterActionPresentationArray();

        vfxLayer = LayerMask.NameToLayer(vfxLayerName);

        FindAnimatorIfNeeded();
        FindFacingIfNeeded();
        PlayIdle();
    }

    private void OnValidate()
    {
        EnsurePlayerSkillPresentations();
        EnsureMonsterActionPresentationArray();
        statusVfx ??= new BattleStatusVfxSet();
        vfxSortingReference = null;
    }

    public void PlayIdle()
    {
        PlayState(idleStateName);
    }

    public void PlayMove()
    {
        if (EnsureAnimator())
            animator.speed = 1f;

        PlayState(moveStateName);
        SpawnVfx(moveVfx);
    }

    /// <summary>
    /// Move 상태를 마지막 프레임부터 역방향으로 재생합니다.
    /// 포탈 도착 연출처럼 같은 이동 애니메이션을 반대로 보여줄 때 사용합니다.
    /// </summary>
    public void PlayMoveReverse()
    {
        if (!TryResolveAnimatorStateName(moveStateName, out string resolvedStateName))
            return;

        animator.speed = -1f;
        animator.Play(resolvedStateName, animatorLayer, 1f);

        if (forceAnimatorUpdate)
            animator.Update(0f);
    }

    /// <summary>
    /// 현재 애니메이터의 재생 속도를 변경합니다.
    /// 다단 공격처럼 같은 행동 안에서 여러 모션을 빠르게 이어갈 때 사용합니다.
    /// </summary>
    public void SetPlaybackSpeed(float speed)
    {
        if (EnsureAnimator())
            animator.speed = Mathf.Max(0.01f, speed);
    }

    public void RestorePlaybackSpeed()
    {
        if (EnsureAnimator())
            animator.speed = 1f;
    }

    public void PlayGuard()
    {
        PlayState(guardStateName);
        SpawnVfx(guardVfx);
    }

    public void PlayHit()
    {
        PlayState(hitStateName);
        SpawnVfx(hitVfx);
    }

    public void PlayHeal()
    {
        PlayOptionalState(healStateName);
        SpawnVfx(healVfx);
    }

    public void PlayDead()
    {
        PlayState(deadStateName);
    }

    public void PlayBuff()
    {
        //PlayState(controlStateName);
    }

    public void PlayDebuff()
    {
        //PlayState(controlStateName);
    }

    public void PlayStatusVfx(string effectId)
    {
        SpawnVfx(statusVfx?.Get(effectId));
    }

    public void PlaySkillReady(SkillMasterData skillData)
    {
    }

    public void PlaySkillAction(SkillMasterData skillData)
    {
        PlaySkillAction(skillData, null);
    }

    public void PlaySkillAction(PlayerReservedCommand command)
    {
        if (command == null)
        {
            PlayIdle();
            return;
        }

        PlaySkillAction(command.SkillData, command);
    }

    private void PlaySkillAction(SkillMasterData skillData, PlayerReservedCommand command)
    {
        if (skillData == null)
        {
            PlayIdle();
            return;
        }

        if (skillData.Category == Category.Move)
        {
            PlaySkillVfx(skillData, command);
            PlayMove();
            return;
        }

        PlaySkillVfx(skillData, command);

        switch (skillData.SkillType)
        {
            case SkillType.Buff:
                EnsurePlayerSkillPresentations();
                PlayPresentation(playerSkillPresentations.power);
                break;

            case SkillType.Debuff:
                EnsurePlayerSkillPresentations();
                PlayPresentation(playerSkillPresentations.skill);
                break;

            case SkillType.Attack:
                if (TryPlaySkillAttackOverride(skillData))
                    break;

                PlayRandomAttackAction();
                break;

            default:
                PlayRandomAttackAction();
                break;
        }
    }

    /// <summary>
    /// 다단 공격의 타격 순서에 맞춰 서로 다른 공격 모션을 재생합니다.
    /// Attack 1~3 중 실제로 등록된 모션만 순서대로 순환합니다.
    /// </summary>
    public void PlaySkillAction(SkillMasterData skillData, int hitIndex)
    {
        PlaySkillAction(skillData, null, hitIndex);
    }

    public void PlaySkillAction(PlayerReservedCommand command, int hitIndex)
    {
        if (command == null)
        {
            PlayIdle();
            return;
        }

        PlaySkillAction(command.SkillData, command, hitIndex);
    }

    private void PlaySkillAction(SkillMasterData skillData, PlayerReservedCommand command, int hitIndex)
    {
        if (skillData == null || skillData.SkillType != SkillType.Attack)
        {
            PlaySkillAction(skillData, command);
            return;
        }

        PlaySkillVfx(skillData, command);

        if (TryPlaySkillAttackOverride(skillData))
            return;

        List<int> assignedAttackIndices = GetAssignedAttackIndices();

        if (assignedAttackIndices.Count <= 0)
        {
            PlayRandomAttackAction();
            return;
        }

        int sequenceIndex = Mathf.Abs(hitIndex) % assignedAttackIndices.Count;
        PlayAttackAction(assignedAttackIndices[sequenceIndex]);
    }

    public void PlayMonsterSkillReady(MonsterReservedCommand command)
    {
    }

    public void PlayMonsterSkillReady(MonsterSkillData skillData)
    {
    }

    public void PlayMonsterSkillAction(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
        {
            PlayIdle();
            return;
        }

        if (IsActualMonsterMove(command))
        {
            PlayMove();
            return;
        }

        if (IsValidMonsterActionIndex(command.ActionIndex))
        {
            BattleUnitActionPresentation monsterPresentation =
                GetMonsterActionPresentation(command.ActionIndex);

            if (HasPresentation(monsterPresentation))
            {
                PlayPresentation(monsterPresentation, command);
                return;
            }
        }

        // 몬스터 전용 슬롯이 비어 있는 공격 행동은 기존 공용 Attack 1~3 연출로 보정합니다.
        if (command.SkillData.TimelineNotation == TimelineActionType.Attack &&
            command.ActionIndex >= 1 && command.ActionIndex <= 3)
        {
            EnsurePlayerSkillPresentations();
            BattleUnitActionPresentation attackPresentation =
                playerSkillPresentations.GetAttack(command.ActionIndex);

            if (HasPresentation(attackPresentation))
            {
                PlayPresentation(attackPresentation, command);
                return;
            }
        }

        PlayMonsterSkillAction(command.SkillData);
    }


    private static bool IsActualMonsterMove(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;

        // 실제 이동 명령만 이동 연출을 사용합니다.
        // 공격 스킬의 EffectIds에 이동 관련 값이 섞여 있더라도 공격 연출이
        // 이동 VFX로 잘못 재생되지 않도록 EffectIds만으로 이동을 판정하지 않습니다.
        if (command.IsPortalMove)
            return true;

        if (command.EffectiveMoveOffset != Vector2Int.zero)
            return true;

        return command.SkillData.TimelineNotation == TimelineActionType.Move;
    }

    public bool HasMonsterProjectileVfx(MonsterReservedCommand command)
    {
        return TryGetMonsterProjectilePresentation(command, out BattleUnitActionPresentation presentation) &&
               !ShouldSpawnProjectileImpactOnMonsterTargetGrids(presentation);
    }

    public IEnumerator PlayMonsterProjectileVfx(
        MonsterReservedCommand command,
        Vector3 targetWorldPosition)
    {
        if (!TryGetMonsterProjectilePresentation(command, out BattleUnitActionPresentation presentation))
            yield break;

        BattleProjectileVfxEntry projectile = presentation.projectileVfx;

        if (!HasProjectileVfx(projectile))
            yield break;

        yield return PlayProjectileVfx(projectile, targetWorldPosition);
    }

    public IEnumerator PlaySkillTargetVfx(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            yield break;

        if (!TryResolveSkillVfxEntry(command.SkillData, out SkillVfxEntry entry))
            yield break;

        if (!TryResolveSelectedGridWorldPosition(command, out Vector3 targetWorldPosition))
            yield break;

        if (!HasProjectileVfx(entry.ProjectileVfx))
            yield break;

        yield return PlayProjectileVfx(entry.ProjectileVfx, targetWorldPosition);
    }

    public void PlayMonsterSkillAction(MonsterSkillData skillData)
    {
        if (skillData == null)
        {
            PlayIdle();
            return;
        }

        switch (skillData.TimelineNotation)
        {
            case TimelineActionType.Move:
                PlayMove();
                break;

            case TimelineActionType.Buff:
            case TimelineActionType.Debuff:
                break;

            case TimelineActionType.Attack:
                break;

            default:
                break;
        }
    }

    public void PlayCurrentAttackAction()
    {
        EnsurePlayerSkillPresentations();

        if (currentAttackIndex < 1 || currentAttackIndex > 3)
            currentAttackIndex = GetRandomAssignedAttackIndex();

        currentAttackIndex = GetAssignedAttackIndexOrFallback(currentAttackIndex);
        PlayPresentation(playerSkillPresentations.GetAttack(currentAttackIndex));
    }

    public void PlayRandomAttackAction()
    {
        currentAttackIndex = GetRandomAssignedAttackIndex();
        PlayCurrentAttackAction();
    }

    private bool TryPlaySkillAttackOverride(SkillMasterData skillData)
    {
        if (!TryResolveSkillAttackOverride(skillData, out int attackIndex))
            return false;

        EnsurePlayerSkillPresentations();

        BattleUnitActionPresentation presentation = playerSkillPresentations.GetAttack(attackIndex);
        if (!HasPresentation(presentation))
            return false;

        currentAttackIndex = attackIndex;
        PlayPresentation(presentation);
        return true;
    }

    private bool TryResolveSkillAttackOverride(SkillMasterData skillData, out int attackIndex)
    {
        attackIndex = 0;

        if (skillData == null || string.IsNullOrWhiteSpace(skillData.SkillId))
            return false;

        string characterId = GetOwnerCharacterId();
        if (string.IsNullOrWhiteSpace(characterId))
            return false;

        SkillAttackOverrideDatabase database = ResolveSkillAttackOverrideDatabase();
        return database != null &&
               database.TryGetAttackIndex(characterId, skillData.SkillId, out attackIndex);
    }

    private SkillAttackOverrideDatabase ResolveSkillAttackOverrideDatabase()
    {
        if (skillAttackOverrideDatabase != null)
            return skillAttackOverrideDatabase;

        return DataManager.Instance != null
            ? DataManager.Instance.SkillAttackOverrideDatabase
            : null;
    }

    private void PlaySkillVfx(SkillMasterData skillData)
    {
        PlaySkillVfx(skillData, null);
    }

    private void PlaySkillVfx(SkillMasterData skillData, PlayerReservedCommand command)
    {
        if (!TryResolveSkillVfx(skillData, out BattleVfxEntry vfx))
            return;

        SpawnVfx(vfx, command);
    }

    private bool TryResolveSkillVfx(SkillMasterData skillData, out BattleVfxEntry vfx)
    {
        vfx = null;

        if (skillData == null || string.IsNullOrWhiteSpace(skillData.SkillId))
            return false;

        SkillVfxDatabase database = ResolveSkillVfxDatabase();
        return database != null && database.TryGetVfx(skillData.SkillId, out vfx);
    }

    private bool TryResolveSkillVfxEntry(SkillMasterData skillData, out SkillVfxEntry entry)
    {
        entry = null;

        if (skillData == null || string.IsNullOrWhiteSpace(skillData.SkillId))
            return false;

        SkillVfxDatabase database = ResolveSkillVfxDatabase();
        return database != null && database.TryGetEntry(skillData.SkillId, out entry);
    }

    private SkillVfxDatabase ResolveSkillVfxDatabase()
    {
        if (skillVfxDatabase != null)
            return skillVfxDatabase;

        return DataManager.Instance != null
            ? DataManager.Instance.SkillVfxDatabase
            : null;
    }

    private string GetOwnerCharacterId()
    {
        BattleCharacter character = GetComponentInParent<BattleCharacter>();
        return character != null && character.RuntimeData != null
            ? character.RuntimeData.CharacterId
            : null;
    }

    private void PlayPresentation(BattleUnitActionPresentation presentation)
    {
        PlayPresentation(presentation, null);
    }

    private void PlayPresentation(
        BattleUnitActionPresentation presentation,
        MonsterReservedCommand command)
    {
        if (presentation == null)
            return;

        PlayState(presentation.stateName);

        if (presentation.spawnVfxOnEachTargetGrid &&
            TrySpawnVfxOnMonsterTargetGrids(presentation.vfx, command))
        {
            return;
        }

        if (presentation.spawnVfxOnEachTargetGrid &&
            TrySpawnProjectileImpactOnMonsterTargetGrids(presentation.projectileVfx, command))
        {
            return;
        }

        SpawnVfx(presentation.vfx);
    }

    /// <summary>
    /// 디버그 입력 등에서 Monster Action Presentations의 지정 슬롯을 직접 재생합니다.
    /// 인스펙터의 Monster Action Presentations 1~10과 동일한 인덱스를 사용합니다.
    /// </summary>
    public void PlayMonsterActionPresentation(int actionIndex)
    {
        if (!IsValidMonsterActionIndex(actionIndex))
            return;

        PlayPresentation(GetMonsterActionPresentation(actionIndex));
    }

    private BattleUnitActionPresentation GetMonsterActionPresentation(int actionIndex)
    {
        EnsureMonsterActionPresentationArray();

        return monsterActionPresentations[actionIndex - 1];
    }

    private bool TryGetMonsterActionPresentation(
        MonsterReservedCommand command,
        out BattleUnitActionPresentation presentation)
    {
        presentation = null;

        if (command == null || !IsValidMonsterActionIndex(command.ActionIndex))
            return false;

        presentation = GetMonsterActionPresentation(command.ActionIndex);
        return presentation != null;
    }

    private bool TryGetMonsterProjectilePresentation(
        MonsterReservedCommand command,
        out BattleUnitActionPresentation presentation)
    {
        presentation = null;

        if (command == null)
            return false;

        if (TryGetMonsterActionPresentation(command, out BattleUnitActionPresentation mapped) &&
            HasProjectileVfx(mapped.projectileVfx))
        {
            presentation = mapped;
            return true;
        }

        return false;
    }

    private void EnsurePlayerSkillPresentations()
    {
        playerSkillPresentations ??= new BattleUnitPlayerSkillPresentations();
        playerSkillPresentations.EnsureSlots();
    }

    private bool IsValidMonsterActionIndex(int actionIndex)
    {
        EnsureMonsterActionPresentationArray();
        return actionIndex >= 1 && actionIndex <= monsterActionPresentations.Length;
    }

    private void EnsureMonsterActionPresentationArray()
    {
        const int ActionPresentationCount = 10;

        if (monsterActionPresentations == null ||
            monsterActionPresentations.Length != ActionPresentationCount)
        {
            BattleUnitActionPresentation[] fixedPresentations =
                BattleUnitActionPresentation.CreateArray(ActionPresentationCount);

            if (monsterActionPresentations != null)
            {
                int copyCount = Mathf.Min(monsterActionPresentations.Length, fixedPresentations.Length);

                for (int i = 0; i < copyCount; i++)
                    fixedPresentations[i] = monsterActionPresentations[i];
            }

            monsterActionPresentations = fixedPresentations;
        }

        for (int i = 0; i < monsterActionPresentations.Length; i++)
        {
            if (monsterActionPresentations[i] == null)
                monsterActionPresentations[i] = new BattleUnitActionPresentation();
        }
    }

    private List<int> GetAssignedAttackIndices()
    {
        EnsurePlayerSkillPresentations();

        List<int> result = new();

        if (HasPresentation(playerSkillPresentations.attack1))
            result.Add(1);

        if (HasPresentation(playerSkillPresentations.attack2))
            result.Add(2);

        if (HasPresentation(playerSkillPresentations.attack3))
            result.Add(3);

        return result;
    }

    private int GetRandomAssignedAttackIndex()
    {
        int assignedCount = 0;

        EnsurePlayerSkillPresentations();

        if (HasPresentation(playerSkillPresentations.attack1))
            assignedCount++;

        if (HasPresentation(playerSkillPresentations.attack2))
            assignedCount++;

        if (HasPresentation(playerSkillPresentations.attack3))
            assignedCount++;

        if (assignedCount <= 0)
            return Random.Range(1, 4);

        int selected = Random.Range(0, assignedCount);

        if (HasPresentation(playerSkillPresentations.attack1))
        {
            if (selected == 0)
                return 1;

            selected--;
        }

        if (HasPresentation(playerSkillPresentations.attack2))
        {
            if (selected == 0)
                return 2;

            selected--;
        }

        return 3;
    }

    private int GetAssignedAttackIndexOrFallback(int attackIndex)
    {
        EnsurePlayerSkillPresentations();

        switch (attackIndex)
        {
            case 1:
                if (HasPresentation(playerSkillPresentations.attack1))
                    return 1;
                break;

            case 2:
                if (HasPresentation(playerSkillPresentations.attack2))
                    return 2;
                break;

            case 3:
                if (HasPresentation(playerSkillPresentations.attack3))
                    return 3;
                break;
        }

        if (HasPresentation(playerSkillPresentations.attack1))
            return 1;

        if (HasPresentation(playerSkillPresentations.attack2))
            return 2;

        if (HasPresentation(playerSkillPresentations.attack3))
            return 3;

        return Mathf.Clamp(attackIndex, 1, 3);
    }

    private bool HasVfx(BattleVfxEntry entry)
    {
        return entry != null && entry.prefab != null;
    }

    private bool HasProjectileVfx(BattleProjectileVfxEntry entry)
    {
        return entry != null && (entry.missilePrefab != null || entry.impactPrefab != null);
    }

    private bool ShouldSpawnProjectileImpactOnMonsterTargetGrids(
        BattleUnitActionPresentation presentation)
    {
        return presentation != null &&
               presentation.spawnVfxOnEachTargetGrid &&
               presentation.vfx?.prefab == null &&
               presentation.projectileVfx != null &&
               presentation.projectileVfx.missilePrefab == null &&
               presentation.projectileVfx.impactPrefab != null;
    }

    private bool HasPresentation(BattleUnitActionPresentation presentation)
    {
        return presentation != null &&
               (!string.IsNullOrWhiteSpace(presentation.stateName) || HasVfx(presentation.vfx));
    }

    private void SpawnVfx(BattleVfxEntry entry)
    {
        SpawnVfx(entry, null);
    }

    private bool TrySpawnVfxOnMonsterTargetGrids(
        BattleVfxEntry entry,
        MonsterReservedCommand command)
    {
        if (entry == null || entry.prefab == null || command == null)
            return false;

        IReadOnlyList<int> targetGridIndices = GetMonsterPresentationVfxGridIndices(command);

        if (targetGridIndices == null || targetGridIndices.Count <= 0)
            return false;

        GridManager manager = ResolveGridManager();

        if (manager == null)
            return false;

        BattleVfxEntry targetGridEntry = CreateTargetGridVfxEntry(entry);
        bool spawnedAny = false;
        HashSet<int> spawnedGridIndices = new();

        for (int i = 0; i < targetGridIndices.Count; i++)
        {
            int gridIndex = targetGridIndices[i];

            if (gridIndex < 0 || !spawnedGridIndices.Add(gridIndex))
                continue;

            if (!TryResolveMonsterPresentationVfxAnchor(manager, gridIndex, out Vector3 anchorPosition))
                continue;

            SpawnDetachedVfx(
                targetGridEntry,
                anchorPosition,
                vfxLifeTime,
                applyFacingFlip: false);
            spawnedAny = true;
        }

        return spawnedAny;
    }

    private bool TrySpawnProjectileImpactOnMonsterTargetGrids(
        BattleProjectileVfxEntry entry,
        MonsterReservedCommand command)
    {
        if (entry == null || entry.missilePrefab != null || entry.impactPrefab == null || command == null)
            return false;

        IReadOnlyList<int> targetGridIndices = GetMonsterPresentationVfxGridIndices(command);

        if (targetGridIndices == null || targetGridIndices.Count <= 0)
            return false;

        GridManager manager = ResolveGridManager();

        if (manager == null)
            return false;

        if (vfxLayer < 0)
            vfxLayer = LayerMask.NameToLayer(vfxLayerName);

        BattleVfxEntry targetGridImpactEntry = CreateTargetGridImpactVfxEntry(entry);
        bool spawnedAny = false;
        HashSet<int> spawnedGridIndices = new();

        for (int i = 0; i < targetGridIndices.Count; i++)
        {
            int gridIndex = targetGridIndices[i];

            if (gridIndex < 0 || !spawnedGridIndices.Add(gridIndex))
                continue;

            if (!TryResolveMonsterPresentationVfxAnchor(manager, gridIndex, out Vector3 anchorPosition))
                continue;

            Vector3 impactPosition = ResolveTargetGridImpactPosition(
                anchorPosition,
                entry.impactOffset);

            SpawnDetachedVfx(
                targetGridImpactEntry,
                impactPosition,
                Mathf.Max(0.01f, entry.impactLifeTime),
                applyFacingFlip: false,
                stabilizeVisualEffects: true);
            spawnedAny = true;
        }

        return spawnedAny;
    }

    private static bool TryResolveMonsterPresentationVfxAnchor(
        GridManager manager,
        int gridIndex,
        out Vector3 anchorPosition)
    {
        anchorPosition = Vector3.zero;

        if (manager == null || gridIndex < 0)
            return false;

        GridCell cell = manager.GetCellByIndex(gridIndex);

        if (cell == null)
            return false;

        anchorPosition = cell.transform.position;
        return true;
    }

    private static Vector3 ResolveTargetGridImpactPosition(
        Vector3 targetWorldPosition,
        Vector3 impactOffset)
    {
        return targetWorldPosition + impactOffset;
    }

    private static IReadOnlyList<int> GetMonsterPresentationVfxGridIndices(
        MonsterReservedCommand command)
    {
        if (command == null)
            return null;

        if (command.TargetGridIndices != null && command.TargetGridIndices.Count > 0)
            return command.TargetGridIndices;

        return command.RangeGridIndices;
    }

    private void SpawnVfx(BattleVfxEntry entry, PlayerReservedCommand command)
    {
        if (entry == null || entry.prefab == null)
            return;

        if (vfxLayer < 0)
            vfxLayer = LayerMask.NameToLayer(vfxLayerName);

        Transform spawn = GetVfxSpawnTransform();

        if (TrySpawnWorldVfx(entry, spawn, vfxLifeTime))
            return;

        if (TrySpawnDirectWorldVfx(entry, spawn, vfxLifeTime))
            return;

        GameObject vfx = Instantiate(entry.prefab, spawn, false);

        ConfigureVfxInstance(vfx, entry);
        ApplyDirectWorldVfxSorting(vfx, entry, GetUnitVfxSortingReferenceY());

        Destroy(vfx, vfxLifeTime);
    }

    private bool TryResolveSelectedGridWorldPosition(
        PlayerReservedCommand command,
        out Vector3 targetWorldPosition)
    {
        targetWorldPosition = Vector3.zero;

        if (command == null || command.SelectedGridIndex < 0)
            return false;

        GridManager manager = ResolveGridManager();

        if (manager == null)
            return false;

        Vector2Int coord = manager.IndexToCoord(command.SelectedGridIndex);
        if (!manager.IsValidCoord(coord))
            return false;

        GridCell cell = manager.GetCell(coord);
        if (cell == null)
            return false;

        targetWorldPosition = cell.transform.position;
        return true;
    }

    private GridManager ResolveGridManager()
    {
        if (gridManager != null)
            return gridManager;

        gridManager = Object.FindFirstObjectByType<GridManager>(FindObjectsInactive.Include);
        return gridManager;
    }

    private void SpawnDetachedVfx(
        BattleVfxEntry entry,
        Vector3 anchorWorldPosition,
        float lifeTime)
    {
        SpawnDetachedVfx(
            entry,
            anchorWorldPosition,
            lifeTime,
            applyFacingFlip: true);
    }

    private void SpawnDetachedVfx(
        BattleVfxEntry entry,
        Vector3 anchorWorldPosition,
        float lifeTime,
        bool applyFacingFlip,
        bool stabilizeVisualEffects = false)
    {
        if (TrySpawnDetachedWorldVfx(
                entry,
                anchorWorldPosition,
                lifeTime,
                useUnitSortingTarget: false,
                applyFacingFlip: applyFacingFlip,
                stabilizeVisualEffects: stabilizeVisualEffects,
                out _))
        {
            return;
        }

        if (TrySpawnDetachedDirectWorldVfx(entry, anchorWorldPosition, lifeTime, applyFacingFlip, stabilizeVisualEffects))
        {
            return;
        }

        SpawnDetachedPrefabVfx(entry, anchorWorldPosition, lifeTime, applyFacingFlip, stabilizeVisualEffects);
    }

    private bool TrySpawnDetachedDirectWorldVfx(
        BattleVfxEntry entry,
        Vector3 anchorWorldPosition,
        float lifeTime,
        bool applyFacingFlip,
        bool stabilizeVisualEffects)
    {
        if (entry.renderMode != BattleVfxRenderMode.DirectWorldRenderer)
            return false;

        GameObject anchor = CreateDetachedVfxAnchor(entry, anchorWorldPosition);
        GameObject vfx = Instantiate(entry.prefab, anchor.transform, false);

        ConfigureDirectWorldVfxInstance(vfx, entry, anchor.transform.position.y, applyFacingFlip);

        if (stabilizeVisualEffects)
            StabilizeVisualEffectPlayback(vfx);

        Destroy(anchor, Mathf.Max(0.01f, lifeTime));
        return true;
    }

    private void SpawnDetachedPrefabVfx(
        BattleVfxEntry entry,
        Vector3 anchorWorldPosition,
        float lifeTime,
        bool applyFacingFlip,
        bool stabilizeVisualEffects)
    {
        GameObject anchor = CreateDetachedVfxAnchor(entry, anchorWorldPosition);
        GameObject vfx = Instantiate(entry.prefab, anchor.transform, false);

        ConfigureDirectWorldVfxInstance(vfx, entry, anchor.transform.position.y, applyFacingFlip);

        if (stabilizeVisualEffects)
            StabilizeVisualEffectPlayback(vfx);

        if (entry.renderMode == BattleVfxRenderMode.IndividualWorldRenderTexture)
        {
            Transform spawn = GetVfxSpawnTransform();
            int visibleLayer = spawn != null ? spawn.gameObject.layer : 0;
            SetLayerRecursively(vfx, visibleLayer);
        }

        Destroy(anchor, Mathf.Max(0.01f, lifeTime));
    }

    private static GameObject CreateDetachedVfxAnchor(
        BattleVfxEntry entry,
        Vector3 anchorWorldPosition)
    {
        string anchorName = entry != null && entry.prefab != null
            ? $"{entry.prefab.name}_VfxAnchor"
            : "SkillVfx_VfxAnchor";
        GameObject anchor = new(anchorName);
        anchor.transform.position = anchorWorldPosition + (entry != null ? entry.proxyWorldOffset : Vector3.zero);
        return anchor;
    }

    private IEnumerator PlayProjectileVfx(
        BattleProjectileVfxEntry entry,
        Vector3 targetWorldPosition)
    {
        if (!HasProjectileVfx(entry))
            yield break;

        if (entry.launchDelay > 0f)
            yield return WaitForVfxPlaybackDelay(entry.launchDelay);

        if (vfxLayer < 0)
            vfxLayer = LayerMask.NameToLayer(vfxLayerName);

        Transform spawn = GetVfxSpawnTransform();
        Vector3 startPosition = spawn.position;

        if (entry.missilePrefab == null)
        {
            SpawnImpactVfx(
                entry,
                ResolveProjectileImpactPosition(
                    targetWorldPosition,
                    entry.impactOffset,
                    startPosition.z));
            yield break;
        }

        BattleVfxEntry missileEntry = CreateRuntimeVfxEntry(
            entry.missilePrefab,
            entry.missileFlipType);

        if (TrySpawnDetachedWorldVfx(
                missileEntry,
                startPosition,
                Mathf.Max(0.01f, entry.travelDuration + 0.5f),
                out BattleWorldVfxHandle missileHandle))
        {
            startPosition += entry.launchOffset;
            missileHandle.SetWorldPosition(startPosition);
            Vector3 worldImpactPosition = ResolveProjectileImpactPosition(
                targetWorldPosition,
                entry.impactOffset,
                startPosition.z);

            yield return MoveProjectileVfx(
                missileHandle.transform,
                startPosition,
                worldImpactPosition,
                entry.travelDuration,
                entry.arrivalDistance);

            if (missileHandle != null)
                Destroy(missileHandle.gameObject);

            SpawnImpactVfx(entry, worldImpactPosition);
            yield break;
        }

        GameObject missile = Instantiate(entry.missilePrefab, spawn, false);
        missile.transform.localPosition = Vector3.zero;

        ConfigureVfxInstance(missile, missileEntry);
        ApplyDirectWorldVfxSorting(missile, missileEntry, GetUnitVfxSortingReferenceY());
        missile.transform.SetParent(null, true);
        missile.transform.position += entry.launchOffset;

        startPosition = missile.transform.position;
        Vector3 impactPosition = ResolveProjectileImpactPosition(
            targetWorldPosition,
            entry.impactOffset,
            startPosition.z);

        yield return MoveProjectileVfx(
            missile.transform,
            startPosition,
            impactPosition,
            entry.travelDuration,
            entry.arrivalDistance);

        if (missile != null)
            Destroy(missile);

        SpawnImpactVfx(entry, impactPosition);
    }

    private static Vector3 ResolveProjectileImpactPosition(
        Vector3 targetWorldPosition,
        Vector3 impactOffset,
        float projectileZ)
    {
        Vector3 impactPosition = targetWorldPosition + impactOffset;
        impactPosition.z = projectileZ;
        return impactPosition;
    }

    private IEnumerator MoveProjectileVfx(
        Transform projectile,
        Vector3 startPosition,
        Vector3 targetPosition,
        float duration,
        float arrivalDistance)
    {
        if (projectile == null)
            yield break;

        float safeArrivalDistance = Mathf.Max(0f, arrivalDistance);

        if (duration <= 0f)
        {
            projectile.position = targetPosition;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (projectile == null)
                yield break;

            elapsed += GetVfxPlaybackDeltaTime();

            float t = Mathf.Clamp01(elapsed / duration);
            projectile.position = Vector3.Lerp(startPosition, targetPosition, t);

            if (safeArrivalDistance > 0f &&
                Vector3.Distance(projectile.position, targetPosition) <= safeArrivalDistance)
            {
                break;
            }

            yield return null;
        }

        if (projectile != null)
            projectile.position = targetPosition;
    }

    private static IEnumerator WaitForVfxPlaybackDelay(float delay)
    {
        if (delay <= 0f)
            yield break;

        float elapsed = 0f;

        while (elapsed < delay)
        {
            elapsed += GetVfxPlaybackDeltaTime();

            yield return null;
        }
    }

    private static float GetVfxPlaybackDeltaTime()
    {
        float deltaTime = Time.deltaTime;

        return BattleVfxPlaybackPauseController.IsGlobalPauseActive
            ? deltaTime * BattleVfxPlaybackPauseController.ActiveSpeedMultiplier
            : deltaTime;
    }

    private void SpawnImpactVfx(BattleProjectileVfxEntry entry, Vector3 impactPosition)
    {
        if (entry == null || entry.impactPrefab == null)
            return;

        BattleVfxEntry impactEntry = CreateRuntimeVfxEntry(
            entry.impactPrefab,
            entry.impactFlipType);

        if (TrySpawnDetachedWorldVfx(
                impactEntry,
                impactPosition,
                Mathf.Max(0.01f, entry.impactLifeTime),
                out _))
        {
            return;
        }

        Transform spawn = GetVfxSpawnTransform();
        GameObject impact = Instantiate(entry.impactPrefab, spawn, false);
        impact.transform.localPosition = Vector3.zero;

        ConfigureVfxInstance(impact, impactEntry);
        ApplyDirectWorldVfxSorting(impact, impactEntry, GetUnitVfxSortingReferenceY());
        impact.transform.SetParent(null, true);
        impact.transform.position = impactPosition;

        Destroy(impact, Mathf.Max(0.01f, entry.impactLifeTime));
    }

    private bool TrySpawnWorldVfx(
        BattleVfxEntry entry,
        Transform spawn,
        float lifeTime)
    {
        bool spawned = BattleWorldVfxRenderer.TrySpawn(
            entry,
            spawn,
            vfxLayer,
            Mathf.Max(0.01f, lifeTime),
            vfx => ConfigureVfxInstance(vfx, entry),
            out BattleWorldVfxHandle handle);

        if (spawned)
            ApplyUnitVfxSortingTarget(handle, entry);

        return spawned;
    }

    private bool TrySpawnDirectWorldVfx(
        BattleVfxEntry entry,
        Transform spawn,
        float lifeTime)
    {
        if (entry.renderMode != BattleVfxRenderMode.DirectWorldRenderer)
            return false;

        GameObject vfx = Instantiate(entry.prefab, spawn, false);
        vfx.transform.localPosition += entry.proxyWorldOffset;
        ConfigureDirectWorldVfxInstance(vfx, entry, GetUnitVfxSortingReferenceY());
        Destroy(vfx, Mathf.Max(0.01f, lifeTime));
        return true;
    }

    private void ConfigureDirectWorldVfxInstance(
        GameObject vfx,
        BattleVfxEntry entry,
        float sortingReferenceY)
    {
        ConfigureDirectWorldVfxInstance(
            vfx,
            entry,
            sortingReferenceY,
            applyFacingFlip: true);
    }

    private void ConfigureDirectWorldVfxInstance(
        GameObject vfx,
        BattleVfxEntry entry,
        float sortingReferenceY,
        bool applyFacingFlip)
    {
        ConfigureVfxInstance(vfx, entry, applyFacingFlip);
        ScaleDirectWorldVfxToProxyHeight(vfx, entry);
        ApplyDirectWorldVfxSorting(vfx, entry, sortingReferenceY);
    }

    private static void ScaleDirectWorldVfxToProxyHeight(
        GameObject vfx,
        BattleVfxEntry entry)
    {
        if (vfx == null || entry == null)
            return;

        if (!entry.scaleDirectWorldRendererToProxyHeight)
            return;

        float targetHeight = Mathf.Max(0.01f, entry.proxyWorldHeight);
        if (!TryGetRendererBounds(vfx, out Bounds bounds))
            return;

        float currentHeight = bounds.size.y;
        if (currentHeight <= 0.0001f)
            return;

        float multiplier = targetHeight / currentHeight;
        vfx.transform.localScale = new Vector3(
            vfx.transform.localScale.x * multiplier,
            vfx.transform.localScale.y * multiplier,
            vfx.transform.localScale.z * multiplier);
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;

        if (root == null)
            return false;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return hasBounds;
    }

    private bool TrySpawnDetachedWorldVfx(
        BattleVfxEntry entry,
        Vector3 position,
        float lifeTime,
        out BattleWorldVfxHandle handle)
    {
        return TrySpawnDetachedWorldVfx(
            entry,
            position,
            lifeTime,
            useUnitSortingTarget: true,
            applyFacingFlip: true,
            stabilizeVisualEffects: false,
            out handle);
    }

    private bool TrySpawnDetachedWorldVfx(
        BattleVfxEntry entry,
        Vector3 position,
        float lifeTime,
        bool useUnitSortingTarget,
        bool applyFacingFlip,
        bool stabilizeVisualEffects,
        out BattleWorldVfxHandle handle)
    {
        Transform spawn = GetVfxSpawnTransform();
        int visibleLayer = spawn != null ? spawn.gameObject.layer : 0;

        bool spawned = BattleWorldVfxRenderer.TrySpawnDetached(
            entry,
            position,
            vfxLayer,
            visibleLayer,
            Mathf.Max(0.01f, lifeTime),
            vfx =>
            {
                ConfigureVfxInstance(vfx, entry, applyFacingFlip);

                if (stabilizeVisualEffects)
                    StabilizeVisualEffectPlayback(vfx);
            },
            out handle);

        if (spawned && useUnitSortingTarget)
            ApplyUnitVfxSortingTarget(handle, entry);

        return spawned;
    }

    private BattleVfxEntry CreateRuntimeVfxEntry(GameObject prefab, VfxFlipType flipType)
    {
        return new BattleVfxEntry
        {
            prefab = prefab,
            flipType = flipType
        };
    }

    private static BattleVfxEntry CreateTargetGridVfxEntry(BattleVfxEntry source)
    {
        if (source == null)
            return null;

        return new BattleVfxEntry
        {
            prefab = source.prefab,
            flipType = VfxFlipType.None,
            renderMode = source.renderMode,
            proxyBlendMode = source.proxyBlendMode,
            scaleDirectWorldRendererToProxyHeight = source.scaleDirectWorldRendererToProxyHeight,
            renderTextureWidth = source.renderTextureWidth,
            renderTextureHeight = source.renderTextureHeight,
            renderCameraOrthographicSize = source.renderCameraOrthographicSize,
            proxyWorldHeight = source.proxyWorldHeight,
            proxyWorldOffset = source.proxyWorldOffset,
            proxySortingLayerName = source.proxySortingLayerName,
            proxySortingOrderOffset = source.proxySortingOrderOffset,
            proxySortingWorldYOffset = source.proxySortingWorldYOffset,
            proxyYMultiplier = source.proxyYMultiplier
        };
    }

    private static BattleVfxEntry CreateTargetGridImpactVfxEntry(BattleProjectileVfxEntry source)
    {
        if (source == null)
            return null;

        return new BattleVfxEntry
        {
            prefab = source.impactPrefab,
            flipType = VfxFlipType.None,
            renderMode = BattleVfxRenderMode.IndividualWorldRenderTexture
        };
    }

    private void ApplyUnitVfxSortingTarget(BattleWorldVfxHandle handle, BattleVfxEntry entry)
    {
        if (handle == null || entry == null)
            return;

        Transform reference = GetVfxSortingReferenceTransform();

        if (reference == null)
            return;

        handle.SetSortingTarget(
            reference,
            vfxSortingReferenceYOffset + entry.proxySortingWorldYOffset);
    }

    private void ConfigureVfxInstance(GameObject vfx, BattleVfxEntry entry)
    {
        ConfigureVfxInstance(vfx, entry, applyFacingFlip: true);
    }

    private void ConfigureVfxInstance(GameObject vfx, BattleVfxEntry entry, bool applyFacingFlip)
    {
        if (vfxLayer >= 0)
            SetLayerRecursively(vfx, vfxLayer);

        EnsureVfxPauseController(vfx);
        if (applyFacingFlip)
            ApplyVfxFlip(vfx, entry.flipType);
        BattleVfxAudioUtility.PlayAndStripEmbeddedAudioSources(vfx, entry.prefab, this);
    }

    private static void StabilizeVisualEffectPlayback(GameObject vfx)
    {
        if (vfx == null)
            return;

        VisualEffect[] visualEffects = vfx.GetComponentsInChildren<VisualEffect>(true);

        for (int i = 0; i < visualEffects.Length; i++)
        {
            VisualEffect visualEffect = visualEffects[i];
            visualEffect.resetSeedOnPlay = false;
            visualEffect.Reinit();
        }
    }

    private static void EnsureVfxPauseController(GameObject vfx)
    {
        if (vfx == null)
            return;

        if (vfx.GetComponent<BattleVfxPlaybackPauseController>() == null)
            vfx.AddComponent<BattleVfxPlaybackPauseController>();
    }

    private float GetUnitVfxSortingReferenceY()
    {
        Transform reference = GetVfxSortingReferenceTransform();
        float referenceY = reference != null ? reference.position.y : transform.position.y;
        return referenceY + vfxSortingReferenceYOffset;
    }

    private Transform GetVfxSortingReferenceTransform()
    {
        if (vfxSortingReference != null)
            return vfxSortingReference;

        if (!string.IsNullOrWhiteSpace(vfxSortingReferenceName))
            vfxSortingReference = FindVfxSortingReferenceTransform(vfxSortingReferenceName);

        return vfxSortingReference != null ? vfxSortingReference : transform;
    }

    private Transform FindVfxSortingReferenceTransform(string targetName)
    {
        Transform found = FindChildRecursive(transform, targetName);

        if (found != null)
            return found;

        Transform current = transform.parent;

        while (current != null)
        {
            Transform directChild = current.Find(targetName);

            if (directChild != null)
                return directChild;

            current = current.parent;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (string.Equals(root.name, targetName, System.StringComparison.Ordinal))
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindChildRecursive(child, targetName);

            if (found != null)
                return found;
        }

        return null;
    }

    private void ApplyDirectWorldVfxSorting(
        GameObject vfx,
        BattleVfxEntry entry,
        float y)
    {
        Renderer[] renderers = vfx.GetComponentsInChildren<Renderer>(true);
        int baseOrder = BattleWorldVfxSortUtility.CalculateSortingOrder(
            y + entry.proxySortingWorldYOffset,
            entry.proxyYMultiplier,
            entry.proxySortingOrderOffset);

        for (int i = 0; i < renderers.Length; i++)
        {
            int prefabOrderOffset = renderers[i].sortingOrder;

            if (!string.IsNullOrWhiteSpace(entry.proxySortingLayerName))
                renderers[i].sortingLayerName = entry.proxySortingLayerName;

            renderers[i].sortingOrder = baseOrder + prefabOrderOffset;
        }
    }

    private Transform GetVfxSpawnTransform()
    {
        return vfxSpawnPoint != null ? vfxSpawnPoint : transform;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }


    private void ApplyVfxFlip(GameObject vfx, VfxFlipType flipType)
    {
        if (vfx == null)
            return;

        if (!ShouldFlipVfx())
            return;

        if (flipVfxLocalPositionX)
            FlipLocalPositionX(vfx.transform);

        switch (flipType)
        {
            case VfxFlipType.None:
                if (flipVfxScaleXWhenFlipTypeNone)
                    FlipLocalScaleX(vfx.transform);
                break;

            case VfxFlipType.RotationY180:
                AddLocalRotationY(vfx.transform, 180f);
                break;

            case VfxFlipType.ParticleRendererFlipY:
                FlipParticleRendererY(vfx);
                break;
        }
    }

    private void FlipLocalPositionX(Transform target)
    {
        Vector3 pos = target.localPosition;
        pos.x *= -1f;
        target.localPosition = pos;
    }

    private void FlipLocalScaleX(Transform target)
    {
        if (target == null)
            return;

        Vector3 scale = target.localScale;
        scale.x *= -1f;
        target.localScale = scale;
    }

    private void AddLocalRotationY(Transform target, float amount)
    {
        Vector3 euler = target.localEulerAngles;
        euler.y += amount;
        target.localEulerAngles = euler;
    }

    private void FlipParticleRendererY(GameObject vfx)
    {
        ParticleSystemRenderer[] renderers =
            vfx.GetComponentsInChildren<ParticleSystemRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Vector3 flip = renderers[i].flip;
            flip.y = 1f - flip.y;
            renderers[i].flip = flip;
        }
    }
    private void PlayState(string stateName)
    {
        if (!TryResolveAnimatorStateName(stateName, out string resolvedStateName))
            return;

        PlayAnimatorState(resolvedStateName);
    }

    private void PlayOptionalState(string stateName)
    {
        if (!TryResolveAnimatorStateName(stateName, out string resolvedStateName))
            return;

        PlayAnimatorState(resolvedStateName);
    }

    private bool CanPlayOptionalState(string stateName)
    {
        return TryResolveAnimatorStateName(stateName, out _);
    }

    /// <summary>
    /// 지정한 애니메이터 상태가 실제 컨트롤러에 존재하는지 확인합니다.
    /// 로비 프리뷰처럼 전투용 상태가 없는 Animator에서는 재생을 건너뛰어
    /// Animator.GotoState 경고가 발생하지 않도록 합니다.
    /// </summary>
    private bool TryResolveAnimatorStateName(string stateName, out string resolvedStateName)
    {
        resolvedStateName = null;

        if (!EnsureAnimator())
            return false;

        if (string.IsNullOrWhiteSpace(stateName))
            return false;

        if (animator.runtimeAnimatorController == null)
            return false;

        if (animatorLayer < 0 || animatorLayer >= animator.layerCount)
            return false;

        int shortStateHash = Animator.StringToHash(stateName);
        if (animator.HasState(animatorLayer, shortStateHash))
        {
            resolvedStateName = stateName;
            return true;
        }

        string layerName = animator.GetLayerName(animatorLayer);
        if (string.IsNullOrWhiteSpace(layerName))
            return false;

        string fullStateName = $"{layerName}.{stateName}";
        int fullStateHash = Animator.StringToHash(fullStateName);
        if (!animator.HasState(animatorLayer, fullStateHash))
            return false;

        resolvedStateName = fullStateName;
        return true;
    }

    private void PlayAnimatorState(string stateName)
    {
        if (crossFadeDuration > 0f)
            animator.CrossFadeInFixedTime(stateName, crossFadeDuration, animatorLayer, 0f);
        else
            animator.Play(stateName, animatorLayer, 0f);

        if (forceAnimatorUpdate)
            animator.Update(0f);
    }

    private bool EnsureAnimator()
    {
        FindAnimatorIfNeeded();
        return animator != null;
    }

    private void FindAnimatorIfNeeded()
    {
        if (animator != null)
            return;

        if (!autoFindAnimatorInChildren)
            return;

        animator = GetComponentInChildren<Animator>(true);
    }

    private void FindFacingIfNeeded()
    {
        if (unitFacing != null)
            return;

        if (!autoFindFacing)
            return;

        unitFacing = GetComponent<BattleUnitFacing>();

        if (unitFacing != null)
            return;

        unitFacing = GetComponentInParent<BattleUnitFacing>();

        if (unitFacing != null)
            return;

        unitFacing = GetComponentInChildren<BattleUnitFacing>(true);
    }

    private bool ShouldFlipVfx()
    {
        FindFacingIfNeeded();

        if (unitFacing == null)
            return false;

        return !unitFacing.IsFacingRight;
    }

    public void PlayAttackAction(int attackIndex)
    {
        currentAttackIndex = Mathf.Clamp(attackIndex, 1, 3);
        PlayCurrentAttackAction();
    }
}
