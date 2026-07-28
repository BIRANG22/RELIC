using UnityEngine;

public class ObjectActiveStateController : MonoBehaviour
{
    [Header("켜고 끌 오브젝트")]
    [SerializeField] private GameObject controlledObject;

    [Header("활성화 상태를 확인할 오브젝트들")]
    [SerializeField] private GameObject[] checkObjects;

    private void Update()
    {
        if (controlledObject == null)
        {
            return;
        }

        bool anyObjectActive = false;

        foreach (GameObject checkObject in checkObjects)
        {
            if (checkObject != null && checkObject.activeInHierarchy)
            {
                anyObjectActive = true;
                break;
            }
        }

        // 확인 대상 중 하나라도 켜져 있으면 끄기
        // 확인 대상이 모두 꺼져 있으면 다시 켜기
        controlledObject.SetActive(!anyObjectActive);
    }
}