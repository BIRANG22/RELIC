using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [Header("References")]
    [SerializeField] private Canvas mainCanvas;

    [Header("UI Prefabs")]
    [SerializeField] private GameObject optionPanelPrefab;

    private GameObject optionPanelInstance;

    protected override void Awake()
    {
        base.Awake();

        if (IsDuplicateInstance)
            return;
    }

    private void Start()
    {
        CreateOptionPanel();
        HideAll();
    }

    private void CreateOptionPanel()
    {
        if (optionPanelInstance != null)
            return;

        if (mainCanvas == null)
        {
            Debug.LogError("[UIManager] MainCanvas is not assigned.");
            return;
        }

        if (optionPanelPrefab == null)
        {
            Debug.LogError("[UIManager] OptionPanelPrefab is not assigned.");
            return;
        }

        optionPanelInstance = Instantiate(optionPanelPrefab, mainCanvas.transform);
        optionPanelInstance.SetActive(false);
    }

    public void ShowOption()
    {
        if (optionPanelInstance == null)
            CreateOptionPanel();

        if (optionPanelInstance != null)
            optionPanelInstance.SetActive(true);
    }

    public void HideOption()
    {
        if (optionPanelInstance != null)
            optionPanelInstance.SetActive(false);
    }

    public void HideAll()
    {
        HideOption();
    }
}