using System.Collections;
using UnityEngine;

public class TitleManager : MonoBehaviour
{
    [Header("Logo")]
    [SerializeField] private GameObject onLogo;
    [SerializeField] private GameObject offLogo;

    [Header("Blink Option")]
    [SerializeField] private float blinkOffDuration = 0.08f;

    private Coroutine blinkCoroutine;

    private void Start()
    {
        RefreshLogoDefaultState();
    }

    public void OnClickLogoArea()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        blinkCoroutine = StartCoroutine(BlinkLogoCoroutine());
    }

    private IEnumerator BlinkLogoCoroutine()
    {
        if (offLogo != null)
        {
            offLogo.SetActive(true);
        }

        if (onLogo != null)
        {
            onLogo.SetActive(false);
        }

        yield return new WaitForSeconds(blinkOffDuration);

        if (offLogo != null)
        {
            offLogo.SetActive(true);
        }

        if (onLogo != null)
        {
            onLogo.SetActive(true);
        }

        blinkCoroutine = null;
    }

    private void RefreshLogoDefaultState()
    {
        if (offLogo != null)
        {
            offLogo.SetActive(true);
        }

        if (onLogo != null)
        {
            onLogo.SetActive(true);
        }
    }
}