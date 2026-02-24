using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.VisualScripting;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private string firstSceneName = "SampleScene";

    private IEnumerator Start()
    {
        // 1. Settings Load
        Settings.Instance.Load();

        // 2. SaveSystem Init
        SaveSystem.Instance.Initialize();

        // 3. EventBus Init
        EventBus.Instance.Initialize();

        // 4. Data Load
        DataManager.Instance.Initialize();

        // 5. Audio Init
        AudioManager.Instance.Initialize();

        // 6. Input Init
        InputManager.Instance.Initialize();

        // 7. GameManager Init
        GameManager.Instance.Initialize();

        yield return null;

        // 8. Ã¹ ¾À ·Îµå
        SceneManager.LoadScene(firstSceneName);
    }
}