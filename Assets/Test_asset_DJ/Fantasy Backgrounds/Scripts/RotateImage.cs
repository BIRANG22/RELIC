using UnityEngine;

public class RotateImageZ : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 20f;

    private void Update()
    {
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }
}