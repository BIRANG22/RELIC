using UnityEngine;

public class ResolutionManager : MonoBehaviour
{
    void Awake()
    {
        int width = Mathf.Min(1920, Screen.currentResolution.width);
        int height = Mathf.Min(1080, Screen.currentResolution.height);

        Screen.SetResolution(width, height, FullScreenMode.Windowed);
    }
}