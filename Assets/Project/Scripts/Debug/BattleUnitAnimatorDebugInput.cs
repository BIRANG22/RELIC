using Relic.Gameplay.Data;
using UnityEngine;

public class BattleUnitAnimatorDebugInput : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private BattleUnitAnimator targetAnimator;

    [Header("Debug Keys")]
    [SerializeField] private KeyCode moveKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode hitKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode guardKey = KeyCode.Alpha3;
    [SerializeField] private KeyCode attackKey = KeyCode.Alpha4;

    [Header("Attack Option")]
    [SerializeField] private bool useRandomAttack = true;

    private void Awake()
    {
        if (targetAnimator == null)
            targetAnimator = GetComponent<BattleUnitAnimator>();
    }

    private void Update()
    {
        if (targetAnimator == null)
            return;

        if (Input.GetKeyDown(moveKey))
        {
            targetAnimator.PlayMove();
            return;
        }

        if (Input.GetKeyDown(hitKey))
        {
            targetAnimator.PlayHit();
            return;
        }

        if (Input.GetKeyDown(guardKey))
        {
            targetAnimator.PlayGuard();
            return;
        }

        if (Input.GetKeyDown(attackKey))
        {
            if (useRandomAttack)
                targetAnimator.PlayRandomAttackAction();
            else
            {
                targetAnimator.PlayRandomAttackReady();
                targetAnimator.PlayCurrentAttackAction();
            }

            return;
        }
    }
}