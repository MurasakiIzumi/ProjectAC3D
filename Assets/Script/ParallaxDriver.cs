using UnityEngine;

/// <summary>
/// 驾驶舱伪3D左右晃动控制器（倒U型轨迹 + 平滑启动）
/// </summary>
public class ParallaxDriver : MonoBehaviour
{
    [Header("移动来源")]
    [Tooltip("用于判断是否在移动状态的机体 Rigidbody")]
    public Rigidbody sourceRigidbody;

    [Tooltip("判断为移动状态的最小速度")]
    public float moveThreshold = 0.1f;

    [Header("晃动参数")]
    [Tooltip("左右摆动幅度（单位：像素）")]
    public float swayAmplitudeX = 30f;

    [Tooltip("上下摆动幅度（单位：像素，负值为下压）")]
    public float swayAmplitudeY = 10f;

    [Tooltip("完整摆动周期（秒）")]
    public float swayCycleDuration = 1.5f;

    [Tooltip("停止后回正速度")]
    public float recoverySpeed = 5f;

    private RectTransform rectTransform;
    private float swayTimer = 0f;
    private Vector2 currentOffset = Vector2.zero;

    private bool wasMovingLastFrame = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (sourceRigidbody == null || rectTransform == null)
            return;

        Vector3 velocity = sourceRigidbody.linearVelocity;
        float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;

        bool isMoving = horizontalSpeed > moveThreshold;

        if (isMoving)
        {
            if (!wasMovingLastFrame)
            {
                swayTimer = 0f; // 回到初始摆动相位
            }

            swayTimer += Time.deltaTime;
            float phase = (swayTimer / swayCycleDuration) * Mathf.PI * 2f;

            // 左右摇：正弦波
            float offsetX = Mathf.Sin(phase) * swayAmplitudeX;

            // 上下摇：倒U轨迹（abs(sin)）
            float offsetY = -Mathf.Abs(Mathf.Sin(phase)) * swayAmplitudeY;

            currentOffset = new Vector2(offsetX, offsetY);
        }
        else
        {
            // 停止时缓慢回中
            currentOffset = Vector2.Lerp(currentOffset, Vector2.zero, Time.deltaTime * recoverySpeed);
        }

        wasMovingLastFrame = isMoving;
        rectTransform.anchoredPosition = currentOffset;
    }
}
