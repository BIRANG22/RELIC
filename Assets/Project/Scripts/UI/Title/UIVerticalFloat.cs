using UnityEngine;

public class UIVerticalFloat : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectTransform target;

    [Header("Float Option")]
    [SerializeField] private float moveAmount = 10f;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private bool useUnscaledTime = true;

    private Vector2 startAnchoredPosition;

    private void Awake()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }

        if (target != null)
        {
            startAnchoredPosition = target.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        if (target != null)
        {
            startAnchoredPosition = target.anchoredPosition;
        }
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }

        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float yOffset = Mathf.Sin(time * moveSpeed) * moveAmount;

        target.anchoredPosition = startAnchoredPosition + new Vector2(0f, yOffset);
    }

    private void OnDisable()
    {
        if (target != null)
        {
            target.anchoredPosition = startAnchoredPosition;
        }
    }
}