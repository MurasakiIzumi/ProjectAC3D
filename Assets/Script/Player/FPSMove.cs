using UnityEngine;

/// <summary>
/// FPS 机体控制器（含左右冲刺时镜头倾斜反馈）
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FPSMove : MonoBehaviour
{
    [Header("== 组件绑定 ==")]
    [Tooltip("CameraRig挂点，仅用于获取方向")]
    public Transform cameraRig;

    [Tooltip("可视模型挂点（用于机体旋转表现）")]
    public Transform visualRoot;

    [Tooltip("镜头倾斜容器节点（控制Z轴倾斜）")]
    public Transform cameraTiltRoot;

    private Rigidbody rb;

    // ────────────── 移动参数 ──────────────
    [Header("== 移动参数 ==")]
    [Tooltip("常规移动速度")]
    public float moveSpeed = 5f;

    // ────────────── 冲刺参数 ──────────────
    [Header("== 冲刺参数 ==")]
    [Tooltip("冲刺速度")]
    public float dashSpeed = 15f;

    [Tooltip("点按冲刺时间（秒）")]
    public float dashTapDuration = 0.25f;

    [Tooltip("长按冲刺最大持续时间")]
    public float dashMaxHoldTime = 1f;

    [Tooltip("每秒冲刺消耗能量")]
    public float dashEnergyPerSecond = 30f;

    // ────────────── 能量系统 ──────────────
    [Header("== 能量系统 ==")]
    [Tooltip("最大能量")]
    public float maxEnergy = 100f;

    [Tooltip("能量恢复速度（每秒）")]
    public float energyRecoveryRate = 50f;

    [Tooltip("停止使用后多少秒开始恢复")]
    public float energyRecoveryDelay = 1.5f;

    // ────────────── 倾斜参数 ──────────────
    [Header("== 镜头倾斜参数 ==")]
    [Tooltip("冲刺时最大倾斜角度（正为右倾）")]
    public float maxTiltAngle = 10f;

    [Tooltip("冲刺触发时的倾斜速度（度/秒）")]
    public float tiltInSpeed = 180f;

    [Tooltip("冲刺结束后的回正速度（度/秒）")]
    public float tiltOutSpeed = 60f;

    private float currentTiltZ = 0f;
    private float targetTiltZ = 0f;

    // ────────────── 状态变量 ──────────────
    private float currentEnergy;
    private float lastEnergyUseTime;

    private bool isDashing = false;
    private bool isDashHeld = false;
    private float dashTimer = 0f;

    private Vector3 dashDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentEnergy = maxEnergy;
    }

    void Update()
    {
        HandleDashInput();
        RecoverEnergy();
        UpdateCameraTilt();
    }

    void FixedUpdate()
    {
        // 同步视觉模型朝向
        Vector3 camForward = GetCameraForward();
        if (camForward.sqrMagnitude > 0.001f && visualRoot != null)
        {
            Quaternion targetRot = Quaternion.LookRotation(camForward);
            visualRoot.rotation = targetRot;
        }

        if (isDashing)
        {
            HandleDashMovement();
            return;
        }

        HandleMovement();
    }

    // ────────────── 普通移动 ──────────────
    void HandleMovement()
    {
        Vector3 inputDir = GetInputDirection();
        rb.linearVelocity = inputDir * moveSpeed;
    }

    // ────────────── 冲刺输入 ──────────────
    void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryStartDash();
            isDashHeld = true;
            dashTimer = 0f;
        }

        if (Input.GetKey(KeyCode.Space)) isDashHeld = true;
        if (Input.GetKeyUp(KeyCode.Space)) isDashHeld = false;
    }

    void TryStartDash()
    {
        Vector3 dir = GetInputDirection();
        if (dir.sqrMagnitude < 0.1f)
            dir = GetCameraForward();

        float minEnergy = dashEnergyPerSecond * 0.1f;
        if (currentEnergy < minEnergy) return;

        dashDirection = dir.normalized;
        isDashing = true;
        dashTimer = 0f;

        // 判断是否横向冲刺，决定镜头倾斜角度
        float dotRight = Vector3.Dot(dashDirection, GetCameraRight());
        if (dotRight > 0.7f)
            targetTiltZ = +maxTiltAngle;
        else if (dotRight < -0.7f)
            targetTiltZ = -maxTiltAngle;
        else
            targetTiltZ = 0f;
    }

    void HandleDashMovement()
    {
        dashTimer += Time.fixedDeltaTime;

        float cost = dashEnergyPerSecond * Time.fixedDeltaTime;
        if (currentEnergy >= cost)
        {
            currentEnergy -= cost;
            lastEnergyUseTime = Time.time;
        }
        else
        {
            StopDash();
            return;
        }

        rb.linearVelocity = dashDirection * dashSpeed;

        if (!isDashHeld || dashTimer >= dashMaxHoldTime)
            StopDash();
    }

    void StopDash()
    {
        isDashing = false;
        dashTimer = 0f;
        isDashHeld = false;
        targetTiltZ = 0f;
    }

    // ────────────── 镜头倾斜更新 ──────────────
    void UpdateCameraTilt()
    {
        if (cameraTiltRoot == null) return;

        float delta = Mathf.Abs(targetTiltZ - currentTiltZ);
        float lerpSpeed = (Mathf.Approximately(targetTiltZ, 0f)) ? tiltOutSpeed : tiltInSpeed;
        currentTiltZ = Mathf.LerpAngle(currentTiltZ, targetTiltZ, Time.deltaTime * lerpSpeed / (delta + 0.01f)); ;
        Vector3 angles = cameraTiltRoot.localEulerAngles;
        angles.z = -currentTiltZ;
        cameraTiltRoot.localEulerAngles = angles;
    }

    // ────────────── 能量恢复 ──────────────
    void RecoverEnergy()
    {
        if (Time.time - lastEnergyUseTime < energyRecoveryDelay || isDashing) return;

        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyRecoveryRate * Time.deltaTime);
    }

    // ────────────── 工具函数 ──────────────
    Vector3 GetCameraForward()
    {
        Vector3 fwd = cameraRig.forward;
        fwd.y = 0;
        return fwd.normalized;
    }

    Vector3 GetCameraRight()
    {
        Vector3 right = cameraRig.right;
        right.y = 0;
        return right.normalized;
    }

    Vector3 GetInputDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        return (GetCameraForward() * v + GetCameraRight() * h).normalized;
    }

    public float GetEnergyNormalized() => currentEnergy / maxEnergy;
}
