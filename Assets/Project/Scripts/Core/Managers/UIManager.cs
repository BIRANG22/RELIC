using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [Header("References")]
    [SerializeField] private Canvas mainCanvas;

    [Header("UI Prefabs")]
    [SerializeField] private GameObject optionPanelPrefab;

    private static readonly Vector3 OptionPanelDefaultScale = Vector3.one;

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

        optionPanelInstance = Instantiate(optionPanelPrefab, mainCanvas.transform, false);
        ApplyOptionPanelScale();
        optionPanelInstance.SetActive(false);
    }

    public void ShowOption()
    {
        if (optionPanelInstance == null)
            CreateOptionPanel();

        if (optionPanelInstance != null)
        {
            ApplyOptionPanelScale();
            optionPanelInstance.SetActive(true);
        }
    }

    private void ApplyOptionPanelScale()
    {
        if (optionPanelInstance == null)
            return;

        Transform optionTransform = optionPanelInstance.transform;
        optionTransform.localScale = OptionPanelDefaultScale;

        if (optionTransform is RectTransform rectTransform)
            rectTransform.localScale = OptionPanelDefaultScale;
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