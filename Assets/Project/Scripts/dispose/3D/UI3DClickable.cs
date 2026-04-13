using UnityEngine;
using UnityEngine.Events;

public class UI3DClickable : MonoBehaviour
{
    [SerializeField] private UnityEvent onClick;

    public void OnClick(RaycastHit hit)
    {
        Debug.Log($"{name} clicked by UI3D raycaster");
        onClick?.Invoke();
    }
}