using UnityEngine;

public class CoroutineHost : MonoBehaviour
{
    public static CoroutineHost Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
}