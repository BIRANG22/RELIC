using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Canvas))]
public class WorldCanvasCameraSetter : MonoBehaviour
{
    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        SetCamera();
    }

    private void OnEnable()
    {
        SetCamera();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetCamera();
    }

    private void SetCamera()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogWarning("MainCamera 태그가 붙은 카메라를 못 찾음");
            return;
        }

        canvas.worldCamera = cam;
    }
}