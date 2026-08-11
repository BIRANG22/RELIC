using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

/// <summary>
/// 시련 해금 진행도를 저장하고 해금 여부를 판정합니다.
/// </summary>
public static class TrialUnlockProgress
{
    private const string Stage3ClearKey = "TrialUnlock.Stage3ClearCount";
    private const string NocturnKillKey = "TrialUnlock.NocturnKillCount";
    private const string TwoTrialStage3ClearKey = "TrialUnlock.TwoTrialStage3ClearCount";

    private const string FirstAreaStageId = "Stage1";
    private const int RequiredNocturnKills = 3;

    private static readonly string[] UnlockRequirementKeys =
    {
        "lobby.trial.unlock.stage3_clear",
        "lobby.trial.unlock.nocturne_kill",
        "lobby.trial.unlock.two_trials_clear",
    };

    private static readonly string[] UnlockRequirementFallbacks =
    {
        "해금 : 3구역 1회 클리어",
        "해금 : 녹턴 3회 처치",
        "해금 : 2개의 시련을 적용 후 클리어",
    };

    public static event Action ProgressChanged;

    public static int Stage3ClearCount => PlayerPrefs.GetInt(Stage3ClearKey, 0);
    public static int NocturnKillCount => PlayerPrefs.GetInt(NocturnKillKey, 0);
    public static int TwoTrialStage3ClearCount => PlayerPrefs.GetInt(TwoTrialStage3ClearKey, 0);

    public static bool IsUnlocked(int trialIndex)
    {
        switch (trialIndex)
        {
            case 0:
                return Stage3ClearCount >= 1;

            case 1:
                return NocturnKillCount >= RequiredNocturnKills;

            case 2:
                return TwoTrialStage3ClearCount >= 1;

            default:
                return false;
        }
    }

    public static string GetUnlockRequirementText(int trialIndex)
    {
        string fallback = GetUnlockRequirementFallbackText(trialIndex);
        string key = GetUnlockRequirementKey(trialIndex);
        return string.IsNullOrWhiteSpace(key)
            ? fallback
            : GameLocalization.Get(key, fallback);
    }

    public static string GetUnlockRequirementKey(int trialIndex)
    {
        return IsValidTrialIndex(trialIndex)
            ? UnlockRequirementKeys[trialIndex]
            : string.Empty;
    }

    public static string GetUnlockRequirementFallbackText(int trialIndex)
    {
        return IsValidTrialIndex(trialIndex)
            ? UnlockRequirementFallbacks[trialIndex]
            : string.Empty;
    }

    private static bool IsValidTrialIndex(int trialIndex)
    {
        return trialIndex >= 0 && trialIndex < UnlockRequirementKeys.Length;
    }

    /// <summary>
    /// 전투 승리 시 처치한 녹턴 수를 누적합니다.
    /// </summary>
    public static void RecordDefeatedMonsters(IReadOnlyList<MonsterRuntimeData> monsters)
    {
        if (monsters == null || monsters.Count == 0)
            return;

        int addedNocturnKills = 0;

        for (int i = 0; i < monsters.Count; i++)
        {
            MonsterRuntimeData monster = monsters[i];
            if (IsNocturn(monster))
                addedNocturnKills++;
        }

        if (addedNocturnKills <= 0)
            return;

        PlayerPrefs.SetInt(NocturnKillKey, NocturnKillCount + addedNocturnKills);
        SaveAndNotify();
    }

    /// <summary>
    /// 보스 노드를 클리어했을 때 구역 및 적용 시련 수에 맞춰 진행도를 기록합니다.
    /// </summary>
    public static void RecordBossClear(MapRuntimeData runtime, int selectedTrialMask)
    {
        if (!IsFirstArea(runtime))
            return;

        PlayerPrefs.SetInt(Stage3ClearKey, Stage3ClearCount + 1);

        if (CountSelectedTrials(selectedTrialMask) >= 2)
        {
            PlayerPrefs.SetInt(
                TwoTrialStage3ClearKey,
                TwoTrialStage3ClearCount + 1);
        }

        SaveAndNotify();
    }

    public static int CountSelectedTrials(int mask)
    {
        int count = 0;
        int validMask = mask & ((1 << TrialSelectionState.TrialCount) - 1);

        while (validMask != 0)
        {
            count += validMask & 1;
            validMask >>= 1;
        }

        return count;
    }

    private static bool IsFirstArea(MapRuntimeData runtime)
    {
        if (runtime == null)
            return false;

        return string.Equals(
            runtime.CurrentStage,
            FirstAreaStageId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNocturn(MonsterRuntimeData monster)
    {
        if (monster == null)
            return false;

        return ContainsNocturn(monster.MonsterId) ||
               ContainsNocturn(monster.Name) ||
               ContainsNocturn(monster.DisplayName);
    }

    private static bool ContainsNocturn(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.IndexOf("Nocturn", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("녹턴", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// 모든 시련 해금 진행도를 초기화합니다.
    /// </summary>
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(Stage3ClearKey);
        PlayerPrefs.DeleteKey(NocturnKillKey);
        PlayerPrefs.DeleteKey(TwoTrialStage3ClearKey);
        PlayerPrefs.Save();

        ProgressChanged?.Invoke();
    }

    private static void SaveAndNotify()
    {
        PlayerPrefs.Save();
        ProgressChanged?.Invoke();
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Dustium/Trial Unlock/Reset Progress")]
    private static void ResetProgressFromEditor()
    {
        ResetProgress();
        Debug.Log("[TrialUnlockProgress] 시련 해금 진행도를 초기화했습니다.");
    }
#endif
}
