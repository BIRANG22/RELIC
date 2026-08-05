using System.Text;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ExplorationResultPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button returnToBaseButton;

    private ExplorationResultData result;
    private bool isTransitioning;
    private bool isDefeatResult;
    private float researchRewardMultiplier = 1f;

    private void Awake()
    {
        if (returnToBaseButton != null)
            returnToBaseButton.onClick.AddListener(ReturnToBase);
    }

    public void Open()
    {
        OpenInternal(false, 1f);
    }

    public void OpenDefeat(float rewardMultiplier = 0.5f)
    {
        OpenInternal(true, rewardMultiplier);
    }

    private void OpenInternal(bool defeat, float rewardMultiplier)
    {
        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.Get();
        result = ExplorationResultBuilder.Build(runtime);
        isDefeatResult = defeat;
        researchRewardMultiplier = Mathf.Max(0f, rewardMultiplier);
        isTransitioning = false;

        if (returnToBaseButton != null)
            returnToBaseButton.interactable = true;

        if (resultText != null)
            resultText.text = BuildText(result, isDefeatResult, researchRewardMultiplier);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private string BuildText(ExplorationResultData value, bool defeat, float rewardMultiplier)
    {
        StringBuilder text = new();
        if (defeat)
        {
            text.AppendLine("\uD328\uBC30");
            text.AppendLine($"\uBCF4\uC0C1 {Mathf.FloorToInt(Mathf.Max(0f, rewardMultiplier) * 100f)}%");
            text.AppendLine();
        }

        text.AppendLine("━━━━━━━━━━━━━━━━━");
        text.AppendLine("탐사 결과");
        text.AppendLine("━━━━━━━━━━━━━━━━━\n");
        text.AppendLine($"레드 더스티움\n{value.Remnant}\n");
        text.AppendLine("유물");
        AppendRelics(text, value);
        text.AppendLine("\n전투 기술 데이터");
        AppendSkills(text, value);
        text.AppendLine("\n캐릭터 전투 통계");
        AppendStatistics(text, value);
        return text.ToString();
    }

    private static void AppendRelics(StringBuilder text, ExplorationResultData value)
    {
        if (value.RelicIds.Count == 0) { text.AppendLine("없음"); return; }
        for (int i = 0; i < value.RelicIds.Count; i++)
        {
            string id = value.RelicIds[i];
            string name = id;
            string rarity = "";
            if (DataManager.Instance?.RelicDatabase != null &&
                DataManager.Instance.RelicDatabase.TryGet(id, out RelicData relic))
            {
                name = string.IsNullOrWhiteSpace(relic.Name) ? id : relic.Name;
                rarity = relic.Rarity;
            }
            text.AppendLine($"• {name} {rarity}");
        }
    }

    private static void AppendSkills(StringBuilder text, ExplorationResultData value)
    {
        if (value.NewSkillIds.Count == 0) { text.AppendLine("없음"); return; }
        for (int i = 0; i < value.NewSkillIds.Count; i++)
        {
            string id = value.NewSkillIds[i];
            string name = id;
            string rarity = "";
            if (DataManager.Instance?.SkillDatabase != null &&
                DataManager.Instance.SkillDatabase.TryGet(id, out SkillMasterData skill))
            {
                name = string.IsNullOrWhiteSpace(skill.Name) ? id : skill.Name;
                rarity = SkillRarityUtility.GetDisplayName(skill.Rarity);
            }
            text.AppendLine($"• {name} {rarity}");
        }
    }

    private static void AppendStatistics(StringBuilder text, ExplorationResultData value)
    {
        if (value.CharacterStatistics.Count == 0) { text.AppendLine("기록 없음"); return; }
        for (int i = 0; i < value.CharacterStatistics.Count; i++)
        {
            BattleRunCharacterStatisticsData stats = value.CharacterStatistics[i];
            string name = stats.CharacterId;
            if (DataManager.Instance?.CharacterDatabase != null &&
                DataManager.Instance.CharacterDatabase.TryGet(stats.CharacterId, out CharacterMasterData character) &&
                !string.IsNullOrWhiteSpace(character.Name))
            {
                name = character.Name;
            }
            text.AppendLine($"[{name}]");
            text.AppendLine($"입힌 피해 {stats.DamageDealt} / 받은 피해 {stats.DamageTaken}");
            text.AppendLine($"사망 {stats.DeathCount} / 처치 {stats.KillCount}");
        }
    }

    private async void ReturnToBase()
    {
        if (isTransitioning || result == null || DataManager.Instance == null || GameManager.Instance == null)
            return;

        isTransitioning = true;
        if (returnToBaseButton != null)
            returnToBaseButton.interactable = false;

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();
        lobby.PendingResearchResult = ExplorationResearchService.CreatePending(
            result,
            DataManager.Instance,
            researchRewardMultiplier);
        lobby.HasPendingResearchResult = true;
        lobby.OwnedRelicIds.Clear();
        ClearLobbyEquippedRelics(lobby);
        DataManager.Instance.LobbyRuntimeStore.Set(lobby);

        BattleRunAbandonService.AbandonCurrentRun(DataManager.Instance);
        SaveSystem.Instance?.SaveCurrentProgress();
        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Lobby);
    }

    private static void ClearLobbyEquippedRelics(LobbyRuntimeData lobby)
    {
        if (lobby.CharacterLoadouts == null)
            return;
        for (int i = 0; i < lobby.CharacterLoadouts.Count; i++)
            lobby.CharacterLoadouts[i].EquippedRelicIds = new string[7];
    }
}
