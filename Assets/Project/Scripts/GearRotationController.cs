using UnityEngine;

public class GearRotationController : MonoBehaviour
{
    [Header("기어 연결")]
    [SerializeField] private RectTransform leftLargeGear;
    [SerializeField] private RectTransform leftSmallGear;
    [SerializeField] private RectTransform rightLargeGear;
    [SerializeField] private RectTransform rightMediumGear;
    [SerializeField] private RectTransform rightSmallGear;

    [Header("회전 속도 (초당 Z 각도)")]
    [SerializeField] private float leftLargeGearSpeed = -10f;
    [SerializeField] private float leftSmallGearSpeed = 20f;
    [SerializeField] private float rightLargeGearSpeed = 10f;
    [SerializeField] private float rightMediumGearSpeed = -15f;
    [SerializeField] private float rightSmallGearSpeed = -20f;

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        RotateGear(leftLargeGear, leftLargeGearSpeed, deltaTime);
        RotateGear(leftSmallGear, leftSmallGearSpeed, deltaTime);
        RotateGear(rightLargeGear, rightLargeGearSpeed, deltaTime);
        RotateGear(rightMediumGear, rightMediumGearSpeed, deltaTime);
        RotateGear(rightSmallGear, rightSmallGearSpeed, deltaTime);
    }

    private static void RotateGear(RectTransform gear, float speed, float deltaTime)
    {
        if (gear == null)
            return;

        gear.Rotate(0f, 0f, speed * deltaTime);
    }
}
