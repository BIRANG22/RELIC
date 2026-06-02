using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonAnimationCoroutine : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("설정값")]
    [SerializeField] private Vector3 moveOffset = new Vector3(20f, 0f, 0f);
    [SerializeField] private Vector3 targetScale = new Vector3(1.1f, 1.1f, 1f);
    [SerializeField] private float targetRotation = 90f; // 배경 회전 각도
    [SerializeField] private float duration = 0.2f;

    [Header("참조")]
    [SerializeField] private RectTransform buttonContent;
    [SerializeField] private RectTransform backgroundImage;

    private Vector3 originPosition;
    private Vector3 originScale;
    private Coroutine animCoroutine;

    void Start()
    {
        if (buttonContent != null)
        {
            originPosition = buttonContent.anchoredPosition;
            originScale = buttonContent.localScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAnimation();
        animCoroutine = StartCoroutine(AnimateButton(originPosition + moveOffset, targetScale, targetRotation));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAnimation();
        animCoroutine = StartCoroutine(AnimateButton(originPosition, originScale, 0f));
    }

    private void StopAnimation()
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
        }
    }

    private IEnumerator AnimateButton(Vector3 targetPos, Vector3 targetScale, float targetRot)
    {
        float time = 0;
        Vector3 startPos = buttonContent.anchoredPosition;
        Vector3 startScale = buttonContent.localScale;
        Quaternion startRot = backgroundImage != null ? backgroundImage.localRotation : Quaternion.identity;
        Quaternion endRot = Quaternion.Euler(0, 0, targetRot);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // SmoothStep을 이용해 가속/감속 효과 적용 (부드럽게 마감)
            t = Mathf.SmoothStep(0f, 1f, t);

            if (buttonContent != null)
            {
                buttonContent.anchoredPosition = Vector3.Lerp(startPos, targetPos, t);
                buttonContent.localScale = Vector3.Lerp(startScale, targetScale, t);
            }

            if (backgroundImage != null)
            {
                backgroundImage.localRotation = Quaternion.Lerp(startRot, endRot, t);
            }

            yield return null;
        }

        // 마지막 최종 값 맞춰주기
        if (buttonContent != null)
        {
            buttonContent.anchoredPosition = targetPos;
            buttonContent.localScale = targetScale;
        }
        if (backgroundImage != null)
        {
            backgroundImage.localRotation = endRot;
        }
    }
}