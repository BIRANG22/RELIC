using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Moves a supplied UI-compatible VFX prefab between two screen positions.
/// The VFX prefab's ParticleSystem/Trails creates its own trail; no ghost objects are created.
/// The existing class name, PlayDetached and Play signatures are kept for callers.
/// </summary>
[RequireComponent(typeof(Canvas))]
public sealed class ScreenSpaceTransferOrbEffect : MonoBehaviour
{
    public enum OrbRarity { Common, Rare, Epic, Unique }

    [Header("Rarity VFX Colors")]
    [SerializeField] private Color commonVfxColor = new Color32(0xA9, 0xB1, 0xBE, 0xFF);
    [SerializeField] private Color rareVfxColor = new Color32(0x52, 0x86, 0xC7, 0xFF);
    [SerializeField] private Color epicVfxColor = new Color32(0xA0, 0x72, 0xC2, 0xFF);
    [SerializeField] private Color uniqueVfxColor = new Color32(0xCD, 0xA4, 0x52, 0xFF);

    public Color GetRarityColor(OrbRarity rarity)
    {
        switch (rarity)
        {
            case OrbRarity.Rare: return rareVfxColor;
            case OrbRarity.Epic: return epicVfxColor;
            case OrbRarity.Unique: return uniqueVfxColor;
            default: return commonVfxColor;
        }
    }

    [Header("Moving VFX")]
    [SerializeField] private GameObject vfxPrefab;
    [SerializeField] private Vector3 vfxLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 vfxLocalScale = Vector3.one;
    [Tooltip("Use the supplied Color to tint each ParticleSystem's Start Color. Disable to preserve the prefab's original colors.")]
    [SerializeField] private bool tintParticleStartColor = false;
    [SerializeField, Min(0.01f)] private float duration = 0.6f;

    [Header("Motion Randomness")]
    [SerializeField] private Vector2 arcHeightMultiplierRange = new(0.72f, 1.28f);
    [SerializeField, Min(0f)] private float lateralRandomness = 130f;
    [SerializeField] private Vector2 durationMultiplierRange = new(0.9f, 1.08f);

    [Header("Launch And Suck")]
    [SerializeField] private Vector2 oppositeKickHorizontalRange = new(85f, 155f);
    [SerializeField] private Vector2 oppositeKickVerticalRange = new(120f, 210f);
    [SerializeField, Min(0f)] private float oppositeKickSideRandomness = 30f;
    [SerializeField, Range(0.12f, 0.55f)] private float kickDurationRatio = 0.32f;
    [SerializeField, Min(0f)] private float suckCurveLift = 75f;

    [Header("UI Rendering")]
    [SerializeField, Min(10002)] private int sortingOrder = 20000;

    [Header("Finish")]
    [Tooltip("Maximum time to wait for the prefab's own particle trails after stopping emission.")]
    [SerializeField, Min(0f)] private float maxParticleFinishWait = 3f;

    private Canvas effectCanvas;
    private RectTransform effectRect;
    private RectTransform movingRoot;
    private GameObject vfxInstance;
    private ParticleSystem[] particles;
    private bool started;
    public bool HasMovingVfxPrefab => vfxPrefab != null;
    /// <summary>Invoked the instant the moving VFX reaches its destination, before trail cleanup.</summary>
    public event System.Action ReachedDestination;

    private void Awake()
    {
        effectCanvas = GetComponent<Canvas>();
        effectCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        effectCanvas.worldCamera = null;
        effectCanvas.overrideSorting = true;
        effectCanvas.sortingOrder = sortingOrder;
        effectRect = transform as RectTransform;
        if (effectRect != null) effectRect.localScale = Vector3.one;

        // Old prefabs may still have their former RawImage orb/trail visuals.
        // The moving VFX replaces them without requiring prefab hierarchy cleanup.
        foreach (RawImage oldOrb in GetComponentsInChildren<RawImage>(true))
            oldOrb.enabled = false;
    }

    /// <summary>Original color-based API. Respects Tint Particle Start Color.</summary>
    public void PlayDetached(Vector2 startScreen, Vector2 endScreen, Color color)
    {
        if (!started && isActiveAndEnabled)
            StartCoroutine(Play(startScreen, endScreen, color));
    }

    /// <summary>Play using the VFX prefab's own colors, ignoring rarity and tint settings.</summary>
    public void PlayDetachedOriginalColors(Vector2 startScreen, Vector2 endScreen)
    {
        if (!started && isActiveAndEnabled)
            StartCoroutine(PlayInternal(startScreen, endScreen, Color.white, false));
    }

    /// <summary>Rarity-based API. Always applies the inspector rarity color.</summary>
    public void PlayDetached(Vector2 startScreen, Vector2 endScreen, OrbRarity rarity)
    {
        if (!started && isActiveAndEnabled)
            StartCoroutine(Play(startScreen, endScreen, rarity));
    }

    public IEnumerator Play(Vector2 startScreen, Vector2 endScreen, OrbRarity rarity)
    {
        return PlayInternal(startScreen, endScreen, GetRarityColor(rarity), true);
    }

    /// <summary>Existing color-based coroutine API remains compatible.</summary>
    public IEnumerator Play(Vector2 startScreen, Vector2 endScreen, Color color)
    {
        return PlayInternal(startScreen, endScreen, color, tintParticleStartColor);
    }

    private IEnumerator PlayInternal(Vector2 startScreen, Vector2 endScreen, Color color, bool applyColor)
    {
        if (started) yield break;
        started = true;

        if (effectRect == null || vfxPrefab == null)
        {
            Debug.LogWarning("[ScreenSpaceTransferOrbEffect] Assign Moving VFX / Vfx Prefab.", this);
            Destroy(gameObject);
            yield break;
        }

        GameObject holder = new GameObject("MovingVFX", typeof(RectTransform));
        movingRoot = holder.GetComponent<RectTransform>();
        movingRoot.SetParent(effectRect, false);
        movingRoot.anchorMin = movingRoot.anchorMax = new Vector2(0.5f, 0.5f);
        movingRoot.pivot = new Vector2(0.5f, 0.5f);
        movingRoot.sizeDelta = Vector2.zero;
        MoveToScreenPosition(startScreen);

        vfxInstance = Instantiate(vfxPrefab, movingRoot, false);
        vfxInstance.name = vfxPrefab.name + "_TransferVFX";
        vfxInstance.transform.localPosition = vfxLocalOffset;
        vfxInstance.transform.localRotation = Quaternion.identity;
        vfxInstance.transform.localScale = vfxLocalScale;
        vfxInstance.SetActive(true);
        particles = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particle in particles)
        {
            if (particle == null || !particle.gameObject.activeInHierarchy) continue;
            if (applyColor)
            {
                ParticleSystem.MainModule main = particle.main;
                main.startColor = color;
            }
            particle.Play(true);
        }

        yield return PlayRoutine(startScreen, endScreen);
    }

    private IEnumerator PlayRoutine(Vector2 startScreen, Vector2 endScreen)
    {
        float durationMultiplier = Random.Range(
            Mathf.Min(durationMultiplierRange.x, durationMultiplierRange.y),
            Mathf.Max(durationMultiplierRange.x, durationMultiplierRange.y));
        float safeDuration = Mathf.Max(0.01f, duration * durationMultiplier);
        float kickDuration = safeDuration * Mathf.Clamp(kickDurationRatio, 0.12f, 0.55f);
        float suckDuration = Mathf.Max(0.01f, safeDuration - kickDuration);

        Vector2 travel = endScreen - startScreen;
        Vector2 direction = travel.sqrMagnitude > 0.001f ? travel.normalized : Vector2.right;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        float horizontalKick = Random.Range(
            Mathf.Min(oppositeKickHorizontalRange.x, oppositeKickHorizontalRange.y),
            Mathf.Max(oppositeKickHorizontalRange.x, oppositeKickHorizontalRange.y));
        float verticalKick = Random.Range(
            Mathf.Min(oppositeKickVerticalRange.x, oppositeKickVerticalRange.y),
            Mathf.Max(oppositeKickVerticalRange.x, oppositeKickVerticalRange.y));
        float kickSign = Mathf.Abs(travel.x) > 1f ? -Mathf.Sign(travel.x) : (Random.value < 0.5f ? -1f : 1f);
        Vector2 kickPoint = startScreen + new Vector2(kickSign * horizontalKick, verticalKick)
            + perpendicular * Random.Range(-oppositeKickSideRandomness, oppositeKickSideRandomness);

        // First jump away from the destination.
        yield return AnimateSegment(kickDuration,
            t => Vector2.LerpUnclamped(startScreen, kickPoint, EaseOutCubic(t)));

        float arcMultiplier = Random.Range(
            Mathf.Min(arcHeightMultiplierRange.x, arcHeightMultiplierRange.y),
            Mathf.Max(arcHeightMultiplierRange.x, arcHeightMultiplierRange.y));
        Vector2 control = Vector2.Lerp(kickPoint, endScreen, 0.35f)
            + Vector2.up * (suckCurveLift * arcMultiplier)
            + perpendicular * (Random.Range(-lateralRandomness, lateralRandomness) * 0.35f);

        // Then accelerate along a quadratic Bezier path into the target.
        yield return AnimateSegment(suckDuration,
            t => EvaluateQuadraticBezier(kickPoint, control, endScreen, EaseInCubic(t)));
        MoveToScreenPosition(endScreen);
        ReachedDestination?.Invoke();

        // No generated ghost trail: let the particle prefab's own Trails/particles fade naturally.
        if (particles != null)
        {
            foreach (ParticleSystem particle in particles)
                if (particle != null) particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            float elapsed = 0f;
            while (elapsed < maxParticleFinishWait)
            {
                bool alive = false;
                foreach (ParticleSystem particle in particles)
                    if (particle != null && particle.IsAlive(true)) { alive = true; break; }
                if (!alive) break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        Destroy(gameObject);
    }

    private IEnumerator AnimateSegment(float segmentDuration, System.Func<float, Vector2> position)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, segmentDuration);
        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            MoveToScreenPosition(position(Mathf.Clamp01(elapsed / safeDuration)));
            yield return null;
        }
    }

    private void MoveToScreenPosition(Vector2 screenPosition)
    {
        if (movingRoot == null) return;
        movingRoot.anchoredPosition = ScreenToLocalPosition(screenPosition);
    }

    private Vector2 ScreenToLocalPosition(Vector2 screenPosition)
    {
        if (effectRect == null) return Vector2.zero;
        Camera camera = effectCanvas != null && effectCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? effectCanvas.worldCamera : null;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(effectRect, screenPosition, camera,
            out Vector2 localPoint) ? localPoint : Vector2.zero;
    }

    public static Vector2 GetRectScreenCenter(RectTransform rect, Camera camera)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return RectTransformUtility.WorldToScreenPoint(camera, (corners[0] + corners[2]) * 0.5f);
    }

    public static Camera ResolveUiCamera(RectTransform rect, Camera fallbackCamera)
    {
        if (rect == null) return fallbackCamera;
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return canvas.worldCamera != null ? canvas.worldCamera : fallbackCamera;
    }

    public static Vector2 EvaluateQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }
}
