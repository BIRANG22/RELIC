using UnityEngine;

public class TitleUnavailableButton : MonoBehaviour
{
    [Header("Warning")]
    [SerializeField] private TitleWarningUI warningUI;
    [SerializeField] private string message = "아직 준비되지 않았습니다.";

    public void ShowWarning()
    {
        TitleWarningUI targetWarningUI = GetWarningUI();

        if (targetWarningUI == null)
        {
            Debug.LogWarning("[TitleUnavailableButton] TitleWarningUI is not assigned or found in the scene.");
            return;
        }

        targetWarningUI.Show(message);
    }

    private TitleWarningUI GetWarningUI()
    {
        if (warningUI != null)
            return warningUI;

        if (TitleWarningUI.Instance != null)
        {
            warningUI = TitleWarningUI.Instance;
            return warningUI;
        }

        warningUI = FindFirstObjectByType<TitleWarningUI>(FindObjectsInactive.Include);
        return warningUI;
    }
}
