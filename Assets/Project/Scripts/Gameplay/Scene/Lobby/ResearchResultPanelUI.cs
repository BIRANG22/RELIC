using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ResearchResultPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button confirmButton;

    private void Start()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);

        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (!PendingResearchSettlementService.HasPending(lobby))
        {
            gameObject.SetActive(false);
            return;
        }

        PendingResearchResultData pending = lobby.PendingResearchResult;

        bool applied = PendingResearchSettlementService.ApplyOnce(lobby);
        DataManager.Instance.LobbyRuntimeStore.Set(lobby);
        SaveSystem.Instance?.SaveCurrentProgress();

        // 로비 HUD가 보상 정산보다 먼저 활성화될 수 있으므로,
        // 정산 직후 블루 더스티움 표시를 다시 갱신합니다.
        if (applied)
            LobbyBlueDustiumHudUI.RefreshAll();

        if (resultText != null)
            resultText.text = BuildText(pending);
        gameObject.SetActive(true);
    }

    private static string BuildText(PendingResearchResultData pending)
    {
        return
            "━━━━━━━━━━━━━━━━━\n" +
            "연구 진행...\n" +
            "━━━━━━━━━━━━━━━━━\n\n" +
            $"[레드 더스티움 정화]\n{pending.ExplorationResult.Remnant} → {pending.RemnantBlue} 블루\n\n" +
            $"[유물 분해]\n희귀 금속 회수\n+{pending.RelicBlue} 블루\n\n" +
            $"[기억 분석]\n전투 기술 추출\n+{pending.SkillBlue} 블루\n\n" +
            "━━━━━━━━━━━━━━━━━\n\n" +
            $"총 획득\n\n블루 더스티움\n{pending.TotalBlue}";
    }

    private void Confirm()
    {
        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (lobby != null)
        {
            PendingResearchSettlementService.Clear(lobby);
            DataManager.Instance.LobbyRuntimeStore.Set(lobby);
            SaveSystem.Instance?.SaveCurrentProgress();
        }

        gameObject.SetActive(false);
    }
}
