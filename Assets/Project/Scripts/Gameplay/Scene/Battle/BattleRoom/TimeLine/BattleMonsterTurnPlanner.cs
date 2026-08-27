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

    [Header("SFX")]
    [SerializeField] private bool playActionReserveSfx = true;
    [SerializeField] private SfxType actionReserveSfxType = SfxType.BattleActionReserveText;
    [SerializeField, Range(0f, 1f)] private float actionReserveSfxVolume = 1f;

    private Coroutine planRoutine;
    private Coroutine battleStartTextRoutine;
    private bool battleStartIntroShown;

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

    public void ResetBattleStartIntroState()
    {
        battleStartIntroShown = false;
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
            battleStartTextRoutine = StartCoroutine(ShowIntroTextAndWaitRoutine(battleStartMessage));

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
            ShowIntroText(battleStartMessage);

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

        BattleContext context = new BattleContext();

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
                command.SetRangeOriginCasterGridIndex(action.RangeOriginCasterGridIndex);
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

                int slotIndex = ResolveMonsterActionSlot(baseSlotIndex, action, plans);

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
        List<MonsterReservedCommandPlan> pendingPlans)
    {
        if (action == null)
            return baseSlotIndex;

        int slotCount = timelineController != null ? timelineController.SlotCount : 0;

        if (slotCount <= 0)
            return -1;

        int offsetBaseSlot = baseSlotIndex + action.SlotOffset;

        switch (action.SlotPreference)
        {
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

            if (action.SlotPreference == MonsterAISlotPreference.Back ||
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
        PlaySfx(playActionReserveSfx, actionReserveSfxType, actionReserveSfxVolume);
        ShowIntroText(actionReserveMessage);
    }

    private void PlaySfx(bool play, SfxType sfxType, float volume)
    {
        if (!play)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(sfxType, volume);
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
