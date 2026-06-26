using Relic.Gameplay.Data;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleTimelineController : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private BattleTimelineBarUI timelineBarUI;
    [SerializeField] private ReserveTurnSlotUI[] reserveSlots;

    [Header("Reservation Preview")]
    [SerializeField] private PlayerSkillReservationController playerSkillReservationController;

    [Header("MoveGhostPreview")]
    [SerializeField] private MoveGhostPreview moveGhostPreview;

    [Header("Grid")]
    [SerializeField] private GridManager gridManager;

    [Header("Selected Slot Text")]
    [SerializeField] private TMP_Text selectedSlotValueText;
    [SerializeField] private string emptySelectedSlotText = "-";
    [SerializeField] private bool autoFindSelectedSlotValueText = true;

    [Header("Selected Slot Effect")]
    [SerializeField] private Transform selectedSlotEffect;
    [SerializeField] private bool autoFindSelectedSlotEffect = true;
    [SerializeField] private string selectedSlotEffectObjectName = "Effect";
    [SerializeField] private float selectedSlotEffectRotateStepZ = -60f;
    [SerializeField] private float selectedSlotEffectDuration = 0.08f;
    [SerializeField] private bool useUnscaledTimeForSelectedSlotEffect = true;

    [Header("Selected Slot Effect SFX")]
    [SerializeField] private bool playSelectedSlotEffectSfx = true;
    [SerializeField] private SfxType selectedSlotEffectSfxType = SfxType.BattleTimelineSlotRotate;
    [SerializeField, Range(0f, 1f)] private float selectedSlotEffectSfxVolume = 1f;

    [Header("Slot Selection Lock")]
    [SerializeField] private bool showWarningWhenSlotSelectionLocked = false;
    [SerializeField] private string slotSelectionLockedMessage = "턴 진행 중에는 슬롯을 선택할 수 없습니다.";

    [Header("Keyboard Input")]
    [SerializeField] private bool enableNumberKeySlotSelection = true;
    [SerializeField] private BattleTurnExecutor turnExecutor;

    [Header("Timeline Slot Slide")]
    [SerializeField] private bool playTimelineSlotSlide = true;
    [SerializeField] private RectTransform[] timelineSlotSlideTargets;
    [SerializeField] private bool autoBindTimelineSlotSlideTargets = true;
    [SerializeField] private string timelineSlotSlideTargetNamePrefix = "TimelineSlot";
    [SerializeField] private float completedTimelineSlotSlideAmountX = -260f;
    [SerializeField] private float waitingTimelineSlotSlideAmountX = -250f;
    [SerializeField] private float timelineSlotSlideDuration = 0.18f;
    [SerializeField] private bool useUnscaledTimeForTimelineSlotSlide = false;

    [Header("End Button Hover Rotation")]
    [SerializeField] private bool playEndButtonHoverRotation = true;
    [SerializeField] private bool autoBindEndButtonHoverRotationTarget = true;
    [SerializeField] private RectTransform endButtonHoverRotationTarget;
    [SerializeField] private string endButtonHoverRotationTargetName = "EndButton";
    [SerializeField] private float endButtonHoverRotationOffsetZ = -45f;
    [SerializeField] private float endButtonHoverRotationDuration = 0.12f;
    [SerializeField] private bool useUnscaledTimeForEndButtonHoverRotation = true;

    private int activeSlotIndex = -1;
    private CharacterRuntimeData selectedCharacter;
    private SkillMasterData selectedSkill;
    private int reservationVersion;
    private Coroutine selectedSlotEffectRoutine;
    private Coroutine timelineSlotSlideRoutine;
    private Coroutine endButtonHoverRotationRoutine;
    private bool isSlotSelectionLocked;
    private bool isEndButtonHovering;
    private float endButtonRotationBeforeHoverZ;
    private Vector2[] timelineSlotOriginalAnchoredPositions;
    private int timelineSlotSlideStepIndex;

    private readonly List<MonsterReservedCommand>[] monsterCommandsBySlot =
        new List<MonsterReservedCommand>[5];

    public int SlotCount => reserveSlots != null ? reserveSlots.Length : 0;
    public int ActiveSlotIndex => activeSlotIndex;
    public int ReservationVersion => reservationVersion;

    private void Awake()
    {
        InitializeMonsterCommandSlots();
        AutoFindSelectedSlotValueTextIfNeeded();
        AutoFindSelectedSlotEffectIfNeeded();
        AutoBindTimelineSlotSlideTargetsIfNeeded();
        CaptureTimelineSlotOriginalPositionsIfNeeded();
        AutoBindEndButtonHoverRotationTargetIfNeeded();
        BindEndButtonHoverRotationEventsIfNeeded();

        if (turnExecutor == null)
            turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);

        RefreshSelectedSlotValueText();

        if (timelineBarUI != null)
            timelineBarUI.Init(this);

        if (reserveSlots != null)
        {
            for (int i = 0; i < reserveSlots.Length; i++)
            {
                if (reserveSlots[i] != null)
                    reserveSlots[i].Init(this, i);
            }
        }

        RefreshTimeline();
        RefreshPlayerHUDs();
    }

    private void Update()
    {
        HandleNumberKeySlotSelectionInput();
    }

    private void HandleNumberKeySlotSelectionInput()
    {
        if (!enableNumberKeySlotSelection)
            return;

        if (!isActiveAndEnabled)
            return;

        if (IsTypingInputFieldSelected())
            return;

        int slotIndex = GetPressedNumberSlotIndex();

        if (slotIndex < 0)
            return;

        if (reserveSlots == null || slotIndex >= reserveSlots.Length || reserveSlots[slotIndex] == null)
            return;

        if (isSlotSelectionLocked)
            return;

        if (turnExecutor == null)
            turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);

        if (turnExecutor != null && !turnExecutor.CanAcceptPlayerInput)
            return;

        OnTimelineSlotClicked(slotIndex);
    }

    private int GetPressedNumberSlotIndex()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            return 0;

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            return 1;

        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            return 2;

        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            return 3;

        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            return 4;

        return -1;
    }

    private bool IsTypingInputFieldSelected()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

        if (selectedObject == null)
            return false;

        if (selectedObject.GetComponent<TMPro.TMP_InputField>() != null)
            return true;

        if (selectedObject.GetComponent<InputField>() != null)
            return true;

        return false;
    }

    public void SelectCharacter(CharacterRuntimeData runtimeData)
    {
        selectedCharacter = runtimeData;
    }

    public void SelectSkill(SkillMasterData skillData)
    {
        selectedSkill = skillData;
        TryStartSkillReservation();
    }

    public void ClearMonsterCommands()
    {
        if (monsterCommandsBySlot == null)
            return;

        for (int i = 0; i < monsterCommandsBySlot.Length; i++)
        {
            if (monsterCommandsBySlot[i] != null)
                monsterCommandsBySlot[i].Clear();
        }

        RefreshTimeline();
    }

    public void OnTimelineSlotClicked(int slotIndex)
    {
        if (isSlotSelectionLocked)
        {
            if (showWarningWhenSlotSelectionLocked)
                ShowBattleWarning(slotSelectionLockedMessage);

            return;
        }

        int previousSlotIndex = activeSlotIndex;
        activeSlotIndex = slotIndex;

        if (timelineBarUI != null)
            timelineBarUI.SetActiveTimelineSlot(activeSlotIndex);

        RefreshSelectedSlotValueText();
        PlaySelectedSlotEffect(previousSlotIndex, activeSlotIndex);
        TryStartSkillReservation();
    }

    public void ClearSelectedSlotSelection()
    {
        activeSlotIndex = -1;
        selectedSkill = null;

        if (timelineBarUI != null)
            timelineBarUI.SetActiveTimelineSlot(activeSlotIndex);

        RefreshSelectedSlotValueText();
    }

    public void SetSlotSelectionLocked(bool locked)
    {
        isSlotSelectionLocked = locked;
    }

    public IEnumerator SlideTimelineSlotsLeftOneStepRoutine()
    {
        yield return SlideTimelineSlotsLeftOneStepRoutine(timelineSlotSlideStepIndex);
    }

    public IEnumerator SlideTimelineSlotsLeftOneStepRoutine(int completedSlotIndex)
    {
        yield return SlideTimelineSlotsLeftThroughSlotRoutine(completedSlotIndex);
    }

    public IEnumerator SlideTimelineSlotsLeftThroughSlotRoutine(int lastSlotIndexInclusive)
    {
        if (!playTimelineSlotSlide)
            yield break;

        AutoBindTimelineSlotSlideTargetsIfNeeded();
        CaptureTimelineSlotOriginalPositionsIfNeeded();

        if (!HasTimelineSlotSlideTargets())
            yield break;

        int startSlotIndex = Mathf.Clamp(timelineSlotSlideStepIndex, 0, timelineSlotSlideTargets.Length);
        int endSlotIndex = Mathf.Clamp(lastSlotIndexInclusive, -1, timelineSlotSlideTargets.Length - 1);

        if (endSlotIndex < startSlotIndex)
            yield break;

        yield return MoveTimelineSlotSlideTargetsThroughCompletedSlotsRoutine(startSlotIndex, endSlotIndex);
        timelineSlotSlideStepIndex = Mathf.Clamp(endSlotIndex + 1, 0, timelineSlotSlideTargets.Length);
    }

    public IEnumerator ResetTimelineSlotsToOriginalPositionRoutine()
    {
        AutoBindTimelineSlotSlideTargetsIfNeeded();
        CaptureTimelineSlotOriginalPositionsIfNeeded();

        if (!HasTimelineSlotSlideTargets())
            yield break;

        yield return MoveTimelineSlotSlideTargetsToOriginalOneByOneRoutine();
        timelineSlotSlideStepIndex = 0;
    }

    private void AutoBindEndButtonHoverRotationTargetIfNeeded()
    {
        if (!autoBindEndButtonHoverRotationTarget)
            return;

        if (endButtonHoverRotationTarget != null)
            return;

        endButtonHoverRotationTarget = FindRectTransformByName(transform, endButtonHoverRotationTargetName);

        if (endButtonHoverRotationTarget != null)
            return;

        Transform searchRoot = GetTimelineSearchRoot();
        endButtonHoverRotationTarget = FindRectTransformByName(searchRoot, endButtonHoverRotationTargetName);

        if (endButtonHoverRotationTarget != null)
            return;

        BattleTimelineBarUI foundTimelineBar = FindFirstObjectByType<BattleTimelineBarUI>(FindObjectsInactive.Include);

        if (foundTimelineBar != null)
            endButtonHoverRotationTarget = FindRectTransformByName(foundTimelineBar.transform, endButtonHoverRotationTargetName);
    }

    private RectTransform FindRectTransformByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName))
            return null;

        Transform found = FindChildRecursive(root, targetName);

        if (found == null)
            return null;

        return found.GetComponent<RectTransform>();
    }

    private void BindEndButtonHoverRotationEventsIfNeeded()
    {
        if (!playEndButtonHoverRotation)
            return;

        AutoBindEndButtonHoverRotationTargetIfNeeded();

        if (endButtonHoverRotationTarget == null)
            return;

        EndButtonHoverRotationRelay relay = endButtonHoverRotationTarget.GetComponent<EndButtonHoverRotationRelay>();

        if (relay == null)
            relay = endButtonHoverRotationTarget.gameObject.AddComponent<EndButtonHoverRotationRelay>();

        relay.Initialize(this);
    }

    private void OnEndButtonHoverEnter()
    {
        if (!playEndButtonHoverRotation)
            return;

        AutoBindEndButtonHoverRotationTargetIfNeeded();

        if (endButtonHoverRotationTarget == null)
            return;

        if (isEndButtonHovering)
            return;

        isEndButtonHovering = true;
        endButtonRotationBeforeHoverZ = GetTransformRotationZ(endButtonHoverRotationTarget);

        float targetRotationZ = endButtonRotationBeforeHoverZ + endButtonHoverRotationOffsetZ;
        PlayEndButtonHoverRotationTo(targetRotationZ);
    }

    private void OnEndButtonHoverExit()
    {
        if (!playEndButtonHoverRotation)
            return;

        AutoBindEndButtonHoverRotationTargetIfNeeded();

        if (endButtonHoverRotationTarget == null)
            return;

        if (!isEndButtonHovering)
            return;

        isEndButtonHovering = false;
        PlayEndButtonHoverRotationTo(endButtonRotationBeforeHoverZ);
    }

    private void PlayEndButtonHoverRotationTo(float targetRotationZ)
    {
        if (endButtonHoverRotationTarget == null)
            return;

        if (endButtonHoverRotationRoutine != null)
            StopCoroutine(endButtonHoverRotationRoutine);

        endButtonHoverRotationRoutine = StartCoroutine(
            RotateEndButtonHoverToRoutine(targetRotationZ)
        );
    }

    private IEnumerator RotateEndButtonHoverToRoutine(float targetRotationZ)
    {
        float duration = Mathf.Max(0.01f, endButtonHoverRotationDuration);
        float elapsed = 0f;
        float startRotationZ = GetTransformRotationZ(endButtonHoverRotationTarget);

        while (elapsed < duration)
        {
            elapsed += useUnscaledTimeForEndButtonHoverRotation ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            float z = Mathf.Lerp(startRotationZ, targetRotationZ, easedT);

            SetTransformRotationZ(endButtonHoverRotationTarget, z);
            yield return null;
        }

        SetTransformRotationZ(endButtonHoverRotationTarget, targetRotationZ);
        endButtonHoverRotationRoutine = null;
    }

    private float GetTransformRotationZ(Transform target)
    {
        if (target == null)
            return 0f;

        return NormalizeAngle(target.localEulerAngles.z);
    }

    private void SetTransformRotationZ(Transform target, float zRotation)
    {
        if (target == null)
            return;

        Vector3 eulerAngles = target.localEulerAngles;
        eulerAngles.z = zRotation;
        target.localEulerAngles = eulerAngles;
    }

    private sealed class EndButtonHoverRotationRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private BattleTimelineController owner;

        public void Initialize(BattleTimelineController controller)
        {
            owner = controller;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.OnEndButtonHoverEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.OnEndButtonHoverExit();
        }
    }

    private void AutoFindSelectedSlotValueTextIfNeeded()
    {
        if (!autoFindSelectedSlotValueText)
            return;

        if (selectedSlotValueText != null)
            return;

        Transform searchRoot = GetTimelineSearchRoot();
        Transform found = FindChildRecursive(searchRoot, "Value_text");

        if (found == null)
        {
            BattleTimelineBarUI foundTimelineBar = FindFirstObjectByType<BattleTimelineBarUI>(FindObjectsInactive.Include);

            if (foundTimelineBar != null)
                found = FindChildRecursive(foundTimelineBar.transform, "Value_text");
        }

        if (found == null)
            return;

        selectedSlotValueText = found.GetComponent<TMP_Text>();
    }

    private void AutoFindSelectedSlotEffectIfNeeded()
    {
        if (!autoFindSelectedSlotEffect)
            return;

        if (selectedSlotEffect != null)
            return;

        Transform searchRoot = GetTimelineSearchRoot();
        Transform found = FindChildRecursive(searchRoot, selectedSlotEffectObjectName);

        if (found == null)
        {
            BattleTimelineBarUI foundTimelineBar = FindFirstObjectByType<BattleTimelineBarUI>(FindObjectsInactive.Include);

            if (foundTimelineBar != null)
                found = FindChildRecursive(foundTimelineBar.transform, selectedSlotEffectObjectName);
        }

        selectedSlotEffect = found;
    }

    private void AutoBindTimelineSlotSlideTargetsIfNeeded()
    {
        if (!autoBindTimelineSlotSlideTargets)
            return;

        if (timelineSlotSlideTargets != null && timelineSlotSlideTargets.Length > 0)
            return;

        List<RectTransform> foundTargets = new();

        if (reserveSlots != null)
        {
            for (int i = 0; i < reserveSlots.Length; i++)
            {
                if (reserveSlots[i] == null)
                    continue;

                RectTransform slotRect = reserveSlots[i].GetComponent<RectTransform>();

                if (slotRect != null && !foundTargets.Contains(slotRect))
                    foundTargets.Add(slotRect);
            }
        }

        if (foundTargets.Count <= 0)
        {
            Transform searchRoot = GetTimelineSearchRoot();
            AddTimelineSlotSlideTargetsRecursive(searchRoot, foundTargets);
        }

        if (foundTargets.Count <= 0)
        {
            BattleTimelineBarUI foundTimelineBar = FindFirstObjectByType<BattleTimelineBarUI>(FindObjectsInactive.Include);

            if (foundTimelineBar != null)
                AddTimelineSlotSlideTargetsRecursive(foundTimelineBar.transform, foundTargets);
        }

        foundTargets.Sort(CompareTimelineSlotSlideTargetOrder);
        timelineSlotSlideTargets = foundTargets.ToArray();
    }

    private void AddTimelineSlotSlideTargetsRecursive(Transform root, List<RectTransform> results)
    {
        if (root == null || results == null)
            return;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name.StartsWith(timelineSlotSlideTargetNamePrefix))
            {
                RectTransform rectTransform = child.GetComponent<RectTransform>();

                if (rectTransform != null && !results.Contains(rectTransform))
                    results.Add(rectTransform);
            }

            AddTimelineSlotSlideTargetsRecursive(child, results);
        }
    }

    private int CompareTimelineSlotSlideTargetOrder(RectTransform a, RectTransform b)
    {
        int aIndex = ExtractTrailingNumber(a != null ? a.name : string.Empty);
        int bIndex = ExtractTrailingNumber(b != null ? b.name : string.Empty);

        if (aIndex != bIndex)
            return aIndex.CompareTo(bIndex);

        string aName = a != null ? a.name : string.Empty;
        string bName = b != null ? b.name : string.Empty;

        return string.CompareOrdinal(aName, bName);
    }

    private int ExtractTrailingNumber(string value)
    {
        if (string.IsNullOrEmpty(value))
            return int.MaxValue;

        int multiplier = 1;
        int result = 0;
        bool hasNumber = false;

        for (int i = value.Length - 1; i >= 0; i--)
        {
            char c = value[i];

            if (c < '0' || c > '9')
                break;

            hasNumber = true;
            result += (c - '0') * multiplier;
            multiplier *= 10;
        }

        return hasNumber ? result : int.MaxValue;
    }

    private void CaptureTimelineSlotOriginalPositionsIfNeeded()
    {
        AutoBindTimelineSlotSlideTargetsIfNeeded();

        if (timelineSlotSlideTargets == null || timelineSlotSlideTargets.Length <= 0)
            return;

        if (timelineSlotOriginalAnchoredPositions != null &&
            timelineSlotOriginalAnchoredPositions.Length == timelineSlotSlideTargets.Length)
        {
            return;
        }

        timelineSlotOriginalAnchoredPositions = new Vector2[timelineSlotSlideTargets.Length];

        for (int i = 0; i < timelineSlotSlideTargets.Length; i++)
        {
            if (timelineSlotSlideTargets[i] != null)
                timelineSlotOriginalAnchoredPositions[i] = timelineSlotSlideTargets[i].anchoredPosition;
        }
    }

    private bool HasTimelineSlotSlideTargets()
    {
        if (timelineSlotSlideTargets == null || timelineSlotSlideTargets.Length <= 0)
            return false;

        for (int i = 0; i < timelineSlotSlideTargets.Length; i++)
        {
            if (timelineSlotSlideTargets[i] != null)
                return true;
        }

        return false;
    }

    private IEnumerator MoveTimelineSlotSlideTargetsThroughCompletedSlotsRoutine(int startSlotIndex, int endSlotIndex)
    {
        AutoBindTimelineSlotSlideTargetsIfNeeded();

        if (!HasTimelineSlotSlideTargets())
            yield break;

        Vector2[] startPositions = GetTimelineSlotCurrentPositions();
        Vector2[] targetPositions = new Vector2[startPositions.Length];

        for (int i = 0; i < startPositions.Length; i++)
            targetPositions[i] = startPositions[i];

        int safeStartSlotIndex = Mathf.Clamp(startSlotIndex, 0, targetPositions.Length);
        int safeEndSlotIndex = Mathf.Clamp(endSlotIndex, -1, targetPositions.Length - 1);

        for (int completedSlotIndex = safeStartSlotIndex; completedSlotIndex <= safeEndSlotIndex; completedSlotIndex++)
        {
            for (int i = completedSlotIndex; i < targetPositions.Length; i++)
            {
                float deltaX = i == completedSlotIndex
                    ? completedTimelineSlotSlideAmountX
                    : waitingTimelineSlotSlideAmountX;

                targetPositions[i] += new Vector2(deltaX, 0f);
            }
        }

        yield return MoveTimelineSlotSlideTargetsToPositionsRoutine(startPositions, targetPositions);
    }

    private IEnumerator MoveTimelineSlotSlideTargetsToOriginalOneByOneRoutine()
    {
        AutoBindTimelineSlotSlideTargetsIfNeeded();
        CaptureTimelineSlotOriginalPositionsIfNeeded();

        if (!HasTimelineSlotSlideTargets())
            yield break;

        if (timelineSlotOriginalAnchoredPositions == null ||
            timelineSlotOriginalAnchoredPositions.Length != timelineSlotSlideTargets.Length)
        {
            yield break;
        }

        for (int i = timelineSlotSlideTargets.Length - 1; i >= 0; i--)
        {
            if (timelineSlotSlideTargets[i] == null)
                continue;

            yield return MoveSingleTimelineSlotToOriginalRoutine(i);
        }
    }

    private IEnumerator MoveSingleTimelineSlotToOriginalRoutine(int slotIndex)
    {
        Vector2[] startPositions = GetTimelineSlotCurrentPositions();
        Vector2[] targetPositions = GetTimelineSlotCurrentPositions();

        if (slotIndex < 0 ||
            slotIndex >= targetPositions.Length ||
            slotIndex >= timelineSlotOriginalAnchoredPositions.Length)
        {
            yield break;
        }

        targetPositions[slotIndex] = timelineSlotOriginalAnchoredPositions[slotIndex];
        yield return MoveTimelineSlotSlideTargetsToPositionsRoutine(startPositions, targetPositions);
    }

    private Vector2[] GetTimelineSlotCurrentPositions()
    {
        AutoBindTimelineSlotSlideTargetsIfNeeded();

        if (timelineSlotSlideTargets == null)
            return new Vector2[0];

        Vector2[] positions = new Vector2[timelineSlotSlideTargets.Length];

        for (int i = 0; i < timelineSlotSlideTargets.Length; i++)
        {
            if (timelineSlotSlideTargets[i] != null)
                positions[i] = timelineSlotSlideTargets[i].anchoredPosition;
        }

        return positions;
    }

    private IEnumerator MoveTimelineSlotSlideTargetsToPositionsRoutine(Vector2[] startPositions, Vector2[] targetPositions)
    {
        if (startPositions == null || targetPositions == null)
            yield break;

        if (startPositions.Length != targetPositions.Length)
            yield break;

        if (timelineSlotSlideRoutine != null)
            StopCoroutine(timelineSlotSlideRoutine);

        timelineSlotSlideRoutine = StartCoroutine(MoveTimelineSlotSlideTargetsCoroutine(startPositions, targetPositions));
        yield return timelineSlotSlideRoutine;
    }

    private IEnumerator MoveTimelineSlotSlideTargetsCoroutine(Vector2[] startPositions, Vector2[] targetPositions)
    {
        float duration = Mathf.Max(0.01f, timelineSlotSlideDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTimeForTimelineSlotSlide ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            ApplyTimelineSlotSlidePositions(startPositions, targetPositions, easedT);
            yield return null;
        }

        ApplyTimelineSlotSlidePositions(startPositions, targetPositions, 1f);
        timelineSlotSlideRoutine = null;
    }

    private void ApplyTimelineSlotSlidePositions(Vector2[] startPositions, Vector2[] targetPositions, float t)
    {
        if (timelineSlotSlideTargets == null || startPositions == null || targetPositions == null)
            return;

        int count = Mathf.Min(timelineSlotSlideTargets.Length, Mathf.Min(startPositions.Length, targetPositions.Length));

        for (int i = 0; i < count; i++)
        {
            RectTransform target = timelineSlotSlideTargets[i];

            if (target == null)
                continue;

            target.anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], t);
        }
    }

    private Transform GetTimelineSearchRoot()
    {
        if (timelineBarUI != null)
            return timelineBarUI.transform;

        return transform;
    }

    private void PlaySelectedSlotEffect(int previousSlotIndex, int currentSlotIndex)
    {
        AutoFindSelectedSlotEffectIfNeeded();

        if (selectedSlotEffect == null)
            return;

        float rotateStepZ = GetSelectedSlotEffectRotateStep(previousSlotIndex, currentSlotIndex);

        if (Mathf.Approximately(rotateStepZ, 0f))
            return;

        if (!selectedSlotEffect.gameObject.activeSelf)
            selectedSlotEffect.gameObject.SetActive(true);

        PlaySelectedSlotEffectSfx();

        if (selectedSlotEffectRoutine != null)
            StopCoroutine(selectedSlotEffectRoutine);

        selectedSlotEffectRoutine = StartCoroutine(PlaySelectedSlotEffectRoutine(rotateStepZ));
    }

    private void PlaySelectedSlotEffectSfx()
    {
        if (!playSelectedSlotEffectSfx)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(selectedSlotEffectSfxType, selectedSlotEffectSfxVolume);
    }

    private float GetSelectedSlotEffectRotateStep(int previousSlotIndex, int currentSlotIndex)
    {
        if (currentSlotIndex < 0)
            return 0f;

        if (previousSlotIndex < 0)
            return selectedSlotEffectRotateStepZ;

        if (currentSlotIndex > previousSlotIndex)
            return selectedSlotEffectRotateStepZ;

        if (currentSlotIndex < previousSlotIndex)
            return -selectedSlotEffectRotateStepZ;

        return 0f;
    }

    private IEnumerator PlaySelectedSlotEffectRoutine(float rotateStepZ)
    {
        float duration = Mathf.Max(0.01f, selectedSlotEffectDuration);
        float elapsed = 0f;

        float startZ = GetSelectedSlotEffectRotationZ();
        float targetZ = startZ + rotateStepZ;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTimeForSelectedSlotEffect ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            float z = Mathf.Lerp(startZ, targetZ, easedT);

            SetSelectedSlotEffectRotation(z);
            yield return null;
        }

        SetSelectedSlotEffectRotation(targetZ);
        selectedSlotEffectRoutine = null;
    }

    private float GetSelectedSlotEffectRotationZ()
    {
        AutoFindSelectedSlotEffectIfNeeded();

        if (selectedSlotEffect == null)
            return 0f;

        return NormalizeAngle(selectedSlotEffect.localEulerAngles.z);
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;

        while (angle <= -180f)
            angle += 360f;

        return angle;
    }

    private void SetSelectedSlotEffectRotation(float zRotation)
    {
        AutoFindSelectedSlotEffectIfNeeded();

        if (selectedSlotEffect == null)
            return;

        Vector3 eulerAngles = selectedSlotEffect.localEulerAngles;
        eulerAngles.z = zRotation;
        selectedSlotEffect.localEulerAngles = eulerAngles;
    }

    private void RefreshSelectedSlotValueText()
    {
        AutoFindSelectedSlotValueTextIfNeeded();

        if (selectedSlotValueText == null)
            return;

        if (activeSlotIndex < 0)
            selectedSlotValueText.text = emptySelectedSlotText;
        else
            selectedSlotValueText.text = (activeSlotIndex + 1).ToString();
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);

            if (found != null)
                return found;
        }

        return null;
    }

    private void TryStartSkillReservation()
    {
        if (activeSlotIndex < 0)
        {
            if (selectedSkill != null)
                ShowBattleWarning("타임라인 슬롯을 먼저 선택해주세요.");

            return;
        }

        if (selectedCharacter == null && selectedSkill == null)
        {
            ShowBattleWarning("캐릭터와 스킬을 먼저 선택해주세요.");
            return;
        }

        if (selectedCharacter == null)
        {
            ShowBattleWarning("캐릭터를 먼저 선택해주세요.");
            return;
        }

        if (selectedSkill == null)
            return;

        if (reserveSlots == null || reserveSlots.Length <= 0)
        {
            ShowBattleWarning("타임라인 슬롯이 없습니다.");
            selectedSkill = null;
            return;
        }

        if (activeSlotIndex >= reserveSlots.Length)
        {
            ShowBattleWarning("선택한 타임라인 슬롯을 사용할 수 없습니다.");
            selectedSkill = null;
            return;
        }

        ReserveTurnSlotUI slot = reserveSlots[activeSlotIndex];

        if (slot == null)
        {
            ShowBattleWarning("선택한 타임라인 슬롯을 사용할 수 없습니다.");
            selectedSkill = null;
            return;
        }

        PlayerReservedCommand costCheckCommand =
            new PlayerReservedCommand(selectedCharacter, selectedSkill);
        bool canMergeMoveCommand =
            CanMergeMoveCommandInSlot(activeSlotIndex, selectedCharacter, selectedSkill);

        if (!canMergeMoveCommand)
        {
            PrepareCommandForReservation(activeSlotIndex, costCheckCommand);

            string blockReason = GetReserveBlockReason(costCheckCommand);
            if (!string.IsNullOrEmpty(blockReason))
            {
                ShowBattleWarning(blockReason);
                selectedSkill = null;
                return;
            }
        }

        if (!slot.CanAcceptCharacter(selectedCharacter))
        {
            ShowBattleWarning("이 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            Debug.LogWarning("[BattleTimelineController] 이 타임라인 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            selectedSkill = null;
            return;
        }

        if (!canMergeMoveCommand && !CanAddPlayerCommandToSlot(activeSlotIndex))
        {
            ShowCombinedSlotCapacityWarning();
            selectedSkill = null;
            return;
        }

        int casterGridIndex = GetPreviewGridIndexAtSlotEnd(selectedCharacter, activeSlotIndex);

        if (casterGridIndex < 0)
        {
            ShowBattleWarning("캐릭터 위치를 찾을 수 없습니다.");
            Debug.LogWarning($"[BattleTimelineController] 캐릭터 위치를 찾을 수 없습니다: {selectedCharacter.CharacterId}");
            selectedSkill = null;
            return;
        }

        if (playerSkillReservationController == null)
            playerSkillReservationController = FindFirstObjectByType<PlayerSkillReservationController>(FindObjectsInactive.Include);

        if (playerSkillReservationController == null)
        {
            ShowBattleWarning("스킬 예약 컨트롤러를 찾을 수 없습니다.");
            Debug.LogWarning("[BattleTimelineController] PlayerSkillReservationController가 없습니다.");
            selectedSkill = null;
            return;
        }

        BattleDirection casterDirection = GetPreviewDirection(selectedCharacter, activeSlotIndex);

        playerSkillReservationController.StartReservation(
            selectedCharacter,
            selectedSkill,
            casterGridIndex,
            activeSlotIndex,
            casterDirection,
            GetSelectedCharacterSprite()
        );

        selectedSkill = null;
    }

    public bool ConfirmPlayerCommand(int slotIndex, PlayerReservedCommand command)
    {
        if (command == null)
        {
            ShowBattleWarning("예약할 스킬 정보가 없습니다.");
            return false;
        }

        if (reserveSlots == null || reserveSlots.Length <= 0)
        {
            ShowBattleWarning("타임라인 슬롯이 없습니다.");
            return false;
        }

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
        {
            ShowBattleWarning("선택한 타임라인 슬롯을 사용할 수 없습니다.");
            return false;
        }

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null)
        {
            ShowBattleWarning("선택한 타임라인 슬롯을 사용할 수 없습니다.");
            return false;
        }

        if (!slot.CanAcceptCharacter(command.UserRuntime))
        {
            ShowBattleWarning("이 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            Debug.LogWarning("[BattleTimelineController] 이 타임라인 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            return false;
        }

        if (TryHandleMoveCommandMerge(slot, command, out bool mergeSucceeded))
            return mergeSucceeded;

        PrepareCommandForReservation(slotIndex, command);

        string blockReason = GetReserveBlockReason(command);
        if (!string.IsNullOrEmpty(blockReason))
        {
            ShowBattleWarning(blockReason);
            return false;
        }

        if (!CanAddPlayerCommandToSlot(slotIndex))
        {
            ShowCombinedSlotCapacityWarning();
            return false;
        }

        bool added = slot.AddCommand(command);

        if (!added)
        {
            ShowBattleWarning("스킬을 예약할 수 없습니다.");
            Debug.LogWarning("[BattleTimelineController] 예약 슬롯이 가득 찼습니다.");
            return false;
        }

        RecalculateAllReservedCosts();

        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();

        return true;
    }

    private bool TryHandleMoveCommandMerge(
        ReserveTurnSlotUI slot,
        PlayerReservedCommand command,
        out bool mergeSucceeded)
    {
        mergeSucceeded = false;

        if (!IsMoveCommand(command) || slot == null || slot.Commands == null)
            return false;

        PlayerReservedCommand existingMoveCommand =
            FindMoveCommandInSlot(slot, command.CharacterId);

        if (existingMoveCommand == null)
            return false;

        if (!IsSelfFlipMoveCommand(command))
            return false;

        if (!CanReserveMergedMoveCommand(existingMoveCommand, command, out string blockReason))
        {
            ShowBattleWarning(blockReason);
            return true;
        }

        existingMoveCommand.MergeMoveReservation(command);

        RecalculateAllReservedCosts();
        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();

        mergeSucceeded = true;
        return true;
    }

    private bool IsSelfFlipMoveCommand(PlayerReservedCommand command)
    {
        return IsMoveCommand(command) &&
               command.ReservedMoveGridIndex >= 0 &&
               command.MoveOffset == Vector2Int.zero;
    }

    private PlayerReservedCommand FindMoveCommandInSlot(
        ReserveTurnSlotUI slot,
        string characterId)
    {
        if (slot == null || slot.Commands == null || string.IsNullOrWhiteSpace(characterId))
            return null;

        for (int i = slot.Commands.Count - 1; i >= 0; i--)
        {
            PlayerReservedCommand command = slot.Commands[i];

            if (!IsMoveCommand(command))
                continue;

            if (command.CharacterId == characterId)
                return command;
        }

        return null;
    }

    private bool CanMergeMoveCommandInSlot(
        int slotIndex,
        CharacterRuntimeData runtime,
        SkillMasterData skillData)
    {
        if (runtime == null || skillData == null || reserveSlots == null)
            return false;

        if (skillData.Category != Category.Move)
            return false;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return false;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        return FindMoveCommandInSlot(slot, runtime.CharacterId) != null;
    }

    private bool CanReserveMergedMoveCommand(
        PlayerReservedCommand existingCommand,
        PlayerReservedCommand nextCommand,
        out string blockReason)
    {
        blockReason = string.Empty;

        if (existingCommand == null || nextCommand == null || nextCommand.UserRuntime == null)
            return false;

        CharacterRuntimeData runtime = nextCommand.UserRuntime;
        int mergedMoveDistance =
            Mathf.Max(0, existingCommand.PlannedMoveDistance) +
            Mathf.Max(0, nextCommand.PlannedMoveDistance);
        int moveDistancePerCost = Mathf.Max(
            1,
            nextCommand.MoveDistancePerCost > 0
                ? nextCommand.MoveDistancePerCost
                : existingCommand.MoveDistancePerCost);
        int mergedCost = PlayerReservedCommand.CalculateMoveCost(
            mergedMoveDistance,
            moveDistancePerCost);
        int additionalCost = Mathf.Max(0, mergedCost - Mathf.Max(0, existingCommand.Cost));

        if (runtime.CanReserveCost(additionalCost))
            return true;

        blockReason = BuildShortageMessage(
            "Cost",
            additionalCost,
            runtime.CurrentCost - runtime.ReservedCost);
        return false;
    }

    public bool ConfirmPlayerCommands(
        int slotIndex,
        IReadOnlyList<PlayerReservedCommand> commands)
    {
        if (commands == null || commands.Count <= 0)
        {
            ShowBattleWarning("예약할 스킬 정보가 없습니다.");
            return false;
        }

        int addedCount = 0;

        for (int i = 0; i < commands.Count; i++)
        {
            if (ConfirmPlayerCommand(slotIndex, commands[i]))
            {
                addedCount++;
                continue;
            }

            RollbackLastPlayerCommands(slotIndex, addedCount);
            return false;
        }

        return true;
    }

    private void RollbackLastPlayerCommands(int slotIndex, int rollbackCount)
    {
        if (rollbackCount <= 0 || reserveSlots == null)
            return;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null)
            return;

        for (int i = 0; i < rollbackCount; i++)
        {
            int removeIndex = slot.Commands != null
                ? slot.Commands.Count - 1
                : -1;

            if (removeIndex < 0)
                break;

            if (slot.RemoveCommandAt(removeIndex, out PlayerReservedCommand removedCommand))
                RemoveReservedCosts(removedCommand);
        }

        reservationVersion++;
        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();
    }

    public int GetRemainingPlayerCommandCapacity(int slotIndex)
    {
        if (reserveSlots == null || slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return 0;

        if (reserveSlots[slotIndex] == null)
            return 0;

        return GetRemainingCombinedCommandCapacity(slotIndex);
    }

    private bool CanAddPlayerCommandToSlot(int slotIndex)
    {
        if (reserveSlots == null)
            return false;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return false;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null)
            return false;

        return slot.CanAddCommand() && GetRemainingCombinedCommandCapacity(slotIndex) > 0;
    }

    private int GetRemainingCombinedCommandCapacity(int slotIndex)
    {
        if (slotIndex < 0)
            return 0;

        int playerCommandCount = GetPlayerCommandCount(slotIndex);
        int monsterCommandCount = GetMonsterCommandCount(slotIndex);

        return Mathf.Max(
            0,
            ReserveTurnSlotUI.MaxCommandCount - playerCommandCount - monsterCommandCount);
    }

    private int GetPlayerCommandCount(int slotIndex)
    {
        if (reserveSlots == null || slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return 0;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        return slot != null ? slot.CommandCount : 0;
    }

    private int GetMonsterCommandCount(int slotIndex)
    {
        InitializeMonsterCommandSlots();

        if (slotIndex < 0 || slotIndex >= monsterCommandsBySlot.Length)
            return 0;

        List<MonsterReservedCommand> commands = monsterCommandsBySlot[slotIndex];

        return commands != null ? commands.Count : 0;
    }

    private void ShowCombinedSlotCapacityWarning()
    {
        ShowBattleWarning("한 슬롯에는 몬스터 행동과 캐릭터 행동을 합쳐 최대 5개만 예약할 수 있습니다.");
    }

    public int GetPreviewReservationCostValue(
        CharacterRuntimeData runtimeData,
        SkillMasterData skillData)
    {
        if (skillData == null)
            return 0;

        PlayerReservedCommand command = new(runtimeData, skillData);

        if (HasActiveReservationSlot())
            PrepareCommandForReservation(activeSlotIndex, command);

        return GetReservationCostValue(command, skillData.ReferenceResource);
    }

    private bool HasActiveReservationSlot()
    {
        return reserveSlots != null &&
               activeSlotIndex >= 0 &&
               activeSlotIndex < reserveSlots.Length;
    }

    private int GetReservationCostValue(
        PlayerReservedCommand command,
        ReferenceResource resource)
    {
        if (command == null)
            return 0;

        switch (resource)
        {
            case ReferenceResource.HP:
                return command.HPCost;

            case ReferenceResource.Cost:
            case ReferenceResource.MovePoint:
                return command.Cost;

            case ReferenceResource.UniqueResource:
                return command.ResourceCost;

            default:
                return 0;
        }
    }

    public IReadOnlyList<PlayerReservedCommand> GetPlayerCommands(int slotIndex)
    {
        if (reserveSlots == null)
            return null;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return null;

        if (reserveSlots[slotIndex] == null)
            return null;

        return reserveSlots[slotIndex].Commands;
    }

    public IReadOnlyList<MonsterReservedCommand> GetMonsterCommands(int slotIndex)
    {
        InitializeMonsterCommandSlots();

        if (slotIndex < 0 || slotIndex >= monsterCommandsBySlot.Length)
            return null;

        return monsterCommandsBySlot[slotIndex];
    }

    public void PreparePreviewCommandForReservation(int slotIndex, PlayerReservedCommand command)
    {
        PrepareCommandForReservation(slotIndex, command);
    }

    public int GetPreviewGridIndexAtSlotEnd(CharacterRuntimeData runtimeData, int targetSlotIndex)
    {
        if (runtimeData == null)
            return -1;

        if (reserveSlots == null || reserveSlots.Length <= 0)
            return GetPreviewGridIndexBeforeCommand(runtimeData, targetSlotIndex, int.MaxValue);

        int safeSlotIndex = Mathf.Clamp(targetSlotIndex, 0, reserveSlots.Length - 1);

        return GetPreviewGridIndexBeforeCommand(runtimeData, safeSlotIndex, int.MaxValue);
    }

    private void PrepareCommandForReservation(int slotIndex, PlayerReservedCommand command)
    {
        if (command == null)
            return;

        bool isFirstMoveCommand =
            BattleEquipmentEffectService.IsMoveCommand(command) &&
            !HasEarlierMoveCommand(command.UserRuntime, slotIndex);

        bool isLastTimelineSlot =
            reserveSlots != null &&
            slotIndex == reserveSlots.Length - 1;

        int duplicateSkillReservationCountInSlot =
            CountEarlierSameSkillReservationsInSlot(command, slotIndex);

        BattleEquipmentEffectService.ApplyReservationCostModifiers(
            command,
            slotIndex,
            isFirstMoveCommand,
            isLastTimelineSlot,
            duplicateSkillReservationCountInSlot);
    }

    private bool HasEarlierMoveCommand(CharacterRuntimeData runtime, int targetSlotIndex)
    {
        if (runtime == null || reserveSlots == null || reserveSlots.Length <= 0)
            return false;

        int safeTargetSlotIndex = Mathf.Clamp(targetSlotIndex, 0, reserveSlots.Length - 1);

        for (int slotIndex = 0; slotIndex <= safeTargetSlotIndex; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (command == null || command.UserRuntime == null)
                    continue;

                if (command.UserRuntime.CharacterId != runtime.CharacterId)
                    continue;

                if (BattleEquipmentEffectService.IsMoveCommand(command))
                    return true;
            }
        }

        return false;
    }

    private int CountEarlierSameSkillReservationsInSlot(
        PlayerReservedCommand targetCommand,
        int targetSlotIndex)
    {
        if (targetCommand == null || reserveSlots == null || reserveSlots.Length <= 0)
            return 0;

        string targetKey = GetDuplicateSkillReservationKey(targetCommand);
        if (string.IsNullOrEmpty(targetKey))
            return 0;

        int safeTargetSlotIndex = Mathf.Clamp(targetSlotIndex, 0, reserveSlots.Length - 1);
        ReserveTurnSlotUI slot = reserveSlots[safeTargetSlotIndex];

        if (slot == null || slot.Commands == null)
            return 0;

        int count = 0;

        for (int i = 0; i < slot.Commands.Count; i++)
        {
            PlayerReservedCommand command = slot.Commands[i];

            if (command == null)
                continue;

            if (GetDuplicateSkillReservationKey(command) == targetKey)
                count++;
        }

        return count;
    }

    private void RecalculateAllReservedCosts()
    {
        reservationVersion++;

        List<CharacterRuntimeData> runtimes = CollectReservedRuntimes();

        for (int i = 0; i < runtimes.Count; i++)
            runtimes[i].ClearReservedCosts();

        if (reserveSlots == null || reserveSlots.Length <= 0)
            return;

        HashSet<string> firstMoveAppliedCharacterIds = new();

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            bool isLastTimelineSlot = slotIndex == reserveSlots.Length - 1;
            Dictionary<string, int> duplicateSkillReservationCounts = new();

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (command == null || command.UserRuntime == null)
                    continue;

                bool isMoveContinuationCommand = command.IsMoveContinuationCommand;
                bool isMoveCommand = BattleEquipmentEffectService.IsMoveCommand(command);
                bool isFirstMoveCommand =
                    isMoveCommand &&
                    !isMoveContinuationCommand &&
                    !firstMoveAppliedCharacterIds.Contains(command.CharacterId);
                int duplicateSkillReservationCountInSlot =
                    GetAndIncrementDuplicateSkillReservationCount(
                        duplicateSkillReservationCounts,
                        command);

                BattleEquipmentEffectService.ApplyReservationCostModifiers(
                    command,
                    slotIndex,
                    isFirstMoveCommand,
                    isLastTimelineSlot,
                    duplicateSkillReservationCountInSlot);

                AddReservedCosts(command);

                if (isMoveCommand &&
                    !isMoveContinuationCommand &&
                    !firstMoveAppliedCharacterIds.Contains(command.CharacterId))
                {
                    firstMoveAppliedCharacterIds.Add(command.CharacterId);
                }
            }
        }
    }

    private int GetAndIncrementDuplicateSkillReservationCount(
        Dictionary<string, int> duplicateSkillReservationCounts,
        PlayerReservedCommand command)
    {
        if (duplicateSkillReservationCounts == null || command == null)
            return 0;

        string key = GetDuplicateSkillReservationKey(command);
        if (string.IsNullOrEmpty(key))
            return 0;

        duplicateSkillReservationCounts.TryGetValue(key, out int currentCount);
        duplicateSkillReservationCounts[key] = currentCount + 1;

        return currentCount;
    }

    private string GetDuplicateSkillReservationKey(PlayerReservedCommand command)
    {
        if (command == null ||
            command.IsMoveContinuationCommand ||
            string.IsNullOrEmpty(command.CharacterId) ||
            string.IsNullOrEmpty(command.SkillId))
        {
            return string.Empty;
        }

        return $"{command.CharacterId}:{command.SkillId}";
    }

    private List<CharacterRuntimeData> CollectReservedRuntimes()
    {
        List<CharacterRuntimeData> runtimes = new();

        if (reserveSlots == null)
            return runtimes;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
                AddRuntimeIfNeeded(runtimes, slot.Commands[i] != null ? slot.Commands[i].UserRuntime : null);
        }

        return runtimes;
    }

    private void AddRuntimeIfNeeded(List<CharacterRuntimeData> runtimes, CharacterRuntimeData runtime)
    {
        if (runtimes == null || runtime == null)
            return;

        for (int i = 0; i < runtimes.Count; i++)
        {
            if (runtimes[i] != null && runtimes[i].CharacterId == runtime.CharacterId)
                return;
        }

        runtimes.Add(runtime);
    }

    private void AddReservedCosts(PlayerReservedCommand command)
    {
        if (command == null || command.UserRuntime == null)
            return;

        command.UserRuntime.AddReservedHP(command.HPCost);
        command.UserRuntime.AddReservedCost(command.Cost);
        command.UserRuntime.AddReservedResource(command.ResourceCost);
        command.UserRuntime.AddReservedShield(command.ShieldCost);
    }

    private bool CanReserveCommand(PlayerReservedCommand command)
    {
        return string.IsNullOrEmpty(GetReserveBlockReason(command));
    }

    private string GetReserveBlockReason(PlayerReservedCommand command)
    {
        if (command == null)
            return "예약할 스킬 정보가 없습니다.";

        if (command.UserRuntime == null)
            return "선택된 캐릭터가 없습니다.";

        CharacterRuntimeData runtime = command.UserRuntime;

        if (command.SkillData != null &&
            command.SkillData.ResourceCostType == ResourceCostType.AllCurrent)
        {
            int minRequired = BattleEquipmentEffectService.GetAllCurrentMinimumCost(
                runtime,
                command.SkillData,
                command.SkillData.ResourceCostValue);

            if (command.ResourceCost < minRequired)
            {
                Debug.LogWarning(
                    $"[BattleTimelineController] AllCurrent 자원 부족 / " +
                    $"Character:{runtime.CharacterId} / " +
                    $"Skill:{command.SkillId} / " +
                    $"Cost:{command.ResourceCost} / " +
                    $"MinRequired:{minRequired}"
                );

                return $"{GetCostLabel(command.SkillData.ReferenceResource)}이 부족합니다. 필요:{minRequired} / 보유:{command.ResourceCost}";
            }
        }

        string shortageMessage = GetShortageMessage(runtime, command);
        if (!string.IsNullOrEmpty(shortageMessage))
            return shortageMessage;

        return string.Empty;
    }

    private string GetShortageMessage(CharacterRuntimeData runtime, PlayerReservedCommand command)
    {
        if (runtime == null || command == null)
            return "예약할 스킬 정보가 없습니다.";

        if (!runtime.CanReserveHP(command.HPCost))
            return BuildShortageMessage("HP", command.HPCost, runtime.CurrentHP - runtime.ReservedHPCost);

        if (!runtime.CanReserveCost(command.Cost))
            return BuildShortageMessage("Cost", command.Cost, runtime.CurrentCost - runtime.ReservedCost);

        if (!runtime.CanReserveResource(command.ResourceCost))
            return BuildShortageMessage("고유자원", command.ResourceCost, runtime.CurrentResource - runtime.ReservedResourceCost);

        if (!runtime.CanReserveShield(command.ShieldCost))
            return BuildShortageMessage("방어도", command.ShieldCost, runtime.CurrentShield - runtime.ReservedShieldCost);

        return string.Empty;
    }

    private string BuildShortageMessage(string label, int required, int available)
    {
        int safeAvailable = Mathf.Max(0, available);
        return $"{label}이 부족합니다. 필요:{required} / 보유:{safeAvailable}";
    }

    private string GetCostLabel(ReferenceResource resource)
    {
        switch (resource)
        {
            case ReferenceResource.HP:
                return "HP";

            case ReferenceResource.Cost:
            case ReferenceResource.MovePoint:
                return "Cost";

            case ReferenceResource.UniqueResource:
                return "고유자원";

            default:
                return "자원";
        }
    }

    public int GetPreviewGridIndex(CharacterRuntimeData runtimeData)
    {
        if (runtimeData == null)
            return -1;

        int gridIndex = GetCurrentBattleCharacterGridIndex(runtimeData.CharacterId);

        if (gridIndex < 0)
            gridIndex = GetRuntimeStartGridIndex(runtimeData.CharacterId);

        if (gridIndex < 0)
            return -1;

        if (reserveSlots == null)
            return gridIndex;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (command == null || command.UserRuntime == null)
                    continue;

                if (command.UserRuntime.CharacterId != runtimeData.CharacterId)
                    continue;

                if (command.ReservedMoveGridIndex >= 0)
                    gridIndex = command.EffectiveMoveGridIndex;
            }
        }

        return gridIndex;
    }

    public bool TryGetLastMoveGhostPreviewResult(
        CharacterRuntimeData runtimeData,
        out int gridIndex,
        out BattleDirection direction)
    {
        gridIndex = -1;
        direction = runtimeData != null
            ? runtimeData.Direction
            : BattleDirection.Right;

        if (runtimeData == null || reserveSlots == null)
            return false;

        PlayerReservedCommand lastMoveCommand = null;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (!IsGhostMoveCommandForRuntime(command, runtimeData.CharacterId))
                    continue;

                lastMoveCommand = command;
            }
        }

        if (lastMoveCommand == null)
            return false;

        gridIndex = lastMoveCommand.PreviewMoveGridIndex;
        direction = lastMoveCommand.PreviewMoveDirection;
        return gridIndex >= 0;
    }

    public BattleDirection GetPreviewDirection(CharacterRuntimeData runtimeData, int targetSlotIndex)
    {
        if (runtimeData == null)
            return BattleDirection.Right;

        BattleDirection direction = runtimeData.Direction;

        if (reserveSlots == null || reserveSlots.Length <= 0)
            return direction;

        if (targetSlotIndex < 0)
            return direction;

        int lastSlotIndex = Mathf.Clamp(targetSlotIndex, 0, reserveSlots.Length - 1);

        for (int slotIndex = 0; slotIndex <= lastSlotIndex; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (command == null || command.UserRuntime == null)
                    continue;

                if (command.UserRuntime.CharacterId != runtimeData.CharacterId)
                    continue;

                direction = GetDirectionAfterCommand(direction, command);
            }
        }

        return direction;
    }

    private BattleDirection GetDirectionAfterCommand(
        BattleDirection currentDirection,
        PlayerReservedCommand command)
    {
        if (command == null)
            return currentDirection;

        if (command.ReservedMoveGridIndex < 0)
            return currentDirection;

        return command.Direction;
    }

    private int GetCurrentBattleCharacterGridIndex(string characterId)
    {
        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null)
                continue;

            if (characters[i].CharacterId != characterId)
                continue;

            return characters[i].CurrentGridIndex;
        }

        return -1;
    }

    private int GetRuntimeStartGridIndex(string characterId)
    {
        if (DataManager.Instance == null)
            return -1;

        var partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int slotIndex = 0; slotIndex < partyStore.MaxPartyCountValue; slotIndex++)
        {
            if (partyStore.GetCharacterId(slotIndex) == characterId)
                return partyStore.GetSpawnGridIndex(slotIndex);
        }

        return -1;
    }

    private Sprite GetSelectedCharacterSprite()
    {
        BattleCharacter[] battleCharacters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < battleCharacters.Length; i++)
        {
            BattleCharacter battleCharacter = battleCharacters[i];

            if (battleCharacter == null || battleCharacter.RuntimeData == null)
                continue;

            if (battleCharacter.RuntimeData.CharacterId != selectedCharacter.CharacterId)
                continue;

            SpriteRenderer spriteRenderer = battleCharacter.GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                return spriteRenderer.sprite;
        }

        return null;
    }

    private void InitializeMonsterCommandSlots()
    {
        for (int i = 0; i < monsterCommandsBySlot.Length; i++)
        {
            if (monsterCommandsBySlot[i] == null)
                monsterCommandsBySlot[i] = new List<MonsterReservedCommand>();
        }
    }

    public void AddMonsterCommand(int slotIndex, MonsterReservedCommand command)
    {
        InitializeMonsterCommandSlots();

        if (command == null)
            return;

        int resolvedSlotIndex = ResolveMonsterSlotIndex(slotIndex, command);

        if (resolvedSlotIndex < 0)
        {
            Debug.LogWarning(
                $"[BattleTimelineController] 몬스터 행동을 넣을 슬롯이 없습니다. " +
                $"Monster:{command.RuntimeId} / Skill:{command.SkillId}"
            );
            return;
        }

        monsterCommandsBySlot[resolvedSlotIndex].Add(command);
        reservationVersion++;

        RefreshTimeline();
    }

    private int ResolveMonsterSlotIndex(int preferredSlotIndex, MonsterReservedCommand command)
    {
        if (command == null)
            return -1;

        int monsterSlotCount = GetMonsterReservationSlotCount();

        if (monsterSlotCount <= 0)
            return -1;

        if (preferredSlotIndex < 0)
            preferredSlotIndex = 0;

        if (preferredSlotIndex >= monsterSlotCount)
            preferredSlotIndex = monsterSlotCount - 1;

        if (CanMonsterUseSlot(preferredSlotIndex, command.RuntimeId))
            return preferredSlotIndex;

        for (int i = preferredSlotIndex + 1; i < monsterSlotCount; i++)
        {
            if (CanMonsterUseSlot(i, command.RuntimeId))
                return i;
        }

        for (int i = preferredSlotIndex - 1; i >= 0; i--)
        {
            if (CanMonsterUseSlot(i, command.RuntimeId))
                return i;
        }

        return -1;
    }

    private bool CanMonsterUseSlot(int slotIndex, string runtimeId)
    {
        if (slotIndex < 0 || slotIndex >= GetMonsterReservationSlotCount())
            return false;

        if (string.IsNullOrWhiteSpace(runtimeId))
            return false;

        if (GetRemainingCombinedCommandCapacity(slotIndex) <= 0)
            return false;

        List<MonsterReservedCommand> commands = monsterCommandsBySlot[slotIndex];

        if (commands == null || commands.Count <= 0)
            return true;

        for (int i = 0; i < commands.Count; i++)
        {
            MonsterReservedCommand command = commands[i];

            if (command == null || command.RuntimeId != runtimeId)
                return false;
        }

        return true;
    }

    private int GetMonsterReservationSlotCount()
    {
        if (monsterCommandsBySlot == null)
            return 0;

        if (reserveSlots == null || reserveSlots.Length <= 0)
            return monsterCommandsBySlot.Length;

        return Mathf.Min(reserveSlots.Length, monsterCommandsBySlot.Length);
    }

    public void ClearMonsterReservations()
    {
        InitializeMonsterCommandSlots();

        for (int i = 0; i < monsterCommandsBySlot.Length; i++)
            monsterCommandsBySlot[i].Clear();

        reservationVersion++;
        RefreshTimeline();
    }

    public void RemoveCommand(int slotIndex, int orderIndex)
    {
        if (reserveSlots == null)
            return;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null)
            return;

        bool removed = slot.RemoveCommandAt(orderIndex, out PlayerReservedCommand removedCommand);

        if (!removed)
            return;

        RemoveReservedCosts(removedCommand);

        if (IsMoveCommand(removedCommand))
            RemoveFollowingMoveCommands(slot, orderIndex, removedCommand.CharacterId);


        RecalculateAllReservedCosts();
        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();

        Debug.Log($"[BattleTimelineController] 예약 취소 / Slot:{slotIndex} / Order:{orderIndex}");
    }

    private bool IsMoveCommand(PlayerReservedCommand command)
    {
        return command != null && command.ReservedMoveGridIndex >= 0;
    }

    private void RemoveFollowingMoveCommands(
        ReserveTurnSlotUI slot,
        int startIndex,
        string characterId)
    {
        if (slot == null || slot.Commands == null)
            return;

        for (int i = slot.Commands.Count - 1; i >= startIndex; i--)
        {
            PlayerReservedCommand command = slot.Commands[i];

            if (command == null)
                continue;

            if (command.CharacterId != characterId)
                continue;

            if (!IsMoveCommand(command))
                continue;

            if (slot.RemoveCommandAt(i, out PlayerReservedCommand removedCommand))
                RemoveReservedCosts(removedCommand);
        }
    }

    public void ClearAllReservations()
    {
        ClearSelectedSlotSelection();
        reservationVersion++;

        if (reserveSlots != null)
        {
            for (int i = 0; i < reserveSlots.Length; i++)
            {
                if (reserveSlots[i] == null)
                    continue;

                var commands = reserveSlots[i].Commands;

                for (int j = commands.Count - 1; j >= 0; j--)
                {
                    if (reserveSlots[i].RemoveCommandAt(j, out PlayerReservedCommand removedCommand))
                        RemoveReservedCosts(removedCommand);
                }

                reserveSlots[i].Clear();
            }
        }

        ClearAllMonsterCommandsWithoutRefresh();

        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
    }

    public int ApplyBlockedMoveCostRefunds()
    {
        int totalRefund = 0;

        if (reserveSlots == null)
            return totalRefund;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (!IsMoveCommand(command))
                    continue;

                int refund = command.ApplyBlockedMoveCostRefund();

                if (refund <= 0)
                    continue;

                totalRefund += refund;
                Debug.Log(
                    $"[BattleTimelineController] Move Cost refund / " +
                    $"Character:{command.CharacterId} / Refund:{refund}"
                );
            }
        }

        if (totalRefund > 0)
            RefreshPlayerHUDs();

        return totalRefund;
    }

    private void ClearAllMonsterCommandsWithoutRefresh()
    {
        InitializeMonsterCommandSlots();

        if (monsterCommandsBySlot == null)
            return;

        for (int i = 0; i < monsterCommandsBySlot.Length; i++)
        {
            if (monsterCommandsBySlot[i] != null)
                monsterCommandsBySlot[i].Clear();
        }
    }

    private void RemoveReservedCosts(PlayerReservedCommand command)
    {
        if (command == null || command.UserRuntime == null)
            return;

        command.UserRuntime.RemoveReservedHP(command.HPCost);

        if (!command.MoveCostConsumed)
            command.UserRuntime.RemoveReservedCost(command.Cost);

        command.UserRuntime.RemoveReservedResource(command.ResourceCost);
        command.UserRuntime.RemoveReservedShield(command.ShieldCost);
    }

    private void RefreshReservationSimulation()
    {
        if (gridManager == null)
            return;

        BattleActionSimulationService simulator = new(gridManager);
        simulator.Simulate(this);
    }

    private void ShowBattleWarning(string message)
    {
        BattleWarningUI.ShowMessage(message);
    }

    private void RefreshTimeline()
    {
        if (timelineBarUI != null)
            timelineBarUI.Refresh(reserveSlots, monsterCommandsBySlot);
        else
        {
            ShowBattleWarning("타임라인 UI를 찾을 수 없습니다.");
            Debug.LogWarning("[BattleTimelineController] timelineBarUI가 없습니다.");
        }
    }

    private void RefreshPlayerHUDs()
    {
        PlayerHUDSlot[] hudSlots = FindObjectsByType<PlayerHUDSlot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < hudSlots.Length; i++)
        {
            if (hudSlots[i] != null)
                hudSlots[i].Refresh();
        }
    }

    public int GetPreviewGridIndexBeforeCommand(
    CharacterRuntimeData runtimeData,
    int targetSlotIndex,
    int targetPlayerCommandIndex)
    {
        if (runtimeData == null)
            return -1;

        int gridIndex = GetCurrentBattleCharacterGridIndex(runtimeData.CharacterId);

        if (gridIndex < 0)
            gridIndex = GetRuntimeStartGridIndex(runtimeData.CharacterId);

        if (gridIndex < 0)
            return -1;

        if (reserveSlots == null)
            return gridIndex;

        for (int slotIndex = 0; slotIndex <= targetSlotIndex; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (command == null || command.UserRuntime == null)
                    continue;

                if (command.UserRuntime.CharacterId != runtimeData.CharacterId)
                    continue;

                if (slotIndex == targetSlotIndex && i >= targetPlayerCommandIndex)
                    break;

                if (command.ReservedMoveGridIndex >= 0)
                    gridIndex = command.EffectiveMoveGridIndex;
            }
        }

        return gridIndex;
    }

    private void RefreshMoveGhostPreview()
    {
        if (moveGhostPreview == null)
            moveGhostPreview = FindFirstObjectByType<MoveGhostPreview>(FindObjectsInactive.Include);

        if (moveGhostPreview == null)
            return;

        if (reserveSlots == null)
        {
            moveGhostPreview.ClearAll();
            return;
        }

        Dictionary<string, PlayerReservedCommand> lastMoveCommands =
            GetLastMoveGhostCommandsByCharacter();

        foreach (var pair in lastMoveCommands)
        {
            PlayerReservedCommand command = pair.Value;

            if (command == null || command.UserRuntime == null)
                continue;

            Sprite sprite = GetCharacterSprite(command.UserRuntime.CharacterId);

            moveGhostPreview.Show(
                command.UserRuntime.CharacterId,
                sprite,
                command.PreviewMoveGridIndex,
                command.PreviewMoveDirection
            );
        }

        moveGhostPreview.ClearExcept(lastMoveCommands.Keys);
    }

    private Dictionary<string, PlayerReservedCommand> GetLastMoveGhostCommandsByCharacter()
    {
        Dictionary<string, PlayerReservedCommand> result = new();

        if (reserveSlots == null)
            return result;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (command == null || command.UserRuntime == null)
                    continue;

                if (!IsGhostMoveCommandForRuntime(command, command.UserRuntime.CharacterId))
                    continue;

                result[command.UserRuntime.CharacterId] = command;
            }
        }

        return result;
    }

    private bool IsGhostMoveCommandForRuntime(PlayerReservedCommand command, string characterId)
    {
        if (command == null || command.UserRuntime == null)
            return false;

        if (command.ReservedMoveGridIndex < 0 || command.PreviewMoveGridIndex < 0)
            return false;

        return command.UserRuntime.CharacterId == characterId;
    }

    private Sprite GetCharacterSprite(string characterId)
    {
        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (character.RuntimeData.CharacterId != characterId)
                continue;

            SpriteRenderer spriteRenderer =
                character.GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                return spriteRenderer.sprite;
        }

        return null;
    }
}
