using UnityEngine;

/// <summary>
/// Adjusts the lobby magic-circle particle color while a linked hover source is active.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public class MagicCircleHoverPulse : MonoBehaviour
{
    private const float ByteToAlphaScale = 1f / 255f;

    [Header("Target")]
    [Tooltip("Particle system that receives the color-over-lifetime gradient. Empty uses the ParticleSystem on this object.")]
    [SerializeField] private ParticleSystem targetParticleSystem;

    [Tooltip("Legacy single hover source. The effect is considered hovered while this object is active.")]
    [SerializeField] private GameObject hoverSourceObject;

    [Tooltip("Additional hover sources. The effect is considered hovered while any source is active.")]
    [SerializeField] private GameObject[] hoverSourceObjects;

    [Header("Alpha")]
    [Range(0, 255)]
    [SerializeField] private int baseAlphaByte = 150;

    [Range(0, 255)]
    [SerializeField] private int hoverAlphaByte = 250;

    [Header("Base Color")]
    [Range(0, 255)]
    [SerializeField] private int baseRedByte = 100;

    [Range(0, 255)]
    [SerializeField] private int baseGreenByte = 100;

    [Range(0, 255)]
    [SerializeField] private int baseBlueByte = 100;

    [Header("Hover Color")]
    [Range(0, 255)]
    [SerializeField] private int hoverRedByte = 100;

    [Range(0, 255)]
    [SerializeField] private int hoverGreenByte = 150;

    [Range(0, 255)]
    [SerializeField] private int hoverBlueByte = 200;

    [Header("Hover Transition")]
    [Tooltip("Alpha byte change speed per second. 255 moves the full alpha range in one second.")]
    [Min(0f)]
    [SerializeField] private float hoverAlphaChangeSpeed = 220f;

    [Tooltip("RGB byte change speed per second. 255 moves the full RGB range in one second.")]
    [Min(0f)]
    [SerializeField] private float hoverColorChangeSpeed = 220f;

    [Tooltip("Middle gradient key time. Values are equal, but the prefab keeps three keys for stable serialization.")]
    [Range(0f, 1f)]
    [SerializeField] private float middleKeyTime = 0.40811878f;

    private float currentAlphaByte;
    private Vector3 currentColorByte;
    private bool isPointerHovering;

    public bool IsHovering =>
        isPointerHovering ||
        IsHoverSourceActive(hoverSourceObject) ||
        AnyHoverSourceActive();

    private void Awake()
    {
        ResolveTargetParticleSystem();
        ResetToBaseState();
        ApplyColorAndAlphaGradient();
    }

    private void OnEnable()
    {
        ResolveTargetParticleSystem();
        ResetToBaseState();
        ApplyColorAndAlphaGradient();
    }

    private void Update()
    {
        ResolveTargetParticleSystem();

        bool isHovering = IsHovering;
        float targetAlphaByte =
            isHovering ? GetClampedHoverAlphaByte() : GetClampedBaseAlphaByte();
        Vector3 targetColorByte =
            isHovering ? GetHoverColorByte() : GetBaseColorByte();

        currentAlphaByte = Mathf.MoveTowards(
            currentAlphaByte,
            targetAlphaByte,
            hoverAlphaChangeSpeed * Time.deltaTime);

        currentColorByte = Vector3.MoveTowards(
            currentColorByte,
            targetColorByte,
            hoverColorChangeSpeed * Time.deltaTime);

        ApplyColorAndAlphaGradient();
    }

    private void OnMouseEnter()
    {
        isPointerHovering = true;
    }

    private void OnMouseOver()
    {
        isPointerHovering = true;
    }

    private void OnMouseExit()
    {
        isPointerHovering = false;
    }

    private void OnDisable()
    {
        isPointerHovering = false;
    }

    private void OnValidate()
    {
        baseAlphaByte = Mathf.Clamp(baseAlphaByte, 0, 255);
        hoverAlphaByte = Mathf.Clamp(hoverAlphaByte, baseAlphaByte, 255);
        baseRedByte = Mathf.Clamp(baseRedByte, 0, 255);
        baseGreenByte = Mathf.Clamp(baseGreenByte, 0, 255);
        baseBlueByte = Mathf.Clamp(baseBlueByte, 0, 255);
        hoverRedByte = Mathf.Clamp(hoverRedByte, 0, 255);
        hoverGreenByte = Mathf.Clamp(hoverGreenByte, 0, 255);
        hoverBlueByte = Mathf.Clamp(hoverBlueByte, 0, 255);
        middleKeyTime = Mathf.Clamp01(middleKeyTime);

        ResolveTargetParticleSystem();
        ResetToBaseState();
        ApplyColorAndAlphaGradient();
    }

    private void ResolveTargetParticleSystem()
    {
        if (targetParticleSystem == null)
            targetParticleSystem = GetComponent<ParticleSystem>();
    }

    private void ResetToBaseState()
    {
        currentAlphaByte = GetClampedBaseAlphaByte();
        currentColorByte = GetBaseColorByte();
    }

    private int GetClampedBaseAlphaByte()
    {
        return Mathf.Clamp(baseAlphaByte, 0, 255);
    }

    private int GetClampedHoverAlphaByte()
    {
        return Mathf.Clamp(hoverAlphaByte, GetClampedBaseAlphaByte(), 255);
    }

    private Vector3 GetBaseColorByte()
    {
        return new Vector3(baseRedByte, baseGreenByte, baseBlueByte);
    }

    private Vector3 GetHoverColorByte()
    {
        return new Vector3(hoverRedByte, hoverGreenByte, hoverBlueByte);
    }

    private bool AnyHoverSourceActive()
    {
        if (hoverSourceObjects == null)
            return false;

        foreach (GameObject source in hoverSourceObjects)
        {
            if (IsHoverSourceActive(source))
                return true;
        }

        return false;
    }

    private static bool IsHoverSourceActive(GameObject source)
    {
        return source != null && source.activeInHierarchy;
    }

    private void ApplyColorAndAlphaGradient()
    {
        if (targetParticleSystem == null)
            return;

        ApplyColorAndAlphaGradient(
            targetParticleSystem,
            currentAlphaByte,
            middleKeyTime,
            currentColorByte);
    }

    private static void ApplyColorAndAlphaGradient(
        ParticleSystem particleSystem,
        float alphaByte,
        float middleTime,
        Vector3 colorByte)
    {
        ParticleSystem.ColorOverLifetimeModule colorModule =
            particleSystem.colorOverLifetime;

        colorModule.enabled = true;

        Gradient gradient = colorModule.color.gradient ?? new Gradient();

        Color color = ByteToColor(colorByte);
        float alpha = ByteToAlpha(alphaByte);
        float clampedMiddleTime = Mathf.Clamp01(middleTime);

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, clampedMiddleTime),
                new GradientColorKey(color, 1f),
            },
            new[]
            {
                new GradientAlphaKey(alpha, 0f),
                new GradientAlphaKey(alpha, clampedMiddleTime),
                new GradientAlphaKey(alpha, 1f),
            });

        colorModule.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    private static Color ByteToColor(Vector3 colorByte)
    {
        return new Color(
            ByteToAlpha(colorByte.x),
            ByteToAlpha(colorByte.y),
            ByteToAlpha(colorByte.z),
            1f);
    }

    private static float ByteToAlpha(float byteValue)
    {
        return Mathf.Clamp(byteValue, 0f, 255f) * ByteToAlphaScale;
    }
}
