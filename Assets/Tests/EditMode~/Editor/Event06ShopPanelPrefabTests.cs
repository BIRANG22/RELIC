#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Event06ShopPanelPrefabTests
{
    private const string ShopPanelPrefabPath = "Assets/Project/PrefabsR/RestRoom/ShopPanel.prefab";
    private const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";

    [Test]
    public void ShopPanelPrefab_ContainsRestRoomShopPanelWithRequiredBindings()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ShopPanelPrefabPath);

        try
        {
            Assert.That(root, Is.Not.Null);

            RestRoomShopPanel shopPanel = root.GetComponent<RestRoomShopPanel>();
            Assert.That(shopPanel, Is.Not.Null);

            SerializedObject serializedPanel = new(shopPanel);
            Assert.That(
                serializedPanel.FindProperty("panelRoot").objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                serializedPanel.FindProperty("contentRoot").objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                serializedPanel.FindProperty("goodsPrefab").objectReferenceValue,
                Is.Not.Null);
        }
        finally
        {
            if (root != null)
                PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [Test]
    public void BattleScene_UsesShopPanelPrefabForRestRoomAndEventRoom()
    {
        Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Additive);

        try
        {
            GameObject restRoom = FindRootChild(scene, "RestRoom");
            GameObject eventRoom = FindRootChild(scene, "EventRoom");

            Assert.That(restRoom, Is.Not.Null);
            Assert.That(eventRoom, Is.Not.Null);

            RestRoomShopPanel restShopPanel =
                restRoom.GetComponentsInChildren<RestRoomShopPanel>(true).FirstOrDefault();
            RestRoomShopPanel eventShopPanel =
                eventRoom.GetComponentsInChildren<RestRoomShopPanel>(true).FirstOrDefault();

            Assert.That(restShopPanel, Is.Not.Null);
            Assert.That(eventShopPanel, Is.Not.Null);
            Assert.That(GetPrefabAssetPath(restShopPanel.gameObject), Is.EqualTo(ShopPanelPrefabPath));
            Assert.That(GetPrefabAssetPath(eventShopPanel.gameObject), Is.EqualTo(ShopPanelPrefabPath));

            RestRoomController restRoomController = restRoom.GetComponent<RestRoomController>();
            EventRoomController eventRoomController = eventRoom.GetComponent<EventRoomController>();

            Assert.That(ReadObjectReference(restRoomController, "shopPanel"), Is.EqualTo(restShopPanel));
            Assert.That(ReadObjectReference(eventRoomController, "shopPanel"), Is.EqualTo(eventShopPanel));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void RestRoomShopPanel_OpenAndCloseApplyConfiguredAnchoredPositions()
    {
        GameObject panelObject = new("ShopPanel", typeof(RectTransform));

        try
        {
            RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
            GameObject contentObject = new("Content", typeof(RectTransform));
            contentObject.transform.SetParent(panelObject.transform, false);

            GameObject goodsObject = new(
                "Goods",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(GoodsIconItem));
            goodsObject.transform.SetParent(contentObject.transform, false);
            goodsObject.SetActive(false);

            RestRoomShopPanel shopPanel = panelObject.AddComponent<RestRoomShopPanel>();

            SerializedObject serializedPanel = new(shopPanel);
            serializedPanel.FindProperty("panelRoot").objectReferenceValue = panelObject;
            serializedPanel.FindProperty("contentRoot").objectReferenceValue = contentObject.transform;
            serializedPanel.FindProperty("goodsPrefab").objectReferenceValue =
                goodsObject.GetComponent<GoodsIconItem>();
            serializedPanel.FindProperty("slideRoot").objectReferenceValue = rectTransform;
            serializedPanel.FindProperty("openAnchoredPosition").vector2Value = Vector2.zero;
            serializedPanel.FindProperty("closedAnchoredPosition").vector2Value = new Vector2(0f, 1100f);
            serializedPanel.FindProperty("deactivateOnClose").boolValue = false;
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();

            rectTransform.anchoredPosition = new Vector2(0f, 1100f);

            shopPanel.Open();

            Assert.That(panelObject.activeSelf, Is.True);
            Assert.That(rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));

            shopPanel.Close();

            Assert.That(panelObject.activeSelf, Is.True);
            Assert.That(rectTransform.anchoredPosition, Is.EqualTo(new Vector2(0f, 1100f)));
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    private static GameObject FindRootChild(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindChildRecursive(root.transform, name);

            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);

            if (found != null)
                return found;
        }

        return null;
    }

    private static string GetPrefabAssetPath(GameObject instance)
    {
        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(instance);

        if (source == null)
            return string.Empty;

        return AssetDatabase.GetAssetPath(source);
    }

    private static Object ReadObjectReference(Object target, string propertyName)
    {
        Assert.That(target, Is.Not.Null);

        SerializedObject serializedObject = new(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        Assert.That(property, Is.Not.Null);
        return property.objectReferenceValue;
    }
}
#endif
