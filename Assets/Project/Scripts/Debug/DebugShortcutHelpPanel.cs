using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DebugShortcutHelpPanel : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
    [SerializeField] private KeyCode resetQuestKey = KeyCode.Backspace;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text shortcutText;

    public static DebugShortcutHelpPanel Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (shortcutText != null)
            shortcutText.text = BuildShortcutText();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            TogglePanel();

        if (IsQuestResetPressed())
            ResetQuestProgress();
    }

    private bool IsQuestResetPressed()
    {
        return Input.GetKeyDown(resetQuestKey) &&
               (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
    }

    private void TogglePanel()
    {
        if (panelRoot == null)
        {
            Debug.LogWarning("[DebugShortcutHelpPanel] Scene-placed panel root is missing.", this);
            return;
        }

        panelRoot.SetActive(!panelRoot.activeSelf);
    }

    private static string BuildShortcutText()
    {
        var lines = new List<string>
        {
            "[전역/디버그]",
            "]  전체 진행 초기화",
            ";  획득 기록 보기",
            "'  전체 기록 공개",
            "- / =  푸른 더스티움 -100 / +100",
            "Ctrl + Backspace  퀘스트 초기화",
            "F12  런타임 데이터 로그 출력",
            "",
            "[로비]",
            "Esc  뒤로가기 / 패널 닫기",
            "1 / 2 / 3  파티 슬롯 또는 캐릭터 슬롯 선택",
            "Tab  스테이지 패널 / 스킬-룬 영역 전환",
            "Space  스테이지 선택 또는 출전",
            "A / D, ← / →  캐릭터 또는 스테이지 이동",
            "F  현재 캐릭터 버튼 실행",
            "",
            "[전투]",
            "Space  턴 진행",
            "Tab  전투방 정보 패널",
            "1 / 2 / 3  전투방 슬롯 선택",
            "A / D  타임라인 이동",
            "Q  타임라인 디버그",
            "W / S / F  스킬 목록 이동/선택",
            "Esc / 우클릭  타겟팅 취소",
            "",
            "[VFX/애니메이션 디버그]",
            "F9  배틀 이펙트 디버그 창",
            "K  모든 몬스터 처치",
            "1~6, Z  유닛 애니메이션 디버그",
            "F2  VFX 워크벤치 패널",
            "F1  VFX 도움말",
            "Space / Delete / C / R  VFX 재생, 삭제, 반복",
            "[ / ]  이전/다음 VFX",
            "U / Tab / G / F  VFX 유닛, 렌더, 플립, 카메라"
        };

        return string.Join("\n", lines);
    }

    private static void ResetQuestProgress()
    {
        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (lobby == null)
        {
            Debug.LogWarning("[DebugShortcutHelpPanel] Lobby runtime data is missing. Quest progress was not reset.");
            return;
        }

        lobby.TutorialProgress = LobbyTutorialProgress.NotStarted;

        if (SaveSystem.Instance != null && !SaveSystem.Instance.SaveCurrentProgress())
        {
            Debug.LogWarning("[DebugShortcutHelpPanel] Failed to save quest reset progress.");
        }

        LobbyQuestManager.Instance?.Refresh();
        Debug.Log("[DebugShortcutHelpPanel] Quest progress reset to the beginning.");
    }
}
