using System.Collections;
using UnityEngine;

public class ObjectPointMover : MonoBehaviour
{
    [Header("위치 설정")]
    [Tooltip("오브젝트가 처음 위치할 좌표")]
    [SerializeField] private Vector3 positionA;

    [Tooltip("오브젝트가 이동할 목표 좌표")]
    [SerializeField] private Vector3 positionB;

    [Header("시간 설정")]
    [Tooltip("위치 A에서 대기하는 시간")]
    [Min(0f)]
    [SerializeField] private float waitTime = 2f;

    [Tooltip("위치 A에서 위치 B까지 이동하는 시간")]
    [Min(0.01f)]
    [SerializeField] private float moveDuration = 1f;

    [Header("실행 설정")]
    [Tooltip("오브젝트가 활성화되면 자동으로 실행")]
    [SerializeField] private bool playOnEnable = true;

    [Tooltip("로컬 좌표를 사용할지 여부")]
    [SerializeField] private bool useLocalPosition = false;

    private Coroutine moveCoroutine;

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    public void Play()
    {
        if (!isActiveAndEnabled)
            return;

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        // 스크립트가 붙어 있는 오브젝트를 위치 A로 이동
        SetPosition(positionA);

        // 위치 A에서 대기
        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        Vector3 startPosition = GetPosition();
        float elapsedTime = 0f;

        // 위치 B로 부드럽게 이동
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / moveDuration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            Vector3 currentPosition = Vector3.Lerp(
                startPosition,
                positionB,
                smoothProgress
            );

            SetPosition(currentPosition);

            yield return null;
        }

        // 마지막 위치 보정
        SetPosition(positionB);

        moveCoroutine = null;
    }

    private Vector3 GetPosition()
    {
        return useLocalPosition
            ? transform.localPosition
            : transform.position;
    }

    private void SetPosition(Vector3 targetPosition)
    {
        if (useLocalPosition)
        {
            transform.localPosition = targetPosition;
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    public void Stop()
    {
        if (moveCoroutine == null)
            return;

        StopCoroutine(moveCoroutine);
        moveCoroutine = null;
    }
}