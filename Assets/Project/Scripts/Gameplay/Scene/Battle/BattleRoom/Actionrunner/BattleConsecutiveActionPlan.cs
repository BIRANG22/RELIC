using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.VFX;

public readonly struct BattleConsecutiveActionInfo
{
    public static readonly BattleConsecutiveActionInfo Single = new(
        groupId: -1,
        groupIndex: 0,
        groupSize: 1,
        speedMultiplier: 1f);

    public int GroupId { get; }
    public int GroupIndex { get; }
    public int GroupSize { get; }
    public float SpeedMultiplier { get; }

    public bool IsGrouped => GroupSize > 1;
    public bool IsGroupStart => IsGrouped && GroupIndex == 0;
    public bool IsGroupEnd => IsGrouped && GroupIndex == GroupSize - 1;

    public bool ShouldEnterCamera => !IsGrouped || IsGroupStart;
    public bool ShouldPlayExternalImpact => !IsGrouped || IsGroupEnd;
    public bool ShouldReturnCamera => !IsGrouped || IsGroupEnd;

    public BattleConsecutiveActionInfo(
        int groupId,
        int groupIndex,
        int groupSize,
        float speedMultiplier)
    {
        GroupId = groupId;
        GroupIndex = Math.Max(0, groupIndex);
        GroupSize = Math.Max(1, groupSize);
        SpeedMultiplier = GroupSize > 1
            ? Math.Max(1f, speedMultiplier)
            : 1f;
    }
}

public sealed class BattleConsecutiveActionPlan
{
    private readonly Dictionary<PlayerReservedCommand, BattleConsecutiveActionInfo> playerInfos = new();
    private readonly Dictionary<MonsterReservedCommand, BattleConsecutiveActionInfo> monsterInfos = new();

    private sealed class PlannedAction
    {
        public PlayerReservedCommand PlayerCommand;
        public MonsterReservedCommand MonsterCommand;
        public ActionSignature Signature;
        public bool IsEligible;
    }

    private readonly struct ActionSignature : IEquatable<ActionSignature>
    {
        private readonly string actorKey;
        private readonly string skillId;
        private readonly BattleDirection direction;
        private readonly string targetKey;

        public ActionSignature(
            string actorKey,
            string skillId,
            BattleDirection direction,
            IReadOnlyList<int> targets)
        {
            this.actorKey = actorKey ?? string.Empty;
            this.skillId = skillId ?? string.Empty;
            this.direction = direction;
            targetKey = BuildTargetKey(targets);
        }

        public bool Equals(ActionSignature other)
        {
            return string.Equals(actorKey, other.actorKey, StringComparison.Ordinal) &&
                   string.Equals(skillId, other.skillId, StringComparison.Ordinal) &&
                   direction == other.direction &&
                   string.Equals(targetKey, other.targetKey, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActionSignature other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(actorKey, skillId, direction, targetKey);
        }

        private static string BuildTargetKey(IReadOnlyList<int> targets)
        {
            if (targets == null || targets.Count <= 0)
                return string.Empty;

            List<int> sorted = new(targets.Count);

            for (int i = 0; i < targets.Count; i++)
                sorted.Add(targets[i]);

            sorted.Sort();
            return string.Join(",", sorted);
        }
    }

    public static BattleConsecutiveActionPlan Build(
        IReadOnlyList<BattleActionBatch> batches,
        float groupedSpeedMultiplier)
    {
        BattleConsecutiveActionPlan plan = new();
        List<PlannedAction> actions = FlattenExecutionOrder(batches);
        float safeSpeed = Math.Max(1f, groupedSpeedMultiplier);
        int nextGroupId = 0;
        int index = 0;

        while (index < actions.Count)
        {
            PlannedAction first = actions[index];

            if (!first.IsEligible)
            {
                plan.SetInfo(first, BattleConsecutiveActionInfo.Single);
                index++;
                continue;
            }

            int endExclusive = index + 1;

            while (endExclusive < actions.Count &&
                   actions[endExclusive].IsEligible &&
                   actions[endExclusive].Signature.Equals(first.Signature))
            {
                endExclusive++;
            }

            int groupSize = endExclusive - index;

            if (groupSize <= 1)
            {
                plan.SetInfo(first, BattleConsecutiveActionInfo.Single);
                index = endExclusive;
                continue;
            }

            int groupId = nextGroupId++;

            for (int groupIndex = 0; groupIndex < groupSize; groupIndex++)
            {
                plan.SetInfo(
                    actions[index + groupIndex],
                    new BattleConsecutiveActionInfo(
                        groupId,
                        groupIndex,
                        groupSize,
                        safeSpeed));
            }

            index = endExclusive;
        }

        return plan;
    }

    public BattleConsecutiveActionInfo GetInfo(PlayerReservedCommand command)
    {
        if (command != null && playerInfos.TryGetValue(command, out BattleConsecutiveActionInfo info))
            return info;

        return BattleConsecutiveActionInfo.Single;
    }

    public BattleConsecutiveActionInfo GetInfo(MonsterReservedCommand command)
    {
        if (command != null && monsterInfos.TryGetValue(command, out BattleConsecutiveActionInfo info))
            return info;

        return BattleConsecutiveActionInfo.Single;
    }

    public BattleConsecutiveActionInfo GetFirstInfo(BattleActionBatch batch)
    {
        List<PlannedAction> actions = FlattenExecutionOrder(
            batch != null ? new[] { batch } : Array.Empty<BattleActionBatch>());

        return actions.Count > 0
            ? GetInfo(actions[0])
            : BattleConsecutiveActionInfo.Single;
    }

    public BattleConsecutiveActionInfo GetLastInfo(BattleActionBatch batch)
    {
        List<PlannedAction> actions = FlattenExecutionOrder(
            batch != null ? new[] { batch } : Array.Empty<BattleActionBatch>());

        return actions.Count > 0
            ? GetInfo(actions[actions.Count - 1])
            : BattleConsecutiveActionInfo.Single;
    }

    public bool ContinuesAcrossBoundary(
        BattleActionBatch currentBatch,
        BattleActionBatch nextBatch)
    {
        BattleConsecutiveActionInfo current = GetLastInfo(currentBatch);
        BattleConsecutiveActionInfo next = GetFirstInfo(nextBatch);

        return current.IsGrouped &&
               next.IsGrouped &&
               !current.IsGroupEnd &&
               !next.IsGroupStart &&
               current.GroupId == next.GroupId;
    }

    private void SetInfo(PlannedAction action, BattleConsecutiveActionInfo info)
    {
        if (action == null)
            return;

        if (action.PlayerCommand != null)
            playerInfos[action.PlayerCommand] = info;

        if (action.MonsterCommand != null)
            monsterInfos[action.MonsterCommand] = info;
    }

    private BattleConsecutiveActionInfo GetInfo(PlannedAction action)
    {
        if (action == null)
            return BattleConsecutiveActionInfo.Single;

        if (action.PlayerCommand != null)
            return GetInfo(action.PlayerCommand);

        if (action.MonsterCommand != null)
            return GetInfo(action.MonsterCommand);

        return BattleConsecutiveActionInfo.Single;
    }

    private static List<PlannedAction> FlattenExecutionOrder(
        IReadOnlyList<BattleActionBatch> batches)
    {
        List<PlannedAction> actions = new();

        if (batches == null)
            return actions;

        for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
        {
            BattleActionBatch batch = batches[batchIndex];

            if (batch == null)
                continue;

            AddPlayerActions(actions, batch.PlayerCommands, requireSwift: true);
            AddMonsterActions(actions, batch.MonsterCommands);
            AddPlayerActions(actions, batch.PlayerCommands, requireSwift: false);
        }

        return actions;
    }

    private static void AddPlayerActions(
        List<PlannedAction> actions,
        IReadOnlyList<PlayerReservedCommand> commands,
        bool requireSwift)
    {
        if (actions == null || commands == null)
            return;

        for (int i = 0; i < commands.Count; i++)
        {
            PlayerReservedCommand command = commands[i];

            if (command == null || BattleActionOrderUtility.HasSwift(command) != requireSwift)
                continue;

            bool isMove = IsPlayerMove(command);
            actions.Add(new PlannedAction
            {
                PlayerCommand = command,
                IsEligible = !isMove,
                Signature = new ActionSignature(
                    "P:" + command.CharacterId,
                    command.SkillId,
                    command.Direction,
                    command.TargetGridIndices)
            });
        }
    }

    private static void AddMonsterActions(
        List<PlannedAction> actions,
        IReadOnlyList<MonsterReservedCommand> commands)
    {
        if (actions == null || commands == null)
            return;

        for (int i = 0; i < commands.Count; i++)
        {
            MonsterReservedCommand command = commands[i];

            if (command == null)
                continue;

            bool isMove = IsMonsterMove(command);
            BattleDirection direction = command.HasForcedDirection
                ? command.ForcedDirection
                : (command.UserRuntime != null
                    ? command.UserRuntime.Direction
                    : BattleDirection.Right);

            actions.Add(new PlannedAction
            {
                MonsterCommand = command,
                IsEligible = !isMove,
                Signature = new ActionSignature(
                    "M:" + command.RuntimeId,
                    command.SkillId,
                    direction,
                    command.TargetGridIndices)
            });
        }
    }

    private static bool IsPlayerMove(PlayerReservedCommand command)
    {
        return command == null ||
               command.ReservedMoveGridIndex >= 0 ||
               command.SkillData == null ||
               command.SkillData.Category == Category.Move ||
               command.SkillData.TimelineNotation == TimelineActionType.Move;
    }

    private static bool IsMonsterMove(MonsterReservedCommand command)
    {
        return command == null ||
               command.SkillData == null ||
               command.IsPortalMove ||
               command.SkillData.TimelineNotation == TimelineActionType.Move;
    }
}

public static class BattleConsecutiveActionPresentationContext
{
    private static readonly Dictionary<Animator, float> ManagedAnimators = new();
    private static int activeGroupId = -1;
    private static BattleHitImpactFeedback suppressedStatusFeedback;
    private static bool suppressedStatusFeedbackWasEnabled;

    public static BattleConsecutiveActionInfo CurrentInfo { get; private set; } =
        BattleConsecutiveActionInfo.Single;

    public static float SpeedMultiplier => CurrentInfo.IsGrouped
        ? CurrentInfo.SpeedMultiplier
        : 1f;

    public static bool ShouldPlayExternalImpact =>
        CurrentInfo.ShouldPlayExternalImpact;

    public static bool ShouldPlayStatusPulse =>
        CurrentInfo.ShouldPlayExternalImpact;

    public static void BeginAction(BattleConsecutiveActionInfo info)
    {
        if (!info.IsGrouped)
        {
            EndGroup();
            CurrentInfo = BattleConsecutiveActionInfo.Single;
            return;
        }

        if (activeGroupId >= 0 && activeGroupId != info.GroupId)
            EndGroup();

        activeGroupId = info.GroupId;
        CurrentInfo = info;
        SetStatusPulseSuppressed(!info.ShouldPlayExternalImpact);
    }

    public static void CompleteAction(
        BattleConsecutiveActionInfo info,
        bool completedNormally)
    {
        if (!info.IsGrouped || !completedNormally || info.IsGroupEnd)
            EndGroup();
    }

    public static float ScaleDuration(float duration)
    {
        return Mathf.Max(0f, duration) / Mathf.Max(1f, SpeedMultiplier);
    }

    public static float ScaleActionBeatDuration(float duration, float minimumGroupedDuration)
    {
        float scaledDuration = ScaleDuration(duration);

        return CurrentInfo.IsGrouped
            ? Mathf.Max(scaledDuration, Mathf.Max(0f, minimumGroupedDuration))
            : scaledDuration;
    }

    public static float ScaleDeltaTime(float deltaTime)
    {
        return Mathf.Max(0f, deltaTime) * Mathf.Max(1f, SpeedMultiplier);
    }

    public static void ApplyAnimatorSpeed(Animator animator, float localMultiplier = 1f)
    {
        if (animator == null)
            return;

        if (!CurrentInfo.IsGrouped)
        {
            animator.speed = Mathf.Max(0.01f, localMultiplier);
            return;
        }

        if (!ManagedAnimators.ContainsKey(animator))
            ManagedAnimators[animator] = animator.speed;

        animator.speed = Mathf.Max(
            0.01f,
            ManagedAnimators[animator] * SpeedMultiplier * Mathf.Max(0.01f, localMultiplier));
    }

    public static void ApplyVfxSpeed(GameObject root)
    {
        if (root == null || !CurrentInfo.IsGrouped)
            return;

        float speed = SpeedMultiplier;
        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];

            if (particle == null)
                continue;

            ParticleSystem.MainModule main = particle.main;
            main.simulationSpeed *= speed;
        }

        Animator[] animators = root.GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
                animators[i].speed *= speed;
        }

        VisualEffect[] visualEffects = root.GetComponentsInChildren<VisualEffect>(true);

        for (int i = 0; i < visualEffects.Length; i++)
        {
            if (visualEffects[i] != null)
                visualEffects[i].playRate *= speed;
        }
    }

    public static void EndGroup()
    {
        RestoreStatusPulseFeedback();

        foreach (KeyValuePair<Animator, float> pair in ManagedAnimators)
        {
            if (pair.Key != null)
                pair.Key.speed = pair.Value;
        }

        ManagedAnimators.Clear();
        activeGroupId = -1;
        CurrentInfo = BattleConsecutiveActionInfo.Single;
    }

    private static void SetStatusPulseSuppressed(bool suppressed)
    {
        if (!suppressed)
        {
            RestoreStatusPulseFeedback();
            return;
        }

        if (suppressedStatusFeedback != null)
            return;

        // 기존 상태 펄스 진입점을 수정하지 않고도 중간 그룹 행동만 억제하기 위해
        // 자동 인스턴스를 확보한 뒤 Behaviour 활성 상태를 그룹 경계 동안 보존합니다.
        BattleHitImpactFeedback.PlayStatusHitFeedback(null);
        BattleHitImpactFeedback feedback =
            UnityEngine.Object.FindFirstObjectByType<BattleHitImpactFeedback>(
                FindObjectsInactive.Include);

        if (feedback == null)
            return;

        suppressedStatusFeedback = feedback;
        suppressedStatusFeedbackWasEnabled = feedback.enabled;
        feedback.enabled = false;
    }

    private static void RestoreStatusPulseFeedback()
    {
        if (suppressedStatusFeedback != null)
            suppressedStatusFeedback.enabled = suppressedStatusFeedbackWasEnabled;

        suppressedStatusFeedback = null;
        suppressedStatusFeedbackWasEnabled = false;
    }
}
