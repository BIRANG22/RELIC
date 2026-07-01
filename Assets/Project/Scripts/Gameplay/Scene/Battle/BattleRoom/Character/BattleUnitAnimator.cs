using System.Collections;
using Relic.Gameplay.Data;
using UnityEngine;

public enum VfxFlipType
{
    RotationY180,
    ParticleRendererFlipY,
    None
}

public class BattleUnitAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("State Names")]
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string moveStateName = "Move";
    [SerializeField] private string guardStateName = "Guard";
    [SerializeField] private string hitStateName = "Hit";
    [SerializeField] private string deadStateName = "Dead";

    [Header("Move VFX")]
    [SerializeField] private BattleVfxEntry moveVfx;

    [Header("Guard VFX")]
    [SerializeField] private BattleVfxEntry guardVfx;

    [Header("Hit VFX")]
    [SerializeField] private BattleVfxEntry hitVfx;

    [Header("Status VFX")]
    [SerializeField] private BattleStatusVfxSet statusVfx = new();

    [Header("Player Skill Presentations")]
    [SerializeField] private BattleUnitPlayerSkillPresentations playerSkillPresentations = new();

    [Header("Monster Action Presentations")]
    [SerializeField] private BattleUnitActionPresentation[] monsterActionPresentations =
        BattleUnitActionPresentation.CreateArray(10);

    [Header("VFX Spawn")]
    [SerializeField] private Transform vfxSpawnPoint;

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
    }

    public void PlayIdle()
    {
        PlayState(idleStateName);
    }

    public void PlayMove()
    {
        PlayState(moveStateName);
        SpawnVfx(moveVfx);
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
        if (skillData == null)
        {
            PlayIdle();
            return;
        }

        if (skillData.Category == Category.Move)
        {
            PlayMove();
            return;
        }

        switch (skillData.SkillType)
        {
            case SkillType.Power:
                EnsurePlayerSkillPresentations();
                PlayPresentation(playerSkillPresentations.power);
                break;

            case SkillType.Skill:
                EnsurePlayerSkillPresentations();
                PlayPresentation(playerSkillPresentations.skill);
                break;

            case SkillType.Attack:
                PlayRandomAttackAction();
                break;

            default:
                PlayRandomAttackAction();
                break;
        }
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

        if (command.SkillData.TimelineNotation == TimelineActionType.Move)
        {
            PlayMove();
            return;
        }

        if (!IsValidMonsterActionIndex(command.ActionIndex))
        {
            PlayMonsterSkillAction(command.SkillData);
            return;
        }

        PlayPresentation(GetMonsterActionPresentation(command.ActionIndex));
    }

    public bool HasMonsterProjectileVfx(MonsterReservedCommand command)
    {
        return TryGetMonsterProjectilePresentation(command, out BattleUnitActionPresentation _);
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

    private void PlayPresentation(BattleUnitActionPresentation presentation)
    {
        if (presentation == null)
            return;

        PlayState(presentation.stateName);
        SpawnVfx(presentation.vfx);
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
            HasProjectileVfx(mapped.projectileVfx) &&
            ProjectileVfxMatchesSkill(command, mapped.projectileVfx, true))
        {
            presentation = mapped;
            return true;
        }

        EnsureMonsterActionPresentationArray();

        for (int i = 0; i < monsterActionPresentations.Length; i++)
        {
            BattleUnitActionPresentation candidate = monsterActionPresentations[i];

            if (candidate == null)
                continue;

            if (!HasProjectileVfx(candidate.projectileVfx))
                continue;

            if (!ProjectileVfxMatchesSkill(command, candidate.projectileVfx, false))
                continue;

            presentation = candidate;
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
        return entry != null && entry.missilePrefab != null;
    }

    private bool ProjectileVfxMatchesSkill(
        MonsterReservedCommand command,
        BattleProjectileVfxEntry entry,
        bool allowEmptySkillId)
    {
        if (entry == null)
            return false;

        if (string.IsNullOrWhiteSpace(entry.skillId))
            return allowEmptySkillId;

        return command != null &&
               string.Equals(entry.skillId.Trim(), command.SkillId, System.StringComparison.Ordinal);
    }

    private bool HasPresentation(BattleUnitActionPresentation presentation)
    {
        return presentation != null &&
               (!string.IsNullOrWhiteSpace(presentation.stateName) || HasVfx(presentation.vfx));
    }

    private void SpawnVfx(BattleVfxEntry entry)
    {
        if (entry == null || entry.prefab == null)
            return;

        if (vfxLayer < 0)
            vfxLayer = LayerMask.NameToLayer(vfxLayerName);

        Transform spawn = GetVfxSpawnTransform();

        GameObject vfx = Instantiate(entry.prefab, spawn, false);

        if (vfxLayer >= 0)
            SetLayerRecursively(vfx, vfxLayer);

        ApplyVfxFlip(vfx, entry.flipType);

        Destroy(vfx, vfxLifeTime);
    }

    private IEnumerator PlayProjectileVfx(
        BattleProjectileVfxEntry entry,
        Vector3 targetWorldPosition)
    {
        if (entry == null || entry.missilePrefab == null)
            yield break;

        if (entry.launchDelay > 0f)
            yield return new WaitForSeconds(entry.launchDelay);

        if (vfxLayer < 0)
            vfxLayer = LayerMask.NameToLayer(vfxLayerName);

        Transform spawn = GetVfxSpawnTransform();
        GameObject missile = Instantiate(entry.missilePrefab, spawn, false);
        missile.transform.localPosition = Vector3.zero;

        if (vfxLayer >= 0)
            SetLayerRecursively(missile, vfxLayer);

        ApplyVfxFlip(missile, entry.missileFlipType);
        missile.transform.SetParent(null, true);
        missile.transform.position += entry.launchOffset;

        Vector3 startPosition = missile.transform.position;
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

            elapsed += Time.deltaTime;

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

    private void SpawnImpactVfx(BattleProjectileVfxEntry entry, Vector3 impactPosition)
    {
        if (entry == null || entry.impactPrefab == null)
            return;

        Transform spawn = GetVfxSpawnTransform();
        GameObject impact = Instantiate(entry.impactPrefab, spawn, false);
        impact.transform.localPosition = Vector3.zero;

        if (vfxLayer >= 0)
            SetLayerRecursively(impact, vfxLayer);

        ApplyVfxFlip(impact, entry.impactFlipType);
        impact.transform.SetParent(null, true);
        impact.transform.position = impactPosition;

        Destroy(impact, Mathf.Max(0.01f, entry.impactLifeTime));
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
        if (!EnsureAnimator())
            return;

        if (string.IsNullOrWhiteSpace(stateName))
            return;

        if (animator.runtimeAnimatorController == null)
            return;

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
