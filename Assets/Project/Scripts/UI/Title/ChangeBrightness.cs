using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ChangeBrightness : MonoBehaviour
{
    private const int MinStep = -5;
    private const int MaxStep = 5;

    [Header("11단계 밝기 선택")]
    [SerializeField] private Transform imageRoot;
    [SerializeField] private Transform tickRoot;

    [Header("드래그")]
    [SerializeField] private RectTransform dragArea;

    [Header("단계 페이드")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.15f;

    private readonly List<StepImage> stepImages = new();
    private int currentStep;

    private sealed class StepImage
    {
        public int Step;
        public Image Image;
        public float OriginalAlpha;
        public Coroutine FadeCoroutine;
    }

    private void Start()
    {
        ResolveRoots();
        BuildStepImages();
        RegisterDragInput();

        currentStep = BrightnessToStep(GetCurrentBrightness());
        ApplyStep(currentStep, save: true, animate: false);
    }

    private void OnDisable()
    {
        for (int i = 0; i < stepImages.Count; i++)
        {
            StepImage stepImage = stepImages[i];
            if (stepImage.FadeCoroutine != null)
            {
                StopCoroutine(stepImage.FadeCoroutine);
                stepImage.FadeCoroutine = null;
            }
        }
    }

    private void ResolveRoots()
    {
        if (imageRoot == null)
            imageRoot = transform.Find("GameObject");

        if (tickRoot == null)
            tickRoot = transform.Find("Tick");

        if (dragArea == null)
            dragArea = imageRoot as RectTransform;

        if (dragArea == null)
            dragArea = transform as RectTransform;
    }

    private void BuildStepImages()
    {
        stepImages.Clear();

        if (imageRoot == null)
        {
            Debug.LogWarning("[ChangeBrightness] GameObject 루트를 찾을 수 없습니다.", this);
            return;
        }

        for (int step = MinStep; step <= MaxStep; step++)
        {
            string childName = GetStepObjectName(step);
            Transform child = imageRoot.Find(childName);
            if (child == null)
            {
                Debug.LogWarning($"[ChangeBrightness] {childName} 오브젝트를 찾을 수 없습니다.", this);
                continue;
            }

            Image image = child.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogWarning($"[ChangeBrightness] {childName}에 Image 컴포넌트가 없습니다.", child);
                continue;
            }

            stepImages.Add(new StepImage
            {
                Step = step,
                Image = image,
                OriginalAlpha = image.color.a
            });

            RegisterClickTarget(child, step);

            if (tickRoot != null)
            {
                Transform tick = tickRoot.Find(GetTickObjectName(step));
                if (tick != null)
                    RegisterClickTarget(tick, step);
            }
        }
    }

    private void RegisterClickTarget(Transform target, int step)
    {
        if (target == null)
            return;

        Graphic graphic = target.GetComponent<Graphic>();
        if (graphic != null)
            graphic.raycastTarget = true;

        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.gameObject.AddComponent<EventTrigger>();

        trigger.triggers ??= new List<EventTrigger.Entry>();

        EventTrigger.Entry clickEntry = new()
        {
            eventID = EventTriggerType.PointerClick
        };
        clickEntry.callback.AddListener(data =>
        {
            if (data is PointerEventData pointerData &&
                pointerData.button == PointerEventData.InputButton.Left)
            {
                ApplyStep(step, save: true, animate: true);
            }
        });
        trigger.triggers.Add(clickEntry);

        RegisterPointerDragEntries(trigger);
    }

    private void RegisterPointerDragEntries(EventTrigger trigger)
    {
        if (trigger == null)
            return;

        EventTrigger.Entry pointerDownEntry = new()
        {
            eventID = EventTriggerType.PointerDown
        };
        pointerDownEntry.callback.AddListener(data =>
        {
            if (data is PointerEventData pointerData &&
                pointerData.button == PointerEventData.InputButton.Left)
            {
                ApplyPointerPosition(pointerData, save: false);
            }
        });
        trigger.triggers.Add(pointerDownEntry);

        EventTrigger.Entry dragEntry = new()
        {
            eventID = EventTriggerType.Drag
        };
        dragEntry.callback.AddListener(data =>
        {
            if (data is PointerEventData pointerData &&
                pointerData.button == PointerEventData.InputButton.Left)
            {
                ApplyPointerPosition(pointerData, save: false);
            }
        });
        trigger.triggers.Add(dragEntry);

        EventTrigger.Entry pointerUpEntry = new()
        {
            eventID = EventTriggerType.PointerUp
        };
        pointerUpEntry.callback.AddListener(data =>
        {
            if (data is PointerEventData pointerData &&
                pointerData.button == PointerEventData.InputButton.Left)
            {
                ApplyPointerPosition(pointerData, save: true);
            }
        });
        trigger.triggers.Add(pointerUpEntry);
    }

    private void RegisterDragInput()
    {
        EventTrigger trigger = GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = gameObject.AddComponent<EventTrigger>();

        trigger.triggers ??= new List<EventTrigger.Entry>();
        RegisterPointerDragEntries(trigger);
    }

    private void ApplyPointerPosition(PointerEventData pointerData, bool save)
    {
        if (dragArea == null || pointerData == null)
            return;

        Camera eventCamera = pointerData.pressEventCamera != null
            ? pointerData.pressEventCamera
            : pointerData.enterEventCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragArea,
                pointerData.position,
                eventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        Rect rect = dragArea.rect;
        if (rect.width <= 0f)
            return;

        float normalized = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        int step = Mathf.RoundToInt(Mathf.Lerp(MinStep, MaxStep, normalized));
        ApplyStep(step, save, animate: true);
    }

    private void ApplyStep(int step, bool save, bool animate)
    {
        currentStep = Mathf.Clamp(step, MinStep, MaxStep);
        float brightness = StepToBrightness(currentStep);

        if (Settings.Instance != null)
        {
            Settings.Instance.Brightness = brightness;
            if (save)
                Settings.Instance.Save();
        }

        GameBrightnessManager.ApplyBrightness(brightness);
        RefreshSelectionVisual(animate);
    }

    private void RefreshSelectionVisual(bool animate)
    {
        for (int i = 0; i < stepImages.Count; i++)
        {
            StepImage stepImage = stepImages[i];
            if (stepImage.Image == null)
                continue;

            bool shouldBeVisible = stepImage.Step <= currentStep;

            if (!animate || fadeDuration <= 0f || !isActiveAndEnabled)
            {
                SetStepImageImmediate(stepImage, shouldBeVisible);
                continue;
            }

            if (stepImage.FadeCoroutine != null)
                StopCoroutine(stepImage.FadeCoroutine);

            stepImage.FadeCoroutine = StartCoroutine(FadeStepImage(stepImage, shouldBeVisible));
        }
    }

    private void SetStepImageImmediate(StepImage stepImage, bool visible)
    {
        if (stepImage.Image == null)
            return;

        Color color = stepImage.Image.color;
        color.a = stepImage.OriginalAlpha;
        stepImage.Image.color = color;
        stepImage.Image.enabled = visible;
        stepImage.FadeCoroutine = null;
    }

    private IEnumerator FadeStepImage(StepImage stepImage, bool visible)
    {
        Image image = stepImage.Image;
        if (image == null)
            yield break;

        bool wasEnabled = image.enabled;
        Color color = image.color;
        float startAlpha = wasEnabled ? color.a : 0f;
        float targetAlpha = visible ? stepImage.OriginalAlpha : 0f;

        if (visible)
            image.enabled = true;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            color = image.color;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            image.color = color;
            yield return null;
        }

        color = image.color;
        color.a = targetAlpha;
        image.color = color;

        if (!visible)
        {
            image.enabled = false;
            color.a = stepImage.OriginalAlpha;
            image.color = color;
        }

        stepImage.FadeCoroutine = null;
    }

    private static string GetStepObjectName(int step)
    {
        if (step < 0)
            return $"Image{step}";

        return step == 0 ? "Image0" : $"Image{step}";
    }

    private static string GetTickObjectName(int step)
    {
        int index = step - MinStep + 1;
        return $"Tick{index:00}";
    }

    private static int BrightnessToStep(float brightness)
    {
        float normalized = Mathf.Clamp01(brightness);
        return Mathf.Clamp(Mathf.RoundToInt(normalized * (MaxStep - MinStep) + MinStep), MinStep, MaxStep);
    }

    private static float StepToBrightness(int step)
    {
        int clampedStep = Mathf.Clamp(step, MinStep, MaxStep);
        return Mathf.InverseLerp(MinStep, MaxStep, clampedStep);
    }

    private static float GetCurrentBrightness()
    {
        return Settings.Instance != null
            ? Mathf.Clamp01(Settings.Instance.Brightness)
            : 0.5f;
    }
}
