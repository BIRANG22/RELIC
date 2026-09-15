using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Option 프리팹에 배치된 전투 연출 배속 버튼을 저장 설정과 동기화합니다.
/// </summary>
public sealed class BattlePresentationSpeedOptionButtonUI : MonoBehaviour
{
    [SerializeField] private Button speedButton;
    [SerializeField] private TextMeshProUGUI speedLabel;

    private void OnEnable()
    {
        if (speedButton != null && speedLabel != null)
            Initialize(speedButton, speedLabel);
    }

    private void OnDisable()
    {
        if (speedButton != null)
            speedButton.onClick.RemoveListener(SelectNextSpeed);
    }

    public void Initialize(Button button, TextMeshProUGUI label)
    {
        if (speedButton != null)
            speedButton.onClick.RemoveListener(SelectNextSpeed);

        speedButton = button;
        speedLabel = label;

        if (speedButton != null)
        {
            speedButton.onClick.RemoveListener(SelectNextSpeed);
            speedButton.onClick.AddListener(SelectNextSpeed);
        }

        RefreshLabel();
    }

    private void SelectNextSpeed()
    {
        int nextIndex = (BattlePresentationSpeedSettings.CurrentIndex + 1)
            % BattlePresentationSpeedSettings.OptionCount;
        BattlePresentationSpeedSettings.SetSelectedIndex(nextIndex);
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (speedLabel != null)
            speedLabel.text = $"{BattlePresentationSpeedSettings.CurrentMultiplier:0.0}x";
    }
}
