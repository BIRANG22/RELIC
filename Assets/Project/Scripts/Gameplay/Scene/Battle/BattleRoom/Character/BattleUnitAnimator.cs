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

    [SerializeField] private string attackReady1StateName = "AttackReady1";
    [SerializeField] private string attackAction1StateName = "AttackAction1";
    [SerializeField] private string attackReady2StateName = "AttackReady2";
    [SerializeField] private string attackAction2StateName = "AttackAction2";
    [SerializeField] private string attackReady3StateName = "AttackReady3";
    [SerializeField] private string attackAction3StateName = "AttackAction3";

    [Header("Move VFX")]
    [SerializeField] private BattleVfxEntry moveVfx;

    [Header("Guard VFX")]
    [SerializeField] private BattleVfxEntry guardVfx;

    [Header("Hit VFX")]
    [SerializeField] private BattleVfxEntry hitVfx;

    [Header("Attack VFX")]
    [SerializeField] private BattleVfxEntry attackVfx1;
    [SerializeField] private BattleVfxEntry attackVfx2;
    [SerializeField] private BattleVfxEntry attackVfx3;
    [SerializeField] private Transform vfxSpawnPoint;
    [SerializeField] private bool parentVfxToSpawnPoint = true;

    [SerializeField] private float vfxLifeTime = 2f;

    [Header("Setting")]
    [SerializeField] private int animatorLayer = 0;
    [SerializeField] private float crossFadeDuration = 0f;
    [SerializeField] private bool forceAnimatorUpdate = true;
    [SerializeField] private bool autoFindAnimatorInChildren = true;

    [SerializeField] private string vfxLayerName = "VFX";
    private int vfxLayer = -1;

    private int currentAttackIndex = 1;

    private void Awake()
    {
        vfxLayer = LayerMask.NameToLayer(vfxLayerName);

        FindAnimatorIfNeeded();
        PlayIdle();
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

    public void PlaySkillReady(SkillMasterData skillData)
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

        if (skillData.SkillType == SkillType.Power)
        {
            PlayGuard();
            return;
        }

        PlayRandomAttackReady();
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

        if (skillData.SkillType == SkillType.Power)
        {
            PlayGuard();
            return;
        }

        PlayCurrentAttackAction();
    }

    public void PlayRandomAttackReady()
    {
        currentAttackIndex = Random.Range(1, 4);

        switch (currentAttackIndex)
        {
            case 1:
                PlayState(attackReady1StateName);
                break;
            case 2:
                PlayState(attackReady2StateName);
                break;
            case 3:
                PlayState(attackReady3StateName);
                break;
        }
    }

    public void PlayCurrentAttackAction()
    {
        switch (currentAttackIndex)
        {
            case 1:
                PlayState(attackAction1StateName);
                SpawnVfx(attackVfx1);
                break;
            case 2:
                PlayState(attackAction2StateName);
                SpawnVfx(attackVfx2);
                break;
            case 3:
                PlayState(attackAction3StateName);
                SpawnVfx(attackVfx3);
                break;
            default:
                PlayState(attackAction1StateName);
                SpawnVfx(attackVfx1);
                break;
        }
    }

    public void PlayRandomAttackAction()
    {
        currentAttackIndex = Random.Range(1, 4);
        PlayCurrentAttackAction();
    }

    private void SpawnVfx(BattleVfxEntry entry)
    {
        if (entry == null || entry.prefab == null)
            return;

        if (vfxLayer < 0)
            vfxLayer = LayerMask.NameToLayer(vfxLayerName);

        Transform spawn = vfxSpawnPoint != null ? vfxSpawnPoint : transform;

        GameObject vfx = Instantiate(entry.prefab, spawn, false);

        if (vfxLayer >= 0)
            SetLayerRecursively(vfx, vfxLayer);

        ApplyVfxFlip(vfx, entry.flipType);

        Destroy(vfx, vfxLifeTime);
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

        FlipLocalPositionX(vfx.transform);

        switch (flipType)
        {
            case VfxFlipType.None:
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

    private bool ShouldFlipVfx()
    {
        BattleUnitFacing facing = GetComponent<BattleUnitFacing>();

        if (facing == null)
            return false;

        return !facing.IsFacingRight;
    }
}