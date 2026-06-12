using Relic.Gameplay.Data;
using UnityEngine;

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

    [Header("Setting")]
    [SerializeField] private int animatorLayer = 0;
    [SerializeField] private float crossFadeDuration = 0f;
    [SerializeField] private bool forceAnimatorUpdate = true;
    [SerializeField] private bool autoFindAnimatorInChildren = true;

    private int currentAttackIndex = 1;

    private void Awake()
    {
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
    }

    public void PlayGuard()
    {
        PlayState(guardStateName);
    }

    public void PlayHit()
    {
        PlayState(hitStateName);
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

    public void PlayRandomAttackAction()
    {
        currentAttackIndex = Random.Range(1, 4);
        PlayCurrentAttackAction();
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
}