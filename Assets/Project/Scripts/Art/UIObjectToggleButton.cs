using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggles explicitly assigned GameObjects each time the UI Button is clicked.
/// Hover state does not affect target visibility.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UIObjectToggleButton : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("GameObjects to toggle. Assign them in the Inspector.")]
    [SerializeField] private GameObject[] targetObjects = System.Array.Empty<GameObject>();

    [Header("Initial State")]
    [Tooltip("Apply Initial Active State when this component first starts.")]
    [SerializeField] private bool applyInitialState = false;
    [SerializeField] private bool initialActiveState = false;

    private Button button;
    private bool isActive;

    private void Awake()
    {
        button = GetComponent<Button>();
        isActive = GetFirstTargetState();
        if (applyInitialState)
        {
            isActive = initialActiveState;
            ApplyTargetState();
        }
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();
        button.onClick.AddListener(Toggle);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(Toggle);
    }

    public void Toggle()
    {
        isActive = !isActive;
        ApplyTargetState();
    }

    public void SetActive(bool active)
    {
        isActive = active;
        ApplyTargetState();
    }

    private bool GetFirstTargetState()
    {
        foreach (GameObject target in targetObjects)
        {
            if (target != null)
                return target.activeSelf;
        }
        return false;
    }

    private void ApplyTargetState()
    {
        foreach (GameObject target in targetObjects)
        {
            if (target != null && target != gameObject && target.activeSelf != isActive)
                target.SetActive(isActive);
        }
    }
}
