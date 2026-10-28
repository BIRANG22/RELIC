using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed class TutorialPartyTransitionPresenter : MonoBehaviour
{
    [Header("Tutorial Party Entry Movement")]
    [SerializeField] private Vector3 entryOffset = new(-2.6666667f, 0f, 0f);
    [SerializeField] private Vector3 exitOffset = new(2.6666667f, 0f, 0f);
    [SerializeField, Min(0f)] private float entryDelay = 0.3f;
    [SerializeField, Min(0f)] private float entryDuration = 0.35f;
    [SerializeField, Min(0f)] private float exitDuration = 0.35f;
    [SerializeField] private AnimationCurve entryCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve exitCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Fade")]
    [SerializeField] private bool fadeCharacters = true;

    private readonly List<ActorState> actors = new();
    private Coroutine transitionRoutine;
    private TaskCompletionSource<bool> completionSource;

    public bool IsPlaying => transitionRoutine != null;
    public int PreparedActorCount => actors.Count;

    public void PrepareEntry()
    {
        PrepareEntry(transform);
    }

    public void PrepareEntry(params Transform[] actorRoots)
    {
        CancelAndRestore(true);
        CaptureActors(actorRoots, false);
        Debug.Log($"[TutorialEntryTrace] PREPARE presenter={GetHierarchyPath(transform)} id={GetInstanceID()} actors={actors.Count} delay={entryDelay:F2} duration={entryDuration:F2} active={isActiveAndEnabled}", this);

        for (int i = 0; i < actors.Count; i++)
        {
            ActorState actor = actors[i];
            if (actor.Transform == null)
                continue;

            actor.From = BattleRoomPartyTransitionMath.GetEntryStart(actor.FinalPosition, entryOffset);
            actor.To = actor.FinalPosition;
            actor.Transform.position = actor.From;
            Debug.Log($"[TutorialEntryTrace] ACTOR {actor.Transform.name}: from={actor.From} to={actor.To}", actor.Transform);
            ApplyAlpha(actor, fadeCharacters ? 0f : 1f);
        }
    }

    // 타이틀 -> Battle 씬: 공용 SceneTransition 오브젝트가 비활성화된 뒤 입장합니다.
    // Battle 씬 내부의 BattlePanelTransition은 여기서 확인하지 않습니다.
    public async Task PlayPreparedEntryAfterTitleTransitionAsync()
    {
        CanvasMaterialSceneTransition transition = CanvasMaterialSceneTransition.Instance;
        SceneFlowManager flow = SceneFlowManager.Instance;

        // SceneFlowManager는 타이틀 씬 전환이 완료된 뒤 IsLoading을 해제합니다.
        // 전환 루트의 비활성화까지 확인해야 캐릭터 이동이 화면에 보입니다.
        while (isActiveAndEnabled &&
               ((flow != null && flow.IsLoading) ||
                (transition != null && transition.IsTransitionRootActive)))
            await Task.Yield();

        if (!isActiveAndEnabled) return;
        await PlayPreparedEntryAsync();
    }

    // Movement between rooms inside Battle scene uses BattlePanelTransition, not SceneTransitionManager.
    public async Task PlayPreparedEntryAfterBattlePanelAsync(BattleDiagonalSceneTransition battleTransition)
    {
        if (battleTransition != null)
        {
            // Called after the panel starts uncovering. Wait for its image to finish moving.
            while (isActiveAndEnabled && battleTransition.IsPlaying)
                await Task.Yield();
        }

        if (!isActiveAndEnabled) return;
        await PlayPreparedEntryAsync();
    }

    public Task PlayPreparedEntryAsync()
    {
        Debug.Log($"[TutorialEntryTrace] PLAY presenter={GetHierarchyPath(transform)} id={GetInstanceID()} actors={actors.Count} delay={entryDelay:F2} duration={entryDuration:F2} active={isActiveAndEnabled}", this);
        return PlayAsync(entryDuration, entryCurve, true);
    }

    public Task PlayExitAsync()
    {
        return PlayExitAsync(transform);
    }

    public Task PlayExitAsync(params Transform[] actorRoots)
    {
        CancelAndRestore(false);
        CaptureActors(actorRoots, true);

        // Face the exit direction before starting the walk animation.
        for (int i = 0; i < actors.Count; i++)
        {
            ActorState actor = actors[i];
            if (actor.Transform == null)
                continue;

            BattleUnitFacing facing = actor.Transform.GetComponentInChildren<BattleUnitFacing>(true);
            if (facing != null)
                facing.FaceRight(true);

            BattleCharacter character = actor.Transform.GetComponent<BattleCharacter>();
            if (character != null && character.RuntimeData != null)
                character.RuntimeData.Direction = BattleDirection.Right;
        }

        for (int i = 0; i < actors.Count; i++)
        {
            ActorState actor = actors[i];
            if (actor.Transform == null)
                continue;

            actor.From = actor.Transform.position;
            actor.To = BattleRoomPartyTransitionMath.GetExitTarget(actor.From, exitOffset);
        }

        return PlayAsync(exitDuration, exitCurve, false);
    }

    public void CancelAndRestore(bool restoreEntryFinalPositions)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        for (int i = 0; i < actors.Count; i++)
        {
            ActorState actor = actors[i];
            if (restoreEntryFinalPositions && actor.Transform != null)
                actor.Transform.position = actor.FinalPosition;

            RestoreColors(actor);
            if (actor.Marker != null)
                actor.Marker.RestoreTransitionIdle();
            else if (actor.Animator != null)
                actor.Animator.PlayIdle();
        }

        actors.Clear();
        CompletePendingTask();
    }

    private Task PlayAsync(float duration, AnimationCurve curve, bool entering)
    {
        if (!isActiveAndEnabled || actors.Count == 0)
        {
            Debug.LogWarning($"[TutorialEntryTrace] SKIPPED active={isActiveAndEnabled} actors={actors.Count}", this);
            Finish(entering);
            return Task.CompletedTask;
        }

        completionSource = new TaskCompletionSource<bool>();
        Task completionTask = completionSource.Task;
        Coroutine startedRoutine = StartCoroutine(PlayRoutine(duration, curve, entering));
        transitionRoutine = completionTask.IsCompleted ? null : startedRoutine;
        return completionTask;
    }

    private IEnumerator PlayRoutine(float duration, AnimationCurve curve, bool entering)
    {
        if (entering && entryDelay > 0f)
        {
            float delayElapsed = 0f;
            while (delayElapsed < entryDelay)
            {
                delayElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        Debug.Log($"[TutorialEntryTrace] WALK START after delay={entryDelay:F2}s", this);
        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i].Marker != null && actors[i].Marker.PlayTransitionWalk())
                continue;

            if (actors[i].Animator != null)
                actors[i].Animator.PlayWalk();
        }

        float safeDuration = Mathf.Max(0f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            ApplyProgress(Mathf.Clamp01(elapsed / safeDuration), curve, entering);
            yield return null;
        }

        ApplyProgress(1f, curve, entering);
        Finish(entering);
    }

    private void ApplyProgress(float normalizedTime, AnimationCurve curve, bool entering)
    {
        for (int i = 0; i < actors.Count; i++)
        {
            ActorState actor = actors[i];
            if (actor.Transform == null)
                continue;

            actor.Transform.position = BattleRoomPartyTransitionMath.Evaluate(
                actor.From,
                actor.To,
                normalizedTime,
                curve);

            if (fadeCharacters)
                ApplyAlpha(actor, entering ? normalizedTime : 1f - normalizedTime);
        }
    }

    private void Finish(bool entering)
    {
        Debug.Log($"[TutorialEntryTrace] FINISH entering={entering} actors={actors.Count}", this);
        for (int i = 0; i < actors.Count; i++)
        {
            ActorState actor = actors[i];
            if (actor.Transform != null)
                actor.Transform.position = actor.To;

            if (entering)
                RestoreColors(actor);
            if (actor.Marker != null)
                actor.Marker.RestoreTransitionIdle();
            else if (actor.Animator != null)
                actor.Animator.PlayIdle();
        }

        transitionRoutine = null;
        CompletePendingTask();
    }

    private void CaptureActors(Transform[] actorRoots, bool includeIncapacitated)
    {
        actors.Clear();
        if (actorRoots == null || actorRoots.Length == 0)
            return;

        var capturedTransforms = new HashSet<Transform>();
        for (int rootIndex = 0; rootIndex < actorRoots.Length; rootIndex++)
            CaptureActors(actorRoots[rootIndex], capturedTransforms, includeIncapacitated);
    }

    private void CaptureActors(Transform actorRoot, HashSet<Transform> capturedTransforms, bool includeIncapacitated)
    {
        if (actorRoot == null)
            return;

        BattleCharacter[] characters = actorRoot.GetComponentsInChildren<BattleCharacter>(true);

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];
            if (character == null || !character.gameObject.activeSelf ||
                (!includeIncapacitated && character.RuntimeData?.IsDead == true))
            {
                continue;
            }

            CaptureActor(character.transform, capturedTransforms);
        }

        BattleMapSelectionCharacterMarker[] markers =
            actorRoot.GetComponentsInChildren<BattleMapSelectionCharacterMarker>(true);
        for (int i = 0; i < markers.Length; i++)
        {
            BattleMapSelectionCharacterMarker marker = markers[i];
            if (marker == null || !marker.gameObject.activeSelf)
                continue;

            CaptureActor(marker.transform, capturedTransforms);
        }
    }

    private void CaptureActor(Transform actorTransform, HashSet<Transform> capturedTransforms)
    {
        if (actorTransform == null || !capturedTransforms.Add(actorTransform))
            return;

        SpriteRenderer[] renderers = actorTransform.GetComponentsInChildren<SpriteRenderer>(true);
        var colors = new Color[renderers.Length];
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            colors[rendererIndex] = renderers[rendererIndex] != null
                ? renderers[rendererIndex].color
                : Color.white;

        actors.Add(new ActorState
        {
            Transform = actorTransform,
            FinalPosition = actorTransform.position,
            Renderers = renderers,
            OriginalColors = colors,
            Animator = actorTransform.GetComponentInChildren<BattleUnitAnimator>(true),
            Marker = actorTransform.GetComponent<BattleMapSelectionCharacterMarker>()
        });
    }

    private static void ApplyAlpha(ActorState actor, float alpha)
    {
        for (int i = 0; i < actor.Renderers.Length; i++)
        {
            SpriteRenderer renderer = actor.Renderers[i];
            if (renderer == null)
                continue;

            Color color = actor.OriginalColors[i];
            color.a *= Mathf.Clamp01(alpha);
            renderer.color = color;
        }
    }

    private static void RestoreColors(ActorState actor)
    {
        for (int i = 0; i < actor.Renderers.Length; i++)
        {
            if (actor.Renderers[i] != null)
                actor.Renderers[i].color = actor.OriginalColors[i];
        }
    }

    private void CompletePendingTask()
    {
        TaskCompletionSource<bool> source = completionSource;
        completionSource = null;
        source?.TrySetResult(true);
    }

    private void OnDisable()
    {
        CancelAndRestore(false);
    }

    private static string GetHierarchyPath(Transform t)
    {
        string path = t != null ? t.name : "(null)";
        while (t != null && t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    private sealed class ActorState
    {
        public Transform Transform;
        public Vector3 FinalPosition;
        public Vector3 From;
        public Vector3 To;
        public SpriteRenderer[] Renderers;
        public Color[] OriginalColors;
        public BattleUnitAnimator Animator;
        public BattleMapSelectionCharacterMarker Marker;
    }
}
