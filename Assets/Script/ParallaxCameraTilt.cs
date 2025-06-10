using UnityEngine;

/// <summary>
/// 驾驶舱摄像头伪3D左右晃动控制器（倒U型轨迹 + 平滑启动）
/// </summary>
public class Parallax3DDriver : MonoBehaviour
{
    [Header("移动来源")]
    [Tooltip("用于判断是否在移动状态的机体 Rigidbody")]
    public Rigidbody sourceRigidbody;

    [Tooltip("判断为移动状态的最小速度")]
    public float moveThreshold = 0.1f;

    [Header("晃动参数")]
    [Tooltip("左右摆动幅度（单位：米）")]
    public float swayAmplitudeX = 0.03f;

    [Tooltip("上下摆动幅度（单位：米，负值为下压）")]
    public float swayAmplitudeY = 0.015f;

    [Tooltip("完整摆动周期（秒）")]
    public float swayCycleDuration = 1.5f;

    [Tooltip("停止后回正速度")]
    public float recoverySpeed = 5f;

    private float swayTimer = 0f;
    private Vector3 currentOffset = Vector3.zero;
    private Vector3 initialLocalPosition;
    private bool wasMovingLastFrame = false;

    void Awake()
    {
        initialLocalPosition = transform.localPosition;
    }

    void Update()
    {
        if (sourceRigidbody == null) return;

        Vector3 velocity = sourceRigidbody.linearVelocity;
        float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;

        bool isMoving = horizontalSpeed > moveThreshold;

        if (isMoving)
        {
            if (!wasMovingLastFrame)
                swayTimer = 0f;

            swayTimer += Time.deltaTime;
            float phase = (swayTimer / swayCycleDuration) * Mathf.PI * 2f;

            float offsetX = Mathf.Sin(phase) * swayAmplitudeX;
            float offsetY = -Mathf.Abs(Mathf.Sin(phase)) * swayAmplitudeY;

            currentOffset = new Vector3(offsetX, offsetY, 0f);
        }
        else
        {
            currentOffset = Vector3.Lerp(currentOffset, Vector3.zero, Time.deltaTime * recoverySpeed);
        }

        wasMovingLastFrame = isMoving;
        transform.localPosition = initialLocalPosition + currentOffset;
    }
}
