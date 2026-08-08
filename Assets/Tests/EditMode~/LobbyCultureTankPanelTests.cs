using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LobbyCultureTankPanelTests
{
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";

    [Test]
    public void LobbyScene_BindsThreeRowsCombineButtonAndCompletionControls()
    {
        Scene scene = EditorSceneManager.OpenPreviewScene(LobbyScenePath);
        try
        {
            LobbyCultureTankPanelPresenter presenter = Find<LobbyCultureTankPanelPresenter>(scene);
            Assert.That(presenter, Is.Not.Null);
            typeof(LobbyCultureTankPanelPresenter).GetMethod("BindSceneObjects", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(presenter, null);
            SerializedObject serialized = new(presenter);
            Assert.That(serialized.FindProperty("rows").arraySize, Is.EqualTo(3));
            Assert.That(serialized.FindProperty("combineButton").objectReferenceValue, Is.TypeOf<Button>());
            Assert.That(serialized.FindProperty("completionRoot").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("completionButton").objectReferenceValue, Is.TypeOf<Button>());
            Assert.That(serialized.FindProperty("completionIcon").objectReferenceValue, Is.TypeOf<Image>());
            Assert.That(serialized.FindProperty("completionIcon").objectReferenceValue.name, Is.EqualTo("icon"));
            Assert.That(serialized.FindProperty("inventoryItemRoot").objectReferenceValue.name, Is.EqualTo("SlotRoot"));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [Test]
    public void CombineButton_UsesInspectorReferenceAndSurvivesHierarchyRename()
    {
        Scene scene = EditorSceneManager.OpenPreviewScene(LobbyScenePath);
        try
        {
            LobbyCultureTankPanelPresenter presenter = Find<LobbyCultureTankPanelPresenter>(scene);
            Assert.That(presenter, Is.Not.Null);

            SerializedObject serialized = new(presenter);
            Button referencedButton = serialized.FindProperty("combineButton").objectReferenceValue as Button;
            Assert.That(referencedButton, Is.Not.Null);
            Assert.That(referencedButton.name, Is.EqualTo("MixButton"));

            referencedButton.name = "RenamedCombineButton";
            referencedButton.transform.parent.name = "RenamedMixRoot";
            typeof(LobbyCultureTankPanelPresenter)
                .GetMethod("BindSceneObjects", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(presenter, null);

            serialized.Update();
            Assert.That(serialized.FindProperty("combineButton").objectReferenceValue, Is.SameAs(referencedButton));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [Test]
    public void CompletionRoot_RemainsVisibleBeforeAndAfterCombination()
    {
        Assert.That(LobbyCultureTankPanelPresenter.ShouldShowCompletionRoot(false), Is.True);
        Assert.That(LobbyCultureTankPanelPresenter.ShouldShowCompletionRoot(true), Is.True);
    }

    [Test]
    public void LobbyScene_CultureTankOwnsEightBagStyleSelectionSlotsWithoutBagActions()
    {
        Scene scene = EditorSceneManager.OpenPreviewScene(LobbyScenePath);
        try
        {
            LobbyCultureTankPanelPresenter presenter = Find<LobbyCultureTankPanelPresenter>(scene);
            Assert.That(presenter, Is.Not.Null);

            Transform inventory = FindChild(presenter.transform, "Inventory");
            Assert.That(inventory, Is.Not.Null);

            Transform slotRoot = FindChild(inventory, "SlotRoot");
            Assert.That(slotRoot, Is.Not.Null);
            Assert.That(slotRoot.GetComponentsInChildren<BattleBagItemSlotUI>(true), Has.Length.EqualTo(8));
            Assert.That(inventory.GetComponentInChildren<BattleBagPanelUI>(true), Is.Null);
            Assert.That(FindChild(inventory, "TooltipPanel"), Is.Null);
            Assert.That(FindChild(inventory, "DiscardButton"), Is.Null);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [TestCase(false, true, false, false)]
    [TestCase(true, false, false, false)]
    [TestCase(true, true, true, false)]
    [TestCase(true, true, false, true)]
    public void InventoryItemSelection_RequiresSelectedRowMutationAuthorityAndNoCompletion(
        bool hasSelectedRow,
        bool canMutate,
        bool hasCompletedCombination,
        bool expected)
    {
        Assert.That(
            LobbyCultureTankPanelPresenter.CanSelectInventoryItem(
                hasSelectedRow,
                canMutate,
                hasCompletedCombination),
            Is.EqualTo(expected));
    }

    [Test]
    public void CultureTankRows_UseOneRootButtonAndRootClickSelectsEachRow()
    {
        Scene scene = EditorSceneManager.OpenPreviewScene(LobbyScenePath);
        try
        {
            LobbyCultureTankPanelPresenter presenter = Find<LobbyCultureTankPanelPresenter>(scene);
            Assert.That(presenter, Is.Not.Null);

            MethodInfo bindSceneObjects = typeof(LobbyCultureTankPanelPresenter)
                .GetMethod("BindSceneObjects", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo bindButtons = typeof(LobbyCultureTankPanelPresenter)
                .GetMethod("BindButtons", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo selectedSlotIndex = typeof(LobbyCultureTankPanelPresenter)
                .GetField("selectedSlotIndex", BindingFlags.Instance | BindingFlags.NonPublic);

            bindSceneObjects?.Invoke(presenter, null);
            bindButtons?.Invoke(presenter, null);

            for (int i = 0; i < 3; i++)
            {
                Transform row = FindChild(presenter.transform, $"CultureTankRow_{i + 1}");
                Assert.That(row, Is.Not.Null);
                Assert.That(row.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(1));
                Assert.That(row.GetComponent<Button>(), Is.Not.Null);
                Assert.That(row.GetComponent<Image>(), Is.Not.Null);

                row.GetComponent<Button>().onClick.Invoke();

                Assert.That(selectedSlotIndex?.GetValue(presenter), Is.EqualTo(i));
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [Test]
    public void BindSceneObjects_ReplacesDestroyedUnityObjectReference()
    {
        Scene scene = EditorSceneManager.OpenPreviewScene(LobbyScenePath);
        GameObject destroyed = new("DestroyedCompletionReference");
        try
        {
            LobbyCultureTankPanelPresenter presenter = Find<LobbyCultureTankPanelPresenter>(scene);
            FieldInfo field = typeof(LobbyCultureTankPanelPresenter).GetField("completionRoot", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(presenter, destroyed);
            Object.DestroyImmediate(destroyed);

            typeof(LobbyCultureTankPanelPresenter).GetMethod("BindSceneObjects", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(presenter, null);

            GameObject rebound = field.GetValue(presenter) as GameObject;
            Assert.That(rebound, Is.Not.Null);
            Assert.That(rebound.name, Is.EqualTo("completion"));
        }
        finally
        {
            if (destroyed != null) Object.DestroyImmediate(destroyed);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [Test]
    public void CultureTankController_WorldClickIsDisabledByDefault()
    {
        FieldInfo field = typeof(LobbyCultureTankController).GetField("allowWorldInteraction", BindingFlags.Instance | BindingFlags.NonPublic);
        GameObject go = new("CultureTank_Test"); LobbyCultureTankController controller = go.AddComponent<LobbyCultureTankController>();
        Assert.That(field.GetValue(controller), Is.False);
        Object.DestroyImmediate(go);
    }

    private static T Find<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        { T found = root.GetComponentInChildren<T>(true); if (found != null) return found; }
        return null;
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == childName) return child;
        return null;
    }
}
