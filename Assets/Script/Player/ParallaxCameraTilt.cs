using UnityEngine;

/// <summary>
/// 驾驶舱摄像头伪3D左右晃动控制器（倒U型轨迹 + 平滑起摆）
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

    [Tooltip("启动摆动的平滑时间（秒）")]
    public float swayStartDuration = 0.2f;

    [Header("音效")]
    [Tooltip("晃动至顶点时播放的脚步声")]
    public AudioClip stepSE;

    [Tooltip("音效播放组件")]
    public AudioSource audioSource;

    private float swayTimer = 0f;
    private Vector3 currentOffset = Vector3.zero;
    private Vector3 initialLocalPosition;

    private bool wasMovingLastFrame = false;
    private float swayWeight = 0f; // 平滑启动因子
    private float previousSin = 0f;  // 上一帧的 sin 值
    private bool hasPlayedFirstStep = false;

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
            swayTimer += Time.deltaTime;
            float phase = (swayTimer / swayCycleDuration) * Mathf.PI * 2f;

            // 平滑提升晃动权重
            swayWeight = Mathf.MoveTowards(swayWeight, 1f, Time.deltaTime / swayStartDuration);

            float sinVal = Mathf.Sin(phase);

            // 检查是否过零点 → 播放脚步声
            if ((previousSin <= 0f && sinVal > 0f) || (previousSin >= 0f && sinVal < 0f))
            {
                // 允许第一次播放时使用较低的swayWeight阈值
                float dynamicThreshold = wasMovingLastFrame ? 0.7f : 0.05f;

                if (swayWeight > dynamicThreshold && audioSource != null && stepSE != null)
                {
                    audioSource.PlayOneShot(stepSE);
                }
            }
            previousSin = sinVal;

            float offsetX = sinVal * swayAmplitudeX * swayWeight;
            float offsetY = -Mathf.Abs(sinVal) * swayAmplitudeY * swayWeight;

            currentOffset = new Vector3(offsetX, offsetY, 0f);
        }

        wasMovingLastFrame = isMoving;
        transform.localPosition = initialLocalPosition + currentOffset;
    }
}

