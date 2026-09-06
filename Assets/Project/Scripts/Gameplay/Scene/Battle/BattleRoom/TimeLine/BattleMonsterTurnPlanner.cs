using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private string battleStartMessage = "전투 시작";
    [SerializeField] private string actionReserveMessage = "행동 예약";
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
            battleStartTextRoutine = StartCoroutine(ShowIntroTextAndWaitRoutine(battleStartMessage));
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
                ShowNocturnPortalDestinationIndicator(command);
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
            battleStartTextRoutine = StartCoroutine(ShowIntroTextAndWaitRoutine(battleStartMessage));
        }

        if (firstMonsterCommandDelay > 0f)
            yield return new WaitForSeconds(firstMonsterCommandDelay);

        List<MonsterReservedCommandPlan> plans = BuildMonsterCommandPlans(monsterUnits);

        for (int i = 0; i < plans.Count; i++)
        {
            if (plans[i].SlotIndex < 0 || plans[i].SlotIndex >= timelineController.SlotCount)
                continue;

            timelineController.AddMonsterCommand(plans[i].SlotIndex, plans[i].Command);
            ShowNocturnPortalDestinationIndicator(plans[i].Command);

            if (monsterCommandInterval > 0f && i < plans.Count - 1)
                yield return new WaitForSeconds(monsterCommandInterval);
        }

        if (battleStartTextRoutine != null)
        {
            yield return battleStartTextRoutine;
            battleStartTextRoutine = null;
        }

        if (actionReserveMessageDelay > 0f)
            yield return new WaitForSeconds(actionReserveMessageDelay);

        // 첫 전투 진입 연출에서는 행동 예약 안내까지 이 코루틴이 직접 기다립니다.
        // BattleRoomLoader가 전역 표시 상태를 폴링하지 않아도 정확한 종료 시점을 알 수 있습니다.
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
            ShowIntroText(battleStartMessage);
        }

        List<MonsterReservedCommandPlan> plans = BuildMonsterCommandPlans(monsterUnits);

        for (int i = 0; i < plans.Count; i++)
        {
            if (plans[i].SlotIndex < 0 || plans[i].SlotIndex >= timelineController.SlotCount)
                continue;

            timelineController.AddMonsterCommand(plans[i].SlotIndex, plans[i].Command);
            ShowNocturnPortalDestinationIndicator(plans[i].Command);
        }

        if (showActionReserve)
            ShowActionReserveIntroText();
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

            MonsterAIPlan plan = monsterUnit.CreateAIPlan(context, gridManager);

            if (plan == null || plan.Actions == null || plan.Actions.Count <= 0)
                continue;

            plan.Actions.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            int baseSlotIndex = FindAvailableMonsterSlot(runtime, plans, plan.Actions);

            if (baseSlotIndex < 0)
                continue;

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

                if (slotIndex < 0 || slotIndex >= timelineController.SlotCount)
                    continue;

                plans.Add(new MonsterReservedCommandPlan(slotIndex, command));
            }
        }

        return plans;
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

        if (slotCount <= 0)
            return -1;

        int offsetBaseSlot = baseSlotIndex + action.SlotOffset;

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

    private int FindEarliestSlot(
        MonsterRuntimeData runtime,
        List<MonsterReservedCommandPlan> pendingPlans)
    {
        if (timelineController == null || runtime == null)
            return -1;

        // 1~5번 슬롯을 앞에서부터 확인합니다.
        // 가능하면 다른 행동과 겹치지 않는 가장 빠른 빈 슬롯을 우선 사용합니다.
        for (int i = 0; i < timelineController.SlotCount; i++)
        {
            if (IsSlotCompletelyEmpty(i, pendingPlans))
                return i;
        }

        // 빈 슬롯이 없다면 같은 몬스터 행동과 공유 가능한 가장 빠른 슬롯을 사용합니다.
        for (int i = 0; i < timelineController.SlotCount; i++)
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

        int limit = Mathf.Min(2, timelineController.SlotCount);

        // 철옹성처럼 FirstTwo를 사용하는 행동은 가능하면 1/2번 슬롯을
        // 다른 행동과 공유하지 않고 단독으로 사용합니다.
        for (int i = 0; i < limit; i++)
        {
            if (IsSlotCompletelyEmpty(i, pendingPlans))
                return i;
        }

        // 앞 두 슬롯이 이미 사용 중이라면, 같은 몬스터 행동과의 공유는 허용합니다.
        for (int i = 0; i < limit; i++)
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
        for (int i = timelineController.SlotCount - 1; i >= 0; i--)
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

        for (int i = 0; i < timelineController.SlotCount; i++)
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

            if (action.SlotPreference == MonsterAISlotPreference.NextSlot)
                requiredSlot += 1;

            if (requiredSlot < 0 || requiredSlot >= timelineController.SlotCount)
                return false;

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
        PlayActionReservationStartSfx();
        ShowIntroText(actionReserveMessage);
    }

    private IEnumerator ShowActionReserveIntroTextAndWaitRoutine()
    {
        PlayActionReservationStartSfx();
        yield return ShowIntroTextAndWaitRoutine(actionReserveMessage);
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
