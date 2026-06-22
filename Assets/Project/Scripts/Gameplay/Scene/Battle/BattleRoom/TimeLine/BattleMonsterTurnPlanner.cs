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

        timelineController.ClearMonsterCommands();

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

        timelineController.ClearMonsterCommands();

        if (showBattleStart)
            ShowIntroText(battleStartMessage);

        List<MonsterReservedCommandPlan> plans = BuildMonsterCommandPlans(monsterUnits);

        for (int i = 0; i < plans.Count; i++)
        {
            if (plans[i].SlotIndex < 0 || plans[i].SlotIndex >= timelineController.SlotCount)
                continue;

            timelineController.AddMonsterCommand(plans[i].SlotIndex, plans[i].Command);
        }

        if (showActionReserve)
            ShowActionReserveIntroText();
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

            int baseSlotIndex = FindAvailableMonsterSlot(runtime, plans);

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

                if (!IsMoveSkill(skillData))
                    SetMonsterRange(monsterUnit, skillData, command);

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

        switch (action.SlotPreference)
        {
            case MonsterAISlotPreference.NextSlot:
                return Mathf.Clamp(baseSlotIndex + 1, 0, slotCount - 1);

            case MonsterAISlotPreference.SameSlot:
                return baseSlotIndex;

            case MonsterAISlotPreference.Back:
                return FindBackSlot(pendingPlans);

            case MonsterAISlotPreference.Last:
                return slotCount - 1;

            case MonsterAISlotPreference.Center:
                return slotCount / 2;

            case MonsterAISlotPreference.Front:
            default:
                return baseSlotIndex;
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
        List<MonsterReservedCommandPlan> pendingPlans)
    {
        if (runtime == null)
            return -1;

        List<int> candidates = new List<int>();

        for (int i = 0; i < timelineController.SlotCount; i++)
        {
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

        int randomIndex = Random.Range(0, candidates.Count);
        return candidates[randomIndex];
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

        BattleDirection direction = GetDirectionToNearestPlayer(monsterUnit);

        List<int> rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
            casterGridIndex,
            skillData.RangeId,
            direction,
            DataManager.Instance.RangeDatabase,
            gridManager
        );

        if (rangeIndices == null)
            rangeIndices = new List<int>();

        command.SetRangeResult(rangeIndices, rangeIndices);
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
            if (players[i] == null || players[i].CurrentGridIndex < 0)
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