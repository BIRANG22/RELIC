using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 콜라이더가 있는 월드 오브젝트를 클릭하면 패널을 열고
/// 카메라 또는 카메라 Rig를 지정 위치로 이동시킵니다.
///
/// 닫기 버튼을 누르면 패널을 닫고
/// 카메라를 패널을 열기 전 위치로 되돌립니다.
/// </summary>
public class PanelCameraMover : MonoBehaviour
{
    [Header("패널 설정")]
    [Tooltip("열고 닫을 패널 오브젝트입니다.")]
    [SerializeField] private GameObject targetPanel;

    [Header("카메라 이동 대상")]
    [Tooltip(
        "실제로 이동시킬 카메라 부모 오브젝트입니다. " +
        "카메라가 단독 오브젝트라면 비워두세요.")]
    [SerializeField] private Transform cameraRig;

    [Tooltip(
        "Camera Rig가 비어 있을 때 직접 이동시킬 카메라입니다. " +
        "비워두면 Main Camera를 자동으로 찾습니다.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("패널이 열릴 때 카메라가 이동할 위치입니다.")]
    [SerializeField] private Transform cameraMoveTarget;

    [Tooltip("목표 오브젝트의 회전값도 적용합니다.")]
    [SerializeField] private bool applyTargetRotation = true;

    [Header("허브 카메라 드래그")]
    [Tooltip(
        "카메라 복귀가 완료된 뒤 현재 위치를 드래그 목표 위치와 동기화합니다. " +
        "비워두면 이동 대상에서 자동으로 찾습니다.")]
    [SerializeField] private HorizontalHubCameraDrag hubCameraDrag;

    [Header("카메라 이동 설정")]
    [Tooltip("카메라가 이동하는 데 걸리는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float moveDuration = 0.35f;

    [Tooltip("카메라 이동에 적용할 곡선입니다.")]
    [SerializeField]
    private AnimationCurve moveCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Time Scale이 0이어도 카메라가 움직이게 합니다.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("콜라이더 클릭 설정")]
    [Tooltip(
        "이 스크립트가 붙은 콜라이더 오브젝트를 클릭하면 " +
        "패널을 엽니다.")]
    [SerializeField] private bool openOnColliderClick = true;

    [Tooltip(
        "패널이 열린 상태에서 같은 오브젝트를 다시 클릭하면 " +
        "패널을 닫습니다.")]
    [SerializeField] private bool toggleOnRepeatedClick = false;

    [Tooltip("마우스가 UI 위에 있을 때 월드 오브젝트 클릭을 무시합니다.")]
    [SerializeField] private bool ignoreClickWhenPointerOverUI = true;

    [Header("디버그")]
    [Tooltip("카메라 이동 상태를 Console에 표시합니다.")]
    [SerializeField] private bool showDebugLog = false;

    // 패널을 열기 직전 카메라 위치와 회전값
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private bool originalTransformSaved;
    private bool panelWasOpen;
    private bool cameraWasMoved;
    private bool isReturning;

    private Coroutine moveCoroutine;

    /// <summary>
    /// 실제로 이동시킬 Transform입니다.
    /// Camera Rig가 연결되어 있으면 Camera Rig를 우선 사용합니다.
    /// </summary>
    private Transform MoveTransform
    {
        get
        {
            FindCameraIfNeeded();

            if (cameraRig != null)
                return cameraRig;

            if (targetCamera != null)
                return targetCamera.transform;

            return null;
        }
    }

    private void Awake()
    {
        FindCameraIfNeeded();
        FindHubCameraDragIfNeeded();

        if (targetPanel != null)
            panelWasOpen = targetPanel.activeInHierarchy;
    }

    private void Update()
    {
        DetectPanelState();
    }

    /// <summary>
    /// 이 스크립트가 붙은 Collider 또는 Collider2D를 클릭하면 호출됩니다.
    /// </summary>
    private void OnMouseDown()
    {
        if (!openOnColliderClick)
            return;

        if (ignoreClickWhenPointerOverUI &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        bool isPanelOpen =
            targetPanel != null &&
            targetPanel.activeInHierarchy;

        if (toggleOnRepeatedClick && isPanelOpen)
        {
            ClosePanelAndReturnCamera();
            return;
        }

        if (isPanelOpen)
            return;

        OpenPanel();
    }

    /// <summary>
    /// 패널을 열고 카메라를 지정 위치로 이동시킵니다.
    /// </summary>
    public void OpenPanel()
    {
        Transform moveTransform = MoveTransform;

        if (moveTransform == null)
        {
            Debug.LogError(
                "[PanelCameraMover] Camera Rig 또는 Target Camera가 없습니다.",
                this);

            return;
        }

        if (cameraMoveTarget == null)
        {
            Debug.LogError(
                "[PanelCameraMover] Camera Move Target이 연결되지 않았습니다.",
                this);

            return;
        }

        if (targetPanel != null &&
            targetPanel.activeInHierarchy &&
            cameraWasMoved)
        {
            return;
        }

        /*
         * 패널을 열고 카메라를 이동시키기 직전의
         * 위치와 회전을 저장합니다.
         */
        originalPosition = moveTransform.position;
        originalRotation = moveTransform.rotation;
        originalTransformSaved = true;

        cameraWasMoved = true;
        isReturning = false;

        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
            panelWasOpen = true;
        }

        Quaternion destinationRotation =
            applyTargetRotation
                ? cameraMoveTarget.rotation
                : moveTransform.rotation;

        StartCameraMove(
            cameraMoveTarget.position,
            destinationRotation,
            false);

        if (showDebugLog)
        {
            Debug.Log(
                "[PanelCameraMover] 패널을 열고 카메라를 이동합니다.",
                this);
        }
    }

    /// <summary>
    /// 버튼의 OnClick에 연결하는 함수입니다.
    ///
    /// 패널을 닫고 카메라를 패널을 열기 전 위치로 되돌립니다.
    /// </summary>
    public void ClosePanelAndReturnCamera()
    {
        if (targetPanel != null)
            targetPanel.SetActive(false);

        panelWasOpen = false;

        ReturnCamera();

        if (showDebugLog)
        {
            Debug.Log(
                "[PanelCameraMover] 버튼으로 패널을 닫고 카메라를 복귀시킵니다.",
                this);
        }
    }

    /// <summary>
    /// 기존 UI 연결을 유지하기 위한 함수입니다.
    /// ClosePanelAndReturnCamera와 동일하게 작동합니다.
    /// </summary>
    public void ClosePanel()
    {
        ClosePanelAndReturnCamera();
    }

    /// <summary>
    /// 패널 상태에 따라 열거나 닫습니다.
    /// </summary>
    public void TogglePanel()
    {
        bool isPanelOpen =
            targetPanel != null &&
            targetPanel.activeInHierarchy;

        if (isPanelOpen)
            ClosePanelAndReturnCamera();
        else
            OpenPanel();
    }

    /// <summary>
    /// 다른 스크립트나 SetActive(false)로 패널이 닫힌 경우를 감지합니다.
    /// </summary>
    private void DetectPanelState()
    {
        if (targetPanel == null)
            return;

        bool isPanelOpen =
            targetPanel.activeInHierarchy;

        /*
         * 이전 프레임에는 열려 있었지만
         * 현재 닫힌 경우 카메라를 원래 위치로 복귀시킵니다.
         */
        if (panelWasOpen &&
            !isPanelOpen &&
            cameraWasMoved &&
            !isReturning)
        {
            ReturnCamera();
        }

        panelWasOpen = isPanelOpen;
    }

    /// <summary>
    /// 카메라를 패널을 열기 전 위치로 되돌립니다.
    /// </summary>
    private void ReturnCamera()
    {
        if (isReturning)
            return;

        if (!originalTransformSaved || !cameraWasMoved)
            return;

        Transform moveTransform = MoveTransform;

        if (moveTransform == null)
            return;

        isReturning = true;
        cameraWasMoved = false;

        StartCameraMove(
            originalPosition,
            originalRotation,
            true);

        if (showDebugLog)
        {
            Debug.Log(
                "[PanelCameraMover] 카메라를 이동 전 위치로 되돌립니다.",
                this);
        }
    }

    /// <summary>
    /// 현재 카메라 위치를 새로운 복귀 위치로 저장합니다.
    /// </summary>
    public void RefreshOriginalCameraTransform()
    {
        Transform moveTransform = MoveTransform;

        if (moveTransform == null)
            return;

        originalPosition = moveTransform.position;
        originalRotation = moveTransform.rotation;
        originalTransformSaved = true;
    }

    /// <summary>
    /// 카메라 이동 코루틴을 시작합니다.
    /// 진행 중인 이동이 있으면 중단하고 새로운 이동을 시작합니다.
    /// </summary>
    private void StartCameraMove(
        Vector3 destinationPosition,
        Quaternion destinationRotation,
        bool returning)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        moveCoroutine = StartCoroutine(
            MoveCameraCoroutine(
                destinationPosition,
                destinationRotation,
                returning));
    }

    /// <summary>
    /// 카메라를 지정 위치까지 부드럽게 이동시킵니다.
    /// </summary>
    private IEnumerator MoveCameraCoroutine(
        Vector3 destinationPosition,
        Quaternion destinationRotation,
        bool returning)
    {
        Transform moveTransform = MoveTransform;

        if (moveTransform == null)
        {
            moveCoroutine = null;
            isReturning = false;
            yield break;
        }

        Vector3 startPosition = moveTransform.position;
        Quaternion startRotation = moveTransform.rotation;

        if (moveDuration <= 0f)
        {
            moveTransform.position = destinationPosition;

            if (applyTargetRotation || returning)
                moveTransform.rotation = destinationRotation;

            FinishCameraMove(returning);
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsedTime / moveDuration);

            float curvedTime =
                moveCurve.Evaluate(normalizedTime);

            moveTransform.position =
                Vector3.LerpUnclamped(
                    startPosition,
                    destinationPosition,
                    curvedTime);

            if (applyTargetRotation || returning)
            {
                moveTransform.rotation =
                    Quaternion.SlerpUnclamped(
                        startRotation,
                        destinationRotation,
                        curvedTime);
            }

            yield return null;
        }

        moveTransform.position = destinationPosition;

        if (applyTargetRotation || returning)
            moveTransform.rotation = destinationRotation;

        FinishCameraMove(returning);
    }

    /// <summary>
    /// 카메라 이동 완료 처리를 합니다.
    /// </summary>
    private void FinishCameraMove(bool returning)
    {
        moveCoroutine = null;

        if (!returning)
            return;

        isReturning = false;

        /*
         * 복귀가 끝난 뒤 HorizontalHubCameraDrag가
         * 이전 목표 위치로 카메라를 다시 끌어당기지 않도록
         * 현재 위치를 드래그 목표 위치와 동기화합니다.
         */
        FindHubCameraDragIfNeeded();

        if (hubCameraDrag != null)
            hubCameraDrag.SynchronizeTargetPosition();

        if (showDebugLog)
        {
            Debug.Log(
                "[PanelCameraMover] 카메라 원위치 복귀가 완료되었습니다.",
                this);
        }
    }

    /// <summary>
    /// Target Camera가 비어 있다면 Main Camera를 찾습니다.
    /// </summary>
    private void FindCameraIfNeeded()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    /// <summary>
    /// HorizontalHubCameraDrag가 연결되지 않았다면 자동으로 찾습니다.
    /// </summary>
    private void FindHubCameraDragIfNeeded()
    {
        if (hubCameraDrag != null)
            return;

        Transform moveTransform = MoveTransform;

        if (moveTransform != null)
        {
            hubCameraDrag =
                moveTransform.GetComponent<HorizontalHubCameraDrag>();

            if (hubCameraDrag == null)
            {
                hubCameraDrag =
                    moveTransform.GetComponentInChildren<HorizontalHubCameraDrag>(
                        true);
            }

            if (hubCameraDrag == null)
            {
                hubCameraDrag =
                    moveTransform.GetComponentInParent<HorizontalHubCameraDrag>(
                        true);
            }
        }

        if (hubCameraDrag == null && targetCamera != null)
        {
            hubCameraDrag =
                targetCamera.GetComponent<HorizontalHubCameraDrag>();
        }
    }

    private void OnDisable()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        isReturning = false;
    }
}