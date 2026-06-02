using System;
using TMPro;
using UnityEngine;

public class StartRoomChatWindow : MonoBehaviour
{
    [SerializeField] private TMP_Text dialogText;

    private string[] lines;
    private int currentIndex;
    private Action onFinished;

    public void Open(string[] dialogLines, Action finishedCallback)
    {
        lines = dialogLines;
        currentIndex = 0;
        onFinished = finishedCallback;

        gameObject.SetActive(true);

        ShowCurrentLine();
    }

    public void OnClickNext()
    {
        currentIndex++;

        if (lines == null || currentIndex >= lines.Length)
        {
            Close();
            onFinished?.Invoke();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (dialogText == null)
            return;

        if (lines == null || lines.Length == 0)
        {
            dialogText.text = "";
            return;
        }

        dialogText.text = lines[currentIndex];
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}