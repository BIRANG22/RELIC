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
    private const string InventoryButtonName = "Inventory";
    private const string PositionButtonName = "TestPosition";

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

            if (objects.Any(item =>
                    item.name == InventoryButtonName &&
                    item.GetComponentInChildren<Button>(true) != null))
            {
                return;
            }

            GameObject panel = objects.FirstOrDefault(item => item.name == InventoryPanelName);
            GameObject positionButton = objects.FirstOrDefault(item =>
                item.name == PositionButtonName &&
                item.GetComponentInChildren<Button>(true) != null);

            // 해당 씬 구성이 아직 준비되지 않았거나 이름이 변경된 경우에는
            // 자동 설치를 시도하지 않고 조용히 종료합니다.
            if (panel == null || positionButton == null)
                return;

            GameObject inventoryButton = UnityEngine.Object.Instantiate(
                positionButton,
                positionButton.transform.parent);
            inventoryButton.name = InventoryButtonName;

            RectTransform buttonRect = inventoryButton.GetComponent<RectTransform>();
            if (buttonRect != null)
                buttonRect.anchoredPosition += Vector2.right * (Mathf.Max(80f, buttonRect.rect.width) + 20f);

            MonoBehaviour[] behaviours = inventoryButton.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour is UIBehaviour || behaviour is UIPanelButton)
                    continue;

                UnityEngine.Object.DestroyImmediate(behaviour);
            }

            Button button = inventoryButton.GetComponentInChildren<Button>(true);
            UIPanelButton panelButton = button.GetComponent<UIPanelButton>();
            if (panelButton == null)
                panelButton = button.gameObject.AddComponent<UIPanelButton>();

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panel.SetActive(true);
            panelRect.anchoredPosition += new Vector2(0f, 1080f);
            panelButton.ConfigurePanelMove(panelRect, new Vector2(0f, -1080f));

            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, panelButton.MovePanel);

            TMPro.TMP_Text[] labels = inventoryButton.GetComponentsInChildren<TMPro.TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
                labels[i].text = "Inventory";

            EditorUtility.SetDirty(inventoryButton);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(lobby);
            EditorSceneManager.SaveScene(lobby);
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
