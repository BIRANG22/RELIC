using UnityEngine;

/// <summary>
/// 캐릭터 클릭 Reaction과 평상시 랜덤 Idle -> Wait 재생을 함께 관리합니다.
///
/// - Event_Idle을 지정한 최소/최대 횟수 사이에서 랜덤 반복한 뒤 Wait Trigger를 실행합니다.
/// - Wait 애니메이션 재생 중에는 클릭해도 Reaction이 실행되지 않습니다.
/// - Reaction을 실행하면 Idle 반복 카운트를 즉시 0으로 초기화합니다.
/// - Reaction이 끝나고 Idle로 돌아오면 새로운 랜덤 반복 횟수로 다시 계산합니다.
/// </summary>
public class CharacterReaction : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Reaction")]
    [Tooltip("Animator Parameters에 등록한 Reaction Trigger 이름입니다.")]
    [SerializeField] private string reactionTriggerName = "ClickReaction";

    [Tooltip("Reaction 애니메이션 State 이름입니다.")]
    [SerializeField] private string reactionStateName = "reaction";

    [Header("Idle / Wait")]
    [Tooltip("평상시 반복 재생되는 Idle State 이름입니다.")]
    [SerializeField] private string idleStateName = "Event_Idle";

    [Tooltip("랜덤 반복 후 1회 재생할 Wait State 이름입니다.")]
    [SerializeField] private string waitStateName = "wait";

    [Tooltip("Animator Parameters에 등록한 Wait Trigger 이름입니다.")]
    [SerializeField] private string waitTriggerName = "Wait";

    [Tooltip("Wait가 실행되기 전 Idle 최소 반복 횟수입니다.")]
    [Min(1)]
    [SerializeField] private int minIdleRepeat = 5;

    [Tooltip("Wait가 실행되기 전 Idle 최대 반복 횟수입니다.")]
    [Min(1)]
    [SerializeField] private int maxIdleRepeat = 10;

    [Header("Click Input")]
    [Tooltip("켜져 있으면 이 오브젝트의 Collider를 직접 클릭했을 때 반응합니다.\n기존 캐릭터 선택 시스템에서 PlayReaction()을 호출할 경우 꺼두어도 됩니다.")]
    [SerializeField] private bool useOnMouseDown = true;

    [Header("Click Limit")]
    [Tooltip("반응 애니메이션 실행 후 다시 클릭 반응을 허용하기까지의 시간(초)입니다.")]
    [Min(0f)]
    [SerializeField] private float clickDelay = 1f;

    private int reactionTriggerHash;
    private int waitTriggerHash;
    private int idleStateHash;
    private int waitStateHash;
    private int reactionStateHash;

    private float nextReactionTime;

    private int targetIdleRepeat;
    private int currentIdleRepeat;
    private int previousIdleLoop;

    private bool wasIdle;
    private bool wasReaction;
    private bool waitRequested;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        UpdateHashes();
        StartNewIdleCycle();
    }

    private void OnValidate()
    {
        if (clickDelay < 0f)
            clickDelay = 0f;

        minIdleRepeat = Mathf.Max(1, minIdleRepeat);
        maxIdleRepeat = Mathf.Max(1, maxIdleRepeat);

        UpdateHashes();
    }

    private void Update()
    {
        if (animator == null)
            return;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        bool isIdle = IsState(currentState, idleStateHash);
        bool isWait = IsState(currentState, waitStateHash);
        bool isReaction = IsState(currentState, reactionStateHash);

        // Reaction에 진입한 순간에도 카운트를 확실히 초기화합니다.
        // PlayReaction() 외의 다른 코드에서 Reaction으로 전환되는 경우도 대응합니다.
        if (isReaction && !wasReaction)
        {
            ResetIdleCounter();
            ResetWaitTrigger();
        }

        if (isReaction)
        {
            wasIdle = false;
            wasReaction = true;
            return;
        }

        wasReaction = false;

        // Wait 재생 중에는 Idle 카운트를 세지 않습니다.
        if (isWait)
        {
            wasIdle = false;
            return;
        }

        if (!isIdle)
        {
            wasIdle = false;
            return;
        }

        // Wait 또는 Reaction이 끝나고 Idle에 새로 들어오면
        // 0부터 새로운 랜덤 횟수로 다시 시작합니다.
        if (!wasIdle)
        {
            StartNewIdleCycle();
            previousIdleLoop = Mathf.FloorToInt(currentState.normalizedTime);
            wasIdle = true;
            return;
        }

        if (waitRequested)
            return;

        int currentLoop = Mathf.FloorToInt(currentState.normalizedTime);

        if (currentLoop <= previousIdleLoop)
            return;

        currentIdleRepeat += currentLoop - previousIdleLoop;
        previousIdleLoop = currentLoop;

        if (currentIdleRepeat >= targetIdleRepeat)
        {
            waitRequested = true;

            if (waitTriggerHash != 0)
            {
                animator.ResetTrigger(waitTriggerHash);
                animator.SetTrigger(waitTriggerHash);
            }
        }
    }

    private void OnMouseDown()
    {
        if (!useOnMouseDown)
            return;

        PlayReaction();
    }

    /// <summary>
    /// 기존 캐릭터 선택/클릭 코드에서도 호출할 수 있습니다.
    /// Wait가 현재 재생 중이거나 Wait로 전환 중이면 Reaction을 실행하지 않습니다.
    /// Reaction을 실행하는 순간 Idle 반복 횟수는 0으로 초기화됩니다.
    /// </summary>
    public void PlayReaction()
    {
        if (animator == null || reactionTriggerHash == 0)
            return;

        // Wait가 실행 중이거나 막 Wait로 넘어가는 중이면 클릭 Reaction 금지.
        if (IsWaitActive())
            return;

        if (Time.unscaledTime < nextReactionTime)
            return;

        nextReactionTime = Time.unscaledTime + clickDelay;

        // Reaction이 실행되면 Wait까지 세던 Idle 반복 횟수를 버리고 0부터 다시 시작합니다.
        ResetIdleCounter();
        ResetWaitTrigger();

        animator.ResetTrigger(reactionTriggerHash);
        animator.SetTrigger(reactionTriggerHash);
    }

    /// <summary>
    /// 외부에서 필요할 때 Idle -> Wait 카운트를 강제로 처음부터 시작할 수 있습니다.
    /// </summary>
    public void ResetIdleWaitLoop()
    {
        ResetIdleCounter();
        ResetWaitTrigger();
        StartNewIdleCycle();
    }

    private bool IsWaitActive()
    {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (IsState(currentState, waitStateHash))
            return true;

        if (!animator.IsInTransition(0))
            return false;

        AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
        return IsState(nextState, waitStateHash);
    }

    private void StartNewIdleCycle()
    {
        ResetIdleCounter();

        int min = Mathf.Min(minIdleRepeat, maxIdleRepeat);
        int max = Mathf.Max(minIdleRepeat, maxIdleRepeat);

        targetIdleRepeat = Random.Range(min, max + 1);
    }

    private void ResetIdleCounter()
    {
        currentIdleRepeat = 0;
        previousIdleLoop = 0;
        waitRequested = false;
    }

    private void ResetWaitTrigger()
    {
        if (animator != null && waitTriggerHash != 0)
            animator.ResetTrigger(waitTriggerHash);

        waitRequested = false;
    }

    private static bool IsState(AnimatorStateInfo stateInfo, int stateHash)
    {
        return stateHash != 0 && stateInfo.shortNameHash == stateHash;
    }

    private void UpdateHashes()
    {
        reactionTriggerHash = GetHash(reactionTriggerName);
        waitTriggerHash = GetHash(waitTriggerName);
        idleStateHash = GetHash(idleStateName);
        waitStateHash = GetHash(waitStateName);
        reactionStateHash = GetHash(reactionStateName);
    }

    private static int GetHash(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? 0 : Animator.StringToHash(value);
    }
}
