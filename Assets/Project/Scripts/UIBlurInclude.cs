using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 블러 배경 캡처에 포함하고 싶은 UI 오브젝트에 추가하는 마커입니다.
/// 일반 UI는 기존처럼 블러에서 제외되며, 이 컴포넌트가 붙은 UI만 캡처에 포함됩니다.
/// 블러 패널이 표시되는 동안에는 원본 UI를 숨겨 캡처된 블러 이미지가 선명한 원본에 덮이지 않도록 합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class UIBlurInclude : MonoBehaviour
{
    [Header("Capture")]
    [Tooltip("체크하면 이 오브젝트의 모든 자식 UI도 함께 블러 캡처에 포함합니다.")]
    [SerializeField] private bool includeChildren = true;

    public bool IncludeChildren => includeChildren;

    private CanvasGroup blurCanvasGroup;
    private bool addedCanvasGroup;
    private bool blurHidden;

    private float originalAlpha = 1f;
    private bool originalInteractable = true;
    private bool originalBlocksRaycasts = true;

    internal void CollectCaptureTargets(List<GameObject> results)
    {
        if (results == null || !isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        if (!includeChildren)
        {
            results.Add(gameObject);
            return;
        }

        Transform[] targets = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < targets.Length; i++)
        {
            Transform target = targets[i];
            if (target != null)
                results.Add(target.gameObject);
        }
    }

    internal void BeginBlurHide()
    {
        if (blurHidden)
            return;

        blurCanvasGroup = GetComponent<CanvasGroup>();
        addedCanvasGroup = blurCanvasGroup == null;

        if (blurCanvasGroup == null)
            blurCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        originalAlpha = blurCanvasGroup.alpha;
        originalInteractable = blurCanvasGroup.interactable;
        originalBlocksRaycasts = blurCanvasGroup.blocksRaycasts;

        blurHidden = true;
        ApplyHiddenState();
    }

    internal void SetTemporarilyVisibleForCapture(bool visible)
    {
        if (!blurHidden || blurCanvasGroup == null)
            return;

        if (visible)
        {
            blurCanvasGroup.alpha = originalAlpha;
            blurCanvasGroup.interactable = originalInteractable;
            blurCanvasGroup.blocksRaycasts = originalBlocksRaycasts;
        }
        else
        {
            ApplyHiddenState();
        }
    }

    internal void EndBlurHide()
    {
        if (!blurHidden)
            return;

        if (blurCanvasGroup != null)
        {
            blurCanvasGroup.alpha = originalAlpha;
            blurCanvasGroup.interactable = originalInteractable;
            blurCanvasGroup.blocksRaycasts = originalBlocksRaycasts;
        }

        CanvasGroup groupToRemove = addedCanvasGroup ? blurCanvasGroup : null;

        blurCanvasGroup = null;
        addedCanvasGroup = false;
        blurHidden = false;

        if (groupToRemove != null)
        {
            if (Application.isPlaying)
                Destroy(groupToRemove);
            else
                DestroyImmediate(groupToRemove);
        }
    }

    private void ApplyHiddenState()
    {
        if (blurCanvasGroup == null)
            return;

        blurCanvasGroup.alpha = 0f;
        blurCanvasGroup.interactable = false;
        blurCanvasGroup.blocksRaycasts = false;
    }

    private void OnDisable()
    {
        if (blurHidden)
            EndBlurHide();
    }

    private void OnDestroy()
    {
        if (blurHidden)
            EndBlurHide();
    }
}
