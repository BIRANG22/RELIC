using System;
using UnityEngine;

/// <summary>
/// 로비의 새 Info_Panel 구조를 보조합니다.
/// CharacterSelect/CharBtn_0~4의 실제 선택, 호버, Idle/Battle Idle 전환은
/// CharPick/CharBtn이 담당하고 이 컴포넌트는 패널 활성 상태와 런타임 갱신만 연결합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyInfoPanelUI : MonoBehaviour
{
    private const string InfoPanelName = "Info_Panel";

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Character Select")]
    [Tooltip("Info_Panel/CharacterSelect. 비워두면 이름으로 자동 연결합니다.")]
    [SerializeField] private Transform characterSelectRoot;

    private CharPick characterPicker;

    private void Awake()
    {
        ResolveReferences();
        EnsureCharacterSelectActive();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureCharacterSelectActive();
        RefreshCharacterData();
    }

    /// <summary>
    /// 현재 PartyRuntimeStore 상태를 새 CharacterSelect UI에 다시 반영합니다.
    /// </summary>
    public void RefreshCharacterData()
    {
        ResolveReferences();
        EnsureCharacterSelectActive();
        characterPicker?.RefreshFromPartyRuntime();
    }

    public static void RefreshAll()
    {
        LobbyInfoPanelUI[] panels = FindObjectsByType<LobbyInfoPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
                panels[i].RefreshCharacterData();
        }
    }

    private void ResolveReferences()
    {
        GameObject root = ResolvePanelRoot();
        if (root == null)
            return;

        if (characterSelectRoot == null || !characterSelectRoot.IsChildOf(root.transform))
            characterSelectRoot = FindChildRecursive(root.transform, "CharacterSelect");

        if (characterSelectRoot != null)
        {
            characterPicker = characterSelectRoot.GetComponent<CharPick>()
                ?? characterSelectRoot.GetComponentInChildren<CharPick>(true);
        }
    }

    private void EnsureCharacterSelectActive()
    {
        if (characterSelectRoot != null && !characterSelectRoot.gameObject.activeSelf)
            characterSelectRoot.gameObject.SetActive(true);
    }

    private GameObject ResolvePanelRoot()
    {
        if (panelRoot != null && string.Equals(panelRoot.name, InfoPanelName, StringComparison.OrdinalIgnoreCase))
            return panelRoot;

        if (string.Equals(gameObject.name, InfoPanelName, StringComparison.OrdinalIgnoreCase))
        {
            panelRoot = gameObject;
            return panelRoot;
        }

        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, InfoPanelName);
            if (found == null)
                continue;

            panelRoot = found.gameObject;
            return panelRoot;
        }

        return panelRoot;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (string.Equals(root.name, targetName, StringComparison.OrdinalIgnoreCase))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }
}
