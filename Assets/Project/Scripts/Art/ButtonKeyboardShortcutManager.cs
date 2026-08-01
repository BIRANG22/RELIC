using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ButtonKeyboardShortcutManager : MonoBehaviour
{
    [Serializable]
    public class ButtonShortcut
    {
        [Tooltip("키보드 입력으로 실행할 UI 버튼")]
        public Button targetButton;

        [Tooltip("버튼을 실행할 키")]
        public Key shortcutKey = Key.Enter;

        [Tooltip("버튼이 비활성화되어 있거나 상호작용 불가능하면 실행하지 않음")]
        public bool checkInteractable = true;
    }

    [Header("버튼 단축키 목록")]
    [SerializeField]
    private List<ButtonShortcut> buttonShortcuts = new List<ButtonShortcut>();

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        foreach (ButtonShortcut shortcut in buttonShortcuts)
        {
            if (shortcut == null || shortcut.targetButton == null)
                continue;

            if (!Keyboard.current[shortcut.shortcutKey].wasPressedThisFrame)
                continue;

            if (!CanExecute(shortcut))
                continue;

            shortcut.targetButton.onClick.Invoke();
        }
    }

    private bool CanExecute(ButtonShortcut shortcut)
    {
        Button button = shortcut.targetButton;

        if (!button.gameObject.activeInHierarchy)
            return false;

        if (shortcut.checkInteractable && !button.interactable)
            return false;

        return true;
    }
}