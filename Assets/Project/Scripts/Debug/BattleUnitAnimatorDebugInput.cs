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

        if (Input.GetKeyDown(attack1Key))
        {
            targetAnimator.PlayAttackAction(1);
            return;
        }

        if (Input.GetKeyDown(attack2Key))
        {
            targetAnimator.PlayAttackAction(2);
            return;
        }

        if (Input.GetKeyDown(attack3Key))
        {
            targetAnimator.PlayAttackAction(3);
            return;
        }
    }
}