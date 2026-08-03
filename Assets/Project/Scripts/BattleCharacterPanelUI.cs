using Relic.Gameplay.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투에서 현재 선택된 캐릭터의 기본 정보를 하단 통합 UI에 표시합니다.
/// 캐릭터가 변경되면 Bind를 호출하고, 수치가 변경되면 Refresh를 호출합니다.
/// </summary>
public class BattleCharacterPanelUI : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text characterNameText;

    [Header("Passive Skill")]
    [SerializeField] private Image passiveIconImage;
    [SerializeField] private TMP_Text passiveNameText;
    [SerializeField] private TMP_Text passiveEffectText;

    [Header("HP")]
    [SerializeField] private Image hpIconImage;
    [SerializeField] private TMP_Text hpValueText;

    [Header("Cost")]
    [SerializeField] private Image costIconImage;
    [SerializeField] private TMP_Text costValueText;

    [Header("Armor")]
    [SerializeField] private Image armorIconImage;
    [SerializeField] private TMP_Text armorValueText;

    [Header("Cost Recovery")]
    [SerializeField] private Image recoveryIconImage;
    [SerializeField] private TMP_Text recoveryValueText;

    [Header("Skill List")]
    [SerializeField] private Button skill01Button;
    [SerializeField] private Image skill01IconImage;
    [SerializeField] private TMP_Text skill01NameText;
    [SerializeField] private Button skill02Button;
    [SerializeField] private Image skill02IconImage;
    [SerializeField] private TMP_Text skill02NameText;
    [SerializeField] private Button skill03Button;
    [SerializeField] private Image skill03IconImage;
    [SerializeField] private TMP_Text skill03NameText;
    [SerializeField] private Button skill04Button;
    [SerializeField] private Image skill04IconImage;
    [SerializeField] private TMP_Text skill04NameText;

    [Header("Battle Action Controllers")]
    [Tooltip("스킬 선택과 범위 미리보기를 처리하는 전투 타임라인 컨트롤러입니다.")]
    [SerializeField] private BattleTimelineController battleTimelineController;

    [Tooltip("액티브 유물의 대상 선택을 처리하는 컨트롤러입니다.")]
    [SerializeField] private ActiveRelicTargetingController activeRelicTargetingController;

    [Tooltip("플레이어 입력 가능 여부를 확인하는 전투 실행 컨트롤러입니다.")]
    [SerializeField] private BattleTurnExecutor turnExecutor;

    [Header("Move Button")]
    [SerializeField] private Button moveButton;
    [SerializeField] private Image moveIconImage;
    [SerializeField] private TMP_Text moveNameText;

    [Header("Item Button")]
    [SerializeField] private Button itemButton;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TMP_Text itemValueText;

    [Header("Rune List")]
    [SerializeField] private Image rune01Image;
    [SerializeField] private Image rune02Image;
    [SerializeField] private Image rune03Image;
    [SerializeField] private Image rune04Image;
    [SerializeField] private Image rune05Image;
    [SerializeField] private Image rune06Image;

    [Header("Passive Relic List")]
    [SerializeField] private Image relic01Image;
    [SerializeField] private Image relic02Image;
    [SerializeField] private Image relic03Image;
    [SerializeField] private Image relic04Image;
    [SerializeField] private Image relic05Image;
    [SerializeField] private Image relic06Image;

    [Header("Skill Slot Visual")]
    [SerializeField] private Color skillNameColor = Color.white;
    [SerializeField] private Color emptySkillNameColor = new Color32(0x77, 0x77, 0x77, 0xFF);
    [SerializeField] private string emptySkillName = "스킬 없음";

    [Header("Skill Info")]
    [SerializeField] private Image skillInfoIconImage;
    [SerializeField] private Image skillInfoRangeImage;
    [SerializeField] private TMP_Text skillInfoNameText;

    [Header("Skill Info Cost")]
    [SerializeField] private Image skillInfoCostIconImage;
    [SerializeField] private TMP_Text skillInfoCostNameText;
    [SerializeField] private TMP_Text skillInfoCostValueText;
    [SerializeField] private Sprite costResourceIcon;
    [SerializeField] private Sprite hpResourceIcon;
    [SerializeField] private Sprite uniqueResourceIcon;
    [SerializeField] private Sprite moveResourceIcon;

    [Header("Skill Info Details")]
    [SerializeField] private TMP_Text skillInfoTypeText;
    [SerializeField] private TMP_Text skillInfoDetailsText;

    [Header("Skill Info Effects")]
    [SerializeField] private GameObject skillEffect01;
    [SerializeField] private TMP_Text skillEffect01Text;
    [SerializeField] private TMP_Text skillEffect01Value;
    [SerializeField] private GameObject skillEffect02;
    [SerializeField] private TMP_Text skillEffect02Text;
    [SerializeField] private TMP_Text skillEffect02Value;
    [SerializeField] private GameObject skillEffect03;
    [SerializeField] private TMP_Text skillEffect03Text;
    [SerializeField] private TMP_Text skillEffect03Value;

    [Header("Panel Position Animation")]
    [Tooltip("전투 진행 중 패널이 내려가 있을 Y 위치입니다.")]
    [SerializeField] private float executionPositionY = 150f;

    [Tooltip("플레이어가 행동을 예약할 때 패널이 올라올 Y 위치입니다.")]
    [SerializeField] private float reservationPositionY = 540f;

    [Header("Battle Slot Position Animation")]
    [Tooltip("BattleSlot 오브젝트의 RectTransform입니다.")]
    [SerializeField] private RectTransform battleSlotRectTransform;

    [Tooltip("전투방에 처음 입장했을 때 BattleSlot이 대기하는 Y 위치입니다.")]
    [SerializeField] private float battleSlotDefaultPositionY = 190f;

    [Tooltip("전투 진행 중 BattleSlot이 표시되는 Y 위치입니다.")]
    [SerializeField] private float battleSlotExecutionPositionY = 250f;

    [Tooltip("플레이어가 행동을 예약할 때 BattleSlot이 올라올 Y 위치입니다.")]
    [SerializeField] private float battleSlotReservationPositionY = 475f;

    [Tooltip("전투방 입장 및 예약 단계에서 사용하는 BattleSlot 크기입니다.")]
    [SerializeField, Min(0f)] private float battleSlotNormalScale = 1f;

    [Tooltip("전투 진행 단계에서 사용하는 BattleSlot 크기입니다.")]
    [SerializeField, Min(0f)] private float battleSlotExecutionScale = 1.3f;

    [Tooltip("패널 위치가 이동하는 데 걸리는 시간입니다.")]
    [SerializeField, Min(0f)] private float panelMoveDuration = 0.25f;

    [SerializeField] private bool useUnscaledTimeForPanelMove = true;

    [Header("Number Change Animation")]
    [Tooltip("현재 표시값에서 변경된 값까지 숫자가 변하는 시간입니다.")]
    [SerializeField, Min(0f)] private float numberChangeDuration = 0.2f;

    [Header("Status Effects")]
    [Tooltip("상태효과 아이콘이 생성될 부모 오브젝트입니다.")]
    [SerializeField] private RectTransform statusEffectListRoot;

    [Tooltip("기존 StatusEffectIcon 프리팹입니다.")]
    [SerializeField] private StatusEffectIcon statusEffectIconPrefab;

    [Tooltip("한 줄에 표시할 상태효과 아이콘 수입니다.")]
    [SerializeField, Min(1)] private int statusEffectColumnCount = 6;

    [Tooltip("상태효과 아이콘 한 칸의 크기입니다.")]
    [SerializeField] private Vector2 statusEffectCellSize = new Vector2(40f, 40f);

    [Tooltip("상태효과 아이콘 사이 간격입니다.")]
    [SerializeField] private Vector2 statusEffectSpacing = new Vector2(4f, 4f);

    [Header("Unique Resource Slots")]
    [Tooltip("Resource01 오브젝트")]
    [SerializeField] private GameObject resource01;

    [Tooltip("Resource02 오브젝트")]
    [SerializeField] private GameObject resource02;

    [Tooltip("Resource03 오브젝트")]
    [SerializeField] private GameObject resource03;

    [Tooltip("Resource04 오브젝트")]
    [SerializeField] private GameObject resource04;

    [Tooltip("Resource05 오브젝트")]
    [SerializeField] private GameObject resource05;

    private CharacterRuntimeData boundRuntime;
    private CharacterMasterData boundMaster;
    private ActiveRelicService activeRelicService;
    private readonly List<StatusEffectIcon> spawnedStatusEffectIcons = new();

    private Coroutine numberChangeCoroutine;
    private Coroutine panelMoveCoroutine;
    private bool isBattleExecutionInProgress;
    private RectTransform panelRectTransform;
    private bool hasDisplayedStats;
    private int displayedHp;
    private int displayedCost;
    private int displayedArmor;
    private int displayedRecovery;
    private int displayedResource;

    private int lastPreviewHp = int.MinValue;
    private int lastPreviewCost = int.MinValue;
    private int lastPreviewShield = int.MinValue;
    private int lastPreviewResource = int.MinValue;
    private int lastMaxHp = int.MinValue;
    private int lastMaxCost = int.MinValue;
    private int lastMaxResource = int.MinValue;
    private int lastRecovery = int.MinValue;
    private int lastStatusEffectHash = int.MinValue;
    private int lastSkillLoadoutHash = int.MinValue;
    private int lastRuneLoadoutHash = int.MinValue;
    private int lastPassiveRelicLoadoutHash = int.MinValue;

    public CharacterRuntimeData BoundRuntime => boundRuntime;

    private void Awake()
    {
        panelRectTransform = GetComponent<RectTransform>();
        RegisterSkillButtonListeners();
        RegisterMoveAndItemButtonListeners();
        EnsureSkillButtonHoverEffects();
        EnsureMoveAndItemButtonHoverEffects();
    }

    private void OnEnable()
    {
        BattleTurnExecutor.BattleExecutionStarted -= HandleBattleExecutionStarted;
        BattleTurnExecutor.BattleExecutionStarted += HandleBattleExecutionStarted;
        BattleTurnExecutor.PlayerTurnReturned -= HandlePlayerTurnReturned;
        BattleTurnExecutor.PlayerTurnReturned += HandlePlayerTurnReturned;
        BattleSceneController.BattleRoomIntroStarted -= HandleBattleRoomIntroStarted;
        BattleSceneController.BattleRoomIntroStarted += HandleBattleRoomIntroStarted;
        BattleSceneController.BattleRoomIntroCompleted -= HandleBattleRoomIntroCompleted;
        BattleSceneController.BattleRoomIntroCompleted += HandleBattleRoomIntroCompleted;
        BattleMapIntroText.IntroStarted -= HandleBattleMapIntroStarted;
        BattleMapIntroText.IntroStarted += HandleBattleMapIntroStarted;
        BattleMapIntroText.IntroCompleted -= HandleBattleMapIntroCompleted;
        BattleMapIntroText.IntroCompleted += HandleBattleMapIntroCompleted;

        ApplyCurrentBattlePhasePositionImmediate();
    }

    private void OnDestroy()
    {
        UnregisterSkillButtonListeners();
        UnregisterMoveAndItemButtonListeners();
    }

    private void OnDisable()
    {
        BattleTurnExecutor.BattleExecutionStarted -= HandleBattleExecutionStarted;
        BattleTurnExecutor.PlayerTurnReturned -= HandlePlayerTurnReturned;
        BattleSceneController.BattleRoomIntroStarted -= HandleBattleRoomIntroStarted;
        BattleSceneController.BattleRoomIntroCompleted -= HandleBattleRoomIntroCompleted;
        BattleMapIntroText.IntroStarted -= HandleBattleMapIntroStarted;
        BattleMapIntroText.IntroCompleted -= HandleBattleMapIntroCompleted;

        StopNumberChangeCoroutine();
        StopPanelMoveCoroutine();
        hasDisplayedStats = false;
    }

    private void HandleBattleExecutionStarted()
    {
        isBattleExecutionInProgress = true;
        MovePanelToY(executionPositionY);
    }

    private void HandlePlayerTurnReturned()
    {
        isBattleExecutionInProgress = false;

        if (IsIntroBlockingPanel())
            return;

        MoveBattleSlotToDefaultThenReservation();
    }


    private void MoveBattleSlotToDefaultThenReservation()
    {
        if (panelRectTransform == null)
            panelRectTransform = GetComponent<RectTransform>();

        StopPanelMoveCoroutine();

        if (!isActiveAndEnabled || panelMoveDuration <= 0f)
        {
            SetPanelAndBattleSlotDefaultImmediate();
            SetPanelPositionYImmediate(reservationPositionY);
            return;
        }

        panelMoveCoroutine = StartCoroutine(MoveBattleSlotToDefaultThenReservationRoutine());
    }

    private IEnumerator MoveBattleSlotToDefaultThenReservationRoutine()
    {
        // 전투가 끝난 직후에는 BattleSlot만 먼저 기본 상태로 돌아옵니다.
        yield return AnimatePanelAndBattleSlotRoutine(
            executionPositionY,
            battleSlotDefaultPositionY,
            battleSlotNormalScale
        );

        // BattleCharacterPanel이 올라올 때 BattleSlot도 예약 위치로 함께 이동합니다.
        yield return AnimatePanelAndBattleSlotRoutine(
            reservationPositionY,
            battleSlotReservationPositionY,
            battleSlotNormalScale
        );

        panelMoveCoroutine = null;
    }

    private void HandleBattleRoomIntroStarted()
    {
        // 새 전투방 입장 인트로는 이전 전투 진행 상태를 종료하고
        // BattleSlot을 기본 위치와 크기로 초기화합니다.
        isBattleExecutionInProgress = false;
        SetPanelAndBattleSlotDefaultImmediate();
    }

    private void HandleBattleRoomIntroCompleted()
    {
        TryMovePanelToReservationPositionAfterIntro();
    }

    private void HandleBattleMapIntroStarted()
    {
        // 전투 진행 중 표시되는 일반 인트로 텍스트는 BattleSlot 상태를
        // 초기화하지 않습니다. TimelineBar가 진행되는 동안에는
        // Y 250, Scale 1.3 상태를 계속 유지해야 합니다.
        if (isBattleExecutionInProgress)
            return;

        SetPanelAndBattleSlotDefaultImmediate();
    }

    private void HandleBattleMapIntroCompleted()
    {
        TryMovePanelToReservationPositionAfterIntro();
    }

    private void TryMovePanelToReservationPositionAfterIntro()
    {
        if (isBattleExecutionInProgress || IsIntroBlockingPanel())
            return;

        EnsureTurnExecutor();

        if (turnExecutor != null && turnExecutor.CanAcceptPlayerInput)
            MovePanelToY(reservationPositionY);
    }

    private static bool IsIntroBlockingPanel()
    {
        return BattleSceneController.IsBattleRoomIntroPlaying ||
               BattleMapIntroText.IsAnyPlayingOrVisible();
    }

    private void ApplyCurrentBattlePhasePositionImmediate()
    {
        EnsureTurnExecutor();

        bool canShowReservationPosition =
            !isBattleExecutionInProgress &&
            !IsIntroBlockingPanel() &&
            turnExecutor != null &&
            turnExecutor.CanAcceptPlayerInput;

        if (IsIntroBlockingPanel())
        {
            SetPanelAndBattleSlotDefaultImmediate();
            return;
        }

        float targetY = canShowReservationPosition
            ? reservationPositionY
            : executionPositionY;

        SetPanelPositionYImmediate(targetY);
    }

    private void MovePanelToY(float targetY)
    {
        if (panelRectTransform == null)
            panelRectTransform = GetComponent<RectTransform>();

        if (panelRectTransform == null)
            return;

        StopPanelMoveCoroutine();

        if (!isActiveAndEnabled || panelMoveDuration <= 0f)
        {
            SetPanelPositionYImmediate(targetY);
            return;
        }

        panelMoveCoroutine = StartCoroutine(MovePanelRoutine(targetY));
    }

    private IEnumerator MovePanelRoutine(float targetY)
    {
        yield return AnimatePanelAndBattleSlotRoutine(
            targetY,
            ResolveBattleSlotTargetY(targetY),
            ResolveBattleSlotTargetScale(targetY)
        );

        panelMoveCoroutine = null;
    }

    private IEnumerator AnimatePanelAndBattleSlotRoutine(
        float panelTargetY,
        float battleSlotTargetY,
        float battleSlotTargetScaleValue)
    {
        Vector2 panelStartPosition = panelRectTransform != null
            ? panelRectTransform.anchoredPosition
            : Vector2.zero;
        Vector2 panelTargetPosition = new Vector2(panelStartPosition.x, panelTargetY);

        Vector2 battleSlotStartPosition = battleSlotRectTransform != null
            ? battleSlotRectTransform.anchoredPosition
            : Vector2.zero;
        Vector2 battleSlotTargetPosition = battleSlotRectTransform != null
            ? new Vector2(battleSlotStartPosition.x, battleSlotTargetY)
            : Vector2.zero;
        Vector3 battleSlotStartScale = battleSlotRectTransform != null
            ? battleSlotRectTransform.localScale
            : Vector3.one;
        Vector3 battleSlotTargetScale = Vector3.one * battleSlotTargetScaleValue;

        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, panelMoveDuration);

        while (elapsed < duration)
        {
            elapsed += useUnscaledTimeForPanelMove
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float smoothTime = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);

            if (panelRectTransform != null)
            {
                panelRectTransform.anchoredPosition = Vector2.LerpUnclamped(
                    panelStartPosition,
                    panelTargetPosition,
                    smoothTime
                );
            }

            if (battleSlotRectTransform != null)
            {
                battleSlotRectTransform.anchoredPosition = Vector2.LerpUnclamped(
                    battleSlotStartPosition,
                    battleSlotTargetPosition,
                    smoothTime
                );
                battleSlotRectTransform.localScale = Vector3.LerpUnclamped(
                    battleSlotStartScale,
                    battleSlotTargetScale,
                    smoothTime
                );
            }

            yield return null;
        }

        if (panelRectTransform != null)
            panelRectTransform.anchoredPosition = panelTargetPosition;

        if (battleSlotRectTransform != null)
        {
            battleSlotRectTransform.anchoredPosition = battleSlotTargetPosition;
            battleSlotRectTransform.localScale = battleSlotTargetScale;
        }
    }

    private void SetPanelPositionYImmediate(float targetY)
    {
        if (panelRectTransform == null)
            panelRectTransform = GetComponent<RectTransform>();

        if (panelRectTransform == null)
            return;

        Vector2 position = panelRectTransform.anchoredPosition;
        position.y = targetY;
        panelRectTransform.anchoredPosition = position;

        if (battleSlotRectTransform != null)
        {
            Vector2 battleSlotPosition = battleSlotRectTransform.anchoredPosition;
            battleSlotPosition.y = ResolveBattleSlotTargetY(targetY);
            battleSlotRectTransform.anchoredPosition = battleSlotPosition;
            battleSlotRectTransform.localScale = Vector3.one * ResolveBattleSlotTargetScale(targetY);
        }
    }

    private void SetPanelAndBattleSlotDefaultImmediate()
    {
        StopPanelMoveCoroutine();

        if (panelRectTransform == null)
            panelRectTransform = GetComponent<RectTransform>();

        if (panelRectTransform != null)
        {
            Vector2 panelPosition = panelRectTransform.anchoredPosition;
            panelPosition.y = executionPositionY;
            panelRectTransform.anchoredPosition = panelPosition;
        }

        if (battleSlotRectTransform != null)
        {
            Vector2 battleSlotPosition = battleSlotRectTransform.anchoredPosition;
            battleSlotPosition.y = battleSlotDefaultPositionY;
            battleSlotRectTransform.anchoredPosition = battleSlotPosition;
            battleSlotRectTransform.localScale = Vector3.one * battleSlotNormalScale;
        }
    }

    private float ResolveBattleSlotTargetY(float panelTargetY)
    {
        return Mathf.Approximately(panelTargetY, reservationPositionY)
            ? battleSlotReservationPositionY
            : battleSlotExecutionPositionY;
    }

    private float ResolveBattleSlotTargetScale(float panelTargetY)
    {
        return Mathf.Approximately(panelTargetY, reservationPositionY)
            ? battleSlotNormalScale
            : battleSlotExecutionScale;
    }

    private void StopPanelMoveCoroutine()
    {
        if (panelMoveCoroutine == null)
            return;

        StopCoroutine(panelMoveCoroutine);
        panelMoveCoroutine = null;
    }

    private void LateUpdate()
    {
        // 다른 UI 갱신이나 레이아웃 처리로 BattleSlot의 위치만 되돌아가는 경우를 막습니다.
        // 전투 진행 중에는 이동 애니메이션이 끝난 뒤 Y 250 / Scale 1.3 상태를 계속 고정합니다.
        if (isBattleExecutionInProgress && panelMoveCoroutine == null)
            EnforceBattleSlotExecutionState();

        if (boundRuntime == null)
            return;

        if (HasRuntimeDisplayChanged())
            Refresh();
    }

    private void EnforceBattleSlotExecutionState()
    {
        if (battleSlotRectTransform == null)
            return;

        Vector2 position = battleSlotRectTransform.anchoredPosition;
        position.y = battleSlotExecutionPositionY;
        battleSlotRectTransform.anchoredPosition = position;
        battleSlotRectTransform.localScale = Vector3.one * battleSlotExecutionScale;
    }

    public void Bind(CharacterRuntimeData runtimeData)
    {
        StopNumberChangeCoroutine();
        hasDisplayedStats = false;
        boundRuntime = runtimeData;
        boundMaster = null;

        if (boundRuntime != null && DataManager.Instance != null &&
            DataManager.Instance.CharacterDatabase != null)
        {
            DataManager.Instance.CharacterDatabase.TryGet(
                boundRuntime.CharacterId,
                out boundMaster
            );
        }

        Refresh();
        ShowDefaultSkillInfo();
    }

    private void ShowDefaultSkillInfo()
    {
        SkillMasterData moveSkillData = ResolveSkillData(
            boundRuntime != null ? boundRuntime.MoveSkillId : string.Empty
        );

        if (moveSkillData != null)
            ShowSkillInfo(moveSkillData);
    }

    public void Refresh()
    {
        if (boundRuntime == null)
        {
            Clear();
            return;
        }

        RefreshPortrait();
        RefreshCharacterName();
        RefreshPassiveSkill();
        RefreshSkillList();
        RefreshRuneList();
        RefreshPassiveRelicList();
        RefreshMoveButton();
        RefreshItemButton();

        int maxHp = ResolveMaxHp();
        int maxCost = ResolveMaxCost();
        int maxResource = ResolveMaxResource();

        SetStatVisualActive(hpIconImage, hpValueText, true);
        SetStatVisualActive(costIconImage, costValueText, true);
        SetStatVisualActive(armorIconImage, armorValueText, true);
        SetStatVisualActive(recoveryIconImage, recoveryValueText, true);

        int targetHp = Mathf.Clamp(boundRuntime.PreviewHP, 0, Mathf.Max(0, maxHp));
        int targetCost = Mathf.Clamp(boundRuntime.PreviewCost, 0, Mathf.Max(0, maxCost));
        int targetArmor = Mathf.Max(0, boundRuntime.PreviewShield);
        int targetRecovery = ResolveRecovery();
        int targetResource = Mathf.Clamp(
            boundRuntime.PreviewResource,
            0,
            Mathf.Max(0, maxResource)
        );

        RefreshAnimatedStats(
            targetHp,
            maxHp,
            targetCost,
            maxCost,
            targetArmor,
            targetRecovery,
            targetResource,
            maxResource
        );

        RefreshStatusEffects();
        CaptureRuntimeDisplayState();
    }

    private bool HasRuntimeDisplayChanged()
    {
        int maxHp = ResolveMaxHp();
        int maxCost = ResolveMaxCost();
        int maxResource = ResolveMaxResource();
        int recovery = ResolveRecovery();
        int statusEffectHash = CalculateStatusEffectHash();
        int skillLoadoutHash = CalculateSkillLoadoutHash();
        int runeLoadoutHash = CalculateRuneLoadoutHash();
        int passiveRelicLoadoutHash = CalculatePassiveRelicLoadoutHash();

        return lastPreviewHp != boundRuntime.PreviewHP ||
               lastPreviewCost != boundRuntime.PreviewCost ||
               lastPreviewShield != boundRuntime.PreviewShield ||
               lastPreviewResource != boundRuntime.PreviewResource ||
               lastMaxHp != maxHp ||
               lastMaxCost != maxCost ||
               lastMaxResource != maxResource ||
               lastRecovery != recovery ||
               lastStatusEffectHash != statusEffectHash ||
               lastSkillLoadoutHash != skillLoadoutHash ||
               lastRuneLoadoutHash != runeLoadoutHash ||
               lastPassiveRelicLoadoutHash != passiveRelicLoadoutHash;
    }

    private void CaptureRuntimeDisplayState()
    {
        if (boundRuntime == null)
        {
            ResetRuntimeDisplayState();
            return;
        }

        lastPreviewHp = boundRuntime.PreviewHP;
        lastPreviewCost = boundRuntime.PreviewCost;
        lastPreviewShield = boundRuntime.PreviewShield;
        lastPreviewResource = boundRuntime.PreviewResource;
        lastMaxHp = ResolveMaxHp();
        lastMaxCost = ResolveMaxCost();
        lastMaxResource = ResolveMaxResource();
        lastRecovery = ResolveRecovery();
        lastStatusEffectHash = CalculateStatusEffectHash();
        lastSkillLoadoutHash = CalculateSkillLoadoutHash();
        lastRuneLoadoutHash = CalculateRuneLoadoutHash();
        lastPassiveRelicLoadoutHash = CalculatePassiveRelicLoadoutHash();
    }

    private void ResetRuntimeDisplayState()
    {
        lastPreviewHp = int.MinValue;
        lastPreviewCost = int.MinValue;
        lastPreviewShield = int.MinValue;
        lastPreviewResource = int.MinValue;
        lastMaxHp = int.MinValue;
        lastMaxCost = int.MinValue;
        lastMaxResource = int.MinValue;
        lastRecovery = int.MinValue;
        lastStatusEffectHash = int.MinValue;
        lastSkillLoadoutHash = int.MinValue;
        lastRuneLoadoutHash = int.MinValue;
        lastPassiveRelicLoadoutHash = int.MinValue;
    }

    private int CalculateSkillLoadoutHash()
    {
        if (boundRuntime == null)
            return 0;

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + GetSkillIdForDisplaySlot(0).GetHashCode();
            hash = hash * 31 + GetSkillIdForDisplaySlot(1).GetHashCode();
            hash = hash * 31 + GetSkillIdForDisplaySlot(2).GetHashCode();
            hash = hash * 31 + GetSkillIdForDisplaySlot(3).GetHashCode();
            hash = hash * 31 + (boundRuntime.MoveSkillId ?? string.Empty).GetHashCode();
            hash = hash * 31 + (boundRuntime.PassiveSkillId ?? string.Empty).GetHashCode();

            string activeRelicId = ActiveRelicRuntimeUtility.GetActiveRelicId(boundRuntime);
            hash = hash * 31 + (activeRelicId ?? string.Empty).GetHashCode();

            if (!string.IsNullOrWhiteSpace(activeRelicId) &&
                DataManager.Instance != null &&
                DataManager.Instance.RelicDatabase != null &&
                DataManager.Instance.RelicDatabase.TryGet(activeRelicId, out RelicData relic) &&
                relic != null)
            {
                hash = hash * 31 + ActiveRelicRuntimeUtility.GetRemainingUses(boundRuntime, relic);
                hash = hash * 31 + ActiveRelicRuntimeUtility.GetMaxUses(relic);
            }

            return hash;
        }
    }

    private int CalculateRuneLoadoutHash()
    {
        if (boundRuntime == null || boundRuntime.EquippedRuneIds == null)
            return 0;

        unchecked
        {
            int hash = 17;
            int slotCount = Mathf.Min(6, boundRuntime.EquippedRuneIds.Length);

            for (int i = 0; i < slotCount; i++)
            {
                string runeId = boundRuntime.EquippedRuneIds[i] ?? string.Empty;
                hash = hash * 31 + runeId.GetHashCode();
            }

            return hash;
        }
    }

    private int CalculatePassiveRelicLoadoutHash()
    {
        if (boundRuntime == null || boundRuntime.EquippedRelicIds == null)
            return 0;

        unchecked
        {
            int hash = 17;

            for (int passiveSlotIndex = 0; passiveSlotIndex < 6; passiveSlotIndex++)
            {
                string relicId = GetEquippedPassiveRelicId(passiveSlotIndex);
                hash = hash * 31 + relicId.GetHashCode();
            }

            return hash;
        }
    }

    private int CalculateStatusEffectHash()
    {
        if (boundRuntime == null || boundRuntime.StatusEffects == null)
            return 0;

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + boundRuntime.StatusEffects.Count;

            for (int i = 0; i < boundRuntime.StatusEffects.Count; i++)
            {
                StatusEffectRuntimeData effect = boundRuntime.StatusEffects[i];
                if (effect == null)
                {
                    hash = hash * 31;
                    continue;
                }

                hash = hash * 31 + (effect.EffectId != null ? effect.EffectId.GetHashCode() : 0);
                hash = hash * 31 + effect.Stack;
                hash = hash * 31 + effect.TurnCount;
                hash = hash * 31 + (effect.IsPassive ? 1 : 0);
                hash = hash * 31 + (effect.SourceSkillId != null ? effect.SourceSkillId.GetHashCode() : 0);
            }

            return hash;
        }
    }

    private void RefreshPortrait()
    {
        if (portraitImage == null)
            return;

        Sprite portrait = null;

        if (DataManager.Instance != null &&
            DataManager.Instance.CharacterIconDatabase != null)
        {
            DataManager.Instance.CharacterIconDatabase.TryGetPortrait(
                boundRuntime.CharacterId,
                out portrait
            );
        }

        portraitImage.sprite = portrait;
        portraitImage.enabled = portrait != null;
        portraitImage.preserveAspect = true;
    }

    private void RefreshCharacterName()
    {
        if (characterNameText == null)
            return;

        characterNameText.text = boundMaster != null &&
                                 !string.IsNullOrWhiteSpace(boundMaster.Name)
            ? boundMaster.Name
            : boundRuntime.CharacterId;
    }


    private void RefreshPassiveSkill()
    {
        string passiveSkillId = boundRuntime != null
            ? boundRuntime.PassiveSkillId
            : string.Empty;

        SkillMasterData passiveSkillData = ResolveSkillData(passiveSkillId);
        Sprite passiveIcon = ResolveSkillIcon(passiveSkillId, passiveSkillData);
        bool hasPassive = passiveSkillData != null;

        if (passiveIconImage != null)
        {
            passiveIconImage.sprite = passiveIcon;
            passiveIconImage.enabled = passiveIcon != null;
            passiveIconImage.gameObject.SetActive(passiveIcon != null);
            passiveIconImage.preserveAspect = true;
        }

        if (passiveNameText != null)
        {
            passiveNameText.text = hasPassive &&
                                   !string.IsNullOrWhiteSpace(passiveSkillData.Name)
                ? passiveSkillData.Name
                : string.Empty;
        }

        if (passiveEffectText != null)
        {
            string effectDescription = string.Empty;

            if (hasPassive)
            {
                if (!string.IsNullOrWhiteSpace(passiveSkillData.Details))
                    effectDescription = passiveSkillData.Details;
                else if (!string.IsNullOrWhiteSpace(passiveSkillData.ToolTip))
                    effectDescription = passiveSkillData.ToolTip;
                else if (!string.IsNullOrWhiteSpace(passiveSkillData.EffectDescription))
                    effectDescription = passiveSkillData.EffectDescription;
                else if (!string.IsNullOrWhiteSpace(passiveSkillData.EffectDesc))
                    effectDescription = passiveSkillData.EffectDesc;
            }

            passiveEffectText.text = effectDescription;
        }
    }

    private void ClearPassiveSkill()
    {
        if (passiveIconImage != null)
        {
            passiveIconImage.sprite = null;
            passiveIconImage.enabled = false;
            passiveIconImage.gameObject.SetActive(false);
        }

        SetText(passiveNameText, string.Empty);
        SetText(passiveEffectText, string.Empty);
    }

    private void RefreshSkillList()
    {
        RefreshSkillSlot(skill01Button, skill01IconImage, skill01NameText, GetSkillIdForDisplaySlot(0));
        RefreshSkillSlot(skill02Button, skill02IconImage, skill02NameText, GetSkillIdForDisplaySlot(1));
        RefreshSkillSlot(skill03Button, skill03IconImage, skill03NameText, GetSkillIdForDisplaySlot(2));
        RefreshSkillSlot(skill04Button, skill04IconImage, skill04NameText, GetSkillIdForDisplaySlot(3));
    }


    private void RefreshRuneList()
    {
        RefreshRuneSlot(rune01Image, GetEquippedRuneId(0));
        RefreshRuneSlot(rune02Image, GetEquippedRuneId(1));
        RefreshRuneSlot(rune03Image, GetEquippedRuneId(2));
        RefreshRuneSlot(rune04Image, GetEquippedRuneId(3));
        RefreshRuneSlot(rune05Image, GetEquippedRuneId(4));
        RefreshRuneSlot(rune06Image, GetEquippedRuneId(5));
    }

    private string GetEquippedRuneId(int slotIndex)
    {
        if (boundRuntime == null ||
            boundRuntime.EquippedRuneIds == null ||
            slotIndex < 0 ||
            slotIndex >= boundRuntime.EquippedRuneIds.Length)
        {
            return string.Empty;
        }

        return boundRuntime.EquippedRuneIds[slotIndex] ?? string.Empty;
    }

    private static void RefreshRuneSlot(Image runeImage, string runeId)
    {
        if (runeImage == null)
            return;

        Sprite runeIcon = null;
        bool hasRune = !string.IsNullOrWhiteSpace(runeId);

        if (hasRune &&
            DataManager.Instance != null &&
            DataManager.Instance.RuneIconDatabase != null)
        {
            DataManager.Instance.RuneIconDatabase.TryGetIcon(runeId, out runeIcon);
        }

        bool showImage = hasRune && runeIcon != null;
        runeImage.sprite = runeIcon;
        runeImage.enabled = showImage;
        runeImage.gameObject.SetActive(showImage);
        runeImage.preserveAspect = true;
    }

    private void ClearRuneList()
    {
        RefreshRuneSlot(rune01Image, string.Empty);
        RefreshRuneSlot(rune02Image, string.Empty);
        RefreshRuneSlot(rune03Image, string.Empty);
        RefreshRuneSlot(rune04Image, string.Empty);
        RefreshRuneSlot(rune05Image, string.Empty);
        RefreshRuneSlot(rune06Image, string.Empty);
    }

    private void RefreshPassiveRelicList()
    {
        RefreshPassiveRelicSlot(relic01Image, GetEquippedPassiveRelicId(0));
        RefreshPassiveRelicSlot(relic02Image, GetEquippedPassiveRelicId(1));
        RefreshPassiveRelicSlot(relic03Image, GetEquippedPassiveRelicId(2));
        RefreshPassiveRelicSlot(relic04Image, GetEquippedPassiveRelicId(3));
        RefreshPassiveRelicSlot(relic05Image, GetEquippedPassiveRelicId(4));
        RefreshPassiveRelicSlot(relic06Image, GetEquippedPassiveRelicId(5));
    }

    private string GetEquippedPassiveRelicId(int passiveSlotIndex)
    {
        int equippedRelicIndex = passiveSlotIndex + 1;

        if (boundRuntime == null ||
            boundRuntime.EquippedRelicIds == null ||
            passiveSlotIndex < 0 ||
            equippedRelicIndex >= boundRuntime.EquippedRelicIds.Length)
        {
            return string.Empty;
        }

        return boundRuntime.EquippedRelicIds[equippedRelicIndex] ?? string.Empty;
    }

    private static void RefreshPassiveRelicSlot(Image relicImage, string relicId)
    {
        if (relicImage == null)
            return;

        Sprite relicIcon = null;
        bool hasRelic = !string.IsNullOrWhiteSpace(relicId);

        if (hasRelic &&
            DataManager.Instance != null &&
            DataManager.Instance.RelicIconDatabase != null)
        {
            DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out relicIcon);
        }

        bool showImage = hasRelic && relicIcon != null;
        relicImage.sprite = relicIcon;
        relicImage.enabled = showImage;
        relicImage.gameObject.SetActive(showImage);
        relicImage.preserveAspect = true;
    }

    private void ClearPassiveRelicList()
    {
        RefreshPassiveRelicSlot(relic01Image, string.Empty);
        RefreshPassiveRelicSlot(relic02Image, string.Empty);
        RefreshPassiveRelicSlot(relic03Image, string.Empty);
        RefreshPassiveRelicSlot(relic04Image, string.Empty);
        RefreshPassiveRelicSlot(relic05Image, string.Empty);
        RefreshPassiveRelicSlot(relic06Image, string.Empty);
    }

    private void RefreshMoveButton()
    {
        string moveSkillId = boundRuntime != null
            ? boundRuntime.MoveSkillId
            : string.Empty;

        SkillMasterData moveSkillData = ResolveSkillData(moveSkillId);
        Sprite moveIcon = ResolveSkillIcon(moveSkillId, moveSkillData);
        bool hasMoveSkill = moveSkillData != null;

        if (moveButton != null)
        {
            moveButton.interactable = hasMoveSkill;
            moveButton.transition = Selectable.Transition.None;
        }

        if (moveIconImage != null)
        {
            moveIconImage.sprite = moveIcon;
            moveIconImage.enabled = moveIcon != null;
            moveIconImage.preserveAspect = true;
        }

        if (moveNameText != null)
        {
            moveNameText.text = hasMoveSkill
                ? (!string.IsNullOrWhiteSpace(moveSkillData.Name) ? moveSkillData.Name : moveSkillId)
                : "이동 없음";
        }

        ConfigureButtonHover(moveButton, "Background", "Background2", moveSkillData);
    }

    private void RefreshItemButton()
    {
        ActiveRelicAvailability availability = GetActiveRelicAvailability();
        bool hasRelic = availability != null && availability.RelicData != null;
        bool canUse = hasRelic && availability.CanUse && availability.RemainingUses > 0;

        if (itemButton != null)
        {
            itemButton.interactable = canUse;
            itemButton.transition = Selectable.Transition.None;
        }

        if (itemIconImage != null)
        {
            Sprite relicIcon = ResolveRelicIcon(availability?.RelicId);
            itemIconImage.sprite = relicIcon;
            itemIconImage.enabled = relicIcon != null;
            itemIconImage.preserveAspect = true;
        }

        if (itemValueText != null)
        {
            int remaining = hasRelic ? Mathf.Max(0, availability.RemainingUses) : 0;
            int maxUses = hasRelic ? Mathf.Max(0, availability.MaxUses) : 0;
            itemValueText.text = $"{remaining}/{maxUses}";
        }

        ConfigureButtonHover(itemButton, "Background", "Background2", null);
    }

    private ActiveRelicAvailability GetActiveRelicAvailability()
    {
        if (boundRuntime == null ||
            DataManager.Instance == null ||
            DataManager.Instance.RelicDatabase == null)
        {
            return null;
        }

        activeRelicService ??= new ActiveRelicService(DataManager.Instance.RelicDatabase);
        return activeRelicService.GetAvailability(boundRuntime);
    }

    private static SkillMasterData ResolveSkillData(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId) ||
            DataManager.Instance == null ||
            DataManager.Instance.SkillDatabase == null)
        {
            return null;
        }

        DataManager.Instance.SkillDatabase.TryGet(skillId, out SkillMasterData skillData);
        return skillData;
    }

    private static Sprite ResolveSkillIcon(string skillId, SkillMasterData skillData)
    {
        if (skillData != null && skillData.Icon != null)
            return skillData.Icon;

        if (string.IsNullOrWhiteSpace(skillId) ||
            DataManager.Instance == null ||
            DataManager.Instance.SkillIconDatabase == null)
        {
            return null;
        }

        return DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId, out Sprite icon)
            ? icon
            : null;
    }

    private static Sprite ResolveRelicIcon(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId) ||
            DataManager.Instance == null ||
            DataManager.Instance.RelicIconDatabase == null)
        {
            return null;
        }

        return DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out Sprite icon)
            ? icon
            : null;
    }

    private void EnsureMoveAndItemButtonHoverEffects()
    {
        ConfigureButtonHover(moveButton, "Background", "Background2", ResolveSkillData(boundRuntime?.MoveSkillId));
        ConfigureButtonHover(itemButton, "Background", "Background2", null);
    }

    private void ConfigureButtonHover(
        Button button,
        string normalBackgroundName,
        string hoverBackgroundName,
        SkillMasterData previewSkillData)
    {
        if (button == null)
            return;

        BattleCharacterSkillHoverUI hover =
            button.GetComponent<BattleCharacterSkillHoverUI>();

        if (hover == null)
            hover = button.gameObject.AddComponent<BattleCharacterSkillHoverUI>();

        Image normalBackground = FindChildImage(button.transform, normalBackgroundName);
        Image hoverBackground = FindChildImage(button.transform, hoverBackgroundName);

        hover.Configure(
            normalBackground,
            hoverBackground,
            button.GetComponent<RectTransform>(),
            previewSkillData,
            boundRuntime,
            ShowSkillInfo
        );
    }

    private static Image FindChildImage(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == childName)
                return child.GetComponent<Image>();
        }

        return null;
    }

    private void EnsureSkillButtonHoverEffects()
    {
        EnsureSkillButtonHoverEffect(skill01Button);
        EnsureSkillButtonHoverEffect(skill02Button);
        EnsureSkillButtonHoverEffect(skill03Button);
        EnsureSkillButtonHoverEffect(skill04Button);
    }

    private void EnsureSkillButtonHoverEffect(Button button)
    {
        if (button == null)
            return;

        BattleCharacterSkillHoverUI hover =
            button.GetComponent<BattleCharacterSkillHoverUI>();

        if (hover == null)
            hover = button.gameObject.AddComponent<BattleCharacterSkillHoverUI>();

        Image normalBackground = null;
        Image hoverBackground = null;

        Transform normalBackgroundTransform = button.transform.Find("Skill_Background");
        Transform hoverBackgroundTransform = button.transform.Find("Skill_Background2");

        if (normalBackgroundTransform != null)
            normalBackground = normalBackgroundTransform.GetComponent<Image>();

        if (hoverBackgroundTransform != null)
            hoverBackground = hoverBackgroundTransform.GetComponent<Image>();

        hover.Configure(
            normalBackground,
            hoverBackground,
            button.GetComponent<RectTransform>(),
            null,
            boundRuntime,
            ShowSkillInfo
        );
    }

    private void RegisterSkillButtonListeners()
    {
        if (skill01Button != null)
            skill01Button.onClick.AddListener(OnSkill01Clicked);

        if (skill02Button != null)
            skill02Button.onClick.AddListener(OnSkill02Clicked);

        if (skill03Button != null)
            skill03Button.onClick.AddListener(OnSkill03Clicked);

        if (skill04Button != null)
            skill04Button.onClick.AddListener(OnSkill04Clicked);
    }

    private void UnregisterSkillButtonListeners()
    {
        if (skill01Button != null)
            skill01Button.onClick.RemoveListener(OnSkill01Clicked);

        if (skill02Button != null)
            skill02Button.onClick.RemoveListener(OnSkill02Clicked);

        if (skill03Button != null)
            skill03Button.onClick.RemoveListener(OnSkill03Clicked);

        if (skill04Button != null)
            skill04Button.onClick.RemoveListener(OnSkill04Clicked);
    }

    private void RegisterMoveAndItemButtonListeners()
    {
        if (moveButton != null)
            moveButton.onClick.AddListener(OnMoveButtonClicked);

        if (itemButton != null)
            itemButton.onClick.AddListener(OnItemButtonClicked);
    }

    private void UnregisterMoveAndItemButtonListeners()
    {
        if (moveButton != null)
            moveButton.onClick.RemoveListener(OnMoveButtonClicked);

        if (itemButton != null)
            itemButton.onClick.RemoveListener(OnItemButtonClicked);
    }

    private void OnMoveButtonClicked()
    {
        SelectSkillDirectly(boundRuntime != null ? boundRuntime.MoveSkillId : string.Empty);
    }

    private void OnItemButtonClicked()
    {
        UseActiveRelicDirectly();
    }

    private void OnSkill01Clicked() => UseSkillSlot(0);
    private void OnSkill02Clicked() => UseSkillSlot(1);
    private void OnSkill03Clicked() => UseSkillSlot(2);
    private void OnSkill04Clicked() => UseSkillSlot(3);

    private void UseSkillSlot(int displaySlotIndex)
    {
        SelectSkillDirectly(GetSkillIdForDisplaySlot(displaySlotIndex));
    }

    private void SelectSkillDirectly(string skillId)
    {
        if (boundRuntime == null)
        {
            ShowBattleWarning("선택된 캐릭터가 없습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(skillId))
        {
            ShowBattleWarning("등록된 스킬이 없습니다.");
            return;
        }

        if (DataManager.Instance == null || DataManager.Instance.SkillDatabase == null ||
            !DataManager.Instance.SkillDatabase.TryGet(skillId, out SkillMasterData skillData) ||
            skillData == null)
        {
            ShowBattleWarning("스킬 데이터를 찾을 수 없습니다.");
            return;
        }

        EnsureBattleTimelineController();
        if (battleTimelineController == null)
        {
            ShowBattleWarning("타임라인 컨트롤러를 찾을 수 없습니다.");
            return;
        }

        battleTimelineController.SelectCharacter(boundRuntime);
        battleTimelineController.SelectSkill(skillData);
    }

    private void UseActiveRelicDirectly()
    {
        if (boundRuntime == null)
        {
            ShowBattleWarning("선택된 캐릭터가 없습니다.");
            return;
        }

        EnsureTurnExecutor();
        if (turnExecutor != null && !turnExecutor.CanAcceptPlayerInput)
        {
            ShowBattleWarning("지금은 유물을 사용할 수 없습니다.");
            return;
        }

        ActiveRelicAvailability availability = GetActiveRelicAvailability();
        if (availability == null || !availability.CanUse)
        {
            ShowBattleWarning(availability != null ? availability.Message : "유물을 사용할 수 없습니다.");
            RefreshItemButton();
            return;
        }

        EnsureBattleTimelineController();
        battleTimelineController?.CancelSkillReservationPreviewFromSkillList(boundRuntime);

        if (!availability.RequiresTarget)
        {
            ActiveRelicUseResult result = activeRelicService.TryUseImmediate(boundRuntime);
            if (!result.Succeeded)
                ShowBattleWarning(result.Message);

            Refresh();
            return;
        }

        EnsureActiveRelicTargetingController();
        if (activeRelicTargetingController == null ||
            !activeRelicTargetingController.BeginTargeting(
                activeRelicService,
                boundRuntime,
                availability,
                Refresh))
        {
            ShowBattleWarning("대상 선택을 시작할 수 없습니다.");
        }
    }

    private void EnsureBattleTimelineController()
    {
        if (battleTimelineController == null)
            battleTimelineController = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
    }

    private void EnsureTurnExecutor()
    {
        if (turnExecutor == null)
            turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
    }

    private void EnsureActiveRelicTargetingController()
    {
        if (activeRelicTargetingController == null)
            activeRelicTargetingController = FindFirstObjectByType<ActiveRelicTargetingController>(FindObjectsInactive.Include);

        if (activeRelicTargetingController == null)
            activeRelicTargetingController = gameObject.AddComponent<ActiveRelicTargetingController>();
    }

    private static void ShowBattleWarning(string message)
    {
        BattleWarningUI.ShowMessage(message);
    }

    private string GetSkillIdForDisplaySlot(int displaySlotIndex)
    {
        if (boundRuntime == null)
            return string.Empty;

        switch (displaySlotIndex)
        {
            case 0:
                return boundRuntime.AbilitySkillId ?? string.Empty;

            case 1:
                return GetEquippedSkillId(2);

            case 2:
                return GetEquippedSkillId(3);

            case 3:
                return boundRuntime.UniqueSkillId ?? string.Empty;

            default:
                return string.Empty;
        }
    }

    private string GetEquippedSkillId(int equippedIndex)
    {
        if (boundRuntime == null ||
            boundRuntime.EquippedSkillIds == null ||
            equippedIndex < 0 ||
            equippedIndex >= boundRuntime.EquippedSkillIds.Length)
        {
            return string.Empty;
        }

        return boundRuntime.EquippedSkillIds[equippedIndex] ?? string.Empty;
    }

    private void RefreshSkillSlot(
        Button button,
        Image iconImage,
        TMP_Text nameText,
        string skillId)
    {
        string normalizedSkillId = string.IsNullOrWhiteSpace(skillId)
            ? string.Empty
            : skillId.Trim();

        SkillMasterData skillData = null;
        Sprite skillIcon = null;

        if (!string.IsNullOrEmpty(normalizedSkillId) &&
            DataManager.Instance != null)
        {
            if (DataManager.Instance.SkillDatabase != null)
            {
                DataManager.Instance.SkillDatabase.TryGet(
                    normalizedSkillId,
                    out skillData
                );
            }

            if (DataManager.Instance.SkillIconDatabase != null)
            {
                DataManager.Instance.SkillIconDatabase.TryGetIcon(
                    normalizedSkillId,
                    out skillIcon
                );
            }
        }

        bool hasSkill = skillData != null;

        if (button != null)
        {
            BattleCharacterSkillHoverUI hover =
                button.GetComponent<BattleCharacterSkillHoverUI>();

            if (hover != null)
            {
                hover.SetSkillRangePreview(skillData);
                hover.SetPreviewCharacter(boundRuntime);
                hover.SetSkillInfoHandler(ShowSkillInfo);
            }
        }

        if (button != null)
        {
            // Button Transition이 Skill_Background를 숨기지 않도록
            // 스킬 슬롯의 시각 효과는 전용 호버 스크립트에서만 처리합니다.
            button.transition = Selectable.Transition.None;
            button.interactable = hasSkill;
            SetSkillBackgroundAlwaysVisible(button);
        }

        if (iconImage != null)
        {
            iconImage.sprite = skillIcon;
            iconImage.enabled = skillIcon != null;
            iconImage.gameObject.SetActive(skillIcon != null);
            iconImage.preserveAspect = true;
        }

        if (nameText != null)
        {
            nameText.text = hasSkill && !string.IsNullOrWhiteSpace(skillData.Name)
                ? skillData.Name
                : emptySkillName;
            nameText.color = hasSkill ? skillNameColor : emptySkillNameColor;
        }
    }

    private static void SetSkillBackgroundAlwaysVisible(Button button)
    {
        if (button == null)
            return;

        Transform backgroundTransform = button.transform.Find("Skill_Background");
        if (backgroundTransform == null)
            return;

        backgroundTransform.gameObject.SetActive(true);

        Image backgroundImage = backgroundTransform.GetComponent<Image>();
        if (backgroundImage != null)
            backgroundImage.enabled = true;
    }

    private void ClearSkillList()
    {
        RefreshSkillSlot(skill01Button, skill01IconImage, skill01NameText, string.Empty);
        RefreshSkillSlot(skill02Button, skill02IconImage, skill02NameText, string.Empty);
        RefreshSkillSlot(skill03Button, skill03IconImage, skill03NameText, string.Empty);
        RefreshSkillSlot(skill04Button, skill04IconImage, skill04NameText, string.Empty);
    }

    private void ClearMoveAndItemButtons()
    {
        if (moveButton != null)
            moveButton.interactable = false;

        if (moveIconImage != null)
        {
            moveIconImage.sprite = null;
            moveIconImage.enabled = false;
        }

        if (moveNameText != null)
            moveNameText.text = "이동 없음";

        if (itemButton != null)
            itemButton.interactable = false;

        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
        }

        if (itemValueText != null)
            itemValueText.text = "0/0";
    }

    private void ShowSkillInfo(SkillMasterData skillData)
    {
        if (skillData == null)
            return;

        SetSkillInfoImage(skillInfoIconImage, ResolveSkillIcon(skillData.SkillId, skillData));
        SetSkillInfoImage(skillInfoRangeImage, ResolveSkillRangeIcon(skillData.RangeId));

        if (skillInfoNameText != null)
        {
            skillInfoNameText.text = !string.IsNullOrWhiteSpace(skillData.Name)
                ? skillData.Name
                : skillData.SkillId;
        }

        RefreshSkillInfoCost(skillData);

        if (skillInfoTypeText != null)
            skillInfoTypeText.text = GetSkillTypeDisplayName(skillData.RangeType);

        if (skillInfoDetailsText != null)
        {
            skillInfoDetailsText.text = !string.IsNullOrWhiteSpace(skillData.Details)
                ? skillData.Details
                : !string.IsNullOrWhiteSpace(skillData.ToolTip)
                    ? skillData.ToolTip
                    : string.Empty;
        }

        RefreshSkillInfoEffects(skillData);
    }

    private void RefreshSkillInfoCost(SkillMasterData skillData)
    {
        if (skillData == null)
            return;

        if (skillInfoCostNameText != null)
            skillInfoCostNameText.text = GetResourceDisplayName(skillData.ReferenceResource);

        if (skillInfoCostValueText != null)
            skillInfoCostValueText.text = Mathf.Max(0, skillData.ResourceCostValue).ToString();

        SetSkillInfoImage(
            skillInfoCostIconImage,
            GetResourceIcon(skillData.ReferenceResource)
        );
    }

    private void RefreshSkillInfoEffects(SkillMasterData skillData)
    {
        List<SkillEffectEntry> entries = skillData.EffectEntries;

        if ((entries == null || entries.Count == 0) && DataManager.Instance != null)
        {
            entries = SkillEffectParser.Parse(
                skillData,
                DataManager.Instance.EffectDatabase
            );
        }

        SetSkillEffectSlot(0, entries != null && entries.Count > 0 ? entries[0] : null);
        SetSkillEffectSlot(1, entries != null && entries.Count > 1 ? entries[1] : null);
        SetSkillEffectSlot(2, entries != null && entries.Count > 2 ? entries[2] : null);
    }

    private void SetSkillEffectSlot(int index, SkillEffectEntry entry)
    {
        GameObject root;
        TMP_Text effectText;
        TMP_Text effectValue;

        switch (index)
        {
            case 0:
                root = skillEffect01;
                effectText = skillEffect01Text;
                effectValue = skillEffect01Value;
                break;

            case 1:
                root = skillEffect02;
                effectText = skillEffect02Text;
                effectValue = skillEffect02Value;
                break;

            default:
                root = skillEffect03;
                effectText = skillEffect03Text;
                effectValue = skillEffect03Value;
                break;
        }

        bool visible = entry != null && !string.IsNullOrWhiteSpace(entry.EffectId);

        if (root != null)
            root.SetActive(visible);

        if (!visible)
            return;

        if (effectText != null)
            effectText.text = GetEffectDisplayName(entry);

        if (effectValue != null)
            effectValue.text = GetEffectDisplayValue(entry).ToString();
    }

    private static int GetEffectDisplayValue(SkillEffectEntry entry)
    {
        if (entry == null)
            return 0;

        return entry.ValueAmount != 0
            ? entry.ValueAmount
            : entry.CountAmount;
    }

    private static string GetEffectDisplayName(SkillEffectEntry entry)
    {
        if (entry == null)
            return string.Empty;

        string effectName = entry.EffectData != null &&
                            !string.IsNullOrWhiteSpace(entry.EffectData.Name)
            ? entry.EffectData.Name
            : entry.EffectId;

        string normalized = effectName.Replace(" ", string.Empty).ToLowerInvariant();

        if (normalized.Contains("타격") || normalized.Contains("strike"))
            return "피해";

        return effectName;
    }

    private static string GetSkillTypeDisplayName(RangeType rangeType)
    {
        switch (rangeType)
        {
            case RangeType.Selection:
                return "그리드 선택";

            case RangeType.Direction:
                return "시전자 위치";

            default:
                return string.Empty;
        }
    }

    private string GetResourceDisplayName(ReferenceResource resource)
    {
        switch (resource)
        {
            case ReferenceResource.HP:
                return "체력";

            case ReferenceResource.UniqueResource:
                return boundMaster != null
                    ? GetUniqueResourceDisplayName(boundMaster.ResourceType)
                    : "고유자원";

            case ReferenceResource.MovePoint:
                return "이동";

            case ReferenceResource.Cost:
            default:
                return "코스트";
        }
    }

    private static string GetUniqueResourceDisplayName(ResourceType resourceType)
    {
        switch (resourceType)
        {
            case ResourceType.Rage: return "분노";
            case ResourceType.Momentum: return "기세";
            case ResourceType.Aether: return "에테르";
            case ResourceType.Faith: return "신앙";
            case ResourceType.Blood: return "혈기";
            default: return "고유자원";
        }
    }

    private Sprite GetResourceIcon(ReferenceResource resource)
    {
        switch (resource)
        {
            case ReferenceResource.HP:
                return hpResourceIcon;

            case ReferenceResource.UniqueResource:
                return uniqueResourceIcon;

            case ReferenceResource.MovePoint:
                return moveResourceIcon;

            case ReferenceResource.Cost:
            default:
                return costResourceIcon;
        }
    }

    private static Sprite ResolveSkillRangeIcon(string rangeId)
    {
        if (string.IsNullOrWhiteSpace(rangeId) ||
            DataManager.Instance == null ||
            DataManager.Instance.SkillRangeIconDatabase == null)
        {
            return null;
        }

        return DataManager.Instance.SkillRangeIconDatabase.TryGetIcon(rangeId, out Sprite icon)
            ? icon
            : null;
    }

    private static void SetSkillInfoImage(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
        image.gameObject.SetActive(sprite != null);
        image.preserveAspect = true;
    }

    private void RefreshAnimatedStats(
        int targetHp,
        int maxHp,
        int targetCost,
        int maxCost,
        int targetArmor,
        int targetRecovery,
        int targetResource,
        int maxResource)
    {
        if (!hasDisplayedStats)
        {
            StopNumberChangeCoroutine();
            displayedHp = targetHp;
            displayedCost = targetCost;
            displayedArmor = targetArmor;
            displayedRecovery = targetRecovery;
            displayedResource = targetResource;
            hasDisplayedStats = true;
            ApplyDisplayedStats(maxHp, maxCost, maxResource);
            return;
        }

        bool unchanged = displayedHp == targetHp &&
                         displayedCost == targetCost &&
                         displayedArmor == targetArmor &&
                         displayedRecovery == targetRecovery &&
                         displayedResource == targetResource;

        if (unchanged)
        {
            StopNumberChangeCoroutine();
            ApplyDisplayedStats(maxHp, maxCost, maxResource);
            return;
        }

        StopNumberChangeCoroutine();
        numberChangeCoroutine = StartCoroutine(
            AnimateNumberChanges(
                targetHp,
                maxHp,
                targetCost,
                maxCost,
                targetArmor,
                targetRecovery,
                targetResource,
                maxResource
            )
        );
    }

    private IEnumerator AnimateNumberChanges(
        int targetHp,
        int maxHp,
        int targetCost,
        int maxCost,
        int targetArmor,
        int targetRecovery,
        int targetResource,
        int maxResource)
    {
        int startHp = displayedHp;
        int startCost = displayedCost;
        int startArmor = displayedArmor;
        int startRecovery = displayedRecovery;
        int startResource = displayedResource;

        if (numberChangeDuration <= 0f)
        {
            displayedHp = targetHp;
            displayedCost = targetCost;
            displayedArmor = targetArmor;
            displayedRecovery = targetRecovery;
            displayedResource = targetResource;
            ApplyDisplayedStats(maxHp, maxCost, maxResource);
            numberChangeCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < numberChangeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / numberChangeDuration);

            displayedHp = Mathf.RoundToInt(Mathf.Lerp(startHp, targetHp, progress));
            displayedCost = Mathf.RoundToInt(Mathf.Lerp(startCost, targetCost, progress));
            displayedArmor = Mathf.RoundToInt(Mathf.Lerp(startArmor, targetArmor, progress));
            displayedRecovery = Mathf.RoundToInt(Mathf.Lerp(startRecovery, targetRecovery, progress));
            displayedResource = Mathf.RoundToInt(Mathf.Lerp(startResource, targetResource, progress));

            ApplyDisplayedStats(maxHp, maxCost, maxResource);
            yield return null;
        }

        displayedHp = targetHp;
        displayedCost = targetCost;
        displayedArmor = targetArmor;
        displayedRecovery = targetRecovery;
        displayedResource = targetResource;
        ApplyDisplayedStats(maxHp, maxCost, maxResource);
        numberChangeCoroutine = null;
    }

    private void ApplyDisplayedStats(int maxHp, int maxCost, int maxResource)
    {
        RefreshCurrentAndMaxText(hpValueText, displayedHp, maxHp);
        RefreshCurrentAndMaxText(costValueText, displayedCost, maxCost);

        if (armorValueText != null)
            armorValueText.text = Mathf.Max(0, displayedArmor).ToString();

        if (recoveryValueText != null)
            recoveryValueText.text = Mathf.Max(0, displayedRecovery).ToString();

        RefreshUniqueResource(maxResource, displayedResource);
    }

    private void StopNumberChangeCoroutine()
    {
        if (numberChangeCoroutine == null)
            return;

        StopCoroutine(numberChangeCoroutine);
        numberChangeCoroutine = null;
    }

    private void RefreshUniqueResource(int maxResource, int currentResource)
    {
        GameObject[] slots = GetResourceSlots();
        currentResource = Mathf.Clamp(
            currentResource,
            0,
            Mathf.Max(0, maxResource)
        );

        bool useThreeSlotLayout = maxResource <= 3;

        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slot = slots[i];
            if (slot == null)
                continue;

            bool slotVisible = !useThreeSlotLayout || (i >= 1 && i <= 3);
            slot.SetActive(slotVisible);

            if (!slotVisible)
                continue;

            int visibleSlotIndex = useThreeSlotLayout ? i - 1 : i;
            SetChildImageEnabled(slot, visibleSlotIndex < currentResource);
        }
    }

    private GameObject[] GetResourceSlots()
    {
        return new[]
        {
            resource01,
            resource02,
            resource03,
            resource04,
            resource05
        };
    }

    private static void SetChildImageEnabled(GameObject slot, bool enabled)
    {
        if (slot == null)
            return;

        Transform imageTransform = slot.transform.Find("Image");
        if (imageTransform == null)
            return;

        imageTransform.gameObject.SetActive(enabled);
    }


    private void RefreshStatusEffects()
    {
        ClearStatusEffectIcons();
        ConfigureStatusEffectLayout();

        if (statusEffectListRoot == null ||
            statusEffectIconPrefab == null ||
            boundRuntime == null ||
            boundRuntime.StatusEffects == null)
        {
            return;
        }

        for (int i = 0; i < boundRuntime.StatusEffects.Count; i++)
        {
            StatusEffectRuntimeData statusEffect = boundRuntime.StatusEffects[i];
            if (statusEffect == null || !statusEffect.IsValid())
                continue;

            StatusEffectIcon icon = Instantiate(
                statusEffectIconPrefab,
                statusEffectListRoot
            );

            icon.Set(statusEffect);
            spawnedStatusEffectIcons.Add(icon);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(statusEffectListRoot);
    }

    private void ConfigureStatusEffectLayout()
    {
        if (statusEffectListRoot == null)
            return;

        GridLayoutGroup grid = statusEffectListRoot.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = statusEffectListRoot.gameObject.AddComponent<GridLayoutGroup>();

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, statusEffectColumnCount);
        grid.cellSize = statusEffectCellSize;
        grid.spacing = statusEffectSpacing;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = statusEffectListRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = statusEffectListRoot.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void ClearStatusEffectIcons()
    {
        for (int i = spawnedStatusEffectIcons.Count - 1; i >= 0; i--)
        {
            StatusEffectIcon icon = spawnedStatusEffectIcons[i];
            if (icon != null)
                Destroy(icon.gameObject);
        }

        spawnedStatusEffectIcons.Clear();

        if (statusEffectListRoot == null)
            return;

        for (int i = statusEffectListRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = statusEffectListRoot.GetChild(i);
            if (child != null && child.GetComponent<StatusEffectIcon>() != null)
                Destroy(child.gameObject);
        }
    }


    private int ResolveRecovery()
    {
        if (boundRuntime == null)
            return 0;

        int recovery = BattleEquipmentEffectService.GetEffectiveCostRecovery(
            boundRuntime,
            boundMaster
        );

        return Mathf.Max(0, recovery);
    }

    private int ResolveMaxHp()
    {
        if (boundRuntime.MaxHP > 0)
            return boundRuntime.MaxHP;

        if (boundMaster != null && boundMaster.MaxHP > 0)
            return boundMaster.MaxHP;

        return Mathf.Max(1, boundRuntime.CurrentHP);
    }

    private int ResolveMaxCost()
    {
        if (boundRuntime.MaxCost > 0)
            return Mathf.Max(boundRuntime.MaxCost, boundRuntime.PreviewCost);

        if (boundMaster != null && boundMaster.MaxCost > 0)
            return boundMaster.MaxCost;

        return Mathf.Max(1, boundRuntime.CurrentCost);
    }

    private int ResolveMaxResource()
    {
        if (boundMaster != null)
            return Mathf.Max(0, boundMaster.MaxResource);

        return Mathf.Max(0, boundRuntime.CurrentResource);
    }

    private static void RefreshCurrentAndMaxText(
        TMP_Text valueText,
        int current,
        int max)
    {
        if (valueText == null)
            return;

        max = Mathf.Max(0, max);
        current = Mathf.Clamp(current, 0, max);
        valueText.text = $"{current}/{max}";
    }

    private void Clear()
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }

        SetText(characterNameText, string.Empty);
        ClearPassiveSkill();
        SetText(hpValueText, string.Empty);
        SetText(costValueText, string.Empty);
        SetText(armorValueText, string.Empty);
        SetText(recoveryValueText, string.Empty);
        ClearSkillList();
        ClearRuneList();
        ClearPassiveRelicList();
        ClearMoveAndItemButtons();

        SetStatVisualActive(hpIconImage, hpValueText, false);
        SetStatVisualActive(costIconImage, costValueText, false);
        SetStatVisualActive(armorIconImage, armorValueText, false);
        SetStatVisualActive(recoveryIconImage, recoveryValueText, false);
        ClearStatusEffectIcons();
        StopNumberChangeCoroutine();
        hasDisplayedStats = false;
        ResetRuntimeDisplayState();

        GameObject[] slots = GetResourceSlots();
        foreach (GameObject slot in slots)
        {
            if (slot == null)
                continue;

            slot.SetActive(false);
            SetChildImageEnabled(slot, false);
        }
    }

    private static void SetStatVisualActive(
        Image iconImage,
        TMP_Text valueText,
        bool active)
    {
        if (iconImage != null)
            iconImage.gameObject.SetActive(active);

        if (valueText != null)
            valueText.gameObject.SetActive(active);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}
