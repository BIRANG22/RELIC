using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPassThroughMover : MonoBehaviour
{
    [Header("위치 설정")]
    [Tooltip("오브젝트가 시작할 왼쪽 위치")]
    [SerializeField] private Vector3 leftPosition;

    [Tooltip("빠른 이동이 끝나고 느려지기 시작할 위치")]
    [SerializeField] private Vector3 centerEnterPosition;

    [Tooltip("느린 이동이 끝나고 다시 빨라지기 시작할 위치")]
    [SerializeField] private Vector3 centerExitPosition;

    [Tooltip("오브젝트가 빠져나갈 오른쪽 위치")]
    [SerializeField] private Vector3 rightPosition;

    [Header("구간별 이동 시간")]
    [Tooltip("왼쪽에서 중앙까지 빠르게 이동하는 시간")]
    [Min(0.01f)]
    [SerializeField] private float enterDuration = 0.25f;

    [Tooltip("중앙 구간을 천천히 지나가는 시간")]
    [Min(0.01f)]
    [SerializeField] private float centerDuration = 1.5f;

    [Tooltip("중앙에서 오른쪽으로 빠르게 이동하는 시간")]
    [Min(0.01f)]
    [SerializeField] private float exitDuration = 0.25f;

    [Header("실행 설정")]
    [Tooltip("연출이 시작되기 전 대기 시간")]
    [Min(0f)]
    [SerializeField] private float startDelay = 0f;

    [Tooltip("부모 오브젝트 기준 로컬 좌표 사용")]
    [SerializeField] private bool useLocalPosition = false;

    [Tooltip("Time Scale이 0이어도 움직이게 설정")]
    [SerializeField] private bool useUnscaledTime = false;

    [Tooltip("연출이 끝난 뒤 이동 오브젝트를 비활성화")]
    [SerializeField] private bool disableAfterFinish = false;

    [Header("연출 전 숨김 및 종료 후 활성화")]
    [Tooltip("연출 전에는 숨기고, 연출 종료 후 활성화할 오브젝트들")]
    [SerializeField]
    private List<GameObject> objectsToActivateAfterFinish = new();

    [Tooltip("게임 시작 시 지정한 오브젝트들을 숨김")]
    [SerializeField] private bool hideObjectsOnAwake = true;

    [Tooltip("Play가 실행될 때마다 지정한 오브젝트들을 다시 숨김")]
    [SerializeField] private bool hideObjectsOnPlay = true;

    private Coroutine moveCoroutine;

    public bool IsPlaying => moveCoroutine != null;

    private void Awake()
    {
        SetPosition(leftPosition);

        if (hideObjectsOnAwake)
        {
            SetTargetObjectsActive(false);
        }
    }

    /// <summary>
    /// 이동 연출을 시작합니다.
    /// </summary>
    public void Play()
    {
        if (!gameObject.activeInHierarchy)
            return;

        StopMovement();

        // 연출을 다시 실행할 때 대상 오브젝트들을 숨김
        if (hideObjectsOnPlay)
        {
            SetTargetObjectsActive(false);
        }

        moveCoroutine = StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        SetPosition(leftPosition);

        if (startDelay > 0f)
        {
            yield return WaitRoutine(startDelay);
        }

        // 왼쪽에서 빠르게 등장하면서 감속
        yield return MoveTo(
            leftPosition,
            centerEnterPosition,
            enterDuration,
            MovementType.FastEnter
        );

        // 중앙 구간을 천천히 통과
        yield return MoveTo(
            centerEnterPosition,
            centerExitPosition,
            centerDuration,
            MovementType.Linear
        );

        // 오른쪽으로 점점 빨라지며 퇴장
        yield return MoveTo(
            centerExitPosition,
            rightPosition,
            exitDuration,
            MovementType.FastExit
        );

        moveCoroutine = null;

        // 연출이 정상적으로 끝난 뒤 대상 오브젝트들을 활성화
        SetTargetObjectsActive(true);

        if (disableAfterFinish)
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator MoveTo(
        Vector3 startPosition,
        Vector3 targetPosition,
        float duration,
        MovementType movementType)
    {
        duration = Mathf.Max(0.01f, duration);

        float elapsedTime = 0f;

        SetPosition(startPosition);

        while (elapsedTime < duration)
        {
            elapsedTime += GetDeltaTime();

            float progress = Mathf.Clamp01(
                elapsedTime / duration
            );

            float movementProgress = EvaluateProgress(
                progress,
                movementType
            );

            Vector3 nextPosition = Vector3.LerpUnclamped(
                startPosition,
                targetPosition,
                movementProgress
            );

            SetPosition(nextPosition);

            yield return null;
        }

        SetPosition(targetPosition);
    }

    private float EvaluateProgress(
        float progress,
        MovementType movementType)
    {
        switch (movementType)
        {
            case MovementType.FastEnter:
                // 처음에는 빠르고 중앙에 가까워지며 감속
                return 1f - Mathf.Pow(1f - progress, 3f);

            case MovementType.Linear:
                // 중앙 구간은 일정한 속도로 이동
                return progress;

            case MovementType.FastExit:
                // 처음에는 느리고 오른쪽으로 갈수록 가속
                return Mathf.Pow(progress, 3f);

            default:
                return progress;
        }
    }

    private IEnumerator WaitRoutine(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += GetDeltaTime();
            yield return null;
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;
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

    /// <summary>
    /// 지정된 대상 오브젝트들을 모두 켜거나 끕니다.
    /// </summary>
    private void SetTargetObjectsActive(bool isActive)
    {
        if (objectsToActivateAfterFinish == null)
            return;

        for (int i = 0;
             i < objectsToActivateAfterFinish.Count;
             i++)
        {
            GameObject target =
                objectsToActivateAfterFinish[i];

            if (target == null)
                continue;

            target.SetActive(isActive);
        }
    }

    /// <summary>
    /// 진행 중인 이동 연출을 중지합니다.
    /// </summary>
    public void StopMovement()
    {
        if (moveCoroutine == null)
            return;

        StopCoroutine(moveCoroutine);
        moveCoroutine = null;
    }

    /// <summary>
    /// 시작 위치로 초기화하고 대상 오브젝트들을 숨깁니다.
    /// </summary>
    public void ResetToStart()
    {
        StopMovement();
        SetPosition(leftPosition);
        SetTargetObjectsActive(false);
    }

    private enum MovementType
    {
        FastEnter,
        Linear,
        FastExit
    }
}