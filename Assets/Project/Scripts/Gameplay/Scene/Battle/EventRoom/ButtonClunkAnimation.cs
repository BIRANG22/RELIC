using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// NPC를 클릭했을 때 ShopPanel이 닫힌 상태라면 Shop 오브젝트의 Z 회전을
/// 0 -> +각도 -> -각도 -> 0 순서로 직접 재생합니다.
///
/// - 이 스크립트는 Npc_shop에 붙입니다.
/// - Target Object에는 실제로 회전할 Shop RectTransform을 넣습니다.
/// - Panel Transform에는 1100 -> 0으로 이동하는 ShopPanel RectTransform을 넣습니다.
/// - BackButton에는 이 스크립트를 붙이지 않습니다.
/// </summary>
public class ButtonClunkAnimation : MonoBehaviour, IPointerClickHandler
{
    [Header("적용 대상")]
    [Tooltip("Z 회전이 실제로 변해야 하는 오브젝트입니다. 예: ChoiceCanvas/ShopPanel/Shop")]
    [SerializeField] private RectTransform targetObject;

    [Header("샵 패널 상태 확인")]
    [Tooltip("Y 1100에서 0으로 이동하는 패널입니다. 예: ChoiceCanvas/ShopPanel")]
    [SerializeField] private RectTransform panelTransform;

    [Tooltip("체크하면 패널이 닫힌 위치에 있을 때만 재생합니다.")]
    [SerializeField] private bool playOnlyWhenPanelClosed = true;

    [Tooltip("패널이 열린 Y 위치입니다.")]
    [SerializeField] private float openY = 0f;

    [Tooltip("패널이 닫힌 Y 위치입니다.")]
    [SerializeField] private float closedY = 1100f;

    [Tooltip("닫힌 위치로 인정할 오차 범위입니다.")]
    [SerializeField] private float closedCheckRange = 80f;

    [Header("재생 설정")]
    [Tooltip("클릭 후 회전이 시작되기 전 대기 시간입니다.")]
    [SerializeField] private float startDelay = 0f;

    [Tooltip("0 -> +각도 -> -각도 -> 0까지 걸리는 시간입니다.")]
    [SerializeField] private float clunkDuration = 0.18f;

    [Header("Z 회전")]
    [Tooltip("1이면 Z값이 0 -> 1 -> -1 -> 0으로 움직입니다.")]
    [SerializeField] private float zRotationAngle = 1f;

    private Coroutine clunkRoutine;
    private Vector3 baseEuler;

    private void Awake()
    {
        CacheBaseRotation();
    }

    private void OnEnable()
    {
        CacheBaseRotation();
    }

    private void OnDisable()
    {
        StopCurrentRoutine();
        RestoreRotation();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Play();
    }

    private void OnMouseDown()
    {
        // SpriteRenderer + Collider2D 조합에서도 동작하도록 처리합니다.
        Play();
    }

    /// <summary>
    /// Unity Button / UI Panel Button의 OnClick에서도 직접 연결해서 사용할 수 있습니다.
    /// </summary>
    public void Play()
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (targetObject == null)
            return;

        if (playOnlyWhenPanelClosed && !IsPanelClosed())
            return;

        if (!targetObject.gameObject.activeInHierarchy)
            return;

        StopCurrentRoutine();
        CacheBaseRotation();
        clunkRoutine = StartCoroutine(PlayRoutine());
    }

    [ContextMenu("Test Z Clunk")]
    private void TestZClunk()
    {
        bool previousCheck = playOnlyWhenPanelClosed;
        playOnlyWhenPanelClosed = false;
        Play();
        playOnlyWhenPanelClosed = previousCheck;
    }

    private bool IsPanelClosed()
    {
        if (panelTransform == null)
            return true;

        float y = panelTransform.anchoredPosition.y;
        float distanceFromClosed = Mathf.Abs(y - closedY);
        float distanceFromOpen = Mathf.Abs(y - openY);

        return distanceFromClosed <= closedCheckRange && distanceFromClosed <= distanceFromOpen;
    }

    private IEnumerator PlayRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        float totalDuration = Mathf.Max(0.01f, clunkDuration);
        float stepDuration = totalDuration / 3f;
        float angle = Mathf.Abs(zRotationAngle);

        yield return RotateZOffset(0f, angle, stepDuration);
        yield return RotateZOffset(angle, -angle, stepDuration);
        yield return RotateZOffset(-angle, 0f, stepDuration);

        RestoreRotation();
        clunkRoutine = null;
    }

    private IEnumerator RotateZOffset(float fromOffset, float toOffset, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float z = baseEuler.z + Mathf.Lerp(fromOffset, toOffset, eased);

            targetObject.localEulerAngles = new Vector3(baseEuler.x, baseEuler.y, z);
            yield return null;
        }

        targetObject.localEulerAngles = new Vector3(baseEuler.x, baseEuler.y, baseEuler.z + toOffset);
    }

    private void CacheBaseRotation()
    {
        if (targetObject != null)
            baseEuler = targetObject.localEulerAngles;
    }

    private void RestoreRotation()
    {
        if (targetObject != null)
            targetObject.localEulerAngles = baseEuler;
    }

    private void StopCurrentRoutine()
    {
        if (clunkRoutine == null)
            return;

        StopCoroutine(clunkRoutine);
        clunkRoutine = null;
    }
}
