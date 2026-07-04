using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class EventRoomController : MonoBehaviour
{
    [Header("Chest")]
    [SerializeField] private ChestOpenButton chestOpenButton;

    [Header("Progression")]
    [SerializeField] private GameObject nextButtonRoot;

    [Header("Background Sorting")]
    [SerializeField] private Transform backgroundRoot;
    [SerializeField] private int backgroundSortingOrder = -100;

    private bool isChestOpened;
    private Button nextButton;

    private void Awake()
    {
        EnsureReferences();
        ApplyBackgroundSorting();
        BindNextButton();
    }

    private void OnEnable()
    {
        EnsureReferences();
        ApplyBackgroundSorting();
        BindNextButton();

        isChestOpened = chestOpenButton != null && chestOpenButton.IsOpened;
        SetNextButtonVisible(isChestOpened);

        if (chestOpenButton != null)
        {
            chestOpenButton.Opened -= NotifyChestOpened;
            chestOpenButton.Opened += NotifyChestOpened;
        }
    }

    private void OnDisable()
    {
        if (chestOpenButton != null)
            chestOpenButton.Opened -= NotifyChestOpened;

        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextButtonClicked);
    }

    public void NotifyChestOpened()
    {
        isChestOpened = true;
        SetNextButtonVisible(true);
    }

    public void OnNextButtonClicked()
    {
        if (!isChestOpened)
            return;

        CompleteCurrentNode();

        BattleSceneController sceneController =
            Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);

        if (sceneController != null)
            sceneController.ReturnToMap();
        else
            Debug.LogWarning("[EventRoomController] BattleSceneController not found");
    }

    private void EnsureReferences()
    {
        if (chestOpenButton == null)
            chestOpenButton = GetComponentInChildren<ChestOpenButton>(true);

        if (backgroundRoot == null)
        {
            Transform backgroundTransform = FindChildRecursive(transform, "background");

            if (backgroundTransform != null)
                backgroundRoot = backgroundTransform;
        }

        EnsureNextButtonRoot();
    }

    private void EnsureNextButtonRoot()
    {
        if (nextButtonRoot == null)
        {
            Transform nextButtonTransform = FindChildRecursive(transform, "NextButton");

            if (nextButtonTransform != null)
                nextButtonRoot = nextButtonTransform.gameObject;
        }

        if (nextButtonRoot == null)
            return;

        if (nextButton == null || nextButton.gameObject != nextButtonRoot)
            nextButton = nextButtonRoot.GetComponent<Button>();
    }

    private void BindNextButton()
    {
        EnsureNextButtonRoot();

        if (nextButton == null)
            return;

        nextButton.onClick.RemoveListener(OnNextButtonClicked);
        nextButton.onClick.AddListener(OnNextButtonClicked);
    }

    private void SetNextButtonVisible(bool visible)
    {
        EnsureNextButtonRoot();

        if (nextButtonRoot != null)
            nextButtonRoot.SetActive(visible);
    }

    private void ApplyBackgroundSorting()
    {
        if (backgroundRoot == null)
            return;

        Renderer[] renderers = backgroundRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].sortingOrder = backgroundSortingOrder;
        }
    }

    private Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (string.Equals(root.name, targetName, System.StringComparison.Ordinal))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), targetName);

            if (result != null)
                return result;
        }

        return null;
    }

    private void CompleteCurrentNode()
    {
        if (DataManager.Instance == null)
            return;

        MapRuntimeData runtime = DataManager.Instance.MapRuntimeStore.Get();

        if (runtime == null)
            return;

        string nodeKey = runtime.CurrentNodeIndex.ToString();

        if (!runtime.ClearedMapIds.Contains(nodeKey))
            runtime.ClearedMapIds.Add(nodeKey);

        if (!runtime.VisitedMapIds.Contains(nodeKey))
            runtime.VisitedMapIds.Add(nodeKey);

        DataManager.Instance.MapRuntimeStore.Set(runtime);

        Debug.Log(
            $"[EventRoomController] Complete Node / Node:{runtime.CurrentNodeIndex} / Map:{runtime.CurrentMapId}"
        );
    }
}
