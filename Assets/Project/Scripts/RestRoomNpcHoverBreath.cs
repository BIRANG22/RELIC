using UnityEngine;

/// <summary>
/// 레스트룸 NPC 오브젝트에 마우스를 올렸을 때 지정한 대상만 기본 스케일과 확대 스케일 사이를 왕복시키는 효과입니다.
/// 예: Npc_shop에 붙여두고 Breath Target에 자식 오브젝트 npc를 넣으면 npc만 1.05배로 숨쉬듯 움직입니다.
/// </summary>
public class RestRoomNpcHoverBreath : MonoBehaviour
{
    [Header("Hover Breath")]
    [SerializeField] private bool useHoverBreath = true;
    [SerializeField] private Transform breathTarget;
    [SerializeField] private float hoverScaleMultiplier = 1.05f;
    [SerializeField] private float breathSpeed = 2.5f;
    [SerializeField] private float returnSpeed = 10f;

    [Header("Option")]
    [SerializeField] private bool resetToDefaultScaleOnEnable = true;
    [SerializeField] private bool autoFindNpcChild = true;
    [SerializeField] private string npcChildName = "npc";

    private Vector3 defaultScale;
    private bool initialized;
    private bool isHovering;
    private float breathTimer;

    private Transform Target
    {
        get
        {
            if (breathTarget == null)
                TryFindNpcChild();

            return breathTarget != null ? breathTarget : transform;
        }
    }

    private void Awake()
    {
        InitializeScale();
    }

    private void OnEnable()
    {
        initialized = false;
        InitializeScale();

        isHovering = false;
        breathTimer = 0f;

        if (resetToDefaultScaleOnEnable)
            Target.localScale = defaultScale;
    }

    private void OnDisable()
    {
        if (initialized && resetToDefaultScaleOnEnable)
            Target.localScale = defaultScale;
    }

    private void Update()
    {
        if (!useHoverBreath)
            return;

        InitializeScale();

        if (isHovering)
        {
            breathTimer += Time.deltaTime * Mathf.Max(0f, breathSpeed);

            float t = (Mathf.Sin(breathTimer) + 1f) * 0.5f;
            Vector3 maxScale = defaultScale * Mathf.Max(0f, hoverScaleMultiplier);
            Target.localScale = Vector3.Lerp(defaultScale, maxScale, t);
        }
        else
        {
            Target.localScale = Vector3.Lerp(
                Target.localScale,
                defaultScale,
                Time.deltaTime * Mathf.Max(0f, returnSpeed));
        }
    }

    private void OnMouseEnter()
    {
        if (!useHoverBreath)
            return;

        InitializeScale();
        isHovering = true;
        breathTimer = 0f;
    }

    private void OnMouseExit()
    {
        isHovering = false;
    }

    private void InitializeScale()
    {
        if (initialized)
            return;

        defaultScale = Target.localScale;
        initialized = true;
    }

    private void TryFindNpcChild()
    {
        if (!autoFindNpcChild || string.IsNullOrWhiteSpace(npcChildName))
            return;

        Transform found = transform.Find(npcChildName);
        if (found != null)
            breathTarget = found;
    }
}
