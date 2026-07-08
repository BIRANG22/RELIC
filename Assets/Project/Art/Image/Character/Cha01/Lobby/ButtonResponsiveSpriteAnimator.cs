using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ButtonResponsiveSpriteAnimator : MonoBehaviour
{
    public enum IdleState
    {
        Normal,
        Skill,
        Rune
    }

    [Header("출력 대상")]
    [SerializeField] private Image targetImage;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    [Header("시작 상태")]
    [SerializeField] private IdleState startState = IdleState.Normal;

    [Header("Idle 애니메이션")]
    [SerializeField] private Sprite[] normalIdleSprites;
    [SerializeField] private Sprite[] skillIdleSprites;
    [SerializeField] private Sprite[] runeIdleSprites;

    [Header("전환 애니메이션")]
    [Tooltip("일반 Idle -> 스킬 Idle 로 전환되는 스프라이트")]
    [SerializeField] private Sprite[] normalToSkillSprites;

    [Tooltip("스킬 Idle -> 룬 Idle 로 전환되는 스프라이트")]
    [SerializeField] private Sprite[] skillToRuneSprites;

    [Tooltip("룬 Idle -> 일반 Idle 로 전환되는 스프라이트")]
    [SerializeField] private Sprite[] runeToNormalSprites;

    [Header("재생 속도")]
    [SerializeField] private float idleFrameInterval = 0.12f;
    [SerializeField] private float transitionFrameInterval = 0.08f;

    [Header("옵션")]
    [SerializeField] private bool playOnEnable = true;

    private IdleState currentState;
    private Coroutine playRoutine;
    private int idleFrameIndex;

    private void Awake()
    {
        currentState = startState;
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            ForceState(startState);
        }
    }

    private void OnDisable()
    {
        StopCurrentRoutine();
    }

    /// <summary>
    /// 프리셋 버튼에서 호출
    /// </summary>
    public void ShowNormal()
    {
        ChangeState(IdleState.Normal);
    }

    /// <summary>
    /// 스킬 버튼에서 호출
    /// </summary>
    public void ShowSkill()
    {
        ChangeState(IdleState.Skill);
    }

    /// <summary>
    /// 룬 버튼에서 호출
    /// </summary>
    public void ShowRune()
    {
        ChangeState(IdleState.Rune);
    }

    /// <summary>
    /// 현재 상태와 상관없이 바로 해당 Idle로 고정
    /// </summary>
    public void ForceState(IdleState state)
    {
        StopCurrentRoutine();

        currentState = state;
        idleFrameIndex = 0;

        Sprite[] idleSprites = GetIdleSprites(state);

        if (idleSprites != null && idleSprites.Length > 0)
        {
            SetSprite(idleSprites[0]);
        }

        // 오브젝트가 비활성화 상태면 Coroutine을 시작하지 않음
        if (!isActiveAndEnabled)
            return;

        playRoutine = StartCoroutine(PlayIdleLoop(idleSprites));
    }

    public void ChangeState(IdleState nextState)
    {
        if (currentState == nextState)
            return;

        StopCurrentRoutine();

        // 오브젝트가 비활성화 상태면 Coroutine을 시작하지 않고
        // 상태와 첫 이미지만 바꿔둠
        if (!isActiveAndEnabled)
        {
            currentState = nextState;
            idleFrameIndex = 0;

            Sprite[] idleSprites = GetIdleSprites(nextState);

            if (idleSprites != null && idleSprites.Length > 0)
            {
                SetSprite(idleSprites[0]);
            }

            return;
        }

        playRoutine = StartCoroutine(ChangeStateRoutine(currentState, nextState));
    }

    private IEnumerator ChangeStateRoutine(IdleState from, IdleState to)
    {
        Sprite[] transitionSprites = GetTransitionSprites(from, to);
        bool reverse = ShouldReverseTransition(from, to);

        if (transitionSprites != null && transitionSprites.Length > 0)
        {
            yield return PlayTransition(transitionSprites, reverse);
        }

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
            {
                idleFrameIndex = 0;
            }

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
        // 일반 -> 스킬
        if (from == IdleState.Normal && to == IdleState.Skill)
            return normalToSkillSprites;

        // 스킬 -> 일반 : 일반 -> 스킬 전환을 역재생
        if (from == IdleState.Skill && to == IdleState.Normal)
            return normalToSkillSprites;

        // 스킬 -> 룬
        if (from == IdleState.Skill && to == IdleState.Rune)
            return skillToRuneSprites;

        // 룬 -> 스킬 : 스킬 -> 룬 전환을 역재생
        if (from == IdleState.Rune && to == IdleState.Skill)
            return skillToRuneSprites;

        // 룬 -> 일반
        if (from == IdleState.Rune && to == IdleState.Normal)
            return runeToNormalSprites;

        // 일반 -> 룬 : 룬 -> 일반 전환을 역재생
        if (from == IdleState.Normal && to == IdleState.Rune)
            return runeToNormalSprites;

        return null;
    }

    private bool ShouldReverseTransition(IdleState from, IdleState to)
    {
        // 스킬 -> 일반 : 일반 -> 스킬 전환 역재생
        if (from == IdleState.Skill && to == IdleState.Normal)
            return true;

        // 룬 -> 스킬 : 스킬 -> 룬 전환 역재생
        if (from == IdleState.Rune && to == IdleState.Skill)
            return true;

        // 일반 -> 룬 : 룬 -> 일반 전환 역재생
        if (from == IdleState.Normal && to == IdleState.Rune)
            return true;

        return false;
    }

    private void SetSprite(Sprite sprite)
    {
        if (sprite == null)
            return;

        if (targetImage != null)
        {
            targetImage.sprite = sprite;
        }

        if (targetSpriteRenderer != null)
        {
            targetSpriteRenderer.sprite = sprite;
        }
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