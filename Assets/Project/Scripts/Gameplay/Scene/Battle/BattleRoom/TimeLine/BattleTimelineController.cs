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

    private int activeSlotIndex = -1;
    private CharacterRuntimeData selectedCharacter;
    private SkillMasterData selectedSkill;
    private Coroutine selectedSlotEffectRoutine;
    private Coroutine timelineSlotSlideRoutine;
    private bool isSlotSelectionLocked;
    private Vector2[] timelineSlotOriginalAnchoredPositions;
    private int timelineSlotSlideStepIndex;

    private readonly List<MonsterReservedCommand>[] monsterCommandsBySlot =
        new List<MonsterReservedCommand>[5];

    public int SlotCount => reserveSlots != null ? reserveSlots.Length : 0;

    private void Awake()
    {
        InitializeMonsterCommandSlots();
        AutoFindSelectedSlotValueTextIfNeeded();
        AutoFindSelectedSlotEffectIfNeeded();
        AutoBindTimelineSlotSlideTargetsIfNeeded();
        CaptureTimelineSlotOriginalPositionsIfNeeded();

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

        string blockReason = GetReserveBlockReason(costCheckCommand);
        if (!string.IsNullOrEmpty(blockReason))
        {
            ShowBattleWarning(blockReason);
            selectedSkill = null;
            return;
        }

        if (!slot.CanAcceptCharacter(selectedCharacter))
        {
            ShowBattleWarning("이 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            Debug.LogWarning("[BattleTimelineController] 이 타임라인 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            selectedSkill = null;
            return;
        }

        if (!slot.CanAddCommand())
        {
            ShowBattleWarning("한 슬롯에는 최대 3개의 스킬만 예약할 수 있습니다.");
            selectedSkill = null;
            return;
        }

        int casterGridIndex = GetPreviewGridIndex(selectedCharacter);

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

        playerSkillReservationController.StartReservation(
            selectedCharacter,
            selectedSkill,
            casterGridIndex,
            activeSlotIndex,
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

        string blockReason = GetReserveBlockReason(command);
        if (!string.IsNullOrEmpty(blockReason))
        {
            ShowBattleWarning(blockReason);
            return false;
        }

        if (!slot.CanAcceptCharacter(command.UserRuntime))
        {
            ShowBattleWarning("이 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            Debug.LogWarning("[BattleTimelineController] 이 타임라인 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            return false;
        }

        if (!slot.CanAddCommand())
        {
            ShowBattleWarning("한 슬롯에는 최대 3개의 스킬만 예약할 수 있습니다.");
            return false;
        }

        bool added = slot.AddCommand(command);

        if (!added)
        {
            ShowBattleWarning("스킬을 예약할 수 없습니다.");
            Debug.LogWarning("[BattleTimelineController] 예약 슬롯이 가득 찼습니다.");
            return false;
        }

        command.UserRuntime.AddReservedHealth(command.HealthCost);
        command.UserRuntime.AddReservedStamina(command.StaminaCost);
        command.UserRuntime.AddReservedResource(command.ResourceCost);
        command.UserRuntime.AddReservedMove(command.MoveCost);
        command.UserRuntime.AddReservedShield(command.ShieldCost);

        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();

        return true;
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
            int minRequired = Mathf.Max(1, command.SkillData.ResourceCostValue);

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

        if (!runtime.CanReserveHealth(command.HealthCost))
            return BuildShortageMessage("체력", command.HealthCost, runtime.CurrentHealth - runtime.ReservedHealthCost);

        if (!runtime.CanReserveStamina(command.StaminaCost))
            return BuildShortageMessage("코스트", command.StaminaCost, runtime.CurrentStamina - runtime.ReservedStaminaCost);

        if (!runtime.CanReserveResource(command.ResourceCost))
            return BuildShortageMessage("고유자원", command.ResourceCost, runtime.CurrentResource - runtime.ReservedResourceCost);

        if (!runtime.CanReserveMove(command.MoveCost))
            return BuildShortageMessage("이동 포인트", command.MoveCost, runtime.CurrentMoveLevel - runtime.ReservedMoveCost);

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
            case ReferenceResource.Health:
                return "체력";

            case ReferenceResource.Stamina:
                return "코스트";

            case ReferenceResource.UniqueResource:
                return "고유자원";

            case ReferenceResource.MovePoint:
                return "이동 포인트";

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
                    gridIndex = command.ReservedMoveGridIndex;
            }
        }

        return gridIndex;
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

        RefreshTimeline();
    }

    private int ResolveMonsterSlotIndex(int preferredSlotIndex, MonsterReservedCommand command)
    {
        if (command == null)
            return -1;

        if (preferredSlotIndex < 0)
            preferredSlotIndex = 0;

        if (preferredSlotIndex >= monsterCommandsBySlot.Length)
            preferredSlotIndex = monsterCommandsBySlot.Length - 1;

        if (CanMonsterUseSlot(preferredSlotIndex, command.RuntimeId))
            return preferredSlotIndex;

        for (int i = preferredSlotIndex + 1; i < monsterCommandsBySlot.Length; i++)
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
        if (slotIndex < 0 || slotIndex >= monsterCommandsBySlot.Length)
            return false;

        List<MonsterReservedCommand> commands = monsterCommandsBySlot[slotIndex];

        if (commands == null || commands.Count <= 0)
            return true;

        for (int i = 0; i < commands.Count; i++)
        {
            MonsterReservedCommand command = commands[i];

            if (command == null)
                continue;

            if (command.RuntimeId != runtimeId)
                return false;
        }

        return true;
    }

    public void ClearMonsterReservations()
    {
        InitializeMonsterCommandSlots();

        for (int i = 0; i < monsterCommandsBySlot.Length; i++)
            monsterCommandsBySlot[i].Clear();

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

        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();

        Debug.Log($"[BattleTimelineController] 예약 취소 / Slot:{slotIndex} / Order:{orderIndex}");
    }

    public void ClearAllReservations()
    {
        ClearSelectedSlotSelection();

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

        RefreshTimeline();
        RefreshPlayerHUDs();
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

        command.UserRuntime.RemoveReservedHealth(command.HealthCost);
        command.UserRuntime.RemoveReservedStamina(command.StaminaCost);
        command.UserRuntime.RemoveReservedResource(command.ResourceCost);
        command.UserRuntime.RemoveReservedMove(command.MoveCost);
        command.UserRuntime.RemoveReservedShield(command.ShieldCost);
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
                    gridIndex = command.ReservedMoveGridIndex;
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

        moveGhostPreview.ClearAll();

        if (reserveSlots == null)
            return;

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

                if (command.ReservedMoveGridIndex < 0)
                    continue;

                Sprite sprite = GetCharacterSprite(command.UserRuntime.CharacterId);

                moveGhostPreview.Show(
                    command.UserRuntime.CharacterId,
                    sprite,
                    command.ReservedMoveGridIndex,
                    command.Direction
                );
            }
        }
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