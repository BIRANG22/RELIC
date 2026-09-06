using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleButtonSwitcher : MonoBehaviour, IPointerEnterHandler
{
    [System.Serializable]
    public class MoveTarget
    {
        public Transform target;

        [Header("Move Position")]
        public Vector3 startLocalPosition;
        public Vector3 endLocalPosition;

        [Header("Move Rotation")]
        public Vector3 startLocalEulerAngles;
        public Vector3 endLocalEulerAngles;

        [Header("Move Option")]
        public float moveDuration = 0.5f;
    }

    [System.Serializable]
    public class SwitchObject
    {
        [Header("Main Object")]
        public GameObject targetObject;

        [Header("Back Main")]
        public GameObject backMainObject;

        [Header("Move Targets")]
        public MoveTarget[] moveTargets;

        [Header("Particle Colors")]
        [Tooltip("이 항목이 선택되었을 때 PS_VFX_Sparkles_01에 적용할 색상입니다.")]
        public Color sparklesColor = Color.white;

        [Tooltip("이 항목이 선택되었을 때 PS_VFX_GlowSmoke_01의 Color over Lifetime에 적용할 색상입니다.")]
        public Color glowSmokeColor = Color.white;
    }

    [Header("시작 화면")]
    [Tooltip("활성화되어 있는 동안 이 스크립트의 모든 기능을 막을 StartImage입니다.")]
    [SerializeField] private GameObject startImageObject;

    [Header("Button")]
    [SerializeField] private Button titleButton;

    [Header("Objects")]
    [Min(1)]
    [SerializeField] private int objectCount = 3;

    [SerializeField] private SwitchObject[] objects = new SwitchObject[3];

    [Header("Particle Systems")]
    [Tooltip("PS_VFX_Sparkles_01 파티클 시스템을 연결합니다.")]
    [SerializeField] private ParticleSystem sparklesParticle;

    [Tooltip("PS_VFX_GlowSmoke_01 파티클 시스템을 연결합니다.")]
    [SerializeField] private ParticleSystem glowSmokeParticle;

    [Tooltip("색상 변경 시 기존 파티클을 지우고 새 색상으로 다시 재생합니다.")]
    [SerializeField] private bool restartParticlesOnColorChange = true;

    [Header("Sound")]
    [SerializeField] private bool playHoverSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string hoverSfx = AudioIds.Sfx.MoveButtonHover;

    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.MoveButtonClick;

    private Coroutine moveCoroutine;

    // StartImage가 꺼진 후 초기 설정이 한 번만 실행되도록 사용합니다.
    private bool hasInitialized;

    // StartImage의 이전 활성 상태입니다.
    private bool previousBlockedState;

    private void OnValidate()
    {
        objectCount = Mathf.Max(1, objectCount);
        ResizeObjectsArray();
    }

    private void Awake()
    {
        ResizeObjectsArray();

        if (titleButton != null)
        {
            titleButton.onClick.RemoveListener(OnClickTitleButton);
            titleButton.onClick.AddListener(OnClickTitleButton);
        }

        previousBlockedState = IsBlockedByStartImage();
        UpdateButtonInteractable();
    }

    private void Start()
    {
        // StartImage가 처음부터 꺼져 있다면 바로 초기화합니다.
        if (!IsBlockedByStartImage())
        {
            InitializeSwitcher();
        }
    }

    private void Update()
    {
        bool isBlocked = IsBlockedByStartImage();

        if (isBlocked != previousBlockedState)
        {
            previousBlockedState = isBlocked;
            UpdateButtonInteractable();
        }

        if (isBlocked)
        {
            return;
        }

        // StartImage가 꺼진 직후 최초 한 번만 초기 설정합니다.
        if (!hasInitialized)
        {
            InitializeSwitcher();
        }
    }

    private void OnDisable()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        if (titleButton != null)
        {
            titleButton.onClick.RemoveListener(OnClickTitleButton);
        }
    }

    /// <summary>
    /// StartImage가 활성화되어 있는지 확인합니다.
    /// </summary>
    private bool IsBlockedByStartImage()
    {
        return startImageObject != null &&
               startImageObject.activeInHierarchy;
    }

    /// <summary>
    /// StartImage 상태에 따라 버튼 클릭 가능 여부를 변경합니다.
    /// </summary>
    private void UpdateButtonInteractable()
    {
        if (titleButton == null)
        {
            return;
        }

        titleButton.interactable = !IsBlockedByStartImage();
    }

    /// <summary>
    /// 현재 활성화된 오브젝트를 기준으로 처음 상태를 설정합니다.
    /// </summary>
    private void InitializeSwitcher()
    {
        if (hasInitialized)
        {
            return;
        }

        hasInitialized = true;

        int currentIndex = GetCurrentActiveIndex();

        if (currentIndex < 0)
        {
            int firstValidIndex = GetFirstValidObjectIndex();

            if (firstValidIndex >= 0)
            {
                SetActiveObject(firstValidIndex);
            }
        }
        else
        {
            SetActiveObject(currentIndex);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsBlockedByStartImage())
        {
            return;
        }

        PlayHoverSound();
    }

    private void OnClickTitleButton()
    {
        if (IsBlockedByStartImage())
        {
            return;
        }

        if (!hasInitialized)
        {
            InitializeSwitcher();
        }

        PlayClickSound();

        int currentIndex = GetCurrentActiveIndex();
        int nextIndex = GetRandomNextIndex(currentIndex);

        SetActiveObject(nextIndex);
    }

    private void PlayHoverSound()
    {
        if (IsBlockedByStartImage() || !playHoverSound)
        {
            return;
        }

        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySfx(hoverSfx);
    }

    private void PlayClickSound()
    {
        if (IsBlockedByStartImage() || !playClickSound)
        {
            return;
        }

        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySfx(clickSfx);
    }

    private void ResizeObjectsArray()
    {
        if (objects == null)
        {
            objects = new SwitchObject[objectCount];

            for (int i = 0; i < objects.Length; i++)
            {
                objects[i] = new SwitchObject();
            }

            return;
        }

        if (objects.Length == objectCount)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null)
                {
                    objects[i] = new SwitchObject();
                }
            }

            return;
        }

        SwitchObject[] resizedObjects = new SwitchObject[objectCount];

        int copyCount = Mathf.Min(
            objects.Length,
            resizedObjects.Length
        );

        for (int i = 0; i < copyCount; i++)
        {
            resizedObjects[i] = objects[i];
        }

        for (int i = 0; i < resizedObjects.Length; i++)
        {
            if (resizedObjects[i] == null)
            {
                resizedObjects[i] = new SwitchObject();
            }
        }

        objects = resizedObjects;
    }

    private int GetCurrentActiveIndex()
    {
        if (objects == null)
        {
            return -1;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null ||
                objects[i].targetObject == null)
            {
                continue;
            }

            if (objects[i].targetObject.activeSelf)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetRandomNextIndex(int currentIndex)
    {
        int validCount = GetValidObjectCount();

        if (validCount <= 0)
        {
            return -1;
        }

        if (validCount == 1)
        {
            return GetFirstValidObjectIndex();
        }

        int nextIndex = currentIndex;

        while (nextIndex == currentIndex)
        {
            nextIndex = GetRandomValidObjectIndex();
        }

        return nextIndex;
    }

    private int GetValidObjectCount()
    {
        int validCount = 0;

        if (objects == null)
        {
            return validCount;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null ||
                objects[i].targetObject == null)
            {
                continue;
            }

            validCount++;
        }

        return validCount;
    }

    private int GetFirstValidObjectIndex()
    {
        if (objects == null)
        {
            return -1;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null ||
                objects[i].targetObject == null)
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    private int GetRandomValidObjectIndex()
    {
        int validCount = GetValidObjectCount();

        if (validCount <= 0)
        {
            return -1;
        }

        int randomValidOrder = Random.Range(0, validCount);
        int currentValidOrder = 0;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null ||
                objects[i].targetObject == null)
            {
                continue;
            }

            if (currentValidOrder == randomValidOrder)
            {
                return i;
            }

            currentValidOrder++;
        }

        return -1;
    }

    private void SetActiveObject(int index)
    {
        if (IsBlockedByStartImage())
        {
            return;
        }

        if (index < 0 ||
            objects == null ||
            index >= objects.Length ||
            objects[index] == null)
        {
            return;
        }

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            bool isActive = i == index;

            if (objects[i].targetObject != null)
            {
                objects[i].targetObject.SetActive(isActive);
            }

            if (objects[i].backMainObject != null)
            {
                objects[i].backMainObject.SetActive(isActive);
            }
        }

        // 선택된 항목에 설정된 색상으로 파티클 색상을 변경합니다.
        ApplyParticleColors(objects[index]);

        moveCoroutine = StartCoroutine(
            MoveTargetsCoroutine(objects[index].moveTargets)
        );
    }

    /// <summary>
    /// 선택된 오브젝트에 설정된 색상을 두 파티클에 적용합니다.
    /// </summary>
    private void ApplyParticleColors(SwitchObject switchObject)
    {
        if (switchObject == null)
        {
            return;
        }

        // Sparkles는 기존처럼 Start Color를 변경합니다.
        SetParticleStartColor(
            sparklesParticle,
            switchObject.sparklesColor
        );

        // Glow Smoke는 Color over Lifetime을 사용하므로
        // 해당 그라디언트의 RGB 색상을 변경합니다.
        SetParticleColorOverLifetime(
            glowSmokeParticle,
            switchObject.glowSmokeColor
        );
    }

    /// <summary>
    /// 파티클 시스템의 Start Color를 변경합니다.
    /// </summary>
    private void SetParticleStartColor(
        ParticleSystem particleSystem,
        Color color
    )
    {
        if (particleSystem == null)
        {
            return;
        }

        ParticleSystem.MainModule main = particleSystem.main;
        main.startColor = color;

        RestartParticleIfNeeded(particleSystem);
    }

    /// <summary>
    /// Color over Lifetime의 기존 시간과 알파 변화는 유지하고
    /// RGB 색상만 선택된 색상으로 변경합니다.
    /// </summary>
    private void SetParticleColorOverLifetime(
        ParticleSystem particleSystem,
        Color color
    )
    {
        if (particleSystem == null)
        {
            return;
        }

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
            particleSystem.colorOverLifetime;

        colorOverLifetime.enabled = true;

        ParticleSystem.MinMaxGradient currentGradient =
            colorOverLifetime.color;

        switch (currentGradient.mode)
        {
            case ParticleSystemGradientMode.TwoGradients:
                {
                    Gradient minimumGradient = CreateTintedGradient(
                        currentGradient.gradientMin,
                        color
                    );

                    Gradient maximumGradient = CreateTintedGradient(
                        currentGradient.gradientMax,
                        color
                    );

                    colorOverLifetime.color =
                        new ParticleSystem.MinMaxGradient(
                            minimumGradient,
                            maximumGradient
                        );

                    break;
                }

            case ParticleSystemGradientMode.TwoColors:
                {
                    Color minimumColor = color;
                    minimumColor.a = currentGradient.colorMin.a;

                    Color maximumColor = color;
                    maximumColor.a = currentGradient.colorMax.a;

                    colorOverLifetime.color =
                        new ParticleSystem.MinMaxGradient(
                            minimumColor,
                            maximumColor
                        );

                    break;
                }

            case ParticleSystemGradientMode.Color:
                {
                    Color singleColor = color;
                    singleColor.a = currentGradient.color.a;

                    colorOverLifetime.color =
                        new ParticleSystem.MinMaxGradient(singleColor);

                    break;
                }

            default:
                {
                    Gradient tintedGradient = CreateTintedGradient(
                        currentGradient.gradient,
                        color
                    );

                    colorOverLifetime.color =
                        new ParticleSystem.MinMaxGradient(tintedGradient);

                    break;
                }
        }

        RestartParticleIfNeeded(particleSystem);
    }

    /// <summary>
    /// 기존 그라디언트의 색상 키 시간과 알파 키를 유지하면서
    /// 모든 색상 키의 RGB만 지정된 색상으로 변경합니다.
    /// </summary>
    private Gradient CreateTintedGradient(
        Gradient sourceGradient,
        Color targetColor
    )
    {
        Gradient resultGradient = new Gradient();

        GradientColorKey[] sourceColorKeys =
            sourceGradient != null
                ? sourceGradient.colorKeys
                : null;

        GradientAlphaKey[] sourceAlphaKeys =
            sourceGradient != null
                ? sourceGradient.alphaKeys
                : null;

        GradientColorKey[] newColorKeys;

        if (sourceColorKeys != null && sourceColorKeys.Length > 0)
        {
            newColorKeys =
                new GradientColorKey[sourceColorKeys.Length];

            for (int i = 0; i < sourceColorKeys.Length; i++)
            {
                newColorKeys[i] = new GradientColorKey(
                    new Color(
                        targetColor.r,
                        targetColor.g,
                        targetColor.b,
                        1f
                    ),
                    sourceColorKeys[i].time
                );
            }
        }
        else
        {
            newColorKeys = new GradientColorKey[]
            {
                new GradientColorKey(
                    new Color(
                        targetColor.r,
                        targetColor.g,
                        targetColor.b,
                        1f
                    ),
                    0f
                ),
                new GradientColorKey(
                    new Color(
                        targetColor.r,
                        targetColor.g,
                        targetColor.b,
                        1f
                    ),
                    1f
                )
            };
        }

        GradientAlphaKey[] newAlphaKeys;

        if (sourceAlphaKeys != null && sourceAlphaKeys.Length > 0)
        {
            newAlphaKeys =
                new GradientAlphaKey[sourceAlphaKeys.Length];

            for (int i = 0; i < sourceAlphaKeys.Length; i++)
            {
                newAlphaKeys[i] = sourceAlphaKeys[i];
            }
        }
        else
        {
            newAlphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(targetColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            };
        }

        resultGradient.SetKeys(newColorKeys, newAlphaKeys);

        return resultGradient;
    }

    /// <summary>
    /// 설정에 따라 기존 파티클을 지우고 새 색상으로 다시 재생합니다.
    /// </summary>
    private void RestartParticleIfNeeded(
        ParticleSystem particleSystem
    )
    {
        if (particleSystem == null ||
            !restartParticlesOnColorChange)
        {
            return;
        }

        bool wasPlaying = particleSystem.isPlaying;
        bool playOnAwake = particleSystem.main.playOnAwake;

        particleSystem.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        if (wasPlaying || playOnAwake)
        {
            particleSystem.Play(true);
        }
    }

    private IEnumerator MoveTargetsCoroutine(
        MoveTarget[] moveTargets
    )
    {
        if (moveTargets == null)
        {
            moveCoroutine = null;
            yield break;
        }

        for (int i = 0; i < moveTargets.Length; i++)
        {
            MoveTarget moveTarget = moveTargets[i];

            if (moveTarget == null ||
                moveTarget.target == null)
            {
                continue;
            }

            moveTarget.target.localPosition =
                moveTarget.startLocalPosition;

            moveTarget.target.localRotation =
                Quaternion.Euler(
                    moveTarget.startLocalEulerAngles
                );
        }

        float maxDuration = GetMaxDuration(moveTargets);
        float elapsedTime = 0f;

        while (elapsedTime < maxDuration)
        {
            if (IsBlockedByStartImage())
            {
                moveCoroutine = null;
                yield break;
            }

            elapsedTime += Time.deltaTime;

            for (int i = 0; i < moveTargets.Length; i++)
            {
                MoveTarget moveTarget = moveTargets[i];

                if (moveTarget == null ||
                    moveTarget.target == null)
                {
                    continue;
                }

                float duration =
                    Mathf.Max(0.01f, moveTarget.moveDuration);

                float t =
                    Mathf.Clamp01(elapsedTime / duration);

                moveTarget.target.localPosition =
                    Vector3.Lerp(
                        moveTarget.startLocalPosition,
                        moveTarget.endLocalPosition,
                        t
                    );

                moveTarget.target.localRotation =
                    Quaternion.Lerp(
                        Quaternion.Euler(
                            moveTarget.startLocalEulerAngles
                        ),
                        Quaternion.Euler(
                            moveTarget.endLocalEulerAngles
                        ),
                        t
                    );
            }

            yield return null;
        }

        for (int i = 0; i < moveTargets.Length; i++)
        {
            MoveTarget moveTarget = moveTargets[i];

            if (moveTarget == null ||
                moveTarget.target == null)
            {
                continue;
            }

            moveTarget.target.localPosition =
                moveTarget.endLocalPosition;

            moveTarget.target.localRotation =
                Quaternion.Euler(
                    moveTarget.endLocalEulerAngles
                );
        }

        moveCoroutine = null;
    }

    private float GetMaxDuration(
        MoveTarget[] moveTargets
    )
    {
        float maxDuration = 0f;

        if (moveTargets == null)
        {
            return Mathf.Max(0.01f, maxDuration);
        }

        for (int i = 0; i < moveTargets.Length; i++)
        {
            if (moveTargets[i] == null)
            {
                continue;
            }

            if (moveTargets[i].moveDuration > maxDuration)
            {
                maxDuration =
                    moveTargets[i].moveDuration;
            }
        }

        return Mathf.Max(0.01f, maxDuration);
    }
}