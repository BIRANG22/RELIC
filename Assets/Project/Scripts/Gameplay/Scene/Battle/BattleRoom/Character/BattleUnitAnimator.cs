using Relic.Gameplay.Data;
using UnityEngine;

public class BattleUnitAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Parameters")]
    [SerializeField] private string isMovingParameter = "IsMoving";
    [SerializeField] private string attackTriggerParameter = "Attack";
    [SerializeField] private string hitTriggerParameter = "Hit";
    [SerializeField] private string deadTriggerParameter = "Dead";
    [SerializeField] private string exhaustTriggerParameter = "Exhaust";

    [Header("State Names")]
    [SerializeField] private bool playStateDirectly = true;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string moveStateName = "Move";
    [SerializeField] private string controlStateName = "Control";
    [SerializeField] private string guardStateName = "Guard";
    [SerializeField] private string hitStateName = "Hit";
    [SerializeField] private string deadStateName = "Dead";
    [SerializeField] private string exhaustStateName = "Exhaust";
    [SerializeField] private string attackReady1StateName = "AttackReady1";
    [SerializeField] private string attackAction1StateName = "AttackAction1";
    [SerializeField] private string attackReady2StateName = "AttackReady2";
    [SerializeField] private string attackAction2StateName = "AttackAction2";
    [SerializeField] private string attackReady3StateName = "AttackReady3";
    [SerializeField] private string attackAction3StateName = "AttackAction3";

    [Header("Setting")]
    [SerializeField] private int animatorLayer = 0;
    [SerializeField] private float crossFadeDuration = 0f;
    [SerializeField] private bool forceAnimatorUpdate = false;
    [SerializeField] private bool autoFindAnimatorInChildren = true;

    private int isMovingHash;
    private int attackTriggerHash;
    private int hitTriggerHash;
    private int deadTriggerHash;
    private int exhaustTriggerHash;

    private bool hasIsMoving;
    private bool hasAttackTrigger;
    private bool hasHitTrigger;
    private bool hasDeadTrigger;
    private bool hasExhaustTrigger;

    private void Awake()
    {
        FindAnimatorIfNeeded();
        CacheHashes();
        CacheAvailableParameters();
        PlayIdle();
    }

    public void PlayIdle()
    {
        SetMoving(false);
        PlayState(idleStateName);
    }

    public void PlayMove()
    {
        SetMoving(true);
        PlayState(moveStateName);
    }

    public void PlayControl()
    {
        SetMoving(false);
        PlayState(controlStateName);
    }

    public void PlayGuard()
    {
        SetMoving(false);
        PlayState(guardStateName);
    }

    public void PlayHit()
    {
        SetMoving(false);
        SetTrigger(hitTriggerHash, hasHitTrigger);
        PlayState(hitStateName);
    }

    public void PlayDead()
    {
        SetMoving(false);
        SetTrigger(deadTriggerHash, hasDeadTrigger);
        PlayState(deadStateName);
    }

    public void PlayExhaust()
    {
        SetMoving(false);
        SetTrigger(exhaustTriggerHash, hasExhaustTrigger);
        PlayState(exhaustStateName);
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
            PlayControl();
            return;
        }

        PlayAttackReady(GetAttackIndex(skillData));
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
            PlayControl();
            return;
        }

        PlayAttackAction(GetAttackIndex(skillData));
    }

    public void PlayAttackReady(int attackIndex)
    {
        SetMoving(false);
        SetTrigger(attackTriggerHash, hasAttackTrigger);

        switch (attackIndex)
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
            default:
                PlayState(attackReady1StateName);
                break;
        }
    }

    public void PlayAttackAction(int attackIndex)
    {
        SetMoving(false);
        SetTrigger(attackTriggerHash, hasAttackTrigger);

        switch (attackIndex)
        {
            case 1:
                PlayState(attackAction1StateName);
                break;
            case 2:
                PlayState(attackAction2StateName);
                break;
            case 3:
                PlayState(attackAction3StateName);
                break;
            default:
                PlayState(attackAction1StateName);
                break;
        }
    }

    private int GetAttackIndex(SkillMasterData skillData)
    {
        if (skillData == null)
            return 1;

        if (skillData.Category == Category.Unique)
            return 2;

        if (skillData.Category == Category.Ability ||
            skillData.Category == Category.Essenece)
            return 3;

        return 1;
    }

    private void SetMoving(bool isMoving)
    {
        if (!EnsureAnimator())
            return;

        if (hasIsMoving)
            animator.SetBool(isMovingHash, isMoving);
    }

    private void SetTrigger(int hash, bool available)
    {
        if (!EnsureAnimator())
            return;

        if (!available)
            return;

        animator.ResetTrigger(hash);
        animator.SetTrigger(hash);
    }

    private void PlayState(string stateName)
    {
        if (!playStateDirectly)
            return;

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

    private void CacheHashes()
    {
        isMovingHash = Animator.StringToHash(isMovingParameter);
        attackTriggerHash = Animator.StringToHash(attackTriggerParameter);
        hitTriggerHash = Animator.StringToHash(hitTriggerParameter);
        deadTriggerHash = Animator.StringToHash(deadTriggerParameter);
        exhaustTriggerHash = Animator.StringToHash(exhaustTriggerParameter);
    }

    private void CacheAvailableParameters()
    {
        hasIsMoving = false;
        hasAttackTrigger = false;
        hasHitTrigger = false;
        hasDeadTrigger = false;
        hasExhaustTrigger = false;

        if (animator == null)
            return;

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.nameHash == isMovingHash && parameter.type == AnimatorControllerParameterType.Bool)
                hasIsMoving = true;
            else if (parameter.nameHash == attackTriggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
                hasAttackTrigger = true;
            else if (parameter.nameHash == hitTriggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
                hasHitTrigger = true;
            else if (parameter.nameHash == deadTriggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
                hasDeadTrigger = true;
            else if (parameter.nameHash == exhaustTriggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
                hasExhaustTrigger = true;
        }
    }
}