using System.Collections;
using UnityEngine;

public class AnimationKeyTestController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Default State")]
    [SerializeField] private string idleStateName = "char1_idle";

    [Header("Test States")]
    [SerializeField] private string attack00StateName = "cha1_attack00";
    [SerializeField] private string attack01StateName = "cha1_attack01";
    [SerializeField] private string guardStateName = "cha1_guard";
    [SerializeField] private string hitStateName = "cha1_hit";
    [SerializeField] private string battleStateName = "char1_battle";
    [SerializeField] private string lowHpStateName = "char1_lowhp";

    private Coroutine currentRoutine;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        PlayIdle();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            PlayOnceThenIdle(attack00StateName);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            PlayOnceThenIdle(attack01StateName);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            PlayOnceThenIdle(guardStateName);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            PlayOnceThenIdle(hitStateName);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            PlayOnceThenIdle(battleStateName);

        if (Input.GetKeyDown(KeyCode.Alpha6))
            PlayOnceThenIdle(lowHpStateName);
    }

    private void PlayOnceThenIdle(string stateName)
    {
        if (animator == null)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(PlayRoutine(stateName));
    }

    private IEnumerator PlayRoutine(string stateName)
    {
        animator.Play(stateName, 0, 0f);

        yield return null;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        float clipLength = stateInfo.length;

        yield return new WaitForSeconds(clipLength);

        PlayIdle();

        currentRoutine = null;
    }

    private void PlayIdle()
    {
        animator.Play(idleStateName, 0, 0f);
    }
}