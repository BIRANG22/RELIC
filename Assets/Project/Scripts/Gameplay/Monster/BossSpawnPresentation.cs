using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public sealed class BossSpawnPresentation : MonoBehaviour
{

    // Keep the original enum IDs so existing serialized camera steps retain their actions.
    public enum SequenceAction
    {
        CameraMove = 0,
        CameraShake = 1,
        Wait = 2,
        RestoreCamera = 8,
        CameraHold = 10
    }

    [Serializable]
    public class PresentationStep
    {
        public string label;
        public SequenceAction action;
        [Header("Global Light 2D")]
        [Tooltip("이 스텝에서 연결된 Global Light 2D 밝기를 변경합니다. 체크하지 않으면 현재 밝기를 유지합니다.")]
        public bool overrideLightIntensity = false;
        [Tooltip("이 스텝의 목표 Global Light 2D 밝기입니다.")]
        [Min(0f)] public float lightIntensity = 0.8f;
        [Tooltip("스텝 Duration 동안 밝기가 변하는 곡선입니다. Duration이 0이면 즉시 적용합니다.")]
        public AnimationCurve lightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("카메라 스텝 시작 전 대기 시간(초). ")]
        [Min(0f)] public float startDelay = 0f;
        [Tooltip("CameraMove: 이동 시간 / CameraHold: 위치 유지 시간 / CameraShake: 흔들림 시간 / Wait: 대기 시간 / RestoreCamera: 복귀 시간")]
        [Min(0f)] public float duration = 0.35f;

        [Header("CameraMove")]
        [InspectorName("World Position")]
        [Tooltip("카메라가 이동할 월드 절대좌표(X, Y, Z)입니다. 보스 위치나 현재 카메라 위치에 더하지 않습니다.")]
        public Vector3 targetOffset = new Vector3(0f, 2f, -20f);

        [Header("CameraShake")]
        [Min(0f)] public float shakeStrength = 0.18f;
        [Min(0f)] public float shakeFrequency = 28f;
        public bool fadeOutShake = true;

        [Header("Step SFX (Sound DB)")]
        [Tooltip("이 스텝이 시작될 때 재생할 Sound DB SFX ID입니다. 비워두면 재생하지 않습니다.")]
        public string sfxId;
        [Tooltip("스텝 SFX 볼륨 배율입니다 (0~1).")]
        [Range(0f, 1f)] public float sfxVolume = 1f;

        [Header("Step VFX")]
        [Tooltip("이 스텝 시작 시 생성할 VFX 프리팹입니다. 비워두면 생성하지 않습니다.")]
        public GameObject vfxPrefab;
        [Tooltip("VFX 생성 기준 Transform입니다. 비워두면 BossSpawnPresentation 오브젝트를 기준으로 합니다.")]
        public Transform vfxOrigin;
        [Tooltip("기준 Transform의 로컬 좌표 기준 생성 위치 보정값입니다.")]
        public Vector3 vfxLocalOffset = Vector3.zero;
        [Tooltip("기준 Transform 회전에 추가할 VFX 로컬 회전값입니다.")]
        public Vector3 vfxLocalEulerAngles = Vector3.zero;
        [Tooltip("체크하면 기준 Transform의 자식으로 생성하여 이동/회전을 따라갑니다.")]
        public bool parentVfxToOrigin = false;
        [Tooltip("0이면 자동 제거하지 않습니다. 양수면 생성 후 해당 초가 지나면 제거합니다 (실시간 기준).")]
        [Min(0f)] public float vfxLifetime = 0f;
        [Tooltip("연출 종료 또는 취소 시 VFX를 제거합니다. 끄면 생성된 VFX가 남습니다.")]
        public bool destroyVfxOnPresentationEnd = true;
    }

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private BattleUnitAnimator unitAnimator;
    [Serializable]
    public class AnimationStep
    {
        [Tooltip("Animator Controller에 등록된 State 이름 (클립 에셋 직접 할당 불필요).")]
        public string stateName = "Appear01";
        [Tooltip("이 애니메이션을 실행하기 전 기다릴 시간(초).")]
        [Min(0f)] public float startDelay = 0f;
        [Tooltip("이 애니메이션의 재생 속도입니다. 1은 기본 속도, 2는 두 배 속도입니다.")]
        [Min(0.01f)] public float speed = 1f;
        [Tooltip("0이면 현재 State의 클립 길이를 개별 Speed로 나누어 자동 사용합니다. 양수면 지정한 시간(초)만큼 대기합니다.")]
        [Min(0f)] public float duration = 0f;
    }

    [Header("Animator State Sequence")]
    [Tooltip("Size를 변경해 원하는 개수만큼 애니메이션을 순차 실행합니다. 각 요소의 State Name을 Animator Controller의 State 이름으로 지정하세요.")]
    [SerializeField] private List<AnimationStep> animationSteps = new List<AnimationStep>
    {
        new AnimationStep { stateName = "Appear01" },
        new AnimationStep { stateName = "Appear02" }
    };

    // Previous inspector fields are kept hidden so existing prefab/scene values can migrate.
    [SerializeField, HideInInspector] private string firstAnimationStateName = "Appear01";
    [SerializeField, HideInInspector] private string secondAnimationStateName = "Appear02";
    [FormerlySerializedAs("firstDialogueDuration")]
    [SerializeField, HideInInspector] private float firstAnimationDuration;
    [FormerlySerializedAs("secondDialogueDuration")]
    [SerializeField, HideInInspector] private float secondAnimationDuration;
    [SerializeField, HideInInspector] private bool animationStepsMigrated;
    // Serialized legacy timing is retained only to migrate existing prefab values once.
    [SerializeField, HideInInspector] private float animationSpeed = 1f;
    [SerializeField, HideInInspector] private float animationStartDelay = 0f;
    [SerializeField, HideInInspector] private bool animationStepTimingMigrated;

    private void OnValidate()
    {
        MigrateAnimationStepSettings();
        if (sequenceSteps == null)
            return;
        foreach (PresentationStep step in sequenceSteps)
        {
            if (step != null)
                step.lightIntensity = Mathf.Max(0f, step.lightIntensity);
        }
    }

    private void MigrateAnimationStepSettings()
    {
        if (animationSteps == null)
            animationSteps = new List<AnimationStep>();

        if (!animationStepsMigrated)
        {
            // Preserve the older two-state inspector settings when converting to the list.
            if (animationSteps.Count == 2 &&
                animationSteps[0] != null && animationSteps[1] != null &&
                animationSteps[0].stateName == "Appear01" &&
                animationSteps[1].stateName == "Appear02")
            {
                animationSteps[0].stateName = firstAnimationStateName;
                animationSteps[0].duration = Mathf.Max(0f, firstAnimationDuration);
                animationSteps[1].stateName = secondAnimationStateName;
                animationSteps[1].duration = Mathf.Max(0f, secondAnimationDuration);
            }
            animationStepsMigrated = true;
        }

        if (!animationStepTimingMigrated)
        {
            // Fold the old common speed/delay into each existing step just once.
            for (int i = 0; i < animationSteps.Count; i++)
            {
                AnimationStep step = animationSteps[i];
                if (step == null)
                    continue;
                step.speed = Mathf.Max(0.01f, animationSpeed);
                step.startDelay = Mathf.Max(0f, step.startDelay) + Mathf.Max(0f, animationStartDelay);
            }
            animationStepTimingMigrated = true;
        }
    }

    [Serializable]
    public class TimedDialogue
    {
        [Tooltip("Localization 대사 키")]
        public string localizationKey;
        [TextArea(1, 3)] public string fallbackText;
        [Tooltip("이 대사를 표시할 시간(초). 시간이 끝나면 다음 대사로 넘어갑니다. 마지막 대사가 0이면 연출 종료까지 유지됩니다.")]
        [Min(0f)] public float displayDuration;

        [Header("Dialogue SFX")]
        [Tooltip("대사가 화면에 나타날 때 재생할 Sound DB의 SFX ID입니다. 비워두면 재생하지 않습니다.")]
        public string sfxId;
        [Tooltip("이 대사의 SFX 볼륨 배율입니다 (0~1).")]
        [Range(0f, 1f)] public float sfxVolume = 1f;
    }

    [Header("Timed Boss Dialogue")]
    [Tooltip("보스 스폰 연출 시작 후 첫 다이어로그를 표시하기까지의 대기 시간(초).")]
    [SerializeField, Min(0f)] private float firstDialogueStartDelay = 0f;
    [Tooltip("각 대사가 끝난 뒤 말풍선을 숨기고 다음 대사를 다시 표시하기까지의 시간(초).")]
    [SerializeField, Min(0f)] private float dialogueBetweenDelay = 0.25f;
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private RectTransform dialogueRect;
    [SerializeField] private Vector3 dialogueLocalPosition = new Vector3(0f, 16f, 0f);
    [Tooltip("첫 대사는 시작 딜레이 후 표시합니다. 이후 대사는 앞 대사의 Display Duration이 끝나면 말풍선을 숨긴 후 Dialogue Between Delay만큼 기다렸다가 표시합니다.")]
    [SerializeField] private List<TimedDialogue> timedDialogues = new()
    {
        new TimedDialogue { displayDuration = 2.5f, localizationKey = "battle.boss.arabella.appear.01", fallbackText = "들리시나요? 이토록 아름다운 기도가..." },
        new TimedDialogue { displayDuration = 0f, localizationKey = "battle.boss.arabella.appear.02", fallbackText = "고통은 잠시뿐입니다. 구원은 영원하니까요" }
    };
    private Coroutine dialogueCoroutine;
    private bool dialogueTimelineCompleted = true;

    [Header("Object Reveal Timing")]
    [Tooltip("보스 등장 연출이 시작된 뒤 TimedObjectRevealSequence를 시작할 때까지 기다리는 시간(초). 보스 애니메이션 종료를 기다리지 않습니다.")]
    [SerializeField, Min(0f)] private float objectRevealStartDelay = 0f;
    public float ObjectRevealStartDelay => Mathf.Max(0f, objectRevealStartDelay);

    [Header("Camera Sequence (비어 있으면 카메라 연출 없음)")]
    [Tooltip("카메라 동작만 순서대로 실행합니다. X/Y/Z는 월드 절대좌표이며, 애니메이션은 별도로 동시에 진행됩니다.")]
    [SerializeField] private PresentationStep[] sequenceSteps = new PresentationStep[0];

    [Header("Battle Effect Slide During Intro")]
    [Tooltip("보스 등장 연출 동안 BattleEffect 슬라이드 효과를 화면에 표시하고, 종료하면 대기 위치로 퇴장시킵니다.")]
    [SerializeField] private bool useBattleEffectSlide = true;
    [Tooltip("비워두면 BattleEffectPlaneSlideController.Instance를 자동 사용합니다.")]
    [SerializeField] private BattleEffectPlaneSlideController battleEffectSlideController;
    private BattleEffectPlaneSlideController activeIntroSlide;

    private bool hasPlayed;
    private bool ownsIntroCamera;
    private Camera introCamera;
    private Coroutine cameraBlendRoutine;
    private Vector3 cameraRestorePosition;
    private Quaternion cameraRestoreRotation;
    private float cameraRestoreOrthographicSize;
    private Vector3 cameraBasePosition;
    private float cameraBaseSize;

    // Reuse the Global Light 2D already assigned to BattleCameraController.
    private Light2D bossIntroLight;
    private bool bossIntroLightSaved;
    private float bossIntroOriginalIntensity;
    private Coroutine bossIntroLightRoutine;
    private readonly List<GameObject> presentationVfx = new List<GameObject>();

    public bool HasPlayed => hasPlayed;

    public bool CanPlay =>
        animator != null &&
        animator.runtimeAnimatorController != null &&
        unitAnimator != null;

    private void Awake()
    {
        ApplyDialogueLayout();
        HideDialogue();
    }

    private void Start()
    {
        PrepareFirstFrameIfIntroPlaying();
    }

    private void OnDisable()
    {
        CancelPresentation();
    }

    private void PrepareFirstFrameIfIntroPlaying()
    {
        if (!BattleSceneController.IsBattleRoomIntroPlaying || !CanPlay)
            return;

        if (animationSteps == null || animationSteps.Count == 0 ||
            animationSteps[0] == null || string.IsNullOrWhiteSpace(animationSteps[0].stateName))
            return;

        PlayAnimatorState(animationSteps[0].stateName);
        animator.speed = 0f;
    }

    public IEnumerator Play()
    {
        if (hasPlayed)
            yield break;

        MigrateAnimationStepSettings();
        hasPlayed = true;
        if (!CanPlay)
        {
            Debug.LogWarning("[BossSpawnPresentation] 등장 연출 참조가 완전하지 않아 연출을 건너뜁니다.", this);
            yield break;
        }

        // Use the normal battle-effect slide during this independent cinematic.
        BeginIntroBattleEffectSlide();

        // Dialogue timing starts here, alongside the independent camera and animation sequences.
        BeginDialogueTimeline();
        CaptureBossIntroLight();

        // Appearance clips run independently of the camera steps.
        animator.speed = 0f;
        bool cameraSequenceEnabled = sequenceSteps != null &&
            Array.Exists(sequenceSteps, step => step != null &&
                (step.action == SequenceAction.CameraMove ||
                 step.action == SequenceAction.CameraShake ||
                 step.action == SequenceAction.CameraHold ||
                 step.action == SequenceAction.RestoreCamera));
        if (cameraSequenceEnabled)
            BeginIntroCamera();

        bool animationCompleted = false;
        StartCoroutine(PlayAppearanceRoutine(() => animationCompleted = true));

        if (sequenceSteps != null)
        {
            foreach (PresentationStep step in sequenceSteps)
            {
                if (step == null)
                    continue;
                if (step.startDelay > 0f)
                    yield return WaitForDuration(step.startDelay);
                ApplyBossIntroStepLighting(step);
                SpawnStepVfx(step);
                PlayStepSfx(step);
                yield return ExecuteCameraStep(step);
            }
        }

        // Do not restore the camera early if its sequence finishes before the animations.
        while (!animationCompleted || !dialogueTimelineCompleted)
            yield return null;

        yield return RestoreIntroCameraRoutine();
        // Wait for the same reserve-position slide used after a turn.
        yield return EndIntroBattleEffectSlideRoutine();
        RestoreBossIntroLight();
        CleanupPresentationVfx();

        StopDialogueTimeline();
        animator.speed = 1f;
        unitAnimator.PlayIdle();
    }

    private IEnumerator PlayAppearanceRoutine(Action onComplete)
    {
        if (animationSteps != null)
        {
            for (int i = 0; i < animationSteps.Count; i++)
            {
                AnimationStep step = animationSteps[i];
                if (step == null || string.IsNullOrWhiteSpace(step.stateName))
                    continue;

                float delay = Mathf.Max(0f, step.startDelay);
                if (delay > 0f)
                    yield return WaitForDuration(delay);

                PlayAnimatorState(step.stateName, step.speed);
                yield return WaitForDuration(ResolvePresentationDuration(step.stateName, step.duration, step.speed));
            }
        }
        onComplete?.Invoke();
    }

    private void CaptureBossIntroLight()
    {
        // There is a single scene Global Light 2D reference on the camera controller.
        // Capture its initial value before any boss camera step modifies it.
        BattleCameraController controller = BattleCameraController.Instance;
        bossIntroLight = controller != null ? controller.MonsterPhaseGlobalLight2D : null;
        bossIntroLightSaved = bossIntroLight != null;
        if (bossIntroLightSaved)
            bossIntroOriginalIntensity = bossIntroLight.intensity;
    }

    private void ApplyBossIntroStepLighting(PresentationStep step)
    {
        if (step == null || !step.overrideLightIntensity || !bossIntroLightSaved || bossIntroLight == null)
            return;

        if (bossIntroLightRoutine != null)
        {
            StopCoroutine(bossIntroLightRoutine);
            bossIntroLightRoutine = null;
        }

        float target = Mathf.Max(0f, step.lightIntensity);
        float duration = Mathf.Max(0f, step.duration);
        if (duration <= 0f)
        {
            bossIntroLight.intensity = target;
            return;
        }
        bossIntroLightRoutine = StartCoroutine(BlendBossIntroLightRoutine(target, duration, step.lightCurve));
    }

    private IEnumerator BlendBossIntroLightRoutine(float target, float duration, AnimationCurve curve)
    {
        if (bossIntroLight == null)
        {
            bossIntroLightRoutine = null;
            yield break;
        }

        float start = bossIntroLight.intensity;
        float elapsed = 0f;
        while (elapsed < duration && bossIntroLight != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = curve != null ? curve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);
            bossIntroLight.intensity = Mathf.Lerp(start, target, eased);
            yield return null;
        }
        if (bossIntroLight != null)
            bossIntroLight.intensity = target;
        bossIntroLightRoutine = null;
    }

    private void RestoreBossIntroLight()
    {
        if (bossIntroLightRoutine != null)
        {
            StopCoroutine(bossIntroLightRoutine);
            bossIntroLightRoutine = null;
        }
        if (bossIntroLightSaved && bossIntroLight != null)
            bossIntroLight.intensity = bossIntroOriginalIntensity;
        bossIntroLightSaved = false;
        bossIntroLight = null;
    }

    private void PlayStepSfx(PresentationStep step)
    {
        if (step == null || string.IsNullOrWhiteSpace(step.sfxId))
            return;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
            audioManager.PlaySfx(step.sfxId.Trim(), Mathf.Clamp01(step.sfxVolume));
        else
            Debug.LogWarning("[BossSpawnPresentation] AudioManager.Instance가 없어 스텝 SFX를 재생할 수 없습니다.", this);
    }

    private void SpawnStepVfx(PresentationStep step)
    {
        if (step == null || step.vfxPrefab == null)
            return;

        Transform origin = step.vfxOrigin != null ? step.vfxOrigin : transform;
        Vector3 worldPosition = origin.TransformPoint(step.vfxLocalOffset);
        Quaternion worldRotation = origin.rotation * Quaternion.Euler(step.vfxLocalEulerAngles);
        Transform parent = step.parentVfxToOrigin ? origin : null;
        GameObject instance = Instantiate(step.vfxPrefab, worldPosition, worldRotation, parent);

        if (step.destroyVfxOnPresentationEnd)
            presentationVfx.Add(instance);

        if (step.vfxLifetime > 0f)
            StartCoroutine(DestroyStepVfxAfterDelay(instance, step.vfxLifetime));
    }

    private static IEnumerator DestroyStepVfxAfterDelay(GameObject instance, float duration)
    {
        yield return WaitForDuration(duration);
        if (instance != null)
            Destroy(instance);
    }

    private void CleanupPresentationVfx()
    {
        for (int i = 0; i < presentationVfx.Count; i++)
        {
            if (presentationVfx[i] != null)
                Destroy(presentationVfx[i]);
        }
        presentationVfx.Clear();
    }

    private IEnumerator ExecuteCameraStep(PresentationStep step)
    {
        switch (step.action)
        {
            case SequenceAction.CameraMove:
                if (ownsIntroCamera)
                {
                    StopCameraBlend();
                    Vector3 target = step.targetOffset;
                    yield return BlendCameraRoutine(cameraBasePosition, target,
                        cameraBaseSize, cameraBaseSize, step.duration);
                }
                break;
            case SequenceAction.CameraHold:
                if (ownsIntroCamera)
                    yield return HoldCameraRoutine(step.duration);
                else
                    yield return WaitForDuration(step.duration);
                break;
            case SequenceAction.CameraShake:
                if (ownsIntroCamera)
                    yield return ShakeCameraRoutine(step.duration, step.shakeStrength,
                        step.shakeFrequency, step.fadeOutShake);
                break;
            case SequenceAction.Wait:
                yield return WaitForDuration(step.duration);
                break;
            case SequenceAction.RestoreCamera:
                yield return RestoreIntroCameraRoutine(step.duration);
                break;
        }
    }

    private void BeginIntroBattleEffectSlide()
    {
        if (!useBattleEffectSlide)
            return;

        BattleEffectPlaneSlideController slide = battleEffectSlideController != null
            ? battleEffectSlideController : BattleEffectPlaneSlideController.Instance;
        if (slide == null || !slide.isActiveAndEnabled)
            return;

        activeIntroSlide = slide;
        activeIntroSlide.MoveToBattleCenter();
    }

    private IEnumerator EndIntroBattleEffectSlideRoutine()
    {
        BattleEffectPlaneSlideController slide = activeIntroSlide;
        activeIntroSlide = null;
        if (slide != null && slide.isActiveAndEnabled)
            yield return slide.MoveToReserveAndWaitRoutine();
    }

    private void CancelIntroBattleEffectSlide()
    {
        BattleEffectPlaneSlideController slide = activeIntroSlide;
        activeIntroSlide = null;
        if (slide != null && slide.isActiveAndEnabled)
            slide.MoveToReservePosition();
    }

    public void CancelPresentation()
    {
        CancelIntroBattleEffectSlide();
        CleanupPresentationVfx();
        RestoreBossIntroLight();
        StopDialogueTimeline();
        if (animator != null)
            animator.speed = 1f;

        StopCameraBlend();
        RestoreIntroCameraImmediate();
    }

    private void BeginDialogueTimeline()
    {
        StopDialogueTimeline();
        ApplyDialogueLayout();
        if (timedDialogues == null || timedDialogues.Count == 0 || dialogueRoot == null || dialogueText == null)
            return;

        dialogueTimelineCompleted = false;
        dialogueCoroutine = StartCoroutine(PlayDialogueTimeline());
    }

    private IEnumerator PlayDialogueTimeline()
    {
        // Dialogue starts independently of camera and animator steps.
        if (firstDialogueStartDelay > 0f)
            yield return WaitForDuration(firstDialogueStartDelay);

        // Display dialogues in list order, hiding the bubble between entries.
        // The last entry with duration=0 stays visible until the full boss intro ends.
        for (int i = 0; i < timedDialogues.Count; i++)
        {
            TimedDialogue entry = timedDialogues[i];
            if (entry == null)
                continue;

            dialogueText.text = string.IsNullOrWhiteSpace(entry.localizationKey)
                ? entry.fallbackText
                : GameLocalization.Get(entry.localizationKey, entry.fallbackText);
            dialogueRoot.SetActive(true);
            PlayDialogueSfx(entry);

            float duration = Mathf.Max(0f, entry.displayDuration);
            if (duration > 0f)
                yield return WaitForDuration(duration);

            int nextIndex = i + 1;
            while (nextIndex < timedDialogues.Count && timedDialogues[nextIndex] == null)
                nextIndex++;

            bool hasNextDialogue = nextIndex < timedDialogues.Count;
            if (hasNextDialogue)
            {
                HideDialogue();
                float gap = Mathf.Max(0f, dialogueBetweenDelay);
                if (gap > 0f)
                    yield return WaitForDuration(gap);
            }
            else if (duration > 0f)
            {
                HideDialogue();
            }
        }

        dialogueCoroutine = null;
        dialogueTimelineCompleted = true;
    }

    private void PlayDialogueSfx(TimedDialogue entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.sfxId))
            return;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            Debug.LogWarning("[BossSpawnPresentation] AudioManager.Instance가 없어 다이어로그 SFX를 재생할 수 없습니다.", this);
            return;
        }

        // Use the same Sound DB routing as DBAudioSource, without adding an
        // OnEnable listener that would trigger duplicate sounds.
        audioManager.PlaySfx(entry.sfxId.Trim(), Mathf.Clamp01(entry.sfxVolume));
    }

    private void ApplyDialogueLayout()
    {
        if (dialogueRect == null)
            return;
        dialogueRect.localPosition = dialogueLocalPosition;
    }

    private void HideDialogue()
    {
        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    private void StopDialogueTimeline()
    {
        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = null;
        }
        dialogueTimelineCompleted = true;
        HideDialogue();
    }

    private void PlayAnimatorState(string stateName, float speed = 1f)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return;

        string requestedState = stateName.Trim();
        int stateHash = Animator.StringToHash(requestedState);
        if (!animator.HasState(0, stateHash))
        {
            Debug.LogWarning($"[BossSpawnPresentation] Animator Controller에서 State '{requestedState}'를 찾을 수 없습니다. Animator State 이름을 확인하세요.", this);
            return;
        }

        animator.speed = Mathf.Max(0.01f, speed);
        animator.Play(stateHash, 0, 0f);
        animator.Update(0f);
    }

    private float ResolvePresentationDuration(string stateName, float overrideDuration, float speed)
    {
        if (overrideDuration > 0f)
            return overrideDuration;

        // PlayAnimatorState() has already entered this state. Use the actual
        // clip bound to the state (including AnimatorOverrideController overrides).
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimatorClipInfo[] activeClips = animator.GetCurrentAnimatorClipInfo(0);
            float longestClip = 0f;
            for (int i = 0; i < activeClips.Length; i++)
            {
                AnimationClip activeClip = activeClips[i].clip;
                if (activeClip != null)
                    longestClip = Mathf.Max(longestClip, activeClip.length);
            }

            if (longestClip > 0f)
                return longestClip / Mathf.Max(0.01f, speed);


        }

        Debug.LogWarning($"[BossSpawnPresentation] Animator State '{stateName}'의 클립 길이를 찾지 못했습니다. Animation Duration을 지정하면 수동 대기 시간을 설정할 수 있습니다.", this);
        return 0f;
    }

    private static IEnumerator WaitForDuration(float duration)
    {
        float elapsed = 0f;
        while (elapsed < Mathf.Max(0f, duration))
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void BeginIntroCamera()
    {
        if (BattleCameraController.Instance == null ||
            !BattleCameraController.Instance.TryBeginCinematicCamera())
            return;

        introCamera = Camera.main;
        if (introCamera == null)
        {
            BattleCameraController.Instance.RestoreCinematicCameraImmediate();
            return;
        }

        ownsIntroCamera = true;
        cameraRestorePosition = introCamera.transform.position;
        cameraRestoreRotation = introCamera.transform.rotation;
        cameraRestoreOrthographicSize = introCamera.orthographicSize;
        cameraBasePosition = cameraRestorePosition;
        cameraBaseSize = cameraRestoreOrthographicSize;
    }

    private IEnumerator RestoreIntroCameraRoutine(float duration = 0.35f)
    {
        if (!ownsIntroCamera || introCamera == null)
            yield break;

        StopCameraBlend();
        yield return BlendCameraRoutine(cameraBasePosition, cameraRestorePosition,
            cameraBaseSize, cameraRestoreOrthographicSize, duration);

        BattleCameraController.Instance?.EndCinematicCameraKeepingCurrentPose();
        ownsIntroCamera = false;
        introCamera = null;
    }

    private void StopCameraBlend()
    {
        if (cameraBlendRoutine == null)
            return;
        StopCoroutine(cameraBlendRoutine);
        cameraBlendRoutine = null;
        if (ownsIntroCamera)
            ApplyCameraPose(cameraBasePosition, cameraBaseSize);
    }

    private IEnumerator BlendCameraRoutine(Vector3 startPosition, Vector3 targetPosition,
        float startSize, float targetSize, float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0f, duration);
        do
        {
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            ApplyCameraPose(Vector3.Lerp(startPosition, targetPosition, t),
                Mathf.Lerp(startSize, targetSize, t));
            if (t >= 1f)
                break;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        while (true);
        cameraBlendRoutine = null;
    }

    // Keep the most recently reached cinematic camera pose for this step's duration.
    // The next CameraMove continues from the same stored cameraBasePosition.
    private IEnumerator HoldCameraRoutine(float duration)
    {
        Vector3 heldPosition = cameraBasePosition;
        float heldSize = cameraBaseSize;
        float elapsed = 0f;
        duration = Mathf.Max(0f, duration);

        while (elapsed < duration && ownsIntroCamera)
        {
            ApplyCameraPose(heldPosition, heldSize, false);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (ownsIntroCamera)
            ApplyCameraPose(heldPosition, heldSize);
    }

    private IEnumerator ShakeCameraRoutine(float duration, float strength, float frequency, bool fadeOut)
    {
        Vector3 fixedBase = cameraBasePosition;
        float size = cameraBaseSize;
        float elapsed = 0f;
        duration = Mathf.Max(0f, duration);
        while (elapsed < duration)
        {
            float envelope = fadeOut ? 1f - Mathf.Clamp01(elapsed / duration) : 1f;
            float phase = elapsed * Mathf.Max(0f, frequency);
            // 두 축의 서로 다른 주파수로 반복 패턴을 줄이고, 마지막에는 원래 위치로 복구.
            Vector3 offset = new Vector3(Mathf.Sin(phase * 2.17f),
                Mathf.Sin(phase * 2.91f + 1.3f), 0f) * (strength * envelope);
            ApplyCameraPose(fixedBase + offset, size, false);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        ApplyCameraPose(fixedBase, size);
    }

    private void ApplyCameraPose(Vector3 position, float orthographicSize, bool updateBase = true)
    {
        if (!ownsIntroCamera)
            return;

        if (updateBase)
        {
            cameraBasePosition = position;
            cameraBaseSize = orthographicSize;
        }

        if (BattleCameraController.Instance != null)
        {
            BattleCameraController.Instance.ApplyCinematicCameraPose(
                position, cameraRestoreRotation, orthographicSize,
                true, false, true);
            return;
        }

        if (introCamera == null)
            return;
        introCamera.transform.position = position;
        if (introCamera.orthographic)
            introCamera.orthographicSize = Mathf.Max(0.01f, orthographicSize);
    }

    private void RestoreIntroCameraImmediate()
    {
        if (!ownsIntroCamera)
            return;

        StopCameraBlend();
        if (BattleCameraController.Instance != null)
            BattleCameraController.Instance.RestoreCinematicCameraImmediate();
        else if (introCamera != null)
        {
            introCamera.transform.SetPositionAndRotation(cameraRestorePosition, cameraRestoreRotation);
            if (introCamera.orthographic)
                introCamera.orthographicSize = cameraRestoreOrthographicSize;
        }

        ownsIntroCamera = false;
        introCamera = null;
    }
}
