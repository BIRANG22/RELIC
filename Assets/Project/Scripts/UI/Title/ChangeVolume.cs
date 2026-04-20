using UnityEngine;
using UnityEngine.UI;

public enum VolumeType
{
    Master,
    BGM,
    SFX
}

public class ChangeVolume : MonoBehaviour
{
    [SerializeField] private VolumeType volumeType;
    [SerializeField] private Slider slider;

    private void Start()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        float value = GetCurrentVolume();
        slider.value = value;

        slider.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDestroy()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnValueChanged(float value)
    {
        ApplyVolume(value);
        Settings.Instance.Save();
    }

    private void ApplyVolume(float value)
    {
        switch (volumeType)
        {
            case VolumeType.Master:
                AudioManager.Instance.SetMasterVolume(value);
                break;

            case VolumeType.BGM:
                AudioManager.Instance.SetBgmVolume(value);
                break;

            case VolumeType.SFX:
                AudioManager.Instance.SetSfxVolume(value);
                break;
        }
    }

    private float GetCurrentVolume()
    {
        switch (volumeType)
        {
            case VolumeType.Master:
                return Settings.Instance.MasterVolume;

            case VolumeType.BGM:
                return Settings.Instance.BGMVolume;

            case VolumeType.SFX:
                return Settings.Instance.SFXVolume;
        }

        return 1f;
    }
}