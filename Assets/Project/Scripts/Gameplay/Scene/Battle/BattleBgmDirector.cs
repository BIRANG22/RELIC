using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleBgmPolicy
{
    public static BgmState? ResolveFirstConfigured(
        IEnumerable<BgmState> candidates,
        Func<BgmState, bool> isConfigured)
    {
        if (candidates == null || isConfigured == null)
            return null;

        foreach (BgmState candidate in candidates)
        {
            if (isConfigured(candidate))
                return candidate;
        }
        return null;
    }

    public static bool IsLowHealth(int currentHp, int maxHp, float threshold)
    {
        return currentHp > 0 && maxHp > 0 && (float)currentHp / maxHp <= Mathf.Clamp01(threshold);
    }

    public static bool MatchesRoom(
        string bindingStage, string bindingType, BattleBgmRoomVariant variant,
        string stage, string roomType, bool hasBattleMap)
    {
        if (!string.Equals(bindingStage, stage, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(bindingType, roomType, StringComparison.OrdinalIgnoreCase))
            return false;

        return variant == BattleBgmRoomVariant.Any ||
            (variant == BattleBgmRoomVariant.Battle && hasBattleMap) ||
            (variant == BattleBgmRoomVariant.Event && !hasBattleMap);
    }

    public static BgmState? ResolveBattleState(
        bool defeatPlaying,
        bool bossIntroPlaying,
        bool arabellaPhaseTwoPlaying,
        bool partyLowHealth,
        BgmState defeatState,
        BgmState bossIntroState,
        BgmState arabellaPhaseTwoState,
        BgmState lowHealthState,
        BgmState? roomState,
        Func<BgmState, bool> isConfigured)
    {
        List<BgmState> candidates = new();
        if (defeatPlaying) candidates.Add(defeatState);
        if (bossIntroPlaying) candidates.Add(bossIntroState);
        if (arabellaPhaseTwoPlaying) candidates.Add(arabellaPhaseTwoState);
        if (partyLowHealth) candidates.Add(lowHealthState);
        if (roomState.HasValue) candidates.Add(roomState.Value);
        return ResolveFirstConfigured(candidates, isConfigured);
    }
}

public enum BattleBgmRoomVariant
{
    Any,
    Battle,
    Event
}

public class BattleBgmDirector : MonoBehaviour
{
    [Serializable]
    private struct RoomBgmBinding
    {
        public string stage;
        public string roomType;
        public BattleBgmRoomVariant variant;
        public BgmState state;
    }

    [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.3f;
    [SerializeField] private BgmState lowHealthState = BgmState.Stage1LowHealth;
    [SerializeField] private BgmState bossIntroState = BgmState.Stage1BossIntro;
    [SerializeField] private BgmState defeatState = BgmState.Stage1Defeat;
    [SerializeField] private BgmState arabellaPhaseTwoState = BgmState.ArabellaPhase2;
    [SerializeField, Min(0f)] private float arabellaPhaseTwoFadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float arabellaPhaseTwoFadeInDuration = 0.5f;
    [SerializeField] private List<RoomBgmBinding> roomBgmBindings = new();

    private string currentStage;
    private string currentRoomType;
    private bool currentHasBattleMap;
    private bool bossIntroPlaying;
    private bool defeatPlaying;
    private bool arabellaPhaseTwoPlaying;

    private void OnEnable()
    {
        BattleSceneController.BattleRoomSelected += SetRoomType;
        BattleSceneController.BattleRoomIntroStarted += OnRoomIntroStarted;
        BattleSceneController.BattleRoomIntroCompleted += OnRoomIntroCompleted;
        BattleResultChecker.DefeatStarted += OnDefeatStarted;
        BattleEffectUtility.OnPlayerDamaged += OnPlayerDamaged;
        BattleTurnExecutor.PlayerTurnReturned += Refresh;
        ArabellaPhaseTransition.PhaseTwoPresentationStarted += OnArabellaPhaseTwoPresentationStarted;
        ArabellaPhaseTransition.PhaseTwoPresentationCompleted += OnArabellaPhaseTwoPresentationCompleted;
    }

    private void OnDisable()
    {
        BattleSceneController.BattleRoomSelected -= SetRoomType;
        BattleSceneController.BattleRoomIntroStarted -= OnRoomIntroStarted;
        BattleSceneController.BattleRoomIntroCompleted -= OnRoomIntroCompleted;
        BattleResultChecker.DefeatStarted -= OnDefeatStarted;
        BattleEffectUtility.OnPlayerDamaged -= OnPlayerDamaged;
        BattleTurnExecutor.PlayerTurnReturned -= Refresh;
        ArabellaPhaseTransition.PhaseTwoPresentationStarted -= OnArabellaPhaseTwoPresentationStarted;
        ArabellaPhaseTransition.PhaseTwoPresentationCompleted -= OnArabellaPhaseTwoPresentationCompleted;
    }

    public void SetRoomType(string stage, string roomType, bool hasBattleMap)
    {
        currentStage = stage;
        currentRoomType = roomType;
        currentHasBattleMap = hasBattleMap;
        bossIntroPlaying = false;
        defeatPlaying = false;
        arabellaPhaseTwoPlaying = false;
        Refresh();
    }

    private void OnRoomIntroStarted() { if (currentRoomType == "Boss") { bossIntroPlaying = true; Refresh(); } }
    private void OnRoomIntroCompleted() { if (bossIntroPlaying) { bossIntroPlaying = false; Refresh(); } }
    private void OnDefeatStarted() { defeatPlaying = true; Refresh(); }
    private void OnPlayerDamaged(BattleCharacter _) => Refresh();
    private void OnArabellaPhaseTwoPresentationStarted()
    {
        AudioManager.Instance?.FadeOutBgm(arabellaPhaseTwoFadeOutDuration);
    }

    private void OnArabellaPhaseTwoPresentationCompleted()
    {
        arabellaPhaseTwoPlaying = true;
        Refresh(0f, arabellaPhaseTwoFadeInDuration);
    }

    private void Refresh()
    {
        Refresh(null, null);
    }

    private void Refresh(float? fadeOutDuration, float? fadeInDuration)
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null)
            return;

        BgmState? roomState = TryGetRoomState(
            currentStage, currentRoomType, currentHasBattleMap, out BgmState configuredRoomState)
            ? configuredRoomState
            : null;
        BgmState? resolved = BattleBgmPolicy.ResolveBattleState(
            defeatPlaying,
            bossIntroPlaying,
            arabellaPhaseTwoPlaying,
            IsPartyLowHealth(),
            defeatState,
            bossIntroState,
            arabellaPhaseTwoState,
            lowHealthState,
            roomState,
            audio.IsBgmConfigured);
        if (!resolved.HasValue)
            return;

        if (fadeOutDuration.HasValue && fadeInDuration.HasValue)
            audio.PlayBgm(resolved.Value, fadeOutDuration.Value, fadeInDuration.Value);
        else
            audio.PlayBgm(resolved.Value);
    }

    private bool IsPartyLowHealth()
    {
        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < characters.Length; i++)
        {
            var data = characters[i]?.RuntimeData;
            if (data != null && BattleBgmPolicy.IsLowHealth(data.CurrentHP, data.MaxHP, lowHealthThreshold))
                return true;
        }
        return false;
    }

    private bool TryGetRoomState(string stage, string roomType, bool hasBattleMap, out BgmState state)
    {
        for (int i = 0; i < roomBgmBindings.Count; i++)
        {
            RoomBgmBinding binding = roomBgmBindings[i];
            if (BattleBgmPolicy.MatchesRoom(
                binding.stage, binding.roomType, binding.variant,
                stage, roomType, hasBattleMap))
            {
                state = binding.state;
                return true;
            }
        }
        state = default;
        return false;
    }
}
