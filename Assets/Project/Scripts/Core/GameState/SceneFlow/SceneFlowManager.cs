using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : Singleton<SceneFlowManager>
{
    public string CurrentScene => SceneManager.GetActiveScene().name;
    public bool IsLoading => isLoading;

    private bool isLoading;

    protected override void Awake()
    {
        base.Awake();

        if (IsDuplicateInstance)
            return;
    }

    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning($"[SceneFlowManager] Already loading scene. Ignored: {sceneName}");
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneFlowManager] sceneName is null or empty.");
            return;
        }

        if (CurrentScene == sceneName)
        {
            Debug.Log($"[SceneFlowManager] Scene already loaded: {sceneName}");
            return;
        }

        isLoading = true;

        Debug.Log($"[SceneFlowManager] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);

        isLoading = false;
        Debug.Log($"[SceneFlowManager] Loaded scene: {sceneName}");
    }

    public async Task LoadSceneAsync(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning($"[SceneFlowManager] Already loading scene. Ignored: {sceneName}");
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneFlowManager] sceneName is null or empty.");
            return;
        }

        if (CurrentScene == sceneName)
        {
            Debug.Log($"[SceneFlowManager] Scene already loaded: {sceneName}");
            return;
        }

        isLoading = true;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        loadOperation.allowSceneActivation = true;

        while (!loadOperation.isDone)
        {
            await Task.Yield();
        }

        isLoading = false;
    }
}