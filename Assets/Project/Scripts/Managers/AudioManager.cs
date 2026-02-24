using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    public void Initialize()
    {
        ApplySettings();
    }

    private void ApplySettings()
    {
        // Settings에서 볼륨 적용
    }
}