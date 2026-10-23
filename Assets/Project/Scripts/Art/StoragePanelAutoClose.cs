using System;
using UnityEngine;

/// <summary>
/// When another assigned panel changes from inactive to active, requests the
/// StoragePanel's UIObjectToggleButton to play its normal hide animation.
/// Panels that were already open do not affect StoragePanel.
/// </summary>
[DisallowMultipleComponent]
public sealed class StoragePanelAutoClose : MonoBehaviour
{
    [Header("Storage Panel")]
    [SerializeField] private GameObject storagePanel;
    [Tooltip("The UIObjectToggleButton component on the button that controls StoragePanel.")]
    [SerializeField] private UIObjectToggleButton storageToggleButton;

    [Header("Other Panels (assign manually)")]
    [SerializeField] private GameObject[] otherPanels = Array.Empty<GameObject>();

    private bool[] previousActiveStates = Array.Empty<bool>();

    private void OnEnable()
    {
        CaptureStates();
    }

    private void Update()
    {
        if (previousActiveStates.Length != otherPanels.Length)
        {
            CaptureStates();
            return;
        }

        bool newlyOpened = false;
        for (int i = 0; i < otherPanels.Length; i++)
        {
            GameObject panel = otherPanels[i];
            bool active = panel != null && panel.activeInHierarchy;

            if (!previousActiveStates[i] && active && panel != storagePanel)
                newlyOpened = true;

            previousActiveStates[i] = active;
        }

        if (newlyOpened && storagePanel != null && storagePanel.activeSelf)
        {
            if (storageToggleButton != null && storageToggleButton.isActiveAndEnabled)
            {
                // Do not deactivate the panel directly: the toggle controls
                // the return movement and deactivates it after the animation.
                storageToggleButton.SetActive(false);
            }
            else
            {
                Debug.LogWarning("StoragePanelAutoClose: Assign the active StoragePanel UIObjectToggleButton in the Inspector to play the close animation.", this);
            }
        }
    }

    private void CaptureStates()
    {
        previousActiveStates = new bool[otherPanels.Length];
        for (int i = 0; i < otherPanels.Length; i++)
        {
            GameObject panel = otherPanels[i];
            previousActiveStates[i] = panel != null && panel.activeInHierarchy;
        }
    }
}
