using System.Collections;
using UnityEngine;

public class BattleCameraController : MonoBehaviour
{
    public static BattleCameraController Instance { get; private set; }

    [SerializeField] private Camera targetCamera;

    [Header("Zoom")]
    [SerializeField] private float zoomSize = 4.2f;
    [SerializeField] private float zoomDuration = 0.18f;
    [SerializeField] private float returnDuration = 0.18f;

    private float defaultSize;
    private Vector3 defaultPosition;
    private Coroutine routine;

    private void Awake()
    {
        Instance = this;

        if (targetCamera == null)
            targetCamera = Camera.main;

        defaultSize = targetCamera.orthographicSize;
        defaultPosition = targetCamera.transform.position;
    }

    public IEnumerator ZoomTo(Transform target)
    {
        if (targetCamera == null || target == null)
            yield break;

        Vector3 originalPos = targetCamera.transform.position;

        Vector3 targetPos = target.position;
        targetPos.z = originalPos.z;

        targetCamera.transform.position =
            Vector3.Lerp(
                originalPos,
                targetPos,
                0.8f
            );

        yield return Shake(0.08f, 0.08f);

        targetCamera.orthographicSize = zoomSize;
    }

    private IEnumerator Shake(
    float duration,
    float strength)
    {
        Vector3 basePos = targetCamera.transform.position;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            targetCamera.transform.position =
                basePos +
                (Vector3)Random.insideUnitCircle * strength;

            yield return null;
        }

        targetCamera.transform.position = basePos;
    }

    public IEnumerator ReturnDefault()
    {
        if (targetCamera == null)
            yield break;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(MoveCamera(
            defaultPosition,
            defaultSize,
            returnDuration
        ));

        yield return routine;
    }

    private IEnumerator MoveCamera(Vector3 targetPos, float targetSize, float duration)
    {
        Vector3 startPos = targetCamera.transform.position;
        float startSize = targetCamera.orthographicSize;

        targetPos.z = startPos.z;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            targetCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            targetCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);

            yield return null;
        }

        targetCamera.transform.position = targetPos;
        targetCamera.orthographicSize = targetSize;
        routine = null;
    }
}