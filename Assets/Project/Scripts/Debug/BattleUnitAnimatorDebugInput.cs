using UnityEngine;

public class BattleUnitAnimatorDebugInput : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private BattleUnitAnimator targetAnimator;

    [Header("Debug Keys")]
    [SerializeField] private KeyCode moveKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode hitKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode guardKey = KeyCode.Alpha3;
    [SerializeField] private KeyCode attack1Key = KeyCode.Alpha4;
    [SerializeField] private KeyCode attack2Key = KeyCode.Alpha5;
    [SerializeField] private KeyCode attack3Key = KeyCode.Alpha6;

    [Header("Facing Debug")]
    [SerializeField] private BattleUnitFacing targetFacing;
    [SerializeField] private KeyCode flipKey = KeyCode.Z;
    private void Awake()
    {
        if (targetAnimator == null)
            targetAnimator = GetComponent<BattleUnitAnimator>();

        if (targetFacing == null)
            targetFacing = GetComponent<BattleUnitFacing>();
    }

    private void Update()
    {
        if (targetAnimator == null)
            return;

        if (Input.GetKeyDown(flipKey))
        {
            targetFacing?.FlipOnce();
            return;
        }

        if (Input.GetKeyDown(moveKey))
        {
            FlipDebug();
            targetAnimator.PlayMove();
            return;
        }

        if (Input.GetKeyDown(hitKey))
        {
            FlipDebug();
            targetAnimator.PlayHit();
            return;
        }

        if (Input.GetKeyDown(guardKey))
        {
            FlipDebug();
            targetAnimator.PlayGuard();
            return;
        }

        if (Input.GetKeyDown(attack1Key))
        {
            FlipDebug();
            targetAnimator.PlayAttackAction(1);
            return;
        }

        if (Input.GetKeyDown(attack2Key))
        {
            FlipDebug();
            targetAnimator.PlayAttackAction(2);
            return;
        }

        if (Input.GetKeyDown(attack3Key))
        {
            FlipDebug();
            targetAnimator.PlayAttackAction(3);
            return;
        }
    }

    private void FlipDebug()
    {
        if (targetFacing == null)
            return;

        targetFacing.FlipOnce();
    }
}