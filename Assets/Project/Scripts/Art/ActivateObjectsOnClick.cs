using UnityEngine;
using UnityEngine.UI;

public class ActivateObjectsOnClick : MonoBehaviour
{
    [Header("버튼 클릭 시 활성화할 오브젝트")]
    [SerializeField] private GameObject[] targetObjects;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(ActivateObjects);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(ActivateObjects);
        }
    }

    private void ActivateObjects()
    {
        foreach (GameObject target in targetObjects)
        {
            if (target != null)
            {
                target.SetActive(true);
            }
        }
    }
}
