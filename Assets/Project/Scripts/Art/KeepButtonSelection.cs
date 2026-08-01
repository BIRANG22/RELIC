using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeepButtonSelection : MonoBehaviour
{
    private GameObject lastSelectedButton;

    private void LateUpdate()
    {
        if (EventSystem.current == null)
            return;

        GameObject currentSelected =
            EventSystem.current.currentSelectedGameObject;

        // 다른 버튼이 선택된 경우
        if (TryGetValidButton(currentSelected, out Button selectedButton))
        {
            lastSelectedButton = selectedButton.gameObject;
            return;
        }

        // 버튼이 아닌 곳을 눌러 선택이 해제되거나
        // 다른 UI 오브젝트가 선택된 경우 기존 버튼을 즉시 다시 선택
        if (lastSelectedButton != null &&
            lastSelectedButton.activeInHierarchy &&
            TryGetValidButton(lastSelectedButton, out _))
        {
            EventSystem.current.SetSelectedGameObject(lastSelectedButton);
        }
    }

    private bool TryGetValidButton(
        GameObject target,
        out Button button)
    {
        button = null;

        if (target == null)
            return false;

        button = target.GetComponent<Button>();

        return button != null &&
               button.isActiveAndEnabled &&
               button.interactable;
    }

    // 외부에서 특정 버튼을 선택할 때 사용
    public void SelectButton(Button button)
    {
        if (button == null ||
            !button.isActiveAndEnabled ||
            !button.interactable ||
            EventSystem.current == null)
        {
            return;
        }

        lastSelectedButton = button.gameObject;
        EventSystem.current.SetSelectedGameObject(lastSelectedButton);
    }

    // 선택 기록을 초기화할 때 사용
    public void ClearSelection()
    {
        lastSelectedButton = null;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}