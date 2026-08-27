using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MenuPanelTextRefresher : MonoBehaviour
{
    private Coroutine refreshRoutine;

    private void OnEnable()
    {
        RefreshNow();
        RefreshNextFrame();
    }

    private void OnDisable()
    {
        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }

    public void RefreshNow()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
            RefreshText(texts[i]);

        if (transform is RectTransform rectTransform)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        Canvas.ForceUpdateCanvases();
    }

    public void RefreshNextFrame()
    {
        if (!isActiveAndEnabled)
            return;

        if (refreshRoutine != null)
            StopCoroutine(refreshRoutine);

        refreshRoutine = StartCoroutine(RefreshDelayed());
    }

    private IEnumerator RefreshDelayed()
    {
        yield return null;
        RefreshNow();
        yield return null;
        RefreshNow();
        refreshRoutine = null;
    }

    private static void RefreshText(TMP_Text text)
    {
        if (text == null)
            return;

        if (text.font != null && text.font.material != null)
            text.fontSharedMaterial = text.font.material;

        text.UpdateMeshPadding();
        text.SetAllDirty();
        text.ForceMeshUpdate(true, true);
    }
}
