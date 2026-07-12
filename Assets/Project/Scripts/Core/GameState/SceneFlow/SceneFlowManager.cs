using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : Singleton<SceneFlowManager>
{
    [Header("Scene Transition")]
    [SerializeField] private bool useSceneTransition = true;
    [SerializeField] private bool playTransitionOnFirstLoad = false;

    public string CurrentScene => SceneManager.GetActiveScene().name;
    public bool IsLoading => isLoading;

    private bool isLoading;
    private bool hasLoadedSceneOnce;

    protected override void Awake()
    {
        base.Awake();

        if (IsDuplicateInstance)
        {
            return;
        }
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
            hasLoadedSceneOnce = true;
            return;
        }

        isLoading = true;

        Debug.Log($"[SceneFlowManager] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);

        hasLoadedSceneOnce = true;
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
            hasLoadedSceneOnce = true;
            return;
        }

        isLoading = true;

        CanvasMaterialSceneTransition transition = GetSceneTransition();
        bool shouldPlayTransition = ShouldPlayTransition(transition);

        if (shouldPlayTransition)
        {
            await transition.PlayCloseAsync();
            await Task.Yield();
        }

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);

        if (loadOperation == null)
        {
            Debug.LogError($"[SceneFlowManager] Failed to load scene: {sceneName}");
            isLoading = false;
            return;
        }

        loadOperation.allowSceneActivation = true;

        while (!loadOperation.isDone)
        {
            await Task.Yield();
        }

        hasLoadedSceneOnce = true;

        if (shouldPlayTransition)
        {
            await transition.HoldClosedAsync();
            await transition.PlayOpenAsync();
        }

        isLoading = false;
        Debug.Log($"[SceneFlowManager] Loaded scene: {sceneName}");
    }

    private CanvasMaterialSceneTransition GetSceneTransition()
    {
        if (CanvasMaterialSceneTransition.Instance != null)
        {
            return CanvasMaterialSceneTransition.Instance;
        }

        return FindFirstObjectByType<CanvasMaterialSceneTransition>(FindObjectsInactive.Include);
    }

    private bool ShouldPlayTransition(CanvasMaterialSceneTransition transition)
    {
        if (!useSceneTransition)
        {
            return false;
        }

        if (transition == null)
        {
            Debug.LogWarning("[SceneFlowManager] Scene transition is enabled, but CanvasMaterialSceneTransition was not found.");
            return false;
        }

        // Bootstrap에서 Title 씬을 직접 불러오기 때문에
        // Title -> Lobby 이동은 SceneFlowManager 기준 첫 로드로 판정됩니다.
        // 타이틀에서 시작하는 첫 씬 이동은 설정값과 관계없이 전환 효과를 재생합니다.
        if (!hasLoadedSceneOnce &&
            !playTransitionOnFirstLoad &&
            CurrentScene != SceneName.Title)
        {
            return false;
        }

        return true;
    }
}
