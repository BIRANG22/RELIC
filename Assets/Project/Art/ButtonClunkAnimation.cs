using System.Collections;
using UnityEngine;

/// <summary>
/// 오브젝트를 클릭했을 때 지정한 대상 오브젝트를 덜컹거리게 하는 스크립트.
/// 위치값은 건드리지 않고 회전값만 사용합니다.
/// </summary>
public class ButtonClunkAnimation : MonoBehaviour
{
    [Header("적용 대상")]
    [Tooltip("클릭했을 때 덜컹거릴 오브젝트를 넣습니다.")]
    [SerializeField] private Transform targetObject;

    [Tooltip("클릭했을 때 대상 오브젝트를 먼저 켤지 설정합니다.")]
    [SerializeField] private bool activateTargetOnClick = true;

    [Header("재생 딜레이")]
    [Tooltip("클릭 후 몇 초 뒤에 애니메이션을 재생할지 설정합니다.")]
    [SerializeField] private float startDelay = 0f;

    [Header("덜컹거림 설정")]
    [Tooltip("덜컹거림이 진행되는 전체 시간입니다.")]
    [SerializeField] private float clunkDuration = 0.25f;

    [Tooltip("덜컹거리는 횟수입니다.")]
    [SerializeField] private int clunkCount = 3;

    [Header("회전 흔들림")]
    [Tooltip("좌우로 흔들리는 회전 각도입니다.")]
    [SerializeField] private float rotationPower = 4f;

    private Quaternion originalRotation;
    private Coroutine animationCoroutine;

    private void OnMouseDown()
    {
        Play();
    }

    /// <summary>
    /// 클릭 또는 Button OnClick에서 호출할 수 있습니다.
    /// </summary>
    public void Play()
    {
        if (targetObject == null)
        {
            Debug.LogWarning($"{nameof(ButtonClunkAnimation)}: Target Object가 지정되지 않았습니다.", this);
            return;
        }

        if (activateTargetOnClick)
        {
            targetObject.gameObject.SetActive(true);
        }

        originalRotation = targetObject.localRotation;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(PlayAnimationWithDelay());
    }

    public void PlayImmediately()
    {
        if (targetObject == null)
        {
            Debug.LogWarning($"{nameof(ButtonClunkAnimation)}: Target Object가 지정되지 않았습니다.", this);
            return;
        }

        if (activateTargetOnClick)
        {
            targetObject.gameObject.SetActive(true);
        }

        originalRotation = targetObject.localRotation;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimationWithDelay()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(startDelay);
        }

        yield return PlayAnimation();
    }

    private IEnumerator PlayAnimation()
    {
        float timer = 0f;

        while (timer < clunkDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / clunkDuration);

            float shake = Mathf.Sin(t * Mathf.PI * clunkCount * 2f);
            float fade = 1f - t;

            float zRotation = shake * rotationPower * fade;

            targetObject.localRotation = originalRotation * Quaternion.Euler(0f, 0f, zRotation);

            yield return null;
        }

        targetObject.localRotation = originalRotation;
        animationCoroutine = null;
    }

    private void OnDisable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        if (targetObject != null)
        {
            targetObject.localRotation = originalRotation;
        }
    }
}