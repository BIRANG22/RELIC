using UnityEngine;
using UnityEngine.UI;

public sealed class ChangeBrightness : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private void Start()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(GetCurrentBrightness());
        slider.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDestroy()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnValueChanged(float value)
    {
        float brightness = Mathf.Clamp01(value);

        if (Settings.Instance != null)
        {
            Settings.Instance.Brightness = brightness;
            Settings.Instance.Save();
        }

        GameBrightnessManager.ApplyBrightness(brightness);
    }

    private static float GetCurrentBrightness()
    {
        return Settings.Instance != null
            ? Mathf.Clamp01(Settings.Instance.Brightness)
            : 0.5f;
    }
}
