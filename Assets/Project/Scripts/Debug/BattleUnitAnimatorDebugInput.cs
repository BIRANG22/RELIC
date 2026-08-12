using System;
using UnityEngine;

public enum BattleUnitAnimatorDebugAction
{
    None,
    Idle,
    Move,
    Hit,
    Guard,
    Heal,
    Dead,
    Attack1,
    Attack2,
    Attack3,
    MonsterAction1,
    MonsterAction2,
    MonsterAction3,
    MonsterAction4,
    MonsterAction5,
    MonsterAction6,
    MonsterAction7,
    MonsterAction8,
    MonsterAction9,
    MonsterAction10,
    Flip
}

[Serializable]
public class BattleUnitAnimatorDebugBinding
{
    [SerializeField] private KeyCode key = KeyCode.None;
    [SerializeField] private BattleUnitAnimatorDebugAction action = BattleUnitAnimatorDebugAction.None;
    [SerializeField] private bool flipBeforeAction;

    public KeyCode Key => key;
    public BattleUnitAnimatorDebugAction Action => action;
    public bool FlipBeforeAction => flipBeforeAction;

    public BattleUnitAnimatorDebugBinding()
    {
    }

    public BattleUnitAnimatorDebugBinding(
        KeyCode key,
        BattleUnitAnimatorDebugAction action,
        bool flipBeforeAction = false)
    {
        this.key = key;
        this.action = action;
        this.flipBeforeAction = flipBeforeAction;
    }
}

public class BattleUnitAnimatorDebugInput : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private BattleUnitAnimator targetAnimator;
    [SerializeField] private BattleUnitFacing targetFacing;

    [Header("Debug Bindings")]
    [Tooltip("각 항목마다 입력 키와 실행할 디버그 액션을 지정합니다.")]
    [SerializeField] private BattleUnitAnimatorDebugBinding[] debugBindings = CreateDefaultBindings();

    private void Awake()
    {
        ResolveTargets();
    }

    private void Reset()
    {
        ResolveTargets();
        debugBindings = CreateDefaultBindings();
    }

    private void OnValidate()
    {
        if (debugBindings == null)
            debugBindings = Array.Empty<BattleUnitAnimatorDebugBinding>();
    }

    private void Update()
    {
        if (targetAnimator == null)
            ResolveTargets();

        if (debugBindings == null || debugBindings.Length == 0)
            return;

        for (int i = 0; i < debugBindings.Length; i++)
        {
            BattleUnitAnimatorDebugBinding binding = debugBindings[i];

            if (binding == null || binding.Key == KeyCode.None)
                continue;

            if (!Input.GetKeyDown(binding.Key))
                continue;

            ExecuteAction(binding);
            break;
        }
    }

    private void ResolveTargets()
    {
        if (targetAnimator == null)
            targetAnimator = GetComponent<BattleUnitAnimator>();

        if (targetFacing == null)
            targetFacing = GetComponent<BattleUnitFacing>();
    }

    private void ExecuteAction(BattleUnitAnimatorDebugBinding binding)
    {
        if (binding == null)
            return;

        if (binding.FlipBeforeAction && binding.Action != BattleUnitAnimatorDebugAction.Flip)
            targetFacing?.FlipOnce();

        switch (binding.Action)
        {
            case BattleUnitAnimatorDebugAction.None:
                break;

            case BattleUnitAnimatorDebugAction.Idle:
                targetAnimator?.PlayIdle();
                break;

            case BattleUnitAnimatorDebugAction.Move:
                targetAnimator?.PlayMove();
                break;

            case BattleUnitAnimatorDebugAction.Hit:
                targetAnimator?.PlayHit();
                break;

            case BattleUnitAnimatorDebugAction.Guard:
                targetAnimator?.PlayGuard();
                break;

            case BattleUnitAnimatorDebugAction.Heal:
                targetAnimator?.PlayHeal();
                break;

            case BattleUnitAnimatorDebugAction.Dead:
                targetAnimator?.PlayDead();
                break;

            case BattleUnitAnimatorDebugAction.Attack1:
                targetAnimator?.PlayAttackAction(1);
                break;

            case BattleUnitAnimatorDebugAction.Attack2:
                targetAnimator?.PlayAttackAction(2);
                break;

            case BattleUnitAnimatorDebugAction.Attack3:
                targetAnimator?.PlayAttackAction(3);
                break;

            case BattleUnitAnimatorDebugAction.MonsterAction1:
                targetAnimator?.PlayMonsterActionPresentation(1);
                break;

            case BattleUnitAnimatorDebugAction.MonsterAction2:
                targetAnimator?.PlayMonsterActionPresentation(2);
                break;

            case BattleUnitAnimatorDebugAction.MonsterAction3:
                targetAnimator?.PlayMonsterActionPresentation(3);
                break;

            case BattleUnitAnimatorDebugAction.MonsterAction4:
                targetAnimator?.PlayMonsterActionPresentation(4);
                break;

            case BattleUnitAnimatorDebugAction.MonsterAction5:
                targetAnimator?.PlayMonsterActionPresentation(5);
                break;

            case BattleUnitAnimatorDebugAction.MonsterAction6:
                targetAnimator?.PlayMonsterActionPresentation(6);
                break;

            case BattleUnitAnimatorDebugAction.MonsterAction7:
                targetAnimator?.PlayMonsterActionPresentation(7);
                break;

            case BattleUnitAnimatorDebugAction.MonsterAction8:
                targetAnimator?.PlayMonsterActionPresentation(8);
                break;

            case BattleUnitAnimatorDebugAction.MonsterAction9:
                targetAnimator?.PlayMonsterActionPresentation(9);
                break;

            case BattleUnitAnimatorDebugAction.MonsterAction10:
                targetAnimator?.PlayMonsterActionPresentation(10);
                break;

            case BattleUnitAnimatorDebugAction.Flip:
                targetFacing?.FlipOnce();
                break;
        }
    }

    private static BattleUnitAnimatorDebugBinding[] CreateDefaultBindings()
    {
        return new[]
        {
            new BattleUnitAnimatorDebugBinding(KeyCode.Alpha1, BattleUnitAnimatorDebugAction.Move, true),
            new BattleUnitAnimatorDebugBinding(KeyCode.Alpha2, BattleUnitAnimatorDebugAction.Hit, true),
            new BattleUnitAnimatorDebugBinding(KeyCode.Alpha3, BattleUnitAnimatorDebugAction.Guard, true),
            new BattleUnitAnimatorDebugBinding(KeyCode.Alpha4, BattleUnitAnimatorDebugAction.Attack1, true),
            new BattleUnitAnimatorDebugBinding(KeyCode.Alpha5, BattleUnitAnimatorDebugAction.Attack2, true),
            new BattleUnitAnimatorDebugBinding(KeyCode.Alpha6, BattleUnitAnimatorDebugAction.Attack3, true),
            new BattleUnitAnimatorDebugBinding(KeyCode.Z, BattleUnitAnimatorDebugAction.Flip)
        };
    }
}
