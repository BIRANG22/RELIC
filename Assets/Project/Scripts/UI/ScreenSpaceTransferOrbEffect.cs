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

    [Header("Conversion Stream Spread")]
    [Tooltip("Maximum horizontal deviation from the straight start-to-HUD path (screen pixels). Each orb chooses left or right randomly.")]
    [SerializeField] private Vector2 conversionSideSpreadRange = new(240f, 460f);
    [Tooltip("Additional vertical deviation from the straight start-to-HUD path at the middle of the arc (screen pixels). ")]
    [SerializeField] private Vector2 conversionVerticalSpreadRange = new(-170f, 210f);

    [Header("UI Rendering")]
    [SerializeField, Min(10002)] private int sortingOrder = 20000;

    [Header("Finish")]
    [Tooltip("Maximum time to wait for the prefab's own particle trails after stopping emission.")]
    [SerializeField, Min(0f)] private float maxParticleFinishWait = 3f;

    private Canvas effectCanvas;
    private RectTransform effectRect;
    private RectTransform movingRoot;
    private CanvasGroup conversionStartupCanvasGroup;
    private GameObject vfxInstance;
    private ParticleSystem[] particles;
    private bool started;
    // Only the conversion-specific API enables in-flight particle color interpolation.
    private bool conversionGradientActive;
    private Color conversionStartColor;
    private Color conversionEndColor;
    private float conversionColorStart;
    private float conversionColorEnd;
    private ParticleSystem.Particle[][] conversionParticleBuffers;
    // Each spawned transfer owns these visuals. No shared prefab/material tint is changed.
    private TrailRenderer[] conversionTrails;
    private SpriteRenderer[] conversionSprites;
    private UnityEngine.UI.Graphic[] conversionUiGraphics;
    private Color[] conversionSpriteBaseColors;
    private Color[] conversionUiBaseColors;
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

    /// <summary>Always tint the moving particles using the explicitly supplied reward color.</summary>
    public void PlayDetachedForcedColor(Vector2 startScreen, Vector2 endScreen, Color color)
    {
        if (!started && isActiveAndEnabled)
            StartCoroutine(PlayInternal(startScreen, endScreen, color, true));
    }

    /// <summary>
    /// Conversion-only transfer: randomly launches left or right and follows one
    /// continuous cubic Bezier curve into the destination. Other acquisition
    /// transfer animations retain their existing motion.
    /// </summary>
    public void PlayDetachedForcedColorSmoothRandom(Vector2 startScreen, Vector2 endScreen, Color color)
    {
        if (!started && isActiveAndEnabled)
            StartCoroutine(PlayInternal(startScreen, endScreen, color, true, true));
    }

    /// <summary>
    /// Conversion-only stream with a continuous red-to-blue color transition.
    /// Transition positions are normalized travel progress (0=start, 1=arrival).
    /// Other orb APIs retain their original colors and motion.
    /// </summary>
    public void PlayDetachedConversionGradient(Vector2 startScreen, Vector2 endScreen,
        Color startColor, Color endColor, float colorChangeStart, float colorChangeEnd)
    {
        if (started || !isActiveAndEnabled) return;
        StartCoroutine(PlayConversionGradientRoutine(startScreen, endScreen,
            startColor, endColor, colorChangeStart, colorChangeEnd));
    }

    private IEnumerator PlayConversionGradientRoutine(Vector2 startScreen, Vector2 endScreen,
        Color startColor, Color endColor, float colorChangeStart, float colorChangeEnd)
    {
        conversionGradientActive = true;
        conversionStartColor = startColor;
        conversionEndColor = endColor;
        conversionColorStart = Mathf.Clamp(colorChangeStart, 0f, 0.999f);
        conversionColorEnd = Mathf.Clamp(colorChangeEnd, conversionColorStart + 0.001f, 1f);
        yield return PlayInternal(startScreen, endScreen, startColor, true, true);
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

    private IEnumerator PlayInternal(Vector2 startScreen, Vector2 endScreen, Color color, bool applyColor, bool smoothRandomLaunch = false)
    {
        if (started) yield break;
        started = true;

        if (effectRect == null || vfxPrefab == null)
        {
            Debug.LogWarning("[ScreenSpaceTransferOrbEffect] Assign Moving VFX / Vfx Prefab.", this);
            Destroy(gameObject);
            yield break;
        }

        // Keep the entire hierarchy inactive until every renderer and particle has
        // its conversion launch color. Instantiating an active prefab directly
        // can run OnEnable/PlayOnAwake with the prefab's blue tint first.
        GameObject holder = new GameObject("MovingVFX", typeof(RectTransform));
        holder.SetActive(false);
        movingRoot = holder.GetComponent<RectTransform>();
        // UIParticle builds its Canvas mesh after ParticleSystem.Play(). Prevent an
        // uninitialized mesh from becoming visible while its tint is still blue.
        // This temporary CanvasGroup belongs only to conversion instances.
        if (conversionGradientActive)
        {
            conversionStartupCanvasGroup = holder.AddComponent<CanvasGroup>();
            conversionStartupCanvasGroup.alpha = 0f;
            conversionStartupCanvasGroup.interactable = false;
            conversionStartupCanvasGroup.blocksRaycasts = false;
        }
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
        // The parent is still inactive: none of the prefab's visuals can render yet.
        // Preserve the prefab's own active state for the non-conversion API.
        particles = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
        if (conversionGradientActive)
        {
            conversionParticleBuffers = new ParticleSystem.Particle[particles.Length][];
            conversionTrails = vfxInstance.GetComponentsInChildren<TrailRenderer>(true);
            conversionSprites = vfxInstance.GetComponentsInChildren<SpriteRenderer>(true);
            conversionUiGraphics = vfxInstance.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            conversionSpriteBaseColors = new Color[conversionSprites.Length];
            conversionUiBaseColors = new Color[conversionUiGraphics.Length];
            for (int i = 0; i < conversionSprites.Length; i++)
                conversionSpriteBaseColors[i] = conversionSprites[i].color;
            for (int i = 0; i < conversionUiGraphics.Length; i++)
                conversionUiBaseColors[i] = conversionUiGraphics[i].color;
            foreach (TrailRenderer trail in conversionTrails)
                if (trail != null) trail.Clear();
            foreach (ParticleSystem particle in particles)
            {
                if (particle == null) continue;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Clear(true);
                // These prefab modules otherwise recolor particles independently of
                // the moving orb's red-to-blue progress.
                var lifetimeColor = particle.colorOverLifetime;
                lifetimeColor.enabled = false;
                var speedColor = particle.colorBySpeed;
                speedColor.enabled = false;
                var particleTrails = particle.trails;
                if (particleTrails.enabled)
                {
                    particleTrails.inheritParticleColor = true;
                    particleTrails.colorOverLifetime = new ParticleSystem.MinMaxGradient(Color.white);
                    particleTrails.colorOverTrail = new ParticleSystem.MinMaxGradient(Color.white);
                }
                ParticleSystem.MainModule launchMain = particle.main;
                launchMain.playOnAwake = false;
                launchMain.prewarm = false;
                launchMain.startColor = new ParticleSystem.MinMaxGradient(conversionStartColor);
            }
            UpdateConversionParticleColors(0f);
        }
        // Show the fully initialized effect only after its red launch tint is set.
        holder.SetActive(true);
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
        // Reapply launch tint after Play: sub-emitters and prefab initialization may
        // have just created particles using their original color settings.
        if (conversionGradientActive)
        {
            UpdateConversionParticleColors(0f);
            // Allow the UIParticle mesh to rebuild once while hidden. Unity's
            // UI Particle component can otherwise display a cached prefab-tinted
            // mesh before the first particle-color synchronization.
            yield return null;
            UpdateConversionParticleColors(0f);
            if (conversionStartupCanvasGroup != null)
                conversionStartupCanvasGroup.alpha = 1f;
        }

        yield return smoothRandomLaunch
            ? PlaySmoothRandomRoutine(startScreen, endScreen)
            : PlayRoutine(startScreen, endScreen);
    }

    // Conversion-only trajectory: one quadratic Bezier from the source to the HUD.
    // There is no intermediate fan-out point and no second "suck" phase, so
    // the particles follow a smooth arc without bouncing or changing curves.
    private IEnumerator PlaySmoothRandomRoutine(Vector2 startScreen, Vector2 endScreen)
    {
        float durationMultiplier = Random.Range(
            Mathf.Min(durationMultiplierRange.x, durationMultiplierRange.y),
            Mathf.Max(durationMultiplierRange.x, durationMultiplierRange.y));
        float safeDuration = Mathf.Max(0.01f, duration * durationMultiplier);

        // The control point determines the curvature of the complete path.
        // A quadratic Bezier reaches half of the control-point deviation at t=0.5,
        // so doubling the desired deviation gives the requested midpoint spread.
        float side = Random.value < 0.5f ? -1f : 1f;
        float horizontalDeviation = Random.Range(
            Mathf.Max(0f, Mathf.Min(conversionSideSpreadRange.x, conversionSideSpreadRange.y)),
            Mathf.Max(0f, Mathf.Max(conversionSideSpreadRange.x, conversionSideSpreadRange.y)));
        float verticalDeviation = Random.Range(
            Mathf.Min(conversionVerticalSpreadRange.x, conversionVerticalSpreadRange.y),
            Mathf.Max(conversionVerticalSpreadRange.x, conversionVerticalSpreadRange.y));
        Vector2 middle = (startScreen + endScreen) * 0.5f;
        Vector2 control = middle + 2f * new Vector2(side * horizontalDeviation, verticalDeviation);

        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            // Gradual acceleration/deceleration without introducing a new segment.
            t = t * t * (3f - 2f * t);
            MoveToScreenPosition(EvaluateQuadraticBezier(startScreen, control, endScreen, t));
            if (conversionGradientActive) UpdateConversionParticleColors(t);
            yield return null;
        }

        MoveToScreenPosition(endScreen);
        if (conversionGradientActive) UpdateConversionParticleColors(1f);
        ReachedDestination?.Invoke();
        // Each conversion orb is independent. Destroy it as soon as it reaches
        // the Blue Dustium HUD instead of waiting for every particle/trail to die.
        // Other acquisition orb paths retain their existing trail fade behavior.
        if (conversionGradientActive)
        {
            Destroy(gameObject);
            yield break;
        }
        yield return FinishParticlesAndDestroy();
    }

    // Changing only main.startColor would affect newly emitted particles but not
    // particles already in flight. Update both for a visible transition across
    // the complete moving VFX, including existing emitted particles.
    private void UpdateConversionParticleColors(float progress)
    {
        if (particles == null || conversionParticleBuffers == null) return;
        float blend = Mathf.InverseLerp(conversionColorStart, conversionColorEnd, progress);
        blend = blend * blend * (3f - 2f * blend);
        Color color = Color.Lerp(conversionStartColor, conversionEndColor, blend);
        // The visual tint belongs to THIS transfer instance and is evaluated using
        // its own travel progress, not the global stream launch/arrival count.
        if (conversionSprites != null)
        {
            for (int i = 0; i < conversionSprites.Length; i++)
            {
                if (conversionSprites[i] == null) continue;
                Color baseColor = conversionSpriteBaseColors[i];
                conversionSprites[i].color = new Color(
                    color.r, color.g, color.b, baseColor.a * color.a);
            }
        }
        if (conversionUiGraphics != null)
        {
            for (int i = 0; i < conversionUiGraphics.Length; i++)
            {
                if (conversionUiGraphics[i] == null) continue;
                Color baseColor = conversionUiBaseColors[i];
                conversionUiGraphics[i].color = new Color(
                    color.r, color.g, color.b, baseColor.a * color.a);
            }
        }
        if (conversionTrails != null)
        {
            for (int i = 0; i < conversionTrails.Length; i++)
            {
                TrailRenderer trail = conversionTrails[i];
                if (trail == null) continue;
                // A single uniform tint for the whole existing trail: prevents
                // a red tail remaining behind a blue moving orb.
                Gradient originalGradient = trail.colorGradient;
                GradientAlphaKey[] alphas = originalGradient.alphaKeys;
                trail.colorGradient = new Gradient
                {
                    colorKeys = new[] {
                        new GradientColorKey(color, 0f),
                        new GradientColorKey(color, 1f)
                    },
                    alphaKeys = alphas
                };
            }
        }
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem system = particles[i];
            if (system == null) continue;
            var main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color);
            int count = system.particleCount;
            if (count == 0) continue;
            var buffer = conversionParticleBuffers[i];
            if (buffer == null || buffer.Length < count)
                conversionParticleBuffers[i] = buffer = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(count)];
            int activeCount = system.GetParticles(buffer);
            for (int j = 0; j < activeCount; j++)
                buffer[j].startColor = color;
            system.SetParticles(buffer, activeCount);
        }
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

        yield return FinishParticlesAndDestroy();
    }

    private IEnumerator FinishParticlesAndDestroy()
    {
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
