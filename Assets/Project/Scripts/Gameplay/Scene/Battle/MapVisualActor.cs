using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapVisualActor : MonoBehaviour
{
    [SerializeField] private string visualObjectId;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform vfxRoot;
    [SerializeField] private List<MapVisualActionEntry> actions = new();

    [Header("World VFX Proxy")]
    [SerializeField] private bool useWorldVfxProxy = true;
    [SerializeField] private string vfxLayerName = "VFX";
    [SerializeField] private BattleVfxProxyBlendMode worldVfxProxyBlendMode = BattleVfxProxyBlendMode.Additive;
    [SerializeField, Min(1)] private int worldVfxRenderTextureWidth = 512;
    [SerializeField, Min(1)] private int worldVfxRenderTextureHeight = 512;
    [SerializeField, Min(0.01f)] private float worldVfxRenderCameraOrthographicSize = 5f;
    [SerializeField, Min(0.01f)] private float worldVfxProxyWorldHeight = 10f;
    [SerializeField] private Vector3 worldVfxProxyWorldOffset = Vector3.zero;
    [SerializeField] private string worldVfxProxySortingLayerName;
    [SerializeField] private int worldVfxProxySortingOrderOffset = 1;
    [SerializeField] private float worldVfxProxySortingWorldYOffset = 0f;
    [SerializeField, Min(0.01f)] private float worldVfxProxyYMultiplier = 100f;

    private string runtimeVisualObjectId;
    private readonly List<BattleWorldVfxHandle> spawnedWorldVfxHandles = new();
    private readonly Dictionary<Transform, Coroutine> shakeRoutines = new();
    private readonly Dictionary<GameObject, Coroutine> fadeRoutines = new();
    private readonly Dictionary<Transform, Vector3> shakeOriginalLocalPositions = new();

    public string VisualObjectId
    {
        get
        {
            string runtimeId = NormalizeId(runtimeVisualObjectId);
            return !string.IsNullOrEmpty(runtimeId)
                ? runtimeId
                : NormalizeId(visualObjectId);
        }
    }

    public void SetRuntimeVisualObjectId(string id)
    {
        runtimeVisualObjectId = NormalizeId(id);
    }

    public bool TryPlayAction(string actionId)
    {
        actionId = NormalizeId(actionId);

        if (string.IsNullOrEmpty(actionId) || actions == null)
            return false;

        bool matched = false;

        for (int i = 0; i < actions.Count; i++)
        {
            MapVisualActionEntry action = actions[i];
            if (action == null || !action.Matches(actionId))
                continue;

            action.Play(this);
            matched = true;
        }

        return matched;
    }

    public bool TryReverseAction(string actionId)
    {
        actionId = NormalizeId(actionId);

        if (string.IsNullOrEmpty(actionId) || actions == null)
            return false;

        bool matched = false;

        for (int i = 0; i < actions.Count; i++)
        {
            MapVisualActionEntry action = actions[i];
            if (action == null || !action.Matches(actionId))
                continue;

            if (action.TryReverseAnimator(this))
                matched = true;
        }

        return matched;
    }

    internal void StartAnimatorReverse(
        Animator targetAnimator,
        int layerIndex,
        int playedStateHash,
        float playedStateLength,
        int originalStateHash,
        float originalNormalizedTime)
    {
        if (targetAnimator == null || playedStateHash == 0)
            return;

        StartCoroutine(ReverseAnimatorRoutine(
            targetAnimator,
            layerIndex,
            playedStateHash,
            playedStateLength,
            originalStateHash,
            originalNormalizedTime));
    }

    private IEnumerator ReverseAnimatorRoutine(
        Animator targetAnimator,
        int layerIndex,
        int playedStateHash,
        float playedStateLength,
        int originalStateHash,
        float originalNormalizedTime)
    {
        if (targetAnimator == null)
            yield break;

        targetAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        targetAnimator.speed = 0f;

        float duration = Mathf.Max(0.01f, playedStateLength);
        float elapsed = 0f;

        while (elapsed < duration && targetAnimator != null)
        {
            float reverseNormalizedTime = 1f - Mathf.Clamp01(elapsed / duration);
            targetAnimator.Play(playedStateHash, layerIndex, reverseNormalizedTime);
            targetAnimator.Update(0f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (targetAnimator == null)
            yield break;

        targetAnimator.Play(playedStateHash, layerIndex, 0f);
        targetAnimator.Update(0f);
        targetAnimator.speed = 1f;

        if (originalStateHash != 0)
        {
            float normalized = Mathf.Repeat(originalNormalizedTime, 1f);
            targetAnimator.Play(originalStateHash, layerIndex, normalized);
        }
        else
        {
            targetAnimator.Play(playedStateHash, layerIndex, 0f);
        }

        targetAnimator.Update(0f);
    }

    internal Animator ResolveAnimator()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        return animator;
    }

    internal Transform ResolveVfxRoot()
    {
        return vfxRoot != null ? vfxRoot : transform;
    }

    internal void SetTargetActive(GameObject target, bool activeState, float delay)
    {
        if (target == null)
            return;

        if (delay <= 0f)
        {
            target.SetActive(activeState);
            return;
        }

        StartCoroutine(SetTargetActiveAfterDelay(target, activeState, delay));
    }

    internal void DisableTarget(GameObject target, float delay)
    {
        SetTargetActive(target, false, delay);
    }

    internal void DisableTargetWithFade(GameObject target, float delay, float fadeDuration, bool fadeVfxColor, string vfxColorProperty)
    {
        if (target == null)
            return;

        if (fadeRoutines.TryGetValue(target, out Coroutine previous) && previous != null)
            StopCoroutine(previous);

        fadeRoutines.Remove(target);
        fadeRoutines[target] = StartCoroutine(DisableTargetFadeRoutine(
            target, Mathf.Max(0f, delay), Mathf.Max(0f, fadeDuration), fadeVfxColor, vfxColorProperty));
    }

    private IEnumerator DisableTargetFadeRoutine(GameObject target, float delay, float duration, bool fadeVfxColor, string vfxColorProperty)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (target == null)
            yield break;

        if (!target.activeInHierarchy || duration <= 0f)
        {
            target.SetActive(false);
            fadeRoutines.Remove(target);
            yield break;
        }

        SpriteRenderer[] sprites = target.GetComponentsInChildren<SpriteRenderer>(true);
        CanvasGroup[] groups = target.GetComponentsInChildren<CanvasGroup>(true);
        Graphic[] graphics = target.GetComponentsInChildren<Graphic>(true);
        Color[] spriteColors = new Color[sprites.Length];
        float[] groupAlphas = new float[groups.Length];
        Color[] graphicColors = new Color[graphics.Length];
        bool[] graphicUsesGroup = new bool[graphics.Length];

        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) spriteColors[i] = sprites[i].color;

        for (int i = 0; i < groups.Length; i++)
            if (groups[i] != null) groupAlphas[i] = groups[i].alpha;

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] == null) continue;
            graphicColors[i] = graphics[i].color;
            Transform t = graphics[i].transform;
            while (t != null)
            {
                if (t.GetComponent<CanvasGroup>() != null)
                {
                    graphicUsesGroup[i] = true;
                    break;
                }
                if (t == target.transform) break;
                t = t.parent;
            }
        }

        // VFX Graph의 Exposed Color(Vector4)와 ParticleSystem의 현재 파티클을 처리합니다.
        // Unity Visual Effect Graph 패키지가 없는 프로젝트에서도 컴파일되도록 리플렉션을 사용합니다.
        List<VfxColorSnapshot> vfxColors = fadeVfxColor
            ? CollectVfxColors(target, vfxColorProperty) : new List<VfxColorSnapshot>();
        ParticleSystem[] particleSystems = fadeVfxColor
            ? target.GetComponentsInChildren<ParticleSystem>(true) : new ParticleSystem[0];
        var particleBuffers = new ParticleSystem.Particle[particleSystems.Length][];
        float previousMultiplier = 1f;

        float elapsed = 0f;
        while (elapsed < duration && target != null && target.activeInHierarchy)
        {
            float multiplier = 1f - Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                Color c = spriteColors[i];
                c.a *= multiplier;
                sprites[i].color = c;
            }
            for (int i = 0; i < groups.Length; i++)
                if (groups[i] != null) groups[i].alpha = groupAlphas[i] * multiplier;
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null || graphicUsesGroup[i]) continue;
                Color c = graphicColors[i];
                c.a *= multiplier;
                graphics[i].color = c;
            }
            for (int i = 0; i < vfxColors.Count; i++)
                vfxColors[i].Apply(multiplier);

            // 기존에 재생 중인 파티클에도 알파 감소를 적용합니다.
            // 매 프레임 현재 알파를 직전 배율 대비 보정해 중복 감쇠를 방지합니다.
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem ps = particleSystems[i];
                if (ps == null) continue;
                int capacity = Mathf.Max(1, ps.main.maxParticles);
                if (particleBuffers[i] == null || particleBuffers[i].Length < capacity)
                    particleBuffers[i] = new ParticleSystem.Particle[capacity];
                int count = ps.GetParticles(particleBuffers[i]);
                float ratio = multiplier / Mathf.Max(0.0001f, previousMultiplier);
                for (int j = 0; j < count; j++)
                {
                    Color32 c = particleBuffers[i][j].startColor;
                    c.a = (byte)Mathf.Clamp(Mathf.RoundToInt(c.a * ratio), 0, 255);
                    particleBuffers[i][j].startColor = c;
                }
                if (count > 0) ps.SetParticles(particleBuffers[i], count);
            }
            previousMultiplier = multiplier;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target != null)
        {
            target.SetActive(false);
            // 원래 알파값을 복원해 다음 활성화 때 정상적으로 표시합니다.
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null) sprites[i].color = spriteColors[i];
            for (int i = 0; i < groups.Length; i++)
                if (groups[i] != null) groups[i].alpha = groupAlphas[i];
            for (int i = 0; i < graphics.Length; i++)
                if (graphics[i] != null && !graphicUsesGroup[i]) graphics[i].color = graphicColors[i];
            for (int i = 0; i < vfxColors.Count; i++)
                vfxColors[i].Restore();
        }
        fadeRoutines.Remove(target);
    }

    // VFX Graph의 Color Exposed Property에 접근합니다. 이름은 인스펙터에서 지정합니다.
    private sealed class VfxColorSnapshot
    {
        public Component Effect;
        public System.Reflection.MethodInfo Setter;
        public string PropertyName;
        public Vector4 Original;

        public void Apply(float multiplier)
        {
            if (Effect == null) return;
            Vector4 value = Original;
            value.w *= multiplier;
            Setter.Invoke(Effect, new object[] { PropertyName, value });
        }

        public void Restore()
        {
            if (Effect != null)
                Setter.Invoke(Effect, new object[] { PropertyName, Original });
        }
    }

    private static List<VfxColorSnapshot> CollectVfxColors(GameObject target, string propertyName)
    {
        var result = new List<VfxColorSnapshot>();
        if (string.IsNullOrWhiteSpace(propertyName)) return result;
        Component[] components = target.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null || component.GetType().FullName != "UnityEngine.VFX.VisualEffect")
                continue;
            Type type = component.GetType();
            var has = type.GetMethod("HasVector4", new[] { typeof(string) });
            var get = type.GetMethod("GetVector4", new[] { typeof(string) });
            var set = type.GetMethod("SetVector4", new[] { typeof(string), typeof(Vector4) });
            if (has == null || get == null || set == null) continue;
            try
            {
                if (!(bool)has.Invoke(component, new object[] { propertyName })) continue;
                Vector4 color = (Vector4)get.Invoke(component, new object[] { propertyName });
                result.Add(new VfxColorSnapshot
                {
                    Effect = component, Setter = set, PropertyName = propertyName, Original = color
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"MapVisualActor: VFX Color '{propertyName}' 읽기 실패: {ex.Message}");
            }
        }
        return result;
    }

    internal void PlayShake(Transform target, float delay, float duration, float strength, float frequency)
    {
        if (target == null || duration <= 0f || strength <= 0f)
            return;

        if (shakeRoutines.TryGetValue(target, out Coroutine running) && running != null)
        {
            StopCoroutine(running);

            if (shakeOriginalLocalPositions.TryGetValue(target, out Vector3 originalPosition))
                target.localPosition = originalPosition;
        }

        shakeOriginalLocalPositions[target] = target.localPosition;
        Coroutine routine = StartCoroutine(ShakeRoutine(
            target,
            Mathf.Max(0f, delay),
            Mathf.Max(0.01f, duration),
            Mathf.Max(0f, strength),
            Mathf.Max(0.01f, frequency)));
        shakeRoutines[target] = routine;
    }

    private IEnumerator ShakeRoutine(Transform target, float delay, float duration, float strength, float frequency)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (target == null)
            yield break;

        if (!shakeOriginalLocalPositions.TryGetValue(target, out Vector3 originalPosition))
            originalPosition = target.localPosition;

        float elapsed = 0f;
        float randomX = UnityEngine.Random.value * 1000f;
        float randomY = UnityEngine.Random.value * 1000f;

        while (elapsed < duration && target != null)
        {
            float t = elapsed * frequency;
            float fade = 1f - Mathf.Clamp01(elapsed / duration);
            float x = (Mathf.PerlinNoise(randomX + t, 0f) * 2f - 1f) * strength * fade;
            float y = (Mathf.PerlinNoise(0f, randomY + t) * 2f - 1f) * strength * fade;

            target.localPosition = originalPosition + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target != null)
            target.localPosition = originalPosition;

        shakeRoutines.Remove(target);
        shakeOriginalLocalPositions.Remove(target);
    }

    internal void PlayActivation(MapVisualActivationEntry activation)
    {
        if (activation == null || !activation.ApplyActiveState || activation.ActiveTarget == null)
            return;

        float delay = Mathf.Max(0f, activation.ActivationDelay);
        SetTargetActive(activation.ActiveTarget, activation.ActiveState, delay);

        if (activation.ActiveState && activation.DisableAfterDelay)
        {
            float disableDelay = delay + Mathf.Max(0f, activation.DisableDelay);
            DisableTarget(activation.ActiveTarget, disableDelay);
        }
    }

    private IEnumerator SetTargetActiveAfterDelay(GameObject target, bool activeState, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (target != null)
            target.SetActive(activeState);
    }

    internal bool TrySpawnWorldVfx(MapVisualActionEntry action, Transform actionRoot)
    {
        if (!useWorldVfxProxy || action == null || action.VfxPrefab == null)
            return false;

        int renderLayer = LayerMask.NameToLayer(vfxLayerName);
        if (renderLayer < 0)
            return false;

        Transform resolvedRoot = actionRoot != null ? actionRoot : ResolveVfxRoot();
        Vector3 worldPosition = resolvedRoot != null
            ? resolvedRoot.TransformPoint(action.VfxLocalPosition)
            : transform.TransformPoint(action.VfxLocalPosition);
        int visibleLayer = ResolveVisibleLayer(resolvedRoot, renderLayer);
        BattleVfxEntry entry = CreateWorldProxyEntry(action);
        float lifeTime = action.VfxLifetime > 0f
            ? action.VfxLifetime
            : float.PositiveInfinity;

        bool spawned = BattleWorldVfxRenderer.TrySpawnDetached(
            entry,
            worldPosition,
            renderLayer,
            visibleLayer,
            lifeTime,
            vfx => ConfigureWorldProxyVfxInstance(vfx, action, renderLayer),
            out BattleWorldVfxHandle handle);

        if (!spawned || handle == null)
            return false;

        spawnedWorldVfxHandles.Add(handle);
        return true;
    }

    private BattleVfxEntry CreateWorldProxyEntry(MapVisualActionEntry action)
    {
        return new BattleVfxEntry
        {
            prefab = action.VfxPrefab,
            renderMode = BattleVfxRenderMode.IndividualWorldRenderTexture,
            proxyBlendMode = worldVfxProxyBlendMode,
            renderTextureWidth = Mathf.Max(1, worldVfxRenderTextureWidth),
            renderTextureHeight = Mathf.Max(1, worldVfxRenderTextureHeight),
            renderCameraOrthographicSize = Mathf.Max(0.01f, worldVfxRenderCameraOrthographicSize),
            proxyWorldHeight = Mathf.Max(0.01f, worldVfxProxyWorldHeight),
            proxyWorldOffset = worldVfxProxyWorldOffset,
            proxySortingLayerName = ResolveWorldVfxSortingLayerName(),
            proxySortingOrderOffset = ResolveWorldVfxSortingOrderOffset(),
            proxySortingWorldYOffset = worldVfxProxySortingWorldYOffset,
            proxyYMultiplier = Mathf.Max(0.01f, worldVfxProxyYMultiplier)
        };
    }

    private string ResolveWorldVfxSortingLayerName()
    {
        if (!string.IsNullOrWhiteSpace(worldVfxProxySortingLayerName))
            return worldVfxProxySortingLayerName.Trim();

        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>(true);
        if (renderer != null && !string.IsNullOrWhiteSpace(renderer.sortingLayerName))
            return renderer.sortingLayerName;

        return "Default";
    }

    private int ResolveWorldVfxSortingOrderOffset()
    {
        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>(true);
        int baseOrder = renderer != null ? renderer.sortingOrder : 0;
        return baseOrder + worldVfxProxySortingOrderOffset;
    }

    private void ConfigureWorldProxyVfxInstance(
        GameObject vfx,
        MapVisualActionEntry action,
        int renderLayer)
    {
        if (vfx == null || action == null)
            return;

        Transform vfxTransform = vfx.transform;
        vfxTransform.localPosition = Vector3.zero;
        vfxTransform.localEulerAngles = action.VfxLocalEulerAngles;
        vfxTransform.localScale = action.VfxLocalScale;
        vfx.SetActive(true);

        SetLayerRecursively(vfx, renderLayer);
        EnsureVfxPauseController(vfx);
        RestartParticles(vfx);
        BattleVfxAudioUtility.PlayAndStripEmbeddedAudioSources(vfx, action.VfxPrefab, this);
    }

    private static int ResolveVisibleLayer(Transform source, int renderLayer)
    {
        if (source == null)
            return 0;

        return source.gameObject.layer == renderLayer
            ? 0
            : source.gameObject.layer;
    }

    private static void EnsureVfxPauseController(GameObject vfx)
    {
        if (vfx == null)
            return;

        if (vfx.GetComponent<BattleVfxPlaybackPauseController>() == null)
            vfx.AddComponent<BattleVfxPlaybackPauseController>();
    }

    private static void RestartParticles(GameObject vfx)
    {
        if (vfx == null)
            return;

        ParticleSystem[] particles = vfx.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null)
                continue;

            particles[i].Clear(true);
            particles[i].Play(true);
        }
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0)
            return;

        target.layer = layer;

        Transform targetTransform = target.transform;
        for (int i = 0; i < targetTransform.childCount; i++)
            SetLayerRecursively(targetTransform.GetChild(i).gameObject, layer);
    }

    private void OnDisable()
    {
        CleanupSpawnedWorldVfxHandles();
    }

    private void OnDestroy()
    {
        CleanupSpawnedWorldVfxHandles();
    }

    private void CleanupSpawnedWorldVfxHandles()
    {
        RestoreAllShakeTargets();

        for (int i = spawnedWorldVfxHandles.Count - 1; i >= 0; i--)
        {
            BattleWorldVfxHandle handle = spawnedWorldVfxHandles[i];
            if (handle != null)
                DestroyUnityObject(handle.gameObject);
        }

        spawnedWorldVfxHandles.Clear();
    }

    private void RestoreAllShakeTargets()
    {
        foreach (KeyValuePair<Transform, Vector3> pair in shakeOriginalLocalPositions)
        {
            if (pair.Key != null)
                pair.Key.localPosition = pair.Value;
        }

        shakeRoutines.Clear();
        shakeOriginalLocalPositions.Clear();
    }

    private static void DestroyUnityObject(UnityEngine.Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(target);
        else
            UnityEngine.Object.DestroyImmediate(target);
    }

    internal static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}

[Serializable]
public sealed class MapVisualActionEntry
{
    public string ActionId;

    [Header("Animator")]
    [Tooltip("이 Action ID가 실행될 때 사용할 Animator입니다. 비워두면 MapVisualActor의 기본 Animator를 사용합니다.")]
    public Animator Animator;
    public string AnimatorTrigger;
    [Tooltip("Trigger가 없는 Controller에서 선택 시 직접 재생할 상태 이름입니다.")]
    public string AnimatorStateName;

    [Header("VFX")]
    public GameObject VfxPrefab;
    public Transform VfxRootOverride;
    public Vector3 VfxLocalPosition;
    public Vector3 VfxLocalEulerAngles;
    public Vector3 VfxLocalScale = Vector3.one;
    public float VfxLifetime;

    [Header("Sprite")]
    [Tooltip("같은 Tint Color를 적용할 스프라이트 목록입니다.")]
    public List<SpriteRenderer> TintTargets = new();
    public bool ApplyTint;
    public Color TintColor = Color.white;

    // 기존 씬/프리팹에 저장되어 있던 단일 TintTarget 데이터를 유지하기 위한 레거시 필드입니다.
    [HideInInspector] public SpriteRenderer TintTarget;

    [Header("Transform")]
    public Transform ScaleTarget;
    public bool ApplyLocalScale;
    public Vector3 LocalScale = Vector3.one;

    [Header("Shake")]
    [Tooltip("이 액션이 실행될 때 흔들 오브젝트입니다.")]
    public Transform ShakeTarget;
    [Tooltip("Shake Target 흔들림을 사용할지 설정합니다.")]
    public bool ApplyShake;
    [Tooltip("액션 실행 후 흔들림이 시작되기까지 기다릴 시간(초)입니다.")]
    [Min(0f)] public float ShakeDelay;
    [Tooltip("흔들림이 유지되는 시간(초)입니다.")]
    [Min(0.01f)] public float ShakeDuration = 0.35f;
    [Tooltip("로컬 좌표 기준 흔들림의 최대 거리입니다.")]
    [Min(0f)] public float ShakeStrength = 0.08f;
    [Tooltip("흔들림 변화 속도입니다. 값이 클수록 더 빠르게 떨립니다.")]
    [Min(0.01f)] public float ShakeFrequency = 25f;

    [Header("Activation")]
    [Tooltip("이 액션에서 활성화/비활성화할 오브젝트 목록입니다.")]
    public List<MapVisualActivationEntry> Activations = new();

    [Header("Disable On Play")]
    [Tooltip("이 액션이 실행될 때 비활성화할 오브젝트 목록입니다.")]
    public List<GameObject> DisableTargetsOnPlay = new();
    [Tooltip("액션 실행 후 오브젝트를 비활성화하기까지 기다릴 시간(초)입니다. 0이면 즉시 비활성화됩니다.")]
    [Min(0f)] public float DisableDelayOnPlay;
    [Tooltip("Disable On Play 대상이 사라질 때 알파를 서서히 낮춥니다.")]
    public bool FadeOutOnDisable;
    [Tooltip("페이드아웃 진행 시간(초)입니다. 대기 시간이 끝나면 페이드아웃이 시작됩니다.")]
    [Min(0f)] public float DisableFadeDuration = 0.35f;
    [Tooltip("VFX Graph의 노출된 Color 및 ParticleSystem 파티클의 알파도 함께 낮춥니다.")]
    public bool FadeVfxColorOnDisable = true;
    [Tooltip("VFX Graph에서 Exposed로 설정한 Vector4/Color 프로퍼티 이름입니다.")]
    public string VfxColorPropertyName = "Color";

    // 기존 씬/프리팹에 저장되어 있던 단일 Activation 데이터를 유지하기 위한 레거시 필드입니다.
    // Inspector에는 숨기고 실행 시에는 계속 적용합니다.
    [HideInInspector] public GameObject ActiveTarget;
    [HideInInspector] public bool ApplyActiveState;
    [HideInInspector] public bool ActiveState = true;

    [NonSerialized] private Animator runtimeAnimator;
    [NonSerialized] private int runtimeAnimatorLayerIndex;
    [NonSerialized] private int runtimeOriginalStateHash;
    [NonSerialized] private float runtimeOriginalNormalizedTime;
    [NonSerialized] private int runtimePlayedStateHash;
    [NonSerialized] private float runtimePlayedStateLength;

    public bool Matches(string actionId)
    {
        return string.Equals(
            MapVisualActor.NormalizeId(ActionId),
            MapVisualActor.NormalizeId(actionId),
            StringComparison.Ordinal);
    }

    internal void Play(MapVisualActor owner)
    {
        PlayAnimator(owner);
        SpawnVfx(owner);
        ApplySpriteTint();
        ApplyTransformScale();
        ApplyShakeEffect(owner);
        ApplyActivation(owner);
        DisableTargets(owner);
    }

    private void PlayAnimator(MapVisualActor owner)
    {
        if (string.IsNullOrWhiteSpace(AnimatorTrigger) &&
            string.IsNullOrWhiteSpace(AnimatorStateName))
        {
            return;
        }

        Animator resolvedAnimator = Animator != null
            ? Animator
            : owner != null ? owner.ResolveAnimator() : null;
        if (resolvedAnimator == null)
            return;

        if (!resolvedAnimator.enabled)
            resolvedAnimator.enabled = true;

        resolvedAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        resolvedAnimator.speed = 1f;

        const int layerIndex = 0;
        AnimatorStateInfo originalState = resolvedAnimator.GetCurrentAnimatorStateInfo(layerIndex);
        runtimeAnimator = resolvedAnimator;
        runtimeAnimatorLayerIndex = layerIndex;
        runtimeOriginalStateHash = originalState.fullPathHash;
        runtimeOriginalNormalizedTime = originalState.normalizedTime;
        runtimePlayedStateHash = 0;
        runtimePlayedStateLength = 0f;

        if (!string.IsNullOrWhiteSpace(AnimatorTrigger))
        {
            resolvedAnimator.SetTrigger(AnimatorTrigger.Trim());
            resolvedAnimator.Update(0f);

            AnimatorStateInfo playedState = resolvedAnimator.IsInTransition(layerIndex)
                ? resolvedAnimator.GetNextAnimatorStateInfo(layerIndex)
                : resolvedAnimator.GetCurrentAnimatorStateInfo(layerIndex);
            runtimePlayedStateHash = playedState.fullPathHash;
            runtimePlayedStateLength = playedState.length;
            return;
        }

        if (!string.IsNullOrWhiteSpace(AnimatorStateName))
        {
            resolvedAnimator.Play(AnimatorStateName.Trim(), layerIndex, 0f);
            resolvedAnimator.Update(0f);

            AnimatorStateInfo playedState = resolvedAnimator.GetCurrentAnimatorStateInfo(layerIndex);
            runtimePlayedStateHash = playedState.fullPathHash;
            runtimePlayedStateLength = playedState.length;
        }
    }

    internal bool TryReverseAnimator(MapVisualActor owner)
    {
        Animator resolvedAnimator = runtimeAnimator != null
            ? runtimeAnimator
            : Animator != null
                ? Animator
                : owner != null ? owner.ResolveAnimator() : null;

        if (resolvedAnimator == null || runtimePlayedStateHash == 0 || owner == null)
            return false;

        owner.StartAnimatorReverse(
            resolvedAnimator,
            runtimeAnimatorLayerIndex,
            runtimePlayedStateHash,
            runtimePlayedStateLength,
            runtimeOriginalStateHash,
            runtimeOriginalNormalizedTime);
        return true;
    }

    private void SpawnVfx(MapVisualActor owner)
    {
        if (VfxPrefab == null)
            return;

        Transform parent = VfxRootOverride != null
            ? VfxRootOverride
            : owner != null
                ? owner.ResolveVfxRoot()
                : null;

        if (owner != null && owner.TrySpawnWorldVfx(this, parent))
            return;

        GameObject instance = UnityEngine.Object.Instantiate(VfxPrefab, parent, false);
        Transform instanceTransform = instance.transform;
        instanceTransform.localPosition = VfxLocalPosition;
        instanceTransform.localEulerAngles = VfxLocalEulerAngles;
        instanceTransform.localScale = VfxLocalScale;

        if (VfxLifetime > 0f)
            UnityEngine.Object.Destroy(instance, VfxLifetime);
    }

    private void ApplySpriteTint()
    {
        if (!ApplyTint)
            return;

        // 기존 단일 TintTarget 설정이 저장되어 있는 경우 그대로 유지합니다.
        if (TintTarget != null)
            TintTarget.color = TintColor;

        if (TintTargets == null)
            return;

        for (int i = 0; i < TintTargets.Count; i++)
        {
            SpriteRenderer target = TintTargets[i];
            if (target == null || target == TintTarget)
                continue;

            target.color = TintColor;
        }
    }

    private void ApplyTransformScale()
    {
        if (!ApplyLocalScale || ScaleTarget == null)
            return;

        ScaleTarget.localScale = LocalScale;
    }

    private void ApplyShakeEffect(MapVisualActor owner)
    {
        if (!ApplyShake || ShakeTarget == null || owner == null)
            return;

        owner.PlayShake(
            ShakeTarget,
            ShakeDelay,
            ShakeDuration,
            ShakeStrength,
            ShakeFrequency);
    }

    private void ApplyActivation(MapVisualActor owner)
    {
        // 기존 단일 Activation 설정이 저장되어 있는 경우 그대로 유지합니다.
        if (ApplyActiveState && ActiveTarget != null)
            ActiveTarget.SetActive(ActiveState);

        if (Activations == null)
            return;

        for (int i = 0; i < Activations.Count; i++)
        {
            MapVisualActivationEntry activation = Activations[i];
            if (activation == null || !activation.ApplyActiveState || activation.ActiveTarget == null)
                continue;

            float activationDelay = Mathf.Max(0f, activation.ActivationDelay);

            if (owner != null)
                owner.SetTargetActive(activation.ActiveTarget, activation.ActiveState, activationDelay);
            else if (activationDelay <= 0f)
                activation.ActiveTarget.SetActive(activation.ActiveState);

            if (activation.ActiveState && activation.DisableAfterDelay)
            {
                float disableDelay = activationDelay + Mathf.Max(0f, activation.DisableDelay);

                if (owner != null)
                    owner.DisableTarget(activation.ActiveTarget, disableDelay);
                else if (disableDelay <= 0f)
                    activation.ActiveTarget.SetActive(false);
            }
        }
    }

    private void DisableTargets(MapVisualActor owner)
    {
        if (DisableTargetsOnPlay == null)
            return;

        float delay = Mathf.Max(0f, DisableDelayOnPlay);

        for (int i = 0; i < DisableTargetsOnPlay.Count; i++)
        {
            GameObject target = DisableTargetsOnPlay[i];
            if (target == null)
                continue;

            if (owner != null)
            {
                if (FadeOutOnDisable && DisableFadeDuration > 0f)
                    owner.DisableTargetWithFade(target, delay, DisableFadeDuration, FadeVfxColorOnDisable, VfxColorPropertyName);
                else
                    owner.DisableTarget(target, delay);
            }
            else
                target.SetActive(false);
        }
    }
}

[Serializable]
public sealed class MapVisualActivationEntry
{
    [Header("Activation Target")]
    public GameObject ActiveTarget;
    public bool ApplyActiveState = true;
    public bool ActiveState = true;

    [Tooltip("이 Activation의 활성화/비활성화 상태와 Animator 재생을 적용하기까지 기다릴 시간(초)입니다. 0이면 즉시 적용됩니다.")]
    [Min(0f)] public float ActivationDelay;

    [Header("Auto Disable")]
    [Tooltip("ActiveState가 true일 때, 활성화 후 일정 시간이 지나면 자동으로 비활성화할지 설정합니다.")]
    public bool DisableAfterDelay;

    [Tooltip("활성화 후 자동으로 비활성화되기까지 기다릴 시간(초)입니다.")]
    [Min(0f)] public float DisableDelay = 1f;
}
