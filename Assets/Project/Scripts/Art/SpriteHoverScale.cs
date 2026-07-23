using UnityEngine;

/// <summary>
/// 일반 스프라이트 오브젝트에 마우스를 올렸을 때
/// 지정한 대상의 스케일과 위치를 즉시 변경하고,
/// 별도의 오브젝트를 호버 중에만 활성화합니다.
/// </summary>
public class SpriteHoverScale : MonoBehaviour
{
    [Header("변경 대상")]
    [Tooltip("호버 시 스케일과 위치를 변경할 오브젝트입니다. 비워두면 현재 오브젝트가 변경됩니다.")]
    [SerializeField] private Transform targetImage;

    [Header("호버 스케일")]
    [Tooltip("마우스를 올렸을 때 적용할 스케일 배율입니다.")]
    [SerializeField] private float hoverScaleMultiplier = 1.1f;

    [Header("호버 위치")]
    [Tooltip("마우스를 올렸을 때 원래 위치에서 이동할 거리입니다.")]
    [SerializeField] private Vector3 hoverPositionOffset = Vector3.zero;

    [Header("호버 표시 오브젝트")]
    [Tooltip("평소에는 꺼져 있다가 호버 중에만 켜질 오브젝트입니다.")]
    [SerializeField] private GameObject hoverOnlyObject;

    [Header("복구 설정")]
    [Tooltip("비활성화될 때 원래 상태로 복구합니다.")]
    [SerializeField] private bool resetOnDisable = true;

    private Vector3 originalScale;
    private Vector3 originalLocalPosition;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        // 호버 표시 오브젝트는 시작할 때 비활성화합니다.
        if (hoverOnlyObject != null)
        {
            hoverOnlyObject.SetActive(false);
        }
    }

    private void Initialize()
    {
        if (targetImage == null)
        {
            targetImage = transform;
        }

        originalScale = targetImage.localScale;
        originalLocalPosition = targetImage.localPosition;

        initialized = true;
    }

    private void OnMouseEnter()
    {
        if (!initialized)
        {
            Initialize();
        }

        if (targetImage != null)
        {
            // 지정한 이미지의 크기와 위치를 즉시 변경합니다.
            targetImage.localScale =
                originalScale * hoverScaleMultiplier;

            targetImage.localPosition =
                originalLocalPosition + hoverPositionOffset;
        }

        // 호버 중에만 지정 오브젝트를 활성화합니다.
        if (hoverOnlyObject != null)
        {
            hoverOnlyObject.SetActive(true);
        }
    }

    private void OnMouseExit()
    {
        RestoreOriginalState();
    }

    private void OnDisable()
    {
        if (resetOnDisable)
        {
            RestoreOriginalState();
        }
    }

    /// <summary>
    /// 대상 이미지와 호버 표시 오브젝트를 원래 상태로 복구합니다.
    /// </summary>
    private void RestoreOriginalState()
    {
        if (initialized && targetImage != null)
        {
            targetImage.localScale = originalScale;
            targetImage.localPosition = originalLocalPosition;
        }

        if (hoverOnlyObject != null)
        {
            hoverOnlyObject.SetActive(false);
        }
    }

    /// <summary>
    /// 현재 대상의 스케일과 위치를 새로운 기본값으로 저장합니다.
    /// </summary>
    public void RefreshOriginalTransform()
    {
        if (targetImage == null)
        {
            targetImage = transform;
        }

        originalScale = targetImage.localScale;
        originalLocalPosition = targetImage.localPosition;

        initialized = true;
    }
}