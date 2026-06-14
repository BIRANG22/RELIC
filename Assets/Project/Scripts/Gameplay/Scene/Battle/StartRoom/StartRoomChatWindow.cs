using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class StartRoomChatWindow : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private TMP_Text dialogText;

    [Header("Fallback")]
    [SerializeField, TextArea(1, 3)] private string fallbackLine = "셋 중에 하나 골라. 필요하면 가져가.";

    private Action onFinished;
    private bool isOpen;
    private bool finishedInvoked;

    public void Open(string[] dialogLines, Action finishedCallback)
    {
        onFinished = finishedCallback;
        isOpen = true;
        finishedInvoked = false;

        gameObject.SetActive(true);
        ShowSingleLine(dialogLines);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClickNext();
    }

    public void OnClickNext()
    {
        if (!isOpen)
            return;

        Close();
        InvokeFinishedOnce();
    }

    public void Close()
    {
        isOpen = false;
        gameObject.SetActive(false);
    }

    private void ShowSingleLine(string[] dialogLines)
    {
        if (dialogText == null)
            return;

        dialogText.text = ResolveSingleLine(dialogLines);
    }

    private string ResolveSingleLine(string[] dialogLines)
    {
        if (dialogLines != null)
        {
            for (int i = 0; i < dialogLines.Length; i++)
            {
                string line = dialogLines[i];

                if (!string.IsNullOrWhiteSpace(line))
                    return NormalizeLine(line);
            }
        }

        return NormalizeLine(fallbackLine);
    }

    private static string NormalizeLine(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Replace("\\n", "\n");
    }

    private void InvokeFinishedOnce()
    {
        if (finishedInvoked)
            return;

        finishedInvoked = true;
        Action callback = onFinished;
        onFinished = null;
        callback?.Invoke();
    }
}
