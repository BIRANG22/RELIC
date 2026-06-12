using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{

    [Header("Logo")]
    [SerializeField] private GameObject onLogo;
    [SerializeField] private GameObject offLogo;

    // 시작 시 on Logo는 켜지고, off Logo는 꺼진 상태
    private bool isLogoOn = true;

    private void Start()
    {

        RefreshLogo();
    }

    public void OnClickLogoArea()
    {
        isLogoOn = !isLogoOn;
        RefreshLogo();
    }

    private void RefreshLogo()
    {
        if (onLogo != null)
            onLogo.SetActive(isLogoOn);

        if (offLogo != null)
            offLogo.SetActive(!isLogoOn);
    }
}