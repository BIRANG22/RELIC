using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonResponsiveSpriteAnimator : MonoBehaviour
{
    private static readonly List<ButtonResponsiveSpriteAnimator> activeSceneInstances = new();

    public enum IdleState
    {
        Normal,
        Skill,
        Rune
    }

    [Header("Sprite Targets")]
    [SerializeField] private Image targetImage;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    [Header("Start State")]
    [SerializeField] private IdleState startState = IdleState.Normal;

    [Header("Animator State Mode")]
    [SerializeField] private bool useAnimatorStates = true;
    [SerializeField] private bool routeAssetCallsToActiveInstance = true;
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private int animatorLayer = 0;
    [SerializeField] private string animatorLayerName = "Base Layer";
    [SerializeField] private string selectIdleStateName = "hit_select_idle";
    [SerializeField] private string skillIdleStateName = "hit_skill_idle";
    [SerializeField] private string runeIdleStateName = "hit_rune_idle";
    [SerializeField] private string selectToRuneStateName = "hit_select_to_rune";
    [SerializeField] private string selectToRuneReverseStateName = "hit_select_to_rune_reverse";
    [SerializeField] private string selectToSkillStateName = "hit_select_to_skill";
    [SerializeField] private string selectToSkillReverseStateName = "hit_select_to_skill_reverse";
    [SerializeField] private string skillToRuneStateName = "hit_skill_to_rune";
    [SerializeField] private string skillToRuneReverseStateName = "hit_skill_to_rune_reverse";
    [SerializeField] private float transitionTimeoutSeconds = 3f;

    [Header("Sprite Idle Animation")]
    [SerializeField] private Sprite[] normalIdleSprites;
    [SerializeField] private Sprite[] skillIdleSprites;
    [SerializeField] private Sprite[] runeIdleSprites;

    [Header("Sprite Transition Animation")]
    [Tooltip("Normal Idle -> Skill Idle sprites")]
    [SerializeField] private Sprite[] normalToSkillSprites;

    [Tooltip("Skill Idle -> Rune Idle sprites")]
    [SerializeField] private Sprite[] skillToRuneSprites;

    [Tooltip("Rune Idle -> Normal Idle sprites")]
    [SerializeField] private Sprite[] runeToNormalSprites;

    [Header("Sprite Frame Timing")]
    [SerializeField] private float idleFrameInterval = 0.12f;
    [SerializeField] private float transitionFrameInterval = 0.08f;

    [Header("Options")]
    [SerializeField] private bool playOnEnable = true;

    private IdleState currentState;
    private Coroutine playRoutine;
    private int idleFrameIndex;

    private void Awake()
    {
        CacheAnimatorIfNeeded();
        currentState = startState;
    }

    private void OnEnable()
    {
        RegisterSceneInstance();
        CacheAnimatorIfNeeded();

        if (playOnEnable)
            ForceState(startState);
    }

    private void OnDisable()
    {
        UnregisterSceneInstance();
        StopCurrentRoutine();
    }

    private void OnDestroy()
    {
        UnregisterSceneInstance();
        StopCurrentRoutine();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheAnimatorIfNeeded();
    }
#endif

    public void ShowNormal()
    {
        GetRouteTarget().ChangeState(IdleState.Normal);
    }

    public void ShowSkill()
    {
        GetRouteTarget().ChangeState(IdleState.Skill);
    }

    public void ShowRune()
    {
        GetRouteTarget().ChangeState(IdleState.Rune);
    }

    public void ForceState(IdleState state)
    {
        if (CanUseAnimatorStates())
        {
            ForceAnimatorState(state);
            return;
        }

        StopCurrentRoutine();

        currentState = state;
        idleFrameIndex = 0;

        Sprite[] idleSprites = GetIdleSprites(state);

        if (idleSprites != null && idleSprites.Length > 0)
            SetSprite(idleSprites[0]);

        if (!isActiveAndEnabled)
            return;

        playRoutine = StartCoroutine(PlayIdleLoop(idleSprites));
    }

    public void ChangeState(IdleState nextState)
    {
        if (currentState == nextState)
            return;

        if (CanUseAnimatorStates())
        {
            ChangeAnimatorState(nextState);
            return;
        }

        StopCurrentRoutine();

        if (!isActiveAndEnabled)
        {
            currentState = nextState;
            idleFrameIndex = 0;

            Sprite[] idleSprites = GetIdleSprites(nextState);

            if (idleSprites != null && idleSprites.Length > 0)
                SetSprite(idleSprites[0]);

            return;
        }

        playRoutine = StartCoroutine(ChangeStateRoutine(currentState, nextState));
    }

    private void ForceAnimatorState(IdleState state)
    {
        StopCurrentRoutine();

        currentState = state;

        if (!isActiveAndEnabled)
            return;

        PlayAnimatorState(GetAnimatorIdleStateName(state), 0f);
    }

    private void ChangeAnimatorState(IdleState nextState)
    {
        // 현재 탭 상태를 전환 출발점으로 사용한다.
        // Animator의 실제 재생 상태를 다시 판정하면 전환 도중 이전 Idle로 오인하여
        // 잘못된 방향의 애니메이션이 선택될 수 있다.
        IdleState previousState = currentState;

        StopCurrentRoutine();

        if (!isActiveAndEnabled)
        {
            currentState = nextState;
            return;
        }

        playRoutine = StartCoroutine(ChangeAnimatorStateRoutine(previousState, nextState));
    }

    private IEnumerator ChangeAnimatorStateRoutine(IdleState from, IdleState to)
    {
        string transitionStateName = GetAnimatorTransitionStateName(from, to);

        if (!string.IsNullOrWhiteSpace(transitionStateName))
            yield return PlayAnimatorTransitionRoutine(transitionStateName);

        currentState = to;
        PlayAnimatorState(GetAnimatorIdleStateName(to), 0f);
        playRoutine = null;
    }

    private IEnumerator PlayAnimatorTransitionRoutine(string stateName)
    {
        if (!PlayAnimatorState(stateName, 0f))
            yield break;

        float waitDuration = transitionTimeoutSeconds;
        float elapsed = 0f;
        yield return null;

        if (targetAnimator != null && targetAnimator.isActiveAndEnabled)
        {
            AnimatorStateInfo stateInfo = targetAnimator.GetCurrentAnimatorStateInfo(animatorLayer);

            if (IsAnimatorState(stateInfo, stateName) && stateInfo.length > 0f)
                waitDuration = Mathf.Min(Mathf.Abs(stateInfo.length), transitionTimeoutSeconds);
        }

        while (elapsed < waitDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private bool PlayAnimatorState(string stateName, float normalizedTime)
    {
        if (string.IsNullOrWhiteSpace(stateName))
            return false;

        CacheAnimatorIfNeeded();

        if (targetAnimator == null || targetAnimator.runtimeAnimatorController == null)
            return false;

        if (!TryGetAnimatorStateHash(stateName, out int stateHash))
        {
            Debug.LogWarning($"{nameof(ButtonResponsiveSpriteAnimator)} could not find animator state '{stateName}'.", this);
            return false;
        }

        if (!targetAnimator.enabled)
            targetAnimator.enabled = true;

        targetAnimator.speed = 1f;
        targetAnimator.Play(stateHash, animatorLayer, normalizedTime);
        targetAnimator.Update(0f);
        return true;
    }

    private bool TryGetAnimatorStateHash(string stateName, out int stateHash)
    {
        stateHash = Animator.StringToHash(stateName);

        if (targetAnimator.HasState(animatorLayer, stateHash))
            return true;

        string fullStateName = GetAnimatorFullStateName(stateName);
        stateHash = Animator.StringToHash(fullStateName);

        return targetAnimator.HasState(animatorLayer, stateHash);
    }

    private string GetAnimatorFullStateName(string stateName)
    {
        if (stateName.Contains(".") || string.IsNullOrWhiteSpace(animatorLayerName))
            return stateName;

        return animatorLayerName + "." + stateName;
    }

    private bool IsAnimatorState(AnimatorStateInfo stateInfo, string stateName)
    {
        int shortHash = Animator.StringToHash(stateName);
        int fullHash = Animator.StringToHash(GetAnimatorFullStateName(stateName));

        return stateInfo.shortNameHash == shortHash ||
               stateInfo.fullPathHash == shortHash ||
               stateInfo.fullPathHash == fullHash;
    }

    private IEnumerator ChangeStateRoutine(IdleState from, IdleState to)
    {
        Sprite[] transitionSprites = GetTransitionSprites(from, to);
        bool reverse = ShouldReverseTransition(from, to);

        if (transitionSprites != null && transitionSprites.Length > 0)
            yield return PlayTransition(transitionSprites, reverse);

        currentState = to;
        idleFrameIndex = 0;

        Sprite[] idleSprites = GetIdleSprites(to);
        yield return PlayIdleLoop(idleSprites);
    }

    private IEnumerator PlayIdleLoop(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
            yield break;

        while (true)
        {
            SetSprite(sprites[idleFrameIndex]);

            idleFrameIndex++;
            if (idleFrameIndex >= sprites.Length)
                idleFrameIndex = 0;

            yield return new WaitForSeconds(idleFrameInterval);
        }
    }

    private IEnumerator PlayTransition(Sprite[] sprites, bool reverse)
    {
        if (sprites == null || sprites.Length == 0)
            yield break;

        if (!reverse)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                SetSprite(sprites[i]);
                yield return new WaitForSeconds(transitionFrameInterval);
            }
        }
        else
        {
            for (int i = sprites.Length - 1; i >= 0; i--)
            {
                SetSprite(sprites[i]);
                yield return new WaitForSeconds(transitionFrameInterval);
            }
        }
    }

    private Sprite[] GetIdleSprites(IdleState state)
    {
        switch (state)
        {
            case IdleState.Normal:
                return normalIdleSprites;

            case IdleState.Skill:
                return skillIdleSprites;

            case IdleState.Rune:
                return runeIdleSprites;
        }

        return normalIdleSprites;
    }

    private Sprite[] GetTransitionSprites(IdleState from, IdleState to)
    {
        if (from == IdleState.Normal && to == IdleState.Skill)
            return normalToSkillSprites;

        if (from == IdleState.Skill && to == IdleState.Normal)
            return normalToSkillSprites;

        if (from == IdleState.Skill && to == IdleState.Rune)
            return skillToRuneSprites;

        if (from == IdleState.Rune && to == IdleState.Skill)
            return skillToRuneSprites;

        if (from == IdleState.Rune && to == IdleState.Normal)
            return runeToNormalSprites;

        if (from == IdleState.Normal && to == IdleState.Rune)
            return runeToNormalSprites;

        return null;
    }

    private bool ShouldReverseTransition(IdleState from, IdleState to)
    {
        if (from == IdleState.Skill && to == IdleState.Normal)
            return true;

        if (from == IdleState.Rune && to == IdleState.Skill)
            return true;

        if (from == IdleState.Normal && to == IdleState.Rune)
            return true;

        return false;
    }

    private string GetAnimatorIdleStateName(IdleState state)
    {
        switch (state)
        {
            case IdleState.Normal:
                return selectIdleStateName;

            case IdleState.Skill:
                return skillIdleStateName;

            case IdleState.Rune:
                return runeIdleStateName;
        }

        return selectIdleStateName;
    }

    private string GetAnimatorTransitionStateName(IdleState from, IdleState to)
    {
        if (from == IdleState.Normal && to == IdleState.Skill)
            return selectToSkillStateName;

        if (from == IdleState.Skill && to == IdleState.Normal)
            return selectToSkillReverseStateName;

        // hilt_rune_to_select는 룬 -> 일반 정방향,
        // hilt_rune_to_select_reverse는 일반 -> 룬 역방향이다.
        if (from == IdleState.Normal && to == IdleState.Rune)
            return selectToRuneReverseStateName;

        if (from == IdleState.Rune && to == IdleState.Normal)
            return selectToRuneStateName;

        if (from == IdleState.Skill && to == IdleState.Rune)
            return skillToRuneStateName;

        if (from == IdleState.Rune && to == IdleState.Skill)
            return skillToRuneReverseStateName;

        return null;
    }

    private bool CanUseAnimatorStates()
    {
        if (!useAnimatorStates)
            return false;

        CacheAnimatorIfNeeded();

        if (targetAnimator == null || targetAnimator.runtimeAnimatorController == null)
            return false;

        return TryGetAnimatorStateHash(selectIdleStateName, out _);
    }

    private ButtonResponsiveSpriteAnimator GetRouteTarget()
    {
        if (IsSceneInstance() || !routeAssetCallsToActiveInstance)
            return this;

        ButtonResponsiveSpriteAnimator activeInstance = FindActiveSceneInstance();
        return activeInstance != null ? activeInstance : this;
    }

    private static ButtonResponsiveSpriteAnimator FindActiveSceneInstance()
    {
        for (int i = activeSceneInstances.Count - 1; i >= 0; i--)
        {
            ButtonResponsiveSpriteAnimator instance = activeSceneInstances[i];

            if (instance == null)
            {
                activeSceneInstances.RemoveAt(i);
                continue;
            }

            if (!instance.isActiveAndEnabled)
                continue;

            if (!instance.gameObject.activeInHierarchy)
                continue;

            return instance;
        }

        return null;
    }

    private void RegisterSceneInstance()
    {
        if (!IsSceneInstance())
            return;

        if (!activeSceneInstances.Contains(this))
            activeSceneInstances.Add(this);
    }

    private void UnregisterSceneInstance()
    {
        activeSceneInstances.Remove(this);
    }

    private bool IsSceneInstance()
    {
        return gameObject != null &&
               gameObject.scene.IsValid() &&
               gameObject.scene.isLoaded;
    }

    private void CacheAnimatorIfNeeded()
    {
        if (targetAnimator == null)
            targetAnimator = GetComponent<Animator>();
    }

    private void SetSprite(Sprite sprite)
    {
        if (sprite == null)
            return;

        if (targetImage != null)
            targetImage.sprite = sprite;

        if (targetSpriteRenderer != null)
            targetSpriteRenderer.sprite = sprite;
    }

    private void StopCurrentRoutine()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
    }
}
