using UnityEngine;

/// <summary>
/// 캐릭터를 클릭했을 때 Animator의 Reaction Trigger를 실행합니다.
/// 평소에는 Animator의 기본 Idle 상태를 유지하고,
/// Reaction 애니메이션이 끝나면 Animator Transition 설정에 따라 Idle로 복귀합니다.
/// </summary>
public class CharacterClickReaction : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Reaction Trigger")]
    [Tooltip("Animator Parameters에 등록한 Trigger 이름입니다.")]
    [SerializeField] private string reactionTriggerName = "ClickReaction";

    [Header("Click Input")]
    [Tooltip("켜져 있으면 이 오브젝트의 Collider를 직접 클릭했을 때 반응합니다.\n기존 캐릭터 선택 시스템에서 PlayReaction()을 호출할 경우 꺼두어도 됩니다.")]
    [SerializeField] private bool useOnMouseDown = true;

    [Header("Click Limit")]
    [Tooltip("반응 애니메이션 실행 후 다시 클릭 반응을 허용하기까지의 시간(초)입니다.")]
    [Min(0f)]
    [SerializeField] private float clickDelay = 1f;

    private int reactionTriggerHash;
    private float nextReactionTime;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        UpdateTriggerHash();
    }

    private void OnValidate()
    {
        if (clickDelay < 0f)
            clickDelay = 0f;

        UpdateTriggerHash();
    }

    private void OnMouseDown()
    {
        if (!useOnMouseDown)
            return;

        PlayReaction();
    }

    /// <summary>
    /// 기존 캐릭터 선택/클릭 코드에서 호출할 수 있습니다.
    /// clickDelay 시간 동안에는 중복 실행되지 않습니다.
    /// </summary>
    public void PlayReaction()
    {
        if (animator == null || string.IsNullOrWhiteSpace(reactionTriggerName))
            return;

        if (Time.unscaledTime < nextReactionTime)
            return;

        nextReactionTime = Time.unscaledTime + clickDelay;

        animator.ResetTrigger(reactionTriggerHash);
        animator.SetTrigger(reactionTriggerHash);
    }

    private void UpdateTriggerHash()
    {
        if (string.IsNullOrWhiteSpace(reactionTriggerName))
            reactionTriggerHash = 0;
        else
            reactionTriggerHash = Animator.StringToHash(reactionTriggerName);
    }
}
