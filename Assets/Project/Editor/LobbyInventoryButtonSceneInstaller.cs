#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class LobbyInventoryButtonSceneInstaller
{
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";
    private const string InventoryPanelName = "InventoryPanel";
    private const string InventoryButtonName = "InventoryButton";
    private const float ClosedPanelY = 1080f;

    static LobbyInventoryButtonSceneInstaller()
    {
        EditorApplication.delayCall += InstallIfNeeded;
    }

    [MenuItem("Tools/RELIC/UI/Install Lobby Inventory Button")]
    public static void InstallIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            Scene lobby = SceneManager.GetSceneByPath(LobbyScenePath);
            if (!lobby.IsValid() || !lobby.isLoaded)
                lobby = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Additive);

            GameObject[] objects = lobby.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(item => item.gameObject)
                .ToArray();

            GameObject panel = objects.FirstOrDefault(item => item.name == InventoryPanelName);
            GameObject inventoryButtonObject = objects.FirstOrDefault(item =>
                item.name == InventoryButtonName &&
                item.GetComponentInChildren<Button>(true) != null);

            if (panel == null)
                throw new InvalidOperationException("InventoryPanel을 찾지 못했습니다.");

            if (inventoryButtonObject == null)
                throw new InvalidOperationException("InventoryButton을 찾지 못했습니다.");

            Button button = inventoryButtonObject.GetComponentInChildren<Button>(true);
            UIPanelButton panelButton = button.GetComponent<UIPanelButton>();
            if (panelButton == null)
                panelButton = button.gameObject.AddComponent<UIPanelButton>();

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            if (panelRect == null)
                throw new InvalidOperationException("InventoryPanel에 RectTransform이 없습니다.");

            // 자동 설치가 여러 번 실행돼도 위치가 누적되지 않도록 절대값으로 지정합니다.
            Vector2 closedPosition = panelRect.anchoredPosition;
            closedPosition.y = ClosedPanelY;
            panelRect.anchoredPosition = closedPosition;

            panel.SetActive(true);
            panelButton.ConfigurePanelMove(panelRect, new Vector2(0f, -ClosedPanelY));

            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, panelButton.MovePanel);

            EditorUtility.SetDirty(inventoryButtonObject);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(lobby);
            EditorSceneManager.SaveScene(lobby);

            Debug.Log("[LobbyInventoryButtonSceneInstaller] InventoryButton과 InventoryPanel 연결을 갱신했습니다.");
        }
        catch (Exception exception)
        {
            Debug.LogError("[LobbyInventoryButtonSceneInstaller] " + exception);
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
#endif
