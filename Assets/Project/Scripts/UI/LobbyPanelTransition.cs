using System;
using System.Collections;
using UnityEngine;

public class LobbyPanelTransition : MonoBehaviour
{
    public enum TransitionDirection
    {
        Horizontal,
        Vertical // 기존 호출 호환용. 실제 연출은 HorizontalTransition만 사용합니다.
    }

    [System.Serializable]
    public class TransitionImageSet
    {
        [Header("Root")]
        public GameObject root;
        public CanvasGroup canvasGroup;

        [Header("Images")]
        public RectTransform firstImage;
        public RectTransform secondImage;

        [Header("First Image Position")]
        public Vector3 firstOpenedLocalPosition;
        public Vector3 firstClosedLocalPosition;

        [Header("Second Image Position")]
        public Vector3 secondOpenedLocalPosition;
        public Vector3 secondClosedLocalPosition;

        public void SetOpenedImmediate()
        {
            if (firstImage != null)
                firstImage.localPosition = firstOpenedLocalPosition;

            if (secondImage != null)
                secondImage.localPosition = secondOpenedLocalPosition;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void SetClosedImmediate()
        {
            if (firstImage != null)
                firstImage.localPosition = firstClosedLocalPosition;

            if (secondImage != null)
                secondImage.localPosition = secondClosedLocalPosition;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void SetRootRotationZ(float zRotation)
        {
            if (root != null)
                root.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        }

        public float GetRootRotationZ()
        {
            if (root == null)
                return 0f;

            return root.transform.localEulerAngles.z;
        }

        public void Show()
        {
            if (root != null)
            {
                root.SetActive(true);
                root.transform.SetAsLastSibling();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (root != null)
                root.SetActive(false);
        }
    }

    [Header("Horizontal Transition")]
    [SerializeField]
    private TransitionImageSet horizontalTransition = new TransitionImageSet
    {
        firstOpenedLocalPosition = new Vector3(-1500f, 0f, 0f),
        firstClosedLocalPosition = new Vector3(-500f, 0f, 0f),
        secondOpenedLocalPosition = new Vector3(1500f, 0f, 0f),
        secondClosedLocalPosition = new Vector3(500f, 0f, 0f)
    };

    [Header("Timing")]
    [SerializeField] private float closeDuration = 0.35f;
    [SerializeField] private float openDuration = 0.35f;
    [SerializeField] private float closedHoldDuration = 0.05f;

    [Header("Curve")]
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Transition Sound")]
    [SerializeField] private bool playTransitionSound = true;
    [SerializeField] private SfxType transitionSfx = SfxType.LobbyPanelTransition;
    [SerializeField] private float transitionSfxVolumeMultiplier = 1f;

    private Coroutine transitionCoroutine;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;
    public float EstimatedTransitionTime
    {
        get
        {
            return Mathf.Max(0f, closeDuration)
                + Mathf.Max(0f, openDuration)
                + Mathf.Max(0f, closedHoldDuration);
        }
    }

    private void Awake()
    {
        SetAllOpenedImmediate();
        HideAllRoots();
    }

    public void PlayPanelChange(
        GameObject[] panelsToClose,
        GameObject panelToOpen,
        TransitionDirection closeDirection,
        TransitionDirection openDirection,
        float startDelay = 0f,
        Action beforePanelChange = null,
        Action afterPanelChange = null)
    {
        PlayPanelChange(
            panelsToClose,
            panelToOpen,
            null,
            null,
            closeDirection,
            openDirection,
            startDelay,
            beforePanelChange,
            afterPanelChange);
    }

    public void PlayPanelChange(
        GameObject[] panelsToClose,
        GameObject panelToOpen,
        GameObject[] worldObjectsToClose,
        GameObject[] worldObjectsToOpen,
        TransitionDirection closeDirection,
        TransitionDirection openDirection,
        float startDelay = 0f,
        Action beforePanelChange = null,
        Action afterPanelChange = null)
    {
        if (isPlaying)
            return;

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(PanelChangeRoutine(
            panelsToClose,
            panelToOpen,
            worldObjectsToClose,
            worldObjectsToOpen,
            closeDirection,
            openDirection,
            startDelay,
            beforePanelChange,
            afterPanelChange));
    }

    private static void UpdateLobbyCameraPauseForOpenedPanel(GameObject panelToOpen)
    {
        bool shouldPause = panelToOpen != null && panelToOpen.name != "PositionPanel";
        CameraMouseParallaxController.SetLobbyContentPanelPause(shouldPause);
    }

    public void SetAllOpenedImmediate()
    {
        horizontalTransition.SetRootRotationZ(0f);
        horizontalTransition.SetOpenedImmediate();
    }

    private IEnumerator PanelChangeRoutine(
        GameObject[] panelsToClose,
        GameObject panelToOpen,
        GameObject[] worldObjectsToClose,
        GameObject[] worldObjectsToOpen,
        TransitionDirection closeDirection,
        TransitionDirection openDirection,
        float startDelay,
        Action beforePanelChange,
        Action afterPanelChange)
    {
        isPlaying = true;

        // CharacterSettingPanel을 포함한 모든 로비 패널 전환은
        // HorizontalTransition 하나만 사용합니다.
        TransitionImageSet activeSet = horizontalTransition;

        HideInactiveSet(activeSet);
        activeSet.Show();
        activeSet.SetRootRotationZ(0f);
        activeSet.SetOpenedImmediate();

        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        PlayTransitionSound();

        yield return AnimatePosition(activeSet, true, closeDuration, closeCurve);
        activeSet.SetClosedImmediate();

        beforePanelChange?.Invoke();
        ApplyWorldObjectChange(worldObjectsToClose, worldObjectsToOpen);
        ApplyPanelChange(panelsToClose, panelToOpen);
        UpdateLobbyCameraPauseForOpenedPanel(panelToOpen);
        afterPanelChange?.Invoke();

        if (closedHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(closedHoldDuration);

        activeSet.SetRootRotationZ(0f);
        activeSet.SetClosedImmediate();

        yield return AnimatePosition(activeSet, false, openDuration, openCurve);
        activeSet.SetOpenedImmediate();
        activeSet.Hide();
        activeSet.SetRootRotationZ(0f);

        isPlaying = false;
        transitionCoroutine = null;
    }

    private IEnumerator AnimatePosition(TransitionImageSet set, bool closing, float duration, AnimationCurve curve)
    {
        if (set == null)
            yield break;

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsedTime = 0f;

        Vector3 firstStart = closing ? set.firstOpenedLocalPosition : set.firstClosedLocalPosition;
        Vector3 firstEnd = closing ? set.firstClosedLocalPosition : set.firstOpenedLocalPosition;
        Vector3 secondStart = closing ? set.secondOpenedLocalPosition : set.secondClosedLocalPosition;
        Vector3 secondEnd = closing ? set.secondClosedLocalPosition : set.secondOpenedLocalPosition;

        if (set.canvasGroup != null)
        {
            set.canvasGroup.alpha = 1f;
            set.canvasGroup.blocksRaycasts = true;
        }

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / safeDuration);
            float t = curve != null ? curve.Evaluate(normalizedTime) : normalizedTime;

            if (set.firstImage != null)
                set.firstImage.localPosition = Vector3.LerpUnclamped(firstStart, firstEnd, t);

            if (set.secondImage != null)
                set.secondImage.localPosition = Vector3.LerpUnclamped(secondStart, secondEnd, t);

            yield return null;
        }

        if (set.firstImage != null)
            set.firstImage.localPosition = firstEnd;

        if (set.secondImage != null)
            set.secondImage.localPosition = secondEnd;
    }

    private void PlayTransitionSound()
    {
        if (!playTransitionSound)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(transitionSfx, transitionSfxVolumeMultiplier);
    }

    private TransitionImageSet GetSet(TransitionDirection direction)
    {
        return horizontalTransition;
    }

    private void HideInactiveSet(TransitionImageSet activeSet)
    {
        // HorizontalTransition만 사용하므로 비활성 전환 세트가 없습니다.
    }

    private void HideAllRoots()
    {
        horizontalTransition.Hide();
    }

    private void ApplyWorldObjectChange(GameObject[] worldObjectsToClose, GameObject[] worldObjectsToOpen)
    {
        if (worldObjectsToClose != null)
        {
            for (int i = 0; i < worldObjectsToClose.Length; i++)
            {
                if (worldObjectsToClose[i] != null)
                    worldObjectsToClose[i].SetActive(false);
            }
        }

        if (worldObjectsToOpen != null)
        {
            for (int i = 0; i < worldObjectsToOpen.Length; i++)
            {
                if (worldObjectsToOpen[i] != null)
                    worldObjectsToOpen[i].SetActive(true);
            }
        }
    }

    private void ApplyPanelChange(GameObject[] panelsToClose, GameObject panelToOpen)
    {
        if (panelsToClose != null)
        {
            for (int i = 0; i < panelsToClose.Length; i++)
            {
                if (panelsToClose[i] != null)
                    panelsToClose[i].SetActive(false);
            }
        }

        if (panelToOpen != null)
            panelToOpen.SetActive(true);
    }
}
