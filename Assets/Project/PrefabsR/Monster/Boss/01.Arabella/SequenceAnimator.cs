using System.Collections;
using UnityEngine;

public class AnimationSequence : BattleRoomIntroSequence
{
    [SerializeField] private Animator animator;

    [Header("Idle Settings")]
    [SerializeField] private string idleState = "appear_idle";
    [SerializeField] private int idleRepeatCount = 5;

    [Header("Sequence States")]
    [SerializeField] private string state1 = "1";
    [SerializeField] private string state2 = "2";
    [SerializeField] private string state3 = "3";

    [Header("Completion")]
    [Min(0f)] [SerializeField] private float postSequenceDelay = 1f;

    private Coroutine sequenceRoutine;

    public float PostSequenceDelay => postSequenceDelay;

    private void OnEnable()
    {
        ResetCompletion();

        if (animator == null)
        {
            Debug.LogWarning("[AnimationSequence] Animator is missing. The intro sequence will be skipped.", this);
            MarkCompleted();
            return;
        }

        sequenceRoutine = StartCoroutine(PlaySequence());
    }

    private void OnDisable()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        ResetCompletion();
    }

    IEnumerator PlaySequence()
    {
        // Idle ¹Ýº¹
        for (int i = 0; i < idleRepeatCount; i++)
        {
            yield return PlayAnimation(idleState);
        }

        // 1 ¡æ 2 ¡æ 3
        yield return PlayAnimation(state1);
        yield return PlayAnimation(state2);
        yield return PlayAnimation(state3);

        float delay = Mathf.Max(0f, postSequenceDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        sequenceRoutine = null;
        MarkCompleted();
    }

    IEnumerator PlayAnimation(string stateName)
    {
        animator.Play(stateName, 0, 0);

        yield return null;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(info.length);
    }
}
