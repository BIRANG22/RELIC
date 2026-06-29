using UnityEngine;

public class SpriteRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("초당 회전 속도입니다. Z값을 사용하면 2D 스프라이트가 화면 기준으로 회전합니다.")]
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 0f, 90f);

    [Tooltip("체크하면 회전이 진행됩니다.")]
    [SerializeField] private bool rotateOnStart = true;

    [Tooltip("체크하면 로컬 회전 기준으로 회전합니다.")]
    [SerializeField] private bool useLocalRotation = true;

    private bool isRotating;

    private void Awake()
    {
        isRotating = rotateOnStart;
    }

    private void Update()
    {
        if (!isRotating)
            return;

        Vector3 rotateAmount = rotationSpeed * Time.deltaTime;

        if (useLocalRotation)
        {
            transform.Rotate(rotateAmount, Space.Self);
        }
        else
        {
            transform.Rotate(rotateAmount, Space.World);
        }
    }

    public void StartRotation()
    {
        isRotating = true;
    }

    public void StopRotation()
    {
        isRotating = false;
    }

    public void SetRotationSpeed(Vector3 newSpeed)
    {
        rotationSpeed = newSpeed;
    }

    public void SetZRotationSpeed(float zSpeed)
    {
        rotationSpeed = new Vector3(0f, 0f, zSpeed);
    }
}