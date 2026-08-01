using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LobbyCultureTankPanelTests
{
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";

    [TestCase(LobbyCultureTankPanelState.Empty, false)]
    [TestCase(LobbyCultureTankPanelState.MissingData, false)]
    [TestCase(LobbyCultureTankPanelState.Running, true)]
    [TestCase(LobbyCultureTankPanelState.Completed, true)]
    public void RowItemIconVisibility_FollowsResearchState(
        LobbyCultureTankPanelState state,
        bool expected)
    {
        Assert.That(
            LobbyCultureTankPanelPresenter.ShouldShowRowItemIcon(state),
            Is.EqualTo(expected));
    }

    [Test]
    public void LobbyScene_CultureTankRowsCreateDedicatedItemIconBindings()
    {
        Scene scene = EditorSceneManager.OpenPreviewScene(LobbyScenePath);

        try
        {
            LobbyCultureTankPanelPresenter presenter =
                FindComponentInScene<LobbyCultureTankPanelPresenter>(scene);
            Assert.That(presenter, Is.Not.Null);

            MethodInfo bind = typeof(LobbyCultureTankPanelPresenter).GetMethod(
                "BindSceneObjects",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bind, Is.Not.Null);
            bind.Invoke(presenter, null);

            Transform[] children = presenter.GetComponentsInChildren<Transform>(true);
            int iconCount = 0;
            foreach (Transform child in children)
            {
                if (child.name == "ItemIcon" && child.GetComponent<Image>() != null)
                    iconCount++;
            }

            Assert.That(iconCount, Is.EqualTo(3));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [Test]
    public void LobbyScene_CultureTankPanelBindsEightInternalInventorySlots()
    {
        Scene scene = EditorSceneManager.OpenPreviewScene(LobbyScenePath);

        try
        {
            LobbyCultureTankPanelPresenter presenter =
                FindComponentInScene<LobbyCultureTankPanelPresenter>(scene);
            Assert.That(presenter, Is.Not.Null);

            var serialized = new SerializedObject(presenter);
            Transform inventoryItemRoot =
                serialized.FindProperty("inventoryItemRoot").objectReferenceValue as Transform;

            Assert.That(inventoryItemRoot, Is.Not.Null);
            Assert.That(inventoryItemRoot.name, Is.EqualTo("item"));
            Assert.That(inventoryItemRoot.childCount, Is.EqualTo(8));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [Test]
    public void CultureTankController_ExposesPanelItemResearchEntryPoint()
    {
        MethodInfo method = typeof(LobbyCultureTankController).GetMethod(
            "TryStartResearchFromPanel",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(method, Is.Not.Null);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(bool)));
        Assert.That(method.GetParameters().Length, Is.EqualTo(1));
        Assert.That(method.GetParameters()[0].ParameterType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void PanelItemResearch_IsNotBlockedByWorldMenuClickGuard()
    {
        DataManager createdDataManager = null;
        if (DataManager.Instance == null)
            createdDataManager = new GameObject("DataManager_PanelResearchTest").AddComponent<DataManager>();

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();
        List<string> originalItems = new(lobby.BagItemIds);
        List<CultureTankResearchRuntimeData> originalResearches = new(lobby.CultureTankResearches);
        GameObject menuPanel = new("MenuPanel");
        GameObject tankObject = new("CultureTank1");
        LobbyCultureTankController controller = tankObject.AddComponent<LobbyCultureTankController>();

        try
        {
            lobby.BagItemIds.Clear();
            lobby.BagItemIds.Add("panel-item");
            lobby.CultureTankResearches.Clear();
            menuPanel.SetActive(true);

            bool started = controller.TryStartResearchFromPanel("panel-item");

            Assert.That(started, Is.True);
            Assert.That(lobby.BagItemIds, Is.Empty);
            Assert.That(lobby.CultureTankResearches.Count, Is.EqualTo(1));
            Assert.That(lobby.CultureTankResearches[0].ItemId, Is.EqualTo("panel-item"));
        }
        finally
        {
            lobby.BagItemIds = originalItems;
            lobby.CultureTankResearches = originalResearches;
            Object.DestroyImmediate(tankObject);
            Object.DestroyImmediate(menuPanel);
            if (createdDataManager != null)
                Object.DestroyImmediate(createdDataManager.gameObject);
        }
    }

    [Test]
    public void CultureTankController_ExposesSeparatePanelNameAndStateText()
    {
        MethodInfo nameMethod = typeof(LobbyCultureTankController).GetMethod("GetPanelName");
        MethodInfo stateMethod = typeof(LobbyCultureTankController).GetMethod("GetPanelStateText");

        Assert.That(nameMethod, Is.Not.Null);
        Assert.That(stateMethod, Is.Not.Null);
        Assert.That(nameMethod.ReturnType, Is.EqualTo(typeof(string)));
        Assert.That(stateMethod.ReturnType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void CultureTankController_ExposesPanelInteractionEntryPoint()
    {
        MethodInfo interact = typeof(LobbyCultureTankController).GetMethod(
            "Interact",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(interact, Is.Not.Null);
        Assert.That(interact.ReturnType, Is.EqualTo(typeof(void)));
        Assert.That(interact.GetParameters(), Is.Empty);
    }

    [Test]
    public void ClaimFeedback_InactiveTankDoesNotCreateCoroutineFeedbackObject()
    {
        GameObject tankObject = new("CultureTank_InactiveClaimFeedbackTest");
        tankObject.SetActive(false);
        LobbyCultureTankController controller =
            tankObject.AddComponent<LobbyCultureTankController>();
        MethodInfo showFeedback = typeof(LobbyCultureTankController).GetMethod(
            "ShowClaimFeedback",
            BindingFlags.Instance | BindingFlags.NonPublic);

        try
        {
            Assert.That(showFeedback, Is.Not.Null);

            showFeedback.Invoke(controller, null);

            Assert.That(tankObject.transform.Find("ResearchClaimFeedbackText"), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(tankObject);
        }
    }

    [Test]
    public void CultureTankController_WorldClickIsDisabledByDefault()
    {
        FieldInfo field = typeof(LobbyCultureTankController).GetField(
            "allowWorldInteraction",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        var controller = new UnityEngine.GameObject("CultureTank_Test")
            .AddComponent<LobbyCultureTankController>();

        try
        {
            Assert.That(field.GetValue(controller), Is.EqualTo(false));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(controller.gameObject);
        }
    }

    [Test]
    public void OpenItemSelection_EmptyBag_OpensBagPanelInSelectionMode()
    {
        GameObject bagObject = new GameObject("BagPanel_Test");
        BattleBagPanelUI bagPanel = bagObject.AddComponent<BattleBagPanelUI>();
        bagObject.SetActive(false);

        GameObject tankObject = new GameObject("CultureTank_Test");
        LobbyCultureTankController controller =
            tankObject.AddComponent<LobbyCultureTankController>();

        FieldInfo bagPanelField = typeof(LobbyCultureTankController).GetField(
            "bagPanel",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo openItemSelection = typeof(LobbyCultureTankController).GetMethod(
            "OpenItemSelection",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo selectionModeField = typeof(BattleBagPanelUI).GetField(
            "isItemSelectionMode",
            BindingFlags.Instance | BindingFlags.NonPublic);

        try
        {
            Assert.That(bagPanelField, Is.Not.Null);
            Assert.That(openItemSelection, Is.Not.Null);
            Assert.That(selectionModeField, Is.Not.Null);

            bagPanelField.SetValue(controller, bagPanel);
            var lobby = new LobbyRuntimeData
            {
                BagItemIds = new List<string>()
            };

            openItemSelection.Invoke(controller, new object[] { lobby });

            Assert.That(bagObject.activeSelf, Is.True);
            Assert.That(selectionModeField.GetValue(bagPanel), Is.EqualTo(true));
        }
        finally
        {
            LobbyPositionModalInputBlocker.Unblock(controller);
            Object.DestroyImmediate(tankObject);
            Object.DestroyImmediate(bagObject);
        }
    }

    [Test]
    public void ResearcherInteraction_OpensCultureTankPanel()
    {
        MethodInfo openPanel = typeof(LobbyResearcherCultureTankInteraction).GetMethod(
            "OpenPanel",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(openPanel, Is.Not.Null);
        Assert.That(openPanel.ReturnType, Is.EqualTo(typeof(void)));
        Assert.That(openPanel.GetParameters(), Is.Empty);
    }

    [Test]
    public void CultureTankController_PanelLabelUsesReadableKoreanStateText()
    {
        DataManager createdDataManager = null;
        GameObject dataObject = null;
        if (DataManager.Instance == null)
        {
            dataObject = new GameObject("DataManager_Test");
            createdDataManager = dataObject.AddComponent<DataManager>();
        }

        GameObject tankObject = new GameObject("CultureTank_LabelTest");
        LobbyCultureTankController controller =
            tankObject.AddComponent<LobbyCultureTankController>();

        try
        {
            string label = controller.GetPanelLabel();

            Assert.That(label, Does.Contain("배양조"));
            Assert.That(label, Does.Contain("비어 있음"));
            Assert.That(label, Does.Not.Contain("鍮"));
            Assert.That(label, Does.Not.Contain("諛"));
        }
        finally
        {
            Object.DestroyImmediate(tankObject);

            if (createdDataManager != null)
                Object.DestroyImmediate(createdDataManager.gameObject);
            else if (dataObject != null)
                Object.DestroyImmediate(dataObject);
        }
    }

    [Test]
    public void ResearcherInteraction_UsesMouseReleaseInsteadOfMousePress()
    {
        string sourcePath = System.IO.Path.Combine(
            UnityEngine.Application.dataPath,
            "Project",
            "Scripts",
            "Gameplay",
            "Scene",
            "Lobby",
            "LobbyResearcherCultureTankInteraction.cs");
        string source = System.IO.File.ReadAllText(sourcePath);

        Assert.That(source, Does.Contain("private void OnMouseUpAsButton()"));
        Assert.That(source, Does.Not.Contain("private void OnMouseDown()"));
    }

    [Test]
    public void AutoBinder_SourceContainsResearcherBindingRule()
    {
        string sourcePath = System.IO.Path.Combine(
            UnityEngine.Application.dataPath,
            "Project",
            "Scripts",
            "Gameplay",
            "Scene",
            "Lobby",
            "LobbyCultureTankAutoBinder.cs");
        string source = System.IO.File.ReadAllText(sourcePath);

        Assert.That(source, Does.Contain("Researcher"));
        Assert.That(source, Does.Contain("LobbyResearcherCultureTankInteraction"));
    }

    private static T FindComponentInScene<T>(Scene scene)
        where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }
}
