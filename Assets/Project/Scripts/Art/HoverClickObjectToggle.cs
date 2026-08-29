using UnityEngine;
using UnityEngine.EventSystems;

public class HoverClickObjectToggle : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Targets")]
    [Tooltip("호버 또는 클릭 선택 상태일 때 활성화할 오브젝트들입니다. Inspector에서 원하는 만큼 추가할 수 있습니다.")]
    [SerializeField] private GameObject[] targetObjects;

    [Header("Click")]
    [Tooltip("체크하면 클릭 후에도 활성화 상태를 유지합니다. 기본값은 유지하지 않음입니다.")]
    [SerializeField] private bool keepActiveOnClick = false;

    [Tooltip("클릭 유지 기능을 사용할 때, 같은 버튼을 다시 클릭하면 선택 상태를 해제합니다.")]
    [SerializeField] private bool toggleOnClick = true;

    [Tooltip("활성화될 때 항상 선택 상태를 초기화합니다.")]
    [SerializeField] private bool resetSelectionOnEnable = true;

    private bool isHovered;
    private bool isSelected;

    private void Awake()
    {
        RefreshTargetState();
    }

    private void OnEnable()
    {
        isHovered = false;

        if (resetSelectionOnEnable)
            isSelected = false;

        RefreshTargetState();
    }

    private void OnDisable()
    {
        isHovered = false;
        isSelected = false;
        SetTargetsActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        RefreshTargetState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        RefreshTargetState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!keepActiveOnClick)
        {
            isSelected = false;
            RefreshTargetState();
            return;
        }

        if (toggleOnClick)
            isSelected = !isSelected;
        else
            isSelected = true;

        RefreshTargetState();
    }

    public void SetSelected(bool selected)
    {
        isSelected = keepActiveOnClick && selected;
        RefreshTargetState();
    }

    public void ClearSelected()
    {
        isSelected = false;
        RefreshTargetState();
    }

    private void RefreshTargetState()
    {
        bool active = isHovered || (keepActiveOnClick && isSelected);
        SetTargetsActive(active);
    }

    private void SetTargetsActive(bool active)
    {
        if (targetObjects == null)
            return;

        for (int i = 0; i < targetObjects.Length; i++)
        {
            GameObject target = targetObjects[i];

            if (target != null)
                target.SetActive(active);
        }
    }
}
