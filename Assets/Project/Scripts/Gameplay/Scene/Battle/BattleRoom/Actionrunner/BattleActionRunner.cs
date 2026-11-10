using Relic.Gameplay.Data;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;
using UnityEngine;

public class BattleActionRunner
{
    // CoroutineHost runs action routines independently of BattleTurnExecutor.
    // A forced room end must cancel these routines before resolving victory.
    private static readonly HashSet<BattleActionRunner> activeRunners = new();
    private readonly HashSet<Coroutine> activeActionCoroutines = new();

    public static void CancelAllActiveActionsForRoomEnd()
    {
        BattleActionRunner[] runners = new BattleActionRunner[activeRunners.Count];
        activeRunners.CopyTo(runners);
        foreach (BattleActionRunner runner in runners)
            runner.CancelActiveActionsForRoomEnd();
        activeRunners.Clear();
    }

    private void CancelActiveActionsForRoomEnd()
    {
        if (CoroutineHost.Instance != null)
        {
            foreach (Coroutine coroutine in activeActionCoroutines)
            {
                if (coroutine != null)
                    CoroutineHost.Instance.StopCoroutine(coroutine);
            }
        }
        activeActionCoroutines.Clear();
    }

    private void TrackActionCoroutine(Coroutine coroutine)
    {
        if (coroutine == null)
            return;
        activeActionCoroutines.Add(coroutine);
        activeRunners.Add(this);
    }

    private void UntrackActionCoroutine(Coroutine coroutine)
    {
        if (coroutine != null)
            activeActionCoroutines.Remove(coroutine);
        if (activeActionCoroutines.Count == 0)
            activeRunners.Remove(this);
    }

    private readonly GridManager gridManager;

    private readonly BattleUnitFinder unitFinder;
    private readonly BattleHUDService hudService;
    private readonly BattleDamageService damageService;
    private readonly BattleDeathService deathService;
    private readonly BattleStatusEffectService statusEffectService;
    private readonly MonsterSkillEffectService monsterSkillEffectService;
    private readonly BattleEffectExecutor effectExecutor = new();
    private readonly bool useSafeSequentialExecution;
    private readonly float actionRoutineTimeout;
    private readonly Action<PlayerReservedCommand, int> onPlayerCommandExecuted;
    private readonly Func<PlayerReservedCommand, IEnumerator> onPlayerSkillStarted;
    private readonly BattleConsecutiveActionPlan consecutiveActionPlan;
    private readonly float multiHitActionInterval;
    private BattleConsecutiveActionInfo activeActionInfo = BattleConsecutiveActionInfo.Single;
    // 같은 스킬의 다단히트는 한 번만 기울이고, 다음 스킬/반격에서 다시 방향을 전환합니다.
    private bool playerAttackPlaneStartedForCurrentSkill;
    private BattleGridEffectController gridEffectController;
    private readonly HashSet<string> nocturnPortalFailedRuntimeIds = new();

    private const float ActionDelay = 0.03f;
    private const float GroupedActionBeatDelay = 0.12f;

    private const float HitCameraDelay = 0.08f;
    private const float ForcedMoveVisualCompletionDelay = 0.18f;
    private const float DefaultActionRoutineTimeout = 8f;
    private const string MuckMonsterId = "Mon_01";
    private const string MuckMoveSkillId = "S_Monster_01";
    private const string MuckProjectileSkillId = "S_Monster_02";
    private const string BlobAttackSkillId = "S_Monster_04";
    private const string BlobMoveSkillId = "S_Monster_03";
    private const string BlobMonsterId = "Mon_02";
    private const string RancorMonsterId = "Mon_03";
    private const string RancorMoveSkillId = "S_Monster_05";
    private const string BlightMonsterId = "Mon_04";
    private const string BlightMoveSkillId = "S_Monster_08";
    private const string BlightDebuffSkillId = "S_Monster_10";
    private const string VespaMonsterId = "Mon_05";
    private const string VespaMoveSkillId = "S_Monster_11";
    private const string VespaAttackSkillId = "S_Monster_12";
    private const string CinderMonsterId = "Mon_06";
    private const string CinderExplodeSkillId = "S_Monster_14";
    private const string DraugrMonsterId = "Mon_07";
    private const string DraugrMoveSkillId = "S_Monster_15";
    private const string DraugrAttackSkillId = "S_Monster_16";
    private const string BarrowMonsterId = "Mon_08";
    private const string BarrowMoveSkillId = "S_Monster_18";
    private const string BarrowDirectShotSkillId = "S_Monster_19";
    private const string BarrowArcShotSkillId = "S_Monster_20";
    private const string RookMonsterId = "Mon_09";
    private const string RookMoveSkillId = "S_Monster_21";
    private const string RookBashSkillId = "S_Monster_23";
    private const string RookEarthquakeSkillId = "S_Monster_24";
    private const string NocturnMonsterId = "Mon_10";
    private const string NocturnMoveSkillId = "S_Monster_25";
    private const string MortMonsterId = "Mon_11";
    private const string MortMoveSkillId = "S_Monster_29";
    private const string MortWraithRushSkillId = "S_Monster_30";
    private const string ArabellaMoveSkillId = "S_Monster_33";
    private static readonly HashSet<string> RangedMonsterAttackSkillIds = new(StringComparer.Ordinal)
    {
        "S_Monster_02",
        "S_Monster_22",
        "S_Monster_30",
        "S_Monster_31",
        "S_Monster_35",
        "S_Monster_38",
        "S_Monster_39",
        "S_Monster_40"
    };
    private const string NocturnThrustSkillId = "S_Monster_26";
    private const string NocturnSlashSkillId = "S_Monster_27";
    private const string NocturnPullSkillId = "S_Monster_28";
    private const string ResidueGridEffectId = "GR_Residue";
    // Keep short moves snappy while giving the camera enough time to follow long moves.
    public const float MoveAnimationDuration = 0.15f;
    private const float AdditionalMoveGridDuration = 0.03f;

    private static float GetMoveAnimationDuration(int gridCount)
    {
        return MoveAnimationDuration + Mathf.Max(0, gridCount - 1) * AdditionalMoveGridDuration;
    }
    private const float GridEffectResumeDelay = 0.65f;

    private class ActionRoutine
    {
        public string Label;
        public IEnumerator Routine;
        public BattleConsecutiveActionInfo PresentationInfo;
    }

    public BattleActionRunner(
      GridManager gridManager,
      BattleMonsterSpawner monsterSpawner = null,
      BattleRoomLoader roomLoader = null)
        : this(gridManager, monsterSpawner, roomLoader, true, DefaultActionRoutineTimeout)
    {
    }

    public BattleActionRunner(
      GridManager gridManager,
      BattleMonsterSpawner monsterSpawner,
      BattleRoomLoader roomLoader,
      bool useSafeSequentialExecution,
      float actionRoutineTimeout,
      Action<PlayerReservedCommand, int> onPlayerCommandExecuted = null,
      BattleConsecutiveActionPlan consecutiveActionPlan = null,
      float multiHitActionInterval = 0.12f,
      Func<PlayerReservedCommand, IEnumerator> onPlayerSkillStarted = null)
    {
        this.gridManager = gridManager;
        this.useSafeSequentialExecution = useSafeSequentialExecution;
        this.actionRoutineTimeout = Mathf.Max(0.1f, actionRoutineTimeout);
        this.onPlayerCommandExecuted = onPlayerCommandExecuted;
        this.onPlayerSkillStarted = onPlayerSkillStarted;
        this.consecutiveActionPlan = consecutiveActionPlan;
        this.multiHitActionInterval = Mathf.Max(0f, multiHitActionInterval);

        unitFinder = new BattleUnitFinder();
        hudService = new BattleHUDService();
        damageService = new BattleDamageService(unitFinder);
        deathService = new BattleDeathService(gridManager, monsterSpawner, roomLoader);
        statusEffectService = new BattleStatusEffectService(damageService, deathService);
        monsterSkillEffectService = new MonsterSkillEffectService(damageService, deathService, hudService, gridManager);
    }

    public BattleActionRunner(
      GridManager gridManager,
      BattleMonsterSpawner monsterSpawner,
      BattleRoomLoader roomLoader,
      object fourthArgument,
      object fifthArgument)
        : this(
            gridManager,
            monsterSpawner,
            roomLoader,
            fourthArgument is bool safeSequential ? safeSequential : true,
            fifthArgument is float timeout ? timeout : DefaultActionRoutineTimeout)
    {
    }

    public IEnumerator RunBatch(BattleActionBatch batch, bool keepCameraAfterBatch = false)
    {
        if (batch == null)
            yield break;

        List<ActionRoutine> actionRoutines = BuildActionRoutines(batch);

        if (actionRoutines.Count <= 0)
        {
            BattleConsecutiveActionInfo skippedLastInfo =
                GetLastPresentationInfo(batch);

            if (skippedLastInfo.IsGrouped && skippedLastInfo.IsGroupEnd)
                BattleConsecutiveActionPresentationContext.EndGroup();

            if (!keepCameraAfterBatch && BattleCameraController.Instance != null)
            {
                BattleCameraController.Instance.SetHoldDefaultReturn(false);
                yield return BattleCameraController.Instance.ReturnDefaultIfNotHeld();
            }

            yield break;
        }

        bool batchHasInteraction = BatchHasInteractionAction(batch);
        bool batchHasCrossSideHit = BatchHasCrossSideHitAction(batch);
        BattleConsecutiveActionInfo firstInfo = consecutiveActionPlan != null
            ? consecutiveActionPlan.GetFirstInfo(batch)
            : BattleConsecutiveActionInfo.Single;
        bool continuesGroupFromPreviousBatch =
            firstInfo.IsGrouped && !firstInfo.IsGroupStart;
        bool holdCameraDuringBatch =
            keepCameraAfterBatch ||
            continuesGroupFromPreviousBatch ||
            (batchHasCrossSideHit && ShouldHoldCameraUntilBatchEnd(batch));

        if (BattleCameraController.Instance != null)
        {
            BattleCameraController.Instance.SetHoldDefaultReturn(holdCameraDuringBatch);

            if (!batchHasCrossSideHit &&
                !continuesGroupFromPreviousBatch &&
                BattleCameraController.Instance.IsCombatZoomActive)
            {
                yield return BattleCameraController.Instance.ReturnDefault();
            }
        }

        if (useSafeSequentialExecution)
            yield return RunSequential(actionRoutines);
        else
            yield return RunParallel(actionRoutines);

        // 다음 행동과 이어지지 않는 슬롯은 타임라인이 이동하기 전에 카메라와
        // BattleEffect 회전을 함께 복귀시킵니다. 빈 슬롯 이동 중 기울어진 화면이 남지 않게 합니다.
        if (!keepCameraAfterBatch && BattleCameraController.Instance != null &&
            (BattleCameraController.Instance.IsCombatZoomActive ||
             BattleEffectPlaneRotation.HasActiveAttackRotationFeedback()))
        {
            BattleCameraController.Instance.SetHoldDefaultReturn(false);
            yield return BattleCameraController.Instance.ReturnDefault();
        }

        IncreaseMonsterTurnCountsOnceInSlot(batch);

        BattleConsecutiveActionInfo lastInfo = GetLastPresentationInfo(batch);

        if (!lastInfo.IsGrouped || lastInfo.IsGroupEnd)
            yield return RunPostActionPresentationRoutine(batchHasInteraction, lastInfo.SpeedMultiplier);
    }

    private List<ActionRoutine> BuildActionRoutines(BattleActionBatch batch)
    {
        List<ActionRoutine> actionRoutines = new();

        if (batch == null)
            return actionRoutines;

        for (int i = 0; i < batch.PlayerCommands.Count; i++)
        {
            PlayerReservedCommand command = batch.PlayerCommands[i];

            if (!BattleActionOrderUtility.HasSwift(command))
                continue;

            AddPlayerActionRoutine(actionRoutines, command, batch.TimelineSlotIndex);
        }

        for (int i = 0; i < batch.MonsterCommands.Count; i++)
        {
            MonsterReservedCommand command = batch.MonsterCommands[i];

            if (command == null || IsDeadMonsterCommand(command))
                continue;

            actionRoutines.Add(CreateActionRoutine(
                $"Monster:{command.RuntimeId}:{command.SkillId}",
                ShowMonsterEffectInfoAndExecute(command),
                GetPresentationInfo(command)));
        }

        for (int i = 0; i < batch.PlayerCommands.Count; i++)
        {
            PlayerReservedCommand command = batch.PlayerCommands[i];

            if (BattleActionOrderUtility.HasSwift(command))
                continue;

            AddPlayerActionRoutine(actionRoutines, command, batch.TimelineSlotIndex);
        }

        return actionRoutines;
    }

    private void AddPlayerActionRoutine(
        List<ActionRoutine> actionRoutines,
        PlayerReservedCommand command,
        int timelineSlotIndex)
    {
        if (actionRoutines == null || command == null)
            return;

        if (command.UserRuntime == null || command.UserRuntime.IsDead)
            return;

        if (command.ReservedMoveGridIndex >= 0)
        {
            if (IsConsumedVisualSkipMove(command))
                return;

            actionRoutines.Add(CreateActionRoutine(
                $"PlayerMove:{command.CharacterId}",
                ExecutePlayerCommandAndNotify(command, timelineSlotIndex, ExecutePlayerMove(command)),
                BattleConsecutiveActionInfo.Single));
            return;
        }

        actionRoutines.Add(CreateActionRoutine(
            $"PlayerSkill:{command.CharacterId}:{command.SkillId}",
            ExecutePlayerCommandAndNotify(command, timelineSlotIndex, ExecutePlayerSkill(command)),
            GetPresentationInfo(command)));
    }

    private IEnumerator ExecutePlayerCommandAndNotify(
        PlayerReservedCommand command,
        int timelineSlotIndex,
        IEnumerator actionRoutine)
    {
        // 같은 배치 안에서 앞선 행동으로 예약자가 사망할 수 있으므로,
        // 실제 실행 직전에도 다시 확인하고 즉시 건너뜁니다.
        if (command == null || command.UserRuntime == null || command.UserRuntime.IsDead)
            yield break;

        BattleEffectInfoFeed.ShowPlayer(command);

        bool consumeSmiteAfterAttack = ShouldConsumeSmite(command);

        if (actionRoutine != null)
        {
            while (actionRoutine.MoveNext())
                yield return actionRoutine.Current;
        }

        if (consumeSmiteAfterAttack)
            ConsumeSmiteAfterAttack(command);

        if (command != null && command.UserRuntime != null && !command.UserRuntime.IsDead)
            onPlayerCommandExecuted?.Invoke(command, timelineSlotIndex);
    }

    private IEnumerator ShowMonsterEffectInfoAndExecute(MonsterReservedCommand command)
    {
        if (command == null || command.UserRuntime == null || command.UserRuntime.IsDead)
            yield break;

        BattleEffectInfoFeed.ShowMonster(command);
        yield return ExecuteMonsterCommand(command);
    }

    private static bool ShouldConsumeSmite(PlayerReservedCommand command)
    {
        if (command == null ||
            command.UserRuntime == null ||
            command.SkillData == null ||
            command.SkillData.SkillType != SkillType.Attack ||
            command.UserRuntime.StatusEffects == null)
        {
            return false;
        }

        for (int i = 0; i < command.UserRuntime.StatusEffects.Count; i++)
        {
            StatusEffectRuntimeData status = command.UserRuntime.StatusEffects[i];

            if (status != null && status.EffectId == "E_Smite" && status.Stack > 0)
                return true;
        }

        return false;
    }

    private static void ConsumeSmiteAfterAttack(PlayerReservedCommand command)
    {
        if (command == null || command.UserRuntime == null || command.UserRuntime.StatusEffects == null)
            return;

        List<StatusEffectRuntimeData> statuses = command.UserRuntime.StatusEffects;

        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            StatusEffectRuntimeData status = statuses[i];

            if (status == null || status.EffectId != "E_Smite")
                continue;

            if (status.Stack > 1)
            {
                status.Stack--;
                status.TurnCount = Mathf.Max(status.TurnCount, 1);
            }
            else
            {
                statuses.RemoveAt(i);
            }

            return;
        }
    }

    private IEnumerator RunPostActionPresentationRoutine(
        bool hasInteraction = true,
        float speedMultiplier = 1f)
    {
        // 행동 종료 후 별도의 대기 없이 다음 타임라인 오더로 진행합니다.
        // HUD 정리 및 대기 애니메이션은 그대로 유지합니다.
        hudService.HideUnselectedMonsterHUDs();
        hudService.PlayAllAliveIdle();
        yield break;
    }

    public bool ApplyTurnEndEffects()
    {
        bool playedPresentation = statusEffectService.ApplyTurnEndEffects();
        hudService.RefreshHUDs();

        return playedPresentation;
    }

    public IEnumerator ApplyTurnEndEffectsRoutine()
    {
        bool playedPresentation = ApplyTurnEndEffects();

        if (!playedPresentation)
            yield break;

        IEnumerator presentationRoutine = RunPostActionPresentationRoutine();

        while (presentationRoutine.MoveNext())
            yield return presentationRoutine.Current;
    }

    public IEnumerator ReturnCameraDefaultIfNeeded()
    {
        BattleConsecutiveActionPresentationContext.EndGroup();

        if (BattleCameraController.Instance == null)
            yield break;

        BattleCameraController.Instance.SetHoldDefaultReturn(false);
        yield return BattleCameraController.Instance.ReturnDefault();
    }

    private bool ShouldHoldCameraUntilBatchEnd(BattleActionBatch batch)
    {
        return CountCrossSideHitActions(batch) > 1;
    }

    public bool BatchHasCrossSideHitAction(BattleActionBatch batch)
    {
        return CountCrossSideHitActions(batch) > 0;
    }

    public bool BatchHasInteractionAction(BattleActionBatch batch)
    {
        if (batch == null)
            return false;

        if (batch.PlayerCommands != null)
        {
            for (int i = 0; i < batch.PlayerCommands.Count; i++)
            {
                if (PlayerCommandHasInteraction(batch.PlayerCommands[i]))
                    return true;
            }
        }

        if (batch.MonsterCommands != null)
        {
            for (int i = 0; i < batch.MonsterCommands.Count; i++)
            {
                if (MonsterCommandHasInteraction(batch.MonsterCommands[i]))
                    return true;
            }
        }

        return false;
    }

    private bool PlayerCommandHasInteraction(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null ||
            command.UserRuntime == null || command.UserRuntime.IsDead)
            return false;

        if (command.ReservedMoveGridIndex >= 0)
            return false;

        if (ShouldPlayerSkillTargetSelf(command))
            return command.UserRuntime != null && !command.UserRuntime.IsDead;

        if (ShouldPlayerSkillTargetPlayerParty(command))
            return HasPlayerPartyTarget(command);

        return HasMonsterTarget(command);
    }

    private bool MonsterCommandHasInteraction(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null || IsDeadMonsterCommand(command))
            return false;

        if (command.SkillData.TimelineNotation == TimelineActionType.Move)
            return false;

        if (command.SkillData.Target == TargetType.Self)
        {
            MonsterUnit monster = unitFinder.FindMonsterUnit(command.RuntimeId);
            return monster != null && monster.RuntimeData != null && !monster.RuntimeData.IsDead;
        }

        if (command.SkillData.Target == TargetType.EnemyParty)
            return FindFirstAliveMonsterTarget(command, unitFinder.FindMonsterUnit(command.RuntimeId)) != null;

        return HasPlayerTarget(command);
    }

    private static bool IsAllRangePlayerSkill(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;

        string rangeId = BattleEquipmentEffectService.GetEffectiveRangeId(
            command.UserRuntime,
            command.SkillData
        );

        return BattleRangeCalculator.IsAllRangeId(rangeId);
    }

    private static bool ShouldPlayerSkillTargetPlayerParty(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;

        if (!IsAllRangePlayerSkill(command))
            return command.SkillData.Target == TargetType.PlayerParty;

        if (command.SkillData.SkillType == SkillType.Buff)
            return true;

        if (command.SkillData.SkillType == SkillType.Attack ||
            command.SkillData.SkillType == SkillType.Debuff)
        {
            return false;
        }

        return command.SkillData.Target == TargetType.PlayerParty;
    }

    private static bool ShouldPlayerSkillTargetSelf(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;

        if (IsAllRangePlayerSkill(command))
            return false;

        return command.SkillData.Target == TargetType.Self;
    }

    private bool HasPlayerPartyTarget(PlayerReservedCommand command)
    {
        if (command == null || command.RangeGridIndices == null)
            return false;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (character.RuntimeData.IsDead)
                continue;

            if (command.RangeGridIndices.Contains(character.CurrentGridIndex))
                return true;
        }

        return false;
    }

    private int CountCrossSideHitActions(BattleActionBatch batch)
    {
        if (batch == null)
            return 0;

        int count = 0;

        if (batch.PlayerCommands != null)
        {
            for (int i = 0; i < batch.PlayerCommands.Count; i++)
            {
                PlayerReservedCommand command = batch.PlayerCommands[i];

                if (command == null || command.SkillData == null ||
                    command.UserRuntime == null || command.UserRuntime.IsDead)
                    continue;

                if (command.ReservedMoveGridIndex >= 0)
                    continue;

                if (ShouldPlayerSkillTargetSelf(command) ||
                    ShouldPlayerSkillTargetPlayerParty(command) ||
                    ShouldSkipPlayerSkillCamera(command))
                {
                    continue;
                }

                if (HasMonsterTarget(command))
                    count++;
            }
        }

        if (batch.MonsterCommands != null)
        {
            for (int i = 0; i < batch.MonsterCommands.Count; i++)
            {
                MonsterReservedCommand command = batch.MonsterCommands[i];

                if (command == null || command.SkillData == null || IsDeadMonsterCommand(command))
                    continue;

                if (command.SkillData.TimelineNotation == TimelineActionType.Move)
                    continue;

                if (ShouldSkipMonsterSkillCamera(command))
                    continue;

                if (HasPlayerTarget(command))
                    count++;
            }
        }

        return count;
    }

    private bool HasMonsterTarget(PlayerReservedCommand command)
    {
        if (command == null)
            return false;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null)
                continue;

            if (IsMonsterInRange(monster, command))
                return true;
        }

        return false;
    }

    private bool HasPlayerTarget(MonsterReservedCommand command)
    {
        return FindFirstPlayerTarget(command) != null;
    }

    private void IncreaseMonsterTurnCountsOnceInSlot(BattleActionBatch batch)
    {
        if (batch == null || batch.MonsterCommands == null)
            return;

        HashSet<string> increasedRuntimeIds = new();

        for (int i = 0; i < batch.MonsterCommands.Count; i++)
        {
            MonsterReservedCommand command = batch.MonsterCommands[i];

            if (command == null || string.IsNullOrWhiteSpace(command.RuntimeId))
                continue;

            if (increasedRuntimeIds.Contains(command.RuntimeId))
                continue;

            MonsterUnit monster = unitFinder.FindMonsterUnit(command.RuntimeId);

            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                continue;

            monster.RuntimeData.IncreaseTurnCount();
            increasedRuntimeIds.Add(command.RuntimeId);
        }
    }

    private ActionRoutine CreateActionRoutine(
        string label,
        IEnumerator routine,
        BattleConsecutiveActionInfo presentationInfo = default)
    {
        return new ActionRoutine
        {
            Label = label,
            Routine = routine,
            PresentationInfo = presentationInfo.GroupSize > 0
                ? presentationInfo
                : BattleConsecutiveActionInfo.Single
        };
    }

    private BattleConsecutiveActionInfo GetPresentationInfo(
        PlayerReservedCommand command)
    {
        if (!useSafeSequentialExecution || consecutiveActionPlan == null)
            return BattleConsecutiveActionInfo.Single;

        return consecutiveActionPlan.GetInfo(command);
    }

    private BattleConsecutiveActionInfo GetPresentationInfo(
        MonsterReservedCommand command)
    {
        if (!useSafeSequentialExecution || consecutiveActionPlan == null)
            return BattleConsecutiveActionInfo.Single;

        return consecutiveActionPlan.GetInfo(command);
    }

    private BattleConsecutiveActionInfo GetLastPresentationInfo(
        BattleActionBatch batch)
    {
        if (!useSafeSequentialExecution || consecutiveActionPlan == null)
            return BattleConsecutiveActionInfo.Single;

        return consecutiveActionPlan.GetLastInfo(batch);
    }

    private bool ShouldEnterCameraForActiveAction()
    {
        return activeActionInfo.ShouldEnterCamera;
    }

    private bool ShouldPlayCameraImpactForHit(int hitIndex, int hitCount)
    {
        int safeHitCount = Mathf.Max(1, hitCount);

        // 화면 흔들림/히트스톱은 예약 행동 하나당 한 번 재생합니다.
        // 다단 공격이라면 해당 행동의 마지막 내부 타격에서만 재생합니다.
        return hitIndex >= safeHitCount - 1;
    }

    private bool ShouldPlayExternalImpactForHit(int hitIndex, int hitCount)
    {
        int safeHitCount = Mathf.Max(1, hitCount);
        bool isLastHit = hitIndex >= safeHitCount - 1;

        // 피격 밀림은 기존 정책대로 연속 행동 그룹의 마지막 행동에서만 재생합니다.
        // 다단 공격이라면 그 마지막 행동의 마지막 내부 타격에서만 적용합니다.
        if (safeHitCount > 1)
        {
            if (!activeActionInfo.IsGrouped)
                return isLastHit;

            return activeActionInfo.ShouldPlayExternalImpact && isLastHit;
        }

        return !activeActionInfo.IsGrouped || activeActionInfo.ShouldPlayExternalImpact;
    }

    private static float GetMultiHitAnimationSpeed(int hitCount)
    {
        if (hitCount <= 1)
            return 1f;

        // 2타=1.25, 3타=1.5, 4타=1.75, 5타 이상=2.0
        return Mathf.Min(2f, 1f + ((hitCount - 1) * 0.25f));
    }

    private bool ShouldReturnCameraForActiveAction()
    {
        return activeActionInfo.ShouldReturnCamera;
    }

    private float GetActivePresentationSpeed()
    {
        // 연속 행동 배율은 현재 행동 내부가 아니라
        // 현재 행동의 마지막 연출 -> 다음 같은 행동 시작 경계에만 적용합니다.
        float consecutiveMultiplier = activeActionInfo.IsGrouped && !activeActionInfo.IsGroupEnd
            ? Mathf.Max(1f, activeActionInfo.SpeedMultiplier)
            : 1f;

        return consecutiveMultiplier * BattlePresentationSpeedSettings.CurrentMultiplier;
    }

    private float GetActiveActionBeatDelay(float baseDelay = ActionDelay)
    {
        return BattleConsecutiveActionPresentationContext.ScaleActionBeatDuration(
            baseDelay,
            GroupedActionBeatDelay);
    }

    private IEnumerator ReturnCameraAfterActiveActionIfNeeded()
    {
        if (!ShouldReturnCameraForActiveAction() || BattleCameraController.Instance == null)
            yield break;

        yield return BattleCameraController.Instance.ReturnDefaultIfNotHeld();
    }

    private IEnumerator RunSequential(List<ActionRoutine> routines)
    {
        if (routines == null || routines.Count == 0)
            yield break;

        for (int i = 0; i < routines.Count; i++)
        {
            if (i > 0 &&
                ShouldReturnCameraBetweenSequentialActions(routines[i - 1], routines[i]) &&
                BattleCameraController.Instance != null)
            {
                yield return BattleCameraController.Instance.ReturnDefault();
            }

            yield return RunSingleWithTimeout(routines[i]);
        }
    }

    private static bool ShouldReturnCameraBetweenSequentialActions(
        ActionRoutine previous,
        ActionRoutine current)
    {
        if (previous != null &&
            current != null &&
            previous.PresentationInfo.IsGrouped &&
            current.PresentationInfo.IsGrouped &&
            previous.PresentationInfo.GroupId == current.PresentationInfo.GroupId)
        {
            return false;
        }

        return IsMonsterActionRoutine(previous) && IsMonsterActionRoutine(current);
    }

    private static bool IsMonsterActionRoutine(ActionRoutine actionRoutine)
    {
        return actionRoutine != null &&
               !string.IsNullOrEmpty(actionRoutine.Label) &&
               actionRoutine.Label.StartsWith("Monster:", StringComparison.Ordinal);
    }

    private IEnumerator RunSingleWithTimeout(ActionRoutine actionRoutine)
    {
        if (actionRoutine == null || actionRoutine.Routine == null)
            yield break;

        if (CoroutineHost.Instance == null)
        {
            Debug.LogError($"[BattleActionRunner] CoroutineHost 없음 / Action:{actionRoutine.Label}");
            yield break;
        }

        bool completed = false;
        activeActionInfo = actionRoutine.PresentationInfo;
        BattleConsecutiveActionPresentationContext.BeginAction(activeActionInfo);

        Coroutine runningCoroutine = CoroutineHost.Instance.StartCoroutine(
            RunAndCountDown(
                actionRoutine,
                () => completed = true
            )
        );

        TrackActionCoroutine(runningCoroutine);
        float elapsed = 0f;

        while (!completed)
        {
            // Finish every hit of the active skill before stopping later reservations.
            // The phase transition is checked at the next action boundary.

            elapsed += Time.unscaledDeltaTime;

            if (elapsed >= actionRoutineTimeout)
            {
                Debug.LogError(
                    $"[BattleActionRunner] Action Timeout / " +
                    $"Action:{actionRoutine.Label} / Timeout:{actionRoutineTimeout:0.00}s"
                );

                if (runningCoroutine != null && CoroutineHost.Instance != null)
                    CoroutineHost.Instance.StopCoroutine(runningCoroutine);

                UntrackActionCoroutine(runningCoroutine);
                BattleConsecutiveActionPresentationContext.CompleteAction(
                    activeActionInfo,
                    completedNormally: false);
                activeActionInfo = BattleConsecutiveActionInfo.Single;
                yield break;
            }

            yield return null;
        }

        UntrackActionCoroutine(runningCoroutine);
        BattleConsecutiveActionPresentationContext.CompleteAction(
            activeActionInfo,
            completedNormally: true);
        activeActionInfo = BattleConsecutiveActionInfo.Single;
    }

    private IEnumerator RunParallel(List<ActionRoutine> routines)
    {
        if (routines == null || routines.Count == 0)
            yield break;

        if (CoroutineHost.Instance == null)
        {
            Debug.LogError("[BattleActionRunner] CoroutineHost 없음");
            yield break;
        }

        int runningCount = routines.Count;
        bool[] completed = new bool[routines.Count];
        Coroutine[] runningCoroutines = new Coroutine[routines.Count];

        for (int i = 0; i < routines.Count; i++)
        {
            int routineIndex = i;

            runningCoroutines[i] = CoroutineHost.Instance.StartCoroutine(
                RunAndCountDown(
                    routines[i],
                    () =>
                    {
                        completed[routineIndex] = true;
                        runningCount--;
                        //Debug.Log($"[BattleActionRunner] Routine End:{routineIndex} / Left:{runningCount}");
                    }
                )
            );
            TrackActionCoroutine(runningCoroutines[i]);
        }

        float elapsed = 0f;

        while (runningCount > 0)
        {
            // Already-started skills must finish, including all repeated hits.
            // The executor will skip later batches after this batch completes.

            elapsed += Time.unscaledDeltaTime;

            if (elapsed >= actionRoutineTimeout)
            {
                Debug.LogError(
                    $"[BattleActionRunner] RunParallel Timeout / " +
                    $"Left:{runningCount} / Timeout:{actionRoutineTimeout:0.00}s / " +
                    $"Actions:{BuildPendingActionLabel(completed, routines)}"
                );

                for (int i = 0; i < runningCoroutines.Length; i++)
                {
                    if (completed[i] || runningCoroutines[i] == null || CoroutineHost.Instance == null)
                        continue;

                    CoroutineHost.Instance.StopCoroutine(runningCoroutines[i]);
                }

                break;
            }

            yield return null;
        }

        for (int i = 0; i < runningCoroutines.Length; i++)
            UntrackActionCoroutine(runningCoroutines[i]);
    }

    private string BuildPendingActionLabel(bool[] completed, List<ActionRoutine> routines)
    {
        if (completed == null || routines == null)
            return "";

        List<string> pendingLabels = new();

        for (int i = 0; i < routines.Count; i++)
        {
            if (i < completed.Length && completed[i])
                continue;

            pendingLabels.Add(routines[i] != null ? routines[i].Label : $"Index:{i}");
        }

        return string.Join(", ", pendingLabels);
    }

    private IEnumerator RunAndCountDown(ActionRoutine actionRoutine, System.Action onComplete)
    {
        bool done = false;
        IEnumerator routine = actionRoutine != null ? actionRoutine.Routine : null;

        while (!done)
        {
            object current = null;

            try
            {
                if (routine == null || !routine.MoveNext())
                {
                    done = true;
                }
                else
                {
                    current = routine.Current;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BattleActionRunner] Action Exception / Action:{actionRoutine?.Label}");
                Debug.LogException(e);
                done = true;
            }

            if (!done)
                yield return current;
        }

        onComplete?.Invoke();
    }

    private void ShowExecutionRange(IReadOnlyCollection<int> rangeGridIndices, BattleExecutionRangeType rangeType)
    {
        if (gridManager == null)
            return;

        gridManager.ShowExecutionRange(rangeGridIndices, rangeType);
    }

    private static BattleExecutionRangeType GetPlayerExecutionRangeType(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return BattleExecutionRangeType.AttackOrDebuff;

        if (BattleEquipmentEffectService.IsMoveCommand(command) || IsMoveSkill(command.SkillData))
            return BattleExecutionRangeType.Move;

        if (command.SkillData.SkillType == SkillType.Buff ||
            command.SkillData.TimelineNotation == TimelineActionType.Buff ||
            command.SkillData.TimelineNotation == TimelineActionType.Heal)
            return BattleExecutionRangeType.Buff;

        return BattleExecutionRangeType.AttackOrDebuff;
    }

    private static BattleExecutionRangeType GetMonsterExecutionRangeType(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return BattleExecutionRangeType.AttackOrDebuff;

        switch (command.SkillData.TimelineNotation)
        {
            case TimelineActionType.Move:
                return BattleExecutionRangeType.Move;
            case TimelineActionType.Buff:
            case TimelineActionType.Heal:
                return BattleExecutionRangeType.Buff;
            default:
                return BattleExecutionRangeType.AttackOrDebuff;
        }
    }

    private void ClearExecutionRange()
    {
        if (gridManager == null)
            return;

        gridManager.ClearExecutionRange();
    }

    private List<int> BuildPlayerExecutionRange(PlayerReservedCommand command)
    {
        List<int> range = new();

        if (command == null)
            return range;

        AddUniqueRange(range, command.RangeGridIndices);

        if (range.Count <= 0 && command.ReservedMoveGridIndex >= 0)
            AddUnique(range, command.EffectiveVisualMoveGridIndex);

        if (range.Count <= 0 && command.SelectedGridIndex >= 0)
            AddUnique(range, command.SelectedGridIndex);

        return range;
    }

    private List<int> BuildPlayerMoveExecutionRange(
        PlayerReservedCommand command,
        int currentGridIndex)
    {
        List<int> range = new();

        if (command == null || gridManager == null || currentGridIndex < 0)
            return range;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);

        if (!gridManager.IsValidCoord(currentCoord))
            return range;

        Vector2Int reservedOffset = command.ExecutionMoveOffset;

        if (command.VisualMoveSteps != null && command.VisualMoveSteps.Count > 0)
            reservedOffset = GetTotalMoveOffset(command.VisualMoveSteps);

        Vector2Int targetCoord = currentCoord + reservedOffset;

        if (!gridManager.IsValidCoord(targetCoord))
            return range;

        AddUnique(range, gridManager.CoordToIndex(targetCoord));
        return range;
    }

    private List<int> BuildPlayerExecutionRange(PlayerReservedCommand command, int excludeGridIndex)
    {
        List<int> range = BuildPlayerExecutionRange(command);
        RemoveGridIndex(range, excludeGridIndex);
        return range;
    }

    private List<int> BuildMonsterSkillExecutionRange(MonsterReservedCommand command)
    {
        List<int> range = new();

        if (command == null)
            return range;

        AddUniqueRange(range, command.RangeGridIndices);
        return range;
    }

    private List<int> BuildMonsterSkillExecutionRange(MonsterReservedCommand command, int excludeGridIndex)
    {
        List<int> range = BuildMonsterSkillExecutionRange(command);
        RemoveGridIndex(range, excludeGridIndex);
        return range;
    }

    private static void RemoveGridIndex(List<int> range, int gridIndex)
    {
        if (range == null || gridIndex < 0)
            return;

        range.RemoveAll(index => index == gridIndex);
    }

    private List<int> BuildMonsterMoveExecutionRange(
        MonsterUnit monster,
        MonsterReservedCommand command)
    {
        List<int> range = new();

        if (monster == null || command == null || gridManager == null)
            return range;

        Vector2Int moveOffset = GetMonsterMoveOffset(command);

        if (moveOffset == Vector2Int.zero)
            return range;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            Vector2Int currentCoord = gridManager.IndexToCoord(monster.OccupiedGridIndices[i]);
            Vector2Int movedCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(movedCoord))
                continue;

            AddUnique(range, gridManager.CoordToIndex(movedCoord));
        }

        return range;
    }

    private static void AddUniqueRange(List<int> target, IReadOnlyCollection<int> source)
    {
        if (target == null || source == null)
            return;

        foreach (int index in source)
            AddUnique(target, index);
    }

    private static void AddUnique(List<int> target, int index)
    {
        if (target == null || index < 0)
            return;

        if (!target.Contains(index))
            target.Add(index);
    }

    private void TryPlaceResidue(int gridIndex)
    {
        if (gridIndex < 0)
            return;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null)
            return;

        controller.TryPlaceEffect(gridIndex, ResidueGridEffectId);
    }

    private bool IsGridEffectBlocked(int gridIndex)
    {
        BattleGridEffectController controller = ResolveGridEffectController();
        return controller != null && controller.IsBlocked(gridIndex);
    }

    /// <summary>
    /// 장애물 효과가 발생하는 칸에서만 이동을 멈추고, 효과 연출 후 남은 경로를 이동합니다.
    /// 장애물 사이의 이동은 통과하는 칸 수에 비례한 시간으로 연속적으로 처리합니다.
    /// </summary>
    private IEnumerator MovePlayerThroughEnteredGrids(
        BattleCharacter character,
        string characterId,
        IReadOnlyList<int> enteredGridIndices)
    {
        if (character == null || character.RuntimeData == null ||
            enteredGridIndices == null || enteredGridIndices.Count == 0)
            yield break;

        BattleGridEffectController controller = ResolveGridEffectController();
        BattleUnitAnimator animator = character.GetComponent<BattleUnitAnimator>();
        int startGridIndex = character.CurrentGridIndex;
        int segmentStart = 0;
        int totalSteps = enteredGridIndices.Count;

        for (int i = 0; i < totalSteps; i++)
        {
            if (character.RuntimeData == null || character.RuntimeData.IsDead)
                yield break;

            int gridIndex = enteredGridIndices[i];
            bool hasEffect = controller != null && controller.HasEffect(gridIndex);
            bool atDestination = i == totalSteps - 1;
            if (!hasEffect && !atDestination)
                continue;

            int previousGridIndex = character.CurrentGridIndex;
            if (gridIndex != previousGridIndex)
            {
                Vector2Int previousCoord = gridManager.IndexToCoord(previousGridIndex);
                Vector2Int nextCoord = gridManager.IndexToCoord(gridIndex);
                // Increase travel time gently for longer movement without slowing every cell equally.
                float segmentDuration = GetMoveAnimationDuration(i - segmentStart + 1);
                yield return MoveTransformSmooth(
                    character.transform,
                    character.transform.position,
                    gridManager.GetWorldPositionByIndex(gridIndex),
                    segmentDuration);

                character.SetGridIndex(gridIndex);
                character.RuntimeData.SetLastMoveOffset(nextCoord - previousCoord);
                BattleEquipmentEffectService.MarkMovedBeforeNextAttack(character.RuntimeData);
                UpdatePartyGridIndex(characterId, gridIndex);
            }

            segmentStart = i + 1;
            if (!hasEffect)
                continue;

            // ApplyToPlayer plays the hit animation itself when damage occurs.
            BattleGridEffectApplyResult result = controller.ApplyToPlayer(gridIndex, character);
            if (character.RuntimeData == null || character.RuntimeData.IsDead)
                yield break;

            if (result != null && result.Applied && i < totalSteps - 1)
            {
                // Allow the obstacle's hit/status feedback to appear before moving again.
                yield return new WaitForSeconds(GridEffectResumeDelay / GetActivePresentationSpeed());
                if (animator != null)
                    animator.PlayMove();
            }
        }
    }

    private void ApplyGridEffectsToPlayer(
        IReadOnlyList<int> gridIndices,
        BattleCharacter character)
    {
        if (gridIndices == null || character == null || character.RuntimeData == null)
            return;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null)
            return;

        for (int i = 0; i < gridIndices.Count; i++)
        {
            controller.ApplyToPlayer(gridIndices[i], character);

            if (character.RuntimeData.IsDead)
                break;
        }
    }

    private void ApplyGridEffectsToMonster(
        IReadOnlyList<int> gridIndices,
        MonsterUnit monster)
    {
        if (gridIndices == null || monster == null || monster.RuntimeData == null)
            return;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null)
            return;

        for (int i = 0; i < gridIndices.Count; i++)
        {
            controller.ApplyToMonster(gridIndices[i], monster);

            if (monster.RuntimeData.IsDead)
                break;
        }

        if (monster.RuntimeData.IsDead)
            deathService.HandleMonsterDead(monster);
    }

    private BattleGridEffectController ResolveGridEffectController()
    {
        if (gridEffectController != null)
            return gridEffectController;

        gridEffectController = Object.FindFirstObjectByType<BattleGridEffectController>(
            FindObjectsInactive.Include
        );

        return gridEffectController;
    }

    private IEnumerator ExecutePlayerMove(PlayerReservedCommand command)
    {
        BattleCharacter character = unitFinder.FindBattleCharacter(command.CharacterId);

        if (character == null)
            yield break;

        if (character.RuntimeData == null || character.RuntimeData.IsDead)
            yield break;

        int currentGridIndex = character.CurrentGridIndex;

        if (currentGridIndex < 0)
            yield break;

        if (IsConsumedVisualSkipMove(command))
        {
            command.SetExecutedMoveDistance(0);
            hudService.RefreshHUDs();
            yield break;
        }

        ShowExecutionRange(BuildPlayerMoveExecutionRange(command, currentGridIndex), BattleExecutionRangeType.Move);
        Transform directCameraUnit = null;

        try
        {
            ConsumePlayerMoveCost(command, character);

            bool useVisualMove = TryGetPlayerVisualMoveTargetGridIndex(
                command,
                currentGridIndex,
                out int targetGridIndex,
                out Vector2Int moveOffset
            );

            if (!useVisualMove)
                moveOffset = command.ExecutionMoveOffset;

            List<int> enteredGridIndices = new();
            List<int> blockingUnitGridIndices = new();

            if (command.VisualMoveSteps != null && command.VisualMoveSteps.Count > 1)
            {
                yield return ExecutePlayerVisualMoveSteps(command, character, currentGridIndex);
                yield break;
            }

            if (moveOffset == Vector2Int.zero)
            {
                command.SetExecutedMoveDistance(0);
                ApplyPlayerMoveFacing(character, command.Direction, moveOffset);
                hudService.RefreshHUDs();
                yield return new WaitForSeconds(GetActiveActionBeatDelay());
                yield break;
            }

            ApplyPlayerMoveFacing(character, command.Direction, moveOffset);

            bool foundMoveTarget = useVisualMove && command.VisualMoveSteps != null
                ? TryGetPlayerMoveTargetGridIndex(
                    currentGridIndex,
                    command.VisualMoveSteps,
                    command.CharacterId,
                    out targetGridIndex,
                    enteredGridIndices,
                    blockingUnitGridIndices)
                : TryGetPlayerMoveTargetGridIndex(
                    currentGridIndex,
                    moveOffset,
                    command.CharacterId,
                    out targetGridIndex,
                    enteredGridIndices,
                    blockingUnitGridIndices);

            if (!foundMoveTarget)
            {
                command.SetExecutedMoveDistance(0);
                hudService.RefreshHUDs();
                Debug.LogWarning($"[BattleActionRunner] Player Move Blocked / {command.CharacterId} / Offset:{moveOffset}");
                yield break;
            }

            RecordPlayerMoveExecutionDistance(command, currentGridIndex, targetGridIndex);

            Vector2Int startCoord = gridManager.IndexToCoord(currentGridIndex);
            Vector2Int resolvedCoord = gridManager.IndexToCoord(targetGridIndex);
            Vector2Int actualMoveOffset = resolvedCoord - startCoord;

            if (targetGridIndex == currentGridIndex)
            {
                ApplyPlayerMoveCollisionOnce(blockingUnitGridIndices, command.CharacterId);

                hudService.RefreshHUDs();
                yield return new WaitForSeconds(GetActiveActionBeatDelay());
                yield break;
            }

            BattleUnitAnimator animator = character.GetComponent<BattleUnitAnimator>();
            if (animator != null)
                animator.PlayMove();

            if (BattleCameraController.Instance != null)
            {
                directCameraUnit = character.transform;
                BattleCameraController.Instance.BeginDirectGridMovement(directCameraUnit);
            }

            // Apply each grid effect only after the character visibly reaches that tile.
            yield return MovePlayerThroughEnteredGrids(character, command.CharacterId, enteredGridIndices);
            ApplyPlayerBleedForMovedCells(character, enteredGridIndices.Count);

            ApplyPlayerMoveCollisionOnce(blockingUnitGridIndices, command.CharacterId);

            hudService.RefreshHUDs();

            yield return new WaitForSeconds(GetActiveActionBeatDelay());
        }
        finally
        {
            if (directCameraUnit != null && BattleCameraController.Instance != null)
                BattleCameraController.Instance.EndDirectGridMovement(directCameraUnit);
            ClearExecutionRange();
        }
    }

    private IEnumerator ExecutePlayerVisualMoveSteps(
        PlayerReservedCommand command,
        BattleCharacter character,
        int startGridIndex)
    {
        int currentGridIndex = startGridIndex;
        int executedDistance = 0;
        BattleUnitAnimator animator = character.GetComponent<BattleUnitAnimator>();
        List<List<Vector2Int>> executionStepGroups =
            PlayerReservedCommand.BuildMoveExecutionSegments(command.VisualMoveSteps);
        bool cameraTrackingStarted = false;

        try
        {
            for (int i = 0; i < executionStepGroups.Count; i++)
            {
                IReadOnlyList<Vector2Int> stepGroup = executionStepGroups[i];
                Vector2Int stepOffset = GetTotalMoveOffset(stepGroup);

                if (stepOffset == Vector2Int.zero)
                {
                    ApplyPlayerMoveFacing(character, command.Direction, stepOffset);
                    continue;
                }

                List<int> enteredGridIndices = new();
                List<int> blockingUnitGridIndices = new();

                if (!TryGetPlayerMoveTargetGridIndex(
                    currentGridIndex,
                    stepGroup,
                    command.CharacterId,
                    out int targetGridIndex,
                    enteredGridIndices,
                    blockingUnitGridIndices))
                {
                    continue;
                }

                Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);
                Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);
                Vector2Int actualOffset = targetCoord - currentCoord;

                if (targetGridIndex == currentGridIndex)
                {
                    ApplyPlayerMoveCollisionOnce(blockingUnitGridIndices, command.CharacterId);
                    continue;
                }

                ApplyPlayerMoveFacing(character, command.Direction, actualOffset);

                if (animator != null)
                    animator.PlayMove();

                if (!cameraTrackingStarted && BattleCameraController.Instance != null)
                {
                    BattleCameraController.Instance.BeginDirectGridMovement(character.transform);
                    cameraTrackingStarted = true;
                }

                yield return MovePlayerThroughEnteredGrids(character, command.CharacterId, enteredGridIndices);
                ApplyPlayerBleedForMovedCells(character, enteredGridIndices.Count);
                int reachedGridIndex = character.CurrentGridIndex;
                executedDistance += GetMoveDistance(
                    gridManager.IndexToCoord(reachedGridIndex) - currentCoord);
                currentGridIndex = reachedGridIndex;

                ApplyPlayerMoveCollisionOnce(blockingUnitGridIndices, command.CharacterId);

                if (character.RuntimeData == null || character.RuntimeData.IsDead)
                    break;
            }
        }
        finally
        {
            if (cameraTrackingStarted && BattleCameraController.Instance != null)
                BattleCameraController.Instance.EndDirectGridMovement(character.transform);
        }

        command.SetExecutedMoveDistance(executedDistance);

        hudService.RefreshHUDs();
        yield return new WaitForSeconds(GetActiveActionBeatDelay());
    }

    private static Vector2Int GetTotalMoveOffset(IReadOnlyList<Vector2Int> moveSteps)
    {
        Vector2Int total = Vector2Int.zero;

        if (moveSteps == null)
            return total;

        for (int i = 0; i < moveSteps.Count; i++)
            total += moveSteps[i];

        return total;
    }

    private static int GetMoveDistance(Vector2Int moveOffset)
    {
        return Mathf.Abs(moveOffset.x) + Mathf.Abs(moveOffset.y);
    }

    private bool IsConsumedVisualSkipMove(PlayerReservedCommand command)
    {
        if (command == null || !command.SkipMoveVisual)
            return false;

        BattleCharacter character = unitFinder.FindBattleCharacter(command.CharacterId);

        return character != null &&
               command.IsVisualSkipConsumedAtGrid(character.CurrentGridIndex);
    }

    private bool TryGetPlayerVisualMoveTargetGridIndex(
        PlayerReservedCommand command,
        int currentGridIndex,
        out int targetGridIndex,
        out Vector2Int visualMoveOffset)
    {
        targetGridIndex = currentGridIndex;
        visualMoveOffset = Vector2Int.zero;

        if (command == null || gridManager == null || command.ReservedMoveGridIndex < 0)
            return false;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);

        if (!gridManager.IsValidCoord(currentCoord))
            return false;

        // 예약 당시의 절대 목적지가 아니라 실행 순간의 실제 위치를 원점으로 사용한다.
        // 넉백/그랩으로 위치가 달라져도 예약된 이동 방향과 이동량은 그대로 유지한다.
        visualMoveOffset = command.ExecutionMoveOffset;

        if (command.VisualMoveSteps != null && command.VisualMoveSteps.Count > 0)
            visualMoveOffset = GetTotalMoveOffset(command.VisualMoveSteps);

        Vector2Int targetCoord = currentCoord + visualMoveOffset;

        if (!gridManager.IsValidCoord(targetCoord))
            return false;

        targetGridIndex = gridManager.CoordToIndex(targetCoord);
        return true;
    }

    private bool TryGetPlayerMoveTargetGridIndex(
        int currentGridIndex,
        Vector2Int moveOffset,
        string characterId,
        out int targetGridIndex,
        List<int> enteredGridIndices = null,
        List<int> blockingUnitGridIndices = null)
    {
        targetGridIndex = currentGridIndex;

        if (gridManager == null)
            return false;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);

        if (!gridManager.IsValidCoord(currentCoord))
            return false;

        if (moveOffset == Vector2Int.zero)
            return true;

        bool reachedTarget = true;

        if (!TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.x, true, characterId, enteredGridIndices, blockingUnitGridIndices))
            reachedTarget = false;

        if (reachedTarget &&
            !TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.y, false, characterId, enteredGridIndices, blockingUnitGridIndices))
        {
            reachedTarget = false;
        }

        targetGridIndex = gridManager.CoordToIndex(currentCoord);
        return true;
    }

    private bool TryGetPlayerMoveTargetGridIndex(
        int currentGridIndex,
        IReadOnlyList<Vector2Int> moveSteps,
        string characterId,
        out int targetGridIndex,
        List<int> enteredGridIndices = null,
        List<int> blockingUnitGridIndices = null)
    {
        targetGridIndex = currentGridIndex;

        if (gridManager == null || moveSteps == null || moveSteps.Count <= 0)
            return false;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);

        if (!gridManager.IsValidCoord(currentCoord))
            return false;

        for (int i = 0; i < moveSteps.Count; i++)
        {
            Vector2Int moveOffset = moveSteps[i];

            if (!TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.x, true, characterId, enteredGridIndices, blockingUnitGridIndices))
                break;

            if (!TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.y, false, characterId, enteredGridIndices, blockingUnitGridIndices))
                break;
        }

        targetGridIndex = gridManager.CoordToIndex(currentCoord);
        return true;
    }

    private bool TryApplyPlayerMoveAxisStep(
        ref Vector2Int currentCoord,
        int amount,
        bool horizontal,
        string characterId,
        List<int> enteredGridIndices = null,
        List<int> blockingUnitGridIndices = null)
    {
        int remaining = amount;

        while (remaining != 0)
        {
            int step = remaining > 0 ? 1 : -1;
            Vector2Int nextCoord = currentCoord + (horizontal
                ? new Vector2Int(step, 0)
                : new Vector2Int(0, step));

            if (!gridManager.IsValidCoord(nextCoord))
                return false;

            int gridIndex = gridManager.CoordToIndex(nextCoord);

            if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex, characterId))
            {
                // 경로 계산에서는 피해를 발생시키지 않는다.
                // 실제 실행 흐름에서 이 충돌 지점을 한 번만 처리한다.
                AddUnique(blockingUnitGridIndices, gridIndex);
                return false;
            }

            if (IsGridEffectBlocked(gridIndex))
                return false;

            currentCoord = nextCoord;
            AddUnique(enteredGridIndices, gridIndex);
            remaining -= step;
        }

        return true;
    }

    private void ApplyPlayerMoveCollisionOnce(
        IReadOnlyList<int> blockingUnitGridIndices,
        string movingCharacterId)
    {
        if (blockingUnitGridIndices == null || blockingUnitGridIndices.Count <= 0)
            return;

        int blockingGridIndex = blockingUnitGridIndices[0];
        if (blockingGridIndex < 0)
            return;

        ApplyCrashToBlockingUnitAtGrid(blockingGridIndex, movingCharacterId, null);
    }

    private void ApplyPlayerMoveFacing(
        BattleCharacter character,
        BattleDirection direction,
        Vector2Int moveOffset)
    {
        ApplyPlayerMoveFacing(character, direction, moveOffset, false);
    }

    private void ApplyPlayerMoveFacing(
        BattleCharacter character,
        BattleDirection direction,
        Vector2Int moveOffset,
        bool preserveFacing)
    {
        if (character == null || preserveFacing)
            return;

        BattleUnitFacing facing = character.GetComponent<BattleUnitFacing>();

        if (facing != null)
        {
            if (moveOffset.x != 0)
                facing.FaceByMoveOffset(moveOffset);
            else
                facing.FaceRight(direction == BattleDirection.Right);

            if (character.RuntimeData != null)
                character.RuntimeData.Direction = facing.GetBattleDirection();

            return;
        }

        if (character.RuntimeData != null)
            character.RuntimeData.Direction = direction;
    }

    private void ConsumePlayerMoveCost(PlayerReservedCommand command, BattleCharacter character)
    {
        if (command == null || character == null || character.RuntimeData == null)
            return;

        if (command.MoveCostConsumed)
            return;

        int cost = Mathf.Max(0, command.Cost);

        if (cost > 0)
        {
            character.RuntimeData.RemoveReservedCost(cost);
            character.RuntimeData.CurrentCost = Mathf.Max(
                0,
                character.RuntimeData.CurrentCost - cost
            );
        }

        command.MarkMoveCostConsumed();
    }

    private void RecordPlayerMoveExecutionDistance(
        PlayerReservedCommand command,
        int startGridIndex,
        int targetGridIndex)
    {
        if (command == null || gridManager == null)
            return;

        Vector2Int startCoord = gridManager.IndexToCoord(startGridIndex);
        Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

        if (!gridManager.IsValidCoord(startCoord) || !gridManager.IsValidCoord(targetCoord))
        {
            command.SetExecutedMoveDistance(0);
            return;
        }

        Vector2Int actualOffset = targetCoord - startCoord;
        command.SetExecutedMoveDistance(
            Mathf.Abs(actualOffset.x) + Mathf.Abs(actualOffset.y)
        );
    }

    private IEnumerator ExecutePlayerSkill(PlayerReservedCommand command)
    {
        playerAttackPlaneStartedForCurrentSkill = false;
        BattleCharacter attacker = unitFinder.FindBattleCharacter(command.CharacterId);

        if (attacker == null)
            yield break;

        if (attacker.RuntimeData == null || attacker.RuntimeData.IsDead)
            yield break;

        BattleDirection executionDirection =
            BattleAttackExecutionDirectionPolicy.ResolveExecutionDirection(
                command.Direction,
                attacker.RuntimeData.Direction);
        command.SetExecutionDirection(executionDirection);

        // 공격 범위, 이동 효과와 연출이 모두 실행 직전 전투 상태 방향을 공유합니다.
        BattleUnitFacing attackFacing = attacker.GetComponent<BattleUnitFacing>();
        if (attackFacing != null)
            attackFacing.FaceRight(executionDirection == BattleDirection.Right);
        attacker.RuntimeData.Direction = executionDirection;

        if (onPlayerSkillStarted != null)
        {
            IEnumerator skillStartPresentation = onPlayerSkillStarted.Invoke(command);

            if (skillStartPresentation != null)
                yield return skillStartPresentation;
        }

        if (BattleSkillNamePopupPresentation.ShouldShow(command))
        {
            BattleSkillTextPopupUI.Show(
                attacker.transform,
                BattleSkillNamePopupPresentation.GetDisplayName(command));
        }

        // E_Move가 첫 피해 효과보다 먼저인 Direction 스킬은 예약 단계에서 이미
        // 이동 예정 위치 기준 범위를 계산했습니다. 실행 시작 시 이동 전 위치로
        // 다시 덮어쓰지 않고, 실제 이동 결과가 나온 뒤에만 범위를 재판정합니다.
        if (!HasDirectionalMoveBeforeFirstDamage(command))
            RecalculatePlayerSkillRangeAtExecution(attacker, command);

        ShowExecutionRange(BuildPlayerExecutionRange(command), GetPlayerExecutionRangeType(command));

        bool shouldControlAttackPlane = ShouldControlAttackPlaneForPlayerSkill(command);
        bool attackActionStarted = false;

        try
        {
            ConsumePlayerSkillCost(command, attacker);

            if (attacker.RuntimeData == null || attacker.RuntimeData.IsDead)
            {
                hudService.RefreshHUDs();
                yield break;
            }

            // Consume once after the attack action, including misses and zero damage.
            // Keep the status through the action so all hits use the same lifesteal effect.
            attackActionStarted = command.SkillData != null &&
                command.SkillData.SkillType == SkillType.Attack;

            BattleUnitAnimator attackerAnimator = attacker.GetComponent<BattleUnitAnimator>();

            if (ShouldPlayerSkillTargetPlayerParty(command))
            {
                if (ShouldEnterCameraForActiveAction() && BattleCameraController.Instance != null)
                {
                    List<Transform> recipients = new();
                    BattleCharacter[] focusCharacters = Object.FindObjectsByType<BattleCharacter>(
                        FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    foreach (BattleCharacter candidate in focusCharacters)
                    {
                        if (candidate != null && candidate.RuntimeData != null &&
                            !candidate.RuntimeData.IsDead &&
                            command.RangeGridIndices != null &&
                            command.RangeGridIndices.Contains(candidate.CurrentGridIndex))
                            recipients.Add(candidate.transform);
                    }
                    yield return BattleCameraController.Instance.ZoomForActionTargets(
                        attacker.transform, recipients, IsPlayerStatusSkill(command),
                        ShouldSkipPlayerSkillCamera(command));
                }

                if (attackerAnimator != null)
                {
                    attackerAnimator.PlaySkillAction(command);

                    float prepareWaitDuration = attackerAnimator.LastScheduledPrepareWaitDuration;
                    if (prepareWaitDuration > 0f)
                        yield return new WaitForSeconds(prepareWaitDuration);
                }

                // Prepare가 아니라 실제 Action 애니메이션이 시작되는 시점에 기울입니다.
                BeginPlayerAttackPlaneAtActionStart(attacker, command);

                if (attacker.RuntimeData == null || attacker.RuntimeData.IsDead)
                {
                    hudService.RefreshHUDs();

                    yield return ReturnCameraAfterActiveActionIfNeeded();

                    yield break;
                }

                BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

                bool allowTargetUnitPlayOnceCue = true;

                for (int i = 0; i < characters.Length; i++)
                {
                    BattleCharacter target = characters[i];

                    if (target == null || target.RuntimeData == null)
                        continue;

                    if (target.RuntimeData.IsDead)
                        continue;

                    if (!command.RangeGridIndices.Contains(target.CurrentGridIndex))
                        continue;

                    bool spawnedTargetUnitVfx = PlayTargetUnitVfxOnPlayerTarget(
                        attackerAnimator,
                        command,
                        target,
                        allowPlayOncePerActionCues: allowTargetUnitPlayOnceCue);

                    if (spawnedTargetUnitVfx)
                        allowTargetUnitPlayOnceCue = false;

                    ExecutePlayerSkillEffectsToPlayer(attacker, target, command);
                }

                hudService.RefreshHUDs();
                // 범위에 아군이 없으면 줌 펄스를 생략하고 미적중 속도로 진행합니다.
                if (IsPlayerStatusSkill(command) && HasPlayerPartyTarget(command))
                    yield return PlayStatusEffectCameraFeedback();
                else
                    yield return new WaitForSeconds(GetActiveActionBeatDelay());

                yield return ReturnCameraAfterActiveActionIfNeeded();

                yield break;
            }

            if (ShouldPlayerSkillTargetSelf(command))
            {
                if (ShouldEnterCameraForActiveAction() && BattleCameraController.Instance != null)
                {
                    if (IsPlayerStatusSkill(command))
                        yield return BattleCameraController.Instance.ZoomForStatusSkill(attacker.transform);
                    else if (ShouldSkipPlayerSkillCamera(command))
                        yield return BattleCameraController.Instance.ZoomForRangedSkill(attacker.transform);
                }

                if (attackerAnimator != null)
                {
                    attackerAnimator.PlaySkillAction(command);

                    float prepareWaitDuration = attackerAnimator.LastScheduledPrepareWaitDuration;
                    if (prepareWaitDuration > 0f)
                        yield return new WaitForSeconds(prepareWaitDuration);
                }

                // Prepare가 아니라 실제 Action 애니메이션이 시작되는 시점에 기울입니다.
                BeginPlayerAttackPlaneAtActionStart(attacker, command);

                if (attacker.RuntimeData == null || attacker.RuntimeData.IsDead)
                {
                    hudService.RefreshHUDs();

                    yield return ReturnCameraAfterActiveActionIfNeeded();

                    yield break;
                }

                PlayTargetUnitVfxOnPlayerTarget(
                    attackerAnimator,
                    command,
                    attacker,
                    allowPlayOncePerActionCues: true);

                ExecutePlayerSkillEffectsToPlayer(attacker, attacker, command);

                hudService.RefreshHUDs();
                if (IsPlayerStatusSkill(command))
                    yield return PlayStatusEffectCameraFeedback();
                else
                    yield return new WaitForSeconds(GetActiveActionBeatDelay());

                yield return ReturnCameraAfterActiveActionIfNeeded();

                yield break;
            }

            MonsterUnit[] monsters =
                Object.FindObjectsByType<MonsterUnit>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            List<MonsterUnit> hitTargets = new();

            for (int i = 0; i < monsters.Length; i++)
            {
                MonsterUnit monster = monsters[i];

                if (monster == null || monster.RuntimeData == null)
                    continue;

                if (!IsMonsterInRange(monster, command))
                    continue;

                hitTargets.Add(monster);
            }

            List<int> gridEffectTargets = BuildDamageableGridEffectTargets(command);

            if (ShouldEnterCameraForActiveAction() && BattleCameraController.Instance != null)
            {
                List<Transform> focusTargets = new();
                foreach (MonsterUnit target in hitTargets)
                    if (target != null && target.RuntimeData != null && !target.RuntimeData.IsDead)
                        focusTargets.Add(target.transform);

                yield return BattleCameraController.Instance.ZoomForActionTargets(
                    attacker.transform, focusTargets, IsPlayerStatusSkill(command),
                    ShouldSkipPlayerSkillCamera(command));
            }

            if (attacker.RuntimeData == null || attacker.RuntimeData.IsDead)
            {
                hudService.RefreshHUDs();

                yield return ReturnCameraAfterActiveActionIfNeeded();

                yield break;
            }

            // 대상이 없는 공격도 효과 처리 루틴을 실행해야 Count만큼의
            // 공격 모션을 모두 재생할 수 있습니다.
            yield return ExecutePlayerSkillEffectsToMonsters(
                attacker,
                hitTargets,
                gridEffectTargets,
                command,
                attackerAnimator);

            // 범위에 실제 대상이 없는 버프/디버프는 공격 미적중과 동일하게
            // 짧게 종료합니다. 대상이 있다면 기존 줌 펄스와 연출 속도를 유지합니다.
            if (IsPlayerStatusSkill(command) && hitTargets.Count > 0)
                yield return PlayStatusEffectCameraFeedback();

            hudService.RefreshHUDs();

            yield return ReturnCameraAfterActiveActionIfNeeded();
        }
        finally
        {
            if (attackActionStarted)
            {
                BattleEquipmentEffectService.ConsumePlayerLifestealOnAttack(
                    attacker != null ? attacker.RuntimeData : command.UserRuntime);
                // Elasticity is consumed as a whole after one attack action, not once per hit.
                ConsumeElasticityOnAttack(
                    attacker != null ? attacker.RuntimeData : command.UserRuntime);
            }

            EndAttackPlaneFeedbackIfNeeded(shouldControlAttackPlane);
            playerAttackPlaneStartedForCurrentSkill = false;
            BattleEquipmentEffectService.ClearMoveFirstAttackPowerIfAttack(
                attacker != null ? attacker.RuntimeData : command.UserRuntime,
                command.SkillData);
            ClearExecutionRange();
        }
    }

    private void RecalculatePlayerSkillRangeAtExecution(
    BattleCharacter attacker,
    PlayerReservedCommand command)
    {
        if (attacker == null || command == null || command.SkillData == null)
            return;

        if (command.ReservedMoveGridIndex >= 0)
            return;

        if (gridManager == null || DataManager.Instance == null)
            return;

        BattleDirection direction = attacker.RuntimeData != null
            ? attacker.RuntimeData.Direction
            : command.Direction;

        string rangeId =
            BattleEquipmentEffectService.GetEffectiveRangeId(attacker.RuntimeData, command.SkillData);

        if (BattleRangeCalculator.IsPartyRangeId(rangeId))
        {
            List<int> partyGridIndices = new();
            BattleCharacter[] partyCharacters = Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < partyCharacters.Length; i++)
            {
                BattleCharacter character = partyCharacters[i];

                if (character == null ||
                    character.RuntimeData == null ||
                    character.RuntimeData.IsDead ||
                    character.CurrentGridIndex < 0)
                {
                    continue;
                }

                if (!partyGridIndices.Contains(character.CurrentGridIndex))
                    partyGridIndices.Add(character.CurrentGridIndex);
            }

            command.SetSimulatedRangeResult(partyGridIndices, partyGridIndices);
            return;
        }

        if (command.SkillData.RangeType == RangeType.None)
            return;

        if (!BattleRangeCalculator.IsAllRangeId(rangeId) &&
            DataManager.Instance.RangeDatabase == null)
        {
            return;
        }

        List<int> rangeGridIndices = new();

        if (command.SkillData.RangeType == RangeType.Direction)
        {
            rangeGridIndices = BattleRangeCalculator.GetDirectionRangeIndices(
                attacker.CurrentGridIndex,
                rangeId,
                direction,
                DataManager.Instance.RangeDatabase,
                gridManager
            );

            command.SetDirectionResult(
                direction,
                rangeGridIndices,
                rangeGridIndices
            );

            return;
        }

        if (command.SkillData.RangeType == RangeType.Selection)
        {
            if (IsMoveSkill(command.SkillData) || command.SelectedGridIndex < 0)
                return;

            rangeGridIndices = BattleRangeCalculator.GetSelectionRangeIndices(
                command.SelectedGridIndex,
                rangeId,
                DataManager.Instance.RangeDatabase,
                gridManager
            );

            command.SetSelectionAreaResult(
                direction,
                command.SelectedGridIndex,
                rangeGridIndices
            );
        }
    }

    private static bool IsMoveSkill(SkillMasterData skillData)
    {
        if (skillData == null)
            return false;

        return skillData.Category == Category.Move ||
               skillData.TimelineNotation == TimelineActionType.Move ||
               skillData.SkillId == "S_Move_1";
    }

    private IEnumerator ExecutePlayerSkillEffectsToMonsters(
        BattleCharacter caster,
        List<MonsterUnit> monsterTargets,
        List<int> gridEffectTargets,
        PlayerReservedCommand command,
        BattleUnitAnimator attackerAnimator)
    {
        if (caster == null || command == null || command.SkillData == null)
            yield break;

        if (command.SkillData.EffectEntries == null || command.SkillData.EffectEntries.Count == 0)
        {
            Debug.LogWarning($"[PlayerSkillEffect] EffectEntries 없음 / Skill:{command.SkillData.SkillId}");

            if (attackerAnimator != null)
            {
                attackerAnimator.PlaySkillAction(command);
                float prepareWaitDuration = attackerAnimator.LastScheduledPrepareWaitDuration;
                if (prepareWaitDuration > 0f)
                    yield return new WaitForSeconds(prepareWaitDuration);
                BeginPlayerAttackPlaneAtActionStart(caster, command);
                yield return PlayPlayerSkillTargetVfxIfNeeded(attackerAnimator, command);
            }

            yield return new WaitForSeconds(GetActiveActionBeatDelay());
            yield break;
        }

        // Snapshot at action start; an effect granted during this skill must not
        // retroactively strengthen its already-reserved attack.
        int elasticityDamage = command.SkillData.SkillType == SkillType.Attack
            ? GetElasticityStack(caster.RuntimeData?.StatusEffects)
            : 0;
        bool elasticityEligible = false;
        bool playedDamageSequence = false;
        bool playedActionForNonDamage = false;
        bool hasForcedMoveVisual = false;
        HashSet<MonsterUnit> draugrCounterCandidates = new();
        BattleRearAttackActionSnapshot rearAttackSnapshot = new();

        for (int i = 0; i < command.SkillData.EffectEntries.Count; i++)
        {
            SkillEffectEntry entry = command.SkillData.EffectEntries[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.EffectId))
                continue;

            string effectId = BattleEquipmentEffectService.GetEffectivePlayerDamageEffectId(
                command.UserRuntime,
                command,
                entry.EffectId);

            if (TryGetExplicitPlayerDirectionalMoveDistance(
                    command,
                    effectId,
                    i,
                    out int directionalMoveDistance))
            {
                yield return ExecutePlayerDirectionalMoveEffect(
                    caster,
                    command,
                    directionalMoveDistance,
                    effectId);

                RefreshPlayerMonsterTargetsAfterDirectionalMove(
                    caster,
                    command,
                    monsterTargets,
                    gridEffectTargets);
                continue;
            }

            int value = GetPlayerEffectValue(command, entry);
            int count = GetPlayerEffectCount(command, entry);

            if (IsPostResolutionEffect(effectId))
                continue;

            if (IsDamageHitEffect(effectId))
            {
                playedDamageSequence = true;
                int resolvedHits = NewSkillEffectRules.ResolveDirectDamageHitCount(effectId, count);
                if (effectId == "E_StrikeCountByDebuff")
                    resolvedHits = NewSkillEffectRules.ResolveConditionalHitCount(
                        count, GetMaximumTargetHarmfulEffectTypes(monsterTargets));
                elasticityEligible |= resolvedHits > 1;

                yield return ExecutePlayerDamageHitSequence(
                    caster,
                    monsterTargets,
                    gridEffectTargets,
                    command,
                    effectId,
                    value,
                    count,
                    attackerAnimator,
                    draugrCounterCandidates,
                    rearAttackSnapshot);

                continue;
            }

            if (!playedDamageSequence && !playedActionForNonDamage)
            {
                if (attackerAnimator != null)
                {
                    attackerAnimator.PlaySkillAction(command);
                    float prepareWaitDuration = attackerAnimator.LastScheduledPrepareWaitDuration;
                    if (prepareWaitDuration > 0f)
                        yield return new WaitForSeconds(prepareWaitDuration);
                    BeginPlayerAttackPlaneAtActionStart(caster, command);
                    yield return PlayPlayerSkillTargetVfxIfNeeded(attackerAnimator, command);
                }

                playedActionForNonDamage = true;
                yield return new WaitForSeconds(GetActiveActionBeatDelay());

                PlayTargetUnitVfxOnMonsterTargets(
                    attackerAnimator,
                    command,
                    monsterTargets,
                    allowPlayOncePerActionCues: true);
            }

            ExecutePlayerNonDamageEffectToMonsters(
                caster,
                monsterTargets,
                command,
                effectId,
                value,
                count);

            if (effectId == "E_Knockback" || effectId == "E_Grab")
                hasForcedMoveVisual = true;
        }

        // Elasticity is a single independent fixed-damage hit AFTER the entire
        // multi-hit sequence. Do not modify each hit's normal damage value.
        if (elasticityEligible && elasticityDamage > 0 && monsterTargets != null)
        {
            foreach (MonsterUnit monster in monsterTargets)
            {
                if (!IsAliveMonsterTarget(monster))
                    continue;

                int hpBefore = monster.RuntimeData.CurrentHP;
                BattleEffectUtility.StatusDamageMonster(monster, elasticityDamage);
                int dealtDamage = Mathf.Max(0, hpBefore - monster.RuntimeData.CurrentHP);
                if (dealtDamage > 0)
                {
                    bool killed = monster.RuntimeData.IsDead;
                    if (!killed && HasActiveMonsterCounter(monster.RuntimeData))
                        draugrCounterCandidates.Add(monster);
                    if (caster.RuntimeData != null)
                        BattleRunStatisticsRecorder.RecordDamageDealt(
                            caster.RuntimeData.CharacterId, dealtDamage, killed);
                    if (statusEffectService.ApplySplitHitAndCheckTrigger(monster))
                        deathService.HandleMuckSplit(monster);
                    else if (killed)
                        deathService.HandleMonsterDead(monster);
                }
            }
            hudService.RefreshHUDs();
        }

        ExecutePlayerPostResolutionEffects(caster, command);

        // 드라우그 반격은 공격 스킬의 모든 효과가 끝난 뒤 실행합니다.
        // 넉백/그랩이 포함된 경우 강제이동과 충돌 처리가 먼저 끝나므로,
        // 반격은 이동이 완료된 실제 드라우그 위치를 기준으로 판정됩니다.
        if (draugrCounterCandidates.Count > 0)
        {
            // 넉백/그랩은 논리 그리드 위치를 먼저 갱신한 뒤 별도 코루틴으로 시각 이동을 진행합니다.
            // 드라우그 반격 애니메이션은 해당 이동 연출까지 끝난 뒤 시작해야 화면 위치가 튀지 않습니다.
            if (hasForcedMoveVisual)
                yield return new WaitForSeconds(
                    ForcedMoveVisualCompletionDelay / GetActivePresentationSpeed());

            yield return ExecuteDraugrCounters(caster, draugrCounterCandidates);
        }

        if (!playedDamageSequence && !playedActionForNonDamage)
        {
            if (attackerAnimator != null)
            {
                attackerAnimator.PlaySkillAction(command);
                float prepareWaitDuration = attackerAnimator.LastScheduledPrepareWaitDuration;
                if (prepareWaitDuration > 0f)
                    yield return new WaitForSeconds(prepareWaitDuration);
                BeginPlayerAttackPlaneAtActionStart(caster, command);
                yield return PlayPlayerSkillTargetVfxIfNeeded(attackerAnimator, command);
            }

            yield return new WaitForSeconds(GetActiveActionBeatDelay());
        }
    }

    private const string ElasticityEffectId = "E_Elasticity";

    private static int GetElasticityStack(
        List<Relic.Gameplay.Data.StatusEffectRuntimeData> statuses)
    {
        if (statuses == null)
            return 0;
        int amount = 0;
        foreach (var status in statuses)
            if (status != null && status.EffectId == ElasticityEffectId)
                amount += Mathf.Max(0, status.Stack);
        return amount;
    }

    private static void ConsumeElasticityOnAttack(
        Relic.Gameplay.Data.CharacterRuntimeData runtime)
    {
        var statuses = runtime?.StatusEffects;
        if (statuses == null ||
            DataManager.Instance?.EffectDatabase?.Get(ElasticityEffectId)?.DecreaseRule !=
                EffectValueDecreaseRule.DecreaseOnTrigger)
            return;

        // All accumulated stacks are spent by one attack, even a single hit or miss.
        for (int i = statuses.Count - 1; i >= 0; i--)
            if (statuses[i] != null && statuses[i].EffectId == ElasticityEffectId)
                statuses.RemoveAt(i);
    }

    private static bool PlayTargetUnitVfxOnPlayerTarget(
        BattleUnitAnimator attackerAnimator,
        PlayerReservedCommand command,
        BattleCharacter target,
        bool allowPlayOncePerActionCues)
    {
        if (attackerAnimator == null ||
            command == null ||
            command.SkillData == null ||
            target == null ||
            target.RuntimeData == null ||
            target.RuntimeData.IsDead)
        {
            return false;
        }

        return attackerAnimator.PlaySkillTargetUnitVfx(
            command.SkillData,
            target.transform,
            allowPlayOncePerActionCues);
    }

    private static void PlayTargetUnitVfxOnMonsterTargets(
        BattleUnitAnimator attackerAnimator,
        PlayerReservedCommand command,
        IReadOnlyList<MonsterUnit> targets,
        bool allowPlayOncePerActionCues)
    {
        if (attackerAnimator == null ||
            command == null ||
            command.SkillData == null ||
            targets == null ||
            targets.Count <= 0)
        {
            return;
        }

        bool allowPlayOnceCueForNextSpawn = allowPlayOncePerActionCues;

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterUnit target = targets[i];

            if (target == null ||
                target.RuntimeData == null ||
                target.RuntimeData.IsDead)
            {
                continue;
            }

            bool spawned = attackerAnimator.PlaySkillTargetUnitVfx(
                command.SkillData,
                target.transform,
                allowPlayOnceCueForNextSpawn);

            if (spawned)
                allowPlayOnceCueForNextSpawn = false;
        }
    }

    private static IEnumerator PlayPlayerSkillTargetVfxIfNeeded(
        BattleUnitAnimator attackerAnimator,
        PlayerReservedCommand command)
    {
        if (!ShouldPlayPlayerSkillTargetVfx(attackerAnimator, command))
            yield break;

        yield return attackerAnimator.PlaySkillTargetVfx(command);
    }

    private static bool ShouldPlayPlayerSkillTargetVfx(
        BattleUnitAnimator attackerAnimator,
        PlayerReservedCommand command)
    {
        return attackerAnimator != null &&
               command != null &&
               command.SkillData != null &&
               command.SkillData.RangeType == RangeType.Selection &&
               command.SelectedGridIndex >= 0 &&
               command.ReservedMoveGridIndex < 0;
    }

    private IEnumerator ExecutePlayerDamageHitSequence(
        BattleCharacter caster,
        List<MonsterUnit> monsterTargets,
        List<int> gridEffectTargets,
        PlayerReservedCommand command,
        string effectId,
        int value,
        int count,
        BattleUnitAnimator attackerAnimator,
        HashSet<MonsterUnit> draugrCounterCandidates,
        BattleRearAttackActionSnapshot rearAttackSnapshot)
    {
        int hitCount = Mathf.Max(
            1,
            NewSkillEffectRules.ResolveDirectDamageHitCount(effectId, count));
        if (effectId == "E_StrikeCountByDebuff")
            hitCount = NewSkillEffectRules.ResolveConditionalHitCount(
                count,
                GetMaximumTargetHarmfulEffectTypes(monsterTargets));

        int effectParameterCount = NewSkillEffectRules.ResolveDirectDamageParameterCount(
            effectId,
            count);
        bool isMultiHit = hitCount > 1;
        float multiHitAnimationSpeed = GetMultiHitAnimationSpeed(hitCount);

        // 연타 횟수가 많을수록 공격 모션을 더 빠르게 재생해 전체 행동 시간이 과도하게 길어지지 않도록 합니다.
        // 2타=1.25, 3타=1.5, 4타=1.75, 5타 이상=2.0
        if (isMultiHit && attackerAnimator != null)
            attackerAnimator.SetPlaybackSpeed(multiHitAnimationSpeed);

        // 다단 공격의 첫 피격 연출만 허용합니다. 피해 계산과 피해 숫자는 매 타격 유지합니다.
        List<BattleUnitAnimator> monsterHitAnimators = new();
        if (isMultiHit && monsterTargets != null)
        {
            foreach (MonsterUnit target in monsterTargets)
            {
                if (target == null) continue;
                BattleUnitAnimator targetAnimator = target.GetComponent<BattleUnitAnimator>();
                if (targetAnimator != null && !monsterHitAnimators.Contains(targetAnimator))
                    monsterHitAnimators.Add(targetAnimator);
            }
        }

        try
        {
            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                if (caster == null || caster.RuntimeData == null || caster.RuntimeData.IsDead)
                    break;

                if (isMultiHit)
                {
                    foreach (BattleUnitAnimator targetAnimator in monsterHitAnimators)
                        if (targetAnimator != null)
                            targetAnimator.SuppressRepeatedMultiHitReaction = hitIndex > 0;
                }

                if (attackerAnimator == null)
                    BeginPlayerAttackPlaneAtActionStart(caster, command);

                if (attackerAnimator != null)
                {
                    attackerAnimator.PlaySkillAction(command, hitIndex);

                    // 각 행동의 첫 타에 Prepare가 있으면 Runner도 그 시간을 실제 행동 시간으로 보장합니다.
                    // 그래야 연속 행동에서도 a -> b / a -> b처럼 다음 행동의 Prepare가 Action에 덮이지 않습니다.
                    float prepareWaitDuration = attackerAnimator.LastScheduledPrepareWaitDuration;
                    if (prepareWaitDuration > 0f)
                        yield return new WaitForSeconds(prepareWaitDuration);

                    BeginPlayerAttackPlaneAtActionStart(caster, command);

                    // Target VFX도 Prepare가 끝난 뒤 Action 시점에 재생합니다.
                    yield return PlayPlayerSkillTargetVfxIfNeeded(attackerAnimator, command);
                }

                // 연타 내부 타격 간격은 일반 ActionDelay와 분리합니다.
                // 연속행동 배율은 이 값에 적용되지 않고, 연타 배율만 적용됩니다.
                float hitActionDelay = isMultiHit
                    ? multiHitActionInterval / multiHitAnimationSpeed
                    : ActionDelay;
                yield return new WaitForSeconds(
                    GetActiveActionBeatDelay(hitActionDelay));

                PlayTargetUnitVfxOnMonsterTargets(
                    attackerAnimator,
                    command,
                    monsterTargets,
                    allowPlayOncePerActionCues: hitIndex <= 0);

                List<Transform> feedbackTargets = new();
                bool appliedAnyHit = false;

                bool prepareCameraImpact = ShouldPlayCameraImpactForHit(hitIndex, hitCount);
                if (prepareCameraImpact && BattleCameraController.Instance != null)
                {
                    if (ShouldSkipPlayerSkillCamera(command))
                    {
                        yield return BattleCameraController.Instance.PrepareRangedDamageImpact();
                    }
                    else
                    {
                        int impactDirection = ResolveCameraImpactDirection(
                            caster != null ? caster.transform : null,
                            monsterTargets,
                            BattleDirectionToHorizontal(command.Direction));
                        yield return BattleCameraController.Instance.PrepareDamageImpact(impactDirection);
                    }
                }

                MonsterUnit randomTarget = effectId == "E_RandomStrike"
                    ? PickRandomAliveMonster(monsterTargets)
                    : null;

                for (int i = 0; i < monsterTargets.Count; i++)
                {
                    if (caster == null || caster.RuntimeData == null || caster.RuntimeData.IsDead)
                        break;

                    MonsterUnit monster = monsterTargets[i];

                    if (!IsAliveMonsterTarget(monster))
                        continue;
                    if (randomTarget != null && monster != randomTarget)
                        continue;
                    if (effectId == "E_StrikeCountByDebuff" &&
                        hitIndex >= NewSkillEffectRules.ResolveConditionalHitCount(
                            count,
                            CountHarmfulEffectTypes(monster.RuntimeData?.StatusEffects)))
                    {
                        continue;
                    }

                    BattleEffectContext context = CreatePlayerMonsterEffectContext(
                        caster,
                        monster,
                        command,
                        effectId,
                        value,
                        effectParameterCount);

                    bool liveRearAttack =
                        BattleDamageModifierUtility.IsAttackingMonsterFromBehind(caster, monster);
                    context.RearAttackQualifiedAtActionStart = rearAttackSnapshot.GetOrCapture(
                        monster.RuntimeData.RuntimeId,
                        liveRearAttack);

                    int hpBeforeHit = monster.RuntimeData != null
                        ? monster.RuntimeData.CurrentHP
                        : 0;
                    int shieldBeforeHit = monster.RuntimeData != null
                        ? monster.RuntimeData.CurrentShield
                        : 0;

                    ExecutePlayerEffectSafely(effectId, context, command.SkillData.SkillId);
                    appliedAnyHit = true;
                    AddFeedbackTarget(feedbackTargets, monster.transform);

                    bool receivedDamage = monster.RuntimeData != null &&
                        (monster.RuntimeData.CurrentHP < hpBeforeHit ||
                         monster.RuntimeData.CurrentShield < shieldBeforeHit);

                    if (receivedDamage &&
                        monster.RuntimeData != null &&
                        !monster.RuntimeData.IsDead)
                    {
                        FaceMonsterAgainstAttackDirection(monster, command.Direction);
                    }

                    // 드라우그는 플레이어의 공격 스킬에 실제로 피격된 경우에만 반격 후보가 됩니다.
                    // 다단 공격은 타격마다 반격하지 않고, 해당 스킬의 모든 타격이 끝난 뒤 한 번만 반격합니다.
                    if (receivedDamage &&
                        monster.RuntimeData != null &&
                        !monster.RuntimeData.IsDead &&
                        HasActiveMonsterCounter(monster.RuntimeData))
                    {
                        draugrCounterCandidates.Add(monster);
                    }

                    bool shouldSplit = receivedDamage &&
                        statusEffectService.ApplySplitHitAndCheckTrigger(monster);

                    if (shouldSplit)
                    {
                        deathService.HandleMuckSplit(monster);
                        continue;
                    }

                    if (monster.RuntimeData != null && monster.RuntimeData.IsDead)
                        deathService.HandleMonsterDead(monster);

                    if (caster.RuntimeData == null || caster.RuntimeData.IsDead)
                        break;
                }

                appliedAnyHit |= ApplyPlayerDamageHitToGridEffects(
                    gridEffectTargets,
                    Mathf.Max(0, value));

                hudService.RefreshHUDs();

                if (!appliedAnyHit && BattleCameraController.Instance != null)
                    BattleCameraController.Instance.CancelPreparedDamageImpact();

                if (!appliedAnyHit && hitIndex == 0)
                {
                    // 빈 공간 공격도 연타 모션은 끝까지 재생하되,
                    // 공격 빗나감 보상은 한 공격 행동에 한 번만 적용합니다.
                    BattleEquipmentEffectService.TryApplyAttackMissCharge(command.UserRuntime);
                }

                bool playCameraImpact = appliedAnyHit && ShouldPlayCameraImpactForHit(hitIndex, hitCount);
                bool playPushFeedback = ShouldPlayExternalImpactForHit(hitIndex, hitCount);

                if (playCameraImpact)
                {
                    if (ShouldSkipPlayerSkillCamera(command))
                    {
                        yield return PlayRangedDamageCameraFeedback();
                    }
                    else if (playPushFeedback)
                    {
                        yield return PlayPlayerDirectedDamageHitFeedback(
                            caster != null ? caster.transform : null,
                            feedbackTargets,
                            command.Direction);
                    }
                    else
                    {
                        yield return PlayDamageCameraFeedback();
                    }
                }

                if (hitIndex >= hitCount - 1)
                {
                    float endDelay = playCameraImpact ? HitCameraDelay : ActionDelay;
                    yield return new WaitForSeconds(endDelay / GetActivePresentationSpeed());
                }
            }

        }
        finally
        {
            foreach (BattleUnitAnimator targetAnimator in monsterHitAnimators)
                if (targetAnimator != null)
                    targetAnimator.SuppressRepeatedMultiHitReaction = false;
            if (isMultiHit && attackerAnimator != null)
                attackerAnimator.RestorePlaybackSpeed();
        }
    }

    // 몬스터가 E_Counter 상태를 보유할 때만 피격 후 반격을 수행합니다.
    private static bool HasActiveMonsterCounter(MonsterRuntimeData runtime)
    {
        if (runtime?.StatusEffects == null)
            return false;

        foreach (StatusEffectRuntimeData status in runtime.StatusEffects)
        {
            if (status != null && status.EffectId == "E_Counter" && status.Stack > 0 && status.TurnCount > 0)
                return true;
        }

        return false;
    }

    private IEnumerator ExecuteDraugrCounters(
        BattleCharacter caster,
        HashSet<MonsterUnit> counterCandidates)
    {
        if (caster == null ||
            caster.RuntimeData == null ||
            caster.RuntimeData.IsDead ||
            caster.CurrentGridIndex < 0 ||
            counterCandidates == null ||
            counterCandidates.Count <= 0)
        {
            yield break;
        }

        MonsterSkillData counterSkill =
            DataManager.Instance?.MonsterSkillDatabase?.Get("S_Monster_17");

        if (counterSkill == null)
        {
            Debug.LogWarning("[DraugrAI] 내려찍기 스킬 S_Monster_17 데이터를 찾을 수 없습니다.");
            yield break;
        }

        foreach (MonsterUnit draugr in counterCandidates)
        {
            if (draugr == null ||
                draugr.RuntimeData == null ||
                draugr.RuntimeData.IsDead ||
                !HasActiveMonsterCounter(draugr.RuntimeData) ||
                draugr.MainGridIndex < 0 ||
                caster.RuntimeData.IsDead ||
                caster.CurrentGridIndex < 0)
            {
                continue;
            }

            Vector2Int draugrCoord = gridManager.IndexToCoord(draugr.MainGridIndex);
            Vector2Int casterCoord = gridManager.IndexToCoord(caster.CurrentGridIndex);

            // 반격은 공격자가 현재 반격 범위 밖에 있더라도 행동 자체는 반드시 실행합니다.
            // 공격자가 좌우 어느 쪽에 있는지는 현재 실제 위치를 기준으로 정하고,
            // 같은 열에 있다면 드라우그가 현재 바라보는 방향을 그대로 사용합니다.
            int deltaX = casterCoord.x - draugrCoord.x;

            BattleUnitFacing draugrFacing = draugr.GetComponent<BattleUnitFacing>();
            BattleDirection counterDirection = deltaX > 0
                ? BattleDirection.Right
                : deltaX < 0
                    ? BattleDirection.Left
                    : draugrFacing != null
                        ? draugrFacing.GetBattleDirection()
                        : BattleDirection.Right;

            int horizontalSign = counterDirection == BattleDirection.Right ? 1 : -1;

            List<int> counterRange = new();

            for (int distance = 1; distance <= 2; distance++)
            {
                Vector2Int coord = draugrCoord + new Vector2Int(horizontalSign * distance, 0);

                if (!gridManager.IsValidCoord(coord))
                    continue;

                counterRange.Add(gridManager.CoordToIndex(coord));
            }

            MonsterReservedCommand counterCommand =
                new MonsterReservedCommand(draugr.RuntimeData, counterSkill);

            counterCommand.SetRangeOriginGridIndex(draugr.MainGridIndex);
            counterCommand.SetForcedDirection(counterDirection);
            counterCommand.SetExplicitRangeResult(counterRange, counterRange);

            // Display reactive monster actions that are not on the timeline.
            BattleEffectInfoFeed.ShowMonster(counterCommand);
            yield return ExecuteMonsterSkill(counterCommand, isCounterAttack: true);

            if (caster.RuntimeData.IsDead)
                yield break;
        }
    }

    private bool HasDirectionalMoveBeforeFirstDamage(PlayerReservedCommand command)
    {
        if (command == null ||
            command.SkillData == null ||
            command.SkillData.EffectEntries == null)
        {
            return false;
        }

        for (int i = 0; i < command.SkillData.EffectEntries.Count; i++)
        {
            SkillEffectEntry entry = command.SkillData.EffectEntries[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.EffectId))
                continue;

            string effectId = BattleEquipmentEffectService.GetEffectivePlayerDamageEffectId(
                command.UserRuntime,
                command,
                entry.EffectId);

            if (IsDamageHitEffect(effectId))
                return false;

            if (TryGetExplicitPlayerDirectionalMoveDistance(
                    command,
                    effectId,
                    i,
                    out _))
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshPlayerMonsterTargetsAfterDirectionalMove(
        BattleCharacter caster,
        PlayerReservedCommand command,
        List<MonsterUnit> monsterTargets,
        List<int> gridEffectTargets)
    {
        if (caster == null || command == null)
            return;

        RecalculatePlayerSkillRangeAtExecution(caster, command);

        // 이동 성공 시에는 예약 프리뷰와 같은 도착 위치 기준 범위가 유지되고,
        // 이동 실패/충돌 시에는 실제 남아 있는 위치 기준 범위로 즉시 갱신합니다.
        ShowExecutionRange(BuildPlayerExecutionRange(command), GetPlayerExecutionRangeType(command));

        if (monsterTargets != null)
        {
            monsterTargets.Clear();

            MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < monsters.Length; i++)
            {
                MonsterUnit monster = monsters[i];

                if (monster == null || monster.RuntimeData == null)
                    continue;

                if (!IsMonsterInRange(monster, command))
                    continue;

                monsterTargets.Add(monster);
            }
        }

        if (gridEffectTargets != null)
        {
            gridEffectTargets.Clear();
            gridEffectTargets.AddRange(BuildDamageableGridEffectTargets(command));
        }
    }

    private static bool TryGetExplicitPlayerDirectionalMoveDistance(
        PlayerReservedCommand command,
        string effectId,
        int effectIndex,
        out int signedDistance)
    {
        signedDistance = 0;

        if (command == null ||
            command.SkillData == null ||
            command.SkillData.RangeType != RangeType.Direction ||
            (!string.Equals(effectId, "E_Move", StringComparison.Ordinal) &&
             !string.Equals(effectId, "E_Rush", StringComparison.Ordinal)) ||
            effectIndex < 0 ||
            string.IsNullOrWhiteSpace(command.SkillData.ValueRate))
        {
            return false;
        }

        if (command.HasDirectionalMoveDistanceOverride)
        {
            signedDistance = command.DirectionalMoveDistanceOverride;
            return signedDistance != 0;
        }

        string[] values = command.SkillData.ValueRate.Split(';');

        if (effectIndex >= values.Length ||
            string.IsNullOrWhiteSpace(values[effectIndex]))
        {
            return false;
        }

        return int.TryParse(values[effectIndex].Trim(), out signedDistance) &&
               signedDistance != 0;
    }

    private IEnumerator ExecutePlayerDirectionalMoveEffect(
        BattleCharacter caster,
        PlayerReservedCommand command,
        int signedDistance,
        string effectId)
    {
        if (caster == null || command == null || caster.RuntimeData == null)
            yield break;

        if (caster.RuntimeData.IsDead || caster.CurrentGridIndex < 0 || gridManager == null)
            yield break;

        if (HasStatus(caster.RuntimeData.StatusEffects, "E_Bondage"))
        {
            BattleStatusEffectService.TryConsumeOnTrigger(
                caster.RuntimeData.StatusEffects,
                "E_Bondage",
                id => DataManager.Instance?.EffectDatabase?.Get(id));
            yield break;
        }

        if (signedDistance == 0)
            yield break;

        Vector2Int attackDirection = command.Direction == BattleDirection.Right
            ? Vector2Int.right
            : Vector2Int.left;
        Vector2Int moveOffset = attackDirection * signedDistance;

        List<int> enteredGridIndices = new();
        List<int> blockingUnitGridIndices = new();
        int startGridIndex = caster.CurrentGridIndex;

        if (!TryGetPlayerMoveTargetGridIndex(
                startGridIndex,
                moveOffset,
                command.CharacterId,
                out int targetGridIndex,
                enteredGridIndices,
                blockingUnitGridIndices))
        {
            yield break;
        }

        if (command.ExecutionResult != null)
        {
            command.ExecutionResult.RushDestinationGridIndex = targetGridIndex;
            command.ExecutionResult.RushCollisionGridIndex =
                blockingUnitGridIndices.Count > 0 ? blockingUnitGridIndices[0] : -1;
            command.ExecutionResult.RushCollisionRuntimeId =
                ResolveCollisionRuntimeId(
                    command.ExecutionResult.RushCollisionGridIndex,
                    command.CharacterId);
        }

        if (targetGridIndex == startGridIndex)
        {
            if (NewSkillEffectRules.ShouldApplyMoveCollisionEffect(effectId))
                ApplyPlayerMoveCollisionOnce(blockingUnitGridIndices, command.CharacterId);
            yield break;
        }

        Vector2Int startCoord = gridManager.IndexToCoord(startGridIndex);
        Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);
        Vector2Int actualMoveOffset = targetCoord - startCoord;

        ApplyPlayerMoveFacing(caster, command.Direction, moveOffset, true);

        BattleUnitAnimator animator = caster.GetComponent<BattleUnitAnimator>();

        if (animator != null &&
            BattleActionPresentationPolicy.ShouldPlayGenericMoveForSkillEffect(command.SkillData))
            animator.PlayMove();

        yield return MovePlayerThroughEnteredGrids(caster, command.CharacterId, enteredGridIndices);
        ApplyPlayerBleedForMovedCells(caster, enteredGridIndices.Count);

        if (NewSkillEffectRules.ShouldApplyMoveCollisionEffect(effectId))
            ApplyPlayerMoveCollisionOnce(blockingUnitGridIndices, command.CharacterId);

        hudService.RefreshHUDs();
    }

    private void ExecutePlayerNonDamageEffectToMonsters(
        BattleCharacter caster,
        List<MonsterUnit> monsterTargets,
        PlayerReservedCommand command,
        string effectId,
        int value,
        int count)
    {
        if (monsterTargets == null)
            return;

        for (int i = 0; i < monsterTargets.Count; i++)
        {
            MonsterUnit monster = monsterTargets[i];

            if (!IsAliveMonsterTarget(monster))
                continue;

            FaceMonsterToAttacker(monster, caster);

            BattleEffectContext context = CreatePlayerMonsterEffectContext(
                caster,
                monster,
                command,
                effectId,
                value,
                count);

            ExecutePlayerNonDamageEffectRepeated(effectId, context, command.SkillData.SkillId);
        }
    }

    private static string ResolveCollisionRuntimeId(int gridIndex, string movingCharacterId)
    {
        if (gridIndex < 0)
            return null;

        if (BattleOccupancyService.TryGetMonsterAtGrid(
                gridIndex,
                out MonsterUnit monster) &&
            monster?.RuntimeData != null)
        {
            return monster.RuntimeData.RuntimeId;
        }

        if (BattleOccupancyService.TryGetCharacterAtGrid(
                gridIndex,
                out BattleCharacter character,
                movingCharacterId) &&
            character?.RuntimeData != null)
        {
            return character.RuntimeData.CharacterId;
        }

        return null;
    }

    private BattleEffectContext CreatePlayerMonsterEffectContext(
        BattleCharacter caster,
        MonsterUnit monsterTarget,
        PlayerReservedCommand command,
        string effectId,
        int value,
        int count)
    {
        return new BattleEffectContext
        {
            PlayerCaster = caster,
            MonsterTarget = monsterTarget,
            PlayerSkillData = command.SkillData,
            PlayerCommand = command,

            Direction = command.Direction,
            GridManager = gridManager,

            EffectId = effectId,
            Value = value,
            Count = count,
            ExecutionResult = command.ExecutionResult
        };
    }

    private bool IsDamageHitEffect(string effectId)
    {
        return effectId == "E_Strike" || effectId == "E_Pierce" || effectId == "E_MissingHPStrike" ||
               effectId == "E_ArmorStrike" || effectId == "E_RandomStrike" ||
               effectId == "E_StrikeByVulnerable" ||
               effectId == "E_StrikeByTargetPoison" ||
               effectId == "E_DamageUpIfBleeding" ||
               effectId == "E_StrikeCountByBuff" ||
               effectId == "E_StrikeCountByDebuff";
    }

    private static bool IsPostResolutionEffect(string effectId)
    {
        return effectId == "E_MissSelfDamage" ||
               effectId == "E_RestoreManaPerHitTarget" ||
               effectId == "E_ValueUpOnKill" ||
               effectId == "E_ValueUpOnHit";
    }

    private void ExecutePlayerPostResolutionEffects(
        BattleCharacter caster,
        PlayerReservedCommand command)
    {
        if (command?.SkillData?.EffectEntries == null)
            return;

        for (int i = 0; i < command.SkillData.EffectEntries.Count; i++)
        {
            SkillEffectEntry entry = command.SkillData.EffectEntries[i];
            if (entry == null || !IsPostResolutionEffect(entry.EffectId))
                continue;

            ExecutePlayerPostResolutionEffect(
                caster,
                command,
                entry.EffectId,
                GetPlayerEffectValue(command, entry));
        }
    }

    private void ExecutePlayerPostResolutionEffect(
        BattleCharacter caster,
        PlayerReservedCommand command,
        string effectId,
        int value)
    {
        effectExecutor.Execute(effectId, new BattleEffectContext
        {
            PlayerCaster = caster,
            PlayerSkillData = command.SkillData,
            PlayerCommand = command,
            Direction = command.Direction,
            GridManager = gridManager,
            EffectId = effectId,
            Value = value,
            Count = 1,
            ExecutionResult = command.ExecutionResult
        });
    }

    private MonsterUnit PickRandomAliveMonster(List<MonsterUnit> targets)
    {
        if (targets == null)
            return null;
        List<MonsterUnit> alive = new();
        for (int i = 0; i < targets.Count; i++)
            if (IsAliveMonsterTarget(targets[i]))
                alive.Add(targets[i]);
        return BattleRandom.Pick(alive);
    }

    private List<int> BuildDamageableGridEffectTargets(PlayerReservedCommand command)
    {
        List<int> targets = new();

        if (command == null || !HasPlayerDamageHitEffect(command))
            return targets;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null || command.RangeGridIndices == null)
            return targets;

        for (int i = 0; i < command.RangeGridIndices.Count; i++)
        {
            int gridIndex = command.RangeGridIndices[i];

            if (!controller.HasDamageableEffect(gridIndex))
                continue;

            AddUnique(targets, gridIndex);
        }

        return targets;
    }

    private bool HasPlayerDamageHitEffect(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;

        if (command.SkillData.EffectEntries != null && command.SkillData.EffectEntries.Count > 0)
        {
            for (int i = 0; i < command.SkillData.EffectEntries.Count; i++)
            {
                SkillEffectEntry entry = command.SkillData.EffectEntries[i];

                if (entry == null || string.IsNullOrWhiteSpace(entry.EffectId))
                    continue;

                string effectId = BattleEquipmentEffectService.GetEffectivePlayerDamageEffectId(
                    command.UserRuntime,
                    command,
                    entry.EffectId);

                if (IsDamageHitEffect(effectId))
                    return true;
            }

            return false;
        }

        if (string.IsNullOrWhiteSpace(command.SkillData.EffectIds))
            return false;

        string[] effectIds = command.SkillData.EffectIds.Split(';');

        for (int i = 0; i < effectIds.Length; i++)
        {
            if (IsDamageHitEffect(effectIds[i].Trim()))
                return true;
        }

        return false;
    }

    private bool HasDamageableGridEffectTarget(List<int> gridEffectTargets)
    {
        if (gridEffectTargets == null || gridEffectTargets.Count <= 0)
            return false;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null)
            return false;

        for (int i = gridEffectTargets.Count - 1; i >= 0; i--)
        {
            int gridIndex = gridEffectTargets[i];

            if (controller.HasDamageableEffect(gridIndex))
                return true;

            gridEffectTargets.RemoveAt(i);
        }

        return false;
    }

    private bool ApplyPlayerDamageHitToGridEffects(
        List<int> gridEffectTargets,
        int damage)
    {
        if (gridEffectTargets == null || gridEffectTargets.Count <= 0 || damage <= 0)
            return false;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null)
            return false;

        bool applied = false;

        for (int i = gridEffectTargets.Count - 1; i >= 0; i--)
        {
            int gridIndex = gridEffectTargets[i];

            if (!controller.TryDamageEffect(gridIndex, damage, out bool destroyed))
            {
                gridEffectTargets.RemoveAt(i);
                continue;
            }

            applied = true;

            if (destroyed)
                gridEffectTargets.RemoveAt(i);
        }

        return applied;
    }

    private bool HasAliveMonsterTarget(List<MonsterUnit> monsterTargets)
    {
        if (monsterTargets == null)
            return false;

        for (int i = 0; i < monsterTargets.Count; i++)
        {
            if (IsAliveMonsterTarget(monsterTargets[i]))
                return true;
        }

        return false;
    }

    private bool IsAliveMonsterTarget(MonsterUnit monster)
    {
        return monster != null &&
               monster.RuntimeData != null &&
               !monster.RuntimeData.IsDead;
    }

    // The defender faces against the direction of the incoming player attack.
    // Do not use monster.x - caster.x: area attacks may hit both sides.
    private void FaceMonsterAgainstAttackDirection(MonsterUnit monster, BattleDirection attackDirection)
    {
        if (monster == null)
            return;

        bool faceRight = attackDirection == BattleDirection.Left;
        BattleUnitFacing hitFacing = monster.GetComponent<BattleUnitFacing>();
        if (hitFacing == null)
            hitFacing = monster.GetComponentInChildren<BattleUnitFacing>();
        if (hitFacing != null)
        {
            hitFacing.FaceRight(faceRight);
        }

        if (monster.RuntimeData != null)
            monster.RuntimeData.Direction = faceRight
                ? BattleDirection.Right
                : BattleDirection.Left;
    }

    private void FaceMonsterToAttacker(MonsterUnit monster, BattleCharacter attacker)
    {
        if (monster == null || attacker == null)
            return;

        BattleUnitFacing hitFacing = monster.GetComponent<BattleUnitFacing>();

        if (hitFacing == null)
            return;

        // Keep the logical monster action direction intact while briefly
        // turning the visible sprite towards the source of the hit.
        hitFacing.FaceByWorldTarget(attacker.transform.position);
    }

    private void ExecutePlayerSkillEffects(
        BattleCharacter caster,
        MonsterUnit monsterTarget,
        PlayerReservedCommand command)
    {
        if (caster == null || monsterTarget == null || command == null || command.SkillData == null)
            return;

        if (command.SkillData.EffectEntries == null || command.SkillData.EffectEntries.Count == 0)
        {
            Debug.LogWarning($"[PlayerSkillEffect] EffectEntries 없음 / Skill:{command.SkillData.SkillId}");
            return;
        }

        for (int i = 0; i < command.SkillData.EffectEntries.Count; i++)
        {
            SkillEffectEntry entry = command.SkillData.EffectEntries[i];

            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.EffectId))
                continue;

            string effectId = BattleEquipmentEffectService.GetEffectivePlayerDamageEffectId(
                command.UserRuntime,
                command,
                entry.EffectId);
            BattleEffectContext context = new BattleEffectContext
            {
                PlayerCaster = caster,
                MonsterTarget = monsterTarget,
                PlayerSkillData = command.SkillData,
                PlayerCommand = command,

                Direction = command.Direction,
                GridManager = gridManager,

                EffectId = effectId,
                Value = GetPlayerEffectValue(command, entry),
                Count = GetPlayerEffectCount(command, entry),
                ExecutionResult = command.ExecutionResult
            };

            if (IsDamageHitEffect(effectId))
            {
                int hitCount = Mathf.Max(1, context.Count);

                for (int hit = 0; hit < hitCount; hit++)
                {
                    if (monsterTarget.RuntimeData.IsDead)
                        break;

                    context.Count = 1;
                    ExecutePlayerEffectSafely(effectId, context, command.SkillData.SkillId);
                }
            }
            else
            {
                ExecutePlayerNonDamageEffectRepeated(effectId, context, command.SkillData.SkillId);
            }
        }
    }

    private void ExecutePlayerSkillEffectsToPlayer(
    BattleCharacter caster,
    BattleCharacter playerTarget,
    PlayerReservedCommand command)
    {
        if (caster == null || playerTarget == null || command == null || command.SkillData == null)
            return;

        if (playerTarget.RuntimeData == null || playerTarget.RuntimeData.IsDead)
            return;

        if (command.SkillData.EffectEntries == null || command.SkillData.EffectEntries.Count == 0)
            return;

        for (int i = 0; i < command.SkillData.EffectEntries.Count; i++)
        {
            if (playerTarget.RuntimeData == null || playerTarget.RuntimeData.IsDead)
                break;

            SkillEffectEntry entry = command.SkillData.EffectEntries[i];

            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.EffectId))
                continue;

            string effectId = BattleEquipmentEffectService.GetEffectivePlayerDamageEffectId(
                command.UserRuntime,
                command,
                entry.EffectId);
            BattleEffectContext context = new BattleEffectContext
            {
                PlayerCaster = caster,
                PlayerTarget = playerTarget,
                PlayerSkillData = command.SkillData,
                PlayerCommand = command,

                Direction = command.Direction,
                GridManager = gridManager,

                EffectId = effectId,
                Value = GetPlayerEffectValue(command, entry),
                Count = GetPlayerEffectCount(command, entry),
                ExecutionResult = command.ExecutionResult
            };

            if (IsDamageHitEffect(effectId))
            {
                int hitCount = Mathf.Max(1, context.Count);

                for (int hit = 0; hit < hitCount; hit++)
                {
                    if (playerTarget.RuntimeData.CurrentHP <= 0)
                        break;

                    context.Count = 1;
                    ExecutePlayerEffectSafely(effectId, context, command.SkillData.SkillId);
                }
            }
            else
            {
                ExecutePlayerNonDamageEffectRepeated(effectId, context, command.SkillData.SkillId);
            }
        }
    }

    private void ExecutePlayerNonDamageEffectRepeated(
        string effectId,
        BattleEffectContext context,
        string skillId)
    {
        if (context == null)
            return;

        int repeatCount = Mathf.Max(1, context.Count);
        context.Count = 1;

        for (int repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
        {
            if (context.PlayerTarget != null &&
                (context.PlayerTarget.RuntimeData == null || context.PlayerTarget.RuntimeData.IsDead))
            {
                break;
            }

            if (context.MonsterTarget != null &&
                (context.MonsterTarget.RuntimeData == null || context.MonsterTarget.RuntimeData.IsDead))
            {
                break;
            }

            ExecutePlayerEffectSafely(effectId, context, skillId);
        }
    }

    private void ExecutePlayerEffectSafely(
        string effectId,
        BattleEffectContext context,
        string skillId)
    {
        try
        {
            effectExecutor.Execute(effectId, context);
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"[PlayerSkillEffect] Effect 실행 중 오류 / " +
                $"Skill:{skillId} / Effect:{effectId}"
            );
            Debug.LogException(e);
        }
    }

    private int GetPlayerEffectValue(PlayerReservedCommand command, SkillEffectEntry entry)
    {
        if (command == null || entry == null)
            return 1;

        int entryIndex = command.SkillData?.EffectEntries?.IndexOf(entry) ?? -1;
        int baseValue = entryIndex >= 0 && entryIndex < command.ResolvedEffectValues.Count
            ? command.ResolvedEffectValues[entryIndex]
            : SkillScalingUtility.ResolveBaseValue(command.UserRuntime, entry);

        if (IsFirstDirectDamageEntry(command.SkillData, entryIndex) && DataManager.Instance != null)
        {
            baseValue += SkillRuntimeValueBonusResolver.GetTotalValueBonus(
                DataManager.Instance.SkillRuntimeStore,
                command.CharacterId,
                command.SkillData.SkillId);
        }

        if (entry.EffectId == "E_Knockback")
            return BattleEquipmentEffectService.ModifyPlayerKnockbackValue(
                command.UserRuntime,
                command,
                entry,
                baseValue);

        return BattleEquipmentEffectService.ModifyPlayerEffectValue(
            command.UserRuntime,
            command,
            entry,
            baseValue);
    }

    private int GetPlayerEffectCount(PlayerReservedCommand command, SkillEffectEntry entry)
    {
        if (command == null || entry == null)
            return 1;

        int entryIndex = command.SkillData?.EffectEntries?.IndexOf(entry) ?? -1;
        int baseCount = entryIndex >= 0 && entryIndex < command.ResolvedEffectCounts.Count
            ? command.ResolvedEffectCounts[entryIndex]
            : entry.CountAmount;

        if (IsDamageHitEffect(entry.EffectId) &&
            HasSkillEffect(command.SkillData, "E_StrikeCountByBuff"))
        {
            baseCount += CountBeneficialEffectTypes(command.UserRuntime?.StatusEffects);
        }

        return BattleEquipmentEffectService.ModifyPlayerEffectCount(
            command.UserRuntime,
            command,
            entry,
            baseCount);
    }

    private static bool IsFirstDirectDamageEntry(SkillMasterData skill, int entryIndex)
    {
        if (skill?.EffectEntries == null || entryIndex < 0)
            return false;
        for (int i = 0; i < skill.EffectEntries.Count; i++)
        {
            string id = skill.EffectEntries[i]?.EffectId;
            if (id == "E_Strike" || id == "E_Pierce" || id == "E_ArmorStrike" ||
                id == "E_RandomStrike" || id == "E_StrikeByVulnerable" ||
                id == "E_StrikeByTargetPoison" ||
                id == "E_DamageUpIfBleeding" || id == "E_StrikeCountByBuff" ||
                id == "E_StrikeCountByDebuff")
                return i == entryIndex;
        }
        return false;
    }

    private static bool HasSkillEffect(SkillMasterData skill, string effectId)
    {
        if (skill?.EffectEntries == null)
            return false;
        for (int i = 0; i < skill.EffectEntries.Count; i++)
            if (skill.EffectEntries[i]?.EffectId == effectId)
                return true;
        return false;
    }

    private static int CountBeneficialEffectTypes(List<StatusEffectRuntimeData> statuses)
    {
        if (statuses == null || DataManager.Instance?.EffectDatabase == null)
            return 0;
        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int i = 0; i < statuses.Count; i++)
        {
            StatusEffectRuntimeData status = statuses[i];
            EffectMasterData master = status != null
                ? DataManager.Instance.EffectDatabase.Get(status.EffectId)
                : null;
            if (status != null && status.Stack > 0 && master?.EffectType == EffectType.Beneficial)
                ids.Add(status.EffectId);
        }
        return ids.Count;
    }

    private static int CountHarmfulEffectTypes(List<StatusEffectRuntimeData> statuses)
    {
        if (statuses == null || DataManager.Instance?.EffectDatabase == null)
            return 0;

        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int i = 0; i < statuses.Count; i++)
        {
            StatusEffectRuntimeData status = statuses[i];
            EffectMasterData master = status != null
                ? DataManager.Instance.EffectDatabase.Get(status.EffectId)
                : null;
            if (status != null && status.Stack > 0 && master?.EffectType == EffectType.Harmful)
                ids.Add(status.EffectId);
        }
        return ids.Count;
    }

    private static int GetMaximumTargetHarmfulEffectTypes(List<MonsterUnit> targets)
    {
        int maximum = 0;
        if (targets == null)
            return maximum;

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterUnit target = targets[i];
            if (target?.RuntimeData == null || target.RuntimeData.IsDead)
                continue;
            maximum = Mathf.Max(maximum, CountHarmfulEffectTypes(target.RuntimeData.StatusEffects));
        }
        return maximum;
    }

    private static bool HasStatus(List<StatusEffectRuntimeData> statuses, string effectId)
    {
        if (statuses == null)
            return false;
        for (int i = 0; i < statuses.Count; i++)
            if (statuses[i] != null && statuses[i].Stack > 0 && statuses[i].EffectId == effectId)
                return true;
        return false;
    }

    private bool IsDeadMonsterCommand(MonsterReservedCommand command)
    {
        if (command == null || string.IsNullOrWhiteSpace(command.RuntimeId))
            return true;

        MonsterUnit monster = unitFinder.FindMonsterUnit(command.RuntimeId);
        return monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead;
    }
    private IEnumerator ExecuteMonsterCommand(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null || IsDeadMonsterCommand(command))
            yield break;

        // 아라벨라의 사냥 강습은 피해와 넉백에 더해 실제 착지 이동을 처리합니다.
        if (command.MonsterId == "Mon_12_2" && command.SkillId == "S_Monster_39")
        {
            yield return ExecuteArabellaLeap(command);
            yield break;
        }

        // 사슬 이동은 투사 데이터의 타입이나 표기와 관계없이 이동 처리로 보냅니다.
        if (IsNocturnPortalMove(command))
        {
            yield return ExecuteMonsterMove(command);
        }
        else if (command.SkillData.TimelineNotation == TimelineActionType.Move)
        {
            yield return ExecuteMonsterMove(command);
        }
        else
        {
            yield return ExecuteMonsterSkill(command);
        }
    }

    // Keep the prefab root/VFX in place. Only the SpriteRoot rises and descends.
    private IEnumerator AnimateArabellaHuntingLeap(
        MonsterUnit monster,
        Vector3 landingPosition,
        System.Action onDescentStarted)
    {
        if (monster == null)
            yield break;

        Transform root = monster.transform;
        Transform visual = root.Find("SpriteRoot");
        if (visual == null)
        {
            Debug.LogWarning("[Arabella] SpriteRoot missing; leap visual skipped to avoid moving VFX.");
            yield break;
        }

        Vector3 savedLocalPosition = visual.localPosition;
        Vector3 departure = visual.position;
        Vector3 visualOffset = departure - root.position;
        const float riseHeight = 4.5f;
        const float fadeOutSeconds = 0.4f;
        const float fadeInSeconds = 0.3f;
        SpriteRenderer[] sprites = visual.GetComponentsInChildren<SpriteRenderer>(true);
        Color[] colors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            colors[i] = sprites[i].color;

        float elapsed = 0f;
        float duration = BattlePresentationSpeedSettings.ScaleDuration(fadeOutSeconds);
        while (elapsed < duration && monster != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration));
            float eased = t * t * (3f - 2f * t);
            visual.position = Vector3.LerpUnclamped(departure, departure + Vector3.up * riseHeight, eased);
            // Remain visible during most of the ascent instead of vanishing halfway up.
            float opacity = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, t));
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null)
                {
                    Color c = colors[i];
                    c.a *= opacity;
                    sprites[i].color = c;
                }
            yield return null;
        }
        if (monster == null)
            yield break;

        Vector3 landingVisual = landingPosition + visualOffset;
        Vector3 arrivalStart = landingVisual + Vector3.up * riseHeight;
        visual.position = arrivalStart;
        onDescentStarted?.Invoke();
        elapsed = 0f;
        duration = BattlePresentationSpeedSettings.ScaleDuration(fadeInSeconds);
        while (elapsed < duration && monster != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration));
            float eased = t * t * (3f - 2f * t);
            visual.position = Vector3.LerpUnclamped(arrivalStart, landingVisual, eased);
            float opacity = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.65f, t));
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null)
                {
                    Color c = colors[i];
                    c.a *= opacity;
                    sprites[i].color = c;
                }
            yield return null;
        }

        if (monster == null)
            yield break;
        // The caller updates the logical cell and root immediately after this routine.
        visual.position = landingVisual;
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null)
                sprites[i].color = colors[i];
    }

    private IEnumerator ExecuteArabellaLeap(MonsterReservedCommand command)
    {
        MonsterUnit monster = unitFinder.FindMonsterUnit(command.RuntimeId);
        BattleMonsterTurnPlanner planner = Object.FindFirstObjectByType<BattleMonsterTurnPlanner>(
            FindObjectsInactive.Include);
        // 낙인은 성공 여부와 관계없이 강습 시작 시 제거합니다.
        if (planner != null)
            planner.HideArabellaStigmaPreview(command);

        if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead ||
            gridManager == null || monster.MainGridIndex < 0 || command.RangeOriginGridIndex < 0)
            yield break;

        int destinationIndex = command.RangeOriginGridIndex;
        GridCell cell = gridManager.GetCellByIndex(destinationIndex);
        if (cell == null)
            yield break;

        Vector2Int from = gridManager.IndexToCoord(monster.MainGridIndex);
        Vector2Int to = gridManager.IndexToCoord(destinationIndex);
        Vector2Int delta = to - from;
        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();
        if (delta.x != 0)
        {
            bool right = delta.x > 0;
            if (facing != null)
                facing.FaceRight(right);
            monster.RuntimeData.Direction = right ? BattleDirection.Right : BattleDirection.Left;
        }

        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();

        // Hunting leap bypasses ExecuteMonsterSkill: focus before the leap animation.
        if (ShouldEnterCameraForActiveAction() && BattleCameraController.Instance != null)
        {
            Vector3 impactWorldPosition = gridManager.GetWorldPositionByIndex(destinationIndex);
            yield return BattleCameraController.Instance.ZoomForActionTargets(
                monster.transform, null, false, false, impactWorldPosition);
        }

        // Prepare VFX and ascent begin together; the prefab root (and its VFX) stay still.
        BattleCharacter initiallyOccupiedCharacter = null;
        if (!IsGridEffectBlocked(destinationIndex) &&
            !BattleOccupancyService.TryGetMonsterAtGrid(destinationIndex, out MonsterUnit blockingMonster, monster))
        {
            Vector3 pos = gridManager.GetWorldPositionByIndex(destinationIndex);
            if (animator != null)
                animator.PlayMonsterSkillPrepare(command);
            Transform leapSpriteRoot = monster.transform.Find("SpriteRoot");
            Vector3 spriteRootDefaultLocalPosition = leapSpriteRoot != null ? leapSpriteRoot.localPosition : Vector3.zero;
            if (animator != null)
                animator.SuspendPersistentIdleVfx();
            try
            {
                yield return AnimateArabellaHuntingLeap(
                    monster,
                    pos,
                    () => animator?.PlayMonsterSkillActionStatePhase(command));
            }
            finally
            {
                if (animator != null)
                    animator.ResumePersistentIdleVfx();
            }
            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                yield break;

            // Displace the character ONLY when Arabella has finished descending.
            if (BattleOccupancyService.TryGetCharacterAtGrid(destinationIndex, out BattleCharacter occupant) &&
                occupant != null && occupant.RuntimeData != null && !occupant.RuntimeData.IsDead)
            {
                initiallyOccupiedCharacter = occupant;
                BattleUnitFacing targetFacing = occupant.GetComponent<BattleUnitFacing>();
                BattleDirection reverse = targetFacing != null
                    ? (targetFacing.IsFacingRight ? BattleDirection.Left : BattleDirection.Right)
                    : (occupant.RuntimeData.Direction == BattleDirection.Right
                        ? BattleDirection.Left : BattleDirection.Right);
                new KnockbackEffect().Execute(new BattleEffectContext
                {
                    MonsterCaster = monster,
                    PlayerTarget = occupant,
                    MonsterSkillData = command.SkillData,
                    MonsterCommand = command,
                    GridManager = gridManager,
                    Direction = reverse,
                    EffectId = "E_Knockback",
                    Value = 1,
                    Count = 1
                });
                // 반대 방향 넉백이 벽이나 점유로 실패해도 착지가 막히지 않게
                // 주변의 유효한 빈칸으로 캐릭터를 밀어냅니다.
                if (occupant.CurrentGridIndex == destinationIndex)
                {
                    Vector2Int[] offsets =
                    {
                        reverse == BattleDirection.Right ? Vector2Int.right : Vector2Int.left,
                        Vector2Int.up, Vector2Int.down,
                        reverse == BattleDirection.Right ? Vector2Int.left : Vector2Int.right
                    };
                    for (int i = 0; i < offsets.Length; i++)
                    {
                        Vector2Int candidate = to + offsets[i];
                        if (!gridManager.IsValidCoord(candidate))
                            continue;
                        int candidateIndex = gridManager.CoordToIndex(candidate);
                        if (BattleOccupancyService.IsOccupiedByAnyUnit(candidateIndex) ||
                            IsGridEffectBlocked(candidateIndex))
                            continue;
                        occupant.SetGridIndex(candidateIndex);
                        occupant.transform.position = gridManager.GetWorldPositionByIndex(candidateIndex);
                        UpdatePartyGridIndex(occupant.CharacterId, candidateIndex);
                        break;
                    }
                }
            }

            if (!BattleOccupancyService.IsOccupiedByAnyUnit(destinationIndex, null, monster))
            {
                monster.MoveOccupiedCells(delta, gridManager);
                monster.transform.position = pos;
                Transform spriteRoot = monster.transform.Find("SpriteRoot");
                if (spriteRoot != null)
                    spriteRoot.localPosition = spriteRootDefaultLocalPosition;
            }
            else
            {
                Debug.LogWarning("[Arabella] Leap landing still occupied after impact displacement.");
                Transform spriteRoot = monster.transform.Find("SpriteRoot");
                if (spriteRoot != null)
                    spriteRoot.localPosition = spriteRootDefaultLocalPosition;
            }
        }
        else
        {
            Debug.LogWarning("[Arabella] Leap destination blocked; landing was cancelled.");
        }

        // 착지 위치가 확정된 뒤 Action VFX만 생성합니다. 하강 상태는 상공에서 이미 시작했습니다.
        if (animator != null)
            animator.PlayMonsterSkillActionVfxPhase(command);

        // 착지점을 중심으로 주변 8칸과 중심 칸에 피해/넉백을 1회씩 적용합니다.
        // 예약 시의 공격 범위가 아니라 실제 착지 목적지를 사용합니다.
        List<BattleCharacter> hitTargets = new();
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter target = characters[i];
            if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead ||
                target.CurrentGridIndex < 0)
                continue;
            Vector2Int coord = gridManager.IndexToCoord(target.CurrentGridIndex);
            if ((Mathf.Abs(coord.x - to.x) <= 1 && Mathf.Abs(coord.y - to.y) <= 1) ||
                target == initiallyOccupiedCharacter)
                hitTargets.Add(target);
        }

        int damageValue = TryGetMonsterSkillEffectValue(command.SkillData, "E_Strike", out int strike)
            ? strike : 0;
        int knockbackValue = TryGetMonsterSkillEffectValue(command.SkillData, "E_Knockback", out int knockback)
            ? knockback : 0;
        for (int i = 0; i < hitTargets.Count; i++)
        {
            BattleCharacter target = hitTargets[i];
            if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
                continue;
            Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex);
            BattleDirection pushDirection = targetCoord.x > to.x ? BattleDirection.Right :
                targetCoord.x < to.x ? BattleDirection.Left :
                (target.RuntimeData.Direction == BattleDirection.Right ? BattleDirection.Left : BattleDirection.Right);
            if (damageValue > 0)
                new StrikeEffect().Execute(new BattleEffectContext
                {
                    MonsterCaster = monster,
                    PlayerTarget = target,
                    MonsterCommand = command,
                    MonsterSkillData = command.SkillData,
                    GridManager = gridManager,
                    Direction = pushDirection,
                    EffectId = "E_Strike",
                    Value = damageValue,
                    Count = 1
                });
            if (knockbackValue > 0 && target != initiallyOccupiedCharacter && !target.RuntimeData.IsDead)
                new KnockbackEffect().Execute(new BattleEffectContext
                {
                    MonsterCaster = monster,
                    PlayerTarget = target,
                    MonsterCommand = command,
                    MonsterSkillData = command.SkillData,
                    GridManager = gridManager,
                    Direction = pushDirection,
                    EffectId = "E_Knockback",
                    Value = knockbackValue,
                    Count = 1
                });
        }
        hudService.RefreshHUDs();
        yield return new WaitForSeconds(GetActiveActionBeatDelay());
        if (animator != null)
            animator.PlayIdle();
    }

    private IEnumerator ExecuteMonsterMove(MonsterReservedCommand command)
    {
        MonsterUnit monster = unitFinder.FindMonsterUnit(command.RuntimeId);

        if (monster == null)
            yield break;

        if (HasStatus(monster.RuntimeData?.StatusEffects, "E_Bondage"))
        {
            BattleStatusEffectService.TryConsumeOnTrigger(
                monster.RuntimeData.StatusEffects,
                "E_Bondage",
                id => DataManager.Instance?.EffectDatabase?.Get(id));
            yield break;
        }

        int currentGridIndex = monster.MainGridIndex;

        if (currentGridIndex < 0)
            yield break;

        Vector2Int moveOffset = GetMonsterMoveOffset(command);

        // 사슬 이동은 예약된 절대 목적지를 사용할 수 있으므로 일반 이동의 0 오프셋 검사보다 먼저 처리합니다.
        if (IsNocturnPortalMove(command))
        {
            ShowExecutionRange(BuildMonsterMoveExecutionRange(monster, command), BattleExecutionRangeType.Move);

            try
            {
                yield return ExecuteNocturnPortalMove(monster, command, moveOffset);
            }
            finally
            {
                ClearExecutionRange();
            }

            yield break;
        }

        if (moveOffset == Vector2Int.zero)
            yield break;

        ShowExecutionRange(BuildMonsterMoveExecutionRange(monster, command), BattleExecutionRangeType.Move);
        Transform directCameraUnit = null;

        try
        {

            BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

            if (facing == null)
                facing = monster.GetComponentInChildren<BattleUnitFacing>(true);
            if (facing != null)
            {
                facing.FaceByMoveOffset(moveOffset);
                if (monster.RuntimeData != null && moveOffset.x != 0)
                    monster.RuntimeData.Direction = facing.GetBattleDirection();
            }

            // 첫 이동 칸이 이미 다른 유닛에게 점유되어 있다면 한 칸도 이동하지 않고 즉시 충돌합니다.
            // 이동자와 해당 칸을 점유한 유닛 모두 충돌 고정 피해를 받습니다.
            // 신더의 대각선 이동은 가로/세로 중 통과 가능한 순서를 선택해야 합니다.
            // 즉시 충돌 검사에서는 가로 첫 칸만 확인하므로 빈 대각선 목적지에도
            // 캐릭터가 옆에 있다는 이유로 충돌할 수 있습니다. 해당 경우는
            // ResolveMonsterMove의 양쪽 경로 비교에 맡깁니다.
            bool isCinderDiagonalMove =
                string.Equals(command.MonsterId, CinderMonsterId, System.StringComparison.Ordinal) &&
                string.Equals(command.SkillId, "S_Monster_13", System.StringComparison.Ordinal) &&
                moveOffset.x != 0 && moveOffset.y != 0;
            if (!isCinderDiagonalMove && TryHandleImmediateMonsterUnitCollision(monster, moveOffset))
            {
                hudService.RefreshHUDs();
                yield return new WaitForSeconds(GetActiveActionBeatDelay());
                yield break;
            }

            MonsterMoveResolution moveResolution = ResolveMonsterMove(monster, moveOffset);

            if (moveResolution.ActualOffset == Vector2Int.zero)
            {
                ApplyMonsterMoveCrashAfterMovement(monster, moveResolution);

                hudService.RefreshHUDs();
                yield return new WaitForSeconds(GetActiveActionBeatDelay());
                yield break;
            }

            List<int> enteredGridIndices = moveResolution.EnteredGridIndices;

            Vector2Int mainCoord = gridManager.IndexToCoord(currentGridIndex);
            Vector2Int movedMainCoord = mainCoord + moveResolution.ActualOffset;
            // 예약은 2칸이어도 실제 이동 결과는 반드시 맵 내부여야 합니다.
            // 잘못된 좌표를 전투 데이터에 기록하거나 엉뚱한 인덱스로 래핑하지 않습니다.
            if (!gridManager.IsValidCoord(movedMainCoord))
            {
                Debug.LogWarning($"[BattleActionRunner] Invalid monster move destination: {movedMainCoord}");
                yield break;
            }

            int movedMainIndex = gridManager.CoordToIndex(movedMainCoord);
            if (gridManager.GetCellByIndex(movedMainIndex) == null)
                yield break;

            Vector3 pos = gridManager.GetWorldPositionByIndex(movedMainIndex);

            BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();

            if (animator != null)
                animator.PlayMove();

            if (BattleCameraController.Instance != null)
            {
                directCameraUnit = monster.transform;
                BattleCameraController.Instance.BeginDirectGridMovement(directCameraUnit);
            }

            yield return MoveTransformSmooth(
                monster.transform,
                monster.transform.position,
                pos,
                GetMoveAnimationDuration(enteredGridIndices.Count)
            );

            monster.MoveOccupiedCells(moveResolution.ActualOffset, gridManager);

            // 예약 후 다른 유닛이 경로를 막은 경우, 실제로 이동 가능한 지점까지 이동한 뒤
            // 막힌 유닛과 이동한 몬스터 양쪽에 충돌 피해를 적용합니다.
            ApplyMonsterMoveCrashAfterMovement(monster, moveResolution);

            // 블롭은 이동이 완료되면 이동 전 그리드에 잔여물을 남깁니다.
            // 잔여물의 피해는 이동 공격과 별개로 GridEffect 데이터에 따라 적용합니다.
            if (monster.RuntimeData != null &&
                string.Equals(monster.RuntimeData.MonsterId, BlobMonsterId, System.StringComparison.Ordinal))
            {
                TryPlaceResidue(currentGridIndex);
            }

            ApplyGridEffectsToMonster(enteredGridIndices, monster);
            ApplyMonsterBleedForMovedCells(monster, enteredGridIndices.Count);

            hudService.RefreshHUDs();

            yield return new WaitForSeconds(GetActiveActionBeatDelay());
        }
        finally
        {
            if (directCameraUnit != null && BattleCameraController.Instance != null)
                BattleCameraController.Instance.EndDirectGridMovement(directCameraUnit);
            ClearExecutionRange();
        }
    }



    private static void HideNocturnPortalDestinationIndicator(MonsterReservedCommand command)
    {
        if (command == null || command.RangeOriginGridIndex < 0)
            return;

        PlayerSkillReservationController reservationController =
            Object.FindFirstObjectByType<PlayerSkillReservationController>(
                FindObjectsInactive.Include);

        if (reservationController != null)
        {
            reservationController.HideNocturnPortalDestinationIndicator(
                command.RuntimeId,
                command.RangeOriginGridIndex);
        }
    }

    private void MarkNocturnPortalFailed(MonsterReservedCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.RuntimeId))
            return;

        nocturnPortalFailedRuntimeIds.Add(command.RuntimeId);
    }

    private void ClearNocturnPortalFailed(MonsterReservedCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.RuntimeId))
            return;

        nocturnPortalFailedRuntimeIds.Remove(command.RuntimeId);
    }

    private bool ConsumeNocturnPortalFailure(MonsterReservedCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.RuntimeId))
            return false;

        return nocturnPortalFailedRuntimeIds.Remove(command.RuntimeId);
    }

    private static bool IsNocturnPortalMove(MonsterReservedCommand command)
    {
        return command != null && command.IsPortalMove;
    }

    private IEnumerator ExecuteNocturnPortalMove(
        MonsterUnit monster,
        MonsterReservedCommand command,
        Vector2Int moveOffset)
    {
        if (monster == null || gridManager == null)
            yield break;

        // 사슬은 예약된 절대 목적지를 우선 사용합니다.
        // 앞선 행동으로 현재 위치가 달라지면 예약 당시의 상대 오프셋이 0이 될 수도 있으므로
        // 절대 목적지가 있는 명령은 MoveOffset이 0이어도 취소하지 않습니다.
        bool hasReservedDestination = command != null && command.RangeOriginGridIndex >= 0;

        if (!hasReservedDestination && moveOffset == Vector2Int.zero)
            yield break;

        int currentGridIndex = monster.MainGridIndex;

        if (currentGridIndex < 0)
            yield break;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);

        // 예약 당시의 절대 목적지가 있으면 그 값을 우선 사용합니다.
        // 앞선 이동이나 그랩 때문에 몬스터의 현재 위치가 달라져도 사슬 목적지가 바뀌지 않습니다.
        int destinationGridIndex = hasReservedDestination
            ? command.RangeOriginGridIndex
            : -1;

        Vector2Int destinationCoord;

        if (destinationGridIndex >= 0)
        {
            destinationCoord = gridManager.IndexToCoord(destinationGridIndex);
            moveOffset = destinationCoord - currentCoord;
        }
        else
        {
            destinationCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(destinationCoord))
                yield break;

            destinationGridIndex = gridManager.CoordToIndex(destinationCoord);
        }

        // 그림자걸음은 실제 이동 성공 여부와 무관하게 예약된 목적지 방향을 바라봅니다.
        // 목적지가 실행 직전에 막혀 순간이동에 실패하더라도, 이후 공격은 이 방향을 기준으로 실행됩니다.
        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

        if (facing != null && moveOffset != Vector2Int.zero)
            facing.FaceByMoveOffset(moveOffset);

        // 실행 직전까지 목적지 칸이 비어 있어야 합니다.
        // 캐릭터나 다른 몬스터가 해당 칸을 차지했다면 그림자걸음의 위치 이동만 취소합니다.
        if (BattleOccupancyService.IsOccupiedByAnyUnit(destinationGridIndex, null, monster))
        {
            MarkNocturnPortalFailed(command);
            HideNocturnPortalDestinationIndicator(command);
            yield break;
        }

        BattleGridEffectController gridEffectController =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (gridEffectController != null && gridEffectController.IsBlocked(destinationGridIndex))
        {
            MarkNocturnPortalFailed(command);
            HideNocturnPortalDestinationIndicator(command);
            yield break;
        }

        // 목적지 검사를 통과했으므로 이번 사슬은 성공 상태로 기록합니다.
        ClearNocturnPortalFailed(command);

        // 이동 연출이 시작되는 순간 목적지 잔여물을 제거합니다.
        HideNocturnPortalDestinationIndicator(command);

        Vector3 destinationPosition = gridManager.GetWorldPositionByIndex(destinationGridIndex);

        // 사슬 이동의 이동 상태임을 보여주기 위해 Move 애니메이션을 재생합니다.
        // 위치 보간은 사용하지 않고 목적지에는 즉시 배치합니다.
        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();

        if (animator != null)
        {
            animator.PlayMove();
            yield return new WaitForSeconds(
                BattlePresentationSpeedSettings.ScaleDuration(MoveAnimationDuration));
        }

        // 논리 그리드와 화면 위치를 함께 갱신해 실제 점유 위치를 전방 칸으로 이동시킵니다.
        monster.MoveOccupiedCells(moveOffset, gridManager);
        monster.transform.position = destinationPosition;

        if (animator != null)
        {
            animator.PlayMoveReverse();
            yield return new WaitForSeconds(
                BattlePresentationSpeedSettings.ScaleDuration(MoveAnimationDuration));
            animator.RestorePlaybackSpeed();
        }

        ApplyGridEffectsToMonster(new List<int> { destinationGridIndex }, monster);
        statusEffectService.ApplyBleedDamageToMonsterOnMove(monster);
        hudService.RefreshHUDs();
        yield return new WaitForSeconds(GetActiveActionBeatDelay());
    }

    private void ApplyPlayerBleedForMovedCells(BattleCharacter character, int movedCellCount)
    {
        for (int i = 0; i < movedCellCount; i++)
        {
            if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
                break;
            statusEffectService.ApplyBleedDamageToPlayerOnMove(character);
        }
    }

    private void ApplyMonsterBleedForMovedCells(MonsterUnit monster, int movedCellCount)
    {
        for (int i = 0; i < movedCellCount; i++)
        {
            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                break;
            statusEffectService.ApplyBleedDamageToMonsterOnMove(monster);
        }
    }

    private sealed class MonsterMoveResolution
    {
        public Vector2Int ActualOffset;
        public bool WasBlocked;
        public bool BlockedByUnit;
        public int BlockingUnitGridIndex = -1;
        public bool BlockedByGridEffect;
        public int BlockingGridEffectGridIndex = -1;
        public bool BlockedByBoundary;
        public List<int> EnteredGridIndices = new();
    }

    private void ApplyMonsterMoveCrashAfterMovement(
        MonsterUnit monster,
        MonsterMoveResolution moveResolution)
    {
        if (monster == null ||
            moveResolution == null ||
            !moveResolution.WasBlocked ||
            monster.RuntimeData == null ||
            monster.RuntimeData.IsDead)
        {
            return;
        }

        if (moveResolution.BlockedByUnit && moveResolution.BlockingUnitGridIndex >= 0)
        {
            ApplyCrashToBlockingUnitAtGrid(
                moveResolution.BlockingUnitGridIndex,
                null,
                monster);
            return;
        }

        if (moveResolution.BlockedByGridEffect &&
            moveResolution.BlockingGridEffectGridIndex >= 0)
        {
            BattleEffectInfoFeed.ShowCollision(true);
            ApplyCrashToMonster(monster);

            BattleGridEffectController controller = ResolveGridEffectController();
            if (controller != null)
            {
                controller.TryDamageEffect(
                    moveResolution.BlockingGridEffectGridIndex,
                    2,
                    out _);
            }

            return;
        }

        // 맵 경계는 충돌 대상이 아닙니다.
        // 이동 가능한 마지막 칸에서 멈추기만 하고 충돌 피해는 발생하지 않습니다.
        if (moveResolution.BlockedByBoundary)
            return;

        ApplyCrashToMonster(monster);
    }

    private MonsterMoveResolution ResolveMonsterMove(MonsterUnit monster, Vector2Int requestedOffset)
    {
        if (monster == null || gridManager == null || requestedOffset == Vector2Int.zero)
            return new MonsterMoveResolution();

        if (requestedOffset.x != 0 && requestedOffset.y != 0)
        {
            MonsterMoveResolution horizontalFirst =
                ResolveMonsterMoveAxisOrder(monster, requestedOffset, true, false);
            MonsterMoveResolution verticalFirst =
                ResolveMonsterMoveAxisOrder(monster, requestedOffset, false, false);

            bool useVerticalFirst =
                GetMoveDistance(verticalFirst.ActualOffset) >
                GetMoveDistance(horizontalFirst.ActualOffset);

            return ResolveMonsterMoveAxisOrder(
                monster,
                requestedOffset,
                !useVerticalFirst,
                true);
        }

        return ResolveMonsterMoveAxisOrder(
            monster,
            requestedOffset,
            requestedOffset.x != 0,
            true);
    }

    private MonsterMoveResolution ResolveMonsterMoveAxisOrder(
        MonsterUnit monster,
        Vector2Int requestedOffset,
        bool horizontalFirst,
        bool applyCrashToBlockingUnit)
    {
        MonsterMoveResolution result = new();
        List<Vector2Int> currentCoords = new();

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
            currentCoords.Add(gridManager.IndexToCoord(monster.OccupiedGridIndices[i]));

        Vector2Int startMainCoord = gridManager.IndexToCoord(monster.MainGridIndex);
        bool completed;

        if (horizontalFirst)
        {
            completed = TryApplyMonsterMoveAxisSteps(currentCoords, requestedOffset.x, true, monster, result.EnteredGridIndices, applyCrashToBlockingUnit, result);
            if (completed)
                completed = TryApplyMonsterMoveAxisSteps(currentCoords, requestedOffset.y, false, monster, result.EnteredGridIndices, applyCrashToBlockingUnit, result);
        }
        else
        {
            completed = TryApplyMonsterMoveAxisSteps(currentCoords, requestedOffset.y, false, monster, result.EnteredGridIndices, applyCrashToBlockingUnit, result);
            if (completed)
                completed = TryApplyMonsterMoveAxisSteps(currentCoords, requestedOffset.x, true, monster, result.EnteredGridIndices, applyCrashToBlockingUnit, result);
        }

        if (currentCoords.Count > 0)
            result.ActualOffset = currentCoords[0] - startMainCoord;

        result.WasBlocked = !completed || result.ActualOffset != requestedOffset;
        return result;
    }

    private bool CanApplyMonsterMove(MonsterUnit monster, Vector2Int moveOffset)
    {
        if (monster == null || gridManager == null)
            return false;

        if (moveOffset == Vector2Int.zero)
            return false;

        if (moveOffset.x != 0 && moveOffset.y != 0)
        {
            return CanApplyMonsterMoveAxisOrder(monster, moveOffset, true) ||
                   CanApplyMonsterMoveAxisOrder(monster, moveOffset, false);
        }

        return CanApplyMonsterMoveAxisOrder(monster, moveOffset, moveOffset.x != 0);
    }

    private bool CanApplyMonsterMoveAxisOrder(
        MonsterUnit monster,
        Vector2Int moveOffset,
        bool horizontalFirst)
    {
        List<Vector2Int> currentCoords = new();

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
            currentCoords.Add(gridManager.IndexToCoord(monster.OccupiedGridIndices[i]));

        if (horizontalFirst)
        {
            return TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.x, true, monster) &&
                   TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.y, false, monster);
        }

        return TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.y, false, monster) &&
               TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.x, true, monster);
    }

    private bool TryApplyMonsterMoveAxisSteps(
        List<Vector2Int> currentCoords,
        int amount,
        bool horizontal,
        MonsterUnit monster,
        List<int> enteredGridIndices = null,
        bool applyCrashToBlockingUnit = false,
        MonsterMoveResolution moveResolution = null)
    {
        int remaining = amount;

        while (remaining != 0)
        {
            int step = remaining > 0 ? 1 : -1;
            List<Vector2Int> nextCoords = new();

            for (int i = 0; i < currentCoords.Count; i++)
            {
                Vector2Int nextCoord = currentCoords[i] + (horizontal
                    ? new Vector2Int(step, 0)
                    : new Vector2Int(0, step));

                if (!gridManager.IsValidCoord(nextCoord))
                {
                    if (applyCrashToBlockingUnit && moveResolution != null)
                        moveResolution.BlockedByBoundary = true;

                    return false;
                }

                int targetIndex = gridManager.CoordToIndex(nextCoord);

                if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, monster))
                {
                    if (applyCrashToBlockingUnit && moveResolution != null)
                    {
                        moveResolution.BlockedByUnit = true;
                        moveResolution.BlockingUnitGridIndex = targetIndex;
                    }

                    return false;
                }

                if (IsGridEffectBlocked(targetIndex))
                {
                    if (applyCrashToBlockingUnit && moveResolution != null)
                    {
                        moveResolution.BlockedByGridEffect = true;
                        moveResolution.BlockingGridEffectGridIndex = targetIndex;
                    }

                    return false;
                }

                nextCoords.Add(nextCoord);
                AddUnique(enteredGridIndices, targetIndex);
            }

            currentCoords.Clear();
            currentCoords.AddRange(nextCoords);
            remaining -= step;
        }

        return true;
    }

    private List<int> BuildMonsterEnteredGridIndices(MonsterUnit monster, Vector2Int moveOffset)
    {
        List<int> enteredGridIndices = new();

        if (monster == null || gridManager == null || moveOffset == Vector2Int.zero)
            return enteredGridIndices;

        if (moveOffset.x != 0 && moveOffset.y != 0)
        {
            if (TryBuildMonsterMovePath(monster, moveOffset, true, enteredGridIndices))
                return enteredGridIndices;

            enteredGridIndices.Clear();

            if (TryBuildMonsterMovePath(monster, moveOffset, false, enteredGridIndices))
                return enteredGridIndices;

            enteredGridIndices.Clear();
            return enteredGridIndices;
        }

        TryBuildMonsterMovePath(monster, moveOffset, moveOffset.x != 0, enteredGridIndices);
        return enteredGridIndices;
    }

    private bool TryBuildMonsterMovePath(
        MonsterUnit monster,
        Vector2Int moveOffset,
        bool horizontalFirst,
        List<int> enteredGridIndices)
    {
        if (monster == null || gridManager == null)
            return false;

        List<Vector2Int> currentCoords = new();

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
            currentCoords.Add(gridManager.IndexToCoord(monster.OccupiedGridIndices[i]));

        if (horizontalFirst)
        {
            return TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.x, true, monster, enteredGridIndices) &&
                   TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.y, false, monster, enteredGridIndices);
        }

        return TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.y, false, monster, enteredGridIndices) &&
               TryApplyMonsterMoveAxisSteps(currentCoords, moveOffset.x, true, monster, enteredGridIndices);
    }

    private bool TryHandleImmediateMonsterUnitCollision(
        MonsterUnit monster,
        Vector2Int requestedOffset)
    {
        if (monster == null || gridManager == null || requestedOffset == Vector2Int.zero)
            return false;

        Vector2Int firstStep;

        if (requestedOffset.x != 0)
            firstStep = new Vector2Int(requestedOffset.x > 0 ? 1 : -1, 0);
        else
            firstStep = new Vector2Int(0, requestedOffset.y > 0 ? 1 : -1);

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            Vector2Int occupiedCoord = gridManager.IndexToCoord(monster.OccupiedGridIndices[i]);
            Vector2Int nextCoord = occupiedCoord + firstStep;

            // 맵 경계는 충돌 피해 대상이 아닙니다.
            if (!gridManager.IsValidCoord(nextCoord))
                return false;

            int nextGridIndex = gridManager.CoordToIndex(nextCoord);

            if (!BattleOccupancyService.IsOccupiedByAnyUnit(nextGridIndex, null, monster))
                continue;

            ApplyCrashToBlockingUnitAtGrid(nextGridIndex, null, monster);
            return true;
        }

        return false;
    }

    private void ApplyCrashToBlockingUnitAtGrid(
        int gridIndex,
        string movingCharacterId,
        MonsterUnit movingMonster)
    {
        const int baseCrashDamage = 2;

        BattleCharacter movingCharacter = !string.IsNullOrWhiteSpace(movingCharacterId)
            ? unitFinder.FindBattleCharacter(movingCharacterId)
            : null;

        if (BattleOccupancyService.TryGetCharacterAtGrid(
                gridIndex,
                out BattleCharacter blockingCharacter,
                movingCharacterId))
        {
            BattleEffectInfoFeed.ShowCollision(movingMonster != null);
            int damageToMovingUnit = baseCrashDamage +
                BattleEquipmentEffectService.GetCollisionTargetDamageDelta(blockingCharacter.RuntimeData);
            int damageToBlockingCharacter = baseCrashDamage +
                BattleEquipmentEffectService.GetCollisionTargetDamageDelta(
                    movingCharacter != null ? movingCharacter.RuntimeData : null);

            bool movingMonsterKilled = false;

            if (movingCharacter != null)
                ApplyCrashToPlayer(movingCharacter, damageToMovingUnit);
            else if (movingMonster != null)
                movingMonsterKilled = ApplyCrashToMonster(movingMonster, damageToMovingUnit);

            ApplyCrashToPlayer(blockingCharacter, damageToBlockingCharacter);

            BattleEquipmentEffectService.ApplyPlayerCollisionEffects(
                movingCharacter,
                blockingCharacter,
                null);
            BattleEquipmentEffectService.ApplyPlayerCollisionEffects(
                blockingCharacter,
                movingCharacter,
                movingMonster,
                movingMonsterKilled);
            return;
        }

        if (BattleOccupancyService.TryGetMonsterAtGrid(
                gridIndex,
                out MonsterUnit blockingMonster,
                movingMonster))
        {
            BattleEffectInfoFeed.ShowCollision(movingMonster != null);
            int damageToBlockingMonster = baseCrashDamage +
                BattleEquipmentEffectService.GetCollisionTargetDamageDelta(
                    movingCharacter != null ? movingCharacter.RuntimeData : null);

            if (movingCharacter != null)
                ApplyCrashToPlayer(movingCharacter, baseCrashDamage);
            else if (movingMonster != null)
                ApplyCrashToMonster(movingMonster, baseCrashDamage);

            bool blockingMonsterKilled = ApplyCrashToMonster(blockingMonster, damageToBlockingMonster);
            BattleEquipmentEffectService.ApplyPlayerCollisionEffects(
                movingCharacter,
                null,
                blockingMonster,
                blockingMonsterKilled);
        }
    }

    private void ApplyCrashToPlayer(BattleCharacter target, int damage = 2)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        BattleEffectPlaneRotation.PlayCollisionRotationFeedback();

        new CrashEffect().Execute(new BattleEffectContext
        {
            PlayerTarget = target,
            GridManager = gridManager,
            EffectId = "E_Crash",
            Value = Mathf.Max(0, damage),
            Count = 1
        });
    }

    private bool ApplyCrashToMonster(MonsterUnit target, int damage = 2)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return false;

        BattleEffectPlaneRotation.PlayCollisionRotationFeedback();

        bool wasAlive = !target.RuntimeData.IsDead;

        new CrashEffect().Execute(new BattleEffectContext
        {
            MonsterTarget = target,
            GridManager = gridManager,
            EffectId = "E_Crash",
            Value = Mathf.Max(0, damage),
            Count = 1
        });

        bool killedByCrash = wasAlive && target.RuntimeData != null && target.RuntimeData.IsDead;

        if (killedByCrash)
            deathService.HandleMonsterDead(target);

        return killedByCrash;
    }

    private IEnumerator ExecuteMonsterSkill(MonsterReservedCommand command, bool isCounterAttack = false)
    {
        if (command == null || command.SkillData == null)
            yield break;

        MonsterUnit monster = unitFinder.FindMonsterUnit(command.RuntimeId);

        if (monster == null)
            yield break;

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();
        BattleDirection liveMonsterDirection = monster.RuntimeData != null
            ? monster.RuntimeData.Direction
            : facing != null && !facing.IsFacingRight
                ? BattleDirection.Left
                : BattleDirection.Right;
        BattleDirection monsterExecutionDirection = liveMonsterDirection;
        bool usesLockedTargetGrid =
            BattleAttackExecutionDirectionPolicy.ShouldPreserveMonsterTargetGrid(
                command.AttackSpatialPolicy) &&
            command.TargetGridIndices != null &&
            command.TargetGridIndices.Count > 0 && gridManager != null &&
            monster.MainGridIndex >= 0;
        if (usesLockedTargetGrid)
        {
            int targetIndex = command.TargetGridIndices[0];
            Vector2Int targetCoord = gridManager.IndexToCoord(targetIndex);
            Vector2Int casterCoord = gridManager.IndexToCoord(monster.MainGridIndex);
            if (gridManager.IsValidCoord(targetCoord) && targetCoord.x != casterCoord.x)
                monsterExecutionDirection = targetCoord.x > casterCoord.x
                    ? BattleDirection.Right : BattleDirection.Left;
            else if (command.HasForcedDirection)
                monsterExecutionDirection = command.ForcedDirection;
        }

        if (facing == null)
            facing = monster.GetComponentInChildren<BattleUnitFacing>(true);
        if (facing != null)
            facing.FaceRight(monsterExecutionDirection == BattleDirection.Right);
        if (monster.RuntimeData != null)
            monster.RuntimeData.Direction = monsterExecutionDirection;

        if (BattleSkillNamePopupPresentation.ShouldShow(command))
        {
            BattleSkillTextPopupUI.Show(
                monster.transform,
                BattleSkillNamePopupPresentation.GetDisplayName(command));
        }

        bool shouldControlAttackPlane = isCounterAttack || ShouldControlAttackPlaneForMonsterSkill(command);
        PlaneAttackType monsterPlaneAttackType = isCounterAttack ? PlaneAttackType.Melee : ShouldSkipMonsterSkillCamera(command)
            ? PlaneAttackType.Ranged
            : PlaneAttackType.Melee;

        // 직전 사슬이 점유 또는 이동 불가로 취소됐다면 예약된 사슬 위치를 공격 원점으로 사용하지 않습니다.
        // 몬스터의 실제 현재 위치를 원점으로 바꾸고, 현재 위치에서 실제 대상을 향해 방향을 다시 정합니다.
        if (ConsumeNocturnPortalFailure(command))
        {
            command.SetRangeOriginGridIndex(monster.MainGridIndex);
            command.ClearForcedDirection();
        }

        if (command.SkillData.SkillId == "S_Monster_12" ||
            command.SkillData.SkillId == "S_Monster_22")
        {
            // 베스파 돌진은 실행 순간의 실제 위치를 범위 원점으로 사용합니다.
            // 선행 이동이 충돌로 실패하거나 일부만 성공해도 예약 당시 예상 위치를 표시하지 않습니다.
            if (command.SkillData.SkillId == VespaAttackSkillId)
            {
                AlignRelativeRangeOriginToActualPosition(monster, command);
                RecalculateMonsterSkillRangeAtExecution(monster, command);
            }

            ShowExecutionRange(BuildMonsterSkillExecutionRange(command, monster.MainGridIndex), GetMonsterExecutionRangeType(command));
            try
            {
                yield return ExecuteMonsterDashAttack(command);
            }
            finally
            {
                EndAttackPlaneFeedbackIfNeeded(shouldControlAttackPlane);
                ClearExecutionRange();
            }

            yield break;
        }

        // 확정된 방향을 기준으로 공격 범위를 계산합니다.
        // 머크는 예약된 공격 오프셋을 유지하고, 블롭은 실제 현재 위치 자체를 공격 원점으로 사용합니다.
        AlignRelativeRangeOriginToActualPosition(monster, command);

        RecalculateMonsterSkillRangeAtExecution(monster, command);

        BattleCharacter firstPlayerTarget = FindFirstPlayerTarget(command);

        BattleUnitAnimator monsterAnimator = monster.GetComponent<BattleUnitAnimator>();


        bool isRookEarthquake =
            string.Equals(command.MonsterId, RookMonsterId, System.StringComparison.Ordinal) &&
            string.Equals(command.SkillId, RookEarthquakeSkillId, System.StringComparison.Ordinal);

        if (isRookEarthquake && BattleCameraController.Instance != null)
        {
            BattleCameraController.Instance.SetHoldDefaultReturn(false);
            yield return BattleCameraController.Instance.ReturnDefault();
        }

        ShowExecutionRange(BuildMonsterSkillExecutionRange(command, monster.MainGridIndex), GetMonsterExecutionRangeType(command));
        try
        {
            if (ShouldEnterCameraForActiveAction() && BattleCameraController.Instance != null)
            {
                List<Transform> focusTargets = new();
                BattleCharacter[] potentialTargets = Object.FindObjectsByType<BattleCharacter>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (BattleCharacter target in potentialTargets)
                {
                    if (target != null && target.RuntimeData != null && !target.RuntimeData.IsDead &&
                        command.RangeGridIndices != null &&
                        command.RangeGridIndices.Contains(target.CurrentGridIndex))
                        focusTargets.Add(target.transform);
                }
                // 아군 몬스터를 대상으로 하는 버프도 실제 범위의 몬스터를 추적합니다.
                if (IsMonsterStatusSkill(command) &&
                    command.SkillData.Target == TargetType.EnemyParty)
                {
                    MonsterUnit[] monsterRecipients = Object.FindObjectsByType<MonsterUnit>(
                        FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    foreach (MonsterUnit recipient in monsterRecipients)
                    {
                        if (recipient != null && recipient.RuntimeData != null &&
                            !recipient.RuntimeData.IsDead &&
                            command.RangeGridIndices != null &&
                            command.RangeGridIndices.Contains(recipient.MainGridIndex))
                            focusTargets.Add(recipient.transform);
                    }
                }
                // Muck's projectile impacts a reserved grid cell, even when
                // the original character has moved away. Frame the monster
                // and that fixed cell at the screen centre; do not use the
                // normal ranged-caster-at-screen-edge camera composition.
                Vector3 muckImpactPosition = Vector3.zero;
                bool muckFixedImpact =
                    string.Equals(command.MonsterId, MuckMonsterId, System.StringComparison.Ordinal) &&
                    string.Equals(command.SkillId, MuckProjectileSkillId, System.StringComparison.Ordinal) &&
                    TryGetGroundTargetedProjectilePosition(command, out muckImpactPosition);
                if (muckFixedImpact)
                {
                    yield return BattleCameraController.Instance.ZoomForActionTargets(
                        monster.transform, null, false, true, muckImpactPosition);
                }
                else
                {
                    if (focusTargets.Count == 0 && firstPlayerTarget != null)
                        focusTargets.Add(firstPlayerTarget.transform);
                    yield return BattleCameraController.Instance.ZoomForActionTargets(
                        monster.transform, focusTargets, IsMonsterStatusSkill(command),
                        ShouldSkipMonsterSkillCamera(command));
                }
            }

            // 카메라의 공격자 포커스 후 기울기와 공격 애니메이션을 함께 시작합니다.
            BeginAttackPlaneFeedbackIfNeeded(
                shouldControlAttackPlane,
                monsterPlaneAttackType,
                // 드라우그 반격은 실제 사정거리 밖이어도 공격 행동을 재생하므로 회전도 실행합니다.
                isCounterAttack || HasAliveMonsterSkillTarget(monster, command));

            bool hasDamageHitEffect = monsterSkillEffectService.HasDamageHitEffect(command);

            if (hasDamageHitEffect)
            {
                yield return ExecuteMonsterDamageHitSequence(monster, command, monsterAnimator);
                monsterSkillEffectService.ApplyMonsterSkillNonDamageEffects(monster, command);
            }
            else
            {
                if (monsterAnimator != null)
                    monsterAnimator.PlayMonsterSkillAction(command);

                yield return PlayMonsterProjectileVfxIfNeeded(monster, command, monsterAnimator);

                monsterSkillEffectService.ApplyMonsterSkill(monster, command);

                if (firstPlayerTarget != null)
                    yield return new WaitForSeconds(HitCameraDelay / GetActivePresentationSpeed());
                else
                    yield return new WaitForSeconds(GetActiveActionBeatDelay());
            }

            // 상태 효과를 실제로 받는 유닛이 있을 때만 줌 펄스를 사용합니다.
            // 빈 범위는 공격 미적중처럼 빠르게 다음 행동으로 넘어갑니다.
            if (IsMonsterStatusSkill(command) && MonsterCommandHasInteraction(command))
                yield return PlayStatusEffectCameraFeedback();

            // 신더의 자폭은 공격 효과 적용이 끝난 후 자신을 제거합니다.
            // 일반 처치가 아니므로 분열 등 고유 사망 시 효과와 처치 보상은 발생하지 않습니다.
            if (monster.RuntimeData != null &&
                monster.RuntimeData.MonsterId == "Mon_06" &&
                command.SkillData.SkillId == "S_Monster_14")
            {
                monster.RuntimeData.IsExplodeReady = false;
                monster.RuntimeData.CurrentHP = 0;
                deathService.HandleMonsterDeadWithoutReward(monster);
            }

            // 녹턴의 26/27 공격 모션 완료 후 Idle로 돌아와 다음 행동을 시작합니다.
            if (monsterAnimator != null &&
                string.Equals(command.MonsterId, "Mon_10", System.StringComparison.Ordinal) &&
                (string.Equals(command.SkillId, "S_Monster_26", System.StringComparison.Ordinal) ||
                 string.Equals(command.SkillId, "S_Monster_27", System.StringComparison.Ordinal)))
            {
                yield return monsterAnimator.WaitForAttackReturnToIdle();
            }

            yield return ReturnCameraAfterActiveActionIfNeeded();
        }
        finally
        {
            EndAttackPlaneFeedbackIfNeeded(shouldControlAttackPlane);
            ClearExecutionRange();
        }
    }

    private IEnumerator ExecuteMonsterDamageHitSequence(
        MonsterUnit monster,
        MonsterReservedCommand command,
        BattleUnitAnimator monsterAnimator)
    {
        if (monster == null || command == null || command.SkillData == null)
            yield break;

        int hitCount = monsterSkillEffectService.GetDamageHitCount(command);
        BattleRearAttackActionSnapshot rearAttackSnapshot = new();

        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            if (monster.RuntimeData == null || monster.RuntimeData.IsDead)
                yield break;

            // A hit reaction may have turned the monster towards its attacker.
            // Restore the direction of the current reserved attack before EVERY hit
            // (including subsequent hits of a multi-hit skill).
            BattleUnitFacing actionFacing = monster.GetComponent<BattleUnitFacing>();
            if (actionFacing == null)
                actionFacing = monster.GetComponentInChildren<BattleUnitFacing>(true);
            if (actionFacing != null && monster.RuntimeData != null)
                actionFacing.FaceRight(monster.RuntimeData.Direction == BattleDirection.Right);

            bool hasAliveTarget = HasAliveMonsterSkillTarget(monster, command);
            bool canPlayProjectileVfx = ShouldPlayMonsterProjectileVfx(monster, command, monsterAnimator);

            // 명중 대상이 없어도 몬스터의 공격 모션은 반드시 재생합니다.
            // 대상 부재는 피해 적용만 건너뛰며, 공격 시도 자체를 취소하지 않습니다.
            if (monsterAnimator != null)
                monsterAnimator.PlayMonsterSkillAction(command);

            if (canPlayProjectileVfx)
                yield return PlayMonsterProjectileVfxIfNeeded(monster, command, monsterAnimator);
            else
                yield return new WaitForSeconds(GetActiveActionBeatDelay());

            if (!hasAliveTarget)
            {
                yield return new WaitForSeconds(GetActiveActionBeatDelay());
                yield break;
            }

            bool isRangedPresentation = ShouldSkipMonsterSkillCamera(command);
            List<Transform> feedbackTargets = BuildMonsterSkillFeedbackTargets(monster, command);

            bool prepareCameraImpact = ShouldPlayCameraImpactForHit(hitIndex, hitCount);
            if (feedbackTargets.Count > 0 && prepareCameraImpact && BattleCameraController.Instance != null)
            {
                if (isRangedPresentation)
                {
                    yield return BattleCameraController.Instance.PrepareRangedDamageImpact();
                }
                else
                {
                    int impactDirection = ResolveCameraImpactDirection(
                        monster.transform,
                        feedbackTargets,
                        GetMonsterImpactFallbackDirection(monster));
                    yield return BattleCameraController.Instance.PrepareDamageImpact(impactDirection);
                }
            }

            monsterSkillEffectService.ApplyMonsterSkillDamageHit(
                monster,
                command,
                hitIndex,
                rearAttackSnapshot);

            if (monster.RuntimeData == null || monster.RuntimeData.IsDead)
                yield break;

            bool playCameraImpact = ShouldPlayCameraImpactForHit(hitIndex, hitCount);
            bool playPushFeedback = ShouldPlayExternalImpactForHit(hitIndex, hitCount);

            if (feedbackTargets.Count > 0 && playCameraImpact)
            {
                if (isRangedPresentation)
                {
                    yield return PlayRangedDamageCameraFeedback();
                }
                else if (playPushFeedback)
                {
                    bool nonDirectionalMonsterAttack =
                        string.Equals(command.MonsterId, RancorMonsterId, System.StringComparison.Ordinal) ||
                        string.Equals(command.MonsterId, BlightMonsterId, System.StringComparison.Ordinal) ||
                        (string.Equals(command.MonsterId, CinderMonsterId, System.StringComparison.Ordinal) &&
                         string.Equals(command.SkillId, CinderExplodeSkillId, System.StringComparison.Ordinal));

                    if (nonDirectionalMonsterAttack)
                    {
                        // 좌우 대칭 공격: 타깃 방향으로 밀지 않고 제자리에서 10% 확대합니다.
                        yield return BattleHitImpactFeedback.PlayNonDirectionalMonsterAttack(
                            monster != null ? monster.transform : null);
                    }
                    else
                    {
                        yield return PlayDamageHitFeedback(
                            monster != null ? monster.transform : null,
                            feedbackTargets,
                            GetMonsterImpactFallbackDirection(monster));
                    }
                }
                else
                {
                    yield return PlayDamageCameraFeedback();
                }
            }

            float hitDelay = playCameraImpact ? HitCameraDelay : ActionDelay;
            yield return new WaitForSeconds(hitDelay / GetActivePresentationSpeed());
        }
    }

    private void AlignRelativeRangeOriginToActualPosition(
        MonsterUnit monster,
        MonsterReservedCommand command)
    {
        if (monster == null || command == null || monster.MainGridIndex < 0)
            return;

        command.SetRangeOriginGridIndex(
            BattleAttackExecutionDirectionPolicy.ResolveExecutionOriginGridIndex(
                command.AttackSpatialPolicy,
                monster.MainGridIndex,
                command.RangeOriginGridIndex));
    }

    private void RecalculateMonsterSkillRangeAtExecution(
    MonsterUnit monster,
    MonsterReservedCommand command)
    {
        if (monster == null || command == null || command.SkillData == null)
            return;

        bool isBarrowDirectShot =
            string.Equals(command.MonsterId, BarrowMonsterId, System.StringComparison.Ordinal) &&
            string.Equals(command.SkillId, BarrowDirectShotSkillId, System.StringComparison.Ordinal);

        // 그리드 지정 공격만 예약 목표를 유지합니다.
        // 그 외 공격은 예약 범위 캐시와 무관하게 실제 위치와 실행 직전 방향으로 다시 계산합니다.
        if (BattleAttackExecutionDirectionPolicy.ShouldPreserveMonsterTargetGrid(
                command.AttackSpatialPolicy))
        {
            return;
        }

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

        bool facingRight = monster.RuntimeData != null
            ? monster.RuntimeData.Direction == BattleDirection.Right
            : facing == null || facing.IsFacingRight;

        List<int> rangeGridIndices =
            MonsterSkillRangeService.BuildRangeGridIndices(
                monster,
                command.SkillData,
                gridManager,
                facingRight,
                command.RangeOriginGridIndex
            );

        List<int> targetGridIndices =
            MonsterSkillRangeService.FilterTargetGridIndices(
                command.SkillData,
                rangeGridIndices
            );

        if (isBarrowDirectShot && gridManager != null)
        {
            // 장애물은 관통하되, 발사 방향에서 가장 먼저 만나는 살아 있는 캐릭터만 타격합니다.
            // 이동 후 위치와 피격에 따른 실제 방향을 기준으로 매 실행 시 갱신합니다.
            int origin = monster.MainGridIndex;
            if (origin >= 0)
            {
                Vector2Int originCoord = gridManager.IndexToCoord(origin);
                BattleCharacter[] characters = UnityEngine.Object.FindObjectsByType<BattleCharacter>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                int closestGrid = -1;
                int closestDistance = int.MaxValue;
                for (int i = 0; i < characters.Length; i++)
                {
                    BattleCharacter character = characters[i];
                    if (character == null || character.RuntimeData == null ||
                        character.RuntimeData.IsDead || character.CurrentGridIndex < 0)
                        continue;

                    Vector2Int coordinate = gridManager.IndexToCoord(character.CurrentGridIndex);
                    int dx = coordinate.x - originCoord.x;
                    if (coordinate.y != originCoord.y ||
                        (facingRight ? dx <= 0 : dx >= 0))
                        continue;
                    int distance = Mathf.Abs(dx);
                    if (distance >= closestDistance || !rangeGridIndices.Contains(character.CurrentGridIndex))
                        continue;
                    closestDistance = distance;
                    closestGrid = character.CurrentGridIndex;
                }

                targetGridIndices.Clear();
                if (closestGrid >= 0)
                    targetGridIndices.Add(closestGrid);
            }
        }

        command.SetRangeResult(rangeGridIndices, targetGridIndices);
    }

    private static bool IsNocturnDirectionalAttack(MonsterReservedCommand command)
    {
        if (command == null ||
            !string.Equals(command.MonsterId, NocturnMonsterId, System.StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals(command.SkillId, NocturnThrustSkillId, System.StringComparison.Ordinal) ||
               string.Equals(command.SkillId, NocturnSlashSkillId, System.StringComparison.Ordinal) ||
               string.Equals(command.SkillId, NocturnPullSkillId, System.StringComparison.Ordinal);
    }

    private bool IsNearestPlayerToRight(int originGridIndex)
    {
        if (gridManager == null || originGridIndex < 0)
            return false;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
        int nearestGridIndex = -1;
        int nearestDistance = int.MaxValue;

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (character.RuntimeData.IsDead || character.CurrentGridIndex < 0)
                continue;

            Vector2Int targetCoord = gridManager.IndexToCoord(character.CurrentGridIndex);
            int distance =
                Mathf.Abs(targetCoord.x - originCoord.x) +
                Mathf.Abs(targetCoord.y - originCoord.y);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestGridIndex = character.CurrentGridIndex;
            }
        }

        BattleGridEffectController gridEffectController = ResolveGridEffectController();

        if (gridEffectController != null)
        {
            IReadOnlyList<int> characterTargetGridIndices =
                gridEffectController.GetCharacterTargetGridIndices();

            for (int i = 0; i < characterTargetGridIndices.Count; i++)
            {
                int gridIndex = characterTargetGridIndices[i];
                Vector2Int targetCoord = gridManager.IndexToCoord(gridIndex);
                int distance =
                    Mathf.Abs(targetCoord.x - originCoord.x) +
                    Mathf.Abs(targetCoord.y - originCoord.y);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestGridIndex = gridIndex;
                }
            }
        }

        if (nearestGridIndex < 0)
            return false;

        return gridManager.IndexToCoord(nearestGridIndex).x >= originCoord.x;
    }

    private BattleCharacter FindFirstPlayerTarget(MonsterReservedCommand command)
    {
        if (command == null || command.TargetGridIndices == null)
            return null;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (character.RuntimeData.IsDead)
                continue;

            if (command.TargetGridIndices.Contains(character.CurrentGridIndex))
                return character;
        }

        return null;
    }

    private bool HasAliveMonsterSkillTarget(
        MonsterUnit caster,
        MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;

        switch (command.SkillData.Target)
        {
            case TargetType.PlayerParty:
                return FindFirstAlivePlayerTarget(command) != null ||
                       HasCharacterGridEffectTarget(command);

            case TargetType.EnemyParty:
                return FindFirstAliveMonsterTarget(command, caster) != null;

            case TargetType.Self:
                return caster != null &&
                       caster.RuntimeData != null &&
                       !caster.RuntimeData.IsDead;

            default:
                return true;
        }
    }

    private bool HasCharacterGridEffectTarget(MonsterReservedCommand command)
    {
        if (command == null || command.TargetGridIndices == null)
            return false;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null)
            return false;

        for (int i = 0; i < command.TargetGridIndices.Count; i++)
        {
            if (controller.IsCharacterTargetEffect(command.TargetGridIndices[i]))
                return true;
        }

        return false;
    }

    private static bool ShouldControlAttackPlaneForPlayerSkill(PlayerReservedCommand command)
    {
        return command != null &&
               command.SkillData != null &&
               command.SkillData.SkillType == SkillType.Attack;
    }

    private static bool ShouldControlAttackPlaneForMonsterSkill(MonsterReservedCommand command)
    {
        return command != null &&
               command.SkillData != null &&
               command.SkillData.TimelineNotation == TimelineActionType.Attack;
    }

    private void BeginPlayerAttackPlaneAtActionStart(
        BattleCharacter attacker,
        PlayerReservedCommand command)
    {
        if (!ShouldControlAttackPlaneForPlayerSkill(command) ||
            playerAttackPlaneStartedForCurrentSkill)
            return;

        bool hasRecipient = HasPlayerAttackPlaneRecipient(attacker, command);
        if (!hasRecipient)
            return;

        // Effect Count가 2~5인 다단히트도 스킬 단위로 최초 Action에서만 회전합니다.
        // 반격은 별도 몬스터 행동이므로 이 플래그와 관계없이 독립적으로 회전합니다.
        playerAttackPlaneStartedForCurrentSkill = true;
        BeginAttackPlaneFeedbackIfNeeded(
            true,
            ShouldSkipPlayerSkillCamera(command) ? PlaneAttackType.Ranged : PlaneAttackType.Melee,
            true);
    }

    private void BeginAttackPlaneFeedbackIfNeeded(
        bool shouldControlAttackPlane,
        PlaneAttackType attackType,
        bool hasRecipient)
    {
        // BattleEffect의 upper/under 기울기 연출은 실제 타격을 받는 대상이 있을 때만 재생합니다.
        // 빈 칸을 공격하거나 대상이 이미 사라진 경우에는 공격 모션만 재생합니다.
        if (!shouldControlAttackPlane || !hasRecipient)
            return;

        // 연속 행동 중 앞 행동은 빗나가고 뒤 행동만 명중할 수도 있으므로 그룹 시작 여부로 막지 않습니다.
        // 이미 연출 중이라면 BattleEffectPlaneRotation에서 중복 시작을 무시합니다.
        BattleEffectPlaneRotation.BeginAttackRotationFeedback(attackType);
    }

    private bool HasPlayerAttackPlaneRecipient(
        BattleCharacter attacker,
        PlayerReservedCommand command)
    {
        if (attacker == null || command == null || command.SkillData == null)
            return false;

        if (ShouldPlayerSkillTargetSelf(command))
            return attacker.RuntimeData != null && !attacker.RuntimeData.IsDead;

        if (ShouldPlayerSkillTargetPlayerParty(command))
            return HasPlayerPartyTarget(command);

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null ||
                monster.RuntimeData == null ||
                monster.RuntimeData.IsDead)
            {
                continue;
            }

            if (IsMonsterInRange(monster, command))
                return true;
        }

        return false;
    }

    private void EndAttackPlaneFeedbackIfNeeded(bool shouldControlAttackPlane)
    {
        if (!shouldControlAttackPlane)
            return;

        if (activeActionInfo.IsGrouped && !activeActionInfo.IsGroupEnd)
            return;

        BattleEffectPlaneRotation.EndAttackRotationFeedback();
    }

    private static IEnumerator PlayStatusEffectCameraFeedback()
    {
        if (BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.PlayStatusEffectZoomPulse();
    }

    private static bool IsPlayerStatusSkill(PlayerReservedCommand command)
    {
        return command != null && command.SkillData != null &&
            (command.SkillData.SkillType == SkillType.Buff ||
             command.SkillData.SkillType == SkillType.Debuff);
    }

    private static bool IsMonsterStatusSkill(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;
        return command.SkillData.TimelineNotation == TimelineActionType.Buff ||
               command.SkillData.TimelineNotation == TimelineActionType.Debuff;
    }

    private static bool ShouldSkipPlayerSkillCamera(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;

        // 이동 스킬은 Range_All을 사용하더라도 카메라 연출 분류에서는 제외합니다.
        if (BattleEquipmentEffectService.IsMoveCommand(command))
            return false;

        string rangeId = BattleEquipmentEffectService.GetEffectiveRangeId(
            command.UserRuntime,
            command.SkillData);

        // Range_All은 UI 분류상 '전체'이지만, 이동 스킬이 아닌 경우
        // 전투 카메라 연출은 원거리 스킬과 동일하게 처리합니다.
        if (BattleRangeCalculator.IsAllRangeId(rangeId))
            return true;

        return command.SkillData.RangeType == RangeType.Selection;
    }

    private static bool ShouldSkipMonsterSkillCamera(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;

        TimelineActionType notation = command.SkillData.TimelineNotation;

        if (notation == TimelineActionType.Buff ||
            notation == TimelineActionType.Debuff)
        {
            return true;
        }

        return notation == TimelineActionType.Attack &&
               RangedMonsterAttackSkillIds.Contains(command.SkillData.SkillId);
    }

    private static IEnumerator PlayDamageCameraFeedback()
    {
        if (BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.PlayDamageImpact();
    }

    private static IEnumerator PlayRangedDamageCameraFeedback()
    {
        if (BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.PlayRangedDamageImpact();
    }

    // Player damage hits use the attack direction, not each target's world position.
    // This keeps every target in an area attack facing/pushing consistently.
    private IEnumerator PlayPlayerDirectedDamageHitFeedback(
        Transform attacker,
        IReadOnlyList<Transform> targets,
        BattleDirection attackDirection)
    {
        int horizontalDirection = BattleDirectionToHorizontal(attackDirection);
        if (BattleCameraController.Instance != null)
            BattleCameraController.Instance.SetNextImpactDirection(horizontalDirection);

        yield return BattleHitImpactFeedback.PlayDamageHitFeedback(
            attacker, targets, horizontalDirection, true, true);
    }

    private IEnumerator PlayDamageHitFeedback(
        Transform attacker,
        IReadOnlyList<Transform> targets,
        BattleDirection fallbackDirection)
    {
        yield return PlayDamageHitFeedback(
            attacker,
            targets,
            BattleDirectionToHorizontal(fallbackDirection));
    }

    private IEnumerator PlayDamageHitFeedback(
        Transform attacker,
        IReadOnlyList<Transform> targets,
        int fallbackHorizontalDirection,
        bool includeTargetPush)
    {
        if (BattleCameraController.Instance != null)
            BattleCameraController.Instance.SetNextImpactDirection(
                ResolveCameraImpactDirection(attacker, targets, fallbackHorizontalDirection));

        yield return BattleHitImpactFeedback.PlayDamageHitFeedback(
            attacker,
            targets,
            fallbackHorizontalDirection,
            includeTargetPush);
    }

    private IEnumerator PlayDamageHitFeedback(
        Transform attacker,
        IReadOnlyList<Transform> targets,
        int fallbackHorizontalDirection)
    {
        if (BattleCameraController.Instance != null)
            BattleCameraController.Instance.SetNextImpactDirection(
                ResolveCameraImpactDirection(attacker, targets, fallbackHorizontalDirection));

        yield return BattleHitImpactFeedback.PlayDamageHitFeedback(
            attacker,
            targets,
            fallbackHorizontalDirection);
    }

    private static int BattleDirectionToHorizontal(BattleDirection direction)
    {
        return direction == BattleDirection.Left ? -1 : 1;
    }

    private static int ResolveCameraImpactDirection<TTarget>(
        Transform attacker,
        IReadOnlyList<TTarget> targets,
        int fallbackHorizontalDirection)
        where TTarget : Component
    {
        Transform firstTarget = null;
        if (targets != null)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null)
                    continue;

                firstTarget = targets[i].transform;
                break;
            }
        }

        return BattleHitImpactFeedback.ResolveHorizontalDirection(
            attacker,
            firstTarget,
            fallbackHorizontalDirection);
    }

    private static void AddFeedbackTarget(List<Transform> targets, Transform target)
    {
        if (targets == null || target == null)
            return;

        if (!targets.Contains(target))
            targets.Add(target);
    }

    private int GetMonsterImpactFallbackDirection(MonsterUnit monster)
    {
        if (monster == null)
            return 1;

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

        if (facing != null)
            return facing.IsFacingRight ? 1 : -1;

        if (monster.RuntimeData != null)
            return BattleDirectionToHorizontal(monster.RuntimeData.Direction);

        return 1;
    }

    private List<Transform> BuildMonsterSkillFeedbackTargets(
        MonsterUnit caster,
        MonsterReservedCommand command)
    {
        List<Transform> targets = new();

        if (command == null || command.SkillData == null)
            return targets;

        switch (command.SkillData.Target)
        {
            case TargetType.PlayerParty:
                AddPlayerFeedbackTargets(command, targets);
                break;

            case TargetType.EnemyParty:
                AddMonsterFeedbackTargets(caster, command, targets);
                break;

            case TargetType.Self:
                AddFeedbackTarget(targets, caster != null ? caster.transform : null);
                break;
        }

        return targets;
    }

    private void AddPlayerFeedbackTargets(MonsterReservedCommand command, List<Transform> targets)
    {
        if (command == null || command.TargetGridIndices == null)
            return;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (character.RuntimeData.CurrentHP <= 0)
                continue;

            if (command.TargetGridIndices.Contains(character.CurrentGridIndex))
                AddFeedbackTarget(targets, character.transform);
        }
    }

    private void AddMonsterFeedbackTargets(
        MonsterUnit caster,
        MonsterReservedCommand command,
        List<Transform> targets)
    {
        if (command == null || command.TargetGridIndices == null)
            return;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster == caster || monster.RuntimeData == null)
                continue;

            if (monster.RuntimeData.IsDead)
                continue;

            if (IsMonsterInTargetGridIndices(monster, command.TargetGridIndices))
                AddFeedbackTarget(targets, monster.transform);
        }
    }

    private static bool IsMonsterInTargetGridIndices(
        MonsterUnit monster,
        IReadOnlyCollection<int> targetGridIndices)
    {
        if (monster == null || targetGridIndices == null)
            return false;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            if (ContainsGridIndex(targetGridIndices, monster.OccupiedGridIndices[i]))
                return true;
        }

        return false;
    }

    private static bool ContainsGridIndex(IReadOnlyCollection<int> gridIndices, int gridIndex)
    {
        if (gridIndices == null)
            return false;

        foreach (int candidate in gridIndices)
        {
            if (candidate == gridIndex)
                return true;
        }

        return false;
    }

    private BattleCharacter FindFirstAlivePlayerTarget(MonsterReservedCommand command)
    {
        if (command == null || command.TargetGridIndices == null)
            return null;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (character.RuntimeData.CurrentHP <= 0)
                continue;

            if (command.TargetGridIndices.Contains(character.CurrentGridIndex))
                return character;
        }

        return null;
    }

    private MonsterUnit FindFirstAliveMonsterTarget(
        MonsterReservedCommand command,
        MonsterUnit caster)
    {
        if (command == null || command.TargetGridIndices == null)
            return null;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster == caster || monster.RuntimeData == null)
                continue;

            if (monster.RuntimeData.IsDead)
                continue;

            for (int j = 0; j < monster.OccupiedGridIndices.Count; j++)
            {
                if (command.TargetGridIndices.Contains(monster.OccupiedGridIndices[j]))
                    return monster;
            }
        }

        return null;
    }

    private IEnumerator MoveTransformSmooth(
        Transform target,
        Vector3 start,
        Vector3 end,
        float duration)
    {
        if (target == null)
            yield break;

        if (duration <= 0f)
        {
            target.position = end;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            target.position = Vector3.Lerp(start, end, t);

            yield return null;
        }

        target.position = end;
    }

    private bool ShouldPlayMonsterProjectileVfx(
        MonsterUnit monster,
        MonsterReservedCommand command,
        BattleUnitAnimator animator)
    {
        return monster != null &&
               command != null &&
               animator != null &&
               animator.HasMonsterProjectileVfx(command) &&
               TryGetMonsterProjectileTargetPosition(command, out _);
    }

    private IEnumerator PlayMonsterProjectileVfxIfNeeded(
        MonsterUnit monster,
        MonsterReservedCommand command,
        BattleUnitAnimator animator)
    {
        if (!ShouldPlayMonsterProjectileVfx(monster, command, animator))
            yield break;

        if (!TryGetMonsterProjectileTargetPosition(command, out Vector3 targetPosition))
            yield break;

        yield return animator.PlayMonsterProjectileVfx(command, targetPosition);
    }

    private bool TryGetMonsterProjectileTargetPosition(
        MonsterReservedCommand command,
        out Vector3 targetPosition)
    {
        targetPosition = Vector3.zero;

        if (gridManager == null || command == null)
            return false;

        if (TryGetGroundTargetedProjectilePosition(command, out targetPosition))
            return true;

        BattleCharacter target = FindFirstAlivePlayerTarget(command);

        if (target == null)
            target = FindFirstPlayerTarget(command);

        if (target != null && target.CurrentGridIndex >= 0)
        {
            targetPosition = gridManager.GetWorldPositionByIndex(target.CurrentGridIndex);
            return true;
        }

        if (command.TargetGridIndices == null || command.TargetGridIndices.Count <= 0)
            return false;

        targetPosition = gridManager.GetWorldPositionByIndex(command.TargetGridIndices[0]);
        return true;
    }

    private bool TryGetGroundTargetedProjectilePosition(
        MonsterReservedCommand command,
        out Vector3 targetPosition)
    {
        targetPosition = Vector3.zero;

        if (gridManager == null || command == null ||
            !string.Equals(command.MonsterId, MuckMonsterId, System.StringComparison.Ordinal) ||
            !string.Equals(command.SkillId, MuckProjectileSkillId, System.StringComparison.Ordinal))
            return false;

        // The AI explicitly reserves the character's target cell. RangeOrigin
        // is an attack-range origin, never the projectile destination.
        int destinationIndex = command.HasExplicitRangeResult &&
            command.TargetGridIndices != null && command.TargetGridIndices.Count > 0
                ? command.TargetGridIndices[0] : -1;
        if (destinationIndex < 0)
            return false;

        Vector2Int destinationCoord = gridManager.IndexToCoord(destinationIndex);
        if (!gridManager.IsValidCoord(destinationCoord))
            return false;

        targetPosition = gridManager.GetWorldPositionByIndex(destinationIndex);
        return true;
    }

    private static bool ShouldUseRangeOriginForProjectileTarget(MonsterReservedCommand command)
    {
        return command != null &&
               command.RangeOriginGridIndex >= 0 &&
               string.Equals(
                   command.SkillId,
                   MuckProjectileSkillId,
                   System.StringComparison.Ordinal);
    }

    // 예약형 턴제: 확정된 이동 명령은 실행 중 재탐색하거나 변경하지 않습니다.
    // 실제로 도착 칸이 선점되었는지 여부는 ExecuteMonsterMove의
    // ResolveMonsterMove에서 판정하여 이동/충돌을 처리합니다.
    private static Vector2Int GetMonsterMoveOffset(MonsterReservedCommand command)
    {
        return command != null ? command.MoveOffset : Vector2Int.zero;
    }

    private bool IsMonsterInRange(MonsterUnit monster, PlayerReservedCommand command)
    {
        if (monster == null || command == null)
            return false;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            if (command.RangeGridIndices.Contains(monster.OccupiedGridIndices[i]))
                return true;
        }

        return false;
    }

    private void ConsumePlayerSkillCost(PlayerReservedCommand command, BattleCharacter caster)
    {
        if (command == null || caster == null || caster.RuntimeData == null)
            return;

        caster.RuntimeData.ApplyReservedCosts();
    }

    private void UpdatePartyGridIndex(string characterId, int gridIndex)
    {
        if (DataManager.Instance == null)
            return;

        var partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (partyStore.GetCharacterId(i) != characterId)
                continue;

            partyStore.SetCurrentGridIndex(i, gridIndex);
            return;
        }
    }

    private IEnumerator ExecuteMonsterDashAttack(MonsterReservedCommand command)
    {
        MonsterUnit monster = unitFinder.FindMonsterUnit(command.RuntimeId);

        if (monster == null || command == null || command.SkillData == null)
            yield break;

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

        if (facing != null && monster.RuntimeData != null)
            facing.FaceRight(monster.RuntimeData.Direction == BattleDirection.Right);

        bool facingRight = monster.RuntimeData != null
            ? monster.RuntimeData.Direction == BattleDirection.Right
            : facing == null || facing.IsFacingRight;

        int dirX = facingRight ? 1 : -1;
        int maxMove = command.SkillData.SkillId == "S_Monster_22"
            ? 6
            : gridManager.Width;

        Vector2Int finalOffset = Vector2Int.zero;
        BattleCharacter hitPlayer = null;
        MonsterUnit hitBlockingMonster = null;
        int hitCharacterGridEffectIndex = -1;
        bool wasBlockedByCollision = false;

        for (int step = 1; step <= maxMove; step++)
        {
            Vector2Int testOffset = new Vector2Int(dirX * step, 0);

            if (IsMonsterDashOutsideGrid(monster, testOffset))
            {
                // 그리드 경계는 충돌 대상이 아니므로 맵 밖으로 나가지 않고 멈추기만 합니다.
                break;
            }

            if (!CanMonsterDashToOffset(
                    monster,
                    testOffset,
                    out BattleCharacter blockingPlayer,
                    out MonsterUnit blockingMonster,
                    out int blockingCharacterGridEffectIndex))
            {
                // 장애물이나 다른 유닛에 막힌 경우에만 충돌로 처리합니다.
                wasBlockedByCollision = true;
                break;
            }

            if (blockingPlayer != null)
            {
                hitPlayer = blockingPlayer;
                break;
            }

            if (blockingMonster != null)
            {
                hitBlockingMonster = blockingMonster;
                break;
            }

            if (blockingCharacterGridEffectIndex >= 0)
            {
                hitCharacterGridEffectIndex = blockingCharacterGridEffectIndex;
                break;
            }

            finalOffset = testOffset;
        }

        BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();

        if (ShouldEnterCameraForActiveAction() &&
            hitPlayer != null &&
            BattleCameraController.Instance != null)
            yield return BattleCameraController.Instance.ZoomToAttacker(monster.transform);

        // 돌진 공격도 카메라 포커스가 끝난 뒤 모션과 기울기를 동시에 시작합니다.
        BeginAttackPlaneFeedbackIfNeeded(
            ShouldControlAttackPlaneForMonsterSkill(command),
            ShouldSkipMonsterSkillCamera(command) ? PlaneAttackType.Ranged : PlaneAttackType.Melee,
            hitPlayer != null || hitBlockingMonster != null || hitCharacterGridEffectIndex >= 0);

        if (animator != null)
            animator.PlayMonsterSkillAction(command);

        if (finalOffset != Vector2Int.zero)
        {
            List<int> enteredGridIndices = BuildMonsterEnteredGridIndices(monster, finalOffset);
            Vector2Int currentCoord = gridManager.IndexToCoord(monster.MainGridIndex);
            Vector2Int movedCoord = currentCoord + finalOffset;
            int movedIndex = gridManager.CoordToIndex(movedCoord);

            Vector3 pos = gridManager.GetWorldPositionByIndex(movedIndex);

            if (hitPlayer != null && BattleCameraController.Instance != null)
                BattleCameraController.Instance.BeginZoomFollowTarget(monster.transform);

            yield return MoveTransformSmooth(
                monster.transform,
                monster.transform.position,
                pos,
                GetMoveAnimationDuration(enteredGridIndices.Count)
            );

            monster.MoveOccupiedCells(finalOffset, gridManager);
            ApplyGridEffectsToMonster(enteredGridIndices, monster);
        }

        if (hitPlayer != null)
        {
            ApplyMonsterDashDamage(command, monster, hitPlayer);

            IEnumerator feedbackRoutine = activeActionInfo.ShouldPlayExternalImpact
                ? PlayDamageHitFeedback(
                    monster != null ? monster.transform : null,
                    new List<Transform> { hitPlayer.transform },
                    GetMonsterImpactFallbackDirection(monster),
                    false)
                : null;
            ApplyMonsterDashKnockback(command, monster, hitPlayer);

            if (feedbackRoutine != null)
                yield return feedbackRoutine;

            float dashHitDelay = activeActionInfo.ShouldPlayExternalImpact
                ? HitCameraDelay
                : ActionDelay;
            yield return new WaitForSeconds(
                activeActionInfo.ShouldPlayExternalImpact
                    ? dashHitDelay / GetActivePresentationSpeed()
                    : GetActiveActionBeatDelay(dashHitDelay));

            yield return ReturnCameraAfterActiveActionIfNeeded();
        }
        else if (hitBlockingMonster != null)
        {
            if (monster.RuntimeData != null && !monster.RuntimeData.IsDead)
                ApplyCrashToMonster(monster);

            if (hitBlockingMonster.RuntimeData != null && !hitBlockingMonster.RuntimeData.IsDead)
                ApplyCrashToMonster(hitBlockingMonster);

            yield return new WaitForSeconds(GetActiveActionBeatDelay());
        }
        else if (hitCharacterGridEffectIndex >= 0)
        {
            ApplyMonsterDashDamageToGridEffect(command, hitCharacterGridEffectIndex);
            yield return new WaitForSeconds(HitCameraDelay / GetActivePresentationSpeed());
        }
        else
        {
            if (wasBlockedByCollision && monster.RuntimeData != null && !monster.RuntimeData.IsDead)
                ApplyCrashToMonster(monster);

            yield return new WaitForSeconds(GetActiveActionBeatDelay());
        }

        hudService.RefreshHUDs();
    }


    private bool IsMonsterDashOutsideGrid(MonsterUnit monster, Vector2Int moveOffset)
    {
        if (monster == null || gridManager == null)
            return true;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            Vector2Int currentCoord = gridManager.IndexToCoord(monster.OccupiedGridIndices[i]);
            Vector2Int targetCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(targetCoord))
                return true;
        }

        return false;
    }

    private bool CanMonsterDashToOffset(
    MonsterUnit monster,
    Vector2Int moveOffset,
    out BattleCharacter blockingPlayer,
    out MonsterUnit blockingMonster,
    out int blockingCharacterGridEffectIndex)
    {
        blockingPlayer = null;
        blockingMonster = null;
        blockingCharacterGridEffectIndex = -1;

        if (monster == null || gridManager == null)
            return false;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            int occupiedIndex = monster.OccupiedGridIndices[i];

            Vector2Int currentCoord = gridManager.IndexToCoord(occupiedIndex);
            Vector2Int targetCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(targetCoord))
                return false;

            int targetIndex = gridManager.CoordToIndex(targetCoord);

            BattleCharacter player = FindPlayerAtGrid(targetIndex);

            if (player != null)
            {
                blockingPlayer = player;
                return true;
            }

            BattleGridEffectController gridEffectController = ResolveGridEffectController();

            if (gridEffectController != null &&
                gridEffectController.IsCharacterTargetEffect(targetIndex))
            {
                blockingCharacterGridEffectIndex = targetIndex;
                return true;
            }

            if (BattleOccupancyService.TryGetMonsterAtGrid(
                    targetIndex,
                    out MonsterUnit occupiedMonster,
                    monster))
            {
                blockingMonster = occupiedMonster;
                return true;
            }

            if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, monster))
                return false;

            if (IsGridEffectBlocked(targetIndex))
                return false;
        }

        return true;
    }

    private BattleCharacter FindPlayerAtGrid(int gridIndex)
    {
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (character.RuntimeData.IsDead)
                continue;

            if (character.CurrentGridIndex == gridIndex)
                return character;
        }

        return null;
    }


    private void ApplyMonsterDashKnockback(
        MonsterReservedCommand command,
        MonsterUnit monster,
        BattleCharacter target)
    {
        if (command == null || command.SkillData == null || monster == null || target == null)
            return;

        if (target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        if (!TryGetMonsterSkillEffectValue(command.SkillData, "E_Knockback", out int knockbackValue))
            return;

        Vector2Int monsterCoord = gridManager.IndexToCoord(monster.MainGridIndex);
        Vector2Int targetCoord = gridManager.IndexToCoord(target.CurrentGridIndex);
        BattleDirection knockbackDirection = targetCoord.x < monsterCoord.x
            ? BattleDirection.Left
            : BattleDirection.Right;

        new KnockbackEffect().Execute(new BattleEffectContext
        {
            MonsterCaster = monster,
            PlayerTarget = target,
            Direction = knockbackDirection,
            GridManager = gridManager,
            EffectId = "E_Knockback",
            Value = Mathf.Max(1, knockbackValue),
            Count = 1,
            ForcedMoveVisualStarted = () =>
            {
                BattleCameraController cameraController = BattleCameraController.Instance;
                if (cameraController != null && monster != null && target != null)
                {
                    cameraController.TransferDirectGridMovementFocus(
                        monster.transform,
                        target.transform);
                }
            },
            ForcedMoveVisualCompleted = () =>
            {
                BattleCameraController cameraController = BattleCameraController.Instance;
                if (cameraController != null && target != null)
                    cameraController.EndDirectGridMovement(target.transform);
            }
        });
    }

    private static bool TryGetMonsterSkillEffectValue(
        Relic.Gameplay.Data.MonsterSkillData skillData,
        string targetEffectId,
        out int value)
    {
        value = 0;

        if (skillData == null || string.IsNullOrWhiteSpace(skillData.EffectIds))
            return false;

        string[] effectIds = skillData.EffectIds.Split(';');
        string[] valueRates = string.IsNullOrWhiteSpace(skillData.ValueRate)
            ? System.Array.Empty<string>()
            : skillData.ValueRate.Split(';');

        for (int i = 0; i < effectIds.Length; i++)
        {
            if (!string.Equals(
                    effectIds[i].Trim(),
                    targetEffectId,
                    System.StringComparison.Ordinal))
            {
                continue;
            }

            if (i < valueRates.Length && int.TryParse(valueRates[i].Trim(), out int parsed))
                value = parsed;
            else
                value = 1;

            return true;
        }

        return false;
    }

    private void ApplyMonsterDashDamageToGridEffect(
        MonsterReservedCommand command,
        int gridIndex)
    {
        if (command == null || gridIndex < 0)
            return;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null || !controller.IsCharacterTargetEffect(gridIndex))
            return;

        int damage = Mathf.Max(0, damageService.GetMonsterDamage(command));

        if (damage > 0)
            controller.TryDamageEffect(gridIndex, damage, out _);
    }

    private void ApplyMonsterDashDamage(
    MonsterReservedCommand command,
    MonsterUnit monster,
    BattleCharacter target)
    {
        if (command == null || monster == null || target == null || target.RuntimeData == null)
            return;

        if (target.RuntimeData.IsDead)
            return;

        int damage = BattleDamageService.CalculateFinalMonsterDamageToPlayer(
            command,
            monster,
            target,
            damageService.GetMonsterDamage(command));

        BattleEffectUtility.DamagePlayer(target, damage, true);

        BattleUnitFacing targetFacing = target.GetComponent<BattleUnitFacing>();

        if (targetFacing != null)
        {
            targetFacing.FaceByWorldTarget(monster.transform.position);

            if (target.RuntimeData != null)
                target.RuntimeData.Direction = targetFacing.GetBattleDirection();
        }

        BattleUnitAnimator hitAnimator = target.GetComponent<BattleUnitAnimator>();

        if (hitAnimator != null)
        {
            if (target.RuntimeData.CurrentHP <= 0)
                hitAnimator.PlayDead();
            else
                hitAnimator.PlayHit();
        }

        Debug.Log(
            $"[MonsterDashAttack] {monster?.RuntimeData?.Name} / " +
            $"Skill:{command?.SkillData?.SkillId} / " +
            $"Target:{target?.CharacterId} / Damage:{damage}"
        );
    }
}

public static class BattleActionPresentationPolicy
{
    public static bool ShouldPlayGenericMoveForSkillEffect(SkillMasterData skillData)
    {
        if (skillData == null)
            return false;

        return skillData.Category == Category.Move ||
               skillData.TimelineNotation == TimelineActionType.Move;
    }
}
