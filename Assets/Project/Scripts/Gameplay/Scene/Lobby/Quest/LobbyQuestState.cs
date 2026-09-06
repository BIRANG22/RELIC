using System;
using Relic.Gameplay.Data;

[Serializable]
public sealed class LobbyQuestTextConfig
{
    public string SetupQuestText = "파티편성 및 캐릭터 세팅이 완료 되었다면, 엘릭과 대화하세요";
    public string FirstExpeditionQuestText = "로데른 폐허에서 아라벨라를 처치하고 아라벨라의 각성핵을 획득하세요. ({Current}/{Target})";
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
                    config.SetupQuestText);

            case LobbyTutorialProgress.FirstExpeditionAssigned:
                int targetCount = Math.Max(1, config.FirstExpeditionTargetCount);
                int currentCount = HasRequiredItem(lobby, config.FirstExpeditionRequiredItemId)
                    ? targetCount
                    : 0;
                string text = (config.FirstExpeditionQuestText ?? string.Empty)
                    .Replace("{Current}", currentCount.ToString())
                    .Replace("{Target}", targetCount.ToString());
                return new LobbyQuestState(progress, true, text);

            default:
                return new LobbyQuestState(progress, false, string.Empty);
        }
    }

    public static bool CanUseFeature(
        LobbyTutorialProgress current,
        LobbyTutorialProgress required)
    {
        return current >= required;
    }

    private static bool HasRequiredItem(LobbyRuntimeData lobby, string itemId)
    {
        if (lobby?.BagItemIds == null || string.IsNullOrWhiteSpace(itemId))
            return false;

        string normalizedItemId = itemId.Trim();
        for (int i = 0; i < lobby.BagItemIds.Count; i++)
        {
            string candidate = lobby.BagItemIds[i];
            if (!string.IsNullOrWhiteSpace(candidate) &&
                string.Equals(
                    candidate.Trim(),
                    normalizedItemId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
