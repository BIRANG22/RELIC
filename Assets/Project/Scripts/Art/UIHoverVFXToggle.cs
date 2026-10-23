using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIHoverVFXToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover VFX")]
    [Tooltip("씬의 VFX 오브젝트 또는 프로젝트의 VFX 프리팹을 지정합니다.")]
    [SerializeField] private GameObject targetVFX;

    [Tooltip("VFX 프리팹을 생성할 부모 위치입니다. 비워두면 현재 Transform을 사용합니다.")]
    [SerializeField] private Transform vfxSpawnPoint;

    [Header("VFX Fade")]
    [Min(0f)][SerializeField] private float fadeInDuration = 0.25f;
    [Min(0f)][SerializeField] private float fadeOutDuration = 0.25f;
    [Tooltip("일시정지 상태에서도 페이드 효과를 실행합니다.")]
    [SerializeField] private bool useUnscaledTime = true;
    [Tooltip("Visual Effect Graph 사용 시 그래프 Blackboard에서 노출한 Float 속성 이름입니다. 해당 속성을 출력 알파에 연결해야 합니다.")]
    [SerializeField] private string vfxGraphAlphaProperty = "HoverAlpha";

    [Header("Character Name")]
    [SerializeField] private string characterName;
    [SerializeField] private TMP_Text characterNameText;
    [Tooltip("비워두면 Character Name Text 오브젝트를 사용합니다.")]
    [SerializeField] private GameObject characterNameRoot;

    [Header("Hover Detection")]
    [SerializeField] private bool detectUIRectHover = true;

    [Header("Option")]
    [SerializeField] private bool restoreOnPointerExit = true;

    // The selection effect belongs to the lobby character prefab but that prefab is
    // also reused by battle/event rooms. Only its SelectPoint VFX is lobby-only.
    private bool IsLobbySelectionEffect
    {
        get
        {
            if (vfxSpawnPoint != null && string.Equals(vfxSpawnPoint.name, "SelectPoint", StringComparison.OrdinalIgnoreCase))
                return true;
            return targetVFX != null && targetVFX.name.StartsWith("Vfx_Lobby_select", StringComparison.OrdinalIgnoreCase);
        }
    }

    private bool IsSceneRestricted => IsLobbySelectionEffect &&
        !string.Equals(SceneManager.GetActiveScene().name, "Lobby", StringComparison.OrdinalIgnoreCase);

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private bool isHovered;
    private bool wasRectHovered;
    private GameObject runtimeVFX;
    private float fadeAlpha;
    private float fadeTarget;

    private readonly List<ParticleFader> particleFaders = new List<ParticleFader>();
    private readonly List<GraphFader> graphFaders = new List<GraphFader>();

    private sealed class ParticleFader
    {
        public ParticleSystem system;
        public ParticleSystem.Particle[] particles = new ParticleSystem.Particle[0];
        public readonly Dictionary<uint, Color32> originalColors = new Dictionary<uint, Color32>();
        public readonly HashSet<uint> aliveSeeds = new HashSet<uint>();

        public void Apply(float alpha)
        {
            if (system == null || !system.gameObject.activeInHierarchy) return;
            int count = system.particleCount;
            if (particles.Length < count) particles = new ParticleSystem.Particle[count];
            count = system.GetParticles(particles);
            aliveSeeds.Clear();
            for (int i = 0; i < count; i++)
            {
                uint seed = particles[i].randomSeed;
                aliveSeeds.Add(seed);
                if (!originalColors.TryGetValue(seed, out Color32 original))
                {
                    original = particles[i].startColor;
                    originalColors[seed] = original;
                }
                Color32 tinted = original;
                tinted.a = (byte)Mathf.RoundToInt(original.a * alpha);
                particles[i].startColor = tinted;
            }
            if (count > 0) system.SetParticles(particles, count);
            // 파티클이 종료되어 재생성된 경우 이전 캐시를 정리합니다.
            if (originalColors.Count > count)
            {
                var stale = new List<uint>();
                foreach (var seed in originalColors.Keys)
                    if (!aliveSeeds.Contains(seed)) stale.Add(seed);
                for (int i = 0; i < stale.Count; i++) originalColors.Remove(stale[i]);
            }
        }
    }

    // Visual Effect Graph 패키지가 없는 프로젝트에서도 컴파일되도록 리플렉션으로 연결합니다.
    private sealed class GraphFader
    {
        public Component component;
        public MethodInfo setFloat;
        public string property;
        public void Apply(float alpha)
        {
            if (component != null && setFloat != null)
                setFloat.Invoke(component, new object[] { property, alpha });
        }
    }

    public static bool IsPointerBlockedByUI()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return false;
        var pointer = new PointerEventData(eventSystem) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        eventSystem.RaycastAll(pointer, results);
        for (int i = 0; i < results.Count; i++)
            if (results[i].module is GraphicRaycaster) return true;
        return false;
    }

    private bool IsWorldCharacter => GetComponentInParent<LobbyWorldCharacterClickRelay>() != null ||
        GetComponent<LobbyWorldCharacterClickRelay>() != null;

    private bool IsHoverBlocked => IsWorldCharacter &&
        (IsPointerBlockedByUI() || UIPanelButton.IsMenuPanelOpen || LobbyPositionModalInputBlocker.IsBlocked);

    public void Configure(GameObject vfx)
    {
        SetVFXVisible(false);
        if (runtimeVFX != null && runtimeVFX != targetVFX) Destroy(runtimeVFX);
        runtimeVFX = null;
        targetVFX = vfx;
        particleFaders.Clear();
        graphFaders.Clear();
        SetNormalValueImmediately();
    }

    private void Awake()
    {
        FindReferences();
        SetNormalValueImmediately();
        SetCharacterNameVisible(false);
    }

    private void OnEnable()
    {
        FindReferences();
        SetNormalValueImmediately();
        isHovered = false;
        wasRectHovered = false;
        SetCharacterNameVisible(false);
    }

    private void OnDisable()
    {
        SetCharacterNameVisible(false);
        SetNormalValueImmediately();
        isHovered = false;
        wasRectHovered = false;
    }

    private void OnDestroy()
    {
        if (runtimeVFX != null && runtimeVFX != targetVFX) Destroy(runtimeVFX);
    }

    private void Update()
    {
        if (IsSceneRestricted)
        {
            // Remove any VFX left visible by prefab initialization or a scene change.
            if (isHovered || fadeAlpha > 0f || fadeTarget > 0f)
            {
                isHovered = false;
                wasRectHovered = false;
                SetCharacterNameVisible(false);
                SetNormalValueImmediately();
            }
            return;
        }

        if (IsHoverBlocked)
        {
            wasRectHovered = false;
            SetHoverState(false);
        }
        else DetectUIHoverDirectly();

        float duration = fadeTarget > fadeAlpha ? fadeInDuration : fadeOutDuration;
        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float next = duration <= 0f ? fadeTarget : Mathf.MoveTowards(fadeAlpha, fadeTarget, delta / duration);
        if (!Mathf.Approximately(next, fadeAlpha))
        {
            fadeAlpha = next;
            ApplyFade(fadeAlpha);
        }
        if (fadeAlpha <= 0f && fadeTarget <= 0f)
            SetVFXVisible(false);
    }

    public void OnPointerEnter(PointerEventData eventData) => SetHoverState(!IsHoverBlocked);
    public void OnPointerExit(PointerEventData eventData) => SetHoverState(false);
    private void OnMouseEnter() => SetHoverState(!IsHoverBlocked);
    private void OnMouseExit() => SetHoverState(false);

    private void DetectUIHoverDirectly()
    {
        if (!detectUIRectHover || rectTransform == null) return;
        bool rectHovered = RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform, Input.mousePosition, GetUICamera());
        if (rectHovered == wasRectHovered) return;
        wasRectHovered = rectHovered;
        SetHoverState(rectHovered);
    }

    private void SetHoverState(bool hovered)
    {
        if (hovered && (IsHoverBlocked || IsSceneRestricted)) hovered = false;
        if (isHovered == hovered) return;
        isHovered = hovered;
        SetCharacterNameVisible(hovered);
        if (hovered)
        {
            // 활성화 전에 페이드 값을 0으로 설정해 처음 프레임부터 투명하게 시작합니다.
            if (fadeAlpha <= 0f) fadeAlpha = 0f;
            SetVFXVisible(true);
            ApplyFade(fadeAlpha);
            fadeTarget = 1f;
        }
        else if (restoreOnPointerExit) fadeTarget = 0f;
    }

    private GameObject GetVFX(bool create)
    {
        if (targetVFX == null) return null;
        if (targetVFX.scene.IsValid())
        {
            if (targetVFX == gameObject)
            {
                Debug.LogWarning("[UIHoverVFXToggle] 자기 자신 대신 별도의 VFX 오브젝트를 지정해야 합니다.", this);
                return null;
            }
            return targetVFX;
        }
        if (runtimeVFX == null && create)
        {
            Transform anchor = vfxSpawnPoint != null ? vfxSpawnPoint : transform;
            runtimeVFX = Instantiate(targetVFX, anchor);
            runtimeVFX.transform.localPosition = Vector3.zero;
            runtimeVFX.transform.localRotation = Quaternion.identity;
            particleFaders.Clear();
            graphFaders.Clear();
        }
        return runtimeVFX;
    }

    private void SetVFXVisible(bool visible)
    {
        GameObject vfx = GetVFX(visible);
        if (vfx == null) return;
        if (vfx.activeSelf != visible) vfx.SetActive(visible);
        if (visible && particleFaders.Count == 0 && graphFaders.Count == 0) FindFadeComponents(vfx);
    }

    private void FindFadeComponents(GameObject vfx)
    {
        particleFaders.Clear();
        graphFaders.Clear();
        foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
            particleFaders.Add(new ParticleFader { system = ps });
        foreach (var component in vfx.GetComponentsInChildren<Component>(true))
        {
            if (component == null || component.GetType().FullName != "UnityEngine.VFX.VisualEffect") continue;
            Type type = component.GetType();
            MethodInfo hasFloat = type.GetMethod("HasFloat", new[] { typeof(string) });
            MethodInfo setFloat = type.GetMethod("SetFloat", new[] { typeof(string), typeof(float) });
            if (hasFloat == null || setFloat == null || string.IsNullOrEmpty(vfxGraphAlphaProperty) ||
                !(bool)hasFloat.Invoke(component, new object[] { vfxGraphAlphaProperty }))
            {
                Debug.LogWarning("[UIHoverVFXToggle] Visual Effect Graph의 노출된 Float 속성 '" +
                    vfxGraphAlphaProperty + "'을 찾을 수 없습니다. 출력 알파와 연결해 주세요.", component);
                continue;
            }
            graphFaders.Add(new GraphFader { component = component, setFloat = setFloat, property = vfxGraphAlphaProperty });
        }
    }

    private void ApplyFade(float alpha)
    {
        for (int i = 0; i < particleFaders.Count; i++) particleFaders[i].Apply(alpha);
        for (int i = 0; i < graphFaders.Count; i++) graphFaders[i].Apply(alpha);
    }

    private void SetCharacterNameVisible(bool visible)
    {
        if (visible && characterNameText != null) characterNameText.text = characterName;
        GameObject root = characterNameRoot;
        if (root == null && characterNameText != null) root = characterNameText.gameObject;
        if (root != null && root.activeSelf != visible) root.SetActive(visible);
    }

    private void FindReferences()
    {
        rectTransform = transform as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private Camera GetUICamera()
    {
        if (parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return parentCanvas.worldCamera;
    }

    public void SetNormalValueImmediately()
    {
        fadeAlpha = 0f;
        fadeTarget = 0f;
        ApplyFade(0f);
        SetVFXVisible(false);
    }

    public void SetHoverValueImmediately()
    {
        if (IsSceneRestricted)
        {
            SetNormalValueImmediately();
            return;
        }
        SetVFXVisible(true);
        fadeAlpha = 1f;
        fadeTarget = 1f;
        ApplyFade(1f);
    }
}
