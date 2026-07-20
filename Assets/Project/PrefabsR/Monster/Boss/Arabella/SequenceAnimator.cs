using System.Collections;
using UnityEngine;

public class AnimationSequence : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Idle Settings")]
    [SerializeField] private string idleState = "appear_idle";
    [SerializeField] private int idleRepeatCount = 5;

    [Header("Sequence States")]
    [SerializeField] private string state1 = "1";
    [SerializeField] private string state2 = "2";
    [SerializeField] private string state3 = "3";

    private void Start()
    {
        StartCoroutine(PlaySequence());
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
    }

    IEnumerator PlayAnimation(string stateName)
    {
        animator.Play(stateName, 0, 0);

        yield return null;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(info.length);
    }
}