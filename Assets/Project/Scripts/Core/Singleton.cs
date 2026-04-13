using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected bool IsDuplicateInstance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            IsDuplicateInstance = true;
            Destroy(gameObject);
            return;
        }

        Instance = this as T;
        DontDestroyOnLoad(gameObject);
        IsDuplicateInstance = false;
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
