using System;
using Relic.Gameplay.Data;
using UnityEngine.Localization.Settings;

[Serializable]
public sealed class LobbyQuestTextConfig
{
    public string SetupQuestText = "탐사 준비";
    public string FirstExpeditionQuestText = "첫 탐사 - 각성핵 회수 {Current}/{Target}";
    public int FirstExpeditionTargetCount = 1;
    public string FirstExpeditionRequiredItemId;

    public static LobbyQuestTextConfig Default => new();
}

public readonly struct LobbyQuestState
{
    public LobbyQuestState(
        LobbyTutorialProgress progress,
        bool isVisible,
        string text)
    {
        Progress = progress;
        IsVisible = isVisible;
        Text = text ?? string.Empty;
    }

    public LobbyTutorialProgress Progress { get; }
    public bool IsVisible { get; }
    public string Text { get; }

    public static LobbyQuestState Build(
        LobbyRuntimeData lobby,
        LobbyQuestTextConfig config)
    {
        config ??= LobbyQuestTextConfig.Default;
        LobbyTutorialProgress progress =
            lobby != null ? lobby.TutorialProgress : LobbyTutorialProgress.NotStarted;

        switch (progress)
        {
            case LobbyTutorialProgress.WaitingForSetup:
                return new LobbyQuestState(
                    progress,
                    true,
                    IsKoreanLocale()
                        ? "탐사 준비"
                        : GameLocalization.Get("lobby.quest.setup", config.SetupQuestText));

            case LobbyTutorialProgress.FirstExpeditionAssigned:
                int targetCount = Math.Max(1, config.FirstExpeditionTargetCount);
                int currentCount = TrialUnlockProgress.HasFirstStageAwakeningCore
                    ? targetCount
                    : 0;
                string text = (IsKoreanLocale()
                        ? "첫 탐사 - 각성핵 회수 {Current}/{Target}"
                        : GameLocalization.Get(
                            "lobby.quest.first_expedition",
                            config.FirstExpeditionQuestText))
                    .Replace("{Current}", currentCount.ToString())
                    .Replace("{Target}", targetCount.ToString());
                return new LobbyQuestState(progress, true, text);

            default:
                return new LobbyQuestState(progress, false, string.Empty);
        }
    }

    // Existing localization entries or serialized Inspector fields may still contain the old wording.
    // Use the revised Korean quest copy without affecting translations in other languages.
    private static bool IsKoreanLocale()
    {
        string localeCode = LocalizationSettings.SelectedLocale?.Identifier.Code;
        return string.IsNullOrEmpty(localeCode) ||
               localeCode.StartsWith("ko", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanUseFeature(
        LobbyTutorialProgress current,
        LobbyTutorialProgress required)
    {
        return current >= required;
    }

}
