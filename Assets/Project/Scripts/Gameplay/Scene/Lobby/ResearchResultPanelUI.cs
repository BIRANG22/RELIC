using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ResearchResultPanelUI : MonoBehaviour
{
    private const string LobbySceneName = "Lobby";

    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button confirmButton;
    [Header("Reward VFX")]
    [SerializeField] private Transform blueDustiumVfxRoot;
    [SerializeField] private CanvasGroup presentationCanvasGroup;

    private Coroutine vfxPresentationCoroutine;
    private bool isOpening;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterPendingResultAutoOpen()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryOpenPendingResultOnLoadedScene();
    }

    private void Start()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);

        OpenIfPending();
    }

    public static bool TryOpenPendingResultOnLoadedScene()
    {
        ResearchResultPanelUI[] panels =
            Resources.FindObjectsOfTypeAll<ResearchResultPanelUI>();

        for (int i = 0; i < panels.Length; i++)
        {
            ResearchResultPanelUI panel = panels[i];
            if (panel == null ||
                panel.gameObject == null ||
                !panel.gameObject.scene.IsValid() ||
                panel.gameObject.scene.name != LobbySceneName)
            {
                continue;
            }

            if (panel.OpenIfPending())
                return true;
        }

        return false;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.IsValid() && scene.name == LobbySceneName)
            TryOpenPendingResultOnLoadedScene();
    }

    private bool OpenIfPending()
    {
        if (isOpening || vfxPresentationCoroutine != null)
            return true;

        isOpening = true;

        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (!PendingResearchSettlementService.HasPending(lobby))
        {
            gameObject.SetActive(false);
            isOpening = false;
            return false;
        }

        PendingResearchResultData pending = lobby.PendingResearchResult;

        if (!CanLocalPlayerMutateHostOnlyState())
        {
            if (resultText != null)
                resultText.text = BuildText(pending);

            if (!BeginVfxPresentation())
                ShowResultPanel();

            isOpening = false;
            return true;
        }

        bool applied = PendingResearchSettlementService.ApplyOnce(lobby);
        DataManager.Instance.LobbyRuntimeStore.Set(lobby);
        SaveSystem.Instance?.SaveCurrentProgress();

        // 로비 HUD가 보상 정산보다 먼저 활성화될 수 있으므로,
        // 정산 직후 블루 더스티움 표시를 다시 갱신합니다.
        if (applied)
            LobbyBlueDustiumHudUI.RefreshAll();

        if (applied)
            PublishHostSnapshotAfterLocalMutation();

        if (resultText != null)
            resultText.text = BuildText(pending);

        if (!BeginVfxPresentation())
            ShowResultPanel();

        isOpening = false;
        return true;
    }

    private bool BeginVfxPresentation()
    {
        if (blueDustiumVfxRoot == null)
            return false;

        gameObject.SetActive(true);
        SetPresentationVisible(false);
        blueDustiumVfxRoot.gameObject.SetActive(true);
        RestartVfxParticles();
        LobbyPositionModalInputBlocker.Block(this);
        vfxPresentationCoroutine = StartCoroutine(WaitForVfxAndShowResult());
        return true;
    }

    private IEnumerator WaitForVfxAndShowResult()
    {
        yield return null;

        while (AreVfxParticlesAlive())
            yield return null;

        vfxPresentationCoroutine = null;
        LobbyPositionModalInputBlocker.Unblock(this);

        if (blueDustiumVfxRoot != null)
            blueDustiumVfxRoot.gameObject.SetActive(false);

        ShowResultPanel();
    }

    private void RestartVfxParticles()
    {
        ParticleSystem[] particleSystems =
            blueDustiumVfxRoot.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    private bool AreVfxParticlesAlive()
    {
        ParticleSystem[] particleSystems =
            blueDustiumVfxRoot.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem != null && particleSystem.IsAlive(true))
                return true;
        }

        return false;
    }

    private void ShowResultPanel()
    {
        gameObject.SetActive(true);
        SetPresentationVisible(true);
    }

    private void SetPresentationVisible(bool visible)
    {
        if (presentationCanvasGroup == null)
            return;

        presentationCanvasGroup.alpha = visible ? 1f : 0f;
        presentationCanvasGroup.interactable = visible;
        presentationCanvasGroup.blocksRaycasts = visible;
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
        if (!CanLocalPlayerMutateHostOnlyState())
        {
            gameObject.SetActive(false);
            return;
        }

        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (lobby != null)
        {
            PendingResearchSettlementService.Clear(lobby);
            DataManager.Instance.LobbyRuntimeStore.Set(lobby);
            SaveSystem.Instance?.SaveCurrentProgress();
            PublishHostSnapshotAfterLocalMutation();
        }

        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (vfxPresentationCoroutine != null)
        {
            StopCoroutine(vfxPresentationCoroutine);
            vfxPresentationCoroutine = null;
        }

        LobbyPositionModalInputBlocker.Unblock(this);

        if (blueDustiumVfxRoot != null)
            blueDustiumVfxRoot.gameObject.SetActive(false);
    }

    private static bool CanLocalPlayerMutateHostOnlyState()
    {
        SteamLobbySharedStateSynchronizer synchronizer =
            SteamLobbySharedStateSynchronizer.Instance;
        return synchronizer == null ||
               synchronizer.CanLocalPlayerMutateHostOnlyState();
    }

    private static void PublishHostSnapshotAfterLocalMutation()
    {
        SteamLobbySharedStateSynchronizer.Instance
            ?.PublishHostSnapshotAfterLocalMutation();
    }
}
