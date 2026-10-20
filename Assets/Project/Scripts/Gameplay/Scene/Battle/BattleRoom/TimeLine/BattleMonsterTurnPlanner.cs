using System.Collections;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.Collections.Generic;
using System.Linq;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleMonsterTurnPlanner : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private BattleTimelineController timelineController;

    [Header("Grid")]
    [SerializeField] private GridManager gridManager;

    [Header("Nocturn Portal Preview")]
    [Tooltip("같은 BattleReservationSystem 오브젝트의 예약 컨트롤러를 사용합니다. 비어 있으면 자동으로 찾습니다.")]
    [SerializeField] private PlayerSkillReservationController playerSkillReservationController;

    [Header("Intro Text")]
    [SerializeField] private BattleMapIntroText battleMapIntroText;
    [SerializeField] private float firstMonsterCommandDelay = 0.15f;
    [SerializeField] private float monsterCommandInterval = 0.18f;
    [SerializeField] private float actionReserveMessageDelay = 0.1f;

    [Header("Action Reservation Start SFX")]
    [Tooltip("'행동 예약' 인트로 텍스트가 표시되기 시작할 때 SFX를 재생합니다.")]
    [SerializeField] private bool playActionReservationStartSfx = true;

    [Tooltip("행동 예약 인트로 시작 시 재생할 SFX입니다.")]
    [SerializeField, SoundId(SoundCategory.Sfx)] private string actionReservationStartSfxId;

    [Tooltip("행동 예약 시작 SFX 볼륨 배율입니다.")]
    [SerializeField, Range(0f, 1f)] private float actionReservationStartSfxVolume = 1f;

    [Header("Reservation Performance Diagnostics")]
    [SerializeField] private bool logReservationPerformance = false;
    [SerializeField, Min(0f)] private float slowMonsterThresholdMs = 1f;

    private Coroutine planRoutine;
    private Coroutine battleStartTextRoutine;
    private bool battleStartIntroShown;
    private int plannedBattleTurn;

    public void PlanMonsterTurns(List<MonsterUnit> monsterUnits)
    {
        PlanMonsterTurns(monsterUnits, false);
    }

    public void PlanMonsterTurns(List<MonsterUnit> monsterUnits, bool showBattleStart)
    {
        bool shouldShowBattleStart = showBattleStart && !battleStartIntroShown;

        if (shouldShowBattleStart)
            battleStartIntroShown = true;

        if (!isActiveAndEnabled)
        {
            StopPlanRoutineOnly();

            if (shouldShowBattleStart)
                StopBattleStartTextRoutine(false);

            PlanMonsterTurnsImmediate(monsterUnits, shouldShowBattleStart, true);
            return;
        }

        StopPlanRoutineOnly();

        if (shouldShowBattleStart)
            StopBattleStartTextRoutine(false);

        planRoutine = StartCoroutine(PlanMonsterTurnsRoutine(monsterUnits, shouldShowBattleStart));
    }

    public IEnumerator PlanMonsterTurnsAndWait(List<MonsterUnit> monsterUnits, bool showBattleStart = false)
    {
        PlanMonsterTurns(monsterUnits, showBattleStart);

        while (planRoutine != null)
            yield return null;
    }


    public IEnumerator RestoreMonsterTurnsAndWait(
        List<MonsterUnit> monsterUnits,
        IReadOnlyList<BattleRoomMonsterCommandSaveData> savedCommands,
        bool showBattleStart = false)
    {
        if (timelineController == null)
        {
            Debug.LogWarning("[BattleMonsterTurnPlanner] BattleTimelineController가 없습니다.");
            yield break;
        }

        ClearNocturnPortalDestinationIndicators();
        timelineController.ClearMonsterCommands();
        ApplyElisePlayerSlotLock(monsterUnits);
        plannedBattleTurn = Mathf.Max(plannedBattleTurn, 1);

        if (showBattleStart)
        {
            BattleMapIntroText.PlayRoomIntroSfx();
            battleStartTextRoutine = StartCoroutine(ShowIntroTextAndWaitRoutine(GameLocalization.Get(LocalizationKeys.Battle.Start)));
        }

        if (firstMonsterCommandDelay > 0f)
            yield return new WaitForSeconds(firstMonsterCommandDelay);

        int restoredCount = 0;
        if (savedCommands != null)
        {
            for (int i = 0; i < savedCommands.Count; i++)
            {
                BattleRoomMonsterCommandSaveData saved = savedCommands[i];
                if (saved == null ||
                    saved.SlotIndex < 0 || saved.SlotIndex >= timelineController.SlotCount ||
                    string.IsNullOrWhiteSpace(saved.SkillId))
                {
                    continue;
                }

                MonsterRuntimeData runtime = ResolveSavedMonsterRuntime(monsterUnits, saved);
                if (runtime == null)
                    continue;

                MonsterSkillData skillData = DataManager.Instance?.MonsterSkillDatabase?.Get(saved.SkillId);
                if (skillData == null)
                    continue;

                MonsterReservedCommand command = new MonsterReservedCommand(runtime, skillData);
                command.SetMoveOffset(new Vector2Int(saved.MoveX, saved.MoveY));
                command.SetActionIndex(saved.ActionIndex);
                command.SetRangeOriginGridIndex(saved.RangeOriginGridIndex);
                command.SetRangeOriginCasterGridIndex(saved.RangeOriginCasterGridIndex);
                command.SetPortalMove(saved.IsPortalMove);
                command.SetUseRequestedMoveOffsetForExecution(saved.UseRequestedMoveOffsetForExecution);

                if (saved.ReservedDamage > 0)
                    command.SetReservedDamage(saved.ReservedDamage);

                if (saved.HasForcedDirection)
                    command.SetForcedDirection((BattleDirection)saved.ForcedDirection);
                else
                    command.ClearForcedDirection();

                if (saved.HasExplicitRangeResult)
                    command.SetExplicitRangeResult(saved.RangeGridIndices, saved.TargetGridIndices);
                else
                    command.SetRangeResult(saved.RangeGridIndices, saved.TargetGridIndices);

                if (saved.HasSimulatedResult)
                {
                    command.SetSimulatedMoveResult(
                        saved.IsSimulatedMoveBlocked,
                        new Vector2Int(saved.SimulatedMoveX, saved.SimulatedMoveY));
                }

                timelineController.AddMonsterCommand(saved.SlotIndex, command);
                // 포털은 턴 시작 행동 애니메이션과 동시에 표시합니다.
                restoredCount++;

                if (monsterCommandInterval > 0f && i < savedCommands.Count - 1)
                    yield return new WaitForSeconds(monsterCommandInterval);
            }
        }

        if (battleStartTextRoutine != null)
        {
            yield return battleStartTextRoutine;
            battleStartTextRoutine = null;
        }

        if (actionReserveMessageDelay > 0f)
            yield return new WaitForSeconds(actionReserveMessageDelay);

        if (showBattleStart)
            yield return ShowActionReserveIntroTextAndWaitRoutine();
        else
            ShowActionReserveIntroText();

        Debug.Log($"[BattleMonsterTurnPlanner] 저장된 1턴 몬스터 예약 {restoredCount}개를 복원했습니다.", this);
    }

    private static MonsterRuntimeData ResolveSavedMonsterRuntime(
        List<MonsterUnit> monsterUnits,
        BattleRoomMonsterCommandSaveData saved)
    {
        if (monsterUnits == null || saved == null)
            return null;

        // RuntimeId는 전투방을 다시 만들 때 새로 발급됩니다.
        // 동일 MonsterId가 여러 마리여도 바뀌지 않도록 저장 당시 생성 순번을 가장 먼저 사용합니다.
        if (saved.MonsterSpawnOrder >= 0 && saved.MonsterSpawnOrder < monsterUnits.Count)
        {
            MonsterUnit orderedUnit = monsterUnits[saved.MonsterSpawnOrder];
            MonsterRuntimeData orderedRuntime = orderedUnit != null ? orderedUnit.RuntimeData : null;
            if (orderedRuntime != null &&
                (string.IsNullOrWhiteSpace(saved.MonsterId) ||
                 string.Equals(orderedRuntime.MonsterId, saved.MonsterId, System.StringComparison.OrdinalIgnoreCase)))
            {
                return orderedRuntime;
            }
        }

        // 저장 순번이 없는 이전 데이터는 MonsterId + 시작 그리드 위치로 찾습니다.
        for (int i = 0; i < monsterUnits.Count; i++)
        {
            MonsterUnit unit = monsterUnits[i];
            MonsterRuntimeData runtime = unit != null ? unit.RuntimeData : null;
            if (runtime == null)
                continue;

            if (!string.IsNullOrWhiteSpace(saved.MonsterId) &&
                string.Equals(runtime.MonsterId, saved.MonsterId, System.StringComparison.OrdinalIgnoreCase) &&
                unit.MainGridIndex == saved.MonsterGridIndex)
            {
                return runtime;
            }
        }

        // 마지막으로 RuntimeId가 우연히 유지되는 경우만 보조 경로로 사용합니다.
        for (int i = 0; i < monsterUnits.Count; i++)
        {
            MonsterRuntimeData runtime = monsterUnits[i] != null ? monsterUnits[i].RuntimeData : null;
            if (runtime != null && runtime.RuntimeId == saved.RuntimeId)
                return runtime;
        }

        return null;
    }

    public void ResetBattleStartIntroState()
    {
        battleStartIntroShown = false;
        plannedBattleTurn = 0;
    }

    private void OnDisable()
    {
        StopPlanRoutines(true);
    }

    private void StopPlanRoutines(bool hideIntroText)
    {
        StopPlanRoutineOnly();
        StopBattleStartTextRoutine(hideIntroText);
    }

    private void StopPlanRoutineOnly()
    {
        if (planRoutine == null)
            return;

        StopCoroutine(planRoutine);
        planRoutine = null;
    }

    private void StopBattleStartTextRoutine(bool hideIntroText)
    {
        if (battleStartTextRoutine != null)
        {
            StopCoroutine(battleStartTextRoutine);
            battleStartTextRoutine = null;
        }

        if (hideIntroText)
            StopIntroText();
    }

    private IEnumerator PlanMonsterTurnsRoutine(List<MonsterUnit> monsterUnits, bool showBattleStart)
    {
        if (timelineController == null)
        {
            Debug.LogWarning("[BattleMonsterTurnPlanner] BattleTimelineController가 없습니다.");
            planRoutine = null;
            yield break;
        }

        ClearNocturnPortalDestinationIndicators();
        timelineController.ClearMonsterCommands();
        ApplyElisePlayerSlotLock(monsterUnits);

        if (showBattleStart)
        {
            BattleMapIntroText.PlayRoomIntroSfx();
            battleStartTextRoutine = StartCoroutine(ShowIntroTextAndWaitRoutine(GameLocalization.Get(LocalizationKeys.Battle.Start)));
        }

        if (firstMonsterCommandDelay > 0f)
            yield return new WaitForSeconds(firstMonsterCommandDelay);

        Stopwatch buildWatch = logReservationPerformance ? Stopwatch.StartNew() : null;
        List<MonsterReservedCommandPlan> plans = BuildMonsterCommandPlans(monsterUnits);
        if (buildWatch != null)
        {
            buildWatch.Stop();
            UnityEngine.Debug.Log($"[MonsterReservationPerf] Turn:{plannedBattleTurn} BuildAll:{buildWatch.Elapsed.TotalMilliseconds:F2}ms Commands:{plans.Count}", this);
        }

        double registerTotalMs = 0d;
        double registerMaxMs = 0d;
        int registeredCount = 0;
        for (int i = 0; i < plans.Count; i++)
        {
            if (plans[i].SlotIndex < 0 || plans[i].SlotIndex >= timelineController.SlotCount)
                continue;

            Stopwatch registerWatch = logReservationPerformance ? Stopwatch.StartNew() : null;
            timelineController.AddMonsterCommand(plans[i].SlotIndex, plans[i].Command);
            // 포털은 턴 시작 행동 애니메이션과 동시에 표시합니다.
            if (registerWatch != null)
            {
                registerWatch.Stop();
                double elapsed = registerWatch.Elapsed.TotalMilliseconds;
                registerTotalMs += elapsed;
                registerMaxMs = System.Math.Max(registerMaxMs, elapsed);
                registeredCount++;
                if (elapsed >= slowMonsterThresholdMs)
                    UnityEngine.Debug.Log($"[MonsterReservationPerf] Register Slot:{plans[i].SlotIndex + 1} Skill:{plans[i].Command.SkillData?.SkillId} Time:{elapsed:F2}ms", this);
            }

            if (monsterCommandInterval > 0f && i < plans.Count - 1)
                yield return new WaitForSeconds(monsterCommandInterval);
        }

        if (logReservationPerformance)
            UnityEngine.Debug.Log($"[MonsterReservationPerf] RegisterAll:{registerTotalMs:F2}ms MaxSingle:{registerMaxMs:F2}ms Count:{registeredCount} Interval:{monsterCommandInterval:F2}s (intentional)", this);

        if (battleStartTextRoutine != null)
        {
            yield return battleStartTextRoutine;
            battleStartTextRoutine = null;
        }

        if (actionReserveMessageDelay > 0f)
            yield return new WaitForSeconds(actionReserveMessageDelay);

        // 전투 시작 안내만 기다리고 행동 예약 문구는 표시하지 않습니다.
        // 턴 시작 문구는 BattleTurnExecutor가 예약 입력 가능 시점에 표시합니다.
        if (showBattleStart)
            yield return ShowActionReserveIntroTextAndWaitRoutine();
        else
            ShowActionReserveIntroText();

        planRoutine = null;
    }

    private void PlanMonsterTurnsImmediate(
        List<MonsterUnit> monsterUnits,
        bool showBattleStart,
        bool showActionReserve)
    {
        if (timelineController == null)
        {
            Debug.LogWarning("[BattleMonsterTurnPlanner] BattleTimelineController가 없습니다.");
            return;
        }

        ClearNocturnPortalDestinationIndicators();
        timelineController.ClearMonsterCommands();
        ApplyElisePlayerSlotLock(monsterUnits);

        if (showBattleStart)
        {
            BattleMapIntroText.PlayRoomIntroSfx();
            ShowIntroText(GameLocalization.Get(LocalizationKeys.Battle.Start));
        }

        List<MonsterReservedCommandPlan> plans = BuildMonsterCommandPlans(monsterUnits);

        for (int i = 0; i < plans.Count; i++)
        {
            if (plans[i].SlotIndex < 0 || plans[i].SlotIndex >= timelineController.SlotCount)
                continue;

            timelineController.AddMonsterCommand(plans[i].SlotIndex, plans[i].Command);
            // 포털은 턴 시작 행동 애니메이션과 동시에 표시합니다.
        }

        if (showActionReserve)
            ShowActionReserveIntroText();
    }


    /// <summary>
    /// 녹턴과 모르트의 첫 예약 행동을 기준으로 턴 시작 시 바라보는 방향을 설정합니다.
    /// 공격은 예약 방향, 순간이동은 포털 미리보기의 위치를 사용합니다.
    /// </summary>
    public void ApplyNocturnFirstReservedActionFacing(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null || timelineController == null)
            return;

        bool isNocturn = string.Equals(monster.RuntimeData.MonsterId, "Mon_10", System.StringComparison.OrdinalIgnoreCase);
        bool isMort = string.Equals(monster.RuntimeData.MonsterId, "Mon_11", System.StringComparison.OrdinalIgnoreCase);
        if (!isNocturn && !isMort)
            return;

        string runtimeId = monster.RuntimeData.RuntimeId;
        for (int slot = 0; slot < timelineController.SlotCount; slot++)
        {
            var commands = timelineController.GetMonsterCommands(slot);
            if (commands == null)
                continue;

            foreach (MonsterReservedCommand command in commands)
            {
                if (command == null || command.RuntimeId != runtimeId)
                    continue;

                BattleDirection direction;
                if (command.IsPortalMove && command.RangeOriginGridIndex >= 0 && gridManager != null)
                {
                    Vector2Int from = gridManager.IndexToCoord(monster.MainGridIndex);
                    Vector2Int to = gridManager.IndexToCoord(command.RangeOriginGridIndex);
                    if (from.x == to.x)
                        return; // 같은 열이면 기존 좌우 방향을 유지합니다.
                    direction = to.x > from.x ? BattleDirection.Right : BattleDirection.Left;
                }
                else if (command.HasForcedDirection)
                {
                    direction = command.ForcedDirection;
                }
                else if (command.MoveOffset.x != 0)
                {
                    direction = command.MoveOffset.x > 0 ? BattleDirection.Right : BattleDirection.Left;
                }
                else if (isMort && command.SkillId == "S_Monster_31" &&
                         command.RangeOriginGridIndex >= 0 && gridManager != null)
                {
                    // 대상 중심 스킬은 효과 범위는 유지하고, 시선만 대상에게 맞춥니다.
                    Vector2Int from = gridManager.IndexToCoord(monster.MainGridIndex);
                    Vector2Int to = gridManager.IndexToCoord(command.RangeOriginGridIndex);
                    if (from.x == to.x)
                        continue;
                    direction = to.x > from.x ? BattleDirection.Right : BattleDirection.Left;
                }
                else
                {
                    // 수직 이동처럼 좌우 방향이 없는 행동은 다음 예약 행동을 확인합니다.
                    if (!isMort)
                        return;
                    continue;
                }

                BattleUnitFacing facing = monster.GetComponentInChildren<BattleUnitFacing>(true);
                if (facing != null)
                    facing.FaceRight(direction == BattleDirection.Right);
                monster.RuntimeData.Direction = direction;
                return;
            }
        }
    }

    /// <summary>
    /// 포털 생성 연출 중에는 첫 번째 포털의 목적지를 바라봅니다.
    /// 연출이 끝나면 첫 예약 행동의 방향을 다시 적용합니다.
    /// </summary>
    public void ApplyNocturnPortalPresentationFacing(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null || gridManager == null ||
            timelineController == null || monster.MainGridIndex < 0 ||
            !string.Equals(monster.RuntimeData.MonsterId, "Mon_10", System.StringComparison.OrdinalIgnoreCase))
            return;

        string runtimeId = monster.RuntimeData.RuntimeId;
        Vector2Int current = gridManager.IndexToCoord(monster.MainGridIndex);
        for (int slot = 0; slot < timelineController.SlotCount; slot++)
        {
            IReadOnlyList<MonsterReservedCommand> commands = timelineController.GetMonsterCommands(slot);
            if (commands == null)
                continue;

            foreach (MonsterReservedCommand command in commands)
            {
                if (command == null || command.RuntimeId != runtimeId ||
                    !command.IsPortalMove || command.RangeOriginGridIndex < 0)
                    continue;

                Vector2Int destination = gridManager.IndexToCoord(command.RangeOriginGridIndex);
                if (destination.x == current.x)
                    return; // 같은 열의 포털이면 좌우 방향을 유지합니다.

                BattleDirection direction = destination.x > current.x
                    ? BattleDirection.Right : BattleDirection.Left;
                BattleUnitFacing facing = monster.GetComponentInChildren<BattleUnitFacing>(true);
                if (facing != null)
                    facing.FaceRight(direction == BattleDirection.Right);
                monster.RuntimeData.Direction = direction;
                return;
            }
        }
    }

    /// <summary>
    /// 지정한 몬스터에게 유효한 포털 이동 예약이 있는지 확인합니다.
    /// </summary>
    public bool HasNocturnPortalReservation(string runtimeId)
    {
        if (timelineController == null || string.IsNullOrEmpty(runtimeId))
            return false;

        for (int slot = 0; slot < timelineController.SlotCount; slot++)
        {
            IReadOnlyList<MonsterReservedCommand> commands = timelineController.GetMonsterCommands(slot);
            if (commands == null)
                continue;

            foreach (MonsterReservedCommand command in commands)
            {
                if (command != null && command.IsPortalMove &&
                    command.RangeOriginGridIndex >= 0 &&
                    string.Equals(command.RuntimeId, runtimeId, System.StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 녹턴의 행동 애니메이션 시작과 동시에 예약된 위치에 포털을 표시합니다.
    /// </summary>
    public void ShowNocturnPortalIndicatorsForRuntime(string runtimeId)
    {
        if (timelineController == null || string.IsNullOrEmpty(runtimeId))
            return;

        for (int slot = 0; slot < timelineController.SlotCount; slot++)
        {
            IReadOnlyList<MonsterReservedCommand> commands = timelineController.GetMonsterCommands(slot);
            if (commands == null)
                continue;

            foreach (MonsterReservedCommand command in commands)
            {
                if (command != null &&
                    string.Equals(command.RuntimeId, runtimeId, System.StringComparison.Ordinal))
                    ShowNocturnPortalDestinationIndicator(command);
            }
        }
    }

    private void ShowNocturnPortalDestinationIndicator(MonsterReservedCommand command)
    {
        if (command == null || !command.IsPortalMove || command.RangeOriginGridIndex < 0)
            return;

        ResolvePlayerSkillReservationController();

        if (playerSkillReservationController == null)
            return;

        playerSkillReservationController.ShowNocturnPortalDestinationIndicator(
            command.RuntimeId,
            command.RangeOriginGridIndex);
    }

    private void ClearNocturnPortalDestinationIndicators()
    {
        ResolvePlayerSkillReservationController();

        if (playerSkillReservationController != null)
            playerSkillReservationController.ClearNocturnPortalDestinationIndicators();
    }

    private void ResolvePlayerSkillReservationController()
    {
        if (playerSkillReservationController != null)
            return;

        playerSkillReservationController = GetComponent<PlayerSkillReservationController>();

        if (playerSkillReservationController == null)
        {
            playerSkillReservationController =
                Object.FindFirstObjectByType<PlayerSkillReservationController>(
                    FindObjectsInactive.Include);
        }
    }

    private void ApplyElisePlayerSlotLock(List<MonsterUnit> monsterUnits)
    {
        if (timelineController == null)
            return;

        int lockedSlotIndex = EliseSlotLockService.RollLockedSlotIndex(
            monsterUnits,
            timelineController.SlotCount);
        timelineController.SetPlayerLockedSlot(lockedSlotIndex);
    }
    private List<MonsterReservedCommandPlan> BuildMonsterCommandPlans(List<MonsterUnit> monsterUnits)
    {
        List<MonsterReservedCommandPlan> plans = new List<MonsterReservedCommandPlan>();

        if (monsterUnits == null)
            return plans;

        plannedBattleTurn++;

        BattleContext context = new BattleContext
        {
            CurrentTurn = plannedBattleTurn
        };

        for (int i = 0; i < monsterUnits.Count; i++)
        {
            MonsterUnit monsterUnit = monsterUnits[i];

            if (monsterUnit == null || monsterUnit.RuntimeData == null)
                continue;

            MonsterRuntimeData runtime = monsterUnit.RuntimeData;

            if (runtime.IsDead)
                continue;

            // 앞서 이동 예약한 몬스터가 비우는 칸을 신더의 후보에 포함합니다.
            // 실제 예약 승인 시에는 슬롯 순서를 다시 검사합니다.
            CinderAI.SetPreviouslyVacatedCells(GetPreviouslyVacatedMonsterCells(monsterUnits, plans));
            CinderAI.SetReservedDestinationCells(GetPreviouslyReservedMonsterDestinations(monsterUnits, plans));
            Stopwatch aiWatch = logReservationPerformance ? Stopwatch.StartNew() : null;
            MonsterAIPlan plan = monsterUnit.CreateAIPlan(context, gridManager);
            CinderAI.SetPreviouslyVacatedCells(null);
            CinderAI.SetReservedDestinationCells(null);
            if (aiWatch != null)
            {
                aiWatch.Stop();
                if (aiWatch.Elapsed.TotalMilliseconds >= slowMonsterThresholdMs)
                    UnityEngine.Debug.Log($"[MonsterReservationPerf] AI Monster:{runtime.MonsterId} Grid:{monsterUnit.MainGridIndex} Time:{aiWatch.Elapsed.TotalMilliseconds:F2}ms Actions:{plan?.Actions?.Count ?? 0}", this);
            }

            if (plan == null || plan.Actions == null || plan.Actions.Count <= 0)
                continue;

            // 녹턴은 하나의 슬롯에서 2개 행동을 순서대로 실행해야 합니다.
            // Priority만 정렬하면 두 슬롯의 행동이 서로 끼어들 수 있습니다.
            if (string.Equals(runtime.MonsterId, "Mon_10", System.StringComparison.OrdinalIgnoreCase))
            {
                plan.Actions.Sort((a, b) =>
                {
                    int slotOrder = a.SlotOffset.CompareTo(b.SlotOffset);
                    return slotOrder != 0 ? slotOrder : a.Priority.CompareTo(b.Priority);
                });
            }
            else
            {
                plan.Actions.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }

            int baseSlotIndex = FindAvailableMonsterSlot(runtime, plans, plan.Actions);

            if (baseSlotIndex < 0)
            {
                if (runtime.MonsterId == "Mon_08" || runtime.MonsterId == "Mon_11")
                    UnityEngine.Debug.LogWarning($"[MonsterMoveReservation] {runtime.MonsterId}: 예약 가능한 시작 슬롯 없음");
                continue;
            }

            // Keep linked actions in the same resolved slot even when the first
            // action explicitly requests the earliest available slot.
            Dictionary<int, int> resolvedGroupSlots = new Dictionary<int, int>();

            for (int actionIndex = 0; actionIndex < plan.Actions.Count; actionIndex++)
            {
                MonsterAIAction action = plan.Actions[actionIndex];

                if (action == null || string.IsNullOrWhiteSpace(action.SkillId))
                    continue;

                MonsterSkillData skillData =
                    DataManager.Instance.MonsterSkillDatabase.Get(action.SkillId);

                if (skillData == null)
                {
                    Debug.LogWarning($"[BattleMonsterTurnPlanner] SkillData 없음: {action.SkillId}");
                    continue;
                }

                MonsterReservedCommand command = new MonsterReservedCommand(runtime, skillData);
                command.SetMoveOffset(action.MoveOffset);
                command.SetRangeOriginGridIndex(action.RangeOriginGridIndex);
                command.SetPortalMove(action.IsPortalMove);

                if (action.HasForcedDirection)
                    command.SetForcedDirection(action.ForcedDirection);

                if (action.ExplicitRangeGridIndices != null && action.ExplicitRangeGridIndices.Count > 0)
                {
                    command.SetExplicitRangeResult(
                        action.ExplicitRangeGridIndices,
                        action.ExplicitRangeGridIndices);
                }
                // 포탈 명령의 RangeOriginGridIndex에는 실제 순간이동 목적지가 저장되어 있습니다.
                // 일반 공격 범위 계산으로 이 값을 덮어쓰면 포탈 목적지가 엉뚱한 칸으로 바뀔 수 있으므로
                // 포탈 이동은 공격 범위 계산에서 제외합니다.
                else if (!action.IsPortalMove && !IsMoveSkill(skillData))
                {
                    SetMonsterRange(monsterUnit, skillData, command);
                }

                int slotIndex = ResolveMonsterActionSlot(baseSlotIndex, action, plans, runtime);
                if (action.SameSlotGroup >= 0 &&
                    action.SlotPreference == MonsterAISlotPreference.SameSlot &&
                    resolvedGroupSlots.TryGetValue(action.SameSlotGroup, out int groupedSlot))
                {
                    slotIndex = groupedSlot;
                }

                if (slotIndex < 0 || slotIndex >= timelineController.SlotCount ||
                    (string.Equals(runtime.MonsterId, "Mon_10", System.StringComparison.OrdinalIgnoreCase) && slotIndex >= 4))
                    continue;

                if (!IsSlotAvailableForMonster(runtime, slotIndex, plans))
                    continue;

                // 일반 몬스터는 다른 몬스터의 현재 위치 및 예약 이동 경로와 겹치는 이동을 방지합니다.
                // 블롭(Mon_02)은 캐릭터가 비운 칸으로 양쪽에서 진입할 수 있으므로
                // 예약 자체를 버리지 않고 실행 시점의 점유 검사 및 충돌 처리에 맡깁니다.
                // 포탈 이동과 강제 이동의 기존 규칙은 그대로 유지합니다.
                bool isBlobMove =
                    string.Equals(runtime.MonsterId, "Mon_02", System.StringComparison.Ordinal) &&
                    string.Equals(skillData.SkillId, "S_Monster_03", System.StringComparison.Ordinal);
                bool isCinderMove =
                    string.Equals(runtime.MonsterId, "Mon_06", System.StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(skillData.SkillId, "S_Monster_13", System.StringComparison.Ordinal);
                // 신더는 첫 이동 후보가 거절되어도 다른 빈칸을 찾아 재예약합니다.
                // 대체 계획은 기존 AI의 폭발 범위/팀 배치 평가를 그대로 사용합니다.
                if (isCinderMove && action.MoveOffset != Vector2Int.zero)
                {
                    HashSet<int> rejected = new HashSet<int>();
                    for (int attempt = 0; attempt < 8 &&
                         WouldCinderDestinationConflict(monsterUnit, action.MoveOffset,
                             slotIndex, monsterUnits, plans); attempt++)
                    {
                        Vector2Int blockedCoord = gridManager.IndexToCoord(monsterUnit.MainGridIndex) + action.MoveOffset;
                        if (gridManager.IsValidCoord(blockedCoord))
                            rejected.Add(gridManager.CoordToIndex(blockedCoord));

                        CinderAI.SetPreviouslyVacatedCells(GetPreviouslyVacatedMonsterCells(monsterUnits, plans));
                        CinderAI.SetReservedDestinationCells(GetPreviouslyReservedMonsterDestinations(monsterUnits, plans));
                        CinderAI.SetRejectedDestinationCells(rejected);
                        MonsterAIPlan alternative;
                        try
                        {
                            alternative = monsterUnit.CreateAIPlan(context, gridManager);
                        }
                        finally
                        {
                            CinderAI.SetRejectedDestinationCells(null);
                            CinderAI.SetPreviouslyVacatedCells(null);
                            CinderAI.SetReservedDestinationCells(null);
                        }

                        MonsterAIAction alternativeMove = alternative?.Actions?.Find(
                            candidate => candidate != null && candidate.SkillId == "S_Monster_13" &&
                                         candidate.MoveOffset != Vector2Int.zero);
                        if (alternativeMove == null)
                            break;

                        action.MoveOffset = alternativeMove.MoveOffset;
                        action.HasForcedDirection = alternativeMove.HasForcedDirection;
                        action.ForcedDirection = alternativeMove.ForcedDirection;
                        command.SetMoveOffset(action.MoveOffset);
                        if (action.HasForcedDirection)
                            command.SetForcedDirection(action.ForcedDirection);
                        else
                            command.ClearForcedDirection();

                        for (int nextIndex = actionIndex + 1; nextIndex < plan.Actions.Count; nextIndex++)
                        {
                            MonsterAIAction explosion = plan.Actions[nextIndex];
                            if (explosion == null || explosion.SkillId != "S_Monster_14") continue;
                            Vector2Int center = gridManager.IndexToCoord(monsterUnit.MainGridIndex) + action.MoveOffset;
                            if (!gridManager.IsValidCoord(center)) continue;
                            int index = gridManager.CoordToIndex(center);
                            explosion.RangeOriginGridIndex = index;
                            explosion.RangeOriginCasterGridIndex = index;
                        }
                    }
                }

                if (!isBlobMove &&
                    !action.IsPortalMove &&
                    skillData.TimelineNotation == TimelineActionType.Move &&
                    action.MoveOffset != Vector2Int.zero &&
                    (isCinderMove
                        ? WouldCinderDestinationConflict(monsterUnit, action.MoveOffset, slotIndex, monsterUnits, plans)
                        : WouldVoluntaryMoveCollide(monsterUnit, action.MoveOffset, monsterUnits, plans)))
                {
                    if ((runtime.MonsterId == "Mon_08" || runtime.MonsterId == "Mon_11") && action.MoveOffset != Vector2Int.zero)
                        UnityEngine.Debug.LogWarning($"[MonsterMoveReservation] {runtime.MonsterId}: 이동 예약 충돌 거부, Offset={action.MoveOffset}, Slot={slotIndex + 1}");
                    if (isCinderMove)
                    {
                        // 이동이 취소됐을 때 대폭발 중심점도 실제 현재 칸으로 되돌립니다.
                        for (int nextIndex = actionIndex + 1; nextIndex < plan.Actions.Count; nextIndex++)
                        {
                            MonsterAIAction nextAction = plan.Actions[nextIndex];
                            if (nextAction == null || nextAction.SkillId != "S_Monster_14") continue;
                            nextAction.RangeOriginGridIndex = monsterUnit.MainGridIndex;
                            nextAction.RangeOriginCasterGridIndex = monsterUnit.MainGridIndex;
                        }
                    }
                    continue;
                }

                // 신더는 실제 이동이 예약된 경우에만 방향을 미리 바꿉니다.
                if (isCinderMove && action.MoveOffset.x != 0)
                {
                    BattleUnitFacing facing = monsterUnit.GetComponentInChildren<BattleUnitFacing>(true);
                    if (facing != null) facing.FaceRight(action.MoveOffset.x > 0);
                    runtime.Direction = action.MoveOffset.x > 0
                        ? BattleDirection.Right : BattleDirection.Left;
                }

                plans.Add(new MonsterReservedCommandPlan(slotIndex, command));
                if (action.SameSlotGroup >= 0 && !resolvedGroupSlots.ContainsKey(action.SameSlotGroup))
                    resolvedGroupSlots.Add(action.SameSlotGroup, slotIndex);
            }
        }

        return plans;
    }

    private HashSet<int> GetPreviouslyReservedMonsterDestinations(
        List<MonsterUnit> units, List<MonsterReservedCommandPlan> plans)
    {
        HashSet<int> destinations = new HashSet<int>();
        if (units == null || plans == null || gridManager == null) return destinations;
        foreach (MonsterReservedCommandPlan planned in plans)
        {
            MonsterReservedCommand cmd = planned.Command;
            if (cmd == null || cmd.UserRuntime == null || cmd.MoveOffset == Vector2Int.zero ||
                cmd.IsPortalMove || cmd.SkillData == null ||
                cmd.SkillData.TimelineNotation != TimelineActionType.Move) continue;
            MonsterUnit moving = units.Find(u => u != null && u.RuntimeData == cmd.UserRuntime);
            if (moving == null || moving.MainGridIndex < 0) continue;
            foreach (int source in moving.OccupiedGridIndices)
            {
                Vector2Int destination = gridManager.IndexToCoord(source) + cmd.MoveOffset;
                if (gridManager.IsValidCoord(destination))
                    destinations.Add(gridManager.CoordToIndex(destination));
            }
        }
        return destinations;
    }

    private HashSet<int> GetPreviouslyVacatedMonsterCells(
        List<MonsterUnit> units, List<MonsterReservedCommandPlan> plans)
    {
        HashSet<int> result = new HashSet<int>();
        if (units == null || plans == null) return result;
        foreach (MonsterReservedCommandPlan planned in plans)
        {
            MonsterReservedCommand cmd = planned.Command;
            if (cmd == null || cmd.UserRuntime == null || cmd.MoveOffset == Vector2Int.zero ||
                cmd.IsPortalMove || cmd.SkillData == null ||
                cmd.SkillData.TimelineNotation != TimelineActionType.Move) continue;
            MonsterUnit moving = units.Find(u => u != null && u.RuntimeData == cmd.UserRuntime);
            if (moving == null) continue;
            foreach (int index in moving.OccupiedGridIndices)
                result.Add(index);
        }
        return result;
    }

    // 앞 몬스터가 더 이른 슬롯에서 이동해 비우는 자리로만 진입을 허용합니다.
    private bool CanCinderEnterEarlierVacatedCell(
        MonsterUnit unit, Vector2Int offset, int slotIndex,
        List<MonsterUnit> units, List<MonsterReservedCommandPlan> plans)
    {
        if (unit == null || gridManager == null || units == null || plans == null ||
            unit.MainGridIndex < 0) return false;
        Vector2Int destination = gridManager.IndexToCoord(unit.MainGridIndex) + offset;
        if (!gridManager.IsValidCoord(destination)) return false;
        int destinationIndex = gridManager.CoordToIndex(destination);
        MonsterUnit blocker = units.Find(u => u != null && u != unit &&
            u.RuntimeData != null && !u.RuntimeData.IsDead &&
            u.OccupiedGridIndices.Contains(destinationIndex));
        if (blocker == null) return false;

        bool vacatesEarlier = false;
        foreach (MonsterReservedCommandPlan earlier in plans)
        {
            MonsterReservedCommand cmd = earlier.Command;
            if (cmd == null || cmd.UserRuntime == null || cmd.MoveOffset == Vector2Int.zero ||
                cmd.IsPortalMove || cmd.SkillData == null ||
                cmd.SkillData.TimelineNotation != TimelineActionType.Move) continue;
            if (cmd.UserRuntime == blocker.RuntimeData && earlier.SlotIndex < slotIndex)
            {
                Vector2Int blockerDestination = gridManager.IndexToCoord(blocker.MainGridIndex) + cmd.MoveOffset;
                if (gridManager.IsValidCoord(blockerDestination) &&
                    gridManager.CoordToIndex(blockerDestination) != destinationIndex)
                    vacatesEarlier = true;
            }
            if (cmd.UserRuntime != blocker.RuntimeData)
            {
                MonsterUnit other = units.Find(u => u != null && u.RuntimeData == cmd.UserRuntime);
                if (other != null && other.MainGridIndex >= 0)
                {
                    Vector2Int otherEnd = gridManager.IndexToCoord(other.MainGridIndex) + cmd.MoveOffset;
                    if (gridManager.IsValidCoord(otherEnd) &&
                        gridManager.CoordToIndex(otherEnd) == destinationIndex)
                        return false;
                }
            }
        }
        return vacatesEarlier;
    }

    // 신더는 출발 칸을 포함하는 넓은 충돌 영역 대신 실제 도착 칸을 기준으로 예약합니다.
    // 다른 몬스터가 더 이른 슬롯에서 해당 칸을 비우는 경우에만 진입을 허용합니다.
    private bool WouldCinderDestinationConflict(
        MonsterUnit movingUnit, Vector2Int offset, int slotIndex,
        List<MonsterUnit> allUnits, List<MonsterReservedCommandPlan> pendingPlans)
    {
        if (movingUnit == null || gridManager == null || movingUnit.MainGridIndex < 0)
            return true;

        Vector2Int destination = gridManager.IndexToCoord(movingUnit.MainGridIndex) + offset;
        if (!gridManager.IsValidCoord(destination))
            return true;
        int destinationIndex = gridManager.CoordToIndex(destination);

        if (BattleOccupancyService.IsOccupiedByCharacter(destinationIndex))
            return true;

        // 예약 시 이미 캐릭터가 점유한 칸으로는 자발적으로 이동하지 않습니다.
        // 이후 캐릭터가 예약 목적지로 이동하는 경우는 실행 시 충돌 규칙에 맡깁니다.
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(FindObjectsSortMode.None);
        foreach (BattleCharacter character in characters)
        {
            if (character != null && character.CurrentGridIndex == destinationIndex)
                return true;
        }

        // 이미 예약한 도착 칸과 겹치면 무조건 다른 목적지를 선택해야 합니다.
        if (pendingPlans != null && allUnits != null)
        {
            foreach (MonsterReservedCommandPlan pending in pendingPlans)
            {
                MonsterReservedCommand previous = pending.Command;
                if (previous == null || previous.UserRuntime == null ||
                    previous.UserRuntime == movingUnit.RuntimeData ||
                    previous.IsPortalMove || previous.MoveOffset == Vector2Int.zero ||
                    previous.SkillData == null ||
                    previous.SkillData.TimelineNotation != TimelineActionType.Move)
                    continue;
                MonsterUnit other = allUnits.Find(u => u != null && u.RuntimeData == previous.UserRuntime);
                if (other == null || other.MainGridIndex < 0) continue;
                Vector2Int otherDestination = gridManager.IndexToCoord(other.MainGridIndex) + previous.MoveOffset;
                if (gridManager.IsValidCoord(otherDestination) &&
                    gridManager.CoordToIndex(otherDestination) == destinationIndex)
                    return true;
            }
        }

        if (allUnits == null) return false;
        foreach (MonsterUnit other in allUnits)
        {
            if (other == null || other == movingUnit || other.RuntimeData == null || other.RuntimeData.IsDead ||
                !other.OccupiedGridIndices.Contains(destinationIndex))
                continue;
            if (!CanCinderEnterEarlierVacatedCell(movingUnit, offset, slotIndex, allUnits, pendingPlans))
                return true;
        }
        return false;
    }

    // Conservatively reserve the swept footprint of previously planned voluntary moves.
    // This also catches diagonal crossings and routes through occupied starting cells.
    private bool WouldVoluntaryMoveCollide(
        MonsterUnit movingUnit,
        Vector2Int offset,
        List<MonsterUnit> allUnits,
        List<MonsterReservedCommandPlan> pendingPlans)
    {
        if (movingUnit == null || gridManager == null)
            return true;

        HashSet<int> proposed = BuildSweptFootprint(movingUnit, offset);
        if (proposed.Count == 0)
            return true;

        // Never voluntarily enter or pass through another monster's current footprint.
        if (allUnits != null)
        {
            foreach (MonsterUnit other in allUnits)
            {
                if (other == null || other == movingUnit || other.RuntimeData == null ||
                    other.RuntimeData.IsDead)
                    continue;

                foreach (int occupiedIndex in other.OccupiedGridIndices)
                    if (proposed.Contains(occupiedIndex))
                        return true;
            }
        }

        if (pendingPlans == null || allUnits == null)
            return false;

        foreach (MonsterReservedCommandPlan pending in pendingPlans)
        {
            MonsterReservedCommand previous = pending.Command;
            if (previous == null || previous.UserRuntime == null ||
                previous.UserRuntime == movingUnit.RuntimeData ||
                previous.IsPortalMove || previous.MoveOffset == Vector2Int.zero ||
                previous.SkillData == null ||
                previous.SkillData.TimelineNotation != TimelineActionType.Move)
                continue;

            MonsterUnit previousUnit = allUnits.Find(u =>
                u != null && u.RuntimeData == previous.UserRuntime);
            if (previousUnit == null)
                continue;

            HashSet<int> reserved = BuildSweptFootprint(previousUnit, previous.MoveOffset);
            reserved.IntersectWith(proposed);
            if (reserved.Count > 0)
                return true;
        }
        return false;
    }

    private HashSet<int> BuildSweptFootprint(MonsterUnit unit, Vector2Int offset)
    {
        HashSet<int> indices = new HashSet<int>();
        if (unit == null || gridManager == null || unit.OccupiedGridIndices == null)
            return indices;

        // Cover both axis orders for diagonal moves: a corner-cut must not bypass a unit.
        int minX = Mathf.Min(0, offset.x);
        int maxX = Mathf.Max(0, offset.x);
        int minY = Mathf.Min(0, offset.y);
        int maxY = Mathf.Max(0, offset.y);
        foreach (int startIndex in unit.OccupiedGridIndices)
        {
            Vector2Int start = gridManager.IndexToCoord(startIndex);
            for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                {
                    Vector2Int cell = start + new Vector2Int(x, y);
                    if (gridManager.IsValidCoord(cell))
                        indices.Add(gridManager.CoordToIndex(cell));
                }
        }
        return indices;
    }

    private int ResolveMonsterActionSlot(
        int baseSlotIndex,
        MonsterAIAction action,
        List<MonsterReservedCommandPlan> pendingPlans,
        MonsterRuntimeData runtime)
    {
        if (action == null)
            return baseSlotIndex;

        int slotCount = timelineController != null ? timelineController.SlotCount : 0;

        // 튜토리얼 1 전투(Map_27 / Battlemap_17)는 블롭의 AI 패턴과 스킬 선택은
        // 그대로 사용하되, 예약되는 타임라인 위치만 항상 3번 슬롯으로 고정합니다.
        // 슬롯 인덱스는 0부터 시작하므로 3번 슬롯은 index 2입니다.
        if (IsTutorialBattle1Map() && slotCount > 2)
            return 2;

        if (slotCount <= 0)
            return -1;

        int offsetBaseSlot = baseSlotIndex + action.SlotOffset;

        // 녹턴은 1~3번 슬롯에서 시작하며 후속 행동은 4번 슬롯을 넘지 않습니다.
        // 후속 간격이 끝을 넘어가도 시작 슬롯 후보가 배제되지 않도록 조정합니다.
        if (string.Equals(runtime?.MonsterId, "Mon_10", System.StringComparison.OrdinalIgnoreCase) &&
            action.SlotOffset > 0)
        {
            offsetBaseSlot = Mathf.Min(offsetBaseSlot, 3);
        }

        switch (action.SlotPreference)
        {
            case MonsterAISlotPreference.Earliest:
                return FindEarliestSlot(runtime, pendingPlans);

            case MonsterAISlotPreference.FirstTwo:
                return FindFirstTwoSlot(runtime, pendingPlans);

            case MonsterAISlotPreference.NextSlot:
                return offsetBaseSlot + 1;

            case MonsterAISlotPreference.SameSlot:
                return offsetBaseSlot;

            case MonsterAISlotPreference.Back:
                return FindBackSlot(pendingPlans);

            case MonsterAISlotPreference.Last:
                return slotCount - 1;

            case MonsterAISlotPreference.Center:
                return slotCount / 2;

            case MonsterAISlotPreference.Front:
            default:
                return offsetBaseSlot;
        }
    }

    private static bool IsTutorialBattle1Map()
    {
        MapRuntimeData mapRuntime = DataManager.Instance?.MapRuntimeStore?.Get();

        return mapRuntime != null &&
               string.Equals(
                   mapRuntime.CurrentMapId?.Trim(),
                   TutorialBattleEntrySetup.FirstTutorialMapId,
                   System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 튜토리얼 전투 2/3에서는 플레이어가 먼저 행동을 예약할 시간을 확보하기 위해
    /// 몬스터가 타임라인 1~2번 슬롯을 사용하지 않습니다.
    /// 허용 슬롯은 3~5번(index 2~4)뿐입니다.
    /// </summary>
    private static bool IsTutorialBattleWithDelayedMonsterSlots()
    {
        MapRuntimeData mapRuntime = DataManager.Instance?.MapRuntimeStore?.Get();
        string currentMapId = mapRuntime?.CurrentMapId?.Trim();

        if (string.IsNullOrWhiteSpace(currentMapId))
            return false;

        return string.Equals(
                   currentMapId,
                   TutorialBattleEntrySetup.SecondTutorialMapId,
                   System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   currentMapId,
                   TutorialBattleEntrySetup.ThirdTutorialMapId,
                   System.StringComparison.OrdinalIgnoreCase);
    }

    private int GetMonsterMinimumSlotIndex()
    {
        if (!IsTutorialBattleWithDelayedMonsterSlots())
            return 0;

        return timelineController != null && timelineController.SlotCount > 2
            ? 2
            : 0;
    }

    private int FindEarliestSlot(
        MonsterRuntimeData runtime,
        List<MonsterReservedCommandPlan> pendingPlans)
    {
        if (timelineController == null || runtime == null)
            return -1;

        int minimumSlotIndex = GetMonsterMinimumSlotIndex();

        // 일반 전투는 1~5번 슬롯, 튜토리얼 전투 2/3은 3~5번 슬롯만 앞에서부터 확인합니다.
        // 가능하면 다른 행동과 겹치지 않는 가장 빠른 빈 슬롯을 우선 사용합니다.
        for (int i = minimumSlotIndex; i < timelineController.SlotCount; i++)
        {
            if (IsSlotCompletelyEmpty(i, pendingPlans))
                return i;
        }

        // 빈 슬롯이 없다면 같은 몬스터 행동과 공유 가능한 가장 빠른 슬롯을 사용합니다.
        for (int i = minimumSlotIndex; i < timelineController.SlotCount; i++)
        {
            if (IsSlotAvailableForMonster(runtime, i, pendingPlans))
                return i;
        }

        return -1;
    }

    private int FindFirstTwoSlot(
        MonsterRuntimeData runtime,
        List<MonsterReservedCommandPlan> pendingPlans)
    {
        if (timelineController == null || runtime == null)
            return -1;

        int minimumSlotIndex = GetMonsterMinimumSlotIndex();
        int limit = Mathf.Min(minimumSlotIndex + 2, timelineController.SlotCount);

        // 일반 전투의 FirstTwo는 1/2번 슬롯을 사용합니다.
        // 튜토리얼 전투 2/3에서는 같은 의미를 허용 범위의 앞 두 슬롯인 3/4번으로 옮깁니다.
        for (int i = minimumSlotIndex; i < limit; i++)
        {
            if (IsSlotCompletelyEmpty(i, pendingPlans))
                return i;
        }

        // 앞 두 허용 슬롯이 이미 사용 중이라면, 같은 몬스터 행동과의 공유는 허용합니다.
        for (int i = minimumSlotIndex; i < limit; i++)
        {
            if (IsSlotAvailableForMonster(runtime, i, pendingPlans))
                return i;
        }

        return -1;
    }

    private bool IsSlotCompletelyEmpty(
        int slotIndex,
        List<MonsterReservedCommandPlan> pendingPlans)
    {
        if (timelineController == null ||
            slotIndex < 0 ||
            slotIndex >= timelineController.SlotCount)
        {
            return false;
        }

        var commands = timelineController.GetMonsterCommands(slotIndex);

        if (commands != null && commands.Count > 0)
            return false;

        if (pendingPlans != null)
        {
            for (int i = 0; i < pendingPlans.Count; i++)
            {
                if (pendingPlans[i].SlotIndex == slotIndex)
                    return false;
            }
        }

        return true;
    }

    private int FindBackSlot(List<MonsterReservedCommandPlan> pendingPlans)
    {
        int minimumSlotIndex = GetMonsterMinimumSlotIndex();

        for (int i = timelineController.SlotCount - 1; i >= minimumSlotIndex; i--)
        {
            var commands = timelineController.GetMonsterCommands(i);

            bool hasCommand = commands != null && commands.Count > 0;

            if (!hasCommand && pendingPlans != null)
            {
                for (int j = 0; j < pendingPlans.Count; j++)
                {
                    if (pendingPlans[j].SlotIndex == i)
                    {
                        hasCommand = true;
                        break;
                    }
                }
            }

            if (!hasCommand)
                return i;
        }

        return timelineController.SlotCount - 1;
    }

    private int FindAvailableMonsterSlot(
        MonsterRuntimeData runtime,
        List<MonsterReservedCommandPlan> pendingPlans,
        List<MonsterAIAction> actions)
    {
        if (runtime == null)
            return -1;

        List<int> candidates = new List<int>();

        int minimumSlotIndex = GetMonsterMinimumSlotIndex();

        int maximumSlotExclusive = string.Equals(runtime.MonsterId, "Mon_10", System.StringComparison.OrdinalIgnoreCase)
            ? Mathf.Min(3, timelineController.SlotCount)
            : timelineController.SlotCount;

        for (int i = minimumSlotIndex; i < maximumSlotExclusive; i++)
        {
            if (!CanPlacePlanAtBaseSlot(runtime, i, pendingPlans, actions))
                continue;
            bool hasCommand = false;
            bool sameMonsterOnly = true;

            var commands = timelineController.GetMonsterCommands(i);

            if (commands != null && commands.Count > 0)
            {
                hasCommand = true;

                for (int j = 0; j < commands.Count; j++)
                {
                    if (commands[j] == null || commands[j].RuntimeId != runtime.RuntimeId)
                    {
                        sameMonsterOnly = false;
                        break;
                    }
                }
            }

            if (pendingPlans != null && sameMonsterOnly)
            {
                for (int j = 0; j < pendingPlans.Count; j++)
                {
                    MonsterReservedCommandPlan plan = pendingPlans[j];

                    if (plan.SlotIndex != i)
                        continue;

                    hasCommand = true;

                    if (plan.Command == null || plan.Command.RuntimeId != runtime.RuntimeId)
                    {
                        sameMonsterOnly = false;
                        break;
                    }
                }
            }

            if (!hasCommand || sameMonsterOnly)
                candidates.Add(i);
        }

        if (candidates.Count <= 0)
            return -1;

        // 녹턴은 앞 슬롯 중심으로 예약합니다. 다른 몬스터의 확률은 변경하지 않습니다.
        // 시작 슬롯은 1~3번(60/35/5)이며, 후속 슬롯은 2~4번만 사용합니다.
        if (string.Equals(runtime.MonsterId, "Mon_10", System.StringComparison.OrdinalIgnoreCase))
        {
            int[] frontWeights = { 60, 35, 5 };
            int totalWeight = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                int index = candidates[i];
                totalWeight += index >= 0 && index < frontWeights.Length ? frontWeights[index] : 1;
            }

            int roll = BattleRandom.Range(0, totalWeight);
            for (int i = 0; i < candidates.Count; i++)
            {
                int index = candidates[i];
                roll -= index >= 0 && index < frontWeights.Length ? frontWeights[index] : 1;
                if (roll < 0)
                    return index;
            }
        }

        int randomIndex = BattleRandom.Range(0, candidates.Count);
        return candidates[randomIndex];
    }

    private bool CanPlacePlanAtBaseSlot(
        MonsterRuntimeData runtime,
        int baseSlotIndex,
        List<MonsterReservedCommandPlan> pendingPlans,
        List<MonsterAIAction> actions)
    {
        if (runtime == null || timelineController == null)
            return false;

        if (actions == null || actions.Count <= 0)
            return true;

        for (int i = 0; i < actions.Count; i++)
        {
            MonsterAIAction action = actions[i];

            if (action == null)
                continue;

            if (action.SlotPreference == MonsterAISlotPreference.Earliest ||
                action.SlotPreference == MonsterAISlotPreference.FirstTwo ||
                action.SlotPreference == MonsterAISlotPreference.Back ||
                action.SlotPreference == MonsterAISlotPreference.Last ||
                action.SlotPreference == MonsterAISlotPreference.Center)
            {
                continue;
            }

            int requiredSlot = baseSlotIndex + action.SlotOffset;

            if (string.Equals(runtime.MonsterId, "Mon_10", System.StringComparison.OrdinalIgnoreCase) &&
                action.SlotOffset > 0)
            {
                requiredSlot = Mathf.Min(requiredSlot, 3);
            }

            if (action.SlotPreference == MonsterAISlotPreference.NextSlot)
                requiredSlot += 1;

            if (requiredSlot < GetMonsterMinimumSlotIndex() ||
                requiredSlot >= timelineController.SlotCount ||
                (string.Equals(runtime.MonsterId, "Mon_10", System.StringComparison.OrdinalIgnoreCase) && requiredSlot >= 4))
            {
                return false;
            }

            if (!IsSlotAvailableForMonster(runtime, requiredSlot, pendingPlans))
                return false;
        }

        return true;
    }

    private bool IsSlotAvailableForMonster(
        MonsterRuntimeData runtime,
        int slotIndex,
        List<MonsterReservedCommandPlan> pendingPlans)
    {
        if (runtime == null || timelineController == null)
            return false;

        var commands = timelineController.GetMonsterCommands(slotIndex);

        if (commands != null)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                if (commands[i] == null || commands[i].RuntimeId != runtime.RuntimeId)
                    return false;
            }
        }

        if (pendingPlans != null)
        {
            for (int i = 0; i < pendingPlans.Count; i++)
            {
                MonsterReservedCommandPlan pending = pendingPlans[i];

                if (pending.SlotIndex != slotIndex)
                    continue;

                if (pending.Command == null || pending.Command.RuntimeId != runtime.RuntimeId)
                    return false;
            }
        }

        return true;
    }

    private void ShowActionReserveIntroText()
    {
        BattleEffectInfoFeed.ShowTurnStart(Mathf.Max(1, plannedBattleTurn));
        StartCoroutine(BattleEffectInfoFeed.FlushInitialEffectsAndWait());
        PlayActionReservationStartSfx();
        // 예약 단계 안내 문구는 출력하지 않습니다.
        // BattleTurnExecutor가 플레이어 입력 가능 시점에 'N턴'을 표시합니다.
    }

    private IEnumerator ShowActionReserveIntroTextAndWaitRoutine(bool showTurnHeading = true)
    {
        if (showTurnHeading)
            BattleEffectInfoFeed.ShowTurnStart(Mathf.Max(1, plannedBattleTurn));
        StartCoroutine(BattleEffectInfoFeed.FlushInitialEffectsAndWait());
        PlayActionReservationStartSfx();
        // 문구 대기 대신 예약 준비만 마치고 즉시 반환합니다.
        // 최초 전투 시작 인트로는 위의 battleStartTextRoutine에서 이미 기다립니다.
        yield break;
    }

    private void PlayActionReservationStartSfx()
    {
        if (!playActionReservationStartSfx || string.IsNullOrWhiteSpace(actionReservationStartSfxId))
            return;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            Debug.LogWarning(
                $"[{nameof(BattleMonsterTurnPlanner)}] 행동 예약 시작 SFX를 재생할 AudioManager.Instance가 없습니다.",
                this);
            return;
        }

        if (!audioManager.TryGetSfxData(actionReservationStartSfxId, out _))
        {
            Debug.LogWarning(
                $"[{nameof(BattleMonsterTurnPlanner)}] 행동 예약 시작 SFX ID를 찾을 수 없습니다: {actionReservationStartSfxId}",
                this);
            return;
        }

        audioManager.PlaySfx(actionReservationStartSfxId, Mathf.Clamp01(actionReservationStartSfxVolume));
    }

    private void ShowIntroText(string text)
    {
        if (battleMapIntroText != null)
        {
            battleMapIntroText.Play(text);
            return;
        }

        BattleMapIntroText.ShowMessage(text);
    }

    private IEnumerator ShowIntroTextAndWaitRoutine(string text)
    {
        if (battleMapIntroText != null)
        {
            yield return battleMapIntroText.PlayAndWait(text);
            yield break;
        }

        yield return BattleMapIntroText.ShowMessageAndWait(text);
    }

    private void StopIntroText()
    {
        if (battleMapIntroText != null)
        {
            battleMapIntroText.StopAndHide();
            return;
        }

        BattleMapIntroText target =
            FindFirstObjectByType<BattleMapIntroText>(FindObjectsInactive.Include);

        if (target != null)
            target.StopAndHide();
    }

    private bool IsMoveSkill(MonsterSkillData skillData)
    {
        if (skillData == null)
            return false;

        if (skillData.TimelineNotation == TimelineActionType.Move)
            return true;

        if (!string.IsNullOrWhiteSpace(skillData.EffectIds) &&
            skillData.EffectIds.Contains("E_Move"))
            return true;

        return false;
    }

    private void SetMonsterRange(
        MonsterUnit monsterUnit,
        MonsterSkillData skillData,
        MonsterReservedCommand command)
    {
        if (monsterUnit == null || skillData == null || command == null)
        {
            Debug.LogWarning("[MonsterRange] null 참조");
            return;
        }

        if (gridManager == null)
        {
            Debug.LogWarning("[MonsterRange] gridManager 없음");
            return;
        }

        int casterGridIndex = monsterUnit.MainGridIndex;

        if (casterGridIndex < 0)
        {
            Debug.LogWarning($"[MonsterRange] MainGridIndex 없음 / Monster:{monsterUnit.RuntimeData?.Name}");
            return;
        }

        if (string.IsNullOrWhiteSpace(skillData.RangeId) || skillData.RangeId == "0")
        {
            Debug.LogWarning($"[MonsterRange] RangeId 없음 / Skill:{skillData.SkillId}");
            return;
        }

        int rangeOriginGridIndex = command.RangeOriginGridIndex >= 0
            ? command.RangeOriginGridIndex
            : casterGridIndex;

        BattleDirection direction = command.HasForcedDirection
            ? command.ForcedDirection
            : GetDirectionToNearestPlayer(rangeOriginGridIndex);

        List<int> rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
            rangeOriginGridIndex,
            skillData.RangeId,
            direction,
            DataManager.Instance.RangeDatabase,
            gridManager
        );

        if (rangeIndices == null)
            rangeIndices = new List<int>();

        command.SetRangeResult(rangeIndices, rangeIndices);
    }

    private BattleDirection GetDirectionToNearestPlayer(int originGridIndex)
    {
        BattleCharacter[] players =
            Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        if (players == null || players.Length <= 0)
            return BattleDirection.Left;

        Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);

        BattleCharacter nearest = null;
        int nearestDistance = int.MaxValue;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null || players[i].RuntimeData == null)
                continue;

            if (players[i].RuntimeData.IsDead)
                continue;

            if (players[i].CurrentGridIndex < 0)
                continue;

            Vector2Int playerCoord = gridManager.IndexToCoord(players[i].CurrentGridIndex);

            int distance =
                Mathf.Abs(playerCoord.x - originCoord.x) +
                Mathf.Abs(playerCoord.y - originCoord.y);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = players[i];
            }
        }

        if (nearest == null)
            return BattleDirection.Left;

        Vector2Int targetCoord = gridManager.IndexToCoord(nearest.CurrentGridIndex);

        if (targetCoord.x >= originCoord.x)
            return BattleDirection.Right;

        return BattleDirection.Left;
    }
    private BattleDirection GetDirectionToNearestPlayer(MonsterUnit monsterUnit)
    {
        BattleCharacter[] players =
            Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        if (players == null || players.Length <= 0)
            return BattleDirection.Left;

        int monsterGrid = monsterUnit.MainGridIndex;
        Vector2Int monsterCoord = gridManager.IndexToCoord(monsterGrid);

        BattleCharacter nearest = null;
        int nearestDistance = int.MaxValue;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null || players[i].RuntimeData == null)
                continue;

            if (players[i].RuntimeData.IsDead)
                continue;

            if (players[i].CurrentGridIndex < 0)
                continue;

            Vector2Int playerCoord = gridManager.IndexToCoord(players[i].CurrentGridIndex);

            int distance =
                Mathf.Abs(playerCoord.x - monsterCoord.x) +
                Mathf.Abs(playerCoord.y - monsterCoord.y);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = players[i];
            }
        }

        if (nearest == null)
            return BattleDirection.Left;

        Vector2Int targetCoord = gridManager.IndexToCoord(nearest.CurrentGridIndex);

        if (targetCoord.x >= monsterCoord.x)
            return BattleDirection.Right;

        return BattleDirection.Left;
    }

    private readonly struct MonsterReservedCommandPlan
    {
        public readonly int SlotIndex;
        public readonly MonsterReservedCommand Command;

        public MonsterReservedCommandPlan(int slotIndex, MonsterReservedCommand command)
        {
            SlotIndex = slotIndex;
            Command = command;
        }
    }
}
